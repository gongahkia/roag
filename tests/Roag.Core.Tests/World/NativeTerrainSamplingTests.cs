using System.Text.Json;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeTerrainSamplingTests
{
    [Theory]
    [InlineData(42)] [InlineData(-2147483648)]
    public void ColdCoordinateSamplingMatchesWholeGeneratedChunksAtFootprintEdgesStairsRoadsAndEveryDepth(int seed)
    {
        var plan = WorldPlan.Generate(seed, false, floorCount: 8);
        var generator = new ChunkGenerator(plan) { FootprintCapacity = plan.Towns.Count + plan.Sites.Count * plan.FloorCount };
        var chunks = new Dictionary<ChunkKey, GeneratedChunk>();
        var points = new HashSet<WorldPosition>();
        void Around(WorldPosition center)
        {
            foreach (int x in new[] { -33, -32, -2, 0, 2, 31, 32 })
                foreach (int y in new[] { -33, -32, 0, 31, 32 })
                    if (plan.Contains(center.Offset(x, y))) points.Add(center.Offset(x, y));
        }
        foreach (var town in plan.Towns.Take(4)) Around(town.Center);
        foreach (var site in plan.Sites.Take(4))
            for (int floor = 0; floor < plan.FloorCount; floor++) Around(site.Center with { Floor = floor });
        foreach (var road in plan.Roads.Take(8))
        {
            Around(road.A); Around(road.B);
            // Include road interiors and river crossings, not only endpoints.
            for (int n = 0; n <= 16; n++)
                points.Add(new(road.A.X + (road.B.X - road.A.X) * n / 16, road.A.Y + (road.B.Y - road.A.Y) * n / 16));
        }
        foreach (var p in points)
        {
            if (!chunks.TryGetValue(p.Chunk, out var chunk)) chunks.Add(p.Chunk, chunk = generator.Generate(p.Chunk));
            Assert.Equal(chunk.Tiles[p.LocalIndex], generator.SampleAt(p));
        }
        Assert.True(points.Count > 1000);
        Assert.Equal(Material.Rock, generator.SampleAt(new(-1, 0)));
        Assert.Equal(Material.Rock, generator.SampleAt(new(0, 0, plan.FloorCount)));
    }

    [Fact]
    public void NativeColdQueriesKeepEditsLandmarksAndExactSavesWithoutFillingTheChunkCacheOrRevealingTerrain()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var cold = world.Plan.Towns.Skip(8).Take(12).Select(t => t.Center.Offset(-10, -10)).ToArray();
        int resident = world.Chunks.ResidentCount;
        string before = JsonSerializer.Serialize(world.State);
        foreach (var p in cold)
        {
            Assert.Equal(world.Chunks.Generator.SampleAt(p), world.Chunks.At(p));
            Assert.False(world.Chunks.Seen(p));
        }
        Assert.Equal(resident, world.Chunks.ResidentCount);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        var edited = cold[0]; world.Chunks.Edit(edited, Material.Camp);
        Assert.Equal(Material.Camp, world.Chunks.At(edited));
        var landmark = world.State.Discoveries[0].Position;
        Material terrain = world.Chunks.At(landmark); var room = world.RoomAt(landmark);
        // Evict every resident chunk through ordinary generation, then compare
        // the cold semantic overlay rather than just the generator's base tile.
        foreach (var p in cold) world.Chunks.Prefetch(p);
        Assert.InRange(world.Chunks.ResidentCount, 1, ChunkCache.Capacity);
        Assert.Equal(Material.Camp, world.Chunks.At(edited));
        Assert.Equal(terrain, world.Chunks.At(landmark)); Assert.Equal(room, world.RoomAt(landmark));
        string root = Path.Combine(Path.GetTempPath(), "roag-native-sampling-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(Material.Camp, loaded.Chunks.At(edited));
            Assert.Equal(terrain, loaded.Chunks.At(landmark)); Assert.Equal(room, loaded.RoomAt(landmark));
            Assert.True(world.SubmitDungeon(new(Roag.Core.Tactics.TacticalAction.Wait)).Success);
            Assert.True(loaded.SubmitDungeon(new(Roag.Core.Tactics.TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
