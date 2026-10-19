using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Regolith.Core;

var values = new Dictionary<string, string>();
var switches = new HashSet<string>();
try
{
    for (int i = 0; i < args.Length; i++)
    {
        string key = args[i];
        if (key is "--demo" or "--load" or "--benchmark" or "--help")
        {
            switches.Add(key);
            continue;
        }
        if (key is not ("--seed" or "--width" or "--height" or "--headless" or "--ticks" or "--agents" or "--save") || i + 1 >= args.Length)
        {
            throw new ArgumentException("Unknown or incomplete option: " + key);
        }

        values[key] = args[++i];
    }
    int Number(string key, int fallback) => values.TryGetValue(key, out string? value) ? int.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : fallback;
    if (switches.Contains("--help"))
    {
        Console.WriteLine("Regolith simulation tools\n  --headless TICKS [--demo] [--seed N] [--width N --height N]\n  --benchmark [--agents N] [--ticks N] [--width N --height N]\n  --load --save PATH\nThe simulation tools never open a graphics window or write a save.");
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
    int steps = Number("--headless", Number("--ticks", 480));
    if (steps < 0)
    {
        throw new ArgumentException("Ticks must be nonnegative.");
    }

    var colony = switches.Contains("--load") ? Simulation.Load(values.GetValueOrDefault("--save", Simulation.DefaultSave)) :
        new Simulation(Number("--seed", 42), Number("--width", 96), Number("--height", 64));
    if (switches.Contains("--demo"))
    {
        colony.DemoOrders();
    }

    colony.Step(steps);
    Console.WriteLine(JsonSerializer.Serialize(colony.Summary(), new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception error) when (error is ArgumentException or FormatException or IOException or JsonException or OverflowException)
{
    Console.Error.WriteLine("Regolith: " + error.Message);
    return 1;
}
