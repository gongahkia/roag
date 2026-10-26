using System.Diagnostics;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public static class WorldDiagnostics
{
    public static object GenerationReport(int seed)
    {
        var timer = Stopwatch.StartNew(); var plan = WorldPlan.Generate(seed); var generator = new ChunkGenerator(plan);
        var traces = new List<GenerationTrace>();
        foreach (var town in plan.Towns) traces.Add(generator.GenerateFootprint(true, town.Id).Trace);
        foreach (var site in plan.Sites) for (int floor = 0; floor < WorldPlan.Floors; floor++) traces.Add(generator.GenerateFootprint(false, site.Id, floor).Trace);
        return new { seed, generationVersion = plan.Version, sampleHash = plan.SampleHash, dimensions = new { tilesAcross = WorldPlan.Size, floors = WorldPlan.Floors, chunkSize = 64 }, towns = plan.Towns.Count, sites = plan.Sites.Count, factions = 6, biomes = Enum.GetNames<Biome>(), footprintCount = traces.Count, fallbackRegions = traces.Sum(t => t.FallbackRegions), repairedCells = traces.Sum(t => t.RepairedCells), elapsedMs = timer.Elapsed.TotalMilliseconds, samples = SampleLibrary.Default.Samples.Select(s => new { s.Id, patterns = SampleLibrary.Default.Model(Array.FindIndex(SampleLibrary.Default.Cultures, c => c.Sample == s.Id)).PatternCount }), traces };
    }
    public static object ValidateSeeds(int first, int count)
    {
        var timer = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            int seed = unchecked(first + i); var plan = WorldPlan.Generate(seed); var errors = plan.Validate();
            if (errors.Count != 0) throw new InvalidDataException($"Seed {seed}: {string.Join("; ", errors)}");
        }
        return new { validatedWorlds = count, firstSeed = first, elapsedMs = timer.Elapsed.TotalMilliseconds, scope = "Macro geography, complete counts, bounds, factions and road connectivity. Use --world-report for full footprints." };
    }
    public static object Headless(int seed)
    {
        var s = WorldSession.Create(seed);
        var person = s.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).MinBy(a => a.Position.Distance(s.State.Position))!;
        foreach (var p in s.RouteToAdjacent(person.Position, 2)) s.Submit(new(WorldCommandKind.Move, p));
        var recruit = s.Submit(new(WorldCommandKind.Recruit, ActorId: person.Id));
        var camp = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.Select(d => s.State.Position.Offset(d.Item1, d.Item2)).First(p => s.CanEnter(p));
        var build = s.Submit(new(WorldCommandKind.Build, camp, Material: Material.Camp));
        s.Submit(new(WorldCommandKind.Wait, Amount: 50));
        var rest = s.Submit(new(WorldCommandKind.Camp));
        var trade = s.Submit(new(WorldCommandKind.Buy, Item: "food", Amount: 3));
        s.Paused = false; long resources = s.AccountedResources();
        long flow = s.RegionalResourceFlow;
        var samples = new List<double>();
        for (int i = 0; i < 600; i++) { var timer = Stopwatch.StartNew(); s.AdvanceTicks(1); samples.Add(timer.Elapsed.TotalMilliseconds); }
        samples.Sort();
        if (!recruit.Success || !build.Success || !rest.Success || !trade.Success || resources + s.RegionalResourceFlow - flow != s.AccountedResources()) throw new InvalidDataException("World headless scenario failed.");
        return new { seed, world = s.Plan.Id, generatedHero = s.Party.First().Name, party = s.Party.Select(a => new { a.Id, a.Name, a.Role }), ticks = s.State.Tick, recruit = recruit.Success, camp = build.Success && rest.Success, trade = trade.Success, resourceConservation = true, residentChunks = s.Chunks.ResidentCount, cacheCapacity = ChunkCache.Capacity, simulationTickP95Ms = samples[(int)(samples.Count * .95)], simulationTickMaxMs = samples[^1], note = "Measured Core tick timings; graphical frame rate is a separate native check." };
    }
}
