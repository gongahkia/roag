using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeResidentRoutinesTests
{
    private static (WorldSession World, Actor Hero, Actor Resident) RescuedResident()
    {
        var world = WorldSession.CreateNativeCampaign(42);
        var hero = world.Party.Single();
        var resident = world.ActorById(1004);
        foreach (var person in world.State.Actors.Where(a => !a.Recruited && a.Kind == ActorKind.Person)) person.Cargo.Remove("medicine");
        var home = world.Plan.Towns[resident.HomeTown].Center;
        for (int y = -19; y <= -6; y++) for (int x = -2; x <= 18; x++) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        hero.Position = world.State.Position = home.Offset(4, -10);
        resident.Position = hero.Position.Offset(0, 2);
        resident.Cargo["medicine"] = 1;
        world.State.Towns[resident.HomeTown].Stocks["medicine"]--;
        resident.Trust[hero.Id] = 40; resident.TraitId = "trait.generous";
        hero.Tactics!.AutoEat = false;
        world.Reveal();
        DungeonAbandonmentTests.Down(world, hero);
        Assert.NotNull(resident.Tactics?.Resident);
        Assert.Same(hero, world.DungeonCombat.AdvanceToPartyDecision());
        Assert.Equal(LifeState.Alive, hero.Life);
        Assert.Equal(0, resident.Tactics.Resident.Target);
        Assert.Equal(0, resident.Cargo["medicine"]);
        return (world, hero, resident);
    }

    private static void Wait(WorldSession world, int turns)
    {
        for (int i = 0; i < turns; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
    }

    private static void Separate(WorldSession world, Actor hero)
    {
        hero.Position = world.State.Position = hero.Position with { Floor = 1 };
        world.Chunks.Edit(hero.Position, Material.Floor);
        world.Reveal();
    }

    private static WorkOrder AssignConstruction(WorldSession world, Actor resident)
    {
        // Fresh native campaigns now begin in daylight (11-tile private sight).
        // Keep this supplied construction genuinely outside party knowledge.
        var target = resident.Position.Offset(14, 0);
        world.Chunks.Edit(target, Material.Floor);
        var order = new WorkOrder { Id = world.State.NextWorkId++, Kind = WorkKind.Build, Definition = "recipe.wall",
            Target = target, Before = Material.Floor, After = Material.Wall, Required = 15, Progress = 14.9,
            Worker = resident.Id, PinnedWorker = true, Reserved = new() { ["stone"] = 2 }, SupplyVersion = 1,
            Supplies = [new() { Stack = "stone", Count = 2, Phase = SupplyPhase.Delivered }] };
        world.State.Inventory["stone"] -= 2; world.State.Work.Add(order);
        return order;
    }

    [Fact]
    public void RescuerResumesTheirActualDaytimeOccupationOnPaidTurnsWhileThePartyIsOnAnotherFloor()
    {
        var (world, hero, resident) = RescuedResident(); using var dispose = world;
        var oldHome = resident.Tactics!.Resident!.Home;
        var build = resident.Tactics.Build;
        Separate(world, hero);
        Wait(world, 30);
        Assert.Same(build, resident.Tactics.Build);
        Assert.NotEqual(oldHome, resident.Position);
        Assert.Equal(0, resident.Position.Floor);
        Assert.Equal(RoutineKind.Occupation, resident.Routine!.Kind);
        Assert.True(resident.Position.Distance(resident.Routine.Target) <= 1);
        Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "Moved" && e.Source == resident.Id && e.Time > 300);
        var scheduled = Assert.Single(world.State.Tactics.Timeline.Pending, e => e.Kind == ScheduledKind.ActorReady && e.ActorId == resident.Id);
        Assert.True(scheduled.Due > world.DungeonCombat.Clock);
        Assert.False(scheduled.PlayerControlled);
        Assert.Null(resident.Journey);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void RescuerEatsTheirOwnFiniteFoodOnTheirNextPaidTurnWithoutUsingPartySupplies()
    {
        var (world, hero, resident) = RescuedResident(); using var dispose = world;
        resident.Body.Hunger = 40;
        resident.Cargo["food"] = 1; world.State.Towns[resident.HomeTown].Stocks["food"]--;
        int food = world.Count("food"); long consumed = world.State.ResourcesConsumed;
        long before = world.DungeonCombat.Clock;
        Separate(world, hero);
        Assert.Equal(40, resident.Body.Hunger);
        Wait(world, 3);
        Assert.InRange(resident.Body.Hunger, 5, 6);
        Assert.Equal(0, resident.Cargo["food"]);
        Assert.Equal(food, world.Count("food"));
        Assert.Equal(consumed + 1, world.State.ResourcesConsumed);
        var meal = Assert.Single(world.State.Tactics!.Events, e => e.Kind == "FoodEaten" && e.Source == resident.Id);
        Assert.True(meal.Time > before);
        Assert.Equal("resident-meal", meal.Ability);
    }

    [Fact]
    public void RescuerPhysicallyCompletesTheirExistingConstructionWithoutBorrowingThePartysMap()
    {
        var (world, hero, resident) = RescuedResident(); using var dispose = world;
        var order = AssignConstruction(world, resident); var target = order.Target;
        Assert.False(world.Chunks.Seen(target));
        long before = world.DungeonCombat.Clock;
        Separate(world, hero);
        int stone = world.Count("stone");
        Assert.Equal(14.9, order.Progress);
        Wait(world, 20);
        Assert.DoesNotContain(order, world.State.Work);
        Assert.Equal(Material.Wall, world.Chunks.At(target));
        Assert.Contains(world.State.Structures, s => s.Position == target && s.Cost.GetValueOrDefault("stone") == 2);
        Assert.Equal(stone, world.Count("stone"));
        Assert.True(resident.Body.WorkDone > 0);
        Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "Moved" && e.Source == resident.Id && e.Time > before);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PaidResidentRoutineFoodAndWorkContinueExactlyAcrossAnOrdinarySave(bool work)
    {
        var (world, hero, resident) = RescuedResident(); using var dispose = world;
        Separate(world, hero);
        resident.Body.Hunger = 40;
        resident.Cargo["food"] = 1; world.State.Towns[resident.HomeTown].Stocks["food"]--;
        var order = work ? AssignConstruction(world, resident) : null;
        string root = Path.Combine(Path.GetTempPath(), "roag-resident-routine-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Wait(world, 30); Wait(loaded, 30);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(0, loaded.ActorById(resident.Id).Cargo["food"]);
            Assert.Equal(RoutineKind.Occupation, loaded.ActorById(resident.Id).Routine!.Kind);
            if (order != null) Assert.Equal(Material.Wall, loaded.Chunks.At(order.Target));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
