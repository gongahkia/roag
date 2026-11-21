# Goal 11 · A connected region

**Implemented and verified.** Goal 12 has not started. The requested Sudo/Debug
panel and 20× controls are a separate, explicitly deferred follow-up.

## Baseline and preservation

Started from clean `7e013b9e5c33db4197443c6cb3b571c86cabc708`, on
`feat/settlement-goal-10`, including the approved right-hand sidebar. Work is on
`feat/settlement-goal-11`; automatic workspace checkpoints and prior release
branches are retained. The latest existing published application workflow is
[44350273041](https://github.com/gongahkia/roag/actions/runs/44350273041) on
`release/settlement-goal-10` at `3564b070`.

The current baseline reran **214 passing Flutter tests**, the app validator
(47 crops, nine bodies × four palettes), and original Lua bindings (599 roles /
67 source PNGs). The catalogue command reran 48 tests with one failure in its
tracked-media ownership allow-list, reproducing its omission of existing derived
settlement artwork and evidence. It is not an entirely green catalogue baseline.

Initial preservation fingerprints were captured before editing for all six
READMEs, 238 Lua files, 755 original asset/catalogue-related files, 86 existing
app PNGs and four original Lua storage files. Private browser profiles are not
used. All new browser journeys must use disposable contexts and explicit fixtures.

## Geography and terrain

`region_geometry.dart` owns regional generation version 1. It reuses the existing
canonical numeric/text/UTF-8 seed semantics and a separate Park–Miller stream with
domain 715225739. Ten sites occupy a jittered, coherent west-to-east network with
16 weighted links and alternate routes. IDs are stable `site-00` through
`site-09`, independent of display names. Site terrain seeds use a separate bounded
formula; geometry draws do not advance any simulation RNG. The capital retains
its exact existing world. Concrete regional geography is serialized and validated,
never regenerated on load. Travel uses actual graph paths restricted to discovered
sites and open connections.

The original six environments keep their version-2/3 generation paths. Two
version-4 environments are implemented:

- **Pine Uplands:** dense 60% ranked conifer woodland, rocky ridge/pass rules,
  wet threshold 18 and rock threshold 36. The protected opening remains viable.
  More wind and winter snow affect actual travel. Standing forest enables and
  improves forestry production.
- **Salt Coast:** a coherent northern shoreline with a three-cell inland inlet,
  wet coastal approaches and ordinary short bridge crossings. Forest 22%, wet
  threshold 45 and rock threshold 14. Wind/rain have increased weights; prolonged
  rain waterlogs marsh travel using the existing maximum-penalty rule.

The audited spruce (`urizen-onebit-r08-c003`, `[40,105,12,12]`) and obelisk
(`urizen-onebit-r25-c035`, `[456,326,12,12]`) are two new derivative exports from
the unchanged Urizen source. The latter is explicitly cast as a Wayfarer Monument,
not a claim about the original drawing's gameplay purpose. The Lumber Camp uses
a warehouse/oak composition from already audited frames. No source sheet or
catalogue record is edited.

## Founding, identity and clocks

Village recognition requires four residents, at least three coins from actual
work and a staffed connected Market. Initial neighbouring discovery costs two
capital coins. Farther discovery requires two towns and a completed delivery.
Founding costs 60 capital coins: 50 transfer to the new town and ten fund its
expedition. It creates the selected site's concrete world exactly once. Free
Build relaxes these costs and development gates. New towns share the regional
date and inherit incident consent, with their own terrain/weather/event streams
and introductory incident grace.

Separate town treasuries preserve the existing construction budget, refund and
forward Undo/Redo semantics. The region provides a combined summary; trade handles
payments automatically. No second currency or manual transfer interface is used.

The capital clock orders every town through the original 50 ms colony step.
Inactive towns run the same pure authoritative model, without another Flame world,
rendering, camera, speech presentation or per-frame animation. This initial
contract uses exact model work, not inferred successful activities. Switching does
not advance time. Positions, trips, employment, incidents and session-local
construction histories remain owned by each existing Colony instance. Measurements and practical limits are recorded below.

## Production and shipments

Real qualifying work produces bounded town stock: Provisions from Farms, Crafted
Wares from Workshops, and Materials from a staffed Lumber Camp with standing
forest within a three-step local land catchment. Water, rock and occupied building cells stop this work catchment, including bridged water; actual commuting still uses normal roads and bridges. Actual weather and condition output are
passed through once. Forest abundance improves forestry; removing its source or
access stops production. Clear itself grants no Materials. Managed standing
woodland is a renewable source while worked. Stock caps at 24; surplus work does
not become an unlimited latent backlog.

A relationship alternates fairly among three goods and both directions. It
reserves at most three surplus units and the buyer's real coin payment. A pack
visitor legally walks from the origin entry to a staffed Market, collects for two
seconds and walks back out. Only actual departure begins regional travel. Distance,
terrain, weather and closures affect that journey. At the destination, a real
visitor must enter, reach a staffed Market and unload for two seconds before goods
and payment are delivered once. Existing resident work income remains separate;
trade payments transfer coins between towns, rather than minting a second work
payment. Imported goods are not automatically re-exported.

Shipment IDs and loading/transit/unloading/delivered/returned/cancelled/lost states
persist. Uncollected cancellation restores stock and escrow once. A failed local
stop can return along an actual regional journey and unload at the original Market.
Prolonged Standard-mode obstruction can lose collected cargo after 180 blocked
seconds, refunding buyer escrow without paying the exporter. Peaceful mode does
not automatically destroy cargo. Local stranded actors retreat only along legal
roads. Disabling a relationship stops new dispatches; collected journeys finish.

Imported Provisions are consumed by a real populated, connected Market town for
a bounded +3 mood benefit. Consumed Wares create opportunities for the existing
visitor pipeline, which still requires actual arrival/service for any ordinary
reward. Materials support optional projects. Regional interruptions derive from
actual snowbound passes and the existing incident director; no unrelated random
threat scheduler is added.

## Development and optional projects

Recognition stays forward even when a building is removed. No old building or
ability is taken away. Legacy towns receive recognition from their existing facts.

| Stage | Requirements / opportunity |
|---|---|
| Hamlet | Starting recognition; all existing building tools remain available. |
| Village | Four residents, three coins of actual work, staffed connected Market. Opens initial discovery/founding. |
| Town | Eight residents, 120 productive resident-seconds, connected Park/Hall, and one delivery or 20 coins of work income. Enables the Monument. |
| Regional Centre | Twelve residents, two enabled relationships that actually delivered, a standing accessible Monument/Hall and a genuinely trained/on-call protective resident. |

Three optional projects use ordinary coins and genuinely produced/imported goods.
They do not end the game or restrict creative building:

- **Trade Network:** three towns, two reliable relationships, four deliveries;
  consumes four Materials and two Wares in the selected town, then gives +2 mood
  region-wide. No recurring coin mint.
- **Wayfarer Monument:** developed Town, healthy connected Hall, six Materials,
  four Wares and 40 coins. Adds the audited obelisk to that exact Hall and improves
  existing visitor opportunities while it stands. Demolition removes the physical
  benefit; it does not refund goods or erase recognition.
- **Resilient Region:** two towns with trained protection and accessible Refuges,
  all extant damage repaired, no active incident. Standard cargo's recoverable
  obstruction window extends from 180 to 240 seconds.

Winter snow can close exposed passes when both endpoints are snowbound. Actual
brigands at a Market approach and floods cutting a Market connection close affected
regional links. Existing Watchpost response, road/Stone/bridge repair, recession or
retreat reopens them. Paths are chosen only through discovered, open links;
caravans may take alternatives at reached nodes, not teleport midway through a
closed edge. Factual interruption/recovery history comes from the existing
incident/weather state. Peaceful consent survives migration and founding.

## Player controls and local build

From the repository root, with Flutter 3.50.0 available:

```sh
make settlement
# if Flutter is outside PATH:
make settlement FLUTTER_BIN=/absolute/path/to/flutter/bin/flutter
# serve an already-built release using only Python:
make settlement-serve
```

The server binds localhost on an available port and prints its URL. `flutter:
command not found` means the SDK is absent from PATH; the explicit override above
uses an installed SDK without changing system configuration. `make game` still
launches the original LÖVE game. Build dependencies remain locked: Dart 3.14,
Flame 1.38.2, material_ui 1.11.0. No new runtime package or hosted service.

**Region / W** opens the map. Select an adjoining question marker, discover,
then explicitly confirm founding and its name/cost. Select a founded town to visit
or connect it to another town. A relationship automatically exchanges native
surplus for actual need. Expand the production/accounts section for quantities,
reserved buyer payments and trade value. Routes show journey time, local Market
requirements, cargo phase and disruption reasons. Copy the regional seed from the
map footer. Newly discovered biome presets become available in New World;
the original six presets remain available, and Free Build exposes all eight.

Ordinary drag building, sidebar scrolling, B tool finding, I inspection/dialogue,
M relocation, Escape cancellation, H lane, +/-/scroll zoom, pan and Ctrl/Command
Z / Shift-Z remain. The ordinary speed controls stay 0/1/2/4; the existing menu
retains 16× year watching. Debug/20× work is a separate, deferred user request.

The map is a temporary panel, not the default game screen. Its scaled miniature
terrain illustrations reveal known environments, houses show founded sites and
pack icons use actual regional shipment progress. One Flame renderer displays
only the selected town. Camera positions are remembered on switching towns;
opening/reloading uses the established home framing for the active town.

## Persistence and transaction boundaries

Regional **schema 7**, regional geography version 1, wraps each unchanged
schema-6 Colony serialization. Concrete sites, links, discovery, towns, RNG,
identities, positions, routes, stocks/progress, clocks, shipments/escrow, projects,
financial counters and bounded histories are stored. Loading never regenerates
geography or town terrain. Strict decode checks local and regional ownership,
clocks, site seeds, inventories, actors and shipment chronology.

Existing schema-1–6 standalone worlds become the capital without regenerating or
resetting anything. The exact original text is backed up at the existing origin's
`roag.settlement.landscape.v1.before-region-v7` key before the first regional
write. Previous migration backups remain. Backup failure protects the old bytes
and pauses autosave; invalid imports are rejected before replacement. New World
review/cancellation changes no live region. Export/import replaces the **whole
region**, with an explicit warning and export option.

The existing main storage key remains. Browser storage dictionary-packs repeated
field names for schema 7 (format `roag-region-dictionary-v1`); exports are ordinary
readable JSON. Old standalone bytes are read/backed up unchanged. Packing is
lossless, validates duplicate/unsafe fields and fails atomically on quota errors.
No private user browser profile was used for verification.

Construction retains each Colony's existing forward Undo/Redo. It never rewinds
work, dates, deliveries, another town, damage or projects. Founding, discovery,
route establishment, dispatch, delivery and project completion are explicit
idempotent regional operations, outside ordinary brush history. As before,
construction Undo stacks are session-local, not a new persisted time machine.

## Implementation ownership

| File | Responsibility |
|---|---|
| `lib/region_geometry.dart` | Versioned independent regional generation, graph validation, known/open shortest paths |
| `lib/region.dart` | Town identity, shared clock, founding/discovery, genuine production, schema-7 codec |
| `lib/regional_trade.dart` | Reservation/escrow, real local Visit integration, regional travel, refunds/loss, source-derived closures |
| `lib/regional_progression.dart` | Latched stages, one-time projects, goods/coin consumption and standing Hall benefit |
| `lib/region_map.dart` | Pixel-art map, known-site inspection, founding, relationships and compact progression |
| `lib/colony.dart`, `lib/visitors.dart` | Narrow work/mood/freight hooks into existing authoritative model, exact per-tick stepping |
| `lib/model.dart` | Appended Lumber Camp catalogue entry and shared reachable timber validation |
| `lib/terrain.dart`, `lib/world_generation.dart`, `lib/climate.dart` | New v4 environments and climate weights, frozen original six generation branches |
| `lib/main.dart`, `lib/settlement_game.dart` | Existing sidebar integration, one renderer/town switching, whole-region saves, audited compositions and opt-in profiling |
| `lib/browser.dart`, `web/platform.js` | Existing save bridge, migration backup and lossless regional field dictionary |
| `tool/assets.py`, `assets/manifest.json` | Two new reproducible derived crops; original art and old PNGs unchanged |
| `test/goal11_*`, `tool/goal11_*` | Focused deterministic regressions, labelled fixtures, actual release input, motion and measurements |
| `.github/workflows/settlement.yml` | Existing pinned test/build/Pages workflow extended to the exact Goal11 release branch |

## Verification record

The final app suite passes **243 tests in CI** (242 in the first full local run, followed by the new
Centre/Monument regression and the five-test safety rerun). The final terminal
return/loss checkpoints also pass exact round-trip and continuation assertions. Formatting covers all 52 library/test
files. Asset checks pass 49 exact app crops / nine bodies × four palettes and
599 original Lua bindings / 67 PNGs. Six storage-codec fault/round-trip checks pass.
The root catalogue allow-list failure was reproduced before editing and remains
separate; no assertion was weakened.

New focused coverage includes 600 deterministic regional graphs and 600 seeds per
each of eight terrain presets, plus 100 targeted seeds per new biome; paid founding,
independent worlds/histories, exact migration, equal standalone/background work,
30/120-Hz continuation, real goods/forestry, actual bidirectional transport,
zero-sum accounting including escrow, exact transit reload, real Peaceful return,
Standard loss/refund, project costs, latched progress and forward Undo, early and
repeated callback rejection, malformed cargo and profiling neutrality.

Actual browser normal-play seed **202 / Peaceful**: First Hearth begins with four
paid cottages, Farm, Workshop, Market and Park (64 coins). Actual work earns
Village. Discover/found **Alder Reach (Woodlands)** for 60 capital coins; its real
50-coin allocation buys three cottages, planted timber, Lumber Camp, Market and
Park. Establish the relationship, observe collection/departure, reload in transit
and see the actual destination Market delivery. Discover/found **Pine Crossing
(Pine Uplands)**, build four cottages/Farm/Market/Park, connect it, deliver enough
goods and complete Trade Network. Inspect a resident's real Regional supply +2,
continue paid construction, consume delivered Wares for an actual merchant
opportunity, cut forestry access while viewing another town and Undo reconnect it.
No Free Build or edited model state was used after New World creation.

The first long harness stopped on a browser-side closure-reference mistake. The
corrected run completed all ten progression/forestry checks, then its exact-name
Home lookup encountered Flutter's duplicate active tooltip accessibility label
during resize. Prefix matching resolves that harness issue. Continuation loads the
**unchanged saved normal-play bytes**, not a rebuilt developer town, for seasonal,
resize, export/import and switch checks. These distinctions remain in raw logs.

Browser inputs are Playwright mouse/wheel/keyboard against the actual release,
plus a separate native XTest run for genuine 110% browser zoom. Headless Dart
fixtures are explicitly labelled; they are not manual play. Human enjoyment and
balance remain unverified. Performance, hosted URL and moving evidence results follow below.

## Public release and actual browser evidence

**Published and actually played:** [ROAG Settlement](https://gabrielongzm.com/roag/settlement/).
[CI/publication 44918484126](https://github.com/gongahkia/roag/actions/runs/44918484126)
passed tests, analysis, format, assets, six JavaScript storage checks, build and
publication for `bb77ab99c969692fc429df32e20fd647508a5f2a`.
The loaded public and local `main.dart.js` both hash to
`54f0e446143df6e969c2343ef01e6858cab23ab0a352f64ceee4d961e7076e73`.
The public browser completed the **entire 13-check paid journey**, including the
previously split local checks, with no page errors or failed assets. No deployment
blocker remains. The web text-fetch tool could not access this Flutter page;
Chromium and direct asset download did, and are the actual verification evidence.

Only the existing GitHub Pages workflow and the exact additional
`release/settlement-goal-11` environment policy were used. Main and releases 07–10
remain allowed. No Sites publication, new paid resource, credential exposure,
reset, rebase or force push occurred. Automatic `work~` checkpoints were retained.

- [Public journey checks and frame timeline](evidence/goal-11/public/results.json).
- [Developed original town](evidence/goal-11/public/legacy-town-protected.png),
  [founding](evidence/goal-11/public/founding-site-01.png),
  [Woodland production](evidence/goal-11/public/woodland-forestry-town.png),
  [three-town map](evidence/goal-11/public/three-town-network.png).
- [Actual regional cargo](evidence/goal-11/public/caravan-on-regional-road.png),
  [Market delivery](evidence/goal-11/public/first-delivery-benefit.png),
  [Network project](evidence/goal-11/public/trade-network-project.png),
  [real resident benefit](evidence/goal-11/public/actual-regional-mood-benefit.png).
- [Seasonal return](evidence/goal-11/public/seasonal-background-return.png),
  [unmodified paid-play export](evidence/goal-11/normal-developed-region.json).
- [Nine fixture-assisted edge checks](evidence/goal-11/edges/results.json),
  [actual regional interruption](evidence/goal-11/edges/actual-regional-interruption.png),
  [recovery](evidence/goal-11/edges/actual-regional-recovery.png),
  [Salt Coast](evidence/goal-11/edges/salt-coast.png),
  [Monument/Centre](evidence/goal-11/edges/regional-centre-monument.png).
- [Native XTest checks](evidence/goal-11/native/results.json),
  [110% map](evidence/goal-11/native/native-regional-map-110.png),
  [110% town switch](evidence/goal-11/native/native-town-switch-110.png).
  Device-pixel ratio changed from 1 to 1.100000024, inner viewport from 1024×640
  to 931×582; this was real browser zoom, separately from Playwright resizing.
- [CI gates](evidence/goal-11/ci-gates.log),
  [deployment](evidence/goal-11/deployment.json),
  [local full suite](evidence/goal-11/tests-final.log),
  [terminal cargo continuation](evidence/goal-11/trade-terminal-final.log).

## Performance and limits

Measurements use Flutter 3.50.0 / Dart 3.14, Chromium 155/SwiftShader on the same
shared Linux machine. The pre-Goal11 `7e013b9e` application was independently
exported and release-built without copying/creating any README. Identical saved
85/500-resident fixtures are loaded in both. Only one owned browser runs during
samples, without simultaneous owned builds, test runners or video encoding.
Unrelated user applications are left alone. Each browser case samples 16 wall
seconds at 1× after warmup. Timings are internal observations, not hardware-GPU
frame-rate or physical display/input guarantees.

| Browser case | Mean model / p95 ms | Frame p95 ms | Background / trade mean ms per frame |
|---|---:|---:|---:|
| Baseline, 85 active | 0.457 / 1.600 | 66.6 | — |
| Goal11, 85 active | 0.590 / 1.701 | 66.7 | 0 / 0.011 |
| Baseline, 500 active | 2.223 / 5.900 | 83.4 | — |
| Goal11, 500 active | 2.469 / 6.601 | 100.0 | 0 / 0.013 |
| Three actual generated biomes, 20 residents, active trade | 0.326 / 1.099 | 50.1 | 0.111 / 0.019 |
| Four developed grid towns, 85 active +255 inactive | 1.185 / 3.899 | 50.0 | 0.818 / 0.023 |
| Four towns, 500 active +255 inactive | 2.940 / 7.600 | 66.7 | 1.013 / 0.031 |

The first 500 comparison adds **0.701 ms model p95** and 16.6 ms frame p95.
This was investigated rather than omitted. Reversing the order gives Goal11
**2.041/6.099 ms model, 66.7 ms frame p95**, baseline **2.196/6.100 ms model,
83.3 ms frame p95**. The earlier exploratory run retained still slower tails
(up to 116.6 ms frame p95). These overlapping, order-sensitive samples do not
establish a consistent frame regression or improvement. There is measured
additional background work; there is no 60-fps claim. Raw maxima include a 300 ms
frame in the 755-person run. The existing 50 ms foreground delta cap remains;
under stalls, 16 wall seconds advances roughly 13.85–15.60 simulated seconds.

Browser regional scheduling costs about 0.002–0.006 ms/frame. Three inactive
85-person towns perform real model work, without sprite/render worlds. The
three-biome fixture uses real generated terrain and actual trade. Large fixtures
explicitly author connected scaling grids using real housing/residents and normal
construction, not fake population or earned-income counters.

Pure Dart VM, 2,000 measured 50 ms ticks followed by **1,000 more simulated
seconds** per region, gives:

| Region | Mean tick / p95 ms | Active / background / trade ms per tick | Mean save / load ms | UTF-8 export bytes |
|---|---:|---:|---:|---:|
| Three biomes, 20 residents | 0.235 / 0.691 | 0.100 / 0.101 / 0.025 | 16.25 / 22.18 | 247,329 |
| Four towns, 340 residents | 0.479 / 1.070 | 0.144 / 0.325 / 0.006 | 36.85 / 56.80 | 636,589 |
| Four towns, 755 residents | 1.494 / 3.510 | 1.086 / 0.393 / 0.010 | 65.93 / 101.00 | 1,089,120 |

All town clocks remain equal through the seasonal run; completed deliveries are
15/4/7 respectively. Model-only town identity switching averages 0.24–0.65 µs;
this excludes rendering/save/UI. Real pointer-down to an **observed post-switch
animation frame** is 522–636 ms including browser-driver observation overhead,
not a physical latency measurement. Reload to accessible UI is 2.34–2.93 seconds.
No new-world generation occurs when switching a founded town.

The standalone before/after VM workload preserves exact export sizes (157,547 and
612,910 bytes). Mean model cost is 0.451→0.328 ms at 85 and 1.875→1.309 ms at 500;
shared-host/JIT variation prevents attributing that improvement to this change.

Regional packing reduces the 755-person browser snapshot from 1,056,809 to
748,519 UTF-16 code units. Browser codec save/unpack takes approximately 16.9/29 ms
there (separate from Dart encode/decode). Lossless packing, storage-write failure
and unchanged old raw bytes have explicit tests. Browser quotas still apply;
there is no claim that every possible ten-town, 500-resident-per-town world fits.
The tested ceiling is **755 actual residents across four towns**. Ten generated
sites are the geography size, not an invented performance population cap. Export
remains available if local storage fills; quota failure preserves the prior save.

[Direct baseline](evidence/goal-11/performance-browser-before.json),
[final browser](evidence/goal-11/performance-browser-final.json),
[reverse-order Goal11](evidence/goal-11/performance-500-after-reverse.json),
[reverse-order baseline](evidence/goal-11/performance-500-before-reverse.json),
[initial exploratory sample](evidence/goal-11/performance-browser-initial.json),
[regional VM](evidence/goal-11/performance-region-vm.json),
[initial standalone VM](evidence/goal-11/performance-baseline.json),
[final standalone VM](evidence/goal-11/performance-standalone-after.json).

## Moving evidence and reproduction

[**58-second actual public gameplay recording**](evidence/goal-11/region-journey.mp4)
shows the developed legacy town, regional map, paid founding, Woodland building,
automatic relationship, local caravan departure, regional travel, actual Market
arrival, Pine town, Network benefit and continued construction. It is an edited
sequence of **real rendered frames**, with no retiming, mock footage or synthetic
frames. The visible 4×/16× controls are normal game speed. The recording is silent.
[Cut timestamps](evidence/goal-11/recording-cuts.json) map it to the public run's
387-second capture; [motion frame A](evidence/goal-11/motion-a.png) and
[frame B](evidence/goal-11/motion-b.png) show the actual pack traveller moving
out along the road. The rendered frames and screenshots were visually inspected.

From `apps/settlement`, with the pinned SDK and existing Playwright dependency:

```sh
flutter pub get --enforce-lockfile
flutter analyze
dart format --output=none --set-exit-if-changed lib test
flutter test
python3 -B tool/assets.py --check
node tool/storage_codec_test.cjs
bash tool/release.sh /roag/settlement/
python3 -B tool/serve.py --port 0 --base-path /roag/settlement/ --build-dir build/site/settlement
dart run tool/goal11_fixture.dart /var/tmp/roag-goal11/fixtures
# set SETTLEMENT_URL to the printed URL; browser scripts accept output directories
node tool/goal11_browser.cjs /var/tmp/roag-goal11/browser
node tool/goal11_edges.cjs /var/tmp/roag-goal11/edges
node tool/goal11_performance.cjs /var/tmp/roag-goal11/performance-browser.json
dart run tool/goal11_performance.dart
```

`SETTLEMENT_FIXTURE_DIR` selects another fixture directory, `SETTLEMENT_CASES`
selects the measured cases, `?profile=1` enables the existing bounded model samples.
Native verification uses `tool/goal11_native.cjs` in an isolated Xvfb display plus
xdotool, with raw CDP only for navigation/setup/read-only observation. This run
used external `/var/tmp/roag-goal09-tools` SDK/Node/browser caches. No global SDK,
user desktop configuration or private profile was changed. Root validators remain
`make assets-check` and `make catalog-test` (the latter retains its baseline
tracked-media allow-list failure).

## Preservation and completion audit

All **six README hashes match**, including root SHA256
`3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a`.
The original 238 Lua files, four original Lua storage files, 755 source
artwork/catalogue-related files and all 86 pre-existing settlement PNGs match the
initial fingerprints. The user's private town was protected by never opening a
private browser profile. Only checked-in or explicitly generated disposable
fixtures were imported. Lua runtime journeys were not rerun for this Dart-only
feature; source/storage fingerprints and the original asset validator pass.
[Initial fingerprints](evidence/goal-11/preservation-initial.json),
[final fingerprints](evidence/goal-11/preservation-final.json).

| Required area | Evidence |
|---|---|
| Deterministic geography, reachable expansion and new biomes | 600-region graph sweep, eight-preset 600-seed terrain sweep, two targeted 100-seed biome sweeps, actual Woodland/Pine/Coast rendering |
| At least three functioning towns, genuine goods and useful autonomous trade | Full normal paid public seed202 journey, actual workers/Market visits, zero-sum reservation/payment tests |
| Shared timeline and trustworthy background state | Exact standalone/background and 30/120-Hz tests, seasonal browser return, four-town 1000-second VM continuation |
| Recoverable regional situations and progression | Actual brigand closure/recovery, Peaceful return, Standard loss/refund, real merchant opportunity, three projects and latched Centre tests |
| Migration, histories and financial boundaries | Exact legacy backup/subset preservation, in-transit reload, terminal continuation, whole-region file round-trip, invalid import/quota fault, forward Undo |
| Existing building-first controls and pleasant switching | Right sidebar preserved, actual paid construction, camera/home/resize, native 110% W/select/switch/Escape, single Flame renderer |
| Performance and release | 85/500 direct pre-goal comparison,755-person regional case, successful pinned CI/Pages workflow and 13 actual hosted checks |

No required implementation or deployment blocker remains. Goal 12 has not started.
The requested Sudo/Debug view and 20× controls remain a separate follow-up until
this goal is complete.

Known limits: ten sites, one global Monument project, renewable standing timber
rather than tree depletion, broad goods without household inventories, no tactical
regional combat, exact background model ticks without aggregation, bounded
history/archive, session-local construction Undo, and browser storage quota.
Existing stock/collected cargo can continue supplying a town after production
stops; lost access stops **new work**, not supplies already produced. Founding and
camera visits are immediate setup/navigation operations through known geography;
temporary regional closures govern actual cargo journeys. Safari, Firefox,
mobile/touch ergonomics and populations beyond the measured cases remain
unverified. Human enjoyment and balance are not established by scripted success.

1. Does founding another settlement give me a satisfying long-term goal?
2. Are different regions genuinely interesting to develop?
3. Does regional trading feel useful without requiring micromanagement?
4. Can I understand why caravans succeed or fail?
5. Does the region feel like a coherent connected world?
6. Do background settlements progress believably?
7. Do progression milestones and regional projects make me want to continue playing?
8. Is switching between towns easy and pleasant?
9. Does the game remain responsive with several settlements?
10. Do I still spend most of my time enjoying construction and watching inhabitants?
