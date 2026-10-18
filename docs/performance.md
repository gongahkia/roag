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

The current player controller passes 34 tests and the expanded Linux graphical smoke suite, including floor/wall collisions, sliding, jumping and landing, rejection of body-intersecting placement, and spectator toggling through the ground. Physics uses fixed 120 Hz steps and direct world queries, with no per-frame collision allocations. macOS arm64 checking and Windows amd64 object compilation also pass; native graphical runs remain outstanding.

- Establish a physical 128–256 MB GPU reference and measure total process RAM/driver VRAM externally. Application counters exclude runtime/driver overhead and deferred driver allocations.
- Measure the Low profile at 720p against the **60 FPS target**, using warmed, repeated routes and frame-time percentiles. This target is not verified here.
- Execute native Windows and macOS graphical runs. Native CI build/core-test jobs are configured but have not been run from this workspace. Local Linux execution and an Apple Silicon macOS compile check passed; Windows cross-checking here needs a Windows SDK.
