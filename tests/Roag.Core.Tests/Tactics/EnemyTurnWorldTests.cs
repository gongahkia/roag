using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void EnemyProfile(Arena arena, string name)
    {
        arena.Enemy.Tactics = EnemyCatalog.Default.Get("core:enemy/" + name).CreateTactics();
        arena.Enemy.Health = arena.Enemy.MaxHealth;
    }

    private static Facing? EnemyRoute(Arena a, WorldPosition from, IReadOnlySet<WorldPosition> goals)
        => EnemyPlanningTests.FirstStep(from, goals, p => p.X is >= 95 and <= 110 && p.Y is >= 95 and <= 110 && a.World.CanEnter(p, a.Enemy.Id));

    [Fact]
    public void NativeEnemyTurnsLockTheSelectedMembersTilesAndPayAuthoredTiming()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        string before = JsonSerializer.Serialize(a.Combat.State); string memory = JsonSerializer.Serialize(a.Enemy.Tactics!.Enemy);
        var preview = a.Combat.PreviewEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        Assert.Equal("core:ability/guard_strike", preview.Ability);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State)); Assert.Equal(memory, JsonSerializer.Serialize(a.Enemy.Tactics.Enemy));
        a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        var attack = Assert.Single(a.Combat.State.Attacks);
        Assert.Equal(a.Hero.Id, attack.IntendedTarget); Assert.Equal(new[] { a.Hero.Position }, attack.Cells);
        Assert.Equal(114, attack.Due);
        Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Enemy.Id);
        a.World.Reveal();
        Assert.Contains(a.Combat.VisibleTimeline(), w => w.ActorId == a.Enemy.Id && w.Kind == ScheduledKind.ActorReady && w.Projected && w.Due == 285);
        a.Next(); // hero ready at 100, before the 114 impact
        int health = a.Hero.Health;
        a.Act(TacticalAction.Move, Facing.North); a.Next();
        Assert.Equal(health, a.Hero.Health);
        Assert.Empty(a.Combat.State.Attacks);
        Assert.Equal(285, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Enemy.Id).Due);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void NativeNavigationMovesOnWorldTilesAndBlockedPursuersUseTheirOwnWaitCost()
    {
        using (var a = new Arena(ClassKind.Adventurer, enemyDue: 0))
        {
            EnemyProfile(a, "mossbound_guard");
            a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
            var move = a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
            Assert.Equal(EnemyIntentKind.Move, move.Kind); Assert.Equal(new(102, 100), a.Enemy.Position);
            Assert.Equal(Facing.West, a.Enemy.Tactics!.Enemy!.LastMovement);
            Assert.Equal(143, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Enemy.Id).Due);
            TacticalValidation.Validate(a.World.Plan, a.World.State);
        }
        using (var a = new Arena(ClassKind.Adventurer, enemyDue: 0))
        {
            EnemyProfile(a, "extraction_pursuer");
            a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
            var wait = a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
            Assert.Equal(EnemyIntentKind.Wait, wait.Kind);
            Assert.Equal(37, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Enemy.Id).Due);
        }
    }

    [Fact]
    public void WardenAttackCounterAndIntendedTargetSurvivePendingWindupSave()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "thorn_warden"); a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        var intent = a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal("core:ability/warden_sweep", intent.Ability);
        Assert.Equal(1, a.Enemy.Tactics!.Enemy!.Attacks);
        string directory = Path.Combine(Path.GetTempPath(), "roag-enemy-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(a.Hero.Id, Assert.Single(resumed.State.Attacks).IntendedTarget);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(a.Combat.AdvanceToDecision()!.Id, resumed.AdvanceToDecision()!.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                if (a.Combat.Active!.Id == a.Enemy.Id)
                {
                    var original = a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
                    var restored = resumed.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
                    Assert.Equal(original.Ability, restored.Ability); Assert.Equal(original.Cells.ToArray(), restored.Cells.ToArray());
                }
                else { a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success); }
            }
            Assert.True(a.Enemy.Tactics.Enemy.Attacks >= 2);
            Assert.Equal(a.Enemy.Tactics.Enemy.Attacks, loaded.State.Actors.Single(x => x.Id == a.Enemy.Id).Tactics!.Enemy!.Attacks);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void EnemyUsesTheSelectedTargetsOwnedDecoyAndCanTargetDownedMembers()
    {
        using var a = new Arena(ClassKind.Shade, ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = new(102, 100);
        a.Act(TacticalAction.Special); // decoy at 101,100
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision()); a.Act(TacticalAction.Wait);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        var intent = a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
        Assert.Equal(new[] { new WorldPosition(101, 100) }, intent.Cells);
        Assert.Null(a.Hero.Tactics!.Resources.Decoy);
        a.Hit(a.Hero, 1000);
        Assert.Equal(LifeState.Downed, a.Hero.Life);
        a.Enemy.Position = new(101, 100);
        a.Next(a.Enemy.Id);
        var downed = a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(EnemyIntentKind.Windup, downed.Kind);
        Assert.Equal(new[] { a.Hero.Position }, downed.Cells);
    }

    [Fact]
    public void ActualEnemyRegistrationBindsStatsFlagsAndKillRewardsWithoutManualCopies()
    {
        using var world = WorldSession.Create(42); var hero = world.Party.Single();
        var enemy = world.State.Actors.First(x => x.Kind == ActorKind.Raider && x.EncounterLeader == 0);
        hero.Position = world.State.Position = new(100, 100); enemy.Position = new(101, 100);
        world.Chunks.Edit(hero.Position, Material.Floor); world.Chunks.Edit(enemy.Position, Material.Floor);
        var combat = new TacticalCombat(world, _ => { });
        combat.Register(hero, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42) });
        enemy.Health = enemy.MaxHealth;
        combat.RegisterEnemy(enemy, "core:enemy/thorn_warden", 10000, guaranteedHealthDrop: true);
        Assert.Equal(44, enemy.Health); Assert.Equal(85, enemy.Speed);
        Assert.False(enemy.Tactics!.RegularEnemy); Assert.True(enemy.Tactics.ImmuneToPull);
        Assert.Same(hero, combat.AdvanceToDecision());
        combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [enemy.Position], "test:attack",
            AttackDelivery.Melee, DamageElement.Physical, 1000, combat.Clock));
        Assert.Equal(12, hero.Tactics!.Build!.Experience);
        Assert.Equal(new[] { (GroundLootKind.Currency, 8), (GroundLootKind.Health, 10) }, combat.State.Loot.Drops.Select(d => (d.Kind, d.Amount)));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void UnportedEnemyBehaviorFailsBeforeMutatingTheWorld()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "grave_cantor");
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        string before = JsonSerializer.Serialize(a.Combat.State);
        Assert.Throws<NotSupportedException>(() => a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null));
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
        Assert.Same(a.Enemy, a.Combat.Active);
        Assert.Equal(0, a.Enemy.Tactics!.Enemy!.Attacks);
    }
}
