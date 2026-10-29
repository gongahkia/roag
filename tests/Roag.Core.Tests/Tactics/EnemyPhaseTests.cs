using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void PreparePhaseBoss(Arena a, string definition)
    {
        EnemyProfile(a, definition); a.Enemy.Position = new(101, 100); a.Friend.Position = new(110, 110);
        a.Enemy.Health = a.Enemy.MaxHealth / 2 + 1;
        a.Hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 37);
        a.Combat.State.RandomState = 37;
    }

    [Fact]
    public void EnemyPhaseCommandsMatchAllOriginalBossesIncludingPendingAttackTimingAndPatternChanges()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-enemy-phases.json")));
        int commands = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            string definition = sample.GetProperty("input").GetProperty("enemy").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PreparePhaseBoss(a, definition);
            int i = 0;
            foreach (var command in sample.GetProperty("input").GetProperty("commands").EnumerateArray())
            {
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() == "wait" ? TacticalAction.Wait : TacticalAction.BasicAttack);
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
                var expected = sample.GetProperty("expected")[i++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "ElitePhaseChanged" or "RegentPhaseChanged" or "DamageTaken" or "DamageBlocked" => $"{e.Kind}:{e.Time}:{e.Amount}",
                    _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), definition + " / " + i + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(expected.GetProperty("speed").GetInt32(), a.Enemy.Speed);
                Assert.Equal(expected.GetProperty("phase").GetBoolean(), a.Enemy.Tactics!.Enemy!.PhaseTwo);
                Assert.Equal(expected.GetProperty("attacks").GetInt64(), a.Enemy.Tactics.Enemy.Attacks);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                foreach (var (key, kind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                    Assert.Equal(expected.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : expected.GetProperty(key).GetInt64(),
                        a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == kind)?.Due);
                var attack = a.Combat.State.Attacks.SingleOrDefault(x => x.Source == a.Enemy.Id);
                Assert.Equal(expected.GetProperty("cells").EnumerateArray().Select(c => new WorldPosition(100 + c.GetProperty("x").GetInt32(), 100 + c.GetProperty("y").GetInt32())), attack?.Cells ?? []);
                TacticalValidation.Validate(a.World.Plan, a.World.State); commands++;
            }
        }
        Assert.Equal(64, commands);
    }

    [Theory]
    [InlineData("thorn_warden")]
    [InlineData("mycelial_matriarch")]
    [InlineData("hollow_regent")]
    public void EnemyPhaseSaveKeepsCommittedImpactAndResumesWithFasterFutureRecovery(string definition)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PreparePhaseBoss(a, definition);
        SentinelWait(a, 1); var attack = Assert.Single(a.Combat.State.Attacks);
        int speed = a.Enemy.Speed; a.Act(TacticalAction.BasicAttack);
        Assert.True(a.Enemy.Tactics!.Enemy!.PhaseTwo); Assert.Equal(speed + 25, a.Enemy.Speed);
        Assert.Equal(JsonSerializer.Serialize(attack), JsonSerializer.Serialize(Assert.Single(a.Combat.State.Attacks)));
        RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
        string directory = Path.Combine(Path.GetTempPath(), "roag-phase-" + Guid.NewGuid().ToString("N"));
        try
        {
            a.World.Save(directory);
            using var loaded = WorldSession.Load(directory); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(speed + 25, loaded.State.Actors.Single(x => x.Id == a.Enemy.Id).Speed);
            for (int i = 0; i < 6; i++)
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

    [Theory]
    [InlineData(DamageCause.DirectAttack, false)]
    [InlineData(DamageCause.DirectAttack, true)]
    [InlineData(DamageCause.OngoingEffect, false)]
    [InlineData(DamageCause.GroundHazard, false)]
    public void EnemyPhaseReactsToOffscreenManualGeneratedAndEnvironmentalDamage(DamageCause cause, bool generated)
    {
        using var a = new Arena(ClassKind.Adventurer); PreparePhaseBoss(a, "mycelial_matriarch");
        a.Enemy.Position = new(101, 100, 1); a.Friend.Position = new(100, 100, 1); a.World.Visible.Clear();
        int speed = a.Enemy.Speed;
        string before = JsonSerializer.Serialize(a.World.State);
        a.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Friend.Id, a.Friend.Position, [a.Enemy.Position], "test:phase",
            AttackDelivery.Melee, DamageElement.Fire, a.Enemy.Armor + 1, a.Combat.Clock, Cause: cause, ClassGenerated: generated));
        Assert.True(a.Enemy.Tactics!.Enemy!.PhaseTwo); Assert.Equal(speed + 25, a.Enemy.Speed);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "ElitePhaseChanged" && e.Position == a.Enemy.Position);
        Assert.Empty(a.Combat.VisibleTimeline());
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("above")]
    [InlineData("blocked")]
    [InlineData("lethal")]
    [InlineData("ordinary")]
    public void EnemyPhaseDoesNotTriggerAboveThresholdOnBlockedDamageDeathOrOrdinaryEnemies(string scenario)
    {
        using var a = new Arena(ClassKind.Adventurer); PreparePhaseBoss(a, scenario == "ordinary" ? "mossbound_guard" : "hollow_regent");
        if (scenario == "above") a.Enemy.Health++;
        if (scenario == "blocked") a.Enemy.Health--;
        int damage = scenario switch { "blocked" => 0, "lethal" => 1000, _ => a.Enemy.Armor + 1 };
        a.Hit(a.Enemy, damage, source: a.Hero);
        Assert.False(a.Enemy.Tactics!.Enemy!.PhaseTwo);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind is "ElitePhaseChanged" or "RegentPhaseChanged");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void EnemyPhaseRemainsAfterHealingAndCannotGrantSpeedAgain()
    {
        using var a = new Arena(ClassKind.Adventurer); PreparePhaseBoss(a, "thorn_warden");
        int speed = a.Enemy.Speed; a.Hit(a.Enemy, a.Enemy.Armor + 1, source: a.Hero);
        a.Enemy.Health = a.Enemy.MaxHealth;
        a.Hit(a.Enemy, a.Enemy.Armor + a.Enemy.MaxHealth / 2, source: a.Hero);
        Assert.True(a.Enemy.Tactics!.Enemy!.PhaseTwo); Assert.Equal(speed + 25, a.Enemy.Speed);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "ElitePhaseChanged");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void EnemyPhaseDoesNotBringAnAlreadyCommittedReadyTurnForward()
    {
        using var a = new Arena(ClassKind.Adventurer); PreparePhaseBoss(a, "thorn_warden");
        string before = JsonSerializer.Serialize(a.Combat.State.Queue.Snapshot());
        a.Hit(a.Enemy, a.Enemy.Armor + 1, source: a.Hero);
        Assert.True(a.Enemy.Tactics!.Enemy!.PhaseTwo);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State.Queue.Snapshot()));
    }

    [Fact]
    public void EnemyPhaseUsesTheCompletedDamageBatchAndKeepsMultipleBossesIndependent()
    {
        using var a = new Arena(ClassKind.Adventurer); PreparePhaseBoss(a, "thorn_warden");
        var killed = RevivalTestEnemy(a, "thorn_warden", new(101, 101)); killed.Health = 1;
        var healthy = RevivalTestEnemy(a, "thorn_warden", new(101, 99));
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Hero.Id, a.Hero.Position,
            [a.Enemy.Position, killed.Position, healthy.Position], "test:phase-batch", AttackDelivery.Area,
            DamageElement.Physical, a.Enemy.Armor + 1, a.Combat.Clock));
        Assert.True(a.Enemy.Tactics!.Enemy!.PhaseTwo); Assert.Equal(LifeState.Dead, killed.Life);
        Assert.False(killed.Tactics!.Enemy!.PhaseTwo); Assert.False(healthy.Tactics!.Enemy!.PhaseTwo);
        Assert.Equal(a.Enemy.Id, Assert.Single(a.Combat.State.Events, e => e.Kind == "ElitePhaseChanged").Target);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void EnemyPhaseRunsWhenAnActualScheduledSentryShotCrossesTheThreshold()
    {
        using var a = new Arena(ClassKind.Tinkerer); EnemyProfile(a, "thorn_warden");
        a.Enemy.Health = a.Enemy.MaxHealth / 2 + 1; int speed = a.Enemy.Speed;
        a.Act(TacticalAction.Special); a.Next(); a.Act(TacticalAction.Wait); a.Next();
        Assert.True(a.Enemy.Tactics!.Enemy!.PhaseTwo); Assert.Equal(speed + 25, a.Enemy.Speed);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "AttackPerformed" && e.Ability == "core:ability/tinkerer_sentry");
        Assert.Single(a.Combat.State.Events, e => e.Kind == "ElitePhaseChanged");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("role")]
    [InlineData("base_speed")]
    [InlineData("overflow")]
    public void EnemyPhaseSavesRejectUnrelatedRolesAndInvalidBaseSpeeds(string invalid)
    {
        using var a = new Arena(ClassKind.Adventurer); EnemyProfile(a, invalid == "role" ? "mossbound_guard" : "thorn_warden");
        a.Enemy.Tactics!.Enemy!.PhaseTwo = true;
        if (invalid != "role") a.Enemy.Tactics.InnateStats = a.Enemy.Tactics.InnateStats! with { Speed = invalid == "overflow" ? int.MaxValue : 0 };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }
}
