using System.Diagnostics;
using System.Text.Json;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;

public static class StreamingBenchmarks
{
    public static object Run(ContentCatalog catalog, int seed, int samples)
    {
        if (samples is < 3 or > 100) throw new ArgumentOutOfRangeException(nameof(samples), "Use 3..100 samples.");
        var plan = WorldPlan.Generate(seed); var plainState = new WorldState(); var preparedState = new WorldState();
        using var plain = new ChunkCache(plan, plainState, catalog); using var prepared = new ChunkCache(plan, preparedState, catalog);
        var cold = new List<double>(); var ready = new List<double>(); double preparationMs = 0;
        var hashes = new List<string>(); var plainMisses = new List<ChunkKey>(); var preparedMisses = new List<ChunkKey>();
        void Record(WorldDiagnosticEvent e, List<ChunkKey> misses)
        {
            if (e.Kind == "chunk-generation") misses.Add(JsonSerializer.SerializeToElement(e.Data).GetProperty("key").Deserialize<ChunkKey>());
        }
        plain.DiagnosticSink = e => Record(e, plainMisses); prepared.DiagnosticSink = e => Record(e, preparedMisses);
        for (int i = 0; i < samples; i++)
        {
            var center = plan.Sites[i % plan.Sites.Count].Center with { Floor = i % WorldPlan.Floors };
            long started = Stopwatch.GetTimestamp();
            prepared.PrepareView(center, center.X - 8, center.Y - 8, center.X + 8, center.Y + 8, 1);
            prepared.PreparationCompletion.GetAwaiter().GetResult();
            preparationMs += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            started = Stopwatch.GetTimestamp(); var expected = plain.Get(center.Chunk); cold.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            started = Stopwatch.GetTimestamp(); var actual = prepared.Get(center.Chunk); ready.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            if (expected.BaseHash != actual.BaseHash || !expected.Tiles.SequenceEqual(actual.Tiles)
                || !expected.Biomes.SequenceEqual(actual.Biomes) || !expected.Cultures.SequenceEqual(actual.Cultures))
                throw new InvalidDataException("Background terrain differs from demand generation.");
            hashes.Add(actual.BaseHash);
            if (plain.ResidentCount != prepared.ResidentCount || prepared.ResidentCount > ChunkCache.Capacity
                || prepared.PreparationStatus.Requested > ChunkCache.PreparationCapacity) throw new InvalidDataException("Cache bound/order changed.");
        }
        if (!plainMisses.SequenceEqual(preparedMisses) || JsonSerializer.Serialize(plainState) != JsonSerializer.Serialize(preparedState))
            throw new InvalidDataException("Speculation changed authoritative state or demand cache order.");
        object Summary(List<double> times)
        {
            times.Sort(); return new { medianMs = times[times.Count / 2], p95Ms = times[(int)((times.Count - 1) * .95)], worstMs = times[^1] };
        }
        return new { schemaVersion = 1, seed, samples, catalog.Hash, hashes, generationMatches = true, demandOrderMatches = true,
            savedStateMatches = true, synchronousDemand = Summary(cold), preparedDemand = Summary(ready), preparationMs,
            preparation = prepared.PreparationStatus, residentChunks = prepared.ResidentCount,
            scope = "Headless terrain/cache workload across site floors. Waits explicitly for speculative work before demand to measure ready-hit adoption separately from preparation cost. Not a frame-rate prediction or proof that travel always outruns generation. No player saves or graphics." };
    }
}
