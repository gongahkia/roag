using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class TacticalCombatTests
{
    private sealed class Arena : IDisposable
    {
        public WorldSession World { get; } = WorldSession.Create(42);
        public TacticalCombat Combat { get; }
        public Actor Hero { get; }
        public Actor Friend { get; }
        public Actor Enemy { get; }
        public List<long> Advanced { get; } = [];

        public Arena(ClassKind hero = ClassKind.Adventurer, bool friend = false, int enemyDue = 0, bool driveWorld = false)
        {
            Hero = World.Party.Single();
            Friend = World.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
            Enemy = World.State.Actors.First(a => a.Kind == ActorKind.Raider && a.EncounterLeader == 0);
            Hero.Position = World.State.Position = new(100, 100);
            Friend.Position = new(99, 100); Enemy.Position = new(102, 100);
            for (int y = 95; y <= 105; y++) for (int x = 95; x <= 145; x++) World.Chunks.Edit(new(x, y), Material.Floor);
            if (friend) { Friend.Recruited = true; World.State.Party.Add(Friend.Id); }
            Combat = driveWorld ? World.CreateTacticalSimulation() : new(World, time => Advanced.Add(time));
            Combat.Register(Hero, new() { Build = CharacterBuild.Create(hero, 42) });
            if (friend) Combat.Register(Friend, new() { Build = CharacterBuild.Create(ClassKind.Berserker, 43) });
            Combat.Register(Enemy, new() { InnateStats = new(100, 3, 3, 0, 100, 0, 150) }, enemyDue);
            World.Reveal();
            Assert.Same(Hero, Combat.AdvanceToDecision());
        }

        public void Hit(Actor source, Actor target, int damage, DamageCause cause = DamageCause.DirectAttack)
            => Combat.ResolveAttack(new(Combat.State.NextAction++, source.Id, source.Position, [target.Position],
                "test:attack", AttackDelivery.Melee, DamageElement.Fire, damage, Combat.Clock, cause));
        public void Dispose() => World.Dispose();
    }

    [Fact]
    public void CommandsUsePersistentWorldActorsAndTilesBeyondTheOldBattleWindow()
    {
        using var arena = new Arena(ClassKind.Ranger, enemyDue: 1000);
        // Both actors are beyond the old 65-wide battle window centered at (100,100).
        arena.Hero.Position = new(140, 100); arena.Enemy.Position = new(143, 100);
        var preview = arena.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East));
        Assert.Contains(arena.Enemy.Position, preview.Cells);
        int before = arena.Enemy.Health;
        Assert.True(arena.Combat.Commit(new(TacticalAction.BasicAttack, Facing.East)).Success);
        Assert.Equal(before - arena.Hero.Power, arena.Enemy.Health);
        Assert.Same(arena.Hero, arena.Combat.AdvanceToDecision());
        arena.World.Chunks.Edit(new(141, 100), Material.Wall);
        Assert.Equal(new[] { new WorldPosition(141, 100) }, arena.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Cells);
        before = arena.Enemy.Health;
        Assert.True(arena.Combat.Commit(new(TacticalAction.BasicAttack, Facing.East)).Success);
        Assert.Equal(before, arena.Enemy.Health);
        Assert.Null(arena.World.State.Battle);
        Assert.Equal("", arena.World.State.RoagCombatState);
        Assert.Equal("", arena.Hero.RoagCharacter);
    }

    [Fact]
    public void PreviewInvalidMovementAndInspectionDoNotMutateTheWorldOrClock()
    {
        using var arena = new Arena(friend: true);
        string before = JsonSerializer.Serialize(arena.World.State);
        Assert.False(arena.Combat.Commit(new(TacticalAction.Move, Facing.West)).Success);
        Assert.False(arena.Combat.Commit(new(TacticalAction.Move, Facing.NorthEast)).Success);
        _ = arena.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East));
        _ = arena.Combat.VisibleAttacks(arena.World.Visible);
        Assert.Equal(before, JsonSerializer.Serialize(arena.World.State));
        Assert.Equal(0, arena.Combat.Clock);
    }

    [Fact]
    public void RecoveryOrdersPartyMembersIndividuallyWithoutChangingLeaderOrPositions()
    {
        using var arena = new Arena(ClassKind.Ranger, friend: true, enemyDue: 1000);
        long leader = arena.World.State.Party[0];
        var friendPosition = arena.Friend.Position;
        Assert.True(arena.Combat.Commit(new(TacticalAction.Move, Facing.North)).Success);
        Assert.Equal(new(100, 99), arena.Hero.Position);
        Assert.Same(arena.Friend, arena.Combat.AdvanceToDecision());
        Assert.True(arena.Combat.Commit(new(TacticalAction.Wait)).Success);
        Assert.Same(arena.Hero, arena.Combat.AdvanceToDecision());
        Assert.Equal(87, arena.Combat.Clock);
        Assert.Equal(friendPosition, arena.Friend.Position);
        Assert.Equal(leader, arena.World.State.Party[0]);
        Assert.Contains(87L, arena.Advanced);
    }

    [Fact]
    public void LockedAttackDoesNotTrackMovementAndCannotBeChangedThroughItsReturnedArray()
    {
        using var arena = new Arena(ClassKind.Ranger);
        Assert.True(arena.Combat.Commit(new(TacticalAction.Wait)).Success);
        Assert.Same(arena.Enemy, arena.Combat.AdvanceToDecision());
        var locked = arena.Combat.CommitWindup("test:locked", [arena.Hero.Position], AttackDelivery.Melee,
            DamageElement.Physical, 10, 100, 100);
        locked.Cells[0] = new(100, 99);
        Assert.Equal(new(100, 100), arena.Combat.State.Attacks.Single().Cells.Single());
        Assert.Same(arena.Hero, arena.Combat.AdvanceToDecision());
        Assert.Equal(87, arena.Combat.Clock);
        int before = arena.Hero.Health;
        Assert.True(arena.Combat.Commit(new(TacticalAction.Move, Facing.North)).Success);
        Assert.Same(arena.Hero, arena.Combat.AdvanceToDecision());
        Assert.Equal(174, arena.Combat.Clock);
        Assert.Equal(before, arena.Hero.Health);
        Assert.Empty(arena.Combat.State.Attacks);
    }

    [Theory]
    [InlineData(1, 0, DamageCause.DirectAttack, 5)]
    [InlineData(1, -1, DamageCause.DirectAttack, 5)]
    [InlineData(1, 1, DamageCause.DirectAttack, 5)]
    [InlineData(0, -1, DamageCause.DirectAttack, 10)]
    [InlineData(-1, 0, DamageCause.DirectAttack, 10)]
    [InlineData(1, 0, DamageCause.OngoingEffect, 10)]
    [InlineData(1, 0, DamageCause.GroundHazard, 10)]
    public void GuardProtectsItsFacingAndDiagonalsButNotFlanksOrExistingHazards(int dx, int dy, DamageCause cause, int loss)
    {
        using var arena = new Arena();
        arena.Enemy.Position = arena.Hero.Position.Offset(dx, dy);
        Assert.True(arena.Combat.Commit(new(TacticalAction.Guard, Facing.East)).Success);
        int before = arena.Hero.Health;
        arena.Hit(arena.Enemy, arena.Hero, 10, cause);
        Assert.Equal(before - loss, arena.Hero.Health);
        Assert.True(arena.Hero.Tactics!.Guarding);
    }

    [Fact]
    public void ImpactAtGuardExpiryResolvesBeforeReadinessThenGuardEnds()
    {
        using var arena = new Arena();
        Assert.True(arena.Combat.Commit(new(TacticalAction.Guard, Facing.East)).Success);
        Assert.Same(arena.Enemy, arena.Combat.AdvanceToDecision());
        arena.Combat.CommitWindup("test:guard", [arena.Hero.Position], AttackDelivery.Melee, DamageElement.Physical, 10, 100, 100);
        int before = arena.Hero.Health;
        Assert.Same(arena.Hero, arena.Combat.AdvanceToDecision());
        Assert.Equal(before - 5, arena.Hero.Health);
        Assert.False(arena.Hero.Tactics!.Guarding);
    }

    [Fact]
    public void OrdinaryProjectileHitsTheFirstFriendlyBodyWhilePiercingAttackHitsBoth()
    {
        using var arena = new Arena(ClassKind.Ranger, friend: true, enemyDue: 1000);
        arena.Friend.Position = new(101, 100);
        int enemyHp = arena.Enemy.Health, friendHp = arena.Friend.Health;
        Assert.True(arena.Combat.Commit(new(TacticalAction.BasicAttack, Facing.East)).Success);
        Assert.Equal(enemyHp, arena.Enemy.Health);
        Assert.Equal(friendHp - Math.Max(0, arena.Hero.Power - arena.Friend.Armor), arena.Friend.Health);
        Assert.Same(arena.Friend, arena.Combat.AdvanceToDecision());
        arena.Combat.Commit(new(TacticalAction.Wait));
        Assert.Same(arena.Hero, arena.Combat.AdvanceToDecision());
        Assert.True(arena.Combat.Commit(new(TacticalAction.Special, Facing.East)).Success);
        Assert.True(arena.Enemy.Health < enemyHp);
    }

    [Fact]
    public void BleedOutCountsOnlyTheDownedMembersScheduledTurnsAndDamageShortensIt()
    {
        using var arena = new Arena();
        arena.Combat.Commit(new(TacticalAction.Wait));
        Assert.Same(arena.Enemy, arena.Combat.AdvanceToDecision());
        arena.Hit(arena.Enemy, arena.Hero, 100);
        Assert.Equal(LifeState.Downed, arena.Hero.Life);
        Assert.Equal(5, arena.Hero.Tactics!.BleedTurns);
        arena.Hit(arena.Enemy, arena.Hero, 1);
        Assert.Equal(4, arena.Hero.Tactics.BleedTurns);
        for (int remaining = 3; remaining >= 1; remaining--)
        {
            arena.Combat.Commit(new(TacticalAction.Wait));
            Assert.Same(arena.Enemy, arena.Combat.AdvanceToDecision());
            Assert.Equal(remaining, arena.Hero.Tactics.BleedTurns);
        }
        arena.Combat.Commit(new(TacticalAction.Wait));
        Assert.Null(arena.Combat.AdvanceToDecision());
        Assert.Equal(LifeState.Dead, arena.Hero.Life);
        Assert.Equal(0, arena.Hero.Health);
        Assert.True(arena.World.State.AwaitingContinuation);
        Assert.False(arena.World.Submit(new(WorldCommandKind.Continue)).Success);
    }

    [Fact]
    public void RescueSpendsMedicineAndRecoveryRestoresQuarterHealthAndRedowningResetsFive()
    {
        using var arena = new Arena(friend: true, enemyDue: 1000);
        arena.Combat.Commit(new(TacticalAction.Wait));
        Assert.Same(arena.Friend, arena.Combat.AdvanceToDecision());
        arena.Hit(arena.Enemy, arena.Hero, 100);
        int medicine = arena.World.State.Inventory["medicine"];
        Assert.True(arena.Combat.Commit(new(TacticalAction.Revive, Target: arena.Hero.Id)).Success);
        Assert.Equal(medicine - 1, arena.World.State.Inventory["medicine"]);
        Assert.Equal(arena.Hero.MaxHealth / 4, arena.Hero.Health);
        Assert.Equal(LifeState.Alive, arena.Hero.Life);
        Assert.Null(arena.Combat.Active);
        arena.Hit(arena.Enemy, arena.Hero, 100);
        Assert.Equal(5, arena.Hero.Tactics!.BleedTurns);
        TacticalValidation.Validate(arena.World.Plan, arena.World.State);
    }

    [Fact]
    public void LiveVisionDoesNotExposeOffscreenAttacksOrHiddenCells()
    {
        using var arena = new Arena();
        arena.Combat.Commit(new(TacticalAction.Wait));
        arena.Combat.AdvanceToDecision();
        arena.Combat.CommitWindup("test:vision", [arena.Hero.Position, arena.Hero.Position.Offset(0, 1)],
            AttackDelivery.Area, DamageElement.Fire, 2, 100, 100);
        Assert.Empty(arena.Combat.VisibleAttacks(new HashSet<WorldPosition> { arena.Hero.Position }));
        var visible = arena.Combat.VisibleAttacks(new HashSet<WorldPosition> { arena.Enemy.Position, arena.Hero.Position });
        Assert.Equal(new[] { arena.Hero.Position }, visible.Single().Cells);
    }

    [Fact]
    public void SaveResumesTheSamePendingImpactAndActorDecisionUsingWorldHealth()
    {
        using var arena = new Arena();
        arena.Combat.Commit(new(TacticalAction.Guard, Facing.East));
        arena.Combat.AdvanceToDecision();
        arena.Combat.CommitWindup("test:saved", [arena.Hero.Position], AttackDelivery.Melee, DamageElement.Fire, 10, 100, 100);
        var directory = Path.Combine(Path.GetTempPath(), "roag-tactics-" + Guid.NewGuid().ToString("N"));
        string slot = Path.Combine(directory, "world.json");
        try
        {
            TacticalValidation.Validate(arena.World.Plan, arena.World.State);
            arena.World.Save(slot);
            using var loaded = WorldSession.Load(slot);
            var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(arena.Combat.AdvanceToDecision()!.Id, resumed.AdvanceToDecision()!.Id);
            Assert.Equal(arena.Hero.Health, loaded.State.Actors.Single(a => a.Id == arena.Hero.Id).Health);
            Assert.Equal(JsonSerializer.Serialize(arena.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.True(resumed.Commit(new(TacticalAction.Wait)).Success);
            Assert.True(arena.Combat.Commit(new(TacticalAction.Wait)).Success);
            Assert.Equal(arena.Combat.AdvanceToDecision()!.Id, resumed.AdvanceToDecision()!.Id);
            Assert.Equal(JsonSerializer.Serialize(arena.Combat.State), JsonSerializer.Serialize(resumed.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void SaveValidationRejectsOrphanedImpactsAndDuplicateRecovery()
    {
        using var arena = new Arena();
        TacticalValidation.Validate(arena.World.Plan, arena.World.State);
        var duplicate = arena.Combat.State.Queue.Schedule(ScheduledKind.ActorReady, 1, arena.Hero.Id, true);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(arena.World.Plan, arena.World.State));
        arena.Combat.State.Queue.Cancel(duplicate.Sequence);
        arena.Combat.State.Queue.Schedule(ScheduledKind.Impact, 1, arena.Enemy.Id, referenceId: 99);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(arena.World.Plan, arena.World.State));
    }

    [Fact]
    public void CommittedRecoveryAdvancesTheWorldCalendarWithoutIdleTimeOrFreeNpcMovement()
    {
        using var arena = new Arena(enemyDue: 100_000, driveWorld: true);
        arena.World.Paused = false;
        string before = JsonSerializer.Serialize(arena.World.State);
        for (int i = 0; i < 100; i++) Assert.Equal(0, arena.World.AdvanceFrame(TimeSpan.FromSeconds(1)));
        Assert.Equal(before, JsonSerializer.Serialize(arena.World.State));
        var enemyPosition = arena.Enemy.Position;
        for (int i = 0; i < 60; i++)
        {
            Assert.True(arena.Combat.Commit(new(TacticalAction.Wait)).Success);
            Assert.Same(arena.Hero, arena.Combat.AdvanceToDecision());
        }
        Assert.Equal(6000, arena.Combat.Clock); // sixty seconds
        Assert.Equal(5, arena.World.State.Tick); // five twelve-second world steps
        Assert.Equal(enemyPosition, arena.Enemy.Position);
        TacticalValidation.Validate(arena.World.Plan, arena.World.State);
    }

    [Fact]
    public void SplitFloorsKeepSeparateVisionSharedExplorationAndExactSavedPositions()
    {
        using var arena = new Arena(friend: true, enemyDue: 1000);
        arena.Friend.Position = new(100, 100, 1);
        for (int y = 98; y <= 102; y++) for (int x = 98; x <= 102; x++) arena.World.Chunks.Edit(new(x, y, 1), Material.Floor);
        var heroPosition = arena.Hero.Position;
        Assert.True(arena.World.Chunks.Seen(heroPosition));
        arena.Combat.Commit(new(TacticalAction.Wait));
        Assert.Same(arena.Friend, arena.Combat.AdvanceToDecision());
        Assert.Equal(arena.Friend.Id, arena.Combat.State.ControlledActor);
        Assert.Equal(arena.Friend.Position, arena.World.State.Position);
        Assert.DoesNotContain(heroPosition, arena.World.Visible);
        Assert.True(arena.World.Chunks.Seen(heroPosition));
        Assert.DoesNotContain(arena.Combat.VisibleTimeline(), e => e.ActorId == arena.Enemy.Id);
        Assert.Contains(arena.Combat.VisibleTimeline(), e => e.ActorId == arena.Hero.Id);
        string directory = Path.Combine(Path.GetTempPath(), "roag-split-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json");
            arena.World.Save(slot);
            using var resumed = WorldSession.Load(slot);
            Assert.Equal(arena.World.Party.Select(a => (a.Id, a.Position)), resumed.Party.Select(a => (a.Id, a.Position)));
            Assert.Equal(arena.Friend.Id, resumed.State.Tactics!.ActiveActor);
            Assert.Equal(arena.Hero.Id, resumed.State.Party[0]);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void AllDownedPartyCanSaveAndResumeItsRescueWindow()
    {
        using var arena = new Arena();
        arena.Combat.Commit(new(TacticalAction.Wait));
        arena.Combat.AdvanceToDecision();
        arena.Hit(arena.Enemy, arena.Hero, 100);
        Assert.False(arena.World.State.AwaitingContinuation);
        string directory = Path.Combine(Path.GetTempPath(), "roag-down-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json");
            arena.World.Save(slot);
            using var resumed = WorldSession.Load(slot);
            var hero = resumed.Party.Single();
            Assert.Equal(LifeState.Downed, hero.Life);
            Assert.Equal(5, hero.Tactics!.BleedTurns);
            Assert.False(resumed.State.AwaitingContinuation);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
