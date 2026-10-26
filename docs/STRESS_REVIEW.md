# Living-world stress review

The full expansion remains in progress. This records executed stress evidence and the remaining acceptance work, alongside [LIVING_WORLD.md](LIVING_WORLD.md) and [VERIFICATION.md](VERIFICATION.md).

## Hundred-day comparisons

Rules commit `119f1b4`, source `3970f59c7cf623ba2d9c631d9dca8c9dd9aa8fdb57449abd5732649cc597cc0d`. Three independent headless processes each executed **720,000 actual ticks**, comparing scheduled bodies with full cadence. Each passed **2,403** complete-state/accounting checkpoints, exact midpoint continuation and real save/load at days **0, 7, 30, 50, 60, 90, 100**. No logical-clock jumps replace elapsed days. All seasons through winter are reached.

| Seed | Final actors / unique IDs | Peak sampled resident chunks | Final referenced/on-disk save bytes | Final retained managed process bytes | Scheduled tick p99 upper bound / worst |
| --- | ---: | ---: | ---: | ---: | ---: |
| 42 | 611 / 611 | 20 | 1,166,301 | 28,990,120 | 2ms / 43.3965ms |
| 1337 | 615 / 615 | 21 | 1,168,950 | 29,172,216 | 2ms / 52.4771ms |
| -42 | 618 / 618 | 17 | 1,173,010 | 28,568,016 | 2ms / 36.6848ms |

All growth snapshots retain **zero obsolete chunk snapshots**. Terrain/footprint caches, body deadlines, route steps, actor memory, journal, weather/habitat read caches and saved habitat counts satisfy the asserted bounds. Journal history reaches its 500-entry cap; actor identity counts remain constant. The stationary-observer fixture activates six physical workers per seed. It does not materialize wildlife populations or run speculative terrain preparation, so its zero population/history/preparation counts do not prove those workloads.

Scheduled body decisions total **52,453,595 /47,507,506 /54,833,140**, versus **439,919,990 /442,799,990 /444,959,990** for full cadence. No measured tick call exceeds 200ms. Scheduled calls allocate **14,283,277,600 /14,198,555,192 /14,727,566,536 bytes** across the hundred days. Process GC counts are **[5139,1208,545] /[5113,1103,546] /[5293,1394,552]**. These include both worlds, serialization, checkpoint loaders, diagnostic data and forced checkpoint collections; they are not client-only GC/memory or FPS estimates. Tick-call allocations exclude assertions and snapshots; timing remains subject to JIT/host scheduling.

Local artifacts: `artifacts/performance-review/hundred-day-source.json` pins source and assembly hashes; `hundred-day-42.json`, `hundred-day-1337.json` and `hundred-day--42.json` retain the complete reports. Logs are `artifacts/hundred-day-{seed}.log`. Reproduce with `./dev audit-simulation --seeds 42,1337,-42 --steps 720000 --output artifacts/hundred-day.json`.

## Corrected food-search results

Rules `3d3d73d`, source `4fbbef75f38a91bca06d49eeb761e7a53866fe3da3caa67a82c5717f0c3c5edc`, now pass three further **720,000-tick** comparisons and **7,209** complete-state/accounting checkpoints, every growth save and exact midpoint continuation. All three handles were observed terminal with exit zero. Immutable Tools/Core/content copies and checksums are pinned by `artifacts/performance-review/forage-hundred-source.json`; reports are `forage-hundred-day-{seed}.json` under that directory.

| Seed | Final starving people, before → after | Save bytes (referenced = disk) | Retained managed process bytes | Scheduled p99 / worst / >200ms calls |
| --- | ---: | ---: | ---: | ---: |
| 42 | 4 → 1 | 1,171,651 | 28,979,896 | ≤2ms / 81.159ms / 0 |
| 1337 | 1 → 1 | 1,165,965 | 28,802,672 | ≤2ms / 200.8637ms / 1 |
| -42 | 9 → 1 | 1,184,890 | 28,623,352 | ≤2ms / 112.0592ms / 0 |

All growth saves retain zero obsolete snapshots; all asserted state/cache/history bounds hold. Actor identities remain 611/615/618. Seed -42 now observes two wildlife populations; this still does not exercise repeated hunting/recovery/history pressure. Separate actual hundred-day identity probes for 42/-42 match these reports' complete final hashes and identify their sole remaining starver as the idle, finite-pack-food hero **100000**, with no starving NPCs. Probes are `artifacts/forage-hundred-starvers-{seed}.jsonl`. The earlier seed-1337 probe identified its sole starver as the idle hero; the corrected report retains one starving person. Empty towns remain 1/1/2; this does not promise that every settlement always has surplus food.

The single >200ms call is retained. Runs overlap compilation and other headless workloads; workload and allocations change after the behavior fix, so timings are observations rather than a controlled speed comparison. Process memory/GC include both worlds and audit overhead. No GUI or player saves were used.

## Balance and remaining scope

At day 100, seeds 42/1337/-42 report **4/1/9 starving people** and **1/0/2 empty-food towns**. The fixture leaves the hero idle with finite pack food; a separate seven-day seed-1337 probe identifies that seed's sole starving person as the hero. Further actual hundred-day probes in `artifacts/hundred-day-starvation-42.jsonl` and `hundred-day-starvation--42.jsonl` match the comparison runs' complete final hashes. In seed -42, all eight original town-zero residents remain in food routines from roughly day 44 onward. Their cleared chunk has **capacity one**, food one and pressure zero, so it can never supply the required two-food gather. They keep targeting their current position rather than seeking another viable habitat. This is a real routine/food-search defect to fix; matching state and conserved resources do not establish healthy resident behavior.

The three-seed `audit-crowded-work` now passes the maximum four-person party, twelve downed blockers and **64 actual excavation orders**. All cases preserve regional resources every tick, extract/deliver exactly **320 stone**, hold progress while suspended, and retain exact blocked/walking/work/hauling/completed saves against full cadence. Scripted cleared terrain/background rest/gate and blocker removal isolate the workload. Completion ticks are **3,582 /674 /580**; retained comparison checkpoints **144 /27 /24**. Tick p99 upper bounds are **8/16/32ms**, with zero >200ms calls, in the concurrent local headless run. Full reports are `artifacts/performance-review/crowded-work-{seed}.json`. This verifies dense work and interruption ownership, with controlled supplies; it is not a survival balance scenario.

The measured food-search defect now has a bounded neighboring-habitat planner and actual arrival-before-gathering checks. All 990 Core tests pass, including 74 focused forage/routine/journey/habitat cases. The corrected-rule results above verify improved outcomes in the reproduced trap without weakening conservation or inventing food. The standard world profile also passes.

Still required: exercise active ecology over elapsed time; actual moving-observer chunk/floor journeys with bounded preparation and native review; then fresh full verification and the original requirement-by-requirement completion audit.
