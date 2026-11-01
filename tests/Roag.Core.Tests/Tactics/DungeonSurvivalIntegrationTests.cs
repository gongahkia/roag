using System.Text.Json;
using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonSurvivalIntegrationTests
{
    private static void Wait(WorldSession world, int actions)
    {
        for (int i = 0; i < actions; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
    }

    [Fact]
    public void NormalNativeStartupEnablesSurvivalFoodBoonsAndPaidAutomaticMeals()
    {
        var (session, resumed) = WorldStartup.Open(Options.Parse(["--new", "--seed", "37"]));
        using var world = Assert.IsType<WorldSession>(session);
        Assert.False(resumed); Assert.True(world.DungeonCombat.Tuning.EnableSurvival);
        Assert.True(world.Party.Single().Tactics!.AutoEat); Assert.False(world.Party.Single().Tactics!.AutoEatInCombat);
        Assert.Contains("core:boon/road_flask", world.State.Tactics!.Chests.AvailableBoons);
        Assert.Contains("core:boon/ration_reserve", world.State.Tactics.Chests.AvailableBoons);
        Assert.Single(world.State.Tactics.Timeline.Pending, w => w.Kind == ScheduledKind.Survival);
    }

    [Fact]
    public void PaidDungeonTimeAdvancesTheImportedNeedsAndIdleFramesOrInspectionDoNot()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        Assert.Equal(0, hero.Body.Hunger); Assert.Equal(0, hero.Body.Fatigue);
        Wait(world, 12);
        Assert.Equal(1200, world.DungeonCombat.Clock); Assert.Equal(1, world.State.Tick);
        Assert.Equal(.04, hero.Body.Hunger, 6); Assert.True(hero.Body.Fatigue > 0);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Equal(0, world.AdvanceFrame(TimeSpan.FromHours(1))); Assert.Equal(0, world.AdvanceTicks(50));
        _ = world.DungeonCombat.Preview(new(TacticalAction.Eat));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void FoodIsHungerOnlyAndRegenUsesItsOwnCadenceWhileBothOverflowWardsRemainAvailable()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        hero.Tactics!.AutoEat = false; hero.Body.Hunger = 5.25; hero.Health -= 5;
        hero.Tactics.Boons.Inventory = hero.Tactics.Boons.Inventory.Grant("core:boon/overflow_chalice", BoonRarity.Common, ClassKind.Adventurer);
        int food = world.Count("food"), hp = hero.Health;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Eat)).Success);
        Assert.Equal(food - 1, world.Count("food")); Assert.Equal(0, hero.Body.Hunger); Assert.Equal(hp, hero.Health);
        Assert.Equal(2, hero.Tactics.Boons.WardTotal(world.DungeonCombat.Clock));
        Wait(world, 8); Assert.Equal(900, world.DungeonCombat.Clock); Assert.Equal(hp, hero.Health);
        Wait(world, 1); Assert.Equal(1000, world.DungeonCombat.Clock); Assert.Equal(hp + 1, hero.Health);
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseMedicine)).Success);
        Assert.Equal(hero.MaxHealth, hero.Health); Assert.Equal(2, hero.Tactics.Boons.WardTotal(world.DungeonCombat.Clock));
    }

    [Fact]
    public void PoliciesAndFractionalNeedsContinueExactlyAcrossFloorsAndAnActualSave()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        var friend = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        string timeline = JsonSerializer.Serialize(world.State.Tactics!.Timeline);
        Assert.True(world.ConfigureDungeonEating(hero.Id, false, true).Success);
        Assert.True(world.ConfigureDungeonEating(friend.Id, true, false).Success);
        Assert.Equal(timeline, JsonSerializer.Serialize(world.State.Tactics.Timeline));
        while (world.DungeonCombat.Clock < 1300) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.True(hero.Body.Hunger > 0); Assert.True(friend.Body.Hunger > 0);
        string root = Path.Combine(Path.GetTempPath(), "roag-dungeon-survival-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            for (int i = 0; i < 16; i++)
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            }
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void HeavyHitsAndDowningLeaveRealInjuriesWithoutRebasingExistingRecovery()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        var source = world.State.Actors.First(a => a.Tactics?.Enemy != null && a.Position.Floor == 1);
        int power = hero.Power, speed = hero.Speed;
        var combat = world.DungeonCombat;
        void Hit(int damage) => combat.ResolveAttack(new(combat.State.NextAction++, source.Id, source.Position,
            [hero.Position], "test:injury", AttackDelivery.Area, DamageElement.Physical, damage, combat.Clock));
        Hit(9); Hit(9);
        Assert.Equal(2, hero.Body.Wounds); Assert.Equal(power - 1, hero.Power); Assert.Equal(speed - 1, hero.Speed);
        Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
        var ready = combat.State.Timeline.Pending.Single(w => w.ActorId == hero.Id && w.Kind == ScheduledKind.ActorReady);
        Hit(7); Assert.Equal(LifeState.Downed, hero.Life); Assert.Equal(3, hero.Body.Wounds);
        Assert.Equal(5, hero.Tactics!.BleedTurns);
        Assert.Contains(ready, combat.State.Timeline.Pending);
    }

    [Fact]
    public void RestedInjuriesRecoverAtTheImportedHourBoundaryWithoutExtraNativeManaOrHpHealing()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        // Set up one valid pre-hour decision, then cross the boundary through a paid turn.
        const long clock = 359900;
        var snapshot = world.State.Tactics!.Timeline;
        world.State.Tactics.Timeline = snapshot with { Clock = clock, Pending = snapshot.Pending.Select(w => w with { Due = w.Due + clock }).ToArray() };
        world.State.Tick = 299; hero.Body.Wounds = 2; hero.Body.RestUntil = 301;
        hero.Body.Hunger = 40; hero.Health -= 5; int hp = hero.Health, mana = hero.Mana;
        Assert.False(world.IsActorInCombat(hero.Id));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(300, world.State.Tick); Assert.Equal(1, hero.Body.Wounds);
        Assert.Equal(hp, hero.Health); Assert.Equal(mana, hero.Mana);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void RestoredFoodDropsUseThePinnedIndependentRollAndNeverPayTwice()
    {
        using var world = WorldSession.CreateDungeonRun(42, survival: true); var combat = world.DungeonCombat;
        int random = combat.State.RandomState;
        foreach (var enemy in world.State.Actors.Where(a => a.Tactics?.Enemy != null))
        {
            var definition = EnemyCatalog.Default.Get(enemy.Tactics!.DefinitionId);
            int roll = RoagRandom.Derive(world.DungeonSeed, "supply:" + enemy.Id) % 100;
            bool expected = !enemy.Tactics.HealthDrop && definition.Ai is not (EnemyBrainKind.HollowRegent or EnemyBrainKind.Pursuer)
                && roll is >= 5 and < 27;
            Assert.Equal(expected, enemy.Tactics.FoodDrop);
        }
        Assert.Equal(random, combat.State.RandomState);
        var target = world.State.Actors.First(a => a.Tactics?.FoodDrop == true);
        var hit = new LockedAttack(combat.State.NextAction++, world.Party.Single().Id, target.Position, [target.Position],
            "test:food-receipt", AttackDelivery.Area, DamageElement.Physical, 10000, combat.Clock, IgnoresTerrain: true);
        combat.ResolveAttack(hit); combat.ResolveAttack(hit);
        Assert.Equal(4, Assert.Single(combat.State.Loot.Drops, d => d.Kind == GroundLootKind.Food && d.DefeatedActor == target.Id).Amount);
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
