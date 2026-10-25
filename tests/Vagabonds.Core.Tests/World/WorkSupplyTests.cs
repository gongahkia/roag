using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.World;
using Xunit;
using Vagabonds.Tests.Content;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.Tests.World;

public sealed class WorkSupplyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-work-supplies-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void AuthoringPreviewExecutesAllThreePhysicalSupplyStages(int seed)
    {
        var report = JsonSerializer.SerializeToNode(WorkSupplyPreview.Run(ContentCatalog.Default, seed))!;
        Assert.True(report["passed"]!.GetValue<bool>()); Assert.Equal(3, report["stages"]!.AsArray().Count);
    }
    private static WorldSession Warehouse(out WorldPosition storage, out WorldPosition target)
    {
        var world = WorldCollisionTests.Arena(); var p = world.State.Position;
        storage = p.Offset(-6, 0); target = p.Offset(7, 0);
        world.Chunks.Edit(storage, Material.Storage); world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex] = new() { ["stone"] = 10 };
        world.State.Inventory["stone"] = 0; world.Paused = false; world.RevealSurface(p.X - 12, p.Y - 12, p.X + 12, p.Y + 12); return world;
    }
    private static WorkOrder Queue(WorldSession world, WorldPosition target)
    {
        Assert.Equal("", world.TargetBlockReason(WorldCommandKind.Build, target, Material.Wall, requireAdjacent: false));
        Assert.Equal(1, world.DesignateWork(WorldCommandKind.Build, [target]).Queued); return Assert.Single(world.State.Work);
    }
    [Fact]
    public void StoragePickupCargoDeliveryAndConstructionConserveEveryMaterial()
    {
        using var world = Warehouse(out var storage, out var target); long initial = world.AccountedMaterial("stone"), regional = world.AccountedResources();
        var work = Queue(world, target); Assert.Equal(1, work.SupplyVersion);
        var supply = Assert.Single(work.Supplies!); Assert.Equal(storage, supply.Storage); Assert.Equal(SupplyPhase.Waiting, supply.Phase);
        Assert.Equal(8, world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex]["stone"]);
        bool carrying = false, delivered = false;
        for (int i = 0; i < 80 && world.State.Work.Count > 0; i++)
        {
            var position = world.State.Position; var phase = supply.Phase;
            Assert.Equal(1, world.AdvanceTicks(1)); Assert.InRange(position.Distance(world.State.Position), 0, 1);
            Assert.Equal(initial, world.AccountedMaterial("stone")); Assert.Equal(regional, world.AccountedResources());
            if (supply.Phase == SupplyPhase.Carrying)
            { carrying = true; Assert.Equal(0, work.Progress); Assert.Equal(2, world.ActorById(supply.Carrier).Cargo["stone"]); if (phase == SupplyPhase.Waiting) Assert.InRange(position.Distance(storage), 0, 1); }
            if (supply.Phase == SupplyPhase.Delivered && phase != SupplyPhase.Delivered)
            { delivered = true; Assert.InRange(world.ActorById(work.Worker).Position.Distance(target), 0, 1); Assert.Equal(0, supply.Carrier); }
        }
        Assert.True(carrying && delivered); Assert.Empty(world.State.Work); Assert.Equal(Material.Wall, world.Chunks.At(target));
        Assert.Equal(2, world.State.Structures.Single().Cost["stone"]);
    }
    [Fact]
    public void WaitingCancellationRestoresTheOriginalStorageAndCannotDoubleRefund()
    {
        using var world = Warehouse(out var storage, out var target); long total = world.AccountedMaterial("stone");
        var work = Queue(world, target); Assert.Contains("reserved", world.TargetBlockReason(WorldCommandKind.Dig, storage, requireAdjacent: false));
        Assert.True(world.Submit(new(WorldCommandKind.CancelWork, WorkId: work.Id)).Success);
        Assert.Equal(10, world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex]["stone"]); Assert.Equal(0, world.Count("stone"));
        Assert.Equal(total, world.AccountedMaterial("stone")); string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.Submit(new(WorldCommandKind.CancelWork, WorkId: work.Id)).Success); Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
    [Fact]
    public void BlockedPickupAndDeliveryRetainOwnershipUntilAccessReopens()
    {
        using var world = Warehouse(out var storage, out var target); long total = world.AccountedMaterial("stone");
        foreach (var (dx, dy) in WorldSession.Directions) world.Chunks.Edit(storage.Offset(dx, dy), Material.Wall);
        var work = Queue(world, target); world.AdvanceTicks(10); Assert.Equal(0, work.Progress); Assert.Equal(SupplyPhase.Waiting, work.Supplies![0].Phase);
        world.Chunks.Edit(storage.Offset(1, 0), Material.Floor);
        for (int i = 0; i < 30 && work.Supplies[0].Phase != SupplyPhase.Carrying; i++) world.AdvanceTicks(1);
        Assert.Equal(SupplyPhase.Carrying, work.Supplies[0].Phase);
        foreach (var (dx, dy) in WorldSession.Directions) world.Chunks.Edit(target.Offset(dx, dy), Material.Wall);
        world.AdvanceTicks(10); Assert.Equal(0, work.Progress); Assert.Equal(SupplyPhase.Carrying, work.Supplies[0].Phase);
        Assert.Equal(total, world.AccountedMaterial("stone"));
        world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: work.Id, Item: "suspend")); world.AdvanceTicks(5);
        Assert.Equal(SupplyPhase.Carrying, work.Supplies[0].Phase); Assert.Equal(total, world.AccountedMaterial("stone"));
        world.Chunks.Edit(target.Offset(-1, 0), Material.Floor); world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: work.Id, Item: "resume"));
        world.AdvanceTicks(80); Assert.Empty(world.State.Work); Assert.Equal(total, world.AccountedMaterial("stone"));
    }
    [Fact]
    public void AResidentCannotEatFoodReservedForConstructionWhileCarryingIt()
    {
        var catalog = ContentTests.Edited("recipe.wall", n => n["cost"] = JsonNode.Parse("{\"item.food\":2}"));
        using var world = WorldSession.Create(42, catalog); var p = world.State.Position;
        for (int y = -5; y <= 5; y++) for (int x = -10; x <= 10; x++) world.Chunks.Edit(p.Offset(x, y), Material.Floor);
        world.Reveal(); var target = p.Offset(8, 0); var person = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        person.Position = p.Offset(1, 0); var work = Queue(world, target);
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: work.Id)).Success);
        world.Paused = false; world.AdvanceTicks(1); Assert.Equal(SupplyPhase.Carrying, work.Supplies![0].Phase);
        int food = world.State.Towns[0].Stocks["food"]; person.Body.Hunger = 40; world.AdvanceTicks(4);
        Assert.Equal(2, person.Cargo["food"]); Assert.Equal(food - 1, world.State.Towns[0].Stocks["food"]); Assert.True(person.Body.Hunger < 10);
    }
    [Fact]
    public void AnUncollectedPackReservationWaitsWhenThePartyLeavesAndResumesOnReturn()
    {
        using var world = WorldCollisionTests.Arena(); var p = world.State.Position; var person = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        person.Position = p.Offset(4, 0); var work = Queue(world, p.Offset(8, 0)); work.Required = 1000;
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: work.Id)).Success);
        var position = person.Position; long balance = world.AccountedMaterial("stone");
        world.State.Position = world.Party.Single().Position = world.Plan.Towns[1].Center; world.Reveal(); world.Paused = false;
        world.AdvanceTicks(50); Assert.Equal(position, person.Position); Assert.Equal(0, work.Progress); Assert.Contains("pack is away", work.Status);
        Assert.Equal(SupplyPhase.Waiting, work.Supplies![0].Phase); Assert.Equal(balance, world.AccountedMaterial("stone"));
        world.State.Position = world.Party.Single().Position = p; world.Reveal(); world.AdvanceTicks(80);
        Assert.True(work.Progress > 0); Assert.All(work.Supplies, s => Assert.Equal(SupplyPhase.Delivered, s.Phase)); Assert.Equal(balance, world.AccountedMaterial("stone"));
    }
    [Fact]
    public void OlderOrdersWithoutSupplyMetadataKeepTheirAlreadyReservedOnsiteCost()
    {
        using var world = WorldCollisionTests.Arena(); var target = world.State.Position.Offset(1, 0); Queue(world, target);
        string path = Path.Combine(_root, "older.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var order = json["state"]!["work"]![0]!.AsObject(); order.Remove("supplyVersion"); order.Remove("supplies"); File.WriteAllText(path, json.ToJsonString());
        using var loaded = WorldSession.Load(path); loaded.Paused = false; Assert.Null(loaded.State.Work[0].Supplies);
        loaded.AdvanceTicks(1); Assert.True(loaded.State.Work[0].Progress > 0); Assert.Equal(0, loaded.Party.First().Cargo.GetValueOrDefault("stone"));
    }
    [Fact]
    public void CancelledCargoStaysInARecoverableBundleAtItsActualLocation()
    {
        using var world = Warehouse(out var storage, out var target); long total = world.AccountedMaterial("stone"); var work = Queue(world, target);
        for (int i = 0; i < 30 && work.Supplies![0].Phase != SupplyPhase.Carrying; i++) world.AdvanceTicks(1);
        var supply = work.Supplies![0]; Assert.Equal(SupplyPhase.Carrying, supply.Phase); var carrier = world.ActorById(supply.Carrier); var location = carrier.Position;
        Assert.True(world.Submit(new(WorldCommandKind.CancelWork, WorkId: work.Id)).Success);
        Assert.Equal(0, carrier.Cargo["stone"]); Assert.Equal(0, world.Count("stone")); Assert.Equal(8, world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex]["stone"]);
        var bundle = Assert.Single(world.State.Loot, c => c.Id.StartsWith("work-return-")); Assert.Equal(location, bundle.Position);
        Assert.True(world.GroundOpen(location)); Assert.Equal(total, world.AccountedMaterial("stone"));
        Assert.True(world.Submit(new(WorldCommandKind.Loot, location)).Success); Assert.Equal(2, world.Count("stone")); Assert.True(bundle.Taken);
        Assert.Equal(total, world.AccountedMaterial("stone"));
    }
    [Fact]
    public void SourceQueriesCannotReserveUnseenOrRemoteStorageOrChangeState()
    {
        using var world = Warehouse(out var storage, out var target);
        world.Chunks.Delta(storage.Chunk).Discovered.Remove(storage.LocalIndex);
        string before = JsonSerializer.Serialize(world.State);
        Assert.NotEqual("", world.TargetBlockReason(WorldCommandKind.Build, target, Material.Wall, requireAdjacent: false));
        Assert.Equal(0, world.DesignateWork(WorldCommandKind.Build, [target]).Queued); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        world.Chunks.Delta(storage.Chunk).Discovered.Add(storage.LocalIndex);
        before = JsonSerializer.Serialize(world.State); Assert.Equal("", world.RecipeSuppliesReason(Material.Wall, target)); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        world.State.WorkRally = world.State.Position.Offset(40, 0); before = JsonSerializer.Serialize(world.State);
        Assert.NotEqual("", world.RecipeSuppliesReason(Material.Wall, target)); Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
    [Theory]
    [InlineData(SupplyPhase.Waiting)] [InlineData(SupplyPhase.Carrying)] [InlineData(SupplyPhase.Delivered)]
    public void EverySupplyPhaseContinuesExactlyAcrossSaveAndReload(SupplyPhase phase)
    {
        using var world = Warehouse(out _, out var target); var work = Queue(world, target);
        for (int i = 0; i < 35 && work.Supplies![0].Phase != phase; i++) world.AdvanceTicks(1);
        Assert.Equal(phase, work.Supplies![0].Phase); string path = Path.Combine(_root, phase + ".json"); world.Save(path);
        using var loaded = WorldSession.Load(path); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        // Both runners use the same explicit presentation viewport. The camera
        // rectangle is not simulation/save state.
        var origin = world.State.WorkRally!.Value;
        loaded.RevealSurface(origin.X - 12, origin.Y - 12, origin.X + 12, origin.Y + 12);
        for (int i = 0; i < 80; i++)
        { world.AdvanceTicks(1); loaded.AdvanceTicks(1); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State)); }
    }
    [Fact]
    public void ANewWorkerPhysicallyRecoversCargoFromTheDownedCarrier()
    {
        using var world = PartyWorkTests.Crew(); var p = world.State.Position; var target = p.Offset(8, 0);
        var work = Queue(world, target); var first = world.Party.Skip(1).First(); var replacement = world.Party.Skip(2).First();
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: first.Id, WorkId: work.Id)).Success);
        for (int i = 0; i < 20 && work.Supplies![0].Phase != SupplyPhase.Carrying; i++) world.AdvanceTicks(1);
        var supply = work.Supplies![0]; Assert.Equal(SupplyPhase.Carrying, supply.Phase); long balance = world.AccountedMaterial("stone");
        first.Health = 0; first.Life = LifeState.Downed;
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: replacement.Id, WorkId: work.Id)).Success);
        for (int i = 0; i < 50 && world.State.Work.Count > 0; i++)
        { world.AdvanceTicks(1); Assert.Equal(balance, world.AccountedMaterial("stone")); }
        Assert.Empty(world.State.Work); Assert.Equal(0, first.Cargo["stone"]); Assert.Equal(Material.Wall, world.Chunks.At(target));
    }
    [Fact]
    public void FullPackCancellationAndPartialBundlePickupCannotOverflowSavedStacks()
    {
        using var world = WorldCollisionTests.Arena(); var target = world.State.Position.Offset(1, 0); var work = Queue(world, target);
        world.State.Inventory["stone"] = 9999; long balance = world.AccountedMaterial("stone");
        world.Submit(new(WorldCommandKind.CancelWork, WorkId: work.Id)); Assert.Equal(9999, world.Count("stone")); Assert.Equal(balance, world.AccountedMaterial("stone"));
        var bundle = Assert.Single(world.State.Loot, c => c.Id.StartsWith("work-return-")); Assert.False(world.Submit(new(WorldCommandKind.Loot, bundle.Position)).Success);
        world.State.Inventory["stone"] = 9998; Assert.True(world.Submit(new(WorldCommandKind.Loot, bundle.Position)).Success);
        Assert.Equal(9999, world.Count("stone")); Assert.False(bundle.Taken); Assert.Equal(1, bundle.Contents["stone"]);
        string file = Path.Combine(_root, "overflow.json"); world.Save(file); using var loaded = WorldSession.Load(file);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
    }
    [Theory]
    [InlineData("version")] [InlineData("missing")] [InlineData("count")] [InlineData("phase")] [InlineData("carrier")] [InlineData("cargo")] [InlineData("progress")]
    public void MalformedSupplyOwnershipIsRejectedWithoutOverwritingTheSave(string field)
    {
        using var world = Warehouse(out _, out var target); Queue(world, target); string path = Path.Combine(_root, "invalid.json"); world.Save(path);
        var json = JsonNode.Parse(File.ReadAllText(path))!; var work = json["state"]!["work"]![0]!; var supply = work["supplies"]![0]!;
        switch (field)
        {
            case "version": work["supplyVersion"] = 999; break;
            case "missing": work["supplies"] = null; break;
            case "count": supply["count"] = 999; break;
            case "phase": supply["phase"] = 999; break;
            case "carrier": supply["phase"] = 1; supply["carrier"] = long.MaxValue; break;
            case "cargo": supply["phase"] = 1; supply["carrier"] = world.State.Party[0]; break;
            case "progress": work["progress"] = 1; break;
        }
        File.WriteAllText(path, json.ToJsonString()); string before = File.ReadAllText(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path)); Assert.Equal(before, File.ReadAllText(path));
    }
}
