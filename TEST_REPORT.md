# DEEPWARD 0.4.0 — verification report

## Execution boundary

The core ran against the installed native **Lua 5.4 shared library**, invoked by a
small host runner implementing script/argument loading. The host runner is not a
project dependency and is not packaged. No LuaJIT, real LÖVE application, SDL event
loop, GPU rendering or physical keyboard/file-drop path was available in this
implementation environment. Source targets Lua 5.1-compatible syntax and LÖVE 11.5;
those compatibility targets are not a claim that those binaries were executed.

All evidence below is for this release. Earlier reports/results live under
`docs/legacy-v030/` and `docs/prior-verification/`; they are labelled historical.

## Executed checks

| Check | Actual result | Evidence |
|---|---|---|
| Source loading / syntax | 64 Lua files compile under Lua 5.4 | `docs/verification-v040/syntax-lua54.txt` |
| Core, map and expansion regressions | **66 groups; 113,663 assertions passed** | `docs/verification-v040/core-lua54.txt` |
| New interaction subset | 22 groups; 498 assertions, included above, not additional | Same core output |
| Original application mock | 420 gameplay frames, construction, layers, resize, timeline, saves, exports | `docs/verification-v040/gui-lua54.txt` |
| Map application mock | Preview settings, exports/imports, archive, save-failure rollback | `docs/verification-v040/map-gui-lua54.txt` |
| Expansion application mock | Nine workers, draft quotas, named tasks, rally, arming, survey, notes, archive lock and lab settings | `docs/verification-v040/expansion-gui-lua54.txt` |
| Benchmark adapter | Two short cases; six cooperative yields; valid CSV/manifest | `docs/verification-v040/benchmark-smoke-lua54.txt` |
| Generation matrix | **66 measured maps**, plus 22 discarded warm-ups; exact cell and encounter round-trips | `docs/verification-v040/generation-lua54.csv` and manifest |
| Ecosystem/colony soak | **Six 800-tick cases**, 3/6/9 workers, zero accounting residuals at each 100-tick check | `docs/verification-v040/soak-lua54.csv` and `.txt` |
| Soak persistence | Each case reconstructs tick 400 from zero after discarding checkpoints, then verifies a full save/load | Same soak output |
| Default colony | **2,000 ticks** on 256x160 frontier, three alive, one farm, three structures including starter, zero water/mineral/food residuals | `docs/verification-v040/default-colony-lua54.txt` |
| Maximum dimensions | All **11 layouts** at **512x256**, nine-person starts, exact map round-trips and two simulation/accounting steps each | `docs/verification-v040/maximum-size-lua54.txt` |
| CLI map workflow | Generated examples; byte-identical roundtrip; regeneration changed geometry but not biome cells | `docs/verification-v040/cli-comparison.json` and example maps |

The large assertion count mainly comes from exhaustive cell comparisons. It does
not mean that 113,663 distinct gameplay scenarios were explored. The maximum-size
check is a boundary smoke test, not a populated long-duration performance guarantee.
Survival was recorded in the soak, not required as a universal game invariant.

## Important regression coverage

The suite retains original material, work, construction, resource, navigation,
challenge and map tests. It adds whole-worker percentage arithmetic (including an
empty pool), pinned/OFF restrictions, named-job execution, reservation release on
policy changes, personal-need overrides, rally/release, and archive rejection.

Charge tests separate construction, actual worker arming and fuse countdown. They
exercise lethal damage, bedrock protection, adjacent-charge delayed chaining, water
phase changes and explicit mineral losses. Wards consume actual delivered water.
Fieldwork tests require reachable workers, move resources rather than duplicate them,
and follow a moving cull target. Ecology tests cover conversion ledgers, disturbance
wake-up, barriers, nursery stock transfers and bounded reproduction. Replay includes
fuses, policies, knowledge and ecosystem state.

Genuine fixtures produced by the original 0.2 and 0.3 source are still compared to
independently recorded expected states. In particular, the original 0.3 fixture
continues to tick 100 with expected hash `4eadf344`. These are diagnostic hashes,
not cryptographic proofs; other replay tests also compare complete encoded state.
New save envelopes are version 0.4.0 so older applications reject them explicitly.
Map schema 2 retains reading of schema 1 and the original frontier-v1 regeneration.

Issues caught and fixed during implementation included a nine-person starting
footprint overlapping the finite well, ground fauna initially being placed in
liquid, and external lethal damage being eligible for bed healing before death
processing. Regression tests now cover valid crew footprints and zero-HP death.

## Visual and UI checks

The mock captures input and draw contracts. Internal rasterisations of captured
commands were inspected for the individual-duty panel, quota panel, nine-person
sidebar, world lab and field notes at 1040x720. These are **not screenshots from
LÖVE** and do not validate fonts, GPU texture uploads, SDL scaling or driver behaviour.
No mock image is presented as a real application screenshot in the release.

## Package verification

The complete archive was extracted into a fresh directory. Its CRCs, archive paths
and every SHA-256 manifest entry validated. The extracted source compiled all
64 Lua files and passed the full 66-group / 113,663-assertion suite again. The
record is `docs/verification-v040/package-core-lua54.txt`. Final packaging adds this
record and documentation only; runtime and test source are identical to that run.
The ZIP contains no host runner, font files, local live saves or outside dependencies.

## Measurements and limitations

CLI durations use `os.clock` CPU seconds. They are not wall time, FPS, GPU timings or
predictions for the user's machine. Automatic GC remained enabled inside timed
operations. The generation manifest documents warm-ups and exclusions. Soak tick
times include history snapshot work; the ecology sub-timer excludes other systems.
Lua heap probes are not RSS, peak allocation or graphics memory.

Balance, colony defence, long-lived ecosystem stability, and subjective interest
require playtesting. The population bounds and tests prevent particular failures;
they do not establish realistic ecology or universal safety. Real LÖVE launch, file
drop, visual legibility, LuaJIT execution and interactive speed remain local checks.

## Reproduce

From the repository root:

```sh
lua tests/syntax.lua
lua tests/run.lua
lua tests/gui_smoke.lua
lua tests/map_gui.lua
lua tests/expansion_gui.lua
lua tests/benchmark_smoke.lua
lua tests/maximum_size.lua
lua tools/expansion_soak.lua soak.csv 800
lua tools/generation_benchmark.lua generation.csv
lua tools/headless.lua 12345 frontier 2000
```

Use `luajit` instead of `lua` to test the target runtime on the user's machine. F10
runs the regression suites inside LÖVE; it does not turn the separate GUI mocks into
real graphical integration tests. No regression tool should be pointed at live saves.
