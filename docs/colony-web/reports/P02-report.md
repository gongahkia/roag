# P02 deterministic world, navigation and browser saves

Date: 2026-11-06. Result: **COMPLETE**. P03 has not started.

## Identity and outcome

Requested goal: implement P02 under the Common contract, preserving accepted P01
and repair 01. Deliver pure deterministic simulation, seeded connected terrain,
six founders and heart, destination orders/routing, position/order/access inspection,
pause/1×/2×/4× with interpolation and hidden-tab behavior, validated transactional
IndexedDB saves and JSON export/import, terminal scaffolding only, all headless and
production browser gates on `/` and `/roag/`, preservation and this report. Stop
before jobs/construction/needs/combat/raids, deletion, push or deployment.

Starting/ending HEAD: `88026d076ea2940d244d1f0e38e4644f9dd2ceb1`. All **30 pre-existing
uncommitted P01/repair source files** were snapshotted before editing, along with
hashes of **242 earlier evidence files**. The source snapshot and exact initial
identity are under `artifacts/colony-web/P02/before-source/` and `before-source.json`.
No unrelated edits were found. No commit was created. Final source inventory/hashes,
full uncommitted patch and incremental P02 patch are `final-source.json`,
`implementation.patch` and `P02.patch` under that same evidence directory.

Players can start a numeric-seed colony, select any founder, right-click a destination
or enter tile coordinates, watch navigation, inspect position/order/step/access,
pause/change speed, save and actually reload, or export/import a JSON snapshot.
A routing-lab scenario demonstrates visible obstruction and recovery. The world is
128 × 128 with six founders at **10 Hz**, retaining the documented defaults. New runs
start paused for planning. Terrain/resource records replace the static fixture as
authority; founder identity/appearance remains authored metadata.

Unavailable: autonomous jobs, harvesting, construction, needs, injuries/combat,
raids and actual defeat events. Terminal policy is scaffolding, not a playable
survival loop. No legacy-runtime dependency or save import was introduced.

## Acceptance evidence

Evidence paths below are relative to `artifacts/colony-web/P02/`. `validation.json`
records exact cwd, argv, environment, exits and durations. Final acceptance commands
exit **0**. Earlier failed attempts are retained and explained below.

| P02 gate / requested evidence | Result | Actual command/scenario | Evidence and observed result |
| --- | --- | --- | --- |
| 1. Same seed/commands give identical state/RNG across render cadences | PASS | `npm test`; `npm run test:scenarios`, seed 42, tick-indexed commands below, frame cadences `[10,17,33]` vs `[250,120,7,850]` ms | `accepted-local-tests.log`, `final-scenarios.log`, `scenarios.json`. Six full-state/RNG checkpoints at ticks 50–300 compare exactly. Rendering interpolation is read-only. Foreground backlog is retained and work bounded to eight ticks/frame. |
| 2. Exact mid-movement save/restore continuation | PASS | Headless seed 1337 at tick 7 with an in-flight step and pending tick-40 order, then 180 further ticks; actual browser save/reload at tick 58 | `scenarios.json`, `browser-evidence.json`, downloaded JSON files. Headless final SHA-256 matches uninterrupted execution. `/` resumes 22 ticks and `/roag/` 24 ticks; browser states equal independent headless continuation. |
| 2. Corrupt/unsupported/invalid-reference saves leave valid state and slot intact | PASS | Schema/local tests and real production import journeys at both mounts | Invalid JSON, schema/content versions, IDs, occupancy, RNG, progress, pending references and terminal consistency rejected locally. Browser corrupt/version/reference imports leave exported current state and exact IndexedDB slot text unchanged, then real reload restores the valid slot. |
| 3. Multi-seed spawn/heart/resource access and bounded fallback | PASS | `npm run test:scenarios`: seeds 1–100, 1337, 123456789, 2147483646; force fallback for 1, 42, 2147483646 | `scenarios.json`: all 103 normal seeds validate connected walkable terrain, six unique spawns, all resource-adjacent access and four clear approaches. Three forced fallback worlds validate. No natural fallback occurred in this sweep. |
| 3. Obstruction, competing movement, invalidation/recovery without teleportation | PASS | Local reservation/conflict/search-bound tests; routing lab seed 42, destination 70,60, close/open gate 64,60; save/reload blocked intent in production | `accepted-local-tests.log`, `scenarios.json`, browser routing snapshots/screenshots. Stable-ID winner reserves current/next tiles; competitors wait and recover after access changes. Per-tick displacement is at most one cardinal tile. No route teleports, swaps or overlaps. Terrain edits invalidate pending paths; a reserved step finishes before replanning. |
| 4. Production reload, JSON round-trip, pause/speed, observed hidden transition | PASS | `npm run test:browser` builds explicit `/` and `/roag/` and runs 20 tests on strict static servers | `accepted-browser.log`, `browser-results.json`, `browser-evidence.json`. 20 passed, zero skipped/flaky/unexpected outcomes. Actual native `visibilityState` becomes hidden/visible; ticks stay fixed while hidden and manual pause survives return. |
| P01 retained presentation, art and browser failure gates | PASS | Adapted camera/selection/resize/provenance/credits/missing-art journeys, plus rendered terrain-crop verification | All ten earlier production cases pass on the generated world. A 2080px desktop establishes the 200% minimum required for the retained resize regression on the larger map. Rock tile `(60,59)` matches all 144 preserved crop pixels after documented layer alpha on both mounts. |
| Installation, type/build/assets and preservation | PASS | Pinned `npm ci`, `npm run typecheck`, `npm test`, `npm run build`, `npm run assets:check`; before/after `python3 -B tools/verify_preservation.py` | Clean install succeeds; **17 local tests** pass; relative/root/subpath builds pass; 27 selected crops validate. All **1,163 protected originals** and preservation manifest remain intact. Original reports/evidence/archives compare unchanged. |

### Replay commands and checkpoints

Seed 42 commands are enqueued in this sequence (stable numeric founder IDs):

1. Due tick 2: actor 2 → `(72,64)`.
2. Due tick 2: actor 3 → `(70,62)`.
3. Due tick 120: actor 2 → `(58,66)`.

Each cadence reaches the same six checkpoint hashes. Full hashes and all six RNG
stream states are in `scenarios.json`; they are measurements, not golden values
silently updated to make a failing test pass. The separate exact-continuation case
uses seed 1337, actor 2 → `(75,64)` at tick 1, actor 3 → `(73,62)` at tick 40, saves
at tick 7, then advances 180 ticks. Its final full-world SHA-256 is:

`3b513b125371110e0e9d3279110bed331a5634bb245da9003822a094e081c2e3`.

Actual production seed-42 save at tick 58 preserves Ada at `(74,63)`, next `(75,63)`,
progress **1/4**, destination `(80,64)`, remaining path and route/retry metadata.
After real reload, exported state and controls equal the pre-reload snapshot. The
resumed `/` state after 22 further ticks has matching expected/actual SHA-256:

`9f5e89328a77e55761a24d544c058c83d2f836b63bbe26ba133ae9c6646ea2a9`.

The `/roag/` state after 24 ticks similarly matches:

`5ce85d9e2eb1bbac770fd7253dd53f0041d376957ea187af090d61a490b15c77`.

Both browser journeys create another seed-1337 run, import the exported continuation,
compare full JSON, reject three invalid imports, verify the prior slot byte-for-byte,
and reload again. Save/control snapshots are retained per browser test.

### Failures and necessary repairs

- Initial type/local checks found stale P01 fixture imports and tests assuming the
  smaller map's minimum zoom. Adapted them to generated founders and new dimensions.
  A route test initially expected an in-flight reserved step to disappear; corrected
  the scenario to verify safe completion, invalidation and waiting without teleportation.
  An occupancy-negative test initially changed only X on different Y tiles; it now
  creates a real overlap. Initial logs remain as `tests-attempt-1.log` and later runs.
- `browser-attempt-1.log`: ten adapted P01 cases passed. `browser-attempt-2.log`:
  18/20 passed; the lab corridor covered two other founders' spawn tiles, so the
  validator correctly prevented export. Relocated only those lab spawns and added
  a blocked-lab save round-trip assertion. `browser-attempt-3.log`: 20/20 passed.
- Review added rejection of unknown command kinds in the development command API,
  with a no-state-change regression, and explicit speed-factor/backlog assertions.
- One later visibility test failed only during temporary Chromium-profile cleanup
  (`ENOTEMPTY`) after its behavior checks passed. Browser shutdown is now graceful
  and filesystem cleanup retries boundedly; final native visibility cases pass.
- Visual review found a near-black floor sprite insufficiently distinguished blocked
  rock. Initial suspicion of shifted crops was corrected after reading Phaser's
  frame-bounds implementation. Added the original stone-wall crop for impassable
  terrain/stone nodes; explicit sheet frame bounds remove a layout warning. A new
  pixel check initially sampled the selection outline; it now samples unobstructed
  tile `(60,59)` with the same strict color tolerance. `browser-terrain-fix.log`
  retains that failed test attempt. Final `accepted-*` logs pass all requirements.
- Earlier failed traces/screenshots/results remain under attempt directories. The
  final report uses the final output, never counts failed runs as accepted checks.

## Port and design decisions

| Legacy source / tests read | Retained invariant | Intentional adaptation | New proof/player scenario |
| --- | --- | --- | --- |
| `RoagRandom.cs`; `NativeFoundationTests.cs`; `original-foundation.json`; `capture_native_foundation_reference.py` | Park–Miller multiplier 48271/modulus 2147483647; unbiased one-based rejection sampling; UTF-8 byte stream labels | Use six named colony streams; do not port `WorldRandom` hash/world layout or legacy combat randomness consumption | **768 draws in 24 reference cases**, final states and **9 derived labels**, including `洞窟🔥`, match preserved Lua-derived fixtures used by the C# tests. Stream independence checked. |
| `ActionTimeline.cs`, `ActionTimelineTests.cs` | Stable sequence ties, saved pending work, inspection/rejected orders consume no simulation RNG/time/sequence | Fixed ticks and `(due,sequence)` commands replace party readiness/event priorities; process commands before movement | Tick-indexed replay/cadence equality, pending-order save continuation, rejected command tests |
| `WorldModel.cs`, `ChunkGenerator.cs`, `OverlappingWfc.cs`, `StructuralLayouts.cs`, generation content | Seeded bounded generation, clear approaches, reachability validation, deterministic fallback | Single 128 × 128 surface, constructive roads/plaza, prune disconnected floor islands; no WFC/streaming/floors/legacy world | 103-seed connectivity/access sweep and three forced fallback validations |
| `WorldNavigation.cs`, `WorldCollision.cs`, `ActorSpatialIndex.cs`, `LocalRoutingTests.cs`, `SpatialIndexTests.cs` | Solid terrain, stable identity/spatial lookup, cardinal/stable ties, bounded search and current occupancy | Full destination intent, four ticks per tile, 4,096 expansions/search, stable founder priority and next-cell reservations; no party formation or stairs | Competition/occupancy invariants, bounded search, live route invalidation/recovery and blocked browser reload |
| `WorldPersistence.cs`, `PersistenceTests.cs`, `SaveSnapshotTests.cs` | Validate before replacing a valid slot; exact authoritative state and RNG continuation; failed writes retain the last save | One atomic IndexedDB object, portable versioned JSON; browser controls saved separately from world; no legacy save conversion | Real transaction-failure injection, unavailable storage, invalid import/slot checks, browser reload/JSON continuation |
| `NativeWorldClockTests.cs`, PORT_MAP.md browser boundary | Simulation progress is owned by committed ticks | Replace paid party turns with 10 Hz; unsmoothed frame input; pause on visibility without offline progression | Exact cadence replay, backlog/pause unit tests, observed native tab transitions |

No C# runtime is invoked by the app. C#/.NET suites were not rerun; the existing
reference-derived fixtures supply concrete RNG known vectors. JS RNG products stay
below 2^53, so no implicit C# integer-overflow equivalence is assumed. Derivation
explicitly encodes Unicode code points as UTF-8, including non-BMP characters.

Authoritative state includes tick, all six RNG states, ID/command counters, terrain
and version, generation result, heart/population life, finite resource quantities,
pending commands and complete movement/retry state. Occupancy/spatial lookup derives
from current and in-flight cells. Camera, selection, interpolation alpha and wall-time
accumulation are presentation/platform data. Rendering never changes world state.

P02 tuning: four ticks/tile, 4,096 expansions/search, retry after ten ticks for failed
routes (one tick after a cached route encounters an occupied tile), eight ticks/frame,
18 resource nodes and a 2 MB JSON-file limit. The 128 × 128 / six-founder / 10 Hz
product defaults are unchanged; DECISIONS.md did not need editing. New runs pause
for planning and do not wander autonomously before a destination order. This is
movement intent, not the P03 autonomous job system.

## Preservation and scope

- Before and after: `python3 -B tools/verify_preservation.py` passes **1,163 files**,
  zero failures. Manifest SHA-256 compared separately; never rebaselined.
- Both P01 reports, all **242 earlier evidence files**, both preservation archives and
  legacy source remain unchanged. `integrity.json` records exact comparisons and
  archive receipt hashes. Archives/tag remain local, not uploaded backups.
- Added only `web/` implementation/tests/tooling, updated scoped web CI and README,
  STATUS.md and this report. No protected original, legacy code, content/catalog,
  archive or old report/evidence file was edited/deleted. The existing .NET workflow
  and global AGENTS.md symlink were preserved.
- Runtime selection is now **27 original crops**. New `rock` alias:
  `urizen-onebit-r02-c000`, original `wall / stone`, source
  `urizen/urizen_onebit_tileset__v2d0.png`, original rectangle **[1,27,12,12]**.
  This assigns a colony terrain role; it does not infer author collision/autotiling.
- Compact manifest retains IDs, original neutral names/confidence, source/crop hashes,
  rectangles and packed frames. The canonical gzip remains authority; stale ignored
  `assets/catalog.json` was never used or rewritten.
- Four runtime files total **18,477 raw bytes**; the same packs/credits apply. The
  new derived PNG SHA-256 is
  `8ab2fc9bb698af4a95969c6e1246e5aa2c96f5b59645eab11b720704d1d1594b`.
  The full research catalog/atlas/browser, archives, fonts, source maps, old saves
  and legacy runtime remain excluded from the nine-file distribution.

## Tests, browser, saves and performance

Pinned toolchain retained: Node **24.21.0**, npm **11.19.0**, TypeScript **5.9.3**,
Phaser **3.90.0**, Vite **8.3.6**, Playwright **1.65.0**, pngjs **7.0.0**. Package
lock/dependency versions were not changed. Installed official source/types were
consulted for Phaser tilemap/frame bounds, unsmoothed TimeStep `rawDelta`, and
Playwright CDP `noDefaults` behavior.

Exact acceptance commands from `web/` (the recorded runner used the absolute form
of the same evidence directory and `fnm exec --using=24.21.0` for every npm command):

```sh
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P02
fnm exec --using=24.21.0 npm ci
fnm exec --using=24.21.0 npm run typecheck
fnm exec --using=24.21.0 npm test
fnm exec --using=24.21.0 npm run test:scenarios
fnm exec --using=24.21.0 npm run build
fnm exec --using=24.21.0 npm run assets:check
fnm exec --using=24.21.0 npm run test:browser
python3 -B ../tools/verify_preservation.py
```

Final logs: `npm-ci.log`, `typecheck.log`, `accepted-local-tests.log`,
`final-scenarios.log`, `accepted-build.log`, `final-asset-check.log`,
`accepted-browser.log`, `preservation-final.log`. Build/browser scripts include
another strict typecheck. Before/after preservation commands were executed at the
repository root. Exact commands/exits and the retained failed attempts are recorded
under P02; old P01 catalog/.NET evidence is not represented as newly executed here.
A fresh host needs `npx playwright install chromium` (minimal Linux CI uses
`--with-deps`); browser installation was reused from the accepted pinned P01 setup.

Platform: Linux x86_64/WSL2, Chromium **158.0.8127.18**. Main suite uses Playwright's
browser; native-visibility journey launches a separate full Chromium and attaches
with **`connectOverCDP({noDefaults:true})`** so Playwright does not force focus/visibility.
Actual tab activation yields actual `document.visibilityState` transitions and
native `visibilitychange` events. No property override or synthetic visibility event
is used in that journey. Pure clock unit tests set flags separately and are not
claimed as the observed browser transition.

Observed final transitions:

| Mount | Running hide → return | Manual-paused hide → return | Result |
| --- | --- | --- | --- |
| `/` | tick 10 → 10, manual pause false | tick 14 → 14, true | Hidden wait does not advance/catch up; running resumes, manual pause remains paused. |
| `/roag/` | tick 5 → 5, false | tick 9 → 9, true | Same behavior. |

Hidden waits were 900 ms and 500 ms. Normal-page journeys test speed controls,
real reload, movement and the UI form/right-click pathways. Corrupt/version/reference
imports preserve current exported state and exact stored slot text. The quota case
injects a `QuotaExceededError` at IndexedDB put, observes transaction failure and
checks the previous real slot; it does not claim an actual disk was filled. The
unavailable-storage case injects `SecurityError`, then demonstrates memory movement,
a new run and successful exports. Valid import requires a successful transaction;
when storage is unavailable it is rejected without replacing the current world.

All final successful journeys have no console/page/network errors; missing-art cases
intentionally return 404. Thirty screenshots were captured; reviewed image hashes
and observations are in `visual-review.json`. Opened/inspected final examples include:

- `browser-results/world-movement-speed-mid-m-2cdf2-N-with-safe-invalid-imports-root/mid-movement-saved.png`: moved selected founder, current step/destination and saved tick readable; rock obstacles visible.
- `browser-results/world-routing-lab-blocks-v-094bf-ers-through-a-reopened-gate-roag/blocked.png`: visible rock corridor/gate and explicit blocked access reason.
- `browser-results/foundation-resize-and-narr-04170--inspector-and-all-founders-root/resize-390.png`: controls wrap, names/inspector/form readable, no horizontal overflow.
- `browser-results/world-native-tab-visibilit-a2d81-eserves-manual-pause-intent-roag/visibility-resumed-manual-pause.png`: manual pause remains visible after real tab return.

The final terrain test checks **144 pixels / three channels** on both bases against
the selected original crop plus known background/layer alpha, tolerance two channel
levels. This complements asset-export pixel/gutter tests and visual inspection.

| Production output | Files | Raw bytes | gzip bytes | Brotli bytes |
| --- | ---: | ---: | ---: | ---: |
| Relative `web/dist/` | 9 | 1,263,200 | 338,575 | 272,642 |
| Explicit `/` | 9 | 1,263,195 | 338,572 | 272,647 |
| Explicit `/roag/` | 9 | 1,263,220 | 338,582 | 272,627 |

Per-file hashes/sizes are in `build-sizes.json`, `root-sizes.json`, `roag-sizes.json`.
Compression sums files independently at gzip level 9/Brotli quality 11, not actual
hosting transfer settings. The reference tick-7 movement snapshot is **35,470 JSON
bytes**. Save size and exact actor/route/pending state are recorded
in `scenarios.json` and browser downloads. Six actors, 18 resource records; no jobs,
raids or growth loops exist. No active-AI throughput, frame-time, long-run memory,
save-growth or 24-founder performance benchmark was required or measured.

Limits: Chromium/WSL2 only; other engines, real touch and screen-reader audit remain
untested. No remote CI or deployed host was exercised. Routing is bounded and can
show a search-budget failure for difficult long routes; it retains intent and retries.
Head-on corridor conflicts may require a new order to yield; no teleportation or
invisible actor removal resolves them. The focused lab is not generation evidence.
No known failing P02 acceptance gate remains. No P03 work was begun.

## Resume and recommendation

From repository root:

```sh
cd web
fnm exec --using=24.21.0 npm run dev
```

Open `http://127.0.0.1:5173/`. Choose Ada, right-click a visible destination or enter
X=80/Y=64 and press Go to tile, then Resume. Try 1×/2×/4× and Pause. Save while her
order is in progress, reload, reselect her and Resume. Export JSON, start a new run
with seed 1337, and import the export to restore it. Invalid JSON shows rejection
while the current run/save remain usable. Always save explicitly before closing.

For route recovery, open `/?scenario=routing-lab`, choose Ada, enter 70,60, expand
Run diagnostics and close the gate before Resume. After the blocked reason appears,
open the gate; she proceeds to 70,60. The lab uses its own IndexedDB slot. For exact
production builds after the acceptance commands, with the evidence variable set:

```sh
fnm exec --using=24.21.0 node scripts/serve-production.mjs root
# http://127.0.0.1:4173/
# In another terminal with the same ROAG_EVIDENCE_DIR:
fnm exec --using=24.21.0 node scripts/serve-production.mjs roag
# http://127.0.0.1:4174/roag/
```

Required repairs: none identified for P02's gates. Recommendation: review this
report/code/evidence before issuing a separate P03 goal. P03 should introduce real
claims, extraction/hauling and construction with conservation accounting, not expand
this phase implicitly. No question requires a product/access decision for P02.

Fresh sessions should read STATUS.md, this report, both P01 reports, PROMPTS.md,
PLAN.md, DECISIONS.md, PRESERVATION.md, PORT_MAP.md and web/README.md.

## Paste-back block

```text
ROAG MIGRATION REPORT
Phase: P02
Status: COMPLETE
Source: HEAD 88026d076ea2940d244d1f0e38e4644f9dd2ceb1 plus preserved uncommitted P01/repair work and P02 changes. Exact source hashes, before snapshot, full patch and P02.patch are under artifacts/colony-web/P02/.
Report: docs/colony-web/reports/P02-report.md; STATUS.md updated.
Player-visible result: A 128x128 seeded colony with its heart and six founders at 10 Hz. Select a founder, right-click a destination or use the coordinate form, watch bounded navigation, inspect position/order/progress/access, pause or choose 1x/2x/4x, save/reload, create a new seeded run and export/import JSON. New runs pause for planning. A separate routing lab demonstrates blockage and recovery.
Acceptance: All P02 gates pass. Seventeen local tests validate reference RNG semantics, deterministic tick ordering, occupancy/routing, schema checks, terminal scaffolding and clock behavior. The 103-seed sweep and three forced fallbacks validate spawns and reachable heart/resources. Different frame cadences produce identical checkpoints. A tick-7 mid-edge save with a pending order matches uninterrupted execution after 180 ticks, hash 3b513b125371110e0e9d3279110bed331a5634bb245da9003822a094e081c2e3. Twenty production browser tests pass across / and /roag/, including actual reload continuation, JSON round-trip/rejection, storage failures, retained P01 journeys and exact rendered terrain pixels. Native tab transitions are observed with real hidden/visible states; hidden ticks remain fixed and manual pause survives. Successful journeys have no console/page/network errors.
Checks executed: From web/, export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P02, then use the pinned Node 24.21.0/npm 11.19.0 for npm ci; npm run typecheck; npm test; npm run test:scenarios; npm run build; npm run assets:check; npm run test:browser. Preservation ran before/after. Final commands exit 0; validation.json, scenarios.json, browser-evidence.json, downloads and 30 screenshots retain evidence. Screenshots were opened and inspected.
Preservation: All 1,163 originals pass; manifest, both preservation archives, both P01 reports and all 242 prior evidence files remain unchanged. Only 27 selected crops ship; full research assets remain excluded. /roag/ build is 1,263,220 raw bytes, 338,582 gzip, 272,627 Brotli.
Ported/changed rules: Retained Park–Miller rejection sampling and UTF-8 derivation against 768 reference draws/nine labels; stable ties, collision and validated continuation adapted to colony ticks and transactional IndexedDB. No legacy runtime imports or gameplay parity claim.
Known gaps: No jobs, harvesting, construction, needs, combat or raids. Chromium/WSL2 tested; other engines, hardware touch, remote CI/deployment and long-run performance untested. Difficult routes can report a bounded-search failure; corridor conflicts may need a yielding order.
Run/reproduce: cd web; fnm exec --using=24.21.0 npm run dev. Open http://127.0.0.1:5173/, select Ada, order 80,64 and Resume. Save mid-move, reload and Resume; use Export/Import JSON. Add ?scenario=routing-lab for gate recovery.
Recommendation: Review P02 before separately authorizing P03. P03 has not started; no legacy deletion, push or deployment occurred.
Questions: None.
END ROAG MIGRATION REPORT
```
