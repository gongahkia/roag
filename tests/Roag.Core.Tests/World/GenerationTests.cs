using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class GenerationTests
{
    [Fact]
    public void ThousandMacroWorldsAndSignedExtremesAreConnectedAndComplete()
    {
        foreach (int seed in Enumerable.Range(-500, 1000).Concat(new[] { int.MinValue, int.MaxValue }))
        {
            var plan = WorldPlan.Generate(seed);
            Assert.Empty(plan.Validate());
            Assert.Equal(6, plan.BiomeRegions.Select(b => plan.BiomeAt(b.X, b.Y)).Distinct().Count());
            Assert.Equal(24, plan.Towns.Select(t => t.Id).Distinct().Count());
        }
    }
    [Fact]
    public void OverlappingModelSupportsHundredsOfPatternsAndEnforcesEveryOverlap()
    {
        var rows = Enumerable.Range(0, 14).Select(y => new string(Enumerable.Range(0, 14).Select(x => (char)('A' + WorldRandom.Hash(73, x, y) % 5)).ToArray())).ToArray();
        var model = new OverlappingWfc(rows, 3, true, true);
        Assert.True(model.PatternCount > 63);
        var output = Assert.IsType<CollapseResult>(model.Solve(8, 8, 19));
        int gw = 6;
        for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++)
        {
            if (x < 5) Assert.True(model.Compatible(output.Patterns[y * gw + x], output.Patterns[y * gw + x + 1], 0));
            if (y < 5) Assert.True(model.Compatible(output.Patterns[y * gw + x], output.Patterns[(y + 1) * gw + x], 1));
        }
        Assert.Equal(output.Cells, model.Solve(8, 8, 19)!.Cells);
        Assert.Null(model.Solve(8, 8, 19, new Dictionary<int, char> { [0] = '?' }));
    }
    [Fact]
    public void FootprintsHaveConnectedFloorsAndDeterministicBoundedFallbacks()
    {
        var plan = WorldPlan.Generate(42); var generator = new ChunkGenerator(plan);
        for (int culture = 0; culture < 6; culture++)
        {
            foreach (bool force in new[] { false, true })
            {
                var footprint = generator.GenerateFootprint(false, culture, 1, force);
                Assert.InRange(footprint.Trace.FallbackRegions, 0, 4);
                if (force) Assert.Equal(4, footprint.Trace.FallbackRegions);
                Assert.InRange(footprint.Trace.Attempts, footprint.Trace.PatternCount > 0 ? 4 : 0, 32);
                var reached = new HashSet<int> { 32 * 64 + 32 }; var q = new Queue<int>(reached);
                while (q.TryDequeue(out int i)) foreach (int offset in new[] { -1, 1, -64, 64 })
                {
                    int n = i + offset;
                    if (n >= 0 && n < 4096 && Math.Abs(n % 64 - i % 64) + Math.Abs(n / 64 - i / 64) == 1 && Materials.Walkable(footprint.Cells[n]) && reached.Add(n)) q.Enqueue(n);
                }
                Assert.Equal(footprint.Cells.Count(Materials.Walkable), reached.Count);
                Assert.Equal(Material.StairsUp, footprint.Cells[32 * 64 + 30]);
                Assert.Equal(Material.StairsDown, footprint.Cells[32 * 64 + 34]);
            }
        }
    }
    [Fact]
    public void VarietyAuditExercisesDistinctTopologiesAndConnectedStairs()
    {
        var audit = WorldVarietyAudit.Run(-1, 3);
        Assert.True(audit.Report.Connected);
        Assert.Equal(51, audit.Report.Footprints);
        Assert.Equal(6, audit.Report.Families.Count);
        Assert.True(audit.Report.DistinctRawLayouts >= 45);
        Assert.All(audit.Report.Measurements, m => Assert.InRange(m.Walkable, 500, 4096));
        Assert.Contains("ring-sanctuary", audit.Svg);
        var plans = Enumerable.Range(0, 4).Select(WorldPlan.Generate).ToArray();
        // Distribution is no longer a fixed grid translated by a small seed offset.
        Assert.Equal(4, plans.Select(p => p.Towns[12].Center.X / 512).Distinct().Count());
    }
    [Fact]
    public void ChunkOrderEvictionAndWorldSeamsCannotRerollTerrainOrEdits()
    {
        var plan = WorldPlan.Generate(1337); var state = new WorldState(); var cache = new ChunkCache(plan, state);
        var center = plan.Towns[0].Center;
        var keys = Enumerable.Range(0, 32).Select(i => new ChunkKey(i, 8)).Append(center.Chunk).ToArray();
        var hashes = keys.ToDictionary(k => k, k => cache.Get(k).BaseHash);
        foreach (var k in keys.Reverse()) Assert.Equal(hashes[k], cache.Get(k).BaseHash);
        Assert.InRange(cache.ResidentCount, 1, 25);
        cache.Edit(center, Material.Camp);
        foreach (var k in keys) cache.Get(k);
        Assert.Equal(Material.Camp, cache.At(center));
        // Sliced structure matches the same global footprint, including chunk boundaries.
        var f = cache.Generator.GenerateFootprint(true, 0);
        for (int y = -31; y < 32; y += 7) for (int x = -31; x < 32; x += 7)
        {
            var p = center.Offset(x, y);
            if (plan.Roads.Any(r => r.Contains(p))) continue;
            Assert.Equal(f.Cells[(y + 32) * 64 + x + 32], cache.Get(p.Chunk).Tiles[p.LocalIndex]);
        }
    }
}
