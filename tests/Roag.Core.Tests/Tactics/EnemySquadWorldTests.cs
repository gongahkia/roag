using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void SquadPair(Arena a, EnemySquadKind kind, bool enemyIsLeader = true)
    {
        var roster = EnemySquads.Roster(kind);
        a.Enemy.Tactics = EnemyCatalog.Default.Get(enemyIsLeader ? roster.Leader : roster.Follower).CreateTactics();
        a.Enemy.Health = a.Enemy.MaxHealth;
        a.Combat.RegisterEnemy(a.Friend, enemyIsLeader ? roster.Follower : roster.Leader, 10000);
        var leader = enemyIsLeader ? a.Enemy : a.Friend;
        var group = new EnemySquad("test:squad", kind, leader.Id, leader.Position);
        a.Enemy.Tactics.Enemy!.Squad = a.Friend.Tactics!.Enemy!.Squad = group;
    }

    [Fact]
    public void SquadWardHasCancellableRecoveryAndSurvivesActualWorldSave()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        SquadPair(a, EnemySquadKind.WardedBodyguards);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        var attack = Assert.Single(a.Combat.State.Attacks);
        Assert.Equal(EnemySupportKind.Ward, attack.Support!.Kind); Assert.False(a.Enemy.Tactics!.Enemy!.WardReady);
        string dir = Path.Combine(Path.GetTempPath(), "roag-squad-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(dir, "world.json"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            FinishExpansion(a, attack);
            while (resumed.Clock < a.Combat.Clock)
            {
                Assert.NotNull(resumed.AdvanceToDecision());
                if (resumed.Clock < a.Combat.Clock) Assert.True(resumed.Commit(new(TacticalAction.Wait)).Success);
            }
            Assert.True(a.Enemy.Tactics.Enemy.WardReady);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.True(loaded.State.Actors.Single(x => x.Id == a.Enemy.Id).Tactics!.Enemy!.WardReady);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void KillingAWeaverDuringItsChannelCancelsWardActivation()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        SquadPair(a, EnemySquadKind.WardedBodyguards);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        var attack = Assert.Single(a.Combat.State.Attacks); a.Hit(a.Enemy, 1000, source: a.Hero);
        FinishExpansion(a, attack);
        Assert.False(a.Enemy.Tactics!.Enemy!.WardReady); Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "SquadWardRaised");
    }

    [Fact]
    public void WardUsesCurrentDistanceWallsStunAndLeaderLife()
    {
        using var a = new Arena(ClassKind.Adventurer);
        SquadPair(a, EnemySquadKind.WardedBodyguards, enemyIsLeader: false);
        a.Friend.Position = new(105, 100); a.Friend.Tactics!.Enemy!.WardReady = true;
        int hp = a.Enemy.Health;
        a.Hit(a.Enemy, 1, source: a.Hero); Assert.Equal(hp, a.Enemy.Health);
        Assert.Equal(new EnemyProtection("forcefield", a.Friend.Id), a.Combat.State.Events.Last(e => e.Kind == "DamageBlocked").Protection);
        a.World.Chunks.Edit(new(104, 100), Material.Wall); a.Hit(a.Enemy, 1, source: a.Hero); Assert.Equal(--hp, a.Enemy.Health);
        a.World.Chunks.Edit(new(104, 100), Material.Floor);
        a.Friend.Tactics.StunnedUntil = 100; a.Hit(a.Enemy, 1, source: a.Hero); Assert.Equal(--hp, a.Enemy.Health);
        a.Friend.Tactics.StunnedUntil = 0;
        a.Friend.Position = new(108, 100); a.Hit(a.Enemy, 1, source: a.Hero); Assert.Equal(--hp, a.Enemy.Health);
        a.Friend.Position = new(105, 100); a.Hit(a.Friend, 1000, source: a.Hero);
        a.Hit(a.Enemy, 1, source: a.Hero); Assert.Equal(--hp, a.Enemy.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void WardProtectsTheWholePrimaryBatchEvenWhenThatBatchKillsItsWeaver()
    {
        using var a = new Arena(ClassKind.Adventurer);
        SquadPair(a, EnemySquadKind.WardedBodyguards, enemyIsLeader: false);
        a.Friend.Position = new(103, 101); a.Friend.Tactics!.Enemy!.WardReady = true;
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Hero.Id, a.Hero.Position, [a.Friend.Position, a.Enemy.Position],
            "test:area", AttackDelivery.Area, DamageElement.Physical, 1000, a.Combat.Clock));
        Assert.Equal(LifeState.Dead, a.Friend.Life); Assert.Equal(a.Enemy.MaxHealth, a.Enemy.Health);
        a.Hit(a.Enemy, 1000, source: a.Hero); Assert.Equal(LifeState.Dead, a.Enemy.Life);
    }

    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    [InlineData(0, 1, false)]
    [InlineData(-1, 0, false)]
    public void EnemyDirectionalShieldUsesFullWedgeAndSecondaryDamageBypasses(int dx, int dy, bool blocked)
    {
        using var a = new Arena(ClassKind.Adventurer);
        EnemyProfile(a, "shield_bearer"); a.Enemy.Tactics!.Enemy!.ShieldFacing = Facing.East;
        a.Hero.Position = a.Enemy.Position.Offset(dx, dy); int hp = a.Enemy.Health;
        a.Hit(a.Enemy, 1, source: a.Hero); Assert.Equal(hp - (blocked ? 0 : 1), a.Enemy.Health);
        hp = a.Enemy.Health;
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Hero.Id, a.Hero.Position, [a.Enemy.Position],
            "test:secondary", AttackDelivery.Melee, DamageElement.Fire, 1, a.Combat.Clock, DamageCause.TriggeredEffect));
        Assert.Equal(hp - 1, a.Enemy.Health);
    }

    [Fact]
    public void ShieldLowersDuringAttackAndTurnsOnlyWhenACommandCommits()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "shield_bearer"); a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.Combat.PreviewEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        Assert.Equal(Facing.South, a.Enemy.Tactics!.Enemy!.ShieldFacing);
        a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        Assert.True(a.Enemy.Tactics.Enemy.ShieldLowered); Assert.Equal(Facing.West, a.Enemy.Tactics.Enemy.ShieldFacing);
        int hp = a.Enemy.Health; a.Hit(a.Enemy, 1, source: a.Hero); Assert.Equal(hp - 1, a.Enemy.Health);
        var attack = Assert.Single(a.Combat.State.Attacks); FinishExpansion(a, attack);
        Assert.True(a.Enemy.Tactics.Enemy.ShieldLowered);
        a.Hero.Position = new(98, 100);
        for (int i = 0; i < 20; i++)
        {
            a.Act(TacticalAction.Wait); var next = a.Combat.AdvanceToDecision(); Assert.NotNull(next);
            if (next.Id == a.Enemy.Id) break;
        }
        Assert.Same(a.Enemy, a.Combat.Active);
        Assert.Equal(EnemyIntentKind.Move, a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals)).Kind);
        Assert.False(a.Enemy.Tactics.Enemy.ShieldLowered);
    }

    [Fact]
    public void CaptainsRallyIsLockedAtCommitAndDoesNotRetroactivelyWeakenOnLeaderDeath()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        SquadPair(a, EnemySquadKind.CaptainAndFollowers, enemyIsLeader: false); a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        var attack = Assert.Single(a.Combat.State.Attacks); Assert.Equal(2, attack.Damage);
        a.Hit(a.Friend, 1000, source: a.Hero); int hp = a.Hero.Health; FinishExpansion(a, attack);
        Assert.Equal(hp - 2, a.Hero.Health);
        for (int i = 0; i < 20; i++)
        {
            a.Act(TacticalAction.Wait); var next = a.Combat.AdvanceToDecision(); Assert.NotNull(next);
            if (next.Id == a.Enemy.Id) break;
        }
        Assert.Same(a.Enemy, a.Combat.Active);
        a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        Assert.Equal(1, Assert.Single(a.Combat.State.Attacks).Damage);
    }

    [Fact]
    public void AcolyteHealingLocksBothTileAndAllyIdentity()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        SquadPair(a, EnemySquadKind.HealerRushwings); a.Friend.Position = new(101, 101); a.Friend.Health = 1;
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        var attack = Assert.Single(a.Combat.State.Attacks); Assert.Equal(a.Friend.Id, attack.Support!.Target);
        var old = a.Friend.Position; a.Friend.Position = new(100, 103);
        var substitute = a.World.State.Actors.First(x => x.Kind == ActorKind.Raider && x.Id != a.Enemy.Id);
        substitute.Position = old; a.Combat.RegisterEnemy(substitute, "core:enemy/rushwing", 10000); substitute.Health = 1;
        FinishExpansion(a, attack);
        Assert.Equal(1, a.Friend.Health); Assert.Equal(1, substitute.Health);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "Healed");
    }

    [Fact]
    public void ScribeTrapDelaysCommittedPartyMovementByFortyAndKeepsSequence()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        var attack = BeginExpansion(a, "snare_scribe"); FinishExpansion(a, attack);
        var trap = Assert.Single(a.Combat.State.EnemySnares);
        a.Hero.Position = trap.Position.Offset(-1, 0); long clock = a.Combat.Clock; long sequence = a.Combat.State.Queue.NextSequence;
        int hp = a.Hero.Health; a.Act(TacticalAction.Move);
        var ready = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Hero.Id);
        Assert.Equal(clock + 140, ready.Due); Assert.Equal(sequence, ready.Sequence);
        Assert.Equal(hp - 1, a.Hero.Health); Assert.Empty(a.Combat.State.EnemySnares);
        Assert.Equal(0, a.Hero.Tactics!.EnemySnareDelay);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ScribeTrapsAffectOtherEnemiesAndDeferTheirNextDecisionWithoutCancellingWindup()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        SquadPair(a, EnemySquadKind.SnaresAndSiege, enemyIsLeader: false);
        a.Combat.State.EnemySnares.Add(new(a.Friend.Id, a.Enemy.Position.Offset(-1, 0)));
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        long clock = a.Combat.Clock; a.Act(TacticalAction.Move, Facing.West);
        Assert.Empty(a.Combat.State.EnemySnares); Assert.Equal(EnemySquads.SnareDelay, a.Enemy.Tactics!.EnemySnareDelay);
        long originalDue = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Enemy.Id).Due;
        for (int i = 0; i < 20; i++)
        {
            var next = a.Combat.AdvanceToDecision(); Assert.NotNull(next);
            if (next.Id == a.Enemy.Id) break;
            a.Act(TacticalAction.Wait);
        }
        Assert.Same(a.Enemy, a.Combat.Active); Assert.Equal(originalDue + EnemySquads.SnareDelay, a.Combat.Clock);
        Assert.Equal(0, a.Enemy.Tactics.EnemySnareDelay); Assert.True(a.Combat.Clock > clock);
    }

    [Fact]
    public void SquadTrapOwnershipAndReferencesPersistAndRejectInvalidSaves()
    {
        using var a = new Arena(ClassKind.Adventurer);
        SquadPair(a, EnemySquadKind.SnaresAndSiege);
        a.Combat.State.EnemySnares.Add(new(a.Enemy.Id, new(101, 100)));
        a.Combat.State.EnemySnares.Add(new(a.Enemy.Id, new(101, 101, 1)));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        var copied = JsonSerializer.Deserialize<TacticalWorldState>(JsonSerializer.Serialize(a.Combat.State))!;
        Assert.Equal(a.Combat.State.EnemySnares, copied.EnemySnares);
        Assert.True(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
        a.Combat.State.EnemySnares.Add(a.Combat.State.EnemySnares[0]);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Combat.State.EnemySnares.RemoveAt(2);
        a.Hit(a.Enemy, 1000, source: a.Hero); Assert.Empty(a.Combat.State.EnemySnares);
        a.Friend.Tactics!.Enemy!.Squad = a.Friend.Tactics.Enemy.Squad! with { Leader = a.Hero.Id };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }
}
