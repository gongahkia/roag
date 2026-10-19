# Performance measurements

Collected on 2026-10-18 in WSL2 on an Intel Core i7-1355U, using Mesa 26.2.4 **llvmpipe software OpenGL**, LLVM 22.1.8. These measurements do not establish performance or physical VRAM use on a real GPU. Odin is pinned to `dev-2026-10` (`84bc3fc`).

## Structural optimization comparison

Both normal scenes use seed 42, the Tiny profile, a fixed camera, radius four, 960×540, and ten-second runs. The unmerged baseline uses `--naive-mesh`; all other terrain/lighting rules match. Both populated all 392 chunks and had zero memory-pressure events.

| Measurement | Greedy meshing | Unmerged baseline |
|---|---:|---:|
| Peak allocated mesh buffers | 14.10 MiB | 21.24 MiB |
| Peak graphics reservation | 24.01 MiB | 31.15 MiB |
| Mean submitted triangles after warmup | 249,833 | 367,537 |
| Mean frame time | 35.68 ms | 25.58 ms |
| Frame time p95 | 47.26 ms | 33.12 ms |

Greedy meshing reduced peak mesh allocation by **33.6%** in this scene. Timings are single runs under a software renderer and uncontrolled system load; the latest greedy run had higher frame times. These data support the memory/geometry improvement, **not a frame-rate speedup claim**. Repeat warmed runs on a reference physical GPU before assessing the 60 FPS goal.

## Streaming and stress checks

- A 52-second flight repeated the same out-and-back route twice. Sampled CPU peaks were 1.47 MiB and 1.41 MiB; the residency-growth check passed. Allocator high-water tracking reported 1.50 MiB across the run. Its console recorded 1,014 GPU buffer reuses.
- Flight peak mesh allocation was 22.48 MiB, with 32.39 MiB total graphics reservation. Terrain data and queued work stayed bounded while moving across thousands of generated chunks.
- The pressure smoke test used a 16 MiB graphics allowance, 32 MiB managed CPU budget, 640×360 framebuffer, radius four, and a dense checkerboard edit. It ended at radius 2 after 2 pressure events, with a 9.99 MiB mesh peak. Each upload stayed at or below 65,536 bytes.
- The checkerboard unit test verifies complete coverage of 98,304 quads in one chunk and bounded mesh capacity. The graphical dense scene also ran with a 64 KiB upload ceiling; no chunk overflow was reported.
- Eleven core tests pass, including exact unit-face coverage, winding, fixed terrain fixtures, snapshot isolation, negative coordinates, stale jobs, corrupt/duplicate save payloads, and repeated shutdown without tracked leaks.
- Graphical smoke checks pass for actual keyboard/mouse editing, F5 saving while the process is alive, save reload, preservation of invalid saves, capture release, resizing under a small budget, and clean exit. Low and Standard profiles also completed startup runs; those brief runs are not full-residency performance measurements.

## Reproduce

```sh
python3 scripts/build.py test
python3 scripts/smoke.py
python3 scripts/benchmark.py --scene normal --seconds 10
python3 scripts/benchmark.py --scene normal --seconds 10 --naive
python3 scripts/benchmark.py --scene flight --seconds 52
python3 scripts/benchmark.py --scene edit --seconds 8
python3 scripts/benchmark.py --scene dense --seconds 8 --upload-kb 64
```

The benchmark runner writes CSV and JSON under ignored `benchmarks/`. It checks sampled managed CPU use, graphics reservations, per-frame upload sizes, and repeated-route residency. The JSON includes command settings, compiler version, driver output, timings, and both sampled CPU use and allocator high-water use where available. The first three seconds are excluded from timing summaries. GPU query timings are asynchronous. Screenshot capture uses only one scanline of scratch space.

Current runs time completed frames, including buffer swaps and cap waits; an optional final screenshot is also included in that frame's interval. Historical snapshots above predate this timing boundary change. See [settings and measured FPS](settings.md) for display detection, frame caps, additional CSV fields, and the distinction between measured rendering FPS and monitor refresh Hz.

[Recorded report snapshots](measurements.json) preserve the measured data; [architecture](architecture.md) explains accounting boundaries and failure behavior. These reports were collected from the implementation working tree during development.

## Outstanding hardware validation

The ore/fluid validation snapshot records 27 passing tests and the full Linux graphical smoke suite. Its six-second fluid scene at 640×360 stays within a 16 MiB application graphics allowance and 32 MiB managed CPU budget, with 64 KiB uploads per frame. The recorded `fluids-smoke` report includes actual fluid work, queue saturation counters, memory accounting, and timings. Sources propagate and cool lava during the run, and the graphical input check places ore and water from the palette.

At the player-controller checkpoint, 34 tests and the expanded Linux graphical smoke suite passed, including floor/wall collisions, sliding, jumping and landing, rejection of body-intersecting placement, and spectator toggling through the ground. Physics uses fixed 120 Hz steps and direct world queries, with no per-frame collision allocations. macOS arm64 checking and Windows amd64 object compilation also passed; native graphical runs remain outstanding.

- Establish a physical 128–256 MB GPU reference and measure total process RAM/driver VRAM externally. Application counters exclude runtime/driver overhead and deferred driver allocations.
- Measure the Low profile at 720p against the **60 FPS target**, using warmed, repeated routes and frame-time percentiles. This target is not verified here.
- Execute native Windows and macOS graphical runs. Native CI build/core-test jobs are configured but have not been run from this workspace. Local Linux execution and an Apple Silicon macOS compile check passed; Windows cross-checking here needs a Windows SDK.

## Generator 3 validation — 2026-10-19

The final implementation passes **43 core tests** and the complete Linux graphical smoke suite. Coverage includes 256 spawn seeds (all found natural dry sites), a separately tested spawn fallback, 681 shared cave ports, lake containment and wall mining, generated-lake swimming, island collisions, concurrent workers, exact padded sampling, formation edges, golden block hashes, and legacy world versions. macOS arm64 checking and Windows amd64 object compilation pass. Native Windows/macOS runtime agreement remains pending CI; the same golden hashes are asserted by each native CI test job.

Two eight-second, fixed-camera runs per version at 640×360, radius two, one worker, 16 MiB application graphics, 32 MiB managed CPU, and 64 KiB uploads measured these completed-job averages:

| Measurement | Generator 2 | Generator 3 |
|---|---:|---:|
| Mean generation time per chunk | 0.94–0.95 ms | 2.65–2.85 ms |
| Mean meshing time per chunk | 1.15–1.20 ms | 1.82–1.88 ms |
| Completed chunks per run | 104 | 104 |
| Peak mesh allocation | 3.92 MiB | 2.63 MiB |

Generator 3 spends more CPU time producing its additional structure. The versions create different geometry at the same camera coordinates, so the mesh-size difference and frame timings are **not a rendering-speedup claim**. These are local WSL2/llvmpipe observations under uncontrolled host load, not physical GPU measurements. Job totals exclude canceled/stale work.

The new surface, floating-island, and cave smoke scenes all retain radius two under the 16/32 MiB budgets. Their graphics reservation peaks are 8.15, 6.99, and 7.96 MiB respectively; managed CPU peaks are below 1.54 MiB. Uploads never exceed 65,536 bytes, and none of these scenes overflow or refuse fluid work. The original checkerboard pressure fixture explicitly retains generator 2 and runs for ten seconds, ensuring it reaches eviction even under variable software-renderer load; it reduces radius four to two after two pressure events.

A 52-second generator-3 flight completes 2,492 chunk jobs. Both 24-second route cycles peak at 1.476 MiB sampled managed CPU use, and the residency plateau check passes. The complete path now repeats, including its sideways motion. Seed 42's origin flight is mostly ocean/lowland; complex regions are exercised by the separate surface/island/cave and landmark scenes.

Ten wider 960×540, radius-four previews use a 64 MiB graphics/CPU allowance and retain the 64 KiB upload ceiling. They show mountains, plains, forest, desert, coast, arches, overhangs, spires, sky islands, and underground lakes. Local images are `build/terrain-v3-{scene}.png`; [terrain documentation](terrain.md) lists the reproducible scene commands. The `terrain_v3` entry in [measurements](measurements.json) records the final commands, driver output, comparisons, flight, stress scenes, and preview reports.

Process RSS includes the software renderer and driver allocations and is much larger than the engine's managed heap. Application graphics reservations likewise are not physical VRAM readings. Physical low-memory GPU validation remains outstanding.

## Generator 4 biome validation — 2026-10-19

The biome expansion passes **51 core tests** and the complete Linux graphical/input smoke suite. The v4 tests cover 17 biome-specific padded chunk hashes, worker/query agreement for every biome, tall canopy boundaries, all nine vegetation shapes with face-connected blocks, cold ice over water, extreme coordinates, concurrent workers, six-bit material packing, five palette pages, and material save/load across generators 1–4. All 128 v4 spawn seeds found natural dry sites; v3's 256-seed and original golden checks still pass. Native macOS/Windows runtime tests remain pending; local macOS arm64 checking and Windows amd64 object compilation pass.

Ten five-second fixed biome scenes at 640×360, radius two, one worker, 16 MiB application graphics, 32 MiB managed CPU, and a 64 KiB upload ceiling all complete 104 chunk jobs without reducing radius. Their graphics reservations peak at **6.69–8.28 MiB**; sampled managed heap peaks at **1.49–1.61 MiB**. Uploads remain at or below 65,536 bytes per frame, and no scene refuses or overflows fluid work. Completed-job generation averages range from 1.51–3.44 ms and meshing averages from 0.93–2.15 ms, depending on terrain and vegetation. These are different scenes, so the range does not measure a speedup over v3.

The texture array grows to 75 small layers, approximately 100 KiB with mipmaps. Vertices still occupy eight bytes; ice reuses the water blending pass. Climate and vegetation data stay in bounded CPU descriptors/caches, with no persistent dense biome grids or GPU climate textures. Process RSS in these software-rendered scenes reaches roughly 107–116 MiB, including allocations outside engine accounting. These results do not establish physical GPU memory use or performance on low-memory hardware.

A final 52-second v4 flight completes 2,479 chunk jobs. Both repeated route cycles peak at 1.548 MiB sampled managed heap, with no residency-growth failure or radius reduction. The route is mostly ocean/lowland; the separate biome scenes exercise dense vegetation and peaks. Twelve wider 960×540, radius-four previews use 64 MiB graphics/CPU allowances and retain the 64 KiB upload ceiling. They show all ten new biomes plus the retained floating islands and underground lakes. Final commands, renderer output, and summaries are recorded in the `terrain_v4` section of [measurements](measurements.json).
