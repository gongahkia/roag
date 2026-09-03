# Desktop browser performance and storage baseline

Reviewed 2026-09-02. This is Phase 1.6's reproducible planning baseline for the current medieval foundation. It is not evidence that Jomon already supports a materialized map, large populations, trade, combat, a detailed renderer, or future persistence scale.

## Browser support policy

Jomon supports current stable desktop Chrome, Edge, Firefox, and Safari through standard browser APIs and feature detection. It must not use user-agent sniffing or vendor-only behavior. Chromium is the automated-browser baseline; the other browsers remain a manual verification matrix until cross-browser automation is explicitly added.

Required capabilities are ES modules, Canvas 2D, keyboard events and focus, IndexedDB with structured clone, Blob/object URLs for chronicle export, and the guarded font-loading path. A missing capability is a compatibility result, not a signal to use a browser-specific fallback. Dedicated workers, SharedWorkers, OPFS, `requestIdleCallback`, WebGPU, and vendor-specific APIs are not baseline requirements; the later optimization slice decides whether any are warranted.

`navigator.storage.estimate()` is advisory only. Its absence, error, private-browsing behavior, variable quota, or eviction behavior must never prevent play, force persistence, or cause application cleanup. Jomon never calls `navigator.storage.persist()` automatically.

The stable-channel references were reviewed on 2026-09-02: [Chrome release notes](https://support.google.com/chrome/a/answer/7679408?hl=en), [Edge Stable release notes](https://learn.microsoft.com/en-us/deployedge/microsoft-edge-relnote-stable-channel), [Firefox release notes](https://www.firefox.com/en-US/releases/), and [Safari release notes](https://developer.apple.com/documentation/safari-release-notes). Review the matrix and these links at least every six months and whenever browser automation or this benchmark changes; “current stable” is intentionally not frozen to the versions below.

| Browser/channel | Version observed locally | Phase 1.6 performance run | Manual scenario still required |
| --- | --- | --- | --- |
| Playwright Chromium (automated baseline) | 151.0.7922.34; Playwright 1.62.1 | Version probe only; no browser timing run | Create a watershed world, select a courier, inspect help/prompt/sidebar, remap/reset a key, save/reload, and confirm no external request or elapsed world time. |
| Google Chrome stable | 152.0.7977.66 installed | Not run | Run the same local-only scenario and record DevTools errors plus optional storage diagnostic if available. |
| Microsoft Edge stable | Unavailable on the measurement machine | Not run | Install the current stable channel; run the same local-only scenario and feature checks. |
| Firefox stable | Unavailable on the measurement machine | Not run | Install the current stable channel; run the same local-only scenario and feature checks. |
| Safari stable | Unavailable at the standard system application location | Not run | Run the same local-only scenario on the current stable Safari and record the exact version. |

No browser performance claim is made for an unrun entry. The prior Chromium correctness suite is separate from this Node benchmark.

## Measurement machine and method

The 2026-09-02 run used only non-identifying machine facts:

- MacBook Pro 15,2; quad-core 2.4 GHz Intel Core i5; 8 GB RAM.
- macOS 15.7.7 (Darwin 24.6.0), Node v24.19.0, npm 11.17.0, `tsx` 4.23.1.
- The local Chrome and Playwright Chromium versions appear in the matrix above. No Edge, Firefox, or Safari executable was available for this run.

Run `npm run benchmark:medieval-foundation`. The script uses real public medieval APIs but creates deterministic worlds only in memory. It has no network request, IndexedDB operation, filesystem write, user-save dependency, or world mutation outside its temporary objects. It reports human-readable lines and one `RESULT_JSON` object on stdout.

For each fixture/operation it performs two warm-ups, then nine measured samples. It uses Node `performance.now()` solely as a benchmark wall-clock source. p50 and p95 are linear interpolation over the sorted sample set; min and max are also reported. Timing is deliberately not asserted in Vitest or CI because hardware, thermal state, and browser scheduling vary.

The benchmark measures real foundation generation, initial-courier selection, terminal/sidebar/deferred-adapter pure projections, one valid 240-minute `wait` action, and deterministic UTF-8 canonical JSON byte sizes. It does not fabricate a map, large population, trade, combat, detailed rendering, or a real save/load timing. Browser-ready/focus, IndexedDB save/load, and multi-world/chronicle measurements below are future browser targets for the persistence/fixture slices.

## Present deterministic fixtures and scale

The source of truth is `src/medieval/performance-budget.ts` v1. Each fixture uses the real `WorldGenerationConfig` resolver and is intentionally a current-foundation specimen, not a future capacity claim. Abbreviations below are terrain/waterway/settlement/population/political/scarcity/ecology/danger scales.

| Fixture / deterministic seed | Resolved configuration | Measured initial records | Persistent people |
| --- | --- | --- | --- |
| `sheltered-reach-focused` / `performance-sheltered-reach` | compact, 200 years, temperate; 2/4/3/2/2/2/3/2; measured era; focused fidelity | 6 waterways, 6 resources, 4 settlements, 8 institutions, 16 generated person seeds, 3 routes | 6 instantiated Jomon crew |
| `watershed-balanced` / `performance-watershed` | standard, 300 years, temperate; 3/4/3/3/3/3/3/3; measured era; balanced fidelity | 6 waterways, 5 resources, 4 settlements, 12 institutions, 24 generated person seeds, 3 routes | 6 instantiated Jomon crew |
| `far-coast-deep` / `performance-far-coast` | broad, 400 years, cool-wet; 4/3/3/3/4/4/4/4; brisk era; deep fidelity | 5 waterways, 4 resources, 4 settlements, 16 institutions, 28 generated person seeds, 3 routes | 6 instantiated Jomon crew |

Initial-world deterministic caps are: 7 waterways, 4 seasons, 7 resources, 5 ecologies, 6 settlements, 30 institutions, 60 generated person seeds, 5 routes, 5 trade links, 5 route hazards, and 8 history events. A generated person seed is history/materialization input, not a mutable persistent person. The only instantiated people at this stage are the six Jomon crew.

| Fidelity | Loaded places / people | Nearby / recurring | Distant individual | Loaded institutions | Distant settlement / institution |
| --- | ---: | ---: | ---: | ---: | ---: |
| focused | 2 / 2 | 1 / 1 | 1 | 2 | 2 / 2 |
| balanced | 4 / 3 | 2 / 2 | 4 | 4 | 4 / 4 |
| deep | 8 / 4 | 3 / 3 | 8 | 8 | 8 / 8 |

Current scheduler cadence is one minute for loaded people/places/institutions, five for nearby people, 30 for recurring people, 120 for distant individual summaries, and 240 for distant settlement/institution summaries. Deferred and historical-only entries are not scheduled. The benchmark's one action is a bounded 240-minute wait specifically to cross every current cadence family without implying real travel or player gameplay.

The future storage measurement shape is deliberately small: three independently valid active-world records plus up to four read-only chronicle records. It is a target shape only; no chronicle fixture is fabricated in this benchmark because creation of a chronicle requires real world-finalization authority and no loss/recovery scenario is being benchmarked here.

## 2026-09-02 Node baseline results

All values are milliseconds, p50 / p95, from the documented two warm-ups and nine measured samples. They are observed measurements, not CI limits and not browser responsiveness claims.

| Operation | Sheltered Reach | Watershed | Far Coast |
| --- | ---: | ---: | ---: |
| Foundation world generation | 18.760 / 19.385 | 18.464 / 22.562 | 23.778 / 35.391 |
| Initial courier selection | 69.884 / 77.319 | 76.014 / 86.907 | 184.879 / 274.350 |
| Terminal presentation projection | 106.191 / 115.637 | 112.975 / 117.867 | 135.323 / 182.992 |
| Management sidebar projection | 104.463 / 105.715 | 142.943 / 169.592 | 119.434 / 141.255 |
| Deferred detailed-adapter projection | 149.237 / 151.598 | 161.552 / 314.529 | 180.579 / 339.454 |
| Bounded 240-minute wait action | 152.712 / 156.241 | 232.279 / 301.518 | 168.201 / 179.249 |

The current projections validate full foundation state by design; their measured cost is therefore not a claim of later renderer performance. The broad fixture's active world after the benchmark action was the largest serialized specimen at 282,385 canonical UTF-8 JSON bytes.

| Deterministic serialized fixture record | Measured bytes | Tested ceiling | Classification |
| --- | ---: | ---: | --- |
| Sheltered active world after bounded wait | 234,703 | 512 KiB | Enforced deterministic fixture budget |
| Watershed active world after bounded wait | 254,485 | 512 KiB | Enforced deterministic fixture budget |
| Far Coast active world after bounded wait | 282,385 | 512 KiB | Enforced deterministic fixture budget |
| Terminal-control preferences | 575 | 4 KiB | Enforced deterministic fixture budget |
| Bounded three-profile creation settings | 620 | 16 KiB | Enforced deterministic fixture budget |
| Three-world index | 292 | 64 KiB | Enforced deterministic fixture budget |

“Enforced” means the deterministic fixture ceiling is checked in the performance-budget unit test. It is not an IndexedDB quota forecast or a new runtime save-rejection rule.

## Review targets and storage planning bands

These are conservative Phase 1.6 review targets. They are intentionally not normal-test wall-clock assertions. The later performance-fixtures slice may introduce platform-aware regression policy after real browser measurements exist.

| Category | p95 / maximum target | Classification |
| --- | ---: | --- |
| Cold browser app ready with focused canvas | 2,000 ms p95 | Future browser target |
| Create and save each current preset | 1,500 ms p95 | Future browser target |
| Selected-world resume/load | 250 ms p95 | Future browser target |
| One terminal/sidebar/deferred-adapter pure projection | 500 ms p95 | Future browser target |
| One zero-time UI input response | 50 ms p95 | Future browser target |
| One bounded current scheduler/time-bearing action | 500 ms p95 | Future browser target |
| IndexedDB active-world save or load | 250 ms p95 | Future browser target |
| Normal all-record local-storage planning band | 16 MiB total | Planning band |
| Advisory write-warning point | 24 MiB total | Planning band |
| Elevated storage-pressure review point | 32 MiB total | Planning band |

Quota is browser-, origin-, device-, and private-mode-dependent, so these bands are not a reservation. The application must not fill quota while measuring. Valid active worlds and finalized chronicles are retention priorities and must never be silently deleted by Jomon. Quota/write recovery, snapshots, and explicit import/export backup UX belong to the next persistence task.

## Optimization eligibility

[Profile-guided optimization boundaries](optimization-boundaries.md) separates the current reproducible workload and recorded review observations from future eligibility. It makes the current foundation ineligible for spatial grids, packed/binary authority data, workers, and broad incremental generation: terminal presentation has zero materialized map cells, frontier commitments are bounded, and catch-up is scheduling/provenance rather than domain simulation. Any later optimization needs identity-bound, fail-closed measurement evidence and must preserve deterministic canonical output; no wall-clock value becomes a simulation input or persisted world fact.
