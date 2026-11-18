# Goal 10 · Landscapes, encounters and settlement stories

Implementation and verification record for the Dart/Flutter/Flame settlement.
Goal 11 is outside this work. Verification sections below are updated as each
release gate completes; a passing model test is not a claim of human playtesting.

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
generation path. The new presets use **version 3**, starting from the existing
integer coarse fields and applying an independent domain-49979687 geometry
stream. No rendering, inspection, scenario objective or incident draws from it.
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

Final measured results and browser/public evidence are recorded here after the
corresponding gates complete. No private browser profile is used by the harnesses.

## Limits and human playtesting

Encounters deliberately use one brigand/pack traveller rather than squads or
inventories. Floods are local bounded overflows, not hydrodynamics. Rock is 2D
impassable ground. An isolated departing actor can wait for restored roads.
Functional scripted outcomes do not establish balance or enjoyment. Software
browser frame timing is not physical display latency or a hardware-GPU promise.

1. Do the new environments genuinely change how I build?
2. Are threatening events surprising but fair?
3. Can I tell whether my preparations helped?
4. Do events create memorable situations rather than repetitive penalties?
5. Does the settlement's history make it feel more personal?
6. Do scenarios make starting another town appealing?
7. Can I continue building without excessive micromanagement?
8. Does the game remain responsive with a large population?
