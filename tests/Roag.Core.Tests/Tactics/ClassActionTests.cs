using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private sealed class Arena : IDisposable
    {
        public WorldSession World { get; } = WorldSession.Create(42);
        public TacticalCombat Combat { get; }
        public Actor Hero { get; }
        public Actor Friend { get; }
        public Actor Enemy { get; }
        public Arena(ClassKind kind, ClassKind? second = null, int enemyDue = 10_000)
        {
            Hero = World.Party.Single();
            Friend = World.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
            Enemy = World.State.Actors.First(a => a.Kind == ActorKind.Raider && a.EncounterLeader == 0);
            Hero.Position = World.State.Position = new(100, 100); Friend.Position = new(100, 102); Enemy.Position = new(103, 100);
            for (int floor = 0; floor <= 1; floor++) for (int y = 95; y <= 110; y++) for (int x = 95; x <= 110; x++)
                World.Chunks.Edit(new(x, y, floor), Material.Floor);
            if (second != null) { Friend.Recruited = true; World.State.Party.Add(Friend.Id); }
            Combat = new(World, _ => { });
            Combat.Register(Hero, new() { Build = CharacterBuild.Create(kind, 42) });
            if (second is { } other) Combat.Register(Friend, new() { Build = CharacterBuild.Create(other, 43) });
            Combat.Register(Enemy, new() { InnateStats = new(200, 6, 6, 0, 100, 0, 150) }, enemyDue);
            Assert.Same(Hero, Combat.AdvanceToDecision());
        }
        public void Act(TacticalAction action, Facing facing = Facing.East)
        { var result = Combat.Commit(new(action, facing)); Assert.True(result.Success, result.Message); }
        public void Next(long id = 0)
        {
            id = id == 0 ? Hero.Id : id;
            for (int i = 0; i < 100; i++)
            {
                var actor = Combat.AdvanceToDecision();
                Assert.NotNull(actor);
                if (actor.Id == id) return;
                Act(TacticalAction.Wait);
            }
            Assert.Fail("Actor never became ready.");
        }
        public void Hit(Actor target, int damage = 5, AttackDelivery delivery = AttackDelivery.Melee, Actor? source = null)
        {
            source ??= Enemy;
            Combat.ResolveAttack(new(Combat.State.NextAction++, source.Id, source.Position, [target.Position], "test:attack",
                delivery, DamageElement.Physical, damage, Combat.Clock));
        }
        public void Dispose() => World.Dispose();
    }

    [Fact]
    public void DuelistParryConsumesOneHostileMeleeHitAndGrantsOnePaidBonusThrust()
    {
        using var a = new Arena(ClassKind.Duelist, enemyDue: 0);
        a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Special);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitWindup("test:melee", [a.Hero.Position], AttackDelivery.Melee, DamageElement.Physical, 5, 50, 100);
        a.Next();
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.True(a.Hero.Tactics!.Resources.Riposte);
        int enemyHealth = a.Enemy.Health;
        a.Act(TacticalAction.BasicAttack);
        Assert.Equal(enemyHealth - a.Hero.Power - 2, a.Enemy.Health);
        Assert.False(a.Hero.Tactics.Resources.Riposte);
        a.Next(); enemyHealth = a.Enemy.Health;
        a.Act(TacticalAction.BasicAttack);
        Assert.Equal(enemyHealth - a.Hero.Power, a.Enemy.Health);
    }

    [Theory]
    [InlineData(AttackDelivery.Projectile, 101, 100)]
    [InlineData(AttackDelivery.Melee, 102, 100)]
    [InlineData(AttackDelivery.Melee, 99, 100)]
    public void DuelistParryDoesNotBecomeGeneralInvulnerability(AttackDelivery delivery, int x, int y)
    {
        using var a = new Arena(ClassKind.Duelist);
        a.Enemy.Position = new(x, y);
        a.Act(TacticalAction.Special);
        a.Hit(a.Hero, delivery: delivery);
        Assert.Equal(a.Hero.MaxHealth - 5, a.Hero.Health);
        Assert.False(a.Hero.Tactics!.Resources.Riposte);
    }

    [Fact]
    public void DuplicateDuelistsOwnTheirParryWindowsAndRipostes()
    {
        using var a = new Arena(ClassKind.Duelist, ClassKind.Duelist);
        a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Special);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Special, Facing.North);
        a.Hit(a.Hero);
        Assert.True(a.Hero.Tactics!.Resources.Riposte);
        Assert.Null(a.Hero.Tactics.Resources.Parry);
        Assert.False(a.Friend.Tactics!.Resources.Riposte);
        Assert.NotNull(a.Friend.Tactics.Resources.Parry);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void DuelistBumpIsOneCellWhileExplicitThrustPiercesTwo()
    {
        using var a = new Arena(ClassKind.Duelist);
        a.Enemy.Position = new(101, 100);
        var bump = a.Combat.Preview(new(TacticalAction.Move, Facing.East));
        Assert.True(bump.Bump);
        Assert.Single(bump.Cells);
        Assert.Equal(2, a.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Cells.Count);
        a.Act(TacticalAction.Move);
        Assert.Equal(new(100, 100), a.Hero.Position);
        Assert.Equal(a.Enemy.MaxHealth - a.Hero.Power, a.Enemy.Health);
    }

    [Fact]
    public void DashStopsBeforeAnOccupiedTileAndPaysItsOriginalRecovery()
    {
        using var a = new Arena(ClassKind.Ranger);
        var preview = a.Combat.Preview(new(TacticalAction.Dash, Facing.East));
        Assert.Equal(new[] { new WorldPosition(101, 100), new WorldPosition(102, 100) }, preview.Cells);
        Assert.Equal(100, preview.Recovery);
        a.Act(TacticalAction.Dash);
        Assert.Equal(new(102, 100), a.Hero.Position);
        a.Next();
        Assert.Equal(100, a.Combat.Clock);
    }

    [Fact]
    public void ShadeSwapConsumesOnlyItsOwnDecoyAndEnemyCommitConsumesTheLure()
    {
        using var a = new Arena(ClassKind.Shade, ClassKind.Shade, enemyDue: 0);
        a.Act(TacticalAction.Special);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Special);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        var friendDecoy = a.Friend.Tactics!.Resources.Decoy!.Value;
        a.Combat.CommitWindup("test:lure", [friendDecoy], AttackDelivery.Melee, DamageElement.Physical, 1, 100, 100);
        Assert.Null(a.Friend.Tactics.Resources.Decoy);
        Assert.NotNull(a.Hero.Tactics!.Resources.Decoy);
        a.Next();
        a.Act(TacticalAction.Dash, Facing.West);
        Assert.Equal(new(101, 100), a.Hero.Position);
        Assert.Null(a.Hero.Tactics.Resources.Decoy);
        a.Next();
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(new(TacticalAction.Dash, Facing.East)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void StormChargeCapsAtThreeAndLockedDischargeUsesTheSavedCross()
    {
        using var a = new Arena(ClassKind.Stormcaller);
        for (int i = 0; i < 4; i++) { a.Act(TacticalAction.BasicAttack); a.Next(); }
        Assert.Equal(3, a.Hero.Tactics!.Resources.StormCharge);
        int before = a.Enemy.Health;
        a.Act(TacticalAction.Special);
        var pending = a.Combat.State.Attacks.Single();
        Assert.Equal(8, pending.Damage);
        Assert.Equal(500, pending.Due);
        Assert.Equal(0, a.Hero.Tactics.Resources.StormCharge);
        a.Enemy.Position = new(104, 100);
        a.Next();
        Assert.Equal(before, a.Enemy.Health);
        Assert.Empty(a.Combat.State.Attacks);
        Assert.False(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
    }

    [Fact]
    public void StormChargesOncePerActionAndGeneratedDamageCannotRechargeIt()
    {
        using var a = new Arena(ClassKind.Stormcaller);
        long action = a.Combat.State.NextAction++;
        var attack = new LockedAttack(action, a.Hero.Id, a.Hero.Position, [a.Enemy.Position],
            ClassCatalog.Get(ClassKind.Stormcaller).BasicAbilityId, AttackDelivery.Area, DamageElement.Lightning, 2, a.Combat.Clock);
        a.Combat.ResolveAttack(attack); a.Combat.ResolveAttack(attack);
        Assert.Equal(1, a.Hero.Tactics!.Resources.StormCharge);
        a.Combat.ResolveAttack(attack with { Id = a.Combat.State.NextAction++, ClassGenerated = true });
        Assert.Equal(1, a.Hero.Tactics.Resources.StormCharge);
    }

    [Fact]
    public void StormDashTradesChargeForOneWardWithoutAFreeAttack()
    {
        using var a = new Arena(ClassKind.Stormcaller);
        a.Act(TacticalAction.BasicAttack); a.Next();
        int enemyHealth = a.Enemy.Health;
        a.Act(TacticalAction.Dash, Facing.North);
        Assert.Equal(0, a.Hero.Tactics!.Resources.StormCharge);
        Assert.True(a.Hero.Tactics.Resources.StormWard);
        a.Hit(a.Hero);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.False(a.Hero.Tactics.Resources.StormWard);
        a.Hit(a.Hero);
        Assert.Equal(a.Hero.MaxHealth - 5, a.Hero.Health);
        Assert.Equal(enemyHealth, a.Enemy.Health);
    }

    [Fact]
    public void SpiritMustBePlacedBesideItsTargetAndOnlyAttacksOnPaidCommand()
    {
        using var a = new Arena(ClassKind.Spiritbinder);
        a.Act(TacticalAction.Special); a.Next();
        Assert.Equal(new WorldPosition(102, 100), a.Hero.Tactics!.Resources.Spirit);
        int before = a.Enemy.Health;
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(before, a.Enemy.Health);
        a.Act(TacticalAction.Special);
        Assert.Equal(before - 3, a.Enemy.Health);
        Assert.Empty(a.Combat.State.Attacks);
    }

    [Fact]
    public void SentrySpendsThreeChargesAndKeepsOperatingOnAnUnobservedFloor()
    {
        using var a = new Arena(ClassKind.Tinkerer);
        a.Enemy.Position = new(104, 100);
        int before = a.Enemy.Health;
        a.Act(TacticalAction.Special);
        a.Hero.Position = new(100, 100, 1);
        a.Next();
        Assert.Equal(1, a.World.State.Position.Floor);
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        while (a.Combat.Clock < 600) { a.Act(TacticalAction.Wait); a.Next(); }
        Assert.Equal(before - 9, a.Enemy.Health);
        Assert.Equal(SentryPhase.Spent, a.Hero.Tactics!.Resources.Sentry!.Phase);
        Assert.Equal(0, a.Hero.Tactics.Resources.Sentry.Charges);
        Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, e => e.Kind == ScheduledKind.Effect);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void SentryLockedProjectileCanHitItsOwnerInsteadOfItsEnemy()
    {
        using var a = new Arena(ClassKind.Tinkerer);
        a.Enemy.Position = new(104, 100);
        a.Act(TacticalAction.Special); a.Next(); // charge committed at 100; impact at 180
        Assert.Equal(168, a.Combat.Clock);
        a.Act(TacticalAction.Dash); a.Next();
        Assert.Equal(a.Hero.MaxHealth - 3, a.Hero.Health);
        Assert.Equal(a.Enemy.MaxHealth, a.Enemy.Health);
    }

    [Fact]
    public void RedeployingOneTinkerersSentryDoesNotCancelAnotherMembersWork()
    {
        using var a = new Arena(ClassKind.Tinkerer, ClassKind.Tinkerer);
        a.Act(TacticalAction.Special);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Special); a.Next();
        long friendWork = a.Friend.Tactics!.Resources.Sentry!.Schedule;
        long oldOwnWork = a.Hero.Tactics!.Resources.Sentry!.Schedule;
        a.Act(TacticalAction.Special, Facing.North);
        var queue = a.Combat.State.Queue.Snapshot().Pending;
        Assert.Contains(queue, e => e.Sequence == friendWork);
        Assert.DoesNotContain(queue, e => e.Sequence == oldOwnWork);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void HookPullIsBlockedByOccupancyAndRespectsExplicitPullImmunity()
    {
        using var a = new Arena(ClassKind.Hookrunner);
        a.Act(TacticalAction.Special);
        Assert.Equal(new(101, 100), a.Enemy.Position); // owner stops the second pull step
        Assert.Equal(a.Enemy.MaxHealth - 3, a.Enemy.Health);
        a.Next();
        a.Enemy.Position = new(103, 100); a.Enemy.Tactics!.ImmuneToPull = true;
        a.Act(TacticalAction.Special);
        Assert.Equal(new(103, 100), a.Enemy.Position);
        Assert.Equal(a.Enemy.MaxHealth - 6, a.Enemy.Health);
    }

    [Fact]
    public void SnareDamagesOnceAndSkipsTheVictimsNextOwnTurn()
    {
        using var a = new Arena(ClassKind.Trapper, enemyDue: 0);
        a.Enemy.Position = new(102, 100);
        a.Act(TacticalAction.Special);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Move, Facing.West);
        Assert.Equal(a.Enemy.MaxHealth - 3, a.Enemy.Health);
        Assert.Null(a.Hero.Tactics!.Resources.Snare);
        Assert.Equal(a.Hero.Id, a.Enemy.Tactics!.SnaredBy);
        Assert.Same(a.Hero, a.Combat.AdvanceToDecision()); // enemy's ready turn at 100 was held
        Assert.Equal(143, a.Combat.Clock);
        Assert.Equal(0, a.Enemy.Tactics.SnaredBy);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "SnareHeld" && e.Target == a.Enemy.Id);
    }

    [Fact]
    public void PendingSentryAndParryResourcesResumeExactlyAndRejectOrphanedWork()
    {
        using var a = new Arena(ClassKind.Tinkerer, ClassKind.Duelist);
        a.Act(TacticalAction.Special);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Special, Facing.North);
        string directory = Path.Combine(Path.GetTempPath(), "roag-class-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json");
            TacticalValidation.Validate(a.World.Plan, a.World.State);
            a.World.Save(slot);
            using var loaded = WorldSession.Load(slot);
            var resumed = new TacticalCombat(loaded, _ => { });
            for (int i = 0; i < 12; i++)
            {
                Assert.Equal(a.Combat.AdvanceToDecision()!.Id, resumed.AdvanceToDecision()!.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(a.World.State.Actors.Where(x => x.Tactics != null).Select(x => JsonSerializer.Serialize(x.Tactics)),
                    loaded.State.Actors.Where(x => x.Tactics != null).Select(x => JsonSerializer.Serialize(x.Tactics)));
                a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            }
            var sentry = loaded.State.Actors.Single(x => x.Id == a.Hero.Id).Tactics!.Resources.Sentry!;
            if (sentry.Schedule != 0) resumed.State.Queue.Cancel(sentry.Schedule);
            else { sentry.Phase = SentryPhase.Waiting; sentry.Charges = 1; sentry.Schedule = 99; sentry.Due = resumed.Clock; }
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(loaded.Plan, loaded.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
