using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeWildlifeNeedsTests
{
    private static (WorldSession World, Actor Animal) Prepare(long tick = 0)
    {
        var world = NativeSocietyTests.AtTick(tick); var hero = world.Party.Single();
        var habitat = WildlifePreview.Find(world, "character.rabbit");
        hero.Position = world.State.Position = habitat.Home; world.Reveal();
        var animal = world.State.Actors.Single(a => a.Habitat == habitat.Home);
        hero.Position = world.State.Position = world.Plan.Towns[0].Center with { Floor = 1 };
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        return (world, animal);
    }

    private static void ReadyAnimal(WorldSession world, Actor animal)
    {
        var combat = world.DungeonCombat;
        Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
        for (int i = 0; i < 5000; i++)
        {
            var actor = combat.AdvanceToDecision(); Assert.NotNull(actor);
            if (actor.Id == animal.Id) return;
            Assert.DoesNotContain(actor.Id, world.State.Party);
            combat.CommitWorldEnemyTurn();
        }
        Assert.Fail("The existing animal never reached its own readiness.");
    }

    private static void GiveFood(WorldSession world, Actor animal)
    {
        world.State.Towns[animal.HomeTown].Stocks["food"]--;
        animal.Cargo["food"] = 1; animal.Body.Hunger = 40;
    }

    private static void Wait(WorldSession world, int turns)
    {
        for (int i = 0; i < turns; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RetainedRaidersAndGuardiansKeepTheirSourceNeedsWithoutSpendingCastAttempts(bool boss)
    {
        using var world = NativeSocietyTests.AtTick(0);
        var creature = world.State.Actors.First(a => a.Tactics?.Enemy?.Retained != null
            && world.Ecology(a) == null && (world.Content.Entries[a.Archetype] is BossDefinition) == boss);
        var hero = world.Party.Single(); hero.Position = world.State.Position = hero.Position with { Floor = 1 };
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        GiveFood(world, creature);
        world.State.Tactics!.Queue.CancelActor(creature.Id, ScheduledKind.ActorReady);
        world.State.Tactics.Queue.Schedule(ScheduledKind.ActorReady, world.DungeonCombat.Clock, creature.Id);
        ReadyAnimal(world, creature);
        string memory = JsonSerializer.Serialize(creature.Tactics!.Enemy!.Retained);
        Assert.Equal(EnemyIntentKind.Eat, world.DungeonCombat.CommitWorldEnemyTurn().Kind);
        Assert.Equal(0, creature.Cargo["food"]); Assert.Equal(5, creature.Body.Hunger);
        Assert.Equal(memory, JsonSerializer.Serialize(creature.Tactics.Enemy.Retained));
        Assert.Null(creature.Tactics.Build);
    }

    [Fact]
    public void AnimalMealsUseOwnFiniteCargoAndPaidRecoveryWithoutHealingOrBorrowingThePartyPack()
    {
        var (world, animal) = Prepare(); using var dispose = world;
        GiveFood(world, animal); animal.Health--;
        ReadyAnimal(world, animal);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Equal(EnemyIntentKind.Eat, world.DungeonCombat.PreviewWorldEnemyTurn().Kind);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        int health = animal.Health, food = world.Count("food"); long consumed = world.State.ResourcesConsumed;
        long clock = world.DungeonCombat.Clock;
        Assert.Equal(EnemyIntentKind.Eat, world.DungeonCombat.CommitWorldEnemyTurn().Kind);
        Assert.Equal(5, animal.Body.Hunger); Assert.Equal(0, animal.Cargo["food"]);
        Assert.Equal(health, animal.Health); Assert.Equal(food, world.Count("food"));
        Assert.Equal(consumed + 1, world.State.ResourcesConsumed);
        var ready = Assert.Single(world.State.Tactics!.Timeline.Pending, e => e.ActorId == animal.Id && e.Kind == ScheduledKind.ActorReady);
        Assert.Equal(clock + ActionTiming.Recovery(world.State.Tactics.Tuning.EatCost, animal.Speed), ready.Due);
        Assert.Single(world.State.Tactics.Events, e => e.Kind == "FoodEaten" && e.Source == animal.Id && e.Ability == "creature-meal");
    }

    [Fact]
    public void NativeFiniteForagingProducesRealCargoWhichIsThenEatenOnTheAnimalsOwnTurn()
    {
        var (world, animal) = Prepare(); using var dispose = world;
        animal.Body.Hunger = 40; animal.Body.ForageProgress = 29.9;
        int habitatFood = world.HabitatAt(animal.Position).Food, food = world.Count("food");
        long produced = world.State.ResourcesProduced, consumed = world.State.ResourcesConsumed;
        Wait(world, 13);
        Assert.Equal(habitatFood - 2, world.HabitatAt(animal.Position).Food);
        Assert.Equal(produced + 2, world.State.ResourcesProduced); Assert.Equal(consumed + 1, world.State.ResourcesConsumed);
        Assert.Equal(1, animal.Cargo["food"]); Assert.InRange(animal.Body.Hunger, 5, 6);
        Assert.Equal(food, world.Count("food")); Assert.True(animal.Body.ForageAfter > world.State.Tick);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void APrivatelySeenTravellerTakesPriorityOverEatingWhileAWallDoesNotInventDanger(bool visible)
    {
        var (world, animal) = Prepare(); using var dispose = world;
        GiveFood(world, animal);
        var hero = world.Party.Single();
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 3; x++) world.Chunks.Edit(animal.Position.Offset(x, y), Material.Floor);
        hero.Position = world.State.Position = animal.Position.Offset(2, 0);
        if (!visible) world.Chunks.Edit(animal.Position.Offset(1, 0), Material.Wall);
        world.Reveal(); ReadyAnimal(world, animal);
        Assert.Equal(visible ? EnemyIntentKind.Move : EnemyIntentKind.Eat, world.DungeonCombat.PreviewWorldEnemyTurn().Kind);
        Assert.Equal(1, animal.Cargo["food"]);
    }

    [Fact]
    public void AnUndisturbedExhaustedAnimalRestsAndRecoversThroughTheSharedWorldBodyClock()
    {
        var (world, animal) = Prepare(49); using var dispose = world;
        animal.Body.Fatigue = 80; animal.Health = 1;
        Wait(world, 12);
        Assert.True(animal.Body.RestUntil > world.State.Tick); Assert.True(animal.Body.Fatigue < 80);
        Assert.Equal(Math.Min(animal.MaxHealth, 3), animal.Health);
        Assert.Equal(LifeState.Alive, animal.Life);
    }

    [Fact]
    public void ACorneredTimidAnimalDoesNotForageOrSleepWhileItPrivatelySeesDanger()
    {
        var (world, animal) = Prepare(49); using var dispose = world;
        var hero = world.Party.Single(); var blocker = world.ActorById(1000);
        animal.Body.Hunger = 40; animal.Body.Fatigue = 80; animal.Health = 1;
        foreach (var (x, y) in new[] { (0, -1), (0, 1), (-1, 0) }) world.Chunks.Edit(animal.Position.Offset(x, y), Material.Wall);
        blocker.Position = animal.Position.Offset(1, 0); world.Chunks.Edit(blocker.Position, Material.Floor);
        hero.Position = world.State.Position = animal.Position.Offset(2, 0); world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        Assert.Same(hero, world.NativeFleeThreat(animal));
        Wait(world, 12);
        Assert.Equal(1, animal.Health); Assert.Equal(0, animal.Body.RestUntil);
        Assert.True(animal.Body.Fatigue >= 80); Assert.Equal(0, animal.Body.ForageProgress);
    }

    [Fact]
    public void SourceAnimalStarvationKeepsItsOriginalHealthFloorAndMemoryInsteadOfBecomingImmune()
    {
        var (world, animal) = Prepare(49); using var dispose = world;
        animal.Body.Hunger = 90; animal.Health = 2;
        Wait(world, 12);
        Assert.Equal(1, animal.Health); Assert.Equal(LifeState.Alive, animal.Life);
        Assert.Contains(animal.Memories, m => m.Key == "hungry");
    }

    [Fact]
    public void AnAnimalsPendingPaidMealAndNeedsContinueExactlyAcrossAnOrdinarySave()
    {
        var (world, animal) = Prepare(); using var dispose = world;
        GiveFood(world, animal); ReadyAnimal(world, animal);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-animal-needs-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            world.DungeonCombat.CommitWorldEnemyTurn(); loaded.DungeonCombat.CommitWorldEnemyTurn();
            Assert.NotNull(world.DungeonCombat.AdvanceToPartyDecision()); Assert.NotNull(loaded.DungeonCombat.AdvanceToPartyDecision());
            Wait(world, 12); Wait(loaded, 12);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(0, loaded.ActorById(animal.Id).Cargo["food"]);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
