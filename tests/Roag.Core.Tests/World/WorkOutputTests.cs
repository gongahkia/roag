using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WorkOutputTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-work-output-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private static WorldSession Orchard(int seed, out WorldPosition target, out WorldPosition storage)
    {
        var world = WorldSession.Create(seed); (target, storage) = WorkOutputPreview.Prepare(world);
        world.Paused = false; return world;
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void RegisteredPreviewExercisesRealExtractionCargoAndDelivery(int seed)
    { Assert.True(JsonSerializer.SerializeToNode(WorkOutputPreview.Run(ContentCatalog.Default, seed))!["passed"]!.GetValue<bool>()); }
    private static WorkOrder Harvest(WorldSession world, WorldPosition target, WorldPosition storage)
    {
        Assert.Equal(1, world.DesignateWork(WorldCommandKind.Harvest, [target]).Queued); var work = Assert.Single(world.State.Work);
        Assert.True(world.Submit(new(WorldCommandKind.ConfigureWork, storage, WorkId: work.Id, Item: "deliver-storage")).Success); return work;
    }
    private static void Extract(WorldSession world, WorkOrder work)
    {
        for (int i = 0; i < 150 && work.Output == null; i++) Assert.Equal(1, world.AdvanceTicks(1));
        Assert.NotNull(work.Output); Assert.Equal(work.Required, work.Progress);
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void NaturalHarvestBecomesCargoAndReachesStorageExactlyOnce(int seed)
    {
        using var world = Orchard(seed, out var target, out var storage); int available = world.ResourceAt(target)!.Available;
        long apples = world.AccountedMaterial("apple"), regional = world.AccountedResources(); int pack = world.Count("apple");
        var work = Harvest(world, target, storage); Extract(world, work); var output = work.Output!; var carrier = world.ActorById(output.Carrier);
        Assert.Equal(available - 1, world.ResourceAt(target)!.Available); Assert.Equal(2, carrier.Cargo["apple"]); Assert.Equal(pack, world.Count("apple"));
        Assert.Equal(apples + 2, world.AccountedMaterial("apple")); Assert.Equal(regional, world.AccountedResources());
        for (int i = 0; i < 100 && world.State.Work.Count > 0; i++)
        { var position = carrier.Position; world.AdvanceTicks(1); Assert.InRange(position.Distance(carrier.Position), 0, 1); Assert.Equal(apples + 2, world.AccountedMaterial("apple")); }
        Assert.Empty(world.State.Work); Assert.Equal(2, world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex]["apple"]);
        Assert.Equal(pack, world.Count("apple")); Assert.Equal(0, carrier.Cargo["apple"]); Assert.Equal(available - 1, world.ResourceAt(target)!.Available);
        Assert.Equal(regional, world.AccountedResources());
    }
    [Fact]
    public void ACompanionMustBringDugMaterialBackToTheActualPack()
    {
        using var world = PartyWorkTests.Crew(); var target = world.State.Position.Offset(8, 0); world.Chunks.Edit(target, Material.Rock);
        int stone = world.Count("stone"); Assert.Equal(1, world.DesignateWork(WorldCommandKind.Dig, [target]).Queued); var work = Assert.Single(world.State.Work);
        var worker = world.Party.Skip(1).First(); Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: worker.Id, WorkId: work.Id)).Success);
        Extract(world, work); int amount = work.Yield["stone"]; Assert.Equal(stone, world.Count("stone")); Assert.Equal(amount, worker.Cargo["stone"]);
        Assert.True(worker.Position.Distance(world.State.Position) > 1); Assert.Equal(Material.Earth, world.Chunks.At(target));
        world.AdvanceTicks(80); Assert.Empty(world.State.Work); Assert.Equal(stone + amount, world.Count("stone")); Assert.Equal(0, worker.Cargo["stone"]);
    }
    [Fact]
    public void ExhaustedHarvestAndPendingDeliveryContinueExactlyAcrossSave()
    {
        using var world = Orchard(42, out var target, out var storage); var node = world.ResourceAt(target)!;
        world.Chunks.Delta(target.Chunk).Resources[target.LocalIndex] = new(node.Definition.Id, 1, 0);
        var work = Harvest(world, target, storage); Extract(world, work); Assert.Equal(0, world.ResourceAt(target)!.Available);
        long recovery = world.Chunks.ResourceStockAt(target)!.RecoverAt; string path = Path.Combine(_root, "exhausted.json"); world.Save(path);
        using var loaded = WorldSession.Load(path); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        var origin = world.State.WorkRally!.Value; loaded.RevealSurface(origin.X - 12, origin.Y - 12, origin.X + 13, origin.Y + 13);
        for (int i = 0; i < 80; i++)
        { world.AdvanceTicks(1); loaded.AdvanceTicks(1); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State)); }
        Assert.Equal(recovery, world.Chunks.ResourceStockAt(target)!.RecoverAt); Assert.Equal(2, world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex]["apple"]);
    }
    [Fact]
    public void FullStorageRetainsRemainderAcrossSaveAndRerouting()
    {
        using var world = Orchard(42, out var target, out var storage); var contents = world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex];
        contents["apple"] = 9998; contents["food"] = 9999; var work = Harvest(world, target, storage); Extract(world, work);
        world.AdvanceTicks(30); Assert.Contains("full", work.Status); Assert.Equal(1, work.Output!.Delivered["apple"]);
        Assert.Equal(1, work.Output.Remaining["apple"]); Assert.Equal(1, work.Output.Remaining["food"]);
        long balance = world.AccountedMaterial("apple"); string path = Path.Combine(_root, "partial.json"); world.Save(path);
        using var loaded = WorldSession.Load(path); loaded.Paused = false; Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        var origin = world.State.WorkRally!.Value; loaded.RevealSurface(origin.X - 12, origin.Y - 12, origin.X + 13, origin.Y + 13);
        foreach (var session in new[] { world, loaded }) Assert.True(session.Submit(new(WorldCommandKind.ConfigureWork, WorkId: work.Id, Item: "deliver-pack")).Success);
        world.AdvanceTicks(1); loaded.AdvanceTicks(1); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        Assert.Empty(world.State.Work); Assert.Equal(balance, world.AccountedMaterial("apple")); Assert.Equal(1, world.Count("apple")); Assert.Equal(9999, contents["apple"]);
    }
    [Fact]
    public void CancellingProducedCraftOutputDoesNotRefundConsumedIngredients()
    {
        using var world = WorldCollisionTests.Arena(); var p = world.State.Position; var camp = p.Offset(1, 0); var storage = p.Offset(-7, 0);
        world.Chunks.Edit(camp, Material.Camp); world.Chunks.Edit(storage, Material.Storage); world.Reveal(); world.Paused = false;
        int wood = world.Count("wood"), food = world.Count("food"); long medicine = world.AccountedMaterial("medicine");
        Assert.True(world.Submit(new(WorldCommandKind.Craft, Item: "recipe.field-medicine")).Success); var work = Assert.Single(world.State.Work);
        Assert.True(world.Submit(new(WorldCommandKind.ConfigureWork, storage, WorkId: work.Id, Item: "deliver-storage")).Success);
        Extract(world, work); Assert.Equal(wood - 1, world.Count("wood")); Assert.Equal(food - 2, world.Count("food"));
        Assert.True(world.Submit(new(WorldCommandKind.CancelWork, WorkId: work.Id)).Success);
        Assert.Equal(wood - 1, world.Count("wood")); Assert.Equal(food - 2, world.Count("food")); Assert.Equal(medicine + 1, world.AccountedMaterial("medicine"));
        Assert.Single(world.State.Loot, c => c.Id.StartsWith("work-return-") && c.Contents.GetValueOrDefault("medicine") == 1);
    }
    [Fact]
    public void ChangedSourceAndBlockedDestinationCannotRepeatExtractionOrLoseCargo()
    {
        using var world = Orchard(42, out var target, out var storage); int stock = world.ResourceAt(target)!.Available;
        var work = Harvest(world, target, storage); Extract(world, work); long balance = world.AccountedMaterial("apple");
        world.Chunks.Edit(target, Material.Earth); world.Chunks.Edit(storage, Material.Floor); world.AdvanceTicks(10);
        Assert.Single(world.State.Work); Assert.Contains("unavailable", work.Status); Assert.Equal(balance, world.AccountedMaterial("apple"));
        Assert.Equal(stock - 1, world.Chunks.ResourceStockAt(target)!.Remaining);
        string path = Path.Combine(_root, "changed.json"); world.Save(path); using var loaded = WorldSession.Load(path);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        Assert.True(world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: work.Id, Item: "deliver-pack")).Success); world.AdvanceTicks(1);
        Assert.Empty(world.State.Work); Assert.Equal(balance, world.AccountedMaterial("apple"));
    }
    [Fact]
    public void ReplacementApproachesDownedOutputCarrierAndContinuesExactlyAfterLoad()
    {
        using var world = PartyWorkTests.Crew(); var target = world.State.Position.Offset(8, 0); world.Chunks.Edit(target, Material.Rock);
        world.DesignateWork(WorldCommandKind.Dig, [target]); var work = Assert.Single(world.State.Work); var first = world.Party.Last();
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: first.Id, WorkId: work.Id)).Success); Extract(world, work);
        long balance = world.AccountedMaterial("stone"); first.Health = 0; first.Life = LifeState.Downed;
        var replacement = world.Party.First(); Assert.True(replacement.Position.Distance(first.Position) > 1);
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: replacement.Id, WorkId: work.Id)).Success);
        world.AdvanceTicks(1); Assert.Equal(first.Id, work.Output!.Carrier); Assert.Equal(work.Yield["stone"], first.Cargo["stone"]);
        string path = Path.Combine(_root, "handoff.json"); world.Save(path); using var loaded = WorldSession.Load(path); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State)); bool transferred = false;
        for (int i = 0; i < 100; i++)
        {
            var previous = replacement.Position; world.AdvanceTicks(1); loaded.AdvanceTicks(1);
            if (!transferred && work.Output.Carrier == replacement.Id)
            { transferred = true; Assert.InRange(previous.Distance(first.Position), 0, 1); }
            Assert.Equal(balance, world.AccountedMaterial("stone")); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        Assert.True(transferred); Assert.Empty(world.State.Work); Assert.Equal(0, first.Cargo["stone"]);
    }
    [Fact]
    public void BlockedOutputWaitsThenResumesWithoutReextracting()
    {
        using var world = Orchard(42, out var target, out var storage); var work = Harvest(world, target, storage); Extract(world, work);
        long balance = world.AccountedMaterial("apple"); int available = world.ResourceAt(target)!.Available;
        foreach (var (x, y) in WorldSession.Directions) world.Chunks.Edit(storage.Offset(x, y), Material.Wall);
        world.AdvanceTicks(25); Assert.Contains("approach", work.Status); Assert.Single(world.State.Work); Assert.Equal(balance, world.AccountedMaterial("apple"));
        world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: work.Id, Item: "suspend")); world.AdvanceTicks(5);
        Assert.Contains("Suspended", work.Status); Assert.NotNull(work.Output);
        world.Chunks.Edit(storage.Offset(-1, 0), Material.Floor); world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: work.Id, Item: "resume"));
        world.AdvanceTicks(60); Assert.Empty(world.State.Work); Assert.Equal(balance, world.AccountedMaterial("apple"));
        Assert.Equal(available, world.ResourceAt(target)!.Available); Assert.Equal(2, world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex]["apple"]);
    }
    [Fact]
    public void CancellingPartialDeliveryLeavesOnlyTheUndeliveredMaterial()
    {
        using var world = Orchard(42, out var target, out var storage); var contents = world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex];
        contents["apple"] = 9998; contents["food"] = 9999; var work = Harvest(world, target, storage); Extract(world, work); world.AdvanceTicks(25);
        long balance = world.AccountedMaterial("apple"); Assert.Equal(1, work.Output!.Delivered["apple"]);
        Assert.True(world.Submit(new(WorldCommandKind.CancelWork, WorkId: work.Id)).Success); Assert.Equal(balance, world.AccountedMaterial("apple"));
        var bundle = Assert.Single(world.State.Loot, c => WorldSession.IsWorkSupplyCache(c) && !c.Taken);
        Assert.Equal(1, bundle.Contents["apple"]); Assert.Equal(1, bundle.Contents["food"]); Assert.Equal(9999, contents["apple"]);
        Assert.False(world.Submit(new(WorldCommandKind.CancelWork, WorkId: work.Id)).Success); Assert.Equal(balance, world.AccountedMaterial("apple"));
    }
    [Fact]
    public void HungryResidentCannotEatOutputPromisedToDelivery()
    {
        using var world = Orchard(42, out var target, out var storage); var work = Harvest(world, target, storage);
        var actor = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        actor.Position = target.Offset(-1, 0); actor.Cargo.Clear(); actor.Body.Hunger = 0; actor.Body.Fatigue = 0;
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: actor.Id, WorkId: work.Id)).Success); Extract(world, work);
        Assert.Equal(actor.Id, work.Output!.Carrier); Assert.Equal(1, actor.Cargo["food"]);
        world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: work.Id, Item: "suspend"));
        actor.Body.Hunger = world.Simulation.MealAt; actor.Body.ForageAfter = world.State.Tick + 100; world.State.Towns[actor.HomeTown].Stocks["food"] = 0;
        world.AdvanceTicks(1); Assert.Equal(1, actor.Cargo["food"]); Assert.Equal(1, work.Output.Remaining["food"]); Assert.True(actor.Body.Hunger >= world.Simulation.MealAt);
    }
    [Fact]
    public void InvalidDeliveryChangesArePureAndEmptyStorageIsDiscoverable()
    {
        using var world = Orchard(42, out var target, out var storage); var work = Harvest(world, target, storage);
        Assert.Contains(storage, world.WorkDeliveryStorage()); string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.Submit(new(WorldCommandKind.ConfigureWork, target, WorkId: work.Id, Item: "deliver-storage")).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State)); world.Chunks.Delta(storage.Chunk).Discovered.Remove(storage.LocalIndex);
        before = JsonSerializer.Serialize(world.State); Assert.DoesNotContain(storage, world.WorkDeliveryStorage()); Assert.NotEqual("", world.WorkDeliveryReason(work, storage));
        Assert.False(world.Submit(new(WorldCommandKind.ConfigureWork, storage, WorkId: work.Id, Item: "deliver-storage")).Success); Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
    [Fact]
    public void OlderPendingWorkWithoutOutputMetadataKeepsItsOriginalContinuation()
    {
        using var world = Orchard(42, out var target, out _); Assert.Equal(1, world.DesignateWork(WorldCommandKind.Harvest, [target]).Queued);
        string path = Path.Combine(_root, "older.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var order = json["state"]!["work"]![0]!.AsObject(); order.Remove("outputVersion"); order.Remove("output"); order.Remove("deliveryStorage"); File.WriteAllText(path, json.ToJsonString());
        using var loaded = WorldSession.Load(path); loaded.Paused = false; loaded.AdvanceTicks(100);
        Assert.Empty(loaded.State.Work); Assert.Equal(2, loaded.Count("apple")); Assert.Equal(0, loaded.Party.First().Cargo.GetValueOrDefault("apple"));
    }
    [Theory]
    [InlineData("version")] [InlineData("carrier")] [InlineData("produced")] [InlineData("remaining")] [InlineData("delivered")] [InlineData("progress")] [InlineData("cargo")]
    public void ForgedOutputLedgersAreRejectedWithoutChangingTheSave(string field)
    {
        using var world = Orchard(42, out var target, out var storage); var work = Harvest(world, target, storage); Extract(world, work);
        string path = Path.Combine(_root, "invalid.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!; var order = json["state"]!["work"]![0]!; var output = order["output"]!;
        switch (field)
        {
            case "version": order["outputVersion"] = 999; break;
            case "carrier": output["carrier"] = long.MaxValue; break;
            case "produced": output["producedTick"] = world.State.Tick + 1; break;
            case "remaining": output["remaining"]!["apple"] = 999; break;
            case "delivered": output["delivered"]!["apple"] = 2; break;
            case "progress": order["progress"] = 0; break;
            case "cargo": json["state"]!["actors"]!.AsArray().Single(a => a!["id"]!.GetValue<long>() == work.Output!.Carrier)!["cargo"]!["apple"] = 0; break;
        }
        File.WriteAllText(path, json.ToJsonString()); string before = File.ReadAllText(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path)); Assert.Equal(before, File.ReadAllText(path));
    }
}
