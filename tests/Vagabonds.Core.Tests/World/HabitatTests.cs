using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class HabitatTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-habitat-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private static WorldSession Prepare()
    { var world = WorldSession.Create(42); HabitatPreview.Prepare(world); world.Paused = false; return world; }
    private static Actor Forager(WorldSession world)
    {
        var actor = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        actor.Position = world.State.Position.Offset(1, 0); actor.Body.RestUntil = 0; actor.Body.Hunger = 40; actor.Body.Fatigue = 0;
        actor.Body.ForageProgress = world.Simulation.ForageWork - .01; world.State.Towns[actor.HomeTown].Stocks["food"] = 0;
        return actor;
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void RegisteredPreviewConsumesAndRecoversRealStock(int seed)
    { Assert.True(JsonSerializer.SerializeToNode(HabitatPreview.Run(ContentCatalog.Default, seed))!["passed"]!.GetValue<bool>()); }
    [Fact]
    public void InspectionNeverCreatesSavedStateTerrainOrActorsEvenAfterLongElapsedTime()
    {
        using var world = Prepare(); world.State.Tick = 1_000_000_000_000;
        string before = JsonSerializer.Serialize(world.State); int chunks = world.Chunks.ResidentCount;
        for (int i = 0; i < 200; i++)
        {
            var p = new WorldPosition(i * 17, i * 13, i % WorldPlan.Floors); var view = world.HabitatAt(p);
            Assert.InRange(view.Food, 0, view.Capacity); Assert.InRange(view.Pressure, 0, 1000);
            _ = world.HabitatDescription(p); _ = world.WildFoodUnavailable(p);
        }
        Assert.Equal(chunks, world.Chunks.ResidentCount); Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
    [Theory]
    [InlineData(0)] [InlineData(199)] [InlineData(200)] [InlineData(219)] [InlineData(220)]
    [InlineData(599)] [InlineData(600)] [InlineData(619)] [InlineData(620)] [InlineData(999)] [InlineData(1000)]
    public void ClosedFormRecoveryMatchesHourlyBandTransitions(int pressure)
    {
        using var world = Prepare(); var p = world.State.Position; var state = world.State.Habitats[WorldSession.HabitatIndex(p.Chunk)];
        state.Food = 0; state.Pressure = pressure; int food = 0, current = pressure;
        int capacity = world.HabitatAt(p).Capacity, rate = Math.Max(1, capacity / 8);
        for (int hour = 0; hour <= 65; hour++)
        {
            world.State.Tick = hour * WorldSession.TicksPerHour; var actual = world.HabitatAt(p);
            Assert.Equal(food, actual.Food); Assert.Equal(current, actual.Pressure);
            current = Math.Max(0, current - 20); food = Math.Min(capacity, food + (current >= 600 ? 0 : current >= 200 ? Math.Max(1, rate / 2) : rate));
        }
    }
    [Fact]
    public void NamedActorsAndPartyForageCompeteForTheSameStockAndWaitingDoesNotCreateOutput()
    {
        using var world = Prepare(); var actor = Forager(world); int pack = world.Count("food"); long balance = world.AccountedResources(), flow = world.RegionalResourceFlow;
        Assert.True(world.Submit(new(WorldCommandKind.Forage)).Success); var order = Assert.Single(world.State.Work);
        world.AdvanceTicks(1); Assert.Equal(0, world.HabitatAt(actor.Position).Food); Assert.Equal(2, actor.Cargo["food"]);
        Assert.Equal(0, order.Progress); Assert.Null(order.Output); Assert.Contains("depleted", order.Status); Assert.Equal(pack, world.Count("food"));
        world.AdvanceTicks(20); Assert.Null(order.Output); Assert.Equal(0, order.Progress); Assert.Equal(1, actor.Cargo["food"]);
        Assert.Equal(balance + world.RegionalResourceFlow - flow, world.AccountedResources());
        for (int i = 0; i < 900 && world.State.Work.Count > 0; i++) world.AdvanceTicks(1);
        Assert.Empty(world.State.Work); Assert.Equal(pack + 2, world.Count("food"));
        Assert.Equal(balance + world.RegionalResourceFlow - flow, world.AccountedResources());
    }
    [Fact]
    public void EmptyWildGroundsRejectWithoutCostAndHungryActorsCannotManufactureFood()
    {
        using var world = Prepare(); var actor = Forager(world); world.State.Habitats[WorldSession.HabitatIndex(actor.Position.Chunk)].Food = 0;
        string before = JsonSerializer.Serialize(world.State); var result = world.Submit(new(WorldCommandKind.Forage));
        Assert.False(result.Success); Assert.Contains("depleted", result.Message); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        double work = actor.Body.WorkDone; long produced = world.State.ResourcesProduced;
        world.AdvanceTicks(20); Assert.Equal(0, actor.Cargo.GetValueOrDefault("food")); Assert.Equal(work, actor.Body.WorkDone);
        Assert.Equal(0, actor.Body.ForageProgress); Assert.Equal(produced, world.State.ResourcesProduced);
    }
    [Fact]
    public void RealConstructionAndDismantlingRetainDistinctLandLossWithoutCreatingFoodOnRestoration()
    {
        using var world = WorldCollisionTests.Arena(); world.Paused = false; var p = world.State.Position.Offset(1, 0);
        world.Chunks.Edit(p, Material.Grass); int capacity = world.HabitatAt(p).Capacity;
        Assert.True(world.Submit(new(WorldCommandKind.Build, p, Material: Material.Floor)).Success); world.AdvanceTicks(60);
        var built = world.HabitatAt(p); Assert.Equal(capacity - 1, built.Capacity); Assert.Equal(1, built.ClearedTiles);
        Assert.True(world.Submit(new(WorldCommandKind.Dig, p)).Success); world.AdvanceTicks(40);
        var restored = world.HabitatAt(p); Assert.Equal(Material.Grass, world.Chunks.At(p)); Assert.Equal(capacity, restored.Capacity);
        Assert.Equal(0, restored.ClearedTiles); Assert.Equal(built.Food, restored.Food);
        Assert.True(world.Submit(new(WorldCommandKind.Build, p, Material: Material.Floor)).Success); world.AdvanceTicks(60);
        Assert.Equal(1, world.HabitatAt(p).ClearedTiles); Assert.Equal(2, world.State.Habitats[WorldSession.HabitatIndex(p.Chunk)].LandLoss[p.LocalIndex]);
    }
    [Fact]
    public void NaturalOccupationFellingPermanentlyReducesCapacity()
    {
        using var world = WorldSession.Create(42); var fixture = OccupationPreview.Prepare(world, "woodcutter"); var actor = world.ActorById(fixture.Actor);
        var before = world.HabitatAt(fixture.Source); world.Paused = false;
        for (int i = 0; i < 100 && actor.Occupation?.Phase != OccupationPhase.Delivering; i++) world.AdvanceTicks(1);
        var after = world.HabitatAt(fixture.Source); Assert.Equal(before.Capacity - 1, after.Capacity); Assert.Equal(before.ClearedTiles + 1, after.ClearedTiles);
        Assert.True(after.Pressure >= before.Pressure + 16); Assert.Equal(Material.Earth, world.Chunks.At(fixture.Source));
    }
    [Fact]
    public void NodeGatheringAndHuntingRaisePressureOnlyOnceAtCompletion()
    {
        using var world = WorldSession.Create(42); var fixture = OccupationPreview.Prepare(world); var actor = world.ActorById(fixture.Actor);
        var before = world.HabitatAt(fixture.Source); world.Paused = false;
        for (int i = 0; i < 100 && actor.Occupation?.Phase != OccupationPhase.Delivering; i++) world.AdvanceTicks(1);
        var after = world.HabitatAt(fixture.Source); Assert.Equal(before.Food, after.Food); Assert.Equal(before.Pressure + 8, after.Pressure);
        world.AdvanceTicks(10); Assert.Equal(after, world.HabitatAt(fixture.Source));
        using var hunt = WorldSession.Create(1337); var animal = WildlifePreview.Prepare(hunt, "character.rabbit"); var hero = hunt.Party.Single();
        hero.Role = 0; hero.BaseSpeed = 30; hero.BasePower = 100; int pressure = hunt.HabitatAt(animal.Habitat!.Value).Pressure;
        hunt.StartBattle(animal.Id); Assert.True(hunt.Act(0, 1).Success); Assert.Equal(LifeState.Dead, animal.Life);
        Assert.Equal(pressure + 120, hunt.HabitatAt(animal.Habitat.Value).Pressure);
        hunt.Reveal(); Assert.Equal(pressure + 120, hunt.HabitatAt(animal.Habitat.Value).Pressure);
    }
    [Fact]
    public void DepletedWaitingAndRecoveryHaveExactSavedContinuation()
    {
        using var world = Prepare(); Forager(world); Assert.True(world.Submit(new(WorldCommandKind.Forage)).Success); world.AdvanceTicks(20);
        string path = Path.Combine(_root, "waiting.json"); world.Save(path); using var loaded = WorldSession.Load(path); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        for (int i = 0; i < 400; i++)
        {
            world.AdvanceTicks(1); loaded.AdvanceTicks(1); Assert.Equal(world.HabitatAt(world.State.Position), loaded.HabitatAt(loaded.State.Position));
            if (i % 31 == 0) Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
    }
    [Theory]
    [InlineData("key")] [InlineData("food")] [InlineData("capacity")] [InlineData("pressure")] [InlineData("epoch")]
    [InlineData("land")] [InlineData("weight")] [InlineData("null")] [InlineData("registry")]
    public void MalformedEcologyStateIsRejectedWithoutRewritingTheSlot(string field)
    {
        using var world = Prepare(); string path = Path.Combine(_root, "bad.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var entries = json["state"]!["habitats"]!.AsObject(); var first = entries.First(); var habitat = first.Value!;
        switch (field)
        {
            case "key": entries["-1"] = habitat.DeepClone(); break;
            case "food": habitat["food"] = -1; break;
            case "capacity": habitat["food"] = 999; break;
            case "pressure": habitat["pressure"] = 1001; break;
            case "epoch": habitat["epoch"] = 1; break;
            case "land": habitat["landLoss"]!["4096"] = 2; break;
            case "weight": habitat["landLoss"]!["0"] = 9; break;
            case "null": entries[first.Key] = null; break;
            case "registry": json["state"]!["habitats"] = null; break;
        }
        File.WriteAllText(path, json.ToJsonString()); string before = File.ReadAllText(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path)); Assert.Equal(before, File.ReadAllText(path));
    }
    [Fact]
    public void OlderCompatibleSavesWithoutHabitatStateLoadWithoutInventingConsumption()
    {
        using var world = WorldSession.Create(42); string path = Path.Combine(_root, "old.json"); world.Save(path);
        var json = JsonNode.Parse(File.ReadAllText(path))!; json["state"]!.AsObject().Remove("habitats"); File.WriteAllText(path, json.ToJsonString());
        using var loaded = WorldSession.Load(path); Assert.Empty(loaded.State.Habitats); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
    }
}
