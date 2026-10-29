using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void PrepareSiltWorm(Arena a)
    {
        EnemyProfile(a, "silt_worm"); a.Enemy.Position = new(103, 100); a.Friend.Position = new(110, 110);
        a.Hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 37);
    }

    [Fact]
    public void SiltPublicCommandsMatchOriginalChargeBlockersVacatedTrailsMovementCostsExpiryAndDeath()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-silt-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSiltWorm(a);
            Actor? guard = name == "blocker" ? RevivalTestEnemy(a, "mossbound_guard", new(102, 100)) : null;
            if (name == "spent") a.Enemy.Tactics!.Enemy!.SiltCharges = 2;
            a.Combat.State.RandomState = 37; int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                if (index == 3)
                {
                    if (name == "walk") a.Hero.Position = new(102, 101);
                    if (name is "dash" or "enemy" or "twice") a.Hero.Position = new(104, 100);
                    if (name == "enemy") guard = RevivalTestEnemy(a, "mossbound_guard", new(102, 101), 301);
                    if (name == "death") a.Enemy.Health = 4;
                }
                a.World.Reveal(); long first = a.Combat.State.NextEvent;
                var action = command.GetString() switch { "basic_attack" => TacticalAction.BasicAttack,
                    "move" => TacticalAction.Move, "dash" => TacticalAction.Dash, _ => TacticalAction.Wait };
                var facing = name switch { "walk" => Facing.North, "dash" => Facing.West, "leave" => Facing.South, _ => Facing.East };
                var preview = a.Combat.Preview(new(action, facing));
                a.Act(action, facing); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "SiltTrailCreated" => $"{e.Kind}:{e.Time}:{e.Position.X - 100}:{e.Position.Y - 100}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}", _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("cost").GetInt64(), preview.Recovery);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("x").GetInt32(), 100 + expected.GetProperty("y").GetInt32()), a.Hero.Position);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("enemy_x").GetInt32(), 100 + expected.GetProperty("enemy_y").GetInt32()), a.Enemy.Position);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(expected.GetProperty("charges").GetInt32(), a.Enemy.Tactics!.Enemy!.SiltCharges);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Hero.Tactics!.Build!.Level > 1 ? a.Hero.Tactics.Build.RandomState : a.Combat.State.RandomState);
                Assert.Equal(expected.GetProperty("trails").EnumerateArray().Select(t =>
                    (t.GetProperty("x").GetInt32(), t.GetProperty("y").GetInt32(), t.GetProperty("expires").GetInt64())),
                    a.Combat.State.Silt.Select(s => (s.Position.X - 100, s.Position.Y - 100, s.Expires)));
                foreach (var (key, kind, id) in new[] { ("ready", ScheduledKind.ActorReady, a.Enemy.Id),
                    ("impact", ScheduledKind.Impact, a.Enemy.Id), ("guard_ready", ScheduledKind.ActorReady, guard?.Id ?? 0) })
                    Assert.Equal(expected.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : expected.GetProperty(key).GetInt64(),
                        a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == id && w.Kind == kind)?.Due);
                if (guard != null) Assert.Equal(expected.GetProperty("guard_hp").GetInt32(), guard.Health);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(45, decisions);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("trail")]
    [InlineData("dead")]
    public void SiltWorldSavesPreservePendingChargesTrailsAndExpiryAfterOwnerDeath(string phase)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSiltWorm(a);
        SentinelWait(a, phase == "pending" ? 1 : 3);
        if (phase == "dead") a.Hit(a.Enemy, 1000, source: a.Hero);
        string directory = Path.Combine(Path.GetTempPath(), "roag-silt-" + Guid.NewGuid().ToString("N"));
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
            Assert.Empty(resumed.State.Silt); TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(Material.Water)]
    [InlineData(Material.StairsDown)]
    [InlineData(Material.Camp)]
    public void SiltChargeRequiresPhysicalClearGroundAndNeverTargetsProtectedLandings(Material obstacle)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSiltWorm(a);
        a.World.Chunks.Edit(new(102, 100), obstacle);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        string before = JsonSerializer.Serialize(a.World.State);
        for (int i = 0; i < 10; i++) Assert.NotEqual(EnemySilt.Charge, a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null).Ability);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void SiltInterruptedChargesStaySpentAndCannotLeaveUntraversedTrails()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSiltWorm(a); SentinelWait(a, 1);
        Assert.Equal(1, a.Enemy.Tactics!.Enemy!.SiltCharges);
        Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        Assert.Empty(a.Combat.State.Attacks); Assert.Empty(a.Combat.State.Silt);
        a.Next(a.Enemy.Id); a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(2, a.Enemy.Tactics.Enemy.SiltCharges);
        Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        a.Next(a.Enemy.Id); Assert.NotEqual(EnemySilt.Charge, a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null).Ability);
        Assert.Empty(a.Combat.State.Silt); TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void SiltOffscreenChargeLeavesPhysicalTrailsThatExpireOnTheExactSharedClock()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 50);
        EnemyProfile(a, "silt_worm"); a.Enemy.Position = new(103, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.World.Visible.Clear(); string before = JsonSerializer.Serialize(a.World.State);
        Assert.Equal(EnemySilt.Charge, a.Combat.PreviewEnemyTurn(a.Friend.Id, (_, _) => null).Ability);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next();
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        for (int i = 0; i < 2; i++) { a.Act(TacticalAction.Wait); a.Next(); }
        Assert.Equal(2, a.Combat.State.Silt.Count); Assert.All(a.Combat.State.Silt, s => { Assert.Equal(1, s.Position.Floor); Assert.Equal(515, s.Expires); });
        a.Enemy.Position = new(105, 105); // The owner's later floor change does not move its physical trail.
        a.Act(TacticalAction.Wait);
        var ready = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id && w.Kind == ScheduledKind.ActorReady);
        Assert.True(a.Combat.State.Queue.Delay(ready.Sequence, 515 - ready.Due));
        a.Next(); Assert.Equal(515, a.Combat.Clock); Assert.Empty(a.Combat.State.Silt);
        Assert.Equal(Material.Floor, a.World.Chunks.At(new(103, 100, 1)));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("expiry")]
    [InlineData("owner")]
    [InlineData("overlap")]
    [InlineData("budget")]
    [InlineData("action")]
    [InlineData("ground")]
    public void SiltSavesRejectForgedTrailLifetimeOwnershipBudgetsAndTerrain(string corruption)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSiltWorm(a); SentinelWait(a, 3);
        var trail = a.Combat.State.Silt[0];
        switch (corruption)
        {
            case "expiry": a.Combat.State.Silt[0] = trail with { Expires = a.Combat.Clock }; break;
            case "owner": a.Combat.State.Silt[0] = trail with { Source = a.Hero.Id }; break;
            case "overlap": a.Combat.State.Silt[1] = trail; break;
            case "budget": a.Enemy.Tactics!.Enemy!.SiltCharges = 0; break;
            case "action": a.Combat.State.Silt[0] = trail with { Action = a.Combat.State.NextAction }; break;
            case "ground": a.World.Chunks.Edit(trail.Position, Material.Water); break;
        }
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void SiltGlobalCapRefusesNewCellsWithoutRefreshingExistingTrailsOrRewritingGround()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSiltWorm(a); SentinelWait(a, 1);
        var owners = Enumerable.Range(0, 3).Select(i => RevivalTestEnemy(a, "silt_worm", new(108, 100 + i))).ToArray();
        foreach (var owner in owners) owner.Tactics!.Enemy!.SiltCharges = 2;
        for (int pair = 0; pair < 6; pair++)
        {
            long action = a.Combat.State.NextAction++;
            for (int side = 0; side < 2; side++)
                a.Combat.State.Silt.Add(new(action, owners[pair / 2].Id, new(95 + pair, 95 + side), 400));
        }
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        string before = JsonSerializer.Serialize(a.Combat.State.Silt); long first = a.Combat.State.NextEvent;
        SentinelWait(a, 2);
        Assert.Equal(new WorldPosition(101, 100), a.Enemy.Position);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State.Silt));
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Sequence >= first && e.Kind == "SiltTrailCreated");
        Assert.Equal(Material.Floor, a.World.Chunks.At(new(103, 100)));
        SentinelWait(a, 1); Assert.Empty(a.Combat.State.Silt);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("lane")]
    [InlineData("direction")]
    [InlineData("advance")]
    [InlineData("delivery")]
    [InlineData("identity")]
    [InlineData("budget")]
    public void SiltPendingChargeRejectsForgedLaneDirectionAdvanceAndBudget(string corruption)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSiltWorm(a); SentinelWait(a, 1);
        var attack = Assert.Single(a.Combat.State.Attacks);
        a.Combat.State.Attacks[0] = corruption switch
        {
            "lane" => attack with { Cells = [attack.Cells[0].Offset(0, 1), attack.Cells[1]] },
            "direction" => attack with { Advance = new(Facing.East, 2) },
            "advance" => attack with { Advance = new(Facing.West, 3) },
            "delivery" => attack with { Delivery = AttackDelivery.Area }, _ => attack
        };
        if (corruption == "identity") a.Combat.State.Attacks[0] = attack with { Ability = "core:ability/worm_touch" };
        if (corruption == "budget") a.Enemy.Tactics!.Enemy!.SiltCharges = 0;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void SiltTerrainChangesPruneOnlyInvalidCellsAndPreviewsStayReadOnly()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSiltWorm(a); SentinelWait(a, 3);
        a.World.Chunks.Edit(new(103, 100), Material.Water);
        a.Hero.Position = new(102, 101);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Equal(140, a.Combat.Preview(new(TacticalAction.Move, Facing.North)).Recovery);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Act(TacticalAction.Move);
        Assert.Equal(new WorldPosition(102, 100), Assert.Single(a.Combat.State.Silt).Position);
        Assert.Equal(Material.Water, a.World.Chunks.At(new(103, 100)));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }
}
