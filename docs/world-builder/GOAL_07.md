# Goal 07 · Seeded landscapes and public browser release

ROAG Settlement now has seeded environments, meaningful terrain, landscape tools
and completed water crossings. The existing residents, buildings, routines,
conversations, civic choices, finances and forward-moving construction history
remain in the Flutter/Flame application. No individual orders or new resources
were added. Goal 08 has not been started.

## Play

Public: <https://gabrielongzm.com/roag/settlement/>. The repository's
<https://gongahkia.github.io/roag/settlement/> address redirects to this existing
account-level domain. HTTPS is enforced. This is GitHub Pages; no ChatGPT Sites
hosting or paid resource was created.

The existing local server can serve the rebuilt game at
<http://127.0.0.1:8765/settlement/>. If it is not running, from the repository root:

```sh
python3 -B apps/settlement/tool/serve.py --port 8765
```

The refreshed local archive is `artifacts/settlement/roag-settlement-web.zip`;
its files match `build/web/` and use the local `/settlement/` prefix.

An occupied port means another server may already be available. Use `--port 0`
for a separate server; its browser origin has separate storage. To build a fresh
checkout with Flutter **3.50.0**, Dart **3.14.0**, Flame **1.38.2** and
material_ui **1.11.0**:

```sh
cd apps/settlement
flutter pub get --enforce-lockfile
python3 -B tool/assets.py --check
flutter build web --release --base-href /settlement/ --no-web-resources-cdn
python3 -B tool/serve.py --port 8765
```

A fresh browser starts in Gentle Plains, seed `202`, with a short dismissible
introduction. Saved settlements open first. **Landscape menu → New landscape…**
lets you choose an environment and enter a numeric or text seed, or generate one.
The description and replacement warning appear before Create. Export current
copy preserves the current town; Continue existing settlement cancels the draft.
Import separately validates its file and asks before replacing the active town.
Both replacement screens offer export. Opening, editing or cancelling a draft
never replaces the town. The normal simulation clock can continue behind menus.

**World information & seed** in the same menu shows the preset, version, terrain
rules and a selectable, copyable seed. Seed entry is optional during ordinary play.

Controls:

- Click/drag and release commits a stroke. Escape cancels. Ctrl/Command Z and
  Shift Z undo/redo construction while simulation time and earned income stay forward.
- `1/C` Cottage, `2/P` free Path, `3/R` Remove, `4/G` Farm, `5/V` Park;
  `6` Workshop, `7` Market, `8` Community Hall, `9` Stone Road, `0` Garden Path.
- `B` opens the scrolling catalogue. Its **Landscape** category contains Clear,
  Plant and Bridge, with real sprite thumbnails, prices and short rules.
- `I`/Alt-click inspects people, buildings, terrain and infrastructure. Terrain
  cards explain preparation and traversal. Bridge cards distinguish a complete
  crossing from a broken one. Road cards describe wet-ground/Stone effects.
- Inspect a building and choose Move (`M`), or short right-click for its context
  menu. Relocation remains free and preserves the building and resident identity.
- Space/right/middle drag pans; scroll or +/− zooms; `H` returns to the lane.
  Pause/1×/2×/4× and `T` retain their existing meanings. Help remains in the menu.

## Generation version 2

New worlds are 40×30 cells. The seed, preset, generator version and fixed
configuration determine the initial concrete landscape. New-world creation is
synchronous and reuses the loaded renderer/assets.

Seeds trim surrounding whitespace and accept 1–64 UTF-16 code units, rejecting
control characters. Signed decimal integers are canonicalised (for example
` +000202 ` becomes `202`) and reduced modulo **2,147,483,647**. Other text is
case-sensitive UTF-8: start at zero, then `n = (n × 131 + byte) mod 2147483647`.
Internal spaces and Unicode code-point sequences are significant. Equivalent
numeric seeds modulo the prime share terrain; the canonical entered text is
retained for display. The finite seed space can have collisions.

Version 2 samples three independent 9×7 coarse grids and interpolates integer
values over five-cell intervals. The forest grid ranks eligible open land into
coherent patches. Preset coverage limits keep Plains open and Woodlands forested,
while preserving the starting clearing. Separate wet and rock fields provide
small difficult patches. Ties use the existing bounded coordinate variation.
No floating-point noise library or platform-dependent hash is used.

| Preset | Forest share of eligible unprotected open ground | Wet / rock thresholds on 0–99 fields | Geography |
| --- | --- | --- | --- |
| Gentle Plains | 16% | 9 / 4 | Broad meadows, scattered forest patches, few obstacles |
| Woodlands | 50% | 12 / 5 | Larger woodland areas around smaller natural clearings |
| River Country | 20% | 18 / 4 | One continuous north–south river, wet banks and useful bridge approaches |

Thresholds are field cutoffs, not independent per-tile probabilities. Counts also
reflect water, protected ground and interpolation. For seed `202`, Plains has
1,019 ground / 175 forest / 3 wet / 3 rock cells; Woodlands 644 / 545 / 7 / 4;
River Country 862 ground / 183 forest / 77 wet / 76 water / 2 rock.

All presets protect an 11×9 starting clearing and the eight-cell arrival lane
at zero-based row 15. The river is two or three cells wide. Its centre moves at most one
cell at four-row boundaries, with overlapping channel rows. A clear three-row
starter approach and far-bank bridgehead allow a first crossing for at most
nine coins, without an isolated arrival or unaffordable escape route.

Forest/wet/rock grids use separate streams with domain offsets 3001, 7001 and
11003. River geometry uses 32452843; scenery 104729; initial variant metadata
130363. The streams use bounded Park–Miller arithmetic. Existing later gathering
and special-event streams retain their separate offsets and persisted states.
The initial variant has no economic or weather bonus. Camera, drawing, inspection,
previews, menus and effects do not draw from these streams.

The first release candidate used linear field sampling. A strengthened test
compared terrain classifications, excluding seed labels, and exposed repeated
layouts: only 441 unique Plains patterns for 600 inputs. Version 2 replaced that
sampling; all 600 terrain maps per preset now differ. Version-1 worlds already
created remain concrete saved towns and are explicitly covered by compatibility
tests. New World uses version 2; loading never reconstructs either version.

## Terrain, tools and navigation

Residents retain the existing road-based travel model; open grass is not an
imaginary shortcut around infrastructure.

| Terrain | Buildings | Ordinary roads | Resident traversal | Preparation / weather |
| --- | --- | --- | --- | --- |
| Open ground | Yes, beside connected road | Yes | On roads | Ordinary existing road/weather effects |
| Forest | No | No | No road through uncleared trees | Clear removes the tree and prepares ground |
| Wet ground | No | Yes | On roads, 1.5× travel time | Clear prepares building ground; rain uses the larger penalty |
| Water | No | No | Only on a complete valid bridge | Cannot be cleared, drained or planted |
| Rock | No | No | Impassable | Build around it; Clear does not remove it |

Legacy trees remain blockers and can be cleared. Existing decorative crops
remain replaceable by ordinary construction. Terrain inspection and invalid
placement messages explain the same rules used by execution.

- **Clear: 1 coin per changed cell.** Remove suitable trees or prepare wet ground.
  Occupied cells, ordinary empty ground, water and rock are rejected. Earlier
  pads/scars are retained; newly prepared land gets a scar. This does not earn resources.
- **Plant: 1 coin per changed cell.** Plant the existing oak on empty ordinary
  ground, preserving prior traces. It becomes forest and obstructs construction.
  Roads, buildings, existing vegetation and other terrain are rejected. There is
  no automatic happiness or income from planting.
- **Bridge: 3 coins per newly built water cell.** Drag straight from clear land
  to clear land across 1–6 uninterrupted water cells. Both approaches must be
  valid; one must join the arrival-connected network. Land approaches become
  free paths. Horizontal and vertical spans work; bends and floating ends do not.

A bridge stroke validates the whole crossing against the committed world. Its
preview remains provisional, and an incomplete/invalid/unaffordable stroke commits
nothing. Release creates one history entry with the exact price. Unused partial
water segments cannot supply access. Existing valid water cells cost nothing
when repairing a gap; missing water cells pay three each, and approaches stay free.

Removing any required bridge cell or approach invalidates the entire surviving
span. Remnants stay visible in a muted tint. They provide no traversal or fictional
building access. Residents finish only legal movement, step locally to surviving
infrastructure where possible, or wait; invalid productive presence stops.
Rebuilding the complete crossing restores normal routing and work. No teleport,
arrival income or special bridge-only resident controller was added.

All landscape strokes use the shared captured budget, deduplication, cancellation,
commit, affordability and 80-entry history system. Clear/Plant accept eligible
cells within the captured stroke budget; all accepted changes commit together.
Undo restores concrete terrain and the recorded charge once. Redo rechecks funds.
Demolition refunds nothing; restoring demolition does not invent a refund.
Reload/import retains the established session-local-history model.

Navigation and actual movement share endpoint-mean travel costs. On Dirt/Garden
roads, wet ground costs 1.5× and rain 1.25×; use **max**, not a product. Stone
retains its 18% speed advantage across wet ground and rain. A wooden bridge uses
ordinary path pace and its rain effect. Mixed endpoints use their mean cost;
Brisk remains a separate constant resident multiplier. The existing frozen current
segment metadata, Garden Path traversal benefit and future-route replanning remain.

Terrain edits invalidate navigation once per committed stroke, not per preview
cell. Shared reverse Dijkstra fields, the 96-field LRU and the eight-request
planning budget remain. Movement checks one proposed edge directly; it no longer
constructs/enumerates a neighbour iterator just to validate that edge.

## Rendering and asset ownership

The existing muted pixel palette, nearest-neighbour rendering, cottages, residents,
body/palette atlas, depths and procedural activity motion remain. Forest uses the
existing oak over muted ground. Marsh uses restrained grass/water marks; rivers
use an opaque blue-green bed, audited ripples and one-pixel shore compositions.
Rock and bridges use four newly exported **existing** Urizen crops:

| Runtime frame | Audited source ID | Source rectangle |
| --- | --- | --- |
| water | `urizen-onebit-r12-c001` | `[14,157,12,12]` |
| boulder | `urizen-onebit-r13-c004` | `[53,170,12,12]` |
| bridge-h | `urizen-onebit-r40-c011` | `[144,521,12,12]` |
| bridge-v | `urizen-onebit-r40-c010` | `[131,521,12,12]` |

The source is the unchanged `assets/urizen/urizen_onebit_tileset__v2d0.png`.
[Terrain](contact-sheets/terrain-2.png) and [routes](contact-sheets/routes-2.png)
were inspected before choosing crops. The existing exporter records exact IDs,
rectangles and hashes, preserves its black-key/tint convention and checks every
product. No atlas rectangle, animation sequence, source art or catalogue record
was invented or edited. Water is deliberately static; this adds no per-frame
water simulation. Existing audio/mute and reduced-motion behaviour remain.

The ground raster still caches by immutable world identity. Hover reuses plans
for the same cell/world/budget. Placement affected-cell geometry is cached on
that immutable plan, including workplace spurs; it is not recomputed every draw.
The colony continues using the committed world while a preview is staged.

## Saves and protection

The existing browser key and schema 3 remain. Generation metadata and terrain /
bridge fields are additive. Old eight-/nine-entry lot records decode as ordinary
legacy ground with their original scenery, roads, buildings, traces and IDs.
Non-ground/bridge lots have explicit surface, terrain and axis fields. Decoder
checks forbid overlapping buildings, forest roads, ordinary water roads and
inconsistent bridge footprints.

Checkpoints persist concrete lots, seed metadata, world clock, money, residents,
needs, experiences, requests, civic decisions, events, RNG and underway segments.
No generator is called to rebuild decoded lots. Unknown newer generation numbers
can retain known concrete terrain; malformed metadata/content is rejected.
Existing schema-1/2 backup safeguards remain. Invalid imports and cancelled
replacement dialogs leave the active town in place. There are no retroactive
terrain fees or inserted rivers beneath old buildings.

Tests use new browser contexts/profiles and separate loopback origins. The user's
normal browser profile was not opened or cleared. The default local player origin and all four normal Lua storage files are
preserved. Verification used separate servers; the rebuilt release is available
on the default local port at handoff. All six README.md paths and
hashes match the recorded initial checkout; no README was created or edited.
Root SHA-256: `3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a`.
Original Lua source/launchers, root art/catalogues, old app PNGs and unrelated
initial working changes were preserved. [Protection fingerprints](evidence/goal-07/protection.json)
record the final comparison.

## Public release and CI

[Settlement workflow](../../.github/workflows/settlement.yml) installs pinned
Flutter 3.50.0, enforces the dependency lockfile, checks assets/formatting/analysis,
runs the complete settlement suite, builds self-contained static assets and then
publishes the successful artifact through official Pages upload/deploy actions.
It runs automatically on relevant pushes to `main` and
`release/settlement-goal-07`, on pull requests without publication, and by dispatch.
Deployment uses Pages/OIDC permissions; no token is committed.

`tool/release.sh /roag/settlement/` checks the toolchain and builds
`build/release-web/` plus `build/site/settlement/`, keeping the local
`build/web/` `/settlement/` release usable. A relative root redirect and `.nojekyll`
make `/roag/` lead to the game. Fonts and CanvasKit are served locally; there is
no external web-resource CDN requirement. The app uses its single static entry
point, without client routes requiring a server rewrite. GitHub serves the usual
600-second cache policy; hard-refresh after a release if an older client remains
cached. The hosted `main.dart.js` is byte-identical to the locally verified Pages build;
[SHA-256 comparison](evidence/goal-07/release-binary.json) records both hashes.
No service-worker migration or player-storage reset is required.

For a local test of the exact Pages path:

```sh
cd apps/settlement
FLUTTER_BIN=flutter bash tool/release.sh /roag/settlement/
python3 -B tool/serve.py --port 8769 --base-path /roag/settlement/ --build-dir build/site/settlement
```

The release branch was created through a separate Git index; the user's main
checkout, staging area and unrelated work were not switched, reset or committed.
The existing account's custom domain is reused. The initial publication was
rejected by GitHub's default main-only environment rule; allowing the exact
release branch alongside main resolved it. HTTPS was enabled with the already
approved domain certificate. Publication succeeded, and the actual canonical URL
was opened in Chromium. The hosted version-2 client created four cottages and
welcomed their residents, rejected a malformed import without changing that town,
then imported the validated 500-person River fixture through the normal warning.
Work income increased after panning away from its workplaces, and a paused reload
retained concrete terrain and residents. No browser error or failed asset request
was observed. [CI run](https://github.com/gongahkia/roag/actions/runs/43724271497)
builds and publishes generation version 2 automatically.

The independent **Classic roguelike and artwork** workflow still fails its global
tracked-media catalogue assertion before its Lua gameplay step. This was reproduced
using the unchanged original test and initial HEAD's tracked-file list: **165**
pre-existing app/contact-sheet/evidence media were already unregistered there.
[Baseline evidence](evidence/goal-07/baseline-catalogue.json) records that exact
check; Goal 07 also adds derived app/evidence images outside the frozen canonical
catalogue. No assertion/catalogue was weakened to hide this failure. Settlement
asset export validation passes, and all **599 classic bindings / 67 original PNGs**
pass their own unchanged asset check. The Lua gameplay/GUI suite was not rerun
locally; unchanged source/storage fingerprints do not claim those tests passed.

## Verification and performance

Baseline: **118** settlement tests passed before editing at
`75c3798b88ea7d5698012208dc4ce714a2a19f24`, with initial work-tree differences
captured separately. Final: **134 tests**, no analyzer issues, clean Dart
formatting, all **32** source crops plus body/palette atlas/font products checked,
and successful local and Pages-prefix production builds.

New checks cover 600 seeds × three presets, concrete reproducibility and genuine
terrain variation, arrival/early expansion, affordable connected river crossings,
seed normalization, RNG isolation, terrain restrictions, clearing/planting,
budgeted strokes/cancellation/history, complete/invalid/broken/repaired and
vertical bridges, aligned edges, weighted routes, exact movement costs,
weather/Stone composition, stale cache revisions, actual crossing/work cessation
and recovery, terrain inspection, different frame cadences, new and legacy saves,
unknown-version concrete persistence and New World draft cancellation/export.
The same 85-person no-choice replay remains byte-identical over 180 simulation
seconds and 36 full checkpoints, including event/RNG/resident state ([hash equality](evidence/goal-07/equivalence.json)).

Commands from `apps/settlement`:

```sh
python3 -B tool/assets.py --check
dart format --output=none --set-exit-if-changed lib test
flutter analyze
flutter test
dart run tool/goal07_fixture.dart
dart run tool/tranche06_replay.dart /tmp/goal07-replay.json
NODE_PATH=/tmp/settlement-browser/node_modules SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/goal07_browser.cjs /tmp/goal07-browser
DISPLAY=:98 LD_LIBRARY_PATH=/tmp/roag-runtime/usr/lib XDOTOOL_BIN=/tmp/roag-runtime/usr/bin/xdotool SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/goal07_native.cjs /tmp/goal07-native
```

Playwright 1.65.1 is installed outside the repository; Chromium 155 and Node
26.12.0 were used. The XTest harness requires an isolated Xvfb display. It uses
raw CDP for observations/accessibility setup, then native pointer/keyboard for
all game interactions. These are automated playtests, not a human preference study.

The full browser journey uses ordinary Playwright pointer/keyboard, real menus,
file downloads/chooser and read-only checkpoint assertions. It continues an old
85-person town unchanged; cancels new/import drafts; creates text-seeded Plains
and a functioning neighbourhood; repeats that seed; clears and builds Woodlands;
constructs, removes and repairs a River crossing; observes actual work stopping
and resuming; tests wet Stone roads/inspection, history, reload/export/import,
relocation, pan, zoom and 1440/1024/931-CSS layouts. The earlier developed-town
regression journey passes relocation, actual work, dialogue/needs/promises and
both civic branches. Native XTest separately verifies Woodland/River creation,
paid clearing/history, bridge cancellation/construction, crossing and work at
**110% browser zoom (931×582 CSS in a 1024×640 viewport)**. Re-selecting the current
1× button checkpoints live positions for sub-second crossing observations; it
does not call a game callback or assign a resident.

Terrain-generation timings are standalone Dart VM observations over 100 seeds
per preset: median 0.733 / 0.950 / 0.802 ms for Plains / Woodlands / River;
p95 2.437 / 1.231 / 1.617 ms. The largest cold sample was 10.502 ms. These are
model timings, not physical input/display latency or browser frame guarantees.

| Actual residents | Before model mean ms, ordinary / recovery | Final model mean ms, ordinary / recovery | Before frame p95 ms | Final frame p95 ms |
| --- | --- | --- | --- | --- |
| 85 | 0.175 / 0.237 | 0.115 / 0.113 | 33.4 / 49.9 | 16.7 / 16.7 |
| 250 | 0.240 / 0.235 | 0.255 / 0.240 | 33.2 / 16.8 | 16.7 / 16.8 |
| 500 | 0.460 / 0.373 | 0.486 / 0.449 | 33.3 / 33.3 | 16.8 / 16.8 |

Median observed frames remain 16.7 ms throughout. The first pass measured 0.605 /
0.553 ms at 500; the final pass removes avoidable single-edge enumeration while
retaining terrain/bridge checks. Relative to baseline, final 500-person recovery
still adds 0.076 ms mean model time; routine play adds 0.026 ms. These small
absolute costs and variable frame tails are reported rather than claiming exact
performance equality. The final runs were collected without concurrent builds or
other browser journeys.

The generated 500-person River fixture measures 0.556 / 0.531 ms model means and
16.8 / 16.8 ms frame p95. Its recovery queue peaks at 436 and ends at zero;
1,081 fields, 11,931 cache hits and 672,537 expansions are cumulative. Ordinary /
recovery simulations advance 16.0 / 15.9 seconds in the 16-second windows. The
legacy 250-person sample ends with two newly queued routine requests; both
500-person samples end with no pending requests. No timing assertion hides this.

[Before](evidence/goal-07/performance-before.json),
[initial pass](evidence/goal-07/performance-initial.json),
[final](evidence/goal-07/performance-after.json) and
[generated River](evidence/goal-07/performance-terrain-500.json) measurements
retain the full frame/model/presentation/queue data.

The first performance pass found avoidable movement neighbour enumeration;
validation now checks the proposed edge directly. Existing scaling fixtures are
compared at 85, 250 and 500 actual residents, with 16 wall-seconds ordinary play
and 16 after a 13-cell road removal. A separate 500-person generated River fixture
has 38 bridge cells and 628 connected road cells. It uses Unlimited construction
only to prepare the explicit developer fixture; the playable rules are unchanged.
Fixtures use ordinary placements and strict round-trip validation, not population
counters or injected earnings. Shared-machine frame tails vary; no physical
latency/60-fps guarantee is claimed.

Reproduce with `tool/scaling_browser.cjs`; `SETTLEMENT_FIXTURE_DIR` and
`SETTLEMENT_POPULATIONS=500` select the generated river fixture. `?profile=1`
uses the existing bounded measurements. No new telemetry framework was added.

## Evidence

- [Continuous 40-second world creation → clearing/building → river bridge → resident work](evidence/goal-07/worlds-and-crossing.mp4).
  Actual running release, seed `202`, Woodlands then River Country. No cuts,
  synthetic frames or speed changes in the recording conversion; the normal
  game speed controls are visible. Brief 900-ms world-review and 700-ms staged
  preview holds are harness reading time, not mandatory game delays. Silent video.
- [Rendered motion frames](evidence/goal-07/moving-frames.png),
  [story checks/timeline](evidence/goal-07/story-results.json).
- [Woodland](evidence/goal-07/04-woodlands-created.png),
  [staged clearing](evidence/goal-07/05-woodland-staged.png),
  [prepared neighbourhood](evidence/goal-07/06-woodland-neighbourhood.png).
- [River](evidence/goal-07/07-river-created.png),
  [bridge preview](evidence/goal-07/08-bridge-preview.png),
  [actual crossing](evidence/goal-07/10-actual-crossing.png),
  [broken span](evidence/goal-07/12-crossing-cut.png),
  [repair](evidence/goal-07/14-crossing-repaired.png),
  [wet-road inspection](evidence/goal-07/15b-wet-road-inspection.png).
- [Native clearing](evidence/goal-07/native-clearing-preview-110.png),
  [native crossing](evidence/goal-07/native-residents-crossing-110.png),
  [native actual work](evidence/goal-07/native-river-working-110.png).
- [Public browser](evidence/goal-07/public-browser.png),
  [public checks](evidence/goal-07/public-smoke.json),
  [hosted 500-person River](evidence/goal-07/public-500-river.png),
  [main journey](evidence/goal-07/browser-results.json),
  [native checks](evidence/goal-07/native-results.json),
  [developed-town regression](evidence/goal-07/regression-results.json),
  [tests](evidence/goal-07/tests.log), [baseline](evidence/goal-07/baseline-tests.log),
  [analysis](evidence/goal-07/analyze.log), [build](evidence/goal-07/release-build.log),
  [generation timing](evidence/goal-07/generation-timing.json).
- [Seeded saved River town](evidence/goal-07/river-town.json) and
  [version-1 compatibility town](evidence/goal-07/legacy-v1-river.json) are synthetic
  verification exports, not the user's private settlement.

## Implementation owners

| File | Responsibility |
| --- | --- |
| `lib/terrain.dart` | Terrain/preset rules, seed normalization, versioned metadata |
| `lib/world_generation.dart` | Independent grids/streams, protected start, coherent river and preset coverage |
| `lib/model.dart` | Concrete terrain/bridge codec, graph validity, placement, complete crossings, shared transaction/history and preview geometry |
| `lib/navigation.dart` | Shared terrain/weather/material costs in weighted fields |
| `lib/residents.dart`, `lib/colony.dart` | Direct legal-edge checks and strict checkpoint route validation; existing scheduling, finance and needs retained |
| `lib/world_creation.dart`, `lib/main.dart` | Isolated draft, replacement/export protection, world info/seed, introduction, catalogue |
| `lib/build_art.dart`, `lib/settlement_game.dart` | Shared ground/bridge compositions, cached raster/preview and truthful inspection |
| `tool/assets.py`, `assets/manifest.json` | Exact additional source crops and deterministic export validation |
| `test/goal07_test.dart`, `tool/goal07_*.{dart,cjs}` | Seed sweep, integrations, valid population fixtures and actual browser/native/video journeys |
| `tool/release.sh`, `tool/serve.py`, `.github/workflows/settlement.yml` | Pinned build, local/public prefixes, static release and automated publication |

## Limits and human playtesting

No required settlement/deployment blocker remains after the checks above. The
separate catalogue CI failure is explicitly retained above. Firefox, Safari,
mobile/touch ergonomics and very large imported maps beyond the tested fixtures
remain unverified. Terrain generation is fixed at 40×30; this is not an infinite
world or survival campaign. Rock and water are not terraformable. Bridges are
straight, short spans; no long/curved elevated road system was added. Water is
static, and people retain the existing bounded overlap/automatic routines.

Density, clearing prices, river-crossing cost and casual enjoyment need human
preference testing. A useful far-bank workplace and an inexpensive first crossing
are available, but automated play does not establish balance or lasting enjoyment.

- Do different seeds produce settlements that feel meaningfully different?
- Does the terrain influence where I want to build?
- Are bridges and clearing satisfying rather than frustrating?
- Does the default landscape remain pleasant for casual building?
- Can I understand what terrain allows without consulting documentation?
- Does the settlement still feel alive and responsive?
