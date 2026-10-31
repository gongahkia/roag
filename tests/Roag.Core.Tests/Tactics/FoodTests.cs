using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void EatingPaysRecoveryUsesSharedFoodAndRestoresOnlyTheActingMembersHunger()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Ranger);
        a.Hero.Body.Hunger = 40; a.Friend.Body.Hunger = 60; a.Hero.Health -= 5;
        a.Friend.Position = new(100, 100, 1); a.World.State.Inventory["food"] = 2;
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.True(a.Combat.Preview(new(TacticalAction.Eat)).Valid);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Act(TacticalAction.Eat);
        Assert.Equal(1, a.World.Count("food")); Assert.Equal(5, a.Hero.Body.Hunger); Assert.Equal(60, a.Friend.Body.Hunger);
        Assert.Equal(a.Hero.MaxHealth - 5, a.Hero.Health);
        Assert.Equal(100, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id).Due);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision()); a.Act(TacticalAction.Eat);
        Assert.Equal(0, a.World.Count("food")); Assert.Equal(25, a.Friend.Body.Hunger);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 0)]
    public void EatingWithoutHungerOrFoodRejectsFreely(int hunger, int food)
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Hero.Body.Hunger = hunger; a.World.State.Inventory["food"] = food;
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(new(TacticalAction.Eat)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void EatingDrivesOriginalFoodBoonsWithFractionalHungerOverflowAndCommittedRecovery()
    {
        using var a = new Arena(ClassKind.Adventurer);
        foreach (var id in new[] { "overflow_chalice", "road_flask", "ration_reserve" })
            a.Hero.Tactics!.Boons.Inventory = a.Hero.Tactics.Boons.Inventory.Grant("core:boon/" + id, BoonRarity.Common, ClassKind.Adventurer);
        a.Hero.Body.Hunger = 33.25; a.Hero.Health -= 5;
        a.Act(TacticalAction.Eat);
        Assert.Equal(0, a.Hero.Body.Hunger); Assert.Equal(a.Hero.MaxHealth - 5, a.Hero.Health);
        Assert.Equal(1, a.Hero.Tactics!.Boons.WardTotal(0)); // floor(35 - 33.25), capped by the original two-point ward.
        Assert.Equal(10, a.Hero.Tactics.Boons.SpeedBonus(0));
        Assert.Equal(100, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id).Due);
        a.Next(); a.Act(TacticalAction.Wait);
        Assert.Equal(a.Hero.MaxHealth - 4, a.Hero.Health);
        Assert.Equal(0, a.Hero.Tactics.Boons.Memory["core:boon/ration_reserve"].Charges);
    }

    [Fact]
    public void FoodPickupStocksProvisionsAtFullHealthWithoutEatingOrTriggeringFoodBoons()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.World.State.Inventory["food"] = 0; a.Hero.Body.Hunger = 40;
        a.Hero.Tactics!.Boons.Inventory = a.Hero.Tactics.Boons.Inventory.Grant("core:boon/road_flask", BoonRarity.Common, ClassKind.Adventurer);
        a.Combat.SpawnLoot(GroundLootKind.Food, 4, a.Hero.Position.Offset(1, 0));
        a.Act(TacticalAction.Move);
        Assert.Empty(a.Combat.State.Loot.Drops); Assert.Equal(4, a.World.Count("food"));
        Assert.Equal(40, a.Hero.Body.Hunger); Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.Equal(0, a.Hero.Tactics.Boons.SpeedBonus(0));
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "ProvisionsCollected");
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void AutomaticEatingIsConfigurableAndPaysTimeInsideCombat(bool automatic, bool inCombat, bool eats)
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Hero.Body.Hunger = 40; a.Hero.Tactics!.AutoEat = automatic; a.Hero.Tactics.AutoEatInCombat = inCombat;
        int food = a.World.Count("food");
        Assert.Same(a.Hero, a.Combat.AdvanceToPartyDecision());
        Assert.Equal(eats ? 100 : 0, a.Combat.Clock); Assert.Equal(food - (eats ? 1 : 0), a.World.Count("food"));
        Assert.Equal(eats ? 5 : 40, a.Hero.Body.Hunger);
    }

    [Fact]
    public void AutomaticEatingOutsideCombatDoesNotDependOnDistantEnemyTurnMembership()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Position = new(100, 100, 1); a.Hero.Body.Hunger = 40;
        Assert.Same(a.Hero, a.Combat.AdvanceToPartyDecision());
        Assert.Equal(100, a.Combat.Clock); Assert.Equal(5, a.Hero.Body.Hunger);
    }
}

public sealed class NativeSurvivalTests
{
    private static TacticalCombat Prepare(WorldSession world)
    {
        var hero = world.Party.Single(); hero.Position = world.State.Position = new(100, 100);
        world.Chunks.Edit(hero.Position, Material.Floor);
        var combat = world.CreateTacticalSimulation();
        combat.Register(hero, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42), AutoEat = false });
        Assert.Same(hero, combat.AdvanceToDecision());
        return combat;
    }

    private static void WaitUntil(TacticalCombat combat, long clock)
    {
        while (combat.Clock < clock)
        {
            Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
            if (combat.AdvanceToDecision() == null) break;
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(40, 0)]
    public void HealthRegeneratesGraduallyOnlyWhileWellFed(double hunger, int healed)
    {
        using var world = WorldSession.Create(42); var combat = Prepare(world); var hero = world.Party.Single();
        hero.Body.Hunger = hunger; hero.Health -= 5;
        WaitUntil(combat, 900); Assert.Equal(hero.MaxHealth - 5, hero.Health);
        WaitUntil(combat, 1000); Assert.Equal(hero.MaxHealth - 5 + healed, hero.Health);
        Assert.DoesNotContain(combat.VisibleTimeline(), w => w.Kind == ScheduledKind.Survival);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void StarvationCanDownAndKillTheLastMemberOnTheirOwnBleedOutTurns()
    {
        using var world = WorldSession.Create(42); var combat = Prepare(world); var hero = world.Party.Single();
        hero.Body.Hunger = 100; hero.Health = 1;
        WaitUntil(combat, 1000);
        Assert.Equal(LifeState.Dead, hero.Life); Assert.True(world.State.AwaitingContinuation);
        Assert.Contains(combat.State.Events, e => e.Kind == "Downed" && e.Source == 0);
        Assert.Equal(5, combat.State.Events.Count(e => e.Kind == "BleedOut"));
        Assert.DoesNotContain(combat.State.Queue.Snapshot().Pending, w => w.Kind == ScheduledKind.Survival);
        Assert.Equal(1400, combat.Clock);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void SurvivalCadenceAndFoodSettingsContinueExactlyThroughAnActualSave()
    {
        using var world = WorldSession.Create(42); var combat = Prepare(world); var hero = world.Party.Single();
        hero.Body.Hunger = 0; hero.Health -= 4; hero.Tactics!.AutoEatInCombat = true;
        WaitUntil(combat, 900);
        string directory = Path.Combine(Path.GetTempPath(), "roag-food-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(directory);
            using var loaded = WorldSession.Load(directory); var resumed = loaded.CreateTacticalSimulation();
            Assert.False(loaded.Party.Single().Tactics!.AutoEat); Assert.True(loaded.Party.Single().Tactics!.AutoEatInCombat);
            WaitUntil(combat, 1300); WaitUntil(resumed, 1300);
            Assert.Equal(hero.Health, loaded.Party.Single().Health);
            Assert.Equal(hero.Body.Hunger, loaded.Party.Single().Body.Hunger);
            Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(resumed.State));
            var work = Assert.Single(resumed.State.Queue.Snapshot().Pending, w => w.Kind == ScheduledKind.Survival);
            resumed.State.Queue.Cancel(work.Sequence);
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(loaded.Plan, loaded.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
