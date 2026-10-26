using System.Text.Json;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class ResidentForageTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-resident-forage-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static string State(WorldSession world) => JsonSerializer.Serialize(world.State);
    private static HabitatState Stock(WorldSession world, WorldPosition p, bool viable)
    {
        world.State.Habitats.Remove(WorldSession.HabitatIndex(p.Chunk));
        int capacity = world.HabitatAt(p).Capacity;
        var state = new HabitatState { RecoveryVersion = 1, Food = viable ? Math.Min(8, capacity) : 1, Pressure = 0, Epoch = world.State.Tick / WorldSession.TicksPerHour };
        if (!viable) for (int i = 0; i < (capacity - 1) * 4; i++) state.LandLoss[i] = 8;
        world.State.Habitats[WorldSession.HabitatIndex(p.Chunk)] = state; return state;
    }
    private static WorldSession Prepare(int seed, out Actor actor, out WorldPosition food)
    {
        var world = WorldSession.Create(seed); actor = RoutinePreview.Prepare(world);
        var key = world.State.Position.Chunk; var origin = new WorldPosition(key.X * 64 + 56, actor.Position.Y);
        // One narrow controlled crossing, with real regional stocks and identity.
        for (int y = -8; y <= 8; y++) for (int x = -4; x <= 24; x++) world.Chunks.Edit(origin.Offset(x, y), Material.Floor);
        actor.Position = origin; actor.Routine = null; actor.Body.Hunger = 90; actor.Body.Fatigue = actor.Body.ForageProgress = 0; actor.Body.RestUntil = 0;
        world.State.Towns[actor.HomeTown].Stocks["food"] = 0;
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) Stock(world, new((key.X + x) * 64 + 32, (key.Y + y) * 64 + 32), false);
        food = new((key.X + 1) * 64 + 7, origin.Y); Stock(world, food, true);
        world.Paused = false; return world;
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)] [InlineData(-42)]
    public void CapacityOneGroundsCauseAWalkToActualViableFoodBeforeGathering(int seed)
    {
        using var world = Prepare(seed, out var actor, out var food); var start = actor.Position;
        long produced = world.State.NamedResourcesProduced, balance = world.AccountedResources(), flow = world.RegionalResourceFlow;
        Assert.Equal(1, world.HabitatAt(start).Capacity);
        bool arrived = false;
        for (int i = 0; i < 220 && actor.Body.Hunger >= world.Simulation.MealAt; i++)
        {
            var before = actor.Position; Assert.Equal(1, world.AdvanceTicks(1)); Assert.InRange(before.Distance(actor.Position), 0, 1);
            arrived |= actor.Position.Chunk == food.Chunk;
            if (!arrived) { Assert.Equal(produced, world.State.NamedResourcesProduced); Assert.Equal(0, actor.Body.ForageProgress); }
            Assert.Equal(balance + world.RegionalResourceFlow - flow, world.AccountedResources());
        }
        Assert.True(arrived); Assert.NotEqual(start, actor.Position); Assert.True(actor.Body.Hunger < world.Simulation.MealAt);
        Assert.Equal(produced + 2, world.State.NamedResourcesProduced); Assert.Equal(1, world.HabitatAt(start).Food);
        Assert.True(world.HabitatAt(food).Food < 8); Assert.Equal(actor.Id, world.ActorById(actor.Id).Id);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void WalkingAndGatheringReloadsMatchNearFarAndFullCadence(bool anotherFloor)
    {
        using var near = Prepare(42, out var actor, out _); var field = JourneyPreview.PrepareFarField(near);
        if (anotherFloor) { field = field with { Floor = 1 }; near.Chunks.Edit(field, Material.Floor); }
        near.AdvanceTicks(4); Assert.NotNull(actor.Journey); Assert.True(actor.Position.Distance(actor.Routine!.Target) > 1);
        string path = Path.Combine(_directory, "walking.json"); near.Save(path); using var far = WorldSession.Load(path);
        Assert.Equal(State(near), State(far)); far.State.Position = far.Party.Single().Position = field; far.Paused = false; far.DetailedBodiesEverywhere = true;
        bool savedGathering = false;
        for (int i = 0; i < 160; i++)
        {
            Assert.Equal(1, near.AdvanceTicks(1)); Assert.Equal(1, far.AdvanceTicks(1));
            Assert.Equal(JsonSerializer.Serialize(actor), JsonSerializer.Serialize(far.ActorById(actor.Id)));
            if (!savedGathering && actor.Body.ForageProgress > 0)
            {
                far.Save(path); using var loaded = WorldSession.Load(path); Assert.Equal(State(far), State(loaded));
                loaded.Paused = false;
                for (int tick = 0; tick < 30; tick++)
                {
                    Assert.Equal(1, near.AdvanceTicks(1)); Assert.Equal(1, far.AdvanceTicks(1)); Assert.Equal(1, loaded.AdvanceTicks(1));
                    Assert.Equal(State(far), State(loaded)); Assert.Equal(JsonSerializer.Serialize(actor), JsonSerializer.Serialize(far.ActorById(actor.Id)));
                }
                savedGathering = true;
            }
        }
        Assert.True(savedGathering); Assert.True(actor.Body.Hunger < near.Simulation.MealAt);
    }
    [Fact]
    public void AssignedWorkKeepsItsOwnerAndMaterialsThroughTheFoodTrip()
    {
        using var world = Prepare(42, out var actor, out _); var target = world.State.Position.Offset(-1, 0);
        var queued = world.Submit(new(WorldCommandKind.Build, target, Material: Material.Wall));
        Assert.True(queued.Success, queued.Message); var order = Assert.Single(world.State.Work);
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, WorkId: order.Id, ActorId: actor.Id)).Success);
        long stone = world.AccountedMaterial("stone"); bool hungry = true;
        for (int i = 0; i < 280 && world.State.Work.Count > 0; i++)
        {
            Assert.Equal(1, world.AdvanceTicks(1)); Assert.Equal(stone, world.AccountedMaterial("stone"));
            if (hungry && actor.Body.Hunger >= world.Simulation.MealAt) { Assert.Equal(actor.Id, order.Worker); Assert.Equal(0, order.Progress); }
            else hungry = false;
        }
        Assert.False(hungry); Assert.Empty(world.State.Work); Assert.Equal(Material.Wall, world.Chunks.At(target));
    }
    [Fact]
    public void NearbyTownFoodCannotBeEatenFromAnOldForagingDestination()
    {
        using var world = Prepare(42, out var actor, out var food); actor.Position = food; world.AdvanceTicks(1);
        Assert.Equal("Foraging for food", actor.Routine!.Reason); Assert.True(actor.Body.ForageProgress > 0);
        var town = world.State.Towns[actor.HomeTown]; town.Stocks["food"] = 10;
        Assert.Equal(1, world.AdvanceTicks(1)); Assert.Equal(10, town.Stocks["food"]);
        for (int i = 0; i < 150 && actor.Body.Hunger >= world.Simulation.MealAt; i++)
        {
            Assert.Equal(1, world.AdvanceTicks(1));
            if (actor.Position.Distance(world.Plan.Towns[actor.HomeTown].Center) > 1) Assert.Equal(10, town.Stocks["food"]);
        }
        Assert.True(actor.Body.Hunger < world.Simulation.MealAt); Assert.Equal(8, town.Stocks["food"]);
    }
    [Fact]
    public void BlockedSearchesHaveAFairGlobalTerrainBudgetAndRecoverAfterAccessChanges()
    {
        using var world = Prepare(42, out var actor, out var food); var key = actor.Position.Chunk;
        var origin = actor.Position;
        var people = world.State.Actors.Where(a => a.Kind == ActorKind.Person && a.HomeTown == 0 && !a.Recruited).ToArray();
        for (int i = 0; i < people.Length; i++)
        {
            people[i].Position = origin.Offset(i % 4 - 3, i / 4); people[i].Body.Hunger = 90;
            people[i].Body.RestUntil = 0; people[i].Routine = null;
        }
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
        {
            if (x == 0 && y == 0) continue;
            int left = (key.X + x) * 64, top = (key.Y + y) * 64;
            var p = new WorldPosition(Math.Clamp(actor.Position.X, left + 7, left + 56), Math.Clamp(actor.Position.Y, top + 7, top + 56));
            Stock(world, p, true);
            for (int dy = -8; dy <= 8; dy++) for (int dx = -10; dx <= 10; dx++) world.Chunks.Edit(p.Offset(dx, dy), Material.Wall);
        }
        var searched = new HashSet<long>(); world.DiagnosticSink = e => { if (e.Kind == "resident-forage-search") searched.Add(JsonSerializer.SerializeToElement(e.Data).GetProperty("actor").GetInt64()); };
        for (int i = 0; i < 100; i++)
        {
            long before = world.ResidentForaging.TerrainCandidates; Assert.Equal(1, world.AdvanceTicks(1));
            Assert.InRange(world.ResidentForaging.TerrainCandidates - before, 0, WorldSession.ResidentForageSearchers * WorldSession.ResidentForageProbes);
        }
        Assert.Equal(people.Select(a => a.Id).Order(), searched.Order()); Assert.True(world.ResidentForaging.TerrainCandidates > 0);
        Assert.All(people, a => Assert.Equal(0, a.Body.ForageProgress));
        for (int dy = -8; dy <= 8; dy++) for (int dx = -12; dx <= 12; dx++) world.Chunks.Edit(food.Offset(dx, dy), Material.Floor);
        for (int i = 0; i < 300 && world.State.NamedResourcesProduced == 0; i++) Assert.Equal(1, world.AdvanceTicks(1));
        Assert.True(world.State.NamedResourcesProduced > 0);
    }
}
