using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WorldSituationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-situations-" + Guid.NewGuid().ToString("N"));
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

    [Theory]
    [InlineData("trapped-storeroom", "disarm")] [InlineData("trapped-storeroom", "fight")] [InlineData("trapped-storeroom", "tunnel")]
    [InlineData("poisoned-spring", "repair")] [InlineData("poisoned-spring", "fight")] [InlineData("poisoned-spring", "tunnel")]
    [InlineData("guarded-crossing", "repair")] [InlineData("guarded-crossing", "negotiate")] [InlineData("guarded-crossing", "fight")] [InlineData("guarded-crossing", "tunnel")]
    public void EachApproachExecutesRealWorkCombatOrExcavationAndOnlyOneReward(string id, string method)
    {
        var result = SituationPreview.RunMethod(ContentCatalog.Default, "discovery." + id, method, 42);
        Assert.True(result.ActionAccepted); Assert.Equal(method, result.Outcome); Assert.Equal(0, result.RemainingOrders);
        Assert.True(result.RewardClaimed); Assert.True(result.RepeatRejected);
        if (method == "tunnel") Assert.True(Materials.Walkable(result.SideWall)); else Assert.Equal(Material.Wall, result.SideWall);
        if (id == "guarded-crossing") Assert.Equal(method == "tunnel" ? Material.Water : Material.Bridge, result.Bridge);
    }

    [Theory]
    [InlineData("trapped-storeroom", "disarm")] [InlineData("poisoned-spring", "repair")] [InlineData("guarded-crossing", "repair")]
    public void ReservedWorkAndOutcomeContinueExactlyAcrossSave(string id, string method)
    {
        var s = WorldSession.Create(1337); var d = SituationPreview.Prepare(s, "discovery." + id);
        Assert.True(s.Submit(new(WorldCommandKind.ApproachDiscovery, d.Position, Item: method)).Success); s.Paused = false; s.AdvanceTicks(3);
        Assert.Single(s.State.Work); Assert.Equal(WorkKind.Discovery, s.State.Work[0].Kind); Assert.Equal(method, s.State.Work[0].Method);
        s.Save(Slot); var loaded = WorldSession.Load(Slot); loaded.Paused = false;
        for (int t = 0; t < 150 && s.State.Work.Count > 0; t++) { s.AdvanceTicks(1); loaded.AdvanceTicks(1); }
        Assert.Empty(s.State.Work); Assert.Equal(method, d.Outcome); Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        var cache = s.State.Loot.Single(c => c.Id == d.Id);
        Assert.True(s.Submit(new(WorldCommandKind.Loot, d.Position, Item: cache.Choices[0])).Success);
        s.Save(Slot); loaded = WorldSession.Load(Slot);
        Assert.False(loaded.Submit(new(WorldCommandKind.Loot, d.Position, Item: cache.Choices[0])).Success);
        Assert.Equal(1, loaded.Count(cache.Choices[0]));
    }

    [Fact]
    public void NegotiationIsAPersistentLocalAgreementWithItsActualPayment()
    {
        var s = WorldSession.Create(42); var d = SituationPreview.Prepare(s, "discovery.guarded-crossing"); var guard = s.State.Actors.Single(a => a.Id == d.Enemies[0]);
        Assert.False(s.IsHostile(guard)); Assert.True(s.DiscoveryGuarded(d)); int gold = s.Count("gold"); uint random = s.State.CombatRandom; long tick = s.State.Tick;
        Assert.True(s.Submit(new(WorldCommandKind.ApproachDiscovery, d.Position, Item: "negotiate")).Success);
        Assert.Equal(gold - 12, s.Count("gold")); Assert.Equal(tick, s.State.Tick); Assert.Equal(random, s.State.CombatRandom);
        Assert.Equal(LifeState.Alive, guard.Life); Assert.False(s.IsHostile(guard)); Assert.False(s.DiscoveryGuarded(d));
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.ApproachDiscovery, d.Position, Item: "negotiate")).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        s.Save(Slot); var loaded = WorldSession.Load(Slot); Assert.Equal("negotiate", loaded.State.Discoveries.Single(x => x.Id == d.Id).Outcome);
        Assert.False(loaded.IsHostile(loaded.State.Actors.Single(a => a.Id == guard.Id)));
    }

    [Fact]
    public void CancellationAndInvalidChoicesNeverSpendExtraOrResolveAPlace()
    {
        var s = WorldSession.Create(42); var d = SituationPreview.Prepare(s, "discovery.trapped-storeroom"); int wood = s.Count("wood");
        string before = JsonSerializer.Serialize(s.State); Assert.False(s.Submit(new(WorldCommandKind.ApproachDiscovery, d.Position, Item: "invented")).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
        Assert.True(s.Submit(new(WorldCommandKind.ApproachDiscovery, d.Position, Item: "disarm")).Success);
        Assert.Equal(wood - 1, s.Count("wood")); long order = s.State.Work.Single().Id;
        Assert.True(s.Submit(new(WorldCommandKind.CancelWork, WorkId: order)).Success); Assert.Equal(wood, s.Count("wood"));
        Assert.False(s.Submit(new(WorldCommandKind.CancelWork, WorkId: order)).Success); Assert.Equal("", d.Outcome);
        s.State.Inventory["wood"] = 0; before = JsonSerializer.Serialize(s.State);
        Assert.Contains("Needs", s.SituationApproachReason(d, "disarm")); Assert.False(s.Submit(new(WorldCommandKind.ApproachDiscovery, d.Position, Item: "disarm")).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }

    [Fact]
    public void DiggingOpensAnActualRouteAndReleasesSupersededReservedWork()
    {
        var s = WorldSession.Create(42); var d = SituationPreview.Prepare(s, "discovery.trapped-storeroom"); var wall = s.DiscoveryBypass(d)!.Value; int wood = s.Count("wood");
        Assert.True(s.Submit(new(WorldCommandKind.ApproachDiscovery, d.Position, Item: "disarm")).Success);
        s.Submit(new(WorldCommandKind.ConfigureWork, WorkId: s.State.Work.Single().Id, Item: "suspend"));
        var origin = wall.Offset(Math.Sign(wall.X - d.Position.X), Math.Sign(wall.Y - d.Position.Y));
        s.State.Position = s.Party.First().Position = origin; s.State.WorkRally = origin; s.Reveal();
        Assert.Equal(Material.Wall, s.Chunks.At(wall)); Assert.Equal(1, s.DesignateWork(WorldCommandKind.Dig, [wall]).Queued);
        s.Paused = false; for (int t = 0; t < 150 && d.Outcome.Length == 0; t++) s.AdvanceTicks(1); s.AdvanceTicks(1);
        Assert.Equal("tunnel", d.Outcome); Assert.Empty(s.State.Work); Assert.Equal(wood, s.Count("wood"));
        Assert.Contains(wall, s.RouteToAdjacent(d.Position)); Assert.True(s.GroundOpen(wall));
        s.Save(Slot); var loaded = WorldSession.Load(Slot); Assert.True(loaded.GroundOpen(wall)); Assert.Equal("tunnel", loaded.State.Discoveries.Single(x => x.Id == d.Id).Outcome);
    }

    [Fact]
    public void EveryGeneratedSituationKeepsItsPhysicalAlternativesAcrossSeeds()
    {
        var positions = new HashSet<WorldPosition>();
        for (int seed = 42; seed < 46; seed++)
        {
            var s = WorldSession.Create(seed); var situations = s.State.Discoveries.Where(d => s.Situation(d) != null).ToArray();
            Assert.Equal(3, situations.Select(d => d.Definition).Distinct().Count());
            foreach (var d in situations)
            {
                var wall = s.DiscoveryBypass(d)!.Value;
                Assert.True(positions.Add(d.Position)); Assert.Equal(Material.Wall, s.Chunks.At(wall));
                if (WorldSession.SituationFeature(d, s.Situation(d)!.BridgeCells) is { } bridge) Assert.Equal(Material.Water, s.Chunks.At(bridge));
            }
        }
    }

    [Fact]
    public void InvalidOutcomeAndForgedWorkAreRejectedWithoutReplacingSaves()
    {
        var s = WorldSession.Create(42); var d = SituationPreview.Prepare(s, "discovery.trapped-storeroom");
        d.Outcome = "negotiate"; s.Save(Slot); string saved = File.ReadAllText(Slot);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot)); Assert.Equal(saved, File.ReadAllText(Slot));
        d.Outcome = ""; s.Submit(new(WorldCommandKind.ApproachDiscovery, d.Position, Item: "disarm")); s.State.Work.Single().Required = 1;
        s.Save(Slot); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }

    [Fact]
    public void RetreatKeepsSurvivingGuardsHostileAndDoesNotReleaseTheirReward()
    {
        var s = WorldSession.Create(42); var d = SituationPreview.Prepare(s, "discovery.guarded-crossing"); var hero = s.Party.First();
        hero.Role = 0; hero.BaseSpeed = 30; hero.BaseHealth = hero.Health = 200; hero.BasePower = 100;
        s.StartBattle(d.Enemies[0]); Assert.True(d.Engaged); Assert.True(s.Act(0, 1).Success);
        while (s.State.Battle is { Active.Friendly: false }) s.AdvanceEnemy();
        Assert.True(s.Act(5).Success); Assert.False(s.IsInCombat); Assert.Equal("", d.Outcome); Assert.True(s.DiscoveryGuarded(d));
        Assert.Single(d.Enemies, id => s.State.Actors.Single(a => a.Id == id).Life == LifeState.Dead);
        s.Save(Slot); var loaded = WorldSession.Load(Slot); var saved = loaded.State.Discoveries.Single(x => x.Id == d.Id);
        Assert.True(saved.Engaged); Assert.True(loaded.DiscoveryGuarded(saved));
        Assert.All(saved.Enemies.Select(id => loaded.State.Actors.Single(a => a.Id == id)).Where(a => a.Life == LifeState.Alive), a => Assert.True(loaded.IsHostile(a)));
    }
    [Fact]
    public void ApproachReferencesAndTerrainCoordinatesAreValidated()
    {
        var catalog = ContentCatalog.Default;
        Assert.Contains(catalog.References, r => r.Source == "discovery.poisoned-spring" && r.Target == "item.herbs" && r.Field == "situation.approaches.cost");
        var broken = ContentCatalog.Load(catalog.Entries.Values.Select(d =>
        {
            var n = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!;
            if (d.Id == "discovery.trapped-storeroom") n["situation"]!["tunnelWalls"] = new JsonArray(new JsonObject { ["x"] = 0, ["y"] = 0 });
            return (File: catalog.Source(d.Id), Text: n.ToJsonString());
        }));
        Assert.Contains(broken.Issues, i => i.Field == "situation.tunnelWalls");
    }
}
