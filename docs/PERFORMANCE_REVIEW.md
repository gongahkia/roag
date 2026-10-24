# Performance review — 2026-10-24

The review covers ordinary exploration, streamed terrain/resource inspection, timed workers, hostile contact, combat inspection/rendering and session diagnostics. Changes preserve gameplay content hashes, save/generation versions, combat rules, actor order and deterministic saved continuation.

## Recorded evidence

The latest closed ordinary session, `20261024-144000-11de8731`, recorded 18,165 frames, eight frames over 50 ms and a worst frame of 419.7 ms. Across its 56 performance windows, median frame p95 was 31.9 ms while median update p95 was 0.46 ms. These are historical frame/CPU observations, not proof of a GPU bottleneck. Ten chunk generations totaled 124.3 ms; the slowest was 92.8 ms. The local aggregate is retained in `artifacts/performance-review/session-summary.json`; player saves were not modified.

Instrumentation now separates update, render and terrain wall time, main-thread frame allocations and graphics submissions. `./dev benchmark` supplies warmed Core workloads; `./dev benchmark-render` uses isolated stationary native scenes without paid model calls. Both commands retain workload identity, and output paths cannot overwrite earlier evidence accidentally.

## Implemented changes

- Terrain/resource inspection shares terrain reads, reuses immutable decorations and indexes stable resource candidates/salts by terrain, depth and biome. Edits, ownership, harvested stock and regeneration are still checked live; there is no stale per-location cache.
- Blocked route destinations return immediately. Worker assignment skips distance fields for adjacent workers; approach selection inspects the interaction neighbourhood with the same distance/Y/X ordering.
- Body/work/expiry and hostile-contact scans avoid closures per actor. First-contact order, downed companions and wildlife notice/leash behavior are retained.
- Combat query/draw passes share a current ability list. Only hovered fighters need full action previews; unrelated disabled controls no longer enumerate equipment. Class definitions are resolved once per session, and pointer hits scan the existing control list in reverse order without copying it. Authoritative actions resolve fresh state; ability lists do not persist between changing frames.

## Core comparison

Baseline `cced8ca` and the optimized build use seeds 42/1337, 50 samples, the same warmup/repetition counts and `DOTNET_TieredCompilation=0` for both processes. Setup/world creation is excluded. One visible-cell operation inspects 960 cells; one work operation advances a real simulation tick beside a long-running dig order. Fixtures use isolated generated worlds, including a deliberately durable job; they do not load a player slot.

| Seed | Workload | Bytes before/op | Bytes after/op | Median ms before → after |
| --- | --- | ---: | ---: | ---: |
| 42 | visible-cell-queries | 48,096 | 0 | 0.4867 → 0.1905 |
| 42 | blocked-click-route | 68,624 | 0 | 0.6467 → <0.0001 |
| 42 | adjacent-work-tick | 357,386 | 23,994 | 0.5966 → 0.2293 |
| 1337 | visible-cell-queries | 46,800 | 0 | 0.4160 → 0.2054 |
| 1337 | blocked-click-route | 65,888 | 0 | 0.3963 → <0.0001 |
| 1337 | adjacent-work-tick | 351,310 | 14,668 | 0.4605 → 0.2190 |

Inspection checksums and complete final simulation hashes match the baseline for both seeds. Core reports are `artifacts/performance-review/comparable-before-*.json` and `final-core-*.json`. The allocation regression uses generous bounds, with no machine-specific timing assertions. Earlier tiered-JIT measurements are retained separately and are not mixed into this table.

## Render comparison and acceptance

Final paired render measurements, graphical inspection and managed acceptance are recorded in [VERIFICATION.md](VERIFICATION.md). Benchmarks use explicit paused scenes at 1024×720, Xvfb, Mesa software rendering, dummy audio and `LP_NUM_THREADS=1` for both builds. Startup and partial reporting windows are discarded. These are warmed wall-clock/driver submission measurements, not GPU timestamps or a promise of production FPS. Initial comparisons overlapped builds and other machine load and remain labelled separately.

The retained paired baseline and final reports are `artifacts/performance-review/paired-before-render/report.json` and `final-render/report.json`; `comparison.json` joins the measurements. Observed results:

| Scene | Bytes/frame before → after | Allocation reduction | Frame p95 ms before → after | Draws/frame |
| --- | ---: | ---: | ---: | ---: |
| world | 35,725 → 19,503 | 45.4% | 30.39 → 15.88 | 36 |
| world-resources | 48,237 → 32,506 | 32.6% | 33.06 → 13.85 | 38 |
| world-battle | 2,000,370 → 33,364 | 98.3% | 40.26 → 15.72 | 99 |

The reduction in allocations is the clearest portable finding. Timing also improved in these local runs, but scheduling, software rendering and other machine load prevent assigning a guaranteed FPS gain to these numbers. Combat update p95 fell from 9.62 to 0.25 ms in this fixture. Body/contact work ticks use 93–96% fewer allocated bytes across the two seeded workloads.

Graphics submission counts remain a useful check on preserved layer order. Cold chunk generation is still synchronous; native Windows/macOS execution, actual GPU-driver profiling and long-session memory behavior are outside this local measurement.

## Commits

- `0b74118`: completed exploration clock, hover/path and work-animation fixes from the preceding request.
- `cced8ca`: reproducible Core/render measurements and recording diagnostics.
- `eb8aeb9`: terrain/resource, blocked-route and worker/body hot paths.
- `9f49de8`: combat UI inspection reuse and allocation guards.
- `cca440a`: ordered contact/wildlife scans without per-actor closures.

This review and final verification records form a separate documentation commit after acceptance.
