using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void PrepareMoth(Arena a)
    {
        EnemyProfile(a, "ember_moth"); a.Enemy.Position = new(103, 100); a.Friend.Position = new(110, 110);
        a.Hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 37);
    }

    [Fact]
    public void EmberPublicCommandsMatchOriginalLockedDivePulsesFriendlyFireImmunityAndCasterDeath()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-ember-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareMoth(a);
            Actor? other = null; a.Combat.State.RandomState = 37; int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                if (index == 2 && name == "friendly") other = RevivalTestEnemy(a, "mossbound_guard", new(100, 100));
                if (index == 3 && name == "death") { a.Hero.Position = new(102, 100); a.Enemy.Health = 4; }
                if (index == 4 && name == "death") a.Hero.Position = new(100, 100);
                if (index == 3 && name == "other_moth")
                { a.Enemy.Position = new(101, 100); other = RevivalTestEnemy(a, "ember_moth", new(100, 100)); }
                a.World.Reveal(); long first = a.Combat.State.NextEvent;
                var action = command.GetString() switch { "basic_attack" => TacticalAction.BasicAttack, "move" => TacticalAction.Move, _ => TacticalAction.Wait };
                a.Act(action, action == TacticalAction.Move ? Facing.South : Facing.East);
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "EmberCreated" or "MothLandingBlocked" => $"{e.Kind}:{e.Time}:{e.Position.X - 100}:{e.Position.Y - 100}",
                    "EmberPulsed" => $"{e.Kind}:{e.Time}:{e.Amount}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}", _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("x").GetInt32(), 100 + expected.GetProperty("y").GetInt32()), a.Hero.Position);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("enemy_x").GetInt32(), 100 + expected.GetProperty("enemy_y").GetInt32()), a.Enemy.Position);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(expected.GetProperty("dives").GetInt32(), a.Enemy.Tactics!.Enemy!.EmberDives);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Hero.Tactics!.Build!.Level > 1 ? a.Hero.Tactics.Build.RandomState : a.Combat.State.RandomState);
                Assert.Equal(expected.GetProperty("embers").EnumerateArray().Select(e =>
                    (e.GetProperty("x").GetInt32(), e.GetProperty("y").GetInt32(), e.GetProperty("remaining").GetInt32(), e.GetProperty("due").GetInt64(), e.GetProperty("expires").GetInt64())),
                    a.Combat.State.Embers.Select(e => (e.Position.X - 100, e.Position.Y - 100, e.Remaining, e.NextDue, e.Expires)));
                foreach (var (key, kind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                    Assert.Equal(expected.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : expected.GetProperty(key).GetInt64(),
                        a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == kind)?.Due);
                if (other != null) Assert.Equal(expected.GetProperty("other_hp").GetInt32(), other.Health);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(40, decisions);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("active")]
    [InlineData("dead")]
    [InlineData("mixed")]
    public void EmberWorldSavesContinuePendingAndActiveFireIncludingCasterDeathBurningAndEchoes(string phase)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareMoth(a);
        SentinelWait(a, phase == "pending" ? 1 : 3);
        if (phase == "dead") { a.Hit(a.Enemy, 1000, source: a.Hero); Assert.True(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id)); }
        if (phase == "mixed")
        {
            var guard = RevivalTestEnemy(a, "mossbound_guard", new(101, 100));
            Grant(a.Hero, "core:boon/emberbrand"); Grant(a.Hero, "core:boon/temporal_echo");
            a.Act(TacticalAction.BasicAttack); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
            Assert.NotNull(guard.Tactics!.Burning); Assert.NotEmpty(a.Combat.State.BoonEffects.Echoes); Assert.Single(a.Combat.State.Embers);
        }
        string directory = Path.Combine(Path.GetTempPath(), "roag-ember-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            for (int i = 0; i < 5; i++)
            {
                a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); RunRevivalEnemies(loaded, resumed, a.Hero.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
            }
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void EmberWarningsUseBurningTileVisionAndExposeExactPulseTimeWithoutMutation()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareMoth(a); SentinelWait(a, 3);
        var ember = Assert.Single(a.Combat.State.Embers);
        a.World.Visible.Clear(); a.World.Visible.Add(ember.Position);
        string before = JsonSerializer.Serialize(a.World.State);
        for (int i = 0; i < 10; i++)
        {
            var warning = Assert.Single(a.Combat.VisibleAttacks(a.World.Visible));
            Assert.Equal(EnemyEmbers.Pulse, warning.Ability); Assert.Equal(330, warning.Due); Assert.Equal(1, warning.Damage);
            warning.Cells[0] = new(90, 90);
            Assert.Contains(a.Combat.VisibleTimeline(), w => w.Kind == ScheduledKind.EmberPulse && w.Due == 330);
        }
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.World.Visible.Clear(); a.World.Visible.Add(a.Enemy.Position);
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        Assert.DoesNotContain(a.Combat.VisibleTimeline(), w => w.Kind == ScheduledKind.EmberPulse);
    }

    [Fact]
    public void EmberOffscreenDiveAndPulsesAdvanceWithoutLeakingToTheSelectedMember()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 50);
        EnemyProfile(a, "ember_moth"); a.Enemy.Position = new(103, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.World.Visible.Clear(); string before = JsonSerializer.Serialize(a.World.State);
        Assert.Equal(EnemyEmbers.Dive, a.Combat.PreviewEnemyTurn(a.Friend.Id, (_, _) => null).Ability);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next();
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        for (int i = 0; i < 4; i++) { a.Act(TacticalAction.Wait); a.Next(); }
        Assert.Equal(a.Friend.MaxHealth - 4, a.Friend.Health); Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.Empty(a.Combat.State.Embers);
        Assert.Equal(new long[] { 330, 430 }, a.Combat.State.Events.Where(e => e.Kind == "EmberPulsed").Select(e => e.Time));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void EmberGroundPulsesBypassDirectionalGuardAndShortenDownedBleedout()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 50);
        PrepareMoth(a); a.Friend.Position = new(110, 110);
        // Advance both controlled members while allowing the actual enemy controller to commit.
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait);
        a.Combat.AdvanceToDecision(); a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); a.Next();
        for (int i = 0; i < 2; i++) { a.Act(TacticalAction.Wait); a.Next(); }
        a.Combat.State.Tuning = new(GuardReductionPercent: 100);
        a.Act(TacticalAction.Guard); a.Next(); Assert.Equal(a.Hero.MaxHealth - 3, a.Hero.Health);
        a.Hit(a.Hero, 1000); Assert.Equal(LifeState.Downed, a.Hero.Life); int turns = a.Hero.Tactics!.BleedTurns;
        a.Next(a.Friend.Id);
        // The next ground pulse pays an additional bleed turn, besides the member's own turn.
        while (a.Combat.Clock < 430) { a.Act(TacticalAction.Wait); a.Next(a.Friend.Id); }
        Assert.True(a.Hero.Tactics.BleedTurns <= turns - 2);
    }

    [Theory]
    [InlineData("count")]
    [InlineData("owner")]
    [InlineData("due")]
    [InlineData("timer")]
    [InlineData("orphan")]
    [InlineData("terrain")]
    public void EmberSavesRejectForgedBudgetsLifetimesTerrainAndMissingOrOrphanedTimers(string corruption)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareMoth(a); SentinelWait(a, 3);
        var ember = Assert.Single(a.Combat.State.Embers);
        switch (corruption)
        {
            case "count": a.Combat.State.Embers[0] = ember with { Remaining = 3 }; break;
            case "owner": a.Combat.State.Embers[0] = ember with { Source = a.Hero.Id }; break;
            case "due": a.Combat.State.Embers[0] = ember with { NextDue = 299 }; break;
            case "timer": a.Combat.State.Queue.Cancel(ember.Schedule); break;
            case "orphan": a.Combat.State.Embers.Clear(); break;
            case "terrain": a.World.Chunks.Edit(ember.Position, Material.Water); break;
        }
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void EmberInterruptedAttemptsStaySpentAndNeverScheduleFire()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareMoth(a); SentinelWait(a, 1);
        Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        Assert.Equal(1, a.Enemy.Tactics!.Enemy!.EmberDives); Assert.Empty(a.Combat.State.Attacks);
        a.Next(a.Enemy.Id); a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(2, a.Enemy.Tactics.Enemy.EmberDives);
        Apply(a, Activation(a, a.Hero, "core:boon/anchor_maul", 0, a.Enemy), BoonEffectKind.DisplaceTarget);
        Assert.Empty(a.Combat.State.Attacks); a.Next(a.Enemy.Id);
        Assert.NotEqual(EnemyEmbers.Dive, a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null).Ability);
        Assert.Empty(a.Combat.State.Embers); Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, w => w.Kind == ScheduledKind.EmberPulse);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("active")]
    [InlineData("at_pulse")]
    public void EmberTerrainInvalidationCancelsFireAndItsScheduledWorkWithoutRewritingTerrain(string phase)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareMoth(a);
        SentinelWait(a, phase == "pending" ? 1 : 3);
        if (phase == "at_pulse")
        {
            var resumed = new TacticalCombat(a.World, time => { if (time == 330) a.World.Chunks.Edit(new(100, 100), Material.Water); });
            Assert.True(resumed.Commit(new(TacticalAction.Wait)).Success);
            RunRevivalEnemies(a.World, resumed, a.Hero.Id);
            Assert.DoesNotContain(resumed.State.Events, e => e.Kind == "EmberPulsed");
        }
        else
        {
            a.World.Chunks.Edit(new(100, 100), Material.Water);
            SentinelWait(a, 2);
        }
        Assert.Empty(a.Combat.State.Embers);
        Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, w => w.Kind == ScheduledKind.EmberPulse);
        Assert.Equal(Material.Water, a.World.Chunks.At(new(100, 100)));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmberOverlapAndGlobalCapRefuseNewFireWithoutRefreshingExistingPulses(bool cap)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareMoth(a); SentinelWait(a, 2);
        var owners = Enumerable.Range(0, cap ? 4 : 1).Select(i => RevivalTestEnemy(a, "ember_moth", new(108, 100 + i))).ToArray();
        foreach (var owner in owners) owner.Tactics!.Enemy!.EmberDives = 2;
        for (int i = 0; i < (cap ? 8 : 1); i++)
        {
            long action = a.Combat.State.NextAction++; var owner = owners[i / 2];
            var work = a.Combat.State.Queue.Schedule(ScheduledKind.EmberPulse, 300, owner.Id, referenceId: action);
            a.Combat.State.Embers.Add(new(action, owner.Id, cap ? new(95 + i, 95) : new(100, 100), 2, 300, 400, work.Sequence));
        }
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        long first = a.Combat.State.NextEvent; SentinelWait(a, 1);
        Assert.Equal(cap ? 8 : 1, a.Combat.State.Embers.Count);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Sequence >= first && e.Kind == "EmberCreated");
        Assert.All(a.Combat.State.Embers, e => { Assert.Equal(1, e.Remaining); Assert.Equal(400, e.NextDue); Assert.Equal(400, e.Expires); });
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("landing")]
    [InlineData("delivery")]
    [InlineData("owner")]
    [InlineData("budget")]
    public void EmberPendingDiveRejectsForgedLandingDeliveryRoleAndBudget(string corruption)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareMoth(a); SentinelWait(a, 1);
        var attack = Assert.Single(a.Combat.State.Attacks);
        if (corruption == "landing") a.Combat.State.Attacks[0] = attack with { EmberLanding = attack.EmberLanding!.Value.Offset(1, 0) };
        if (corruption == "delivery") a.Combat.State.Attacks[0] = attack with { Delivery = AttackDelivery.Projectile };
        if (corruption == "owner") a.Enemy.Tactics!.DefinitionId = "core:enemy/silt_worm";
        if (corruption == "budget") a.Enemy.Tactics!.Enemy!.EmberDives = 0;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }
}
