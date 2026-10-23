using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class WorldResourceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-resources-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    [Theory]
    [InlineData("resource.apple-tree")] [InlineData("resource.fishing-bank")] [InlineData("resource.iron-deposit")]
    [InlineData("resource.cave-mushrooms")] [InlineData("resource.deep-iron")]
    public void NaturalNodesHarvestThroughTheNormalWorkDispatcher(string id)
    {
        var s = WorldSession.Create(42); var p = ResourcePreview.Prepare(s, id); var node = s.ResourceAt(p)!;
        string before = JsonSerializer.Serialize(s.State); uint random = s.State.CombatRandom;
        Assert.Equal(node, s.ResourceAt(p)); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        var inventory = new Dictionary<string, int>(s.State.Inventory);
        Assert.Equal(1, s.DesignateWork(WorldCommandKind.Harvest, [p, p]).Queued);
        for (int i = 0; i < 200 && s.State.Work.Count > 0; i++) s.AdvanceTicks(1);
        Assert.Empty(s.State.Work); Assert.Equal(node.Available - 1, s.ResourceAt(p)!.Available);
        Assert.Equal(random, s.State.CombatRandom);
        foreach (var (item, amount) in node.Definition.Yield)
        { var stack = s.Content.Get<ItemDefinition>(item).Stack; Assert.Equal(inventory.GetValueOrDefault(stack) + amount, s.Count(stack)); }
        Assert.True(s.Party.First().Body.Fatigue > 0);
    }
    [Fact]
    public void ExhaustionRecoveryAndPendingWorkSurviveSaveAndCacheEvictionExactly()
    {
        var s = WorldSession.Create(1337); var p = ResourcePreview.Prepare(s, "resource.apple-tree");
        int capacity = s.ResourceAt(p)!.Available;
        for (int n = 0; n < capacity; n++)
        {
            Assert.Equal(1, s.DesignateWork(WorldCommandKind.Harvest, [p]).Queued); s.AdvanceTicks(3);
            Directory.CreateDirectory(_directory); string path = Path.Combine(_directory, "world.json"); s.Save(path);
            var loaded = WorldSession.Load(path); loaded.Paused = false;
            for (int i = 0; i < 60 && s.State.Work.Count > 0; i++) { s.AdvanceTicks(1); loaded.AdvanceTicks(1); }
            Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        }
        Assert.Equal(0, s.ResourceAt(p)!.Available); Assert.Equal(0, s.DesignateWork(WorldCommandKind.Harvest, [p]).Queued);
        var stock = s.Chunks.ResourceStockAt(p)!;
        for (int x = 0; x < 30; x++) s.Chunks.Get(new(x, 40));
        Assert.Equal(0, s.ResourceAt(p)!.Available); Assert.Equal(stock, s.Chunks.ResourceStockAt(p));
        s.State.Tick = stock.RecoverAt - 1; Assert.Equal(0, s.ResourceAt(p)!.Available);
        s.State.Tick++; string before = JsonSerializer.Serialize(s.State);
        Assert.Equal(capacity, s.ResourceAt(p)!.Available); Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
    [Fact]
    public void CancellationAndDestroyedNodesNeverGrantFreeHarvests()
    {
        var s = WorldSession.Create(42); var p = ResourcePreview.Prepare(s, "resource.apple-tree"); int stock = s.ResourceAt(p)!.Available;
        Assert.Equal(1, s.DesignateWork(WorldCommandKind.Harvest, [p]).Queued); s.AdvanceTicks(2);
        Assert.True(s.Submit(new(WorldCommandKind.CancelWork, WorkId: s.State.Work.Single().Id)).Success);
        Assert.Equal(stock, s.ResourceAt(p)!.Available); Assert.Equal(0, s.Count("apple"));
        Assert.Equal(1, s.DesignateWork(WorldCommandKind.Harvest, [p]).Queued); s.Chunks.Edit(p, Material.Earth); s.AdvanceTicks(1);
        Assert.Empty(s.State.Work); Assert.Null(s.ResourceAt(p)); Assert.Equal(0, s.Count("apple"));
    }
    [Fact]
    public void FiniteOreDoesNotRegrowAndToolsImproveOnlyTheirActivities()
    {
        var s = WorldSession.Create(42); var p = ResourcePreview.Prepare(s, "resource.iron-deposit"); var actor = s.Party.First();
        Assert.True(s.Submit(new(WorldCommandKind.Equip, ActorId: actor.Id, Item: "item.pickaxe")).Success);
        var harvest = new WorkOrder { Kind = WorkKind.Harvest, Definition = "resource.iron-deposit" };
        Assert.Equal(1.6, s.WorkMultiplier(actor, harvest)); Assert.Equal(1, s.WorkMultiplier(actor, new() { Kind = WorkKind.Build }));
        int count = s.ResourceAt(p)!.Available;
        for (int n = 0; n < count; n++)
        { Assert.Equal(1, s.DesignateWork(WorldCommandKind.Harvest, [p]).Queued); for (int t = 0; t < 100 && s.State.Work.Count > 0; t++) s.AdvanceTicks(1); }
        Assert.Equal(0, s.ResourceAt(p)!.Available); Assert.Equal(0, s.Chunks.ResourceStockAt(p)!.RecoverAt);
        s.State.Tick += 100000; Assert.Equal(0, s.ResourceAt(p)!.Available);
    }
    [Fact]
    public void DragSizedResourceBatchDistributesToDistinctCompanions()
    {
        var s = WorldSession.Create(42); var first = ResourcePreview.Prepare(s, "resource.apple-tree"); var origin = s.State.Position;
        var nodes = (from y in Enumerable.Range(-7, 15) from x in Enumerable.Range(-7, 15)
            let p = origin.Offset(x, y) where s.ResourceAt(p) is { Available: > 0 } select p).Take(3).ToArray();
        Assert.Equal(3, nodes.Length);
        for (int y = -9; y <= 9; y++) for (int x = -9; x <= 9; x++)
        { var p = origin.Offset(x, y); if (!nodes.Contains(p)) s.Chunks.Edit(p, Material.Floor); }
        foreach (var person in s.State.Actors.Where(a => !a.Recruited && a.Kind == ActorKind.Person).Take(2).ToArray())
        { person.Position = origin.Offset(1, 0); Assert.True(s.Submit(new(WorldCommandKind.Recruit, ActorId: person.Id)).Success); }
        s.Reveal(); var stocks = nodes.Select(p => s.ResourceAt(p)!.Available).ToArray();
        Assert.Equal(3, s.DesignateWork(WorldCommandKind.Harvest, nodes).Queued); int mostWorkers = 0;
        for (int t = 0; t < 150 && s.State.Work.Count > 0; t++)
        {
            s.AdvanceTicks(1); mostWorkers = Math.Max(mostWorkers, s.State.Work.Count(w => w.Worker != 0));
            Assert.Equal(3, s.Party.Select(a => a.Position).Distinct().Count());
        }
        Assert.Equal(3, mostWorkers); Assert.Empty(s.State.Work);
        for (int i = 0; i < nodes.Length; i++) Assert.Equal(stocks[i] - 1, s.ResourceAt(nodes[i])!.Available);
    }
    [Fact]
    public void UnknownResourceTargetsAndInvalidContentAreRejectedWithoutMutations()
    {
        var s = WorldSession.Create(42); var p = ResourcePreview.Find(s, "resource.deep-iron"); string before = JsonSerializer.Serialize(s.State);
        Assert.Equal(0, s.DesignateWork(WorldCommandKind.Harvest, [p]).Queued); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        var docs = s.Content.Entries.Values.Select(d => (File: d.Id, Text: JsonSerializer.Serialize<ContentDefinition>(d, ContentCatalog.Json))).ToArray();
        var invalid = ContentCatalog.Load(docs.Select(d =>
        {
            if (d.File != "resource.apple-tree") return d;
            var node = JsonNode.Parse(d.Text)!; node["capacity"] = 0; node["depletedSprite"] = "unbound";
            node["yield"] = new JsonObject { ["item.missing"] = 2 }; return (d.File, node.ToJsonString());
        }));
        Assert.Contains(invalid.Issues, e => e.Field == "capacity"); Assert.Contains(invalid.Issues, e => e.Field == "depletedSprite");
        Assert.Contains(invalid.Issues, e => e.Field == "yield");
        Assert.Contains(s.Content.References, r => r.Source == "game.vagabonds" && r.Target == "resource.apple-tree");
    }
}
