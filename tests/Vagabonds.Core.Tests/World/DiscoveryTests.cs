using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class DiscoveryTests
{
    private static void Position(WorldSession s, WorldPosition p)
    { s.State.Position = p; foreach (var a in s.Party) a.Position = p; s.Reveal(); }
    [Fact]
    public void SeededDiscoveriesCoverSettlementsAndKeepEveryAccessTrailOpen()
    {
        var catalog = ContentCatalog.Default; var templates = new HashSet<string>(); var layouts = new HashSet<string>();
        for (int seed = 40; seed < 56; seed++)
        {
            var plan = WorldPlan.Generate(seed); var finds = DiscoveryPlacement.Generate(plan, catalog);
            Assert.Equal(JsonSerializer.Serialize(finds), JsonSerializer.Serialize(DiscoveryPlacement.Generate(plan, catalog)));
            Assert.All(plan.Towns, t => Assert.Equal(4, finds.Count(d => d.Context == "settlement" && d.TownId == t.Id)));
            Assert.Equal(48, finds.Count(d => d.Context == "wilderness"));
            Assert.InRange(finds.Count, 180, 600);
            var terrain = DiscoveryPlacement.Terrain(finds, catalog);
            Assert.All(finds, d =>
            {
                Assert.All(DiscoveryPlacement.Trail(d.Anchor, d.Position), p => Assert.True(Materials.Walkable(terrain[p]), seed + "/" + d.Id));
                Assert.DoesNotContain(plan.Sites, s => Math.Abs(s.Center.X - d.Position.X) < 36 && Math.Abs(s.Center.Y - d.Position.Y) < 36);
                templates.Add(d.Definition); layouts.Add(d.Definition + "/" + d.Rotation);
            });
        }
        Assert.Equal(12, templates.Count); Assert.True(layouts.Count >= 40);
    }
    [Fact]
    public void ARealWalkFromTheStartingTownReachesAUsefulCampWithoutUnlockingAnything()
    {
        var s = WorldSession.Create(42); var camp = s.State.Discoveries.First(d => d.TownId == 0 && d.Context == "settlement");
        Assert.Equal("camp", s.Content.Get<DiscoveryDefinition>(camp.Definition).Action);
        var road = s.Plan.Roads[0]; int distance = road.A.Distance(camp.Anchor);
        var waypoints = Enumerable.Range(1, distance).Select(step => DiscoveryPlacement.Along(road, step))
            .Concat(DiscoveryPlacement.Trail(camp.Anchor, camp.Position).Skip(1).SkipLast(1));
        foreach (var p in waypoints)
        {
            if (!s.CanEnter(p)) continue; // Walk around people and solid roadside props.
            var path = s.Route(p); Assert.NotEmpty(path);
            foreach (var cell in path) { var moved = s.Submit(new(WorldCommandKind.Move, cell)); Assert.True(moved.Success, moved.Message); }
        }
        var result = s.Submit(new(WorldCommandKind.Discover, camp.Position)); Assert.True(result.Success);
        Assert.Contains(s.Plan.Towns[camp.TownId].Name, result.Message);
        Assert.Contains(s.State.Actors.Single(a => a.Id == camp.WitnessId).Name, result.Message);
        Assert.Contains(s.Plan.Sites[camp.SiteId].Name, result.Message);
        Assert.Equal(Material.Camp, s.Chunks.At(camp.Position)); Assert.Equal(0, s.State.Arc.Progress);
        int food = s.Count("food"); Assert.True(s.Submit(new(WorldCommandKind.Loot, camp.Position)).Success); Assert.True(s.Count("food") > food);
        Assert.Contains(camp.SiteId, s.State.KnownSites);
        Assert.False(s.Chunks.Seen(s.Plan.Sites[camp.SiteId].Center));
    }
    [Fact]
    public void LocalRumoursRevealMetadataWithoutRevealingTerrainOrSpendingTimeOrRandomness()
    {
        var s = WorldSession.Create(42); var npc = s.State.Actors.First(a => a.Archetype == s.Content.Game.Villager); npc.Position = s.State.Position.Offset(1, 0);
        string chunks = JsonSerializer.Serialize(s.State.Chunks); uint rng = s.State.CombatRandom; long tick = s.State.Tick;
        var result = s.PerformInteraction(npc.Id, "interaction.rumours");
        Assert.True(result.Success); Assert.Contains("tiles", result.Message); Assert.Contains(s.State.Discoveries, d => d.Known && d.TownId == npc.HomeTown);
        Assert.Equal(chunks, JsonSerializer.Serialize(s.State.Chunks)); Assert.Equal(rng, s.State.CombatRandom); Assert.Equal(tick, s.State.Tick);
    }
    [Fact]
    public void GuardedSuppliesCannotBeClaimedAndChoiceRewardsCannotBeDuplicatedOrRerolled()
    {
        var s = WorldSession.Create(42); var guarded = s.State.Discoveries.First(d => d.Enemies.Length > 0);
        Position(s, guarded.Position.Offset(0, 1)); var cache = s.State.Loot.Single(c => c.Id == guarded.Id);
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Loot, cache.Position, Item: cache.Choices[0])).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        var free = s.State.Discoveries.First(d => d.Enemies.Length == 0 && s.State.Loot.Any(c => c.Id == d.Id && c.Choices.Length == 3));
        Position(s, free.Position.Offset(0, 1)); cache = s.State.Loot.Single(c => c.Id == free.Id);
        before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Loot, cache.Position)).Success);
        Assert.False(s.Submit(new(WorldCommandKind.Loot, cache.Position, Item: "item.food")).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        Assert.Equal(3, cache.Choices.Select(id => s.Item(id).Slot).Distinct().Count());
        var item = s.Item(cache.Choices[0]); int count = s.Count(item.Stack);
        Assert.True(s.Submit(new(WorldCommandKind.Loot, cache.Position, Item: item.Id)).Success);
        Assert.Equal(count + 1, s.Count(item.Stack)); Assert.Equal(item.Id, cache.Selected);
        before = JsonSerializer.Serialize(s.State); Assert.False(s.Submit(new(WorldCommandKind.Loot, cache.Position, Item: cache.Choices[1])).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
    [Fact]
    public void LandmarksKnowledgeChoicesAndEditsSurviveEvictionAndExactSaveContinuation()
    {
        string root = Path.Combine(Path.GetTempPath(), "vagabonds-discovery-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var s = WorldSession.Create(42); var d = s.State.Discoveries.First(d => d.Enemies.Length == 0 && s.State.Loot.Any(c => c.Id == d.Id && c.Choices.Length == 3));
            Position(s, d.Position.Offset(0, 1)); Assert.True(s.Submit(new(WorldCommandKind.Discover, d.Position)).Success);
            var cache = s.State.Loot.Single(c => c.Id == d.Id); Assert.True(s.Submit(new(WorldCommandKind.Loot, d.Position, Item: cache.Choices[0])).Success);
            var edited = d.Position.Offset(0, 1); s.Chunks.Edit(edited, Material.Floor);
            for (int i = 0; i < 40; i++) s.Chunks.Get(new(i, 50));
            Assert.Equal(Material.Floor, s.Chunks.At(edited)); Assert.True(s.Chunks.ResidentCount <= 25);
            string slot = Path.Combine(root, "world.json"); s.Save(slot); string state = JsonSerializer.Serialize(s.State); var restored = WorldSession.Load(slot);
            Assert.Equal(state, JsonSerializer.Serialize(restored.State)); Assert.Equal(Material.Floor, restored.Chunks.At(edited));
            Assert.Equal(s.DiscoveryText(d), restored.DiscoveryText(restored.State.Discoveries.Single(m => m.Id == d.Id)));
            var node = JsonNode.Parse(File.ReadAllText(slot))!; node["state"]!["discoveries"]![0]!["anchor"]!["x"] = 4000; File.WriteAllText(slot, node.ToJsonString());
            Assert.Throws<InvalidDataException>(() => WorldSession.Load(slot));
        }
        finally { Directory.Delete(root, true); }
    }
}
