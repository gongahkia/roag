using System.Diagnostics;
using System.Text.Json;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;

public static class PerformanceBenchmarks
{
    public sealed record Measurement(string Name, int Samples, int OperationsPerSample, double MedianMs, double P95Ms, double BytesPerOperation);
    public static object Run(ContentCatalog catalog, int seed, int samples)
    {
        if (samples is < 3 or > 100) throw new ArgumentOutOfRangeException(nameof(samples), "Use 3..100 samples.");
        var world = WorldSession.Create(seed, catalog); var origin = world.State.Position;
        world.RevealSurface(origin.X - 20, origin.Y - 12, origin.X + 20, origin.Y + 12);
        var cells = world.Visible.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray();
        var results = new List<Measurement>();
        long checksum = 0;
        void InspectCells()
        {
            foreach (var cell in cells)
            {
                checksum += (int)world.Chunks.At(cell);
                checksum += world.ObjectAt(cell)?.Solid == true ? 1 : 0;
                checksum += world.ResourceAt(cell)?.Available ?? 0;
            }
        }
        string before = JsonSerializer.Serialize(world.State);
        results.Add(Measure("visible-cell-queries", samples, 10, InspectCells));
        bool inspectionReadOnly = before == JsonSerializer.Serialize(world.State);
        var blocked = origin.Offset(4, 0); world.Chunks.Edit(blocked, Material.Wall);
        results.Add(Measure("blocked-click-route", samples, 3, () => checksum += world.Route(blocked).Count));

        var worker = PrepareWorker(catalog, seed);
        results.Add(Measure("adjacent-work-tick", samples, 5, () => worker.AdvanceTicks(1)));
        return new { schemaVersion = 1, seed, catalog.Hash, runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            processorCount = Environment.ProcessorCount, cells = cells.Length, inspectionReadOnly, checksum, results,
            simulationHash = DiagnosticJson.Hash(JsonSerializer.SerializeToUtf8Bytes(worker.State)),
            scope = "Warmed synchronous Core workloads, milliseconds and current-thread allocations per operation. World creation/setup excluded. No player saves or graphics. Timing is comparative evidence, not a CI threshold." };
    }

    private static WorldSession PrepareWorker(ContentCatalog catalog, int seed)
    {
        var world = WorldSession.Create(seed, catalog); var origin = world.State.Position;
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited && a.Position.Distance(origin) < 100))
            actor.Position = origin.Offset(150 + (int)(actor.Id % 20), 0);
        for (int y = -10; y <= 10; y++) for (int x = -10; x <= 10; x++) world.Chunks.Edit(origin.Offset(x, y), Material.Floor);
        var target = origin.Offset(1, 0); world.Chunks.Edit(target, Material.Rock); world.Reveal();
        if (!world.Submit(new(WorldCommandKind.Dig, target)).Success) throw new InvalidOperationException("Cannot prepare benchmark work.");
        world.State.Work.Single().Required = 1_000_000;
        world.Paused = false; return world;
    }

    private static Measurement Measure(string name, int samples, int repetitions, Action action)
    {
        for (int i = 0; i < 10; i++) action();
        var times = new double[samples]; long bytes = 0;
        for (int sample = 0; sample < samples; sample++)
        {
            long allocated = GC.GetAllocatedBytesForCurrentThread(), started = Stopwatch.GetTimestamp();
            for (int i = 0; i < repetitions; i++) action();
            times[sample] = Stopwatch.GetElapsedTime(started).TotalMilliseconds / repetitions;
            bytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
        }
        Array.Sort(times);
        return new(name, samples, repetitions, times[samples / 2], times[(int)((samples - 1) * .95)], bytes / (double)(samples * repetitions));
    }
}
