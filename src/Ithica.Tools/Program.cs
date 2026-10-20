using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Ithica.Core.Colony;
using Ithica.Core.Diagnostics;
using Ithica.Core.Generation;
using Ithica.Tools;
using IntegratedCampaign = Ithica.Core.Campaign;

var values = new Dictionary<string, string>();
var switches = new HashSet<string>();
try
{
    for (int i = 0; i < args.Length; i++)
    {
        string key = args[i];
        if (key is "--demo" or "--load" or "--benchmark" or "--help" or "--campaign-benchmark" or "--generation-report" or "--legacy-colony" or "--story-check"
            || key == "--headless" && (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)))
        {
            switches.Add(key);
            continue;
        }
        if (key is not ("--seed" or "--width" or "--height" or "--headless" or "--ticks" or "--agents" or "--save" or "--validate-seeds" or "--samples" or "--export-generation") || i + 1 >= args.Length)
        {
            throw new ArgumentException("Unknown or incomplete option: " + key);
        }

        values[key] = args[++i];
    }
    int Number(string key, int fallback) => values.TryGetValue(key, out string? value) ? int.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : fallback;
    if (switches.Contains("--help"))
    {
        Console.WriteLine("Ithica simulation tools\n  --headless [TICKS] [--demo] (without TICKS, completes the integrated loop)\n  --export-generation PATH.svg [--samples 4 --seed N] (developer map comparison)\n  --story-check (ordinary-command fault, repair, workload and recovery walkthrough)\n  --validate-seeds N --generation-report --campaign-benchmark [--samples N]\n  --legacy-colony --headless TICKS [--demo] [--seed N] [--width N --height N]\n  --benchmark [--agents N] [--ticks N] [--width N --height N]\n  --load --save PATH\nThe simulation tools never open a graphics window or write a save.");
        return 0;
    }
    if (switches.Contains("--benchmark"))
    {
        int width = Number("--width", 256), height = Number("--height", 256), agents = Number("--agents", 1000), ticks = Number("--ticks", 240);
        if (width < 48 || height < 32 || width > 2048 || height > 2048 || agents < 1 || agents > 100000 || ticks < 1 || ticks > 100000)
        {
            throw new ArgumentException("Invalid benchmark dimensions, agents, or ticks.");
        }

        var sim = new Simulation(Number("--seed", 42), width, height, false) { Resources = new(10000000, 10000000, 10000000, 10000000), IncidentsEnabled = false };
        Array.Fill(sim.World.Tiles, Terrain.Floor);
        Array.Fill(sim.World.Oxygen, .92f);
        Array.Fill(sim.World.Heat, 19);
        Array.Fill(sim.World.Discovered, (byte)1);
        for (int x = 0; x < width; x++)
        {
            sim.World.Tiles[sim.World.Index(x, 0)] = Terrain.Wall;
            sim.World.Tiles[sim.World.Index(x, height - 1)] = Terrain.Wall;
        }
        for (int y = 0; y < height; y++)
        {
            sim.World.Tiles[sim.World.Index(0, y)] = Terrain.Wall;
            sim.World.Tiles[sim.World.Index(width - 1, y)] = Terrain.Wall;
        }
        sim.World.InvalidateTopology();
        int slotsX = (width - 4) / 3, slotsY = (height - 4) / 3;
        for (int n = 0; n < agents; n++)
        {
            int slot = n % (slotsX * slotsY), x = 2 + (slot % slotsX) * 3, y = 2 + (slot / slotsX) * 3;
            sim.AddColonist("Crew " + n, n % 2 == 0 ? "Miner" : "Engineer", "Benchmark", sim.World.Index(x, y));
            if (n < slotsX * slotsY)
            {
                sim.World.SetTerrain(sim.World.Index(x + 1, y), Terrain.Ore);
                sim.Designate(x + 1, y, Tool.Dig);
            }
        }
        sim.AddMachine(ModuleKind.Rtg, sim.World.Center);
        sim.AddMachine(ModuleKind.Oxygen, sim.World.Center + 1);
        sim.UpdatePower();
        // Warm up JIT and work queues. The measured run includes real routes, mining,
        // needs, power, and atmosphere over a sealed, densely active habitat.
        sim.Step(16);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long bytesBefore = GC.GetTotalAllocatedBytes(true), pathsBefore = sim.Router.Searches;
        var timings = new double[ticks];
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < ticks; i++)
        {
            long a = Stopwatch.GetTimestamp();
            sim.Step();
            timings[i] = Stopwatch.GetElapsedTime(a).TotalMilliseconds;
        }
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        long allocated = GC.GetTotalAllocatedBytes(true) - bytesBefore;
        Array.Sort(timings);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            scenario = "sealed habitat, live needs, staggered work assignment, BFS routes, mining, power, full active-floor diffusion",
            runtime = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            width,
            height,
            cells = sim.World.Count,
            activeAtmosphereCells = sim.World.Floors.Count,
            agents,
            ticks,
            actualTick = sim.Tick,
            totalMilliseconds = Math.Round(elapsed, 2),
            meanTickMilliseconds = Math.Round(elapsed / ticks, 3),
            p95TickMilliseconds = Math.Round(timings[(int)((ticks - 1) * .95)], 3),
            allocatedBytes = allocated,
            allocatedBytesPerTick = allocated / ticks,
            pathSearches = sim.Router.Searches - pathsBefore,
            completedWork = sim.Crew.Sum(c => c.CompletedWork),
            jobsRemaining = sim.Jobs.Count,
            living = sim.LivingCount,
            managedMemoryBytes = GC.GetTotalMemory(false)
        }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    if (values.TryGetValue("--export-generation", out string? exportPath))
    {
        int seed = Number("--seed", 42), samples = Number("--samples", 4);
        GenerationGallery.Write(exportPath, seed, samples);
        Console.WriteLine(JsonSerializer.Serialize(new { export = Path.GetFullPath(exportPath), seed, samples, generatorVersion = LunarGenerator.CurrentVersion }));
        return 0;
    }
    if (values.ContainsKey("--validate-seeds"))
    {
        int count = Number("--validate-seeds", 1000), first = Number("--seed", -500), fallback = 0;
        if (count is < 1 or > 10000) throw new ArgumentException("Validate 1-10000 seeds.");
        var timer = Stopwatch.StartNew();
        for (int n = 0; n < count; n++) foreach (bool structure in new[] { false, true })
        {
            int seed = unchecked(first + n);
            var zone = LunarGenerator.Create(structure ? "below" : "surface", seed, structure, false);
            var validity = LunarGenerator.Validate(zone);
            if (!validity.Valid) throw new InvalidDataException($"Seed {seed}: {string.Join("; ", validity.Errors)}");
            if (zone.Generation.UsedFallback) fallback++;
        }
        Console.WriteLine(JsonSerializer.Serialize(new { validatedSeeds = count, firstSeed = first, sites = count * 2, fallback, milliseconds = timer.Elapsed.TotalMilliseconds }));
        return 0;
    }
    if (switches.Contains("--generation-report"))
    {
        int seed = Number("--seed", 42);
        var geology = new World(96, 64); geology.GenerateLunar(seed);
        Console.WriteLine(JsonSerializer.Serialize(new { seed, generatorVersion = LunarGenerator.CurrentVersion,
            colony = new { geology.GenerationVersion, geology.GeologyProfile, terrain = geology.Tiles.GroupBy(t => t).ToDictionary(t => t.Key.ToString(), t => t.Count()) }, sites = new[] { false, true }.Select(structure => {
            var zone = LunarGenerator.Create(structure ? "below" : "surface", seed, structure, false);
            return new { zone.Id, zone.Width, zone.Height, zone.Generation, validation = LunarGenerator.Validate(zone),
                terrain = zone.Tiles.GroupBy(g => g).ToDictionary(g => g.Key.ToString(), g => g.Count()) };
        }) }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    if (switches.Contains("--campaign-benchmark"))
    {
        int count = Number("--samples", 32), first = Number("--seed", 0);
        if (count is < 1 or > 1000) throw new ArgumentException("Use 1-1000 samples.");
        CampaignScenarios.FirstLoop(new IntegratedCampaign(first)); // Warm up runtime and generation.
        long bytes = GC.GetTotalAllocatedBytes(true); var watch = Stopwatch.StartNew(); long ticks = 0;
        for (int i = 0; i < count; i++)
        {
            var game = new IntegratedCampaign(unchecked(first + i)); CampaignScenarios.FirstLoop(game);
            ticks += game.CampaignTick;
            string snapshot = game.StateJson();
            if (IntegratedCampaign.FromJson(snapshot).StateJson() != snapshot) throw new InvalidDataException("Campaign continuation differs.");
        }
        Console.WriteLine(JsonSerializer.Serialize(new { scenario = "real solo rescue, home recovery, salvage combat, returned cargo, constructed powered suit service, saved continuation",
            samples = count, firstSeed = first, totalTicks = ticks, meanLoopMilliseconds = watch.Elapsed.TotalMilliseconds / count,
            elapsedMilliseconds = watch.Elapsed.TotalMilliseconds, allocatedBytes = GC.GetTotalAllocatedBytes(true) - bytes,
            completedRescues = count, constructedUpgrades = count, runtime = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    if (switches.Contains("--story-check"))
    {
        Console.WriteLine(JsonSerializer.Serialize(StationScenario.Run().Summary(), new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    int steps = Number("--headless", Number("--ticks", 0));
    if (steps is < 0 or > 1000000) throw new ArgumentException("Ticks must be between 0 and 1000000.");
    if (switches.Contains("--legacy-colony"))
    {
        var colony = switches.Contains("--load") ? Simulation.Load(values.GetValueOrDefault("--save", Simulation.DefaultSave)) :
            new Simulation(Number("--seed", 42), Number("--width", 96), Number("--height", 64));
        if (switches.Contains("--demo")) colony.DemoOrders();
        colony.Step(steps);
        Console.WriteLine(JsonSerializer.Serialize(colony.Summary(), new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    var campaign = switches.Contains("--load") ? IntegratedCampaign.Load(values.GetValueOrDefault("--save", IntegratedCampaign.DefaultSave)) :
        new IntegratedCampaign(Number("--seed", 42), Number("--width", 96), Number("--height", 64));
    if (switches.Contains("--demo") || switches.Contains("--headless") && !switches.Contains("--load")) CampaignScenarios.FirstLoop(campaign);
    while (steps > 0) { int batch = Math.Min(10000, steps); CampaignScenarios.Require(campaign.StepColony(batch)); steps -= batch; }
    Console.WriteLine(JsonSerializer.Serialize(campaign.Summary(), new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception error) when (error is ArgumentException or FormatException or IOException or JsonException or OverflowException)
{
    Console.Error.WriteLine("Ithica: " + error.Message);
    return 1;
}
