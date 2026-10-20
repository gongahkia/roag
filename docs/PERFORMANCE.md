# Ithica colony simulation performance

These measurements cover the current colony prototype under its legacy Regolith project names. They do not measure the planned combined campaign. See the [Ithica integration plan](ITHICA_PLAN.md#target-architecture) for the shared clock and additional workload to measure during integration.

The C# core uses contiguous cell arrays, reusable atmosphere/BFS buffers, spatial work buckets, and cached power connectivity. The client renders visible map cells and advances fixed simulation ticks independently of draw frequency. These choices make a useful starting point; this project does not yet demonstrate the full workload of Dwarf Fortress or Songs of Syx.

## Measured baseline

Release measurements on the development Mac, 20 October 2026: **Intel Core i5-8279U @ 2.40 GHz**, macOS 15.7.7, x64, .NET 10.0.13 / SDK 10.0.402. Each run warms up 16 ticks, collects garbage, then measures **120 ticks**. Results are individual local samples, not a statistical benchmark campaign. The raw output is retained in [performance-baseline.json](performance-baseline.json).

| World | Agents | Active atmosphere cells at finish | Mean tick | p95 tick | Allocated / tick | Mining completed / queued |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 256 × 256 | 1,000 | 64,516 | 4.094 ms | 10.988 ms | 1,827 B | 1,000 / 1,000 |
| 512 × 512 | 10,000 | 257,315 | 15.666 ms | 16.731 ms | 8,505 B | 7,215 / 10,000 |
| 1024 × 1024 | 10,000 | 1,041,699 | 64.465 ms | 65.901 ms | 8,423 B | 7,215 / 10,000 |

The scenario fills the map with one sealed habitat and distributes agents on a three-cell grid, each with one adjacent mining order. It includes live hunger/energy/suit/oxygen needs, staggered work assignment, BFS requests, mining, power, and almost entirely active floor diffusion. It excludes hazards and incoming survivors, supplies abundant shared resources, and permits overlapping people. After finishing their one mining job, agents use local idle patrols. Therefore it is neither a no-op counter loop nor a sustained production-city benchmark. Agents and jobs have already progressed during the 16 warm-up ticks; completion totals include that warm-up.

At 10,000 agents, 2,785 jobs remain after the measured interval because routing/assignment is limited to 64 requests per tick. Low frame/tick time is only useful if work also makes progress; the tool reports both. A normal lunar map contains far less deck than these dense stress maps, so it benefits from active-floor atmosphere updates. These results do not measure long-distance rescues, disconnected-job worst cases, mass fires, farming across thousands of machines, crowding, GPU frame rate, or save times.

The reported memory is managed .NET memory; it excludes native graphics/driver memory and is not process RSS. The atmosphere kernel itself allocates zero managed bytes in the dedicated repeated-update test; the full simulation still allocates small paths, notices, and bookkeeping data.

## Reproduce

Use `./tools/dotnet.sh` instead of `dotnet` if the SDK is installed locally through the setup script. Run samples sequentially to avoid CPU contention:

```sh
dotnet build Regolith.sln --configuration Release
dotnet run --project src/Regolith.Tools --configuration Release --no-build -- --benchmark --width 256 --height 256 --agents 1000 --ticks 120
dotnet run --project src/Regolith.Tools --configuration Release --no-build -- --benchmark --width 512 --height 512 --agents 10000 --ticks 120
dotnet run --project src/Regolith.Tools --configuration Release --no-build -- --benchmark --width 1024 --height 1024 --agents 10000 --ticks 120
```

`./play.sh --benchmark ...` selects the same tool and builds as needed. Output includes dimensions, runtime, tick timing, allocations, paths, completion counts, living agents, and managed memory. Increasing `--ticks` changes the work/idle mixture; comparisons should use the same arguments. Startup/JIT, rendering, and serialization are outside the timed loop.

## Current constraints and next measurements

The simulation is synchronous on the main thread. The client budgets 12 ms for starting more ticks in a frame, but a single long tick cannot be interrupted. A 64 ms stress tick can cause visible hitches even if the requested simulation rate averages eight ticks per second. The accumulator slows wall-clock progress under overload. A 60 FPS claim for large colonies would require separate native frame-time measurements and likely a simulation worker with render snapshots.

The request budget does not bound each BFS expansion count. Inaccessible jobs or distant emergency routes may scan a whole world, and manual rescue dispatch evaluates ready crew on the UI thread. Job buckets reduce common searches but can still scan many empty buckets. Safe-floor lists and full network rebuilds also scale with habitat size. Many farms/beds, deaths, and social features would introduce additional agent scans. World size is capped at 2048 per side and stored as one dense layer; there is no chunk streaming.

Before promising a population target, measure a sustained colony with repeated build/mining cycles, widely separated sites, unreachable work, many farms and power islands, multiple rescues, breaches, and fires. Track time to complete jobs, route latency/fairness, p95/p99 frame time, memory, and save duration over thousands of ticks. Routing regions/expansion budgets, incremental connectivity, maintained aggregate statistics, and a worker-thread boundary should follow evidence from those workloads.
