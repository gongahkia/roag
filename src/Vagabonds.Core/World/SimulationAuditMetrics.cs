using System.Diagnostics;
using System.Text.Json;

namespace Vagabonds.Core.World;

public sealed record AuditTickTiming(long Samples, long AllocatedBytes, double TotalMs, double WorstMs,
    double? P95UpperBoundMs, double? P99UpperBoundMs, long Over200Ms);
public sealed record AuditSaveSize(long ReferencedBytes, long DirectoryBytes, int ReferencedChunks, int UnreferencedChunks);
public sealed record AuditResourceFlow(long NamedProduced, long RegionalProduced, long Consumed, long Lost,
    int EmptyFoodTowns, int StarvingPeople, int PeopleAtOneHealth);
public sealed record AuditGrowth(long Tick, WorldSeason Season, long StateBytes, long ProcessRetainedManagedBytes,
    long ProcessAllocatedBytes, int[] Collections, AuditSaveSize Save, IReadOnlyDictionary<string, int> Counts, AuditResourceFlow Resources);
public sealed record AuditMeasurements(AuditTickTiming Scheduled, AuditTickTiming Detailed, IReadOnlyList<AuditGrowth> Growth);

/// <summary>Bounded measurement storage; timing/allocation covers actual tick calls only.</summary>
internal sealed class AuditTickMeter
{
    private static readonly double[] Bounds = [.001, .002, .004, .008, .016, .032, .064, .128, .256, .512, 1, 2, 4, 8, 16, 32, 64, 128, 200, 512, 1024];
    private readonly long[] _histogram = new long[Bounds.Length + 1];
    private long _samples, _allocated, _over200;
    private double _total, _worst;
    public int Advance(WorldSession world)
    {
        long bytes = GC.GetAllocatedBytesForCurrentThread(), started = Stopwatch.GetTimestamp();
        int advanced = world.AdvanceTicks(1);
        double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        _allocated += GC.GetAllocatedBytesForCurrentThread() - bytes;
        _samples++; _total += elapsed; _worst = Math.Max(_worst, elapsed); if (elapsed > 200) _over200++;
        int bucket = 0; while (bucket < Bounds.Length && elapsed > Bounds[bucket]) bucket++; _histogram[bucket]++;
        return advanced;
    }
    private double? Percentile(double fraction)
    {
        long target = (long)Math.Ceiling(_samples * fraction), count = 0;
        for (int i = 0; i < _histogram.Length; i++) if ((count += _histogram[i]) >= target) return i < Bounds.Length ? Bounds[i] : null;
        return null;
    }
    public AuditTickTiming Report() => new(_samples, _allocated, _total, _worst, Percentile(.95), Percentile(.99), _over200);
}

internal static class SimulationAuditMetrics
{
    public static bool GrowthCheckpoint(int tick, int total) => tick == 0 || tick == total || tick == total / 2
        || tick % WorldSession.TicksPerDay == 0 && tick / WorldSession.TicksPerDay is 7 or 30 or 60 or 90 or 100;
    public static AuditGrowth Capture(WorldSession world, string path, long allocatedAtStart, int[] collectionsAtStart)
    {
        var counts = world.AuditRetainedCounts();
        if (counts["residentChunks"] > ChunkCache.Capacity || counts["footprints"] > 8
            || counts["bodyDeadlines"] > counts["actors"] || counts["bodyBindings"] > counts["actors"]
            || counts["journeySteps"] > counts["actors"] * WorldSession.ResidentJourneyLimit
            || counts["maxMemories"] > 12 || counts["maxConsequences"] > 4 || counts["journal"] > 500
            || counts["habitats"] > 16384 || counts["populations"] > WorldSession.WildlifePopulationLimit
            || counts["maxPopulationHistory"] > WorldSession.WildlifeHistoryLimit
            || counts["weatherSlots"] > 64 || counts["habitatReadSlots"] > 128
            || counts["preparationRequested"] > ChunkCache.PreparationCapacity || counts["preparationInFlight"] > 1
            || counts["uniqueActorIds"] != counts["actors"])
            throw new InvalidDataException("Simulation audit exceeded a retained-state/identity bound at " + world.State.Tick + ".");
        byte[] state = JsonSerializer.SerializeToUtf8Bytes(world.State);
        world.Save(path);
        using (var loaded = WorldSession.Load(path, world.Content))
            if (!state.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(loaded.State)))
                throw new InvalidDataException("Growth checkpoint save changed state at " + world.State.Tick + ".");
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(path));
        var hashes = manifest.RootElement.GetProperty("chunks").EnumerateArray().Select(c => c.GetProperty("hash").GetString()!).ToHashSet();
        long manifestBytes = new FileInfo(path).Length;
        long active = manifestBytes + hashes.Sum(hash => new FileInfo(Path.Combine(path + ".chunks", hash + ".json")).Length);
        var files = Directory.GetFiles(path + ".chunks", "*.json");
        var save = new AuditSaveSize(active, manifestBytes + files.Sum(file => new FileInfo(file).Length), hashes.Count, files.Count(file => !hashes.Contains(Path.GetFileNameWithoutExtension(file))));
        var people = world.State.Actors.Where(a => a.Kind == ActorKind.Person && a.Life == LifeState.Alive).ToArray();
        var flow = new AuditResourceFlow(world.State.NamedResourcesProduced, world.State.RegionalResourcesProduced,
            world.State.ResourcesConsumed, world.State.ResourcesLost, world.State.Towns.Count(t => t.Stocks["food"] == 0),
            people.Count(a => a.Body.Hunger >= world.Simulation.StarveAt), people.Count(a => a.Health == 1));
        return new(world.State.Tick, world.WeatherAt(world.State.Position).Season, state.Length, GC.GetTotalMemory(true),
            GC.GetTotalAllocatedBytes() - allocatedAtStart, Enumerable.Range(0, 3).Select(i => GC.CollectionCount(i) - collectionsAtStart[i]).ToArray(), save, counts, flow);
    }
}

public sealed partial class WorldSession
{
    internal Dictionary<string, int> AuditRetainedCounts() => new()
    {
        ["actors"] = State.Actors.Count, ["uniqueActorIds"] = State.Actors.Select(a => a.Id).Distinct().Count(),
        ["residentChunks"] = Chunks.ResidentCount, ["footprints"] = Chunks.Generator.CachedFootprints,
        ["chunkDeltas"] = State.Chunks.Count, ["habitats"] = State.Habitats.Count, ["populations"] = State.WildlifePopulations.Count,
        ["maxPopulationHistory"] = State.WildlifePopulations.Values.Select(p => p.History.Count).DefaultIfEmpty().Max(),
        ["maxMemories"] = State.Actors.Select(a => a.Memories.Count).DefaultIfEmpty().Max(),
        ["maxConsequences"] = State.Actors.Select(a => a.Consequences.Count).DefaultIfEmpty().Max(),
        ["journal"] = State.Journal.Count, ["tasks"] = State.Tasks.Count, ["orders"] = State.Work.Count,
        ["activatedWorkers"] = State.Actors.Count(a => a.OccupationVersion > 0), ["occupations"] = State.Actors.Count(a => a.Occupation != null),
        ["journeySteps"] = ResidentJourneys.RetainedSteps, ["bodyDeadlines"] = BodySimulation.QueuedDeadlines, ["bodyBindings"] = BodySimulation.BoundActors,
        ["weatherSlots"] = _climateRegions.Length, ["habitatReadSlots"] = _habitatReads?.Length ?? 0,
        ["preparationRequested"] = Chunks.PreparationStatus.Requested, ["preparationInFlight"] = Chunks.PreparationStatus.InFlight
    };
}
