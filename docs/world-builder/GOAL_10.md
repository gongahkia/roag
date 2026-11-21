# Goal 10 · Landscapes, encounters and settlement stories

Implemented and publicly verified in the Dart/Flutter/Flame settlement.
Goal 11 has not been started. Automated functional journeys are distinct from
human preference testing.

## Play

From the repository root, with Flutter **3.50.0**, Dart **3.14.x**, Flame
**1.38.2**, material_ui **1.11.0** and the committed lockfile:

```sh
cd apps/settlement
flutter pub get --enforce-lockfile
bash tool/release.sh /roag/settlement/
python3 -B tool/serve.py --port 0 --base-path /roag/settlement/ --build-dir build/site/settlement
```

Open the printed local URL. Port 0 avoids interfering with an unidentified
listener. The established production address is
<https://gabrielongzm.com/roag/settlement/>; publication verification is recorded
below. This uses existing GitHub Pages, not ChatGPT Sites or a new paid service.

Saved towns open first. **Landscape menu → New landscape…** selects an
environment, optional scenario, incident mode and numeric/text seed. The draft
offers export and explicit replacement; cancelling retains the town. Scenario
selection suggests its environment, which remains an editable, visible choice.
The entered seed is never replaced by a hidden scenario seed.

Click/drag builds; release commits one stroke. B opens all buildings and tools.
I inspects, M moves an inspected building, Ctrl/⌘ Z and Shift Z undo/redo eligible
construction. Space/right/middle drag pans; arrows, +/- and H retain their camera
controls. Pause and 1×/2×/4× remain on the top bar; the menu retains optional 16×.
**Scenario & settlement history** opens compact objectives and chronological
memories. Incidents have a nonmodal locate/details button and the existing
Preparedness menu. Residents need no individual orders.

## Baseline and ownership

Initial clean branch: `feat/settlement-goal-09`, HEAD
`945c3f4e09d1fb0c103a7e25a052175a7a8e8946`. Existing release branches and
automatic workspace commits were retained. Work uses `feat/settlement-goal-10`.
The actual baseline passed **189 Flutter tests**. The catalogue suite reproduced
**47 passes / 1 failure** before editing, in
`test_all_tracked_media_are_originals_or_explicit_references`: its original
tracked-media allow-list omits derived settlement art and prior rendered
evidence. Original catalogue assertions and files are unchanged.

Important owners under `apps/settlement/`:

| File | Responsibility |
|---|---|
| `lib/terrain.dart`, `world_generation.dart` | Preset data, stable seeds, versioned concrete generation |
| `lib/model.dart`, `navigation.dart` | Terrain/placement/brush/history; cached road graph, transient closures and weighted costs |
| `lib/colony.dart`, `incidents.dart` | Existing clock, actor/planning ownership, one incident director and real response |
| `lib/emergence.dart` | New encounter consequences, scenario evaluation, bounded factual history and codec |
| `lib/preparedness_codec.dart` | Strict incident/condition/actor checkpoint validation, old state compatibility |
| `lib/settlement_game.dart`, `main.dart`, `world_creation.dart` | Existing renderer, flood overlays, input/inspection, creation and history UI |
| `lib/dialogue.dart`, `reasons.dart` | Personal memory from real participants, truthful response text |
| `lib/browser.dart`, `web/platform.js` | Isolated settlement storage and new pre-migration backup |
| `test/goal10_test.dart`, `tool/goal10_*` | Focused assertions, explicitly labelled fixtures and actual release-browser journeys |
| `.github/workflows/settlement.yml` | Existing pinned build/test/Pages workflow, retaining all old branches |

## Generation and terrain

Gentle Plains, Woodlands and River Country retain their exact **version 2**
generation path. The new presets use **version 3**, applying their own declared terrain weights
to the existing integer coarse fields, then an independent domain-49979687
geometry stream. No rendering, inspection, scenario objective or incident draws from it.
Numeric seed normalisation and case-sensitive UTF-8 base-131 text seeds retain
Goal 07's semantics. All concrete lots and version metadata are saved; loading
never regenerates either old or new terrain.

| Environment | Geography and initial decisions |
|---|---|
| Wetlands | Existing north–south river plus two connected three-row tributaries; coarse wet threshold 57/100 and three-cell marsh patches, rock threshold 3/100; 30% ranked eligible woodland, minimum 72 forest cells |
| Highlands | Coarse rock threshold 47/100 and wet threshold 12/100, a two-cell rocky ridge with two three-row passes; 30% ranked eligible woodland; real 2D bottlenecks, no simulated height |
| Harsh Frontier | River, wet/rock thresholds 25/100, rocky ridge and exposed eastern rock bands; 42% ranked eligible woodland; brigand selection has three tickets instead of one, with unchanged damage and prices |

Forest ranking uses bounded domain-983 coordinate variation, with explicit
coordinate tie-breaking. Before vegetation, a single bounded validation pass restores at least 450 open
cells nearest the protected lane, preserving water and the critical ridge. Then
woodland coverage is applied. The minimum forest count and protected corridors are
bounded generation-time rules, not live adaptation. All six presets retain the
11×9 open starting clearing and eight arrival roads. New presets also preserve
three rows of dry land along the main expansion lane, except the real river.
Its two/three-cell starter crossing costs at most 9 coins. The default opening
still affords cottages, work and social facilities without late-game tools.

The five existing terrain categories remain authoritative: open ground builds;
forest requires Clear; marsh supports slow roads and requires Clear for
buildings; water needs a complete bank-to-bank Bridge; rock remains impassable.
Clear/Plant/Bridge retain their costs, exact transactions and Undo/Redo. No new
inventories, permanent currencies or direct actor controls were added.

Wetlands accumulate waterlogging during rain, capped at 180 simulation seconds,
and dry at 0.5 second per non-rain second. At 20 accumulated seconds, marsh travel
takes **2×** rather than 1.5× time until it dries below the threshold. Other
environments retain the old marsh factor. Terrain and weather use the larger
penalty once; Stone stays `1/1.18`. Navigation and actual edge walking share the
same cost. A changed waterlogging tier invalidates cached fields once. Actors
retain the established still-legal current-edge convention.

## Encounters, preparedness and pacing

The existing `Incident` owner now handles five families. One major/assistance
encounter runs at a time, alongside the existing bounded ordinary visitors and
compatible positive life. New encounters use the existing actor registry,
eight-request planning budget, weighted road graph, fixed clock, refuge and
condition pipeline. There is no second destructive scheduler.

**Storm surge:** eligible buildings are within two Manhattan cells of real
water. A six-second warning marks a contiguous bank footprint, grown through
non-rock cells from an actual water source, radius three and hard cap 40. It is
selected after prolonged waterlogging. After warning, ordinary roads in that
footprint close; Stone Roads and valid bridge decks stay passable. A bridge still
needs usable approaches. Residents leave along real accessible roads, seek real
Refuge capacity or wait; they cannot enter a closed cell. At most eight exposed
buildings take three damage per five active seconds, capped at **18 each / 144
total**. Active water lasts 45 seconds, then all temporary closures recede.
Concrete terrain is untouched; damage remains for Repair. The footprint is
computed once; hazard work visits that bounded footprint at 1 Hz.

**Road brigand:** a single bounded autonomous disruptor, using the existing
hooded human silhouette, enters at the real arrival road after warning. It
follows legal routes to a Market. Only actual arrival starts disruption. After
two actual seconds it can take at most **4 coins**, once; four damage ticks of
three health give at most **12 damage**. A trained Watchpost within ten weighted
road-cost units dispatches its existing guard. Only physical proximity and two
actual shoo seconds drive the brigand away. A disconnected/untrained station
protects nothing. The brigand legally retreats; it never kills a resident or
destroys a building. If no target can be reached it causes no theft/damage.

**Stranded caravan:** one actual pack traveller represents the caravan, using
the existing merchant drawing. It walks to a reachable Inn or Refuge. Actual
service requires a present Inn worker and spare social capacity, or spare Refuge
capacity. It reserves one future Inn visitor place. Eight served seconds award
**2 coins once**, then it walks back out. Missing roads, full shelter and absent
staff do not provide service. After 100 seconds without completed help it
attempts departure without an award. A cut exit causes physical waiting until
the player restores access, following the existing visitor convention. Help and
departure remain in persistent history; later caravan opportunities can recur.

The brigand uses audited `urizen-onebit-r02-c104` (the existing hooded human),
and the caravan uses `r00-c198` (the existing pack carrier). These are explicit
game casting choices, not publisher claims that those drawings are raiders or
a full caravan. Both come from the retained CC0 Urizen source and reuse the
existing actor atlas, palettes and procedural movement; no source crop or new
artwork was introduced.

Fire and rabbit rules remain available. Response still depends on actual staff,
condition, route and arrival. The shared retreat path now finishes only a legal
current edge before replanning, so a moving actor cannot resume its old target
after being driven away.

Standard retains three days / 450 seconds of introductory grace, normal work
hours, six residents, no active gathering and one incident at a time. Opportunities
are checked every 30 seconds. An extant unusable building or six damaged buildings
defer the next encounter; retired IDs do not block recovery. A serious conclusion
starts **600 seconds** cooldown; assistance uses 150 seconds. Peaceful rejects
destructive starts but permits ordinary positive life and assistance. Existing
Peaceful saves never opt in automatically.

Existing fire/wildlife tickets remain; caravan has one when weather is covered
or a caravan has visited before; flood has four after prolonged waterlogging;
brigand has one, or three in Frontier. When alternatives exist, the most recent
family loses its tickets. Selection/targeting uses the existing persisted incident
Park–Miller stream. Deterministic actor movement and scenario setup need no new
mutable RNG. Preparation never increases threat damage or heat.

## Scenarios and factual memory

Free Build stays default, with the existing 200 coins and no objectives. Scenarios
change suggested geography, starting cash and priorities, without disabling tools:

| Scenario | Start and three actual objectives |
|---|---|
| Riverside Trade | River Country, 210 coins; connected working Market; merchant actually served; completed resident/merchant trade |
| Highland Crossing | Highlands, 220 coins; connected road beyond column 20; six residents plus actual work beyond the pass; retain far-side access throughout a serious incident with actual productive work during the danger |
| Frontier Outpost | Frontier, 210 coins; six residents; trained protective staff; conclude a serious incident and restore all extant damage |

Objectives latch once, survive Undo/reload/import and never end the settlement.
There is no scenario cash award or claim button. Peaceful remains selectable;
destructive-incident objectives are optional there.

History retains at most **80 facts**, with stable IDs, simulation time, location,
kind/outcome, actual participant IDs and source encounter/repair IDs when relevant.
Warnings, broken/open crossings, incident conclusions, repairs and objectives are
recorded by authoritative transitions, not previews. The last 24 detailed incident
memories remain. Earlier pre-schema-6 incidents are also visible. A resident's
personal remembrance uses actual warned/affected/responding/sheltered membership,
not a stale exposure timer or an invented injury/family story. Existing authored
demolition traces and building scorch/repair/development details remain.

## Saves and forward construction history

Schema **6** adds scenario completion, rain exposure, story IDs/history and the
new active encounter data. Schemas 1–5 load concrete terrain and existing identity,
clock, money, climate, jobs, visitors, dialogues and incidents. Schema-5 Standard
stays Standard; older neutral/Peaceful migration remains. Existing worlds gain
Free Build with no retroactive objectives or threats. The browser preserves the
original text in `.before-stories-v6` before its first migrated write; older
v3/v4/v5 safeguards remain for their corresponding source schemas. Backup failure
protects the original and pauses autosave.

Temporary flood closures are derived from saved active incident state, never
serialized into concrete landscape snapshots. Undo/Redo reconciles closures
against today's roads. Incident damage, actual theft, real assistance income,
memories, objectives and clock are independent forward state. Existing used-repair
and stale-Redo safeguards remain. Invalid imports decode before replacement.
Construction Undo itself remains session-local, as before.

## Verification, performance and release

The final source passed **214/214 Flutter tests**, versus the observed 189-test
baseline, with all original test files and assertions unchanged. The 25 new tests
cover generation, weather costs, coherent flooding, actual service/response,
blocked/reconnected access, active-state continuation, financial limits,
scenario causality, factual participation and invalid imports. Existing scheduler,
construction, needs, income, dialogue, visitor, repair and save regressions pass.

The existing sweep now tests **600 distinct seeds per preset across all six
presets**: reproducible concrete maps, real variation, starter access, plots and
terrain counts. The added 300-seed sweep for each new preset also actually builds
the eastward route, checks work/social plots and the ≤9-coin crossing. Thus all
three new environments have 600 distinct tested seeds; the first 300 receive the
additional developed-route check. Full encoded seed-202 worlds for all three v2
presets match independently captured baseline fingerprints.

Analysis reports no issues; formatting reports 42 files / zero changes. The app
asset validator verifies 47 source crops and nine actor bodies × four palettes.
The original binding check verifies 599 bindings / 67 unchanged PNGs. The broad
catalogue command remains **47 passes / 1 failure** in the same original
media-ownership assertion. Newly captured Goal 10 media also fall outside that
unchanged allow-list; this is not reported as an entirely green catalogue run.
[Baseline log](evidence/goal-10/catalog-baseline.log),
[final log](evidence/goal-10/catalog-final.log),
[Flutter log](evidence/goal-10/tests-all.log),
[analysis](evidence/goal-10/analyze.log),
[asset check](evidence/goal-10/assets.log).

### Actual browser journeys

The production HTTPS build completed **17 grouped browser checks**, covering all
25 requested journey items through the main harness, scaling fixtures and retained
regression harness. These use actual Chromium 155 / Playwright 1.65.1 pointer,
keyboard, context-menu, download and file-picker input against real CanvasKit
rendering. State observations read saved checkpoints; no live Dart gameplay
callbacks or direct actor orders are used. This is automated browser input,
**not native desktop manual play or human preference testing**.

- A genuine schema-5 fixture opens with identical lots, clock, money and complete
  resident records, and its original text is backed up. Draft cancellation keeps it.
- All three entered seed-202 environments receive paid neighbourhoods with actual
  work and social services. A paid Wetlands bridge carries a real arriving resident.
- A Highland Farm is relocated through its existing right-click menu, retaining
  its ID. Workers cross the pass and actually produce; neighbours socialise and
  the existing contextual conversation still opens.
- Same-seed maps match, a different seed changes terrain, and the Trade scenario
  completes all three actual objectives before further paid construction.
- The labelled flood fixture receives paid Stone Roads, actual evacuation,
  **six sheltered residents and 54 lasting building damage**, then recession,
  paid repair and forward-moving Undo/Redo. Active paused reload is exact.
- The same brigand takes **12 damage / up to four coins** at the unprepared Market;
  the trained, physically arriving guard drives it away with **zero damage**.
- The caravan earns nothing while its road is cut. Reconnection permits real Inn
  service, exactly two coins, legal departure and retained assistance memory.
- A real JSON download/import and reload retain the developed town and histories.
- A separately labelled **naturally scheduled** rabbit incident starts at
  **480.0 seconds**, ends at 494.6, and records a real guard response. Its fixture
  was legitimately advanced to 449 seconds; no forced start selected that event.
- Ordinary construction and inspection work after camera pan, zoom and resize.

The unchanged Goal 09 network regression additionally removes a genuine bridge
on a responder's route, observes physical rerouting/waiting, restores it with
Undo and exercises reduced motion, 931×582 / 1440×900 layouts and mute/unmute.
The new storage harness explicitly injects failures into both old v5 and new v6
migration backups: original bytes remain intact and autosave pauses. Invalid
incident RNG and unknown scenario files are refused before replacement.
[Public results](evidence/goal-10/browser/results-public.json),
[retained regression results](evidence/goal-10/results-journeys.json),
[storage fault results](evidence/goal-10/results-storage.json).

### Motion and screenshots

[53-second actual public-game recording](evidence/goal-10/emergent-settlement.mp4) combines
four excerpts from the continuous release-browser recording. It shows a seeded
challenging landscape and construction, the Highland working district, flood
preparation/warning, autonomous shelter, consequences and recovery/history.
Only waits between excerpts were cut; no mock scene or invented actor motion is
substituted. The incident sequence is explicitly a developer comparison fixture,
not the naturally scheduled rabbit journey.

Readable actual screenshots:
[Wetlands](evidence/goal-10/browser/environment-wetlands.png),
[Highlands](evidence/goal-10/browser/environment-highlands.png),
[Frontier](evidence/goal-10/browser/environment-frontier.png),
[bridge in use](evidence/goal-10/browser/wetlands-real-crossing.png),
[far-side work](evidence/goal-10/browser/highlands-far-side-work.png),
[completed scenario](evidence/goal-10/browser/scenario-trade-completed.png),
[flood response](evidence/goal-10/browser/flood-active-shelter.png),
[history](evidence/goal-10/browser/flood-recovery-history.png),
[caravan memory](evidence/goal-10/browser/caravan-departure-memory.png).

### Scaling and generation measurements

A fresh isolated archive of exact starting commit `945c3f4e` was built with the
same pinned SDK; no README was extracted. Its JavaScript matches the previously
published Goal 09 hash. Both builds use the same valid pre-Goal-10 85/250/500-person
checkpoints. One owned browser runs at a time, without concurrent builds or video
encoding during measurements. Samples are 16 wall-seconds ordinary + 16 recovery
after the same 13-cell road cut. Actors remain simulated off-screen.

| Case | Population | Model p95 before edit → recovery | Frame p95 | Recovery queue |
|---|---:|---:|---:|---:|
| Baseline | 85 | 1.00 → 0.80 ms | 33.4 ms | 33 → 0 |
| Final, comparable | 85 | 1.10 → 0.90 ms | 33.4 ms | 23 → 0 |
| Baseline | 250 | 2.20 → 2.10 ms | 50 → 33.4 ms | 196 → 0 |
| Final, comparable | 250 | 2.20 → 2.40 ms | 50 → 33.4 ms | 176 → 0 |
| Baseline | 500 | 3.90 → 4.30 ms | 50 ms | 438 → 0 |
| Final, comparable | 500 | 3.90 → 4.70 ms | 50 ms | 414 → 0 |
| Rain + active flood | 85 | 1.90 → 1.40 ms | 33.4 ms | 31 → 0 |
| Rain + active flood | 250 | 4.90 → 4.50 ms | 33.4 → 33.5 ms | 170 → 0 |
| Rain + active flood | 500 | 8.50 → 8.40 ms | 50 ms | 413 → 0 |
| Rain + brigand | 500 | 3.60 → 4.20 ms | 50 ms | 437 → 0 |
| Rain + caravan | 500 | 3.80 → 4.90 ms | 50 ms | 442 → 0 |

The comparable 500-person recovery adds approximately **0.40 ms** model p95;
frame p95 remains 50 ms. Mean frame interval is 28.95 → 29.66 ms ordinary and
28.85 → 30.32 ms recovery. Flood mean frame interval is 31.35 / 31.64 ms. Maximum
observed 1 Hz hazard-check cost is **11.90 ms** for 500-person flooding, **4.70 ms**
for the brigand and **1.40 ms** for caravan service. These are Chromium SwiftShader
measurements on a shared Linux machine, not a hardware-GPU frame-rate or physical
input-latency claim. Raw data retain maxima, distributions, clock advance,
planning peaks, cache work and active actor counters; no assertion hides slow
samples. A drained queue means planning has caught up; residents in genuinely cut
districts can still wait for road repair.

[Baseline](evidence/goal-10/performance-before.json),
[comparable release](evidence/goal-10/performance-comparable.json),
[flood](evidence/goal-10/performance-flood.json),
[brigand](evidence/goal-10/performance-raiders.json),
[caravan](evidence/goal-10/performance-caravan.json).

A separate Dart VM sample of 100 generated seeds per new preset reports mean
2.01 / 1.84 / 1.95 ms and p95 **3.61 / 3.19 / 2.62 ms** (Wetlands / Highlands /
Frontier). The first series retains its 37.18 ms cold/JIT maximum. This is model
creation cost, not browser display latency.
[Raw generation timings](evidence/goal-10/generation-times.json).

### Public release and reproducibility

**Published and actually opened:**
<https://gabrielongzm.com/roag/settlement/>.
[GitHub Actions 44343582241](https://github.com/gongahkia/roag/actions/runs/44343582241)
passed the web and publish jobs for `5a9bb7e27ffc0a9e9ef304bfb23d1a4e5fbd1cd0`.
The full-journey browser-loaded `main.dart.js` SHA256 is
`41880434d79d69bb954bc7dc230ca944e93230f4f4f7bc41c9714bf3deea35a7`, matching the
local tested release. All public journeys report zero failed asset responses and
zero page errors. [Deployment](evidence/goal-10/deployment.json),
[CI gates](evidence/goal-10/ci-gates.log),
[build fingerprints](evidence/goal-10/build-fingerprints.json).

A final presentation-only follow-up, `28550b09`, replaces the shared internal
`grazing` word with **Market disruption** and **shelter stop** in human encounter
cards/inspection. It changes no saved phase, timing, damage, RNG or actor rule.
Analysis and all 214 tests passed again. The final public build's JavaScript is
`2b4221d4bc6742276372d7d17893e8e747b9f772c31c7dd62567dedcb8a24788`.
[Publication 44349324990](https://github.com/gongahkia/roag/actions/runs/44349324990)
passed both jobs. Two additional real public browser journeys exercise the new
labels and prove complete paused checkpoints reload exactly while retaining the
original raw saved phases. [Final phase results](evidence/goal-10/browser/results-phase-public.json),
[Market label](evidence/goal-10/browser/raid-unprepared-phase.png),
[caravan label](evidence/goal-10/browser/caravan-phase.png).

The 17-journey recording and measurements above use the complete release before
that wording-only change; the final two UI checks cover the changed presentation.
The final browser helper also skips cached/redirect response bodies when hashing
scripts. Its automatic `3564b070 work~` checkpoint is retained and passed
[publication 44350273041](https://github.com/gongahkia/roag/actions/runs/44350273041),
with unchanged application bytes. [Latest deployment](evidence/goal-10/deployment-latest.json).

After the explicit pause/resume, the unchanged application was revalidated on
21 November: all **214 tests**, formatting, analysis and the asset check pass
again. The original 599-binding asset check passes too; the broad catalogue
suite remains 47/48 on the documented ownership assertion.
A fresh isolated browser opened the actual public URL and repeated both
human-encounter phase/reload journeys, loading the same final JavaScript hash.
[Resumed tests](evidence/goal-10/tests-resumed.log),
[resumed public checks](evidence/goal-10/browser/results-phase-resumed.json).

Only the `release/settlement-goal-10` workflow branch and exact Pages deployment
policy were added; main and releases 07–09 are retained. No reset, force push,
credential exposure, new paid resource or Sites deployment occurred. The
workspace's automatic `0d22f22f work~` checkpoint was retained, followed by the
browser-harness wait fix. Later documentation/media commits retain the same
application bytes. [Policies before](evidence/goal-10/pages-policies-before.json),
[after](evidence/goal-10/pages-policies-after.json). No deployment blocker remains.

Reproduce focused checks from `apps/settlement`:

```sh
flutter analyze
dart format --output=none --set-exit-if-changed lib test
flutter test
python3 -B tool/assets.py --check
bash tool/release.sh /roag/settlement/
dart run tool/goal10_fixture.dart /var/tmp/roag-goal10/fixtures
node tool/goal10_browser.cjs /var/tmp/roag-goal10/browser
node tool/goal10_storage_browser.cjs /var/tmp/roag-goal10/storage
node tool/goal10_phase_browser.cjs /var/tmp/roag-goal10/phase-ui
SETTLEMENT_NETWORK_ONLY=1 node tool/goal09_journeys_browser.cjs /var/tmp/roag-goal10/regressions
```

Set `SETTLEMENT_URL` to the served/production URL and `SETTLEMENT_FIXTURE_DIR` when
using another fixture directory. Browser tooling requires the existing pinned
Playwright package, Chromium at `/usr/bin/chromium` and recording FFmpeg. This
verification used external `/var/tmp/roag-goal09-tools` SDK/Node/browser caches,
not a new system installation or a private browser profile. For comparison,
run `tool/goal08_fixture.dart` in the isolated baseline source and point both
builds' existing `tool/scaling_browser.cjs` at that one fixed fixture directory.
Active measurement directories link `scale-N.json` to the corresponding
`flood-scale-N.json`, `raiders-scale-N.json` or `caravan-scale-N.json` generated
by the new fixture tool. `SETTLEMENT_POPULATIONS=500` selects the travelling-actor
samples. Root checks remain `make assets-check` and `make catalog-test`, from the repository root.

### Preservation and completion audit

The final preservation manifest is recorded alongside the initial fingerprints.
All six README files, four original Lua storage files, original Lua source,
original artwork/catalogue and all **86 pre-existing settlement PNGs** retain
their initial hashes. Root README SHA256 is
`3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a`.
The manifest also verifies 238 Lua source files, 729 original artwork/catalogue
files and all 14 pre-existing settlement test files unchanged. No private browser
profile was opened. Lua runtime journeys were not rerun for
this Dart-only change; their source and storage were verified by fingerprints.
[Initial](evidence/goal-10/preservation-initial.json),
[final](evidence/goal-10/preservation-final.json).

| Objective group | Completion evidence |
|---|---|
| Seeded difficult worlds and viable access | Six-preset 600-seed sweep, 900 developed routes, v2 frozen full-world fingerprints, all three real browser neighbourhoods |
| Consequential autonomous encounters | Coherent flood, real brigand/guard comparison, real caravan service/departure, phase-by-phase exact checkpoint tests |
| Shared pacing and interactions | Existing director extended, weather costs/flood exposure, refuge capacity, disconnected response and cooldown tests; natural scheduled incident observed |
| Optional replayability | Three scenarios, genuine far-side work/route-history test, actual public Trade completion followed by more building |
| Persistent factual stories | Bounded memories with source/participant IDs, resident conversation, repair/history and forward Undo checks |
| Compatibility and storage | Existing assertions unchanged, schema-5 identity/land/clock comparison, active exact reload, file export/import and backup/import fault tests |
| Scaling and presentation | 85/250/500 real browser populations, bounded hazards/actors, queue recovery, real screenshots and motion, retained bridge/mute/reduced-motion/resize regressions |
| Public publication | Successful existing Pages workflows, 17 full journeys plus two final phase/reload journeys on the matching HTTPS builds |

The scope is complete. Human judgement of balance, memorability and enjoyment
remains separate from these functional results.

## Limits and human playtesting

Encounters deliberately use one brigand/pack traveller rather than squads or
inventories. Floods are local bounded overflows, not hydrodynamics. Rock is 2D
impassable ground. An isolated departing actor can wait for restored roads.
In crowded tiles at low zoom, the existing I inspection prioritises nearby
residents; the building right-click menu gives unambiguous Move/Inspect. Browser
verification covers Chromium, not Safari/Firefox, touchscreens or manual physical
mouse/keyboard play. Functional scripted outcomes do not establish balance or enjoyment. Software
browser frame timing is not physical display latency or a hardware-GPU promise.

1. Do the new environments genuinely change how I build?
2. Are threatening events surprising but fair?
3. Can I tell whether my preparations helped?
4. Do events create memorable situations rather than repetitive penalties?
5. Does the settlement's history make it feel more personal?
6. Do scenarios make starting another town appealing?
7. Can I continue building without excessive micromanagement?
8. Does the game remain responsive with a large population?
