# Workplaces, roads, events and individual residents

This tranche extends the existing local Flutter/Flame settlement. The world and
its construction brush remain the primary controls. New work, service and social
venues use actual resident journeys and productive time; there are no goods,
wallets, recipes, individual orders or extra resource bars.

## Local play and controls

From the repository root:

```sh
python3 -B apps/settlement/tool/serve.py --port 8765
```

Open or refresh <http://127.0.0.1:8765/settlement/>. The existing server can serve
the rebuilt release. Port 8765 already serves this app on the verification
machine; check the URL before starting another copy. Use a hard refresh if an
old toolbar is cached. Another port has separate browser
storage; export/import transfers a copy without resetting the original town.
Nothing in this tranche is deployed or published.

The **Build…** popup (`B`) groups Homes, Work, Social and Roads. The original
five tools stay directly available; the popup shows a new selection for repeated
painting. Existing `1/C`, `2/P`, `3/R`, `4/G`, `5/V` shortcuts retain Cottage,
free dirt Path, Remove, Farm and Park. New shortcuts:

| Key | Tool |
| --- | --- |
| 6 | Workshop |
| 7 | Market |
| 8 | Community Hall |
| 9 | Stone Road |
| 0 | Garden Path |

Click/drag and release still makes one construction transaction. Escape cancels;
Ctrl/⌘ Z and Shift Z undo/redo. Space/right/middle drag pans; scroll or +/− zooms;
H returns to the lane; T cycles pause/1×/2×/4×. The grouped popup scrolls where
needed rather than adding all tools to the permanent row.

`I`, the header inspection button or Alt-click selects a resident or place.
Selection happens on release with a five-pixel drag threshold. Repeated clicks
at the same overlapping group cycle its nearby residents, with a selected-person
ring and a hint in the card. Right/middle/Space dragging still pans. Ordinary
construction gestures are not intercepted by residents. Escape dismisses
inspection. Cards are read-only and show the real sprite, stable name, trait,
activity, home/workplace, mood and its reasons.

## Buildings and economy

| Building | Cost | Work / visitors | Actual role |
| --- | --- | --- | --- |
| Cottage | 5 | One resident | Existing home and arrival rules |
| Farm | 8 | 2 jobs | 0.2 coin per validated worker-second; existing 12-second bouts |
| Workshop | 16 | 1 job | 0.4 coin per validated worker-second; 12-second bouts |
| Market | 14 | 1 vendor, 3 customers | Vendor work pays 0.1 coin/second; completed service separately earns 0.25 coin |
| Park | 6 | 6 visitors | Existing six-second social visits and evening gatherings |
| Community Hall | 12 | 8 visitors | Covered social venue; preferred for new relaxation visits during rain |

Every building has one cell and requires the same adjoining arrival-connected
path network. Farms supply two inexpensive jobs; Workshops cost twice as much
and supply the same total full-staff income using one person. Parks remain the
cheapest social venue. The ordinary 20-coin grant per 150-second day, happiness
bounds, demolition traces and existing financial history rules remain.

A vendor must actually be working at a reachable Market. Customers must arrive,
fit its customer capacity and spend six valid service seconds. Missing staff
stops service progress; travellers and waiting people do not occupy served
visitor slots. Vendor work income and trade income have separate counters and
inspection explanations. The displayed individual trade value is **0.25**, not
rounded to a misleading 0.3.

The resident's persisted `tradeDay` allows at most one base trade credit per
simulation day **across all Markets**. A Market-day bonus attaches to that same
transaction; already-credited visits can still be real social visits but pay no
second base or bonus. Removing, rebuilding, importing or reloading a Market does
not clear a person's record. There are no household wallets or goods.

Daily shopping uses the existing routed visit machinery after productive work
or a fruitless job search, while a vendor is present. It is distinct from an
ordinary relaxation preference, so commerce can still happen in rain. A sole
vendor never visits its own stall as a customer. Vendors work longer 35-second
bouts and can retry after six seconds; this gives customer journeys a usable
service window. Completed valid visits still feed the existing social happiness
system. In towns containing a Market, idle unemployed residents can seek later
social visits after the existing 18-second interval. No new need meter is added.

## Roads, movement and daily traversal credit

All surfaces occupy the existing road cell and share coherent ports and access.

| Surface | Cost per changed cell | Traversal / effect |
| --- | --- | --- |
| Dirt Path | Free | Existing baseline; rain makes travel 20% slower |
| Stone Road | 2 | 18% faster; retains that speed in rain |
| Garden Path | 1 | Normal pace; rain slows it; actual traversal can grant +3 mood once per day |

Changing to a different paid surface charges its full displayed price once.
Repainting the same surface is a no-op. Switching to dirt or removing a road
refunds nothing automatically. A stroke deduplicates changed cells and keeps its
original budget; cancellation charges nothing. Undo/Redo reverse/reapply the
recorded charge, with existing atomic affordability checks and a forward-moving
ID allocator. A surface change never temporarily removes the road or recreates
adjacent residents.

Weighted navigation and movement share the same edge cost: the mean of the
endpoint surface time multipliers. Brisk is a constant resident multiplier, so
shared destination fields remain valid for different traits. A segment records
its speed and Garden Path eligibility when it starts. A surface/weather change
applies to later segments; a removed target still fails the immediate legality
check. Segment metadata survives save/reload.

The arrival-time test found that the old integrator discarded leftover movement
time at every tile boundary, sometimes erasing an 8% Brisk advantage. Movement
now consumes that leftover time on the next legal segment. Reaching work does
not turn the leftover fraction into paid productive time: arrivals still pay
nothing in their arrival tick.

`gardenDay` changes only on a completed qualifying traversal, not from painting
beneath someone, hovering, inspection or standing. It grants +3 for that day,
less than a completed Park visit. Crossing more tiles does not stack it. The
record survives history and persistence, and a new day requires a new traversal.

## Traits, identity and special events

Names use frozen local first/last-name pools. Explicit deterministic mappings
assign missing names/traits once from durable resident identity; stored choices
do not depend on catalogue length, population, camera, frames or event RNG.
Existing bodies and palette indices are retained, including inactive records.

| Trait | One authoritative effect |
| --- | --- |
| Brisk | 8% faster walking, verified by actual arrival ticks |
| Sociable | +3 extra mood from an otherwise qualifying completed social visit |
| Green thumb | Prefers an available Farm when making a new job assignment; keeps an existing valid job |

Special events use a separate bounded Park–Miller stream. The first opportunity
is 12 simulation seconds ahead for a new run/migrated checkpoint; events last
25 seconds with a 15-second cooldown. Eligible choices are sampled only at
explicit simulation boundaries. A busy existing gathering defers a new special
event by five seconds without drawing RNG. At most one special event is active.
Identity, times, next opportunity, counter and RNG persist. No historical event
days are replayed on load.

| Event | Eligibility and actual effect |
| --- | --- |
| Rain shower | A populated town; restrained rain, 20% slower dirt/Garden travel; Stone stays quick; new relaxation visits prefer an available reachable Hall |
| Good harvest | A connected usable Farm; +25% on actual productive Farm time, no bonus for Workshop/vendor work or ownership alone |
| Market day | A present vendor and residents with available time; greater Market interest and +0.25 on the same daily qualifying trade transaction |

Changed preferences apply at decision boundaries, so a valid visit is not
abandoned when rain starts. Outdoor venues remain usable. Events do not reserve
a second actor or visitor slot: existing activity and capacity validation still
owns participation. Gathering invitations take priority at routine boundaries.
A removed/disconnected destination stops its invalid work/service or visit;
the selected event is not rerolled. Harvest can lapse with no valid workers;
a removed Market earns no trade. Construction Undo preserves event time,
legitimate earnings and daily credits.

A short announcement states the effect, remaining time and relevant build
choice. Its persistent compact label opens read-only event inspection after the
announcement shortens. Rain respects reduced motion; no new audio layer bypasses
existing mute settings. Harvest highlights a real productive farmer, while
Workshop hammer gestures and Market service use existing resident presentation.

## Source art

Original sheets, catalogue and audit images are unchanged. New buildings use
verified distinct 12×12 Urizen crops from
`assets/urizen/urizen_onebit_tileset__v2d0.png` (origin 1, pitch 13):

| Use | Source ID | Rectangle x,y,w,h | Treatment |
| --- | --- | --- | --- |
| Workshop | `urizen-onebit-r33-c005` | 66,430,12,12 | Warehouse miniature, slate tint; procedural working hammer cue |
| Market | `urizen-onebit-r33-c004` | 53,430,12,12 | Farm/fence miniature adapted as a stall with separately drawn four-stripe awning |
| Community Hall | `urizen-onebit-r33-c002` | 27,430,12,12 | Granary miniature, plum tint, covered-venue interpretation |

These are deliberately documented interpretations/compositions, not claims of
new authored Market or Hall art. Dirt/Stone/Garden surfaces reuse the verified
row-11 road crops c000–c012 and existing clipped cardinal end recipes. Stone and
Garden colours and small paving accents are derived renderer treatments using
the same connection geometry. The approved four resident bodies and palettes
are unchanged; source art stays static, with procedural activity motion.
Exact hashes/provenance are in the app asset manifest.

## Coordinated simulation and navigation

`Colony` retains a single fixed 0.05-second simulation clock. Movement/activity,
productive time, capacity checks, event boundaries and payments advance in that
clock; off-screen people use the same update path. Hidden-tab/focus suspension
keeps the existing no-catch-up policy. There are no per-resident Flutter widgets
or independent asynchronous timers.

Expensive decisions and replans use one deterministic FIFO with an eight-request
budget per fixed tick. Entries identify their navigation revision; stale entries
are discarded and requeued fairly. A monotonic cost revision invalidates reverse
weighted destination fields on committed edits, Undo/Redo, import and rain
changes. Tentative strokes do not touch navigation. A committed stroke coalesces
invalidation. Existing legal segments finish with their recorded duration; invalid
cells are never traversed while waiting for a replan.

Up to 96 reverse weighted fields are reused with LRU eviction. Cached job,
worker and visitor counts replace repeated whole-population scans. Derived
caches can be rebuilt; authoritative queue order and segment state are saved.
Import rebases fresh queue entries to a newer revision while retaining genuinely
stale entries, so invalidation does not change logical continuation.

Rendering culls off-screen foreground objects and people, while the model keeps
simulating them. Existing cached ground rasterization and shared sprite assets
remain. Profiling is opt-in with `?profile=1`; its bounded timing samples and
operation counters do not enter player storage or the normal UI.

Resident bodies are now drawn from a **single 416×260 atlas** in depth bands with
`Canvas.drawAtlas`. Each approved body/palette has an explicit mirrored copy;
nearest-neighbour 4× source pixels and a baked outline occupy a 52-pixel cell.
The foot anchor is (26,50), drawn at 9/48 world units per atlas pixel. The shadow
shares the atlas. Tool gestures, rings, speech and arrival accents still follow
real resident activity. The source bodies and colours are unchanged. Batching
approximates the old tiny body skew with a small rotation; it does not add new
source animation frames. Atlas layout, mappings and bytes are checked by tests
and the asset validator. This followed a measured rendering bottleneck after
model coordination and off-screen culling alone were insufficient at 500 people.

## Ownership and save compatibility

| File | Responsibility |
| --- | --- |
| `apps/settlement/lib/model.dart`, `rules.dart` | Tool catalogue, prices/capacities, physical road kinds, additive lot decoding and transactional charges |
| `lib/residents.dart`, `resident_identity.dart` | Real movement, frozen segments, stable names/traits and daily resident records |
| `lib/colony.dart`, `navigation.dart`, `special_event.dart` | Fixed-step routines, jobs/service/payments, bounded planning, weighted shared fields, events, validation and persistence |
| `lib/settlement_game.dart`, `main.dart` | Batched/cullable world rendering, real activity cues, inspection, grouped catalogue and input |
| `lib/browser.dart`, `web/platform.js` | Opt-in bounded profiling bridge; no player-storage writes from metrics |
| `tool/assets.py`, `assets/manifest.json` | Auditable derived crops and resident atlas |
| `test/tranche05_test.dart` | Focused new model, persistence, movement, event, queue and atlas contracts |
| `tool/tranche05_*`, `tool/scaling_*` | Explicit developer fixtures, browser/native journeys and release measurements |

Paths abbreviated to `lib/…` above live under `apps/settlement/`. Changes stay
inside that application and this documentation/evidence. The original Lua game
and the source artwork/catalogue are unchanged.

The existing schema-3 storage key remains `roag.settlement.landscape.v1`.
Ordinary Dirt/non-road lots retain their eight-field representation; only a
non-Dirt road adds a ninth field. Old five/six-field lots and older schema-3
checkpoints still load. Missing special-event fields schedule the first future
opportunity, without replaying historical days. Names/traits are assigned from
stable identity without replacing appearances or IDs. Existing migration backup
and failed-backup protection remain in place.

Authoritative queue order, cost revision, active segment metadata, new building
assignments, trade/Garden day records, event IDs/times/RNG and separate trade
income round-trip together. Derived navigation fields are rebuilt. Import
intentionally raises the navigation revision; the browser regression now compares
all gameplay fields while checking that fresh queue tags rebase and stale tags
remain stale. It no longer incorrectly requires byte identity for that explicitly
monotonic invalidation counter. Reload of a paused checkpoint still agrees exactly.

Construction history remains forward-moving: Undo restores the recorded layout
and charge, not clock, legitimate income, benefit records, selected events or the
ID allocator. No normal Lua profile/save, achievement or unlock system is involved.

## Measured release performance

Baseline: `6e36b7bd7221095e7fb20cd069d84b19b0740109`, with only the same opt-in
measurement hooks added to its frozen build. The [baseline suite](evidence/tranche-05/baseline-tests.log) passed 66 tests.
Both builds used Flutter 3.50.0 / Chromium 155.0.8059.84, software WebGL rendering,
1024×640 and fresh isolated browser contexts on this Linux machine. They imported
the same valid connected 40×30 fixture files: 85, 250 or 500 actual residents,
615 connected Dirt cells, ten Farms and five Parks. Unlimited construction was
used only to create the developer fixtures; population counters were not inflated.
Special events were held beyond the measurement window to isolate comparable
navigation workloads. The separate 85-person gameplay journey exercises the new
venues and a natural event.

Each workload ran for 16 wall seconds ordinarily, then 16 after a brush removed
a **13-cell** road strip. The FIFO budget and weighted routes are intentional
model changes; before/after states need not be byte-identical. Browser animation
frame intervals are observed scheduling intervals, not physical display latency.
Model means measure `Game.update` calls, including frames with no fixed tick.

| Residents / phase | Baseline frame median / p95 ms | Final frame median / p95 ms | Baseline model mean ms | Final model mean ms |
| --- | --- | --- | --- | --- |
| 85 / ordinary | 16.7 / 16.8 | 16.7 / 33.3 | 0.422 | 0.187 |
| 85 / recovery | 16.7 / 16.8 | 16.7 / 16.8 | 0.834 | 0.118 |
| 250 / ordinary | 50.0 / 83.4 | 16.7 / 49.9 | 6.449 | 0.442 |
| 250 / recovery | 50.0 / 100.0 | 16.7 / 33.4 | 16.717 | 0.395 |
| 500 / ordinary | 66.7 / 133.3 | 16.7 / 33.4 | 10.442 | 0.610 |
| 500 / recovery | 100.1 / 183.3 | 16.7 / 50.0 | 48.789 | 0.925 |

The final run encountered unrelated concurrent build/browser load on the shared
machine. Its 85-person ordinary p95 is worse than baseline despite lower model
cost; these data do not establish an improvement on every frame. An earlier
atlas build on the same machine measured 16.7/16.8 ms at 500 in both phases,
with model means 0.430/0.394 ms. Raw earlier, intermediate-culling and final
results are retained; the table uses the final release, rather than selecting the
best run. Results are observations from this machine, not cross-device guarantees.

Model coordination/cache work plus culling first reduced 500-person recovery
model cost to 0.719 ms, but median/p95 frames remained 33.3/50 ms. That separated
the remaining rendering bottleneck from model work and justified the resident
atlas batching. No general benchmarking framework or engine replacement was added.

| Residents | Final recovery queue peak → end | Cumulative route queries | Built fields / cache hits | Expanded cells | Sim seconds in recovery |
| --- | --- | --- | --- | --- | --- |
| 85 | 37 → 0 | 2249 | 186 / 2063 | 111245 | 16.15 |
| 250 | 202 → 0 | 7520 | 535 / 6607 | 320293 | 16.20 |
| 500 | 444 → 0 | 13883 | 1056 / 12302 | 632346 | 15.90 |

Counters are cumulative at the end of recovery, including ordinary operation.
The baseline used synchronous per-person BFS and had no shared-field cache or
planning backlog; only its route query count was available. At 500, it made
17,034 route queries and advanced 6.90 simulation seconds during recovery;
the final build advanced 15.90. No timing assertions are in unit tests. A focused
deterministic test explicitly queues stale work before a 500-person edit,
observes its rejection and verifies eventual progress for every resident.

Raw data: [baseline](evidence/tranche-05/performance-before.json),
[coordination/culling](evidence/tranche-05/performance-culling.json),
[earlier atlas](evidence/tranche-05/performance-atlas-earlier.json),
[final release](evidence/tranche-05/performance-final.json).
Reproduce fixtures with `dart run tool/scaling_fixture.dart`, then
`SETTLEMENT_URL='http://127.0.0.1:8768/settlement/?profile=1' node tool/scaling_browser.cjs /tmp/scaling.json`
from `apps/settlement/`. Use the same browser/machine and keep other work idle
when possible. Baseline files must be frozen separately before rebuilding.

## Verification and actual browser evidence

The final suite has **93 passing tests**, including all previous 66 and 27 focused
new contracts. No baseline game-test failures or final regressions remain.
The focused tests cover actual productive income, vendor-dependent service,
capacity/daily credits, all three trait effects, real arrival times, frozen
segments, weighted routes, paid road history/budgets, daily traversal, Hall
preferences/gatherings, all event eligibility/boundaries/rewards, strict additive
save validation, stale request rejection, queue fairness, render-rate equivalence
and atlas physical mappings. The same colony is replayed at 30 and 144 Hz;
presentation/camera state is not an input to the authoritative model.

Run from `apps/settlement/`, with Flutter/Dart and the pinned Playwright tool
dependencies available. Start a separate local test server first with
`python3 -B tool/serve.py --port 8768`:

```sh
flutter test
flutter analyze
dart format --output=none --set-exit-if-changed lib test tool/scaling_fixture.dart tool/tranche05_fixture.dart tool/tranche05_trace.dart
python3 -B tool/assets.py --check
flutter build web --release --base-href /settlement/ --no-web-resources-cdn
dart run tool/tranche05_fixture.dart
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche05_browser.cjs /tmp/roag-tranche05-browser-final
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche05_settings.cjs /tmp/roag-tranche05-settings-final
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/browser_test.cjs /tmp/brush
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche03_edges.cjs /tmp/edges
```

The settings check imports the safe `large-state.json` exported by the preceding
journey. These are developer verification tools with explicit `/tmp` fixture
paths; they are not runtime event controls. `NODE_PATH` can point to an isolated
Playwright installation. This run used `/tmp/flutter/bin` and
`/tmp/settlement-browser/node_modules`, without changing project dependencies.

| Check | Result / evidence |
| --- | --- |
| Full tests, static analysis, format | 93 passed; no analysis issues; 24 Dart files already formatted. [tests](evidence/tranche-05/tests.log), [analysis](evidence/tranche-05/analyze.log) |
| App/source assets | 27 original crops, four bodies × four palettes, mirrored atlas and manifest verified. Root `python3 -B tools/build_classic_assets.py --check`: 599 bindings from 67 unchanged source PNGs |
| Release build | Passed; existing local release archive rebuilt. [build log](evidence/tranche-05/build.log) |
| Main browser journeys | Paid fresh eight-person town, all additions, mixed roads, natural Rain, real Workshop/vendor/service/social activity, forward history, read-only names/traits, two different overlapping people, event reload/import, resize/pan/zoom and accelerated play. [results](evidence/tranche-05/browser-results.json) |
| Safe 85-person journey | Imported a copy of the earlier committed verification fixture; added new buildings with actual funds, natural Rain and a nine-cell paid Stone upgrade at 1×; actual work/trade/social records checked. [checkpoint](evidence/tranche-05/populated-checkpoint.json) |
| Native X11 / XTest | Actual mouse/keyboard, Ctrl-plus 110% zoom, native tab focus-loss cancellation, drag painting, new shortcuts, pan/wheel, catalogue scrolling, I/Escape, real work, footer bounds and hidden-tab pause/no catch-up. [results](evidence/tranche-05/native-results.json) |
| Existing brush/input/audio | Passed; exact construction/history, unsupported import, refresh, resize, pointer-anchored zoom and running user-gesture audio context/mute. [results](evidence/tranche-05/brush-results.json) |
| Existing edges/persistence | Passed; DPR 1.5 picking, cancellation, speech-layer input, storage failure, legacy backup/failed-backup protection, night repair and active reload. [results](evidence/tranche-05/edge-results.json) |
| Reduced motion / event inspection | Browser media preference during real active Rain hides the new rain motion; opening/closing persistent event inspection leaves the paused checkpoint unchanged. [results](evidence/tranche-05/settings-results.json) |

Native tests used a fresh Chromium profile under Xvfb, **not** headless callbacks:
`DISPLAY=:98 XDOTOOL_BIN=/tmp/roag-runtime/usr/bin/xdotool SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche05_native.cjs …`.
The executable/runtime path is machine-specific. CDP observes state/screens and
performs one accessibility setup; game actions use XTest. Other journeys use
Playwright mouse/keyboard and actual menus/file selection, plus read-only saved
checkpoints. They do not invoke game methods. Unit tests are separate headless
model verification. This is automated interaction and rendered-frame inspection,
not a human balance/preference test or a subjective claim that the game is fun.

Harvest and Market-day verification explicitly imports developer fixtures made
with `beginSpecial(..., fixture: true)` after real work/service eligibility.
Natural Rain is independently demonstrated twice at seed 202. That fixture-only
seam has no playable button and is never used by the runtime scheduler.

During verification, native screenshots exposed a tiny resident portrait and a
long Hall selection that clipped Redo at 110%; both were corrected and retested.
The stronger overlap assertion caught a test precondition with only one nearby
person; it now selects a real overlapping group and proves the IDs differ.
The old import byte-equality assumption was updated for the required monotonic
navigation revision, while retaining full gameplay-state checks. A headless
browser did not hide its page when another page opened, so hidden-tab simulation
verification was moved to actual native tab switching. One SDK test run hit a
`/tmp` disk quota; removing the redundant downloaded SDK archive resolved it,
and the complete suite was rerun. None of these is reported as a baseline game
failure. No required final check is left failing.

## Moving and readable evidence

- [23-second continuous populated gameplay](evidence/tranche-05/populated-rain-stone.mp4):
  85 real residents at 1×; natural Rain begins, nine Stone cells are painted for
  18 coins, and subsequent real journeys/work/service continue. This is a direct
  crop of seconds 110–133 of the browser capture, with no cuts or speed changes.
- [33-second continuous small-town work/service journey](evidence/tranche-05/paid-build-service.mp4):
  the fresh paid town, natural rain, mixed road upgrades, productive work and
  service. Direct seconds 14–47 of the same capture, no speed changes.
- [Consecutive rendered gameplay frames](evidence/tranche-05/gameplay-frames.png)
  extracted every two seconds from the populated clip and inspected for movement,
  rain and the committed road change. The videos contain actual game rendering,
  not a separate mock animation. MP4 conversion is silent; audio verification is
  the separate instrumented running AudioContext/mute check.
- [Populated rain](evidence/tranche-05/populated-natural-rain.png),
  [paid Stone response](evidence/tranche-05/populated-stone-response.png),
  [subsequent activity](evidence/tranche-05/populated-service.png).
- [Named resident card](evidence/tranche-05/resident-card.png) and
  [first selected person in the same overlapping group](evidence/tranche-05/resident-cycle-first.png).
- [Market staffing, service and exact trade rules](evidence/tranche-05/market-inspection.png),
  [fixture-assisted harvest](evidence/tranche-05/harvest-fixture.png),
  [fixture-assisted Market day](evidence/tranche-05/market-day-fixture.png),
  [reduced-motion Rain inspection](evidence/tranche-05/rain-reduced-motion-inspection.png).
- [Grouped catalogue](evidence/tranche-05/catalogue.png),
  [1440×900](evidence/tranche-05/desktop-1440.png),
  [native 110% Hall/footer](evidence/tranche-05/native-hall-selected.png),
  [native 110% scrolled catalogue](evidence/tranche-05/native-catalogue-110-bottom.png).

The videos start from safe developer towns; the user's private browser profile
was never opened or reset. The fixture exports in this folder reproduce the
small and populated journeys. All test contexts use separate storage on ports
8767/8768; the local 8765 origin remains the player's origin.

## Protection and limits

All **six README.md files have identical file sets and SHA-256 hashes** to the
initial checkout. Root README SHA-256:
`3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a`.
The four normal Lua player-storage files and their file set are unchanged.
Original root artwork, Lua sources, catalogue/audits and all original app image
crops remain byte-identical. [Protection fingerprints](evidence/tranche-05/protection.json)
record the comparisons. No normal saves/profiles were loaded, no achievements or
unlocks touched, and nothing was deployed or republished. The serving workflow
and archive are local only.

There are no known remaining regressions from these checks. Verification covers
this Linux/Chromium environment; other browsers/devices and subjective enjoyment
remain unverified. Dense venues can still create visible waiting crowds, although
service/visitor capacity is authoritative and bounded. A single Market serves
only three customers at a time; more venues and better access are real build
choices. The 96-field cache and eight-decision budget are deliberately bounded;
a bulk edit may briefly leave people waiting for replanning. Names can repeat
from the small local pool in very large towns; resident IDs remain distinct.
The modest trade and road/mood values need human balance testing alongside the
existing daily grant. Performance is observational, not a promise of frame rate.

Human playtest questions:

1. Do I have several appealing and understandable things to build?
2. Do the new destinations make different parts of town feel different?
3. Can I understand an event and respond mainly by building?
4. Do resident names and traits make me notice individual people?
5. Does the game remain responsive as the population grows?
6. After watching something happen, do I naturally want to build the next thing?
