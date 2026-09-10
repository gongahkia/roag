# Major systemic-world expansion — live assessment

## Starting evidence

- Actual start: `e32997d4eadc09b63a8cd14d4bf4c8b0932539d9`, `main`, clean,
  equal to local `origin/main` (0 behind, 0 ahead); no fetch or push performed.
- Development interpreter: Python 3.14.7; supported application floor: 3.11.
- Save format: 6. Baseline production definitions: four regional destinations,
  30 containers, 18 weapons, 18 armour, 37 passives, eight secondary tools,
  five supports, eight drinks, six relics, four regional questlines, one arc,
  24 standard enemy situations and eight elite alternatives. Definitions are
  not evidence of distinct play. Four taught technique names currently lack
  a production hook; nominal build labels are not verified build scenarios.
- Baseline full suite: **177 tests passed in 443.430 seconds** on the unchanged
  production modules. The prior 567.579-second result is historical. A first
  completed run lost its summary during interruption and was not counted.
- No concurrent changes encountered at inspection. Existing geography, items,
  deaths, quest outcomes and migration history must remain intact.

## Continuation audit — 10 September 2026

- Work resumed at `5be7ca6ca0cc9a093dbed893acd8f6b0a58fae04` on `main`,
  equal to local `origin/main`. The expected start remains an ancestor 24
  commits behind. No fetch or push was performed.
- The worktree already contained one unrelated owner change:
  `PROMPT-THAT-WAS-MIDWAY.md` was locally emptied. It is preserved, excluded
  from every project commit, and not treated as implementation work.
- Python 3.14.7 is the active interpreter; Python 3.11.16 is also installed.
  Save format is 7. The current unmodified continuation baseline passes
  **373 tests in 421.415 seconds** (428.96 seconds wall time).
- Production inspection, including lazy generation of all frontiers, finds
  eight destinations; 62 containers; 24 weapons; 36 armour pieces; 37
  passives and 16 named role/recruit/work techniques; six relics; 40 broad
  secondary/support/discovery/drink definitions; 12 regional questlines; one
  cross-region arc; 12 voyage families, eight with physical deck play; and
  eight commodities.
- Enemy tables contain 42 standard and 15 elite definitions. Six directly
  authored Hearthford standards bring the nominal standard total to 48; the
  authored runaway wheel brings elite situations to 16. Four frontier elites
  are named returning claimants. A coarse production signature identifies
  fewer than 48 distinct standard behaviours, so the mechanical-distinctness
  gate remains open despite the nominal count.
- Current verification gaps are explicit: the checked-in 1,000-seed result
  predates later workshop, voyage, clothing, elite, weapon and workline work;
  there are no standalone content, persistence, replay or long-session memory
  audit commands; the old quest audit covers only the established four; there
  is no repository fixture captured from a real format-6 save; and complete
  manual branch/build/campaign coverage has not occurred.

### Remaining implementation sequence

1. Add validated production manifests and focused audit commands. Make every
   headline count derive from production definitions and strengthen quest,
   persistence, replay and soak evidence before adding more labels.
2. Complete 36 mechanically differentiated weapon families, 64 combined
   passive/technique effects, 12 finite relics and 24 automated build
   scenarios, with physical sources, inspection and shared reducer hooks.
3. Add six genuinely distinct standard roles, deepen institutional
   relationships and commodity handling, and generate history-backed named
   objects whose mechanics, claims and clues come from the regional ledger.
4. Add two authored cross-region arcs with physical evidence, disclosed
   approaches, persistent route/market/institution outcomes and succession-safe
   state. Extend all audits across the 12 regional lines and three arcs.
5. Close integration, performance and manual PTY gates honestly: rerun the
   1,000-seed world audit on final code, run the new audits and soak, complete
   the requested playable paths where feasible, then record untested platforms
   or paths without converting automated fixtures into manual claims.

## Plan and ownership

The primary agent owns all implementation, integration, and claims. Bounded
read-only research/content audits informed the baseline. Their conclusions
must be checked against production entry points.

1. Commit research, scope, baseline evidence and this acceptance ledger.
2. Add reproducible JSON benchmarks and a fast developer suite before changing
   game systems. Profile generation, packing, movement/FOV, pathfinding and
   saves; optimize measured hot paths without changing their outcomes.
3. Add format-7 sparse state with deterministic format-6 migration. Preserve
   existing maps exactly; new destinations are lazy and stage-seeded. Keep
   derived indexes, path/FOV caches and queues reconstructible and unsaved.
4. Introduce four bounded causal histories and geography families: peat fen
   islands, a mineral river gorge, flood terraces/clayworks, and a cold braided
   estuary. Connect them to the existing chart and physical expedition loop.
5. Integrate sparse material reactions and shared exposure for couriers,
   enemies, named adults, physical items and structures. Add contextual verbs,
   telegraphs and inspection before increasing reaction density.
6. Add ecology, institution interests, bounded stock changes and perceived
   cross-actor goals. Extend production encounter composition, not just audits.
7. Add physical workshop fittings, active equipment/treasure, named historical
   objects and tested cross-system builds. Audit every retained definition.
8. Author the additional regional lines, arcs, contracts, rivals and elites;
   preserve loss, refusal, alteration, succession and repeat-visit outcomes.
9. Activate optional vessel work and twelve sporadic voyage families, including
   six tactical deck encounters, through existing action-clock rules.
10. Run correctness, 1,000-seed generation/encounter/quest/content/persistence/
    living-world/replay audits and performance/memory soaks. Play the requested
    branches in real PTYs, adjust observed balance, and report missing coverage
    explicitly. Finish with a documentation/assessment commit, never a push.

## Acceptance ledger

Working-weapon continuation: six new physical families bring the arsenal to
24, with normal finite container/merchant sources, payload selection and
recoverable readied-axe throws. Fourteen focused action/ownership tests and six
targeting tests pass; a real 80×24 Dunmire chest/packing/pitch-cast/return loop is
recorded in `working-weapons.md`.

Continuation completion: twelve further authored working forms bring the
arsenal to **36 families**. They are not numeric reskins: glaive cleave,
pollaxe protection/support break, arming-sword counter-guard, quiet aim-breaking
knife, flood-enhanced boat-hook pull, morale-breaking mace, anti-rigid estoc,
timber-cutting axe, guarded quarterstaff spacing, fuel-producing reed sickle,
flood-braced anchor fluke and non-damaging chain restraint each use an explicit
production reducer. Every form has regional chest or merchant provenance,
physical size/weight, inspection text, and a focused behavior assertion. The
15 weapon tests passed in **1.672 seconds**; the 40 inventory and ranged
regressions passed in **14.827 seconds**. The 24-build gate remains open.

Eleven physical discoveries close the active passive/technique quantity gate:
**48 passives plus 16 inherent or learned techniques = 64**. Each addition
changes a production reducer: fen and ice footing, fire and pitch handling,
lime exposure, smoke melee, witnessed salvage, guarded support, released-water
cargo movement, telegraphed ranged aim, or named-account delivery. They are
distributed among the four frontier container sets and retain ordinary bulk,
packing, loss and persistence. The focused 11-test file passed in **0.555
seconds**. Relic quantity and the 24 scenario demonstrations remain open.

Four additional regressions reproduced loose `%` cover having no projectile
effect, a repeated cache rumor selecting the same marked coffer, and exhausted
rumors raising `StopIteration`. The shared cover query now affects actual
player and enemy shots, and the contact advances through unmarked caches or
reports exhaustion. All four tests pass (0.637 seconds); the frontier-elite
and 18 ranged-combat checks also pass. No map repaint or respawn was added.

All gates below remain open until measured and exercised. Counts cannot close
them by themselves.

| Gate | Required evidence | Status |
|---|---|---|
| Immediate play | equipped bargemaster, gangplank E, unchanged first route | baseline retained; regression pending |
| Responsiveness | baseline/final median, p95, p99, worst, RSS, save size | profiling underway |
| Geography/history | eight distinct regions, 3–7 causal events each, 1,000-seed access audit | open |
| Materials | bounded shared reactions, inspection, player agency, persistence | open |
| Ecology/AI | perceived cross-actor goals, 48 standards, 16 elites, four named rivals | open |
| Institutions/economy | witnessed consequences, services, stock and physical contracts | open |
| Builds | 36 weapons, 36 armour, 64 passives/techniques, 32 tools/supplies, 12 relics, 24 demonstrated builds | weapon/armour/passive/tool counts met; relics and scenarios open |
| Treasure | 60 persistent containers, history-backed objects, clue trails | open |
| Stories | 12 substantial regional lines, three arcs, alternate outcomes | open |
| Vessel/voyages | optional stations, twelve families, six physical deck encounters | open |
| Migration | real format-6 preservation, corruption and mid-reaction round trips | open |
| PTY | region/branch/build/campaign/crisis/resize/mouse paths requested by owner | open |

## Measurement and limitations log

Ranged targeting now includes visible cross-level actors, with Tab cycling,
`<>` level inspection, an above/below marker and separate name, range/cover/
ammunition and preparation rows. Actor-name inspection and direct attacks share
weather-limited sight; previously the cursor could expose a hidden actor and
the reducer could shoot beyond current visibility. The arbalest reload test now
explicitly uses daylight because its twelve-pace fixture lay beyond dawn sight.
The serial suite passed **341 tests in 184.353 seconds**. A subsequent status
correction shows the readied weapon's physical ammunition, not always bolts;
the **24 ranged/visibility tests passed in 6.304 seconds** with that regression.
Compilation and whitespace checks passed. The full 341-test result predates
the final status-only correction.

Real PTY (`vertical range`, forced saved load/arrival, unmodified watchtower
geometry): at 100×32, selected the watch-roof keeper from the floor below and
committed a sling shot through the ladder opening (four stones became three).
The keeper withdrew into partial cover. Resizing that active targeting screen
to 80×24 retained all three information rows and consumed no action; Escape
cancelled, then E physically climbed to the roof and marked a height treasure
lead. Synthetic terminal mouse click reports selected an empty cell without
firing. This tests curses event handling, not native desktop mouse reporting.
It is a targeting fixture, not a full ranged-build expedition.

Eight additional frontier elite situations are now generated in production,
one seeded alternative per new region. Four are named finite returning
claimants. Material telegraphs, shared reactions, escort interception, physical
reward/recovery and contact settlements are documented in `frontier-elites.md`.
The catalogue now has fifteen elite definitions plus Hearthford's directly
authored mill mechanism: sixteen situations, not sixteen independently played
or balanced encounters. It has forty-two standard catalogue definitions, with
additional directly authored Hearthford actors. The roster count remains nominal until the distinctness
audit and campaign play are complete.

The first serial integration run ran **331 tests in 183.812 seconds**, with
**one failure**: a frontier quest guard's assigned causal explanation was
overwritten by normal utility selection. Its ongoing intent may change, but
the original material duty is now retained alongside it. The next focused run
passed **29 tests in 13.755 seconds** including that regression; two strengthened
water/stair checks then passed in **0.338 seconds**. A new full run is required.
An 80×24 forced-position Darrin PTY exercised a warned collapse, structural
bracing using the billhook's levering capability, finite withdrawal,
accompanying attackers and the tailrace. It did not
complete an expedition or a rival-return campaign. Exact limits are recorded
with the encounter documentation.

The integrated **100-seed audit passed with zero failures in 207.953 seconds**:
eight regions per world, 100 distinct geography signatures per region, 281
whole-region encounter compositions, most repeated 28 times. All eight new
elite variants were generated (41–59 occurrences each). Its initial-position
check formerly skipped dormant actors; dedicated elite tests covered those,
and the general audit now checks dormant positions too. The subsequent full
suite covers that strengthened assertion; the 100-seed result predates it.
This audit does not complete quest branches or evaluate human counterplay.

The subsequent serial run passed **335 tests in 177.122 seconds**, including
dormant-position validation, retained quest-duty evidence, stair travel and
cross-level releases. `compileall` and `git diff --check` also passed. This is
the current integration result, not closure of the remaining content or PTY
gates.

The next 80×24 tactical fixture approached the peat crown, observed its corrected
mechanism notice, watched smoke and downwind reed ignition, and physically
braced its linkage. A nearby scavenger then picked up the dropped Ember Cloth
and withdrew with that actual item while a lynx continued its warned charges.
This exposed an overly broad material rule: bracing also quenched fire without
water. Bracing now repairs support only; burning fuel remains dangerous until
water, fuel exhaustion or another appropriate action resolves it. The thirteen
material tests passed in **0.997 seconds**, including that regression. The
previous 335-test result predates this one-line reaction correction.

Following the Marlbank trip, the frontier composer no longer spends worksite
slots on repeated wildlife already present in a separate pair. Six working
actors now prefer unseen definitions, use at most two of one definition, and
include a ranged worker when that region has one. Rillscar currently has no
standard ranged frontier worker; this is a content gap, not an audit success.
The new tests sampled 400 region/seed compositions, checked real placements
and reproduced/fixed pressure pursuit entering the courier tile: **three tests
passed in 0.982 seconds**. Existing ecology tests passed **16 in 8.058 seconds**.
The first composition regression run failed 59 subcases because the preference
still allowed three repeated signal callers; a hard per-definition cap fixed
those cases. Full-suite and live-play verification of this adjustment remain
due; the preceding 302-test result does not cover it.

The work-clothing benchmark adds a separate 64-cell sparse-material scenario
without changing the older smoke/water baseline scenario. In 24 samples,
ordinary input/layout was **16.512 / 16.785 / 16.968 ms** median/p95/p99;
sparse-material turns were **26.261 / 30.845 / 37.890 ms**. New-world median
313.210 ms, load 174.008 ms, save 13.523 ms, replay matched. Boarding remained
above the ordinary median target at 32.107 ms. Peak RSS was 142,348 KiB;
this larger harness now retains another material fixture and is not evidence
of a production memory leak or a successful long-session soak. Full JSON is
in `performance-work-clothing.json`. The PTY was idle except its final quit
during part of this run; native-driver input latency remains unmeasured.

Eighteen additional regional work garments bring protective clothing to 36,
six per body location. Fresh frontier containers and a bounded visiting
merchant spare expose all eighteen. Saved contents and existing equipment are
not replaced. Slot-local grip, buoyancy/load limits, narrow visor sight, wet
padding weight, cold/terrain protection, heat wear and lime/salt exposure use
production movement, guard, targeting and shared material actions. The sparse
smoke producer now uses the hyphenated status consumed by ranged/guard rules;
underground fire is no longer extinguished by surface rain.

The clothing regression file passed **15 tests in 4.386 seconds**; the full
serial suite passed **302 tests in 163.724 seconds**. Tests include real
format-6 migration, physical purchasing, condition, exposure, exact inventory
cancellation and 80×24/100×32 stock panels. These do not close all-content or
24-build acceptance gates.

A fresh, unforced 80×24 PTY campaign (`winter work clothing`, despite its name
played in spring) took Hessa Silt from immediate Hearthford entry/return,
through the chart to Marlbank, and completed its field-release ending.
Quay, market, flood-mark and roof stores were opened; kiln wrap, apron and
potter mitts were equipped. Spare starting clothing, arrows and a duplicate
apron were deliberately dropped. A seed-sack taker stole the load ledger,
reducing capacity; billhook combat defeated it and the same ledger
was recovered from the floor. Upper works supplied counted grain; a roof
clue revealed Odrin's named coffer. Pack overload made leaving the apron a
material decision. The kiln release was operated, late grain delivered and
the field branch chosen; physical return, save and quit completed at world
time 417, health 10/10. No cave or winter traversal, native mouse, or active
apron firefighting was exercised in this trip. The repeated animal charge
pattern and long encumbered return remain balance weaknesses, not successful
evidence of genuinely distinct new enemies.

The deck integration full suite passed **287 tests in 173.361 seconds**,
with compilation and `git diff --check` passing. Earlier iterations reported
four failures in 279 tests: one test left a regional vertical query aboard
Jomon, and three expected the deliberately replaced read-only ship stations.
The corrected tests use the appropriate spatial context and assert that
opening the new work preview still takes zero time. Additional regressions
exercise physical ship ammunition/theft, finite retreat, storm approach time,
shared crew exposure, save ownership, door/smoke cache invalidation and actual
hazard colours, rather than only count twelve voyage labels.

Twelve voyage families now have recorded responses; eight offer ordinary
movement/combat/material play across the connected decks. Mooring work spends
counted timber/provisions and action time. Regional populations remain separate
from temporary boarders; stolen shipments, corpses and dropped items keep their
physical ownership through resolution and succession.

Isolated real PTYs at 80×24 and 100×32 exercised forced raiders, two-rail
boarders, hold thieves, a rudder grazer, galley fire, storm and flooded hold.
The bowman telegraphed lanes, fired at vacated positions and was supported by a
shield bearer climbing the stair; the courier withdrew with cargo/hull loss.
Two raiders were defeated by guarded billhook combat after a foot strike.
A net bearer fought through the cargo hatch while a thief escaped with the
same physical grain shipment. The grazer was diverted with counted fish.
Flooding was secured at the lower station and residual water pumped away.
Saves and normal quits restored the terminal. These were forced verification
fixtures, not seven complete regional expeditions or natural-frequency data.

The first storm test failed its playability check: its twelve-action warning
expired before the twenty-seven-move approach. The warning now allows forty
actions; a fresh PTY repeated the normal walk and three-action intervention
with hull 10/10. Galley fuel burned out during the ordinary approach, leaving
ash and smoke; that test only demonstrates endurance and aftermath handling,
not an active manual firefight. The generic endurance resolution and some
reused boarding descriptions still need the later balance pass.

A follow-up isolated PTY continued the flooded-hold consequence with a forced
driftwood opportunity: one rope use recovered one wet timber lot, then the
courier physically descended to the lower repair station and spent that lot
and two actions to restore hull 8 → 10. A seated pilot interrupted the direct
chart aisle; the adjacent aisle and doorway remained usable. The saved result
has no remaining timber and keeps the earlier cargo, water and voyage history.

Boarding profiling exposed repeated vessel FOV calculation. A bounded,
reconstructed single-entry cache includes space, position, crisis state,
terrain and smoke; it never stores actors or save data. In two 24-sample
benchmarks, boarding input/layout median fell **60.230 → 31.696 ms**, p95
**64.465 → 32.311 ms**. Ordinary input/layout in the latter run measured
**16.726 / 21.753 / 22.880 ms** median/p95/p99. World generation was 316.392 ms,
load 169.968 ms, save 13.323 ms; replay matched. JSON evidence is in the two
`performance-boarding-*.json` files. Rendering uses a production layout sink,
not native-driver latency; the boarding median still misses the 25 ms ordinary
target. These are intermediate measurements, not the expansion's final soak.

The first full production seed audit completed **1,000 eight-region worlds**
with **zero reported failures in 2,410.5 seconds**, worst seed 3.842 seconds.
Every region had 1,000 distinct geography signatures; production population
grouping produced 1,303 unique compositions (most repeated composition: 260).
It checked deterministic regeneration and save round trips, required access,
placement/opening preparation, history references and seasonal chart return
paths. The process imported snapshot `4492919` before workshop/voyage edits;
this is not a final audit of subsequent content. It explicitly does not measure
human counterplay, full quest branches, all material-stage alternate routes,
native terminal latency or genuinely distinct mechanical content counts.

Concurrent commit `52d054c1402b4aa7314a0b03d72b2b321a03ad9f`
(`newhistory`) captured the pending workshop implementation and its initial
tests while further regressions were being added. Local `origin/main` also
advanced to that commit externally. It is preserved without rewriting; this
agent performed no push. The remaining test and documentation changes are
committed separately rather than pretending the captured code is a new diff.

Eight counted physical fittings now operate at the optional lower workshop.
Structure/treatment/lining sockets affect sound, material work, thrown-item
recovery, wet strings, smoke range, terrain protection, mass and local armour.
Removal is space-checked, mounted parts follow their parent through theft and
death, and zero-condition parts stop working without disappearing. The
focused suite passed **18 tests in 2.223 seconds**. An added blocked-shot
test initially compared the causal failure message as if inspection could
not log; its corrected assertion checks all state except that message.
The serial full suite passed **259 tests in 176.361 seconds**; compilation
and whitespace checks passed. That duration overlapped the 1,000-seed audit
and a PTY campaign, so it is not a controlled performance comparison.

Further inspection removed ash-wrap fitting from melee-only weapons and
resin sealing from arbalests whose preparation already ignores rain: neither
purchase would have provided its advertised benefit. A regression checks
these rejected combinations; the updated workshop file passes **19 tests in
2.145 seconds**. The later full suite must include that additional test.

Real PTY continuation of `working fen`: Dunmire → Hearthford → Willow Ferry
→ Reed Anchor → Charter Market, selling counted grain and paper at physical
gangplank services, then walking down to the lower workshop. At 100×32,
previewed/cancelled and confirmed a quiet binding on the original billhook.
Its extra weight crossed the encumbered threshold; the inventory explicitly
reported slower movement/weak-floor risk. Cancelled a trial repack, then
stored spare arrows and confirmed the lighter load. At 80×24, inspected the
mounted part and removal cost, declined removal, saved and quit cleanly.
The saved parent/part identity and depleted stock reload correctly. This is
one normal-play fitting purchase, not manual coverage of all eight kits or
all resulting combat builds. Four route legs were uneventful; no tactical
voyage claim is inferred from them.

`python -m jomon.systemic_audit --seeds N --start K` now audits actual expanded
worlds rather than independently sampled encounter plans. It compares complete
seed regeneration and save round trips, checks actor/objective/container and
vertical access, named history references, incompatible alert groups, seasonal
chart return routes and production budgets, and reports open-ground/cycle
metrics. Three initial seeds passed in 7.132 seconds. Four focused audit tests
passed in 2.260 seconds, including deliberately broken actor placement,
evidence and a stranded winter destination. This is not yet the 1,000-seed
result, nor verification of completed quest branches or all process stages.

The ecology continuation adds 24 frontier actor definitions and production
population composition, bounded local wildlife/work decisions, finite enemy
firefighting, ignition, drainage, bracing, treatment and signals, physical
scavenging, and perceived rival/prey attacks. Existing visited populations are
preserved rather than replenished. The archetype target is not closed: several
animal variants share routines and require further play differentiation.
`O` exposes actual visible duties and counters. Routine player attack no longer
auto-selects a peaceful grazer. Material or rival deaths release exactly the
same stolen physical item as direct player combat.

The fast suite passed **57 tests in 22.029 seconds**. Sixteen focused ecology
tests passed in **8.604 seconds**, including actual actions for all 24 new
definitions, finite supplies, actor budgets, non-omniscient perception and
item recovery. The first full run had **236 tests, one failure, 194.246
seconds**: an opening quest incorrectly assigned a grazing hare as its guard
and its AI then replaced the quest intent. The same defect was independently
visible in the fresh PTY run. Assignment now selects a non-animal guard;
the repeated full suite passed **237 tests in 184.571 seconds**. The suite was
run serially; this elapsed measurement overlapped interactive PTY work and is
not a controlled benchmark comparison.

Real 80×24 PTY, seed `working fen`: physical chart trip to Dunmire, normal
road approach, hare flight and `O` inspection, flood-mark chest with charcoal
mask/waders/arrows, warning and ignition by a cinder thrower, autonomous
movement and quenching by a pail keeper, drying-spill operation, movement out
of aimed sling lanes, Yara hand-in and the held-fuel ending, physical return,
save and clean quit. The saved state records world time 217, health 10/10,
completed stage 3 with `ending:h`, a wet extinguished resin feed, the thrower's
three spent fuel charges and three spent sling shots, and one spent pail.
No human-facing claim of all ecological encounters or requested build paths
is made from this trip. The earlier Dunmire breach ending remains separate
play evidence from seed `frontier voyage`.

Continuation checkpoint: `7bfda5e40418edeaaf614996d5d1ee434d38a92e`
(`newhistory`) committed the pending history changes outside the agent's
commit sequence. The worktree was clean and local `origin/main` pointed to
that same commit. It is preserved unchanged; the agent performed no push.
Five linked working-history events now exist for each generated region;
regional stock consumes supplies at bounded day boundaries, physical supply
deliveries influence institutional treatment credit, and `Z` separates
testimony, evidence and forecast. Ten focused history tests passed in
3.041 seconds at this checkpoint. This does not close the full historical
generation, institution or legendary-object acceptance gates.

The Dunmire play observation exposed an intact-floor sight defect: the old
cross-level query accepted two clear horizontal projections without checking
where the projectile crossed the floor. Three new regressions reproduced it.
Cross-level lanes now require an actual opening or exposed exterior edge;
underground shots cannot pass through unexcavated ceilings. Target previews
use the same level geometry. All 18 focused ranged tests passed in 6.463
seconds. Roof-edge and aligned-ladder shots remain supported.
The resumed full suite passed **217 tests in 153.283 seconds**. A prior
interrupted run lost its final result and is not counted. Compilation and
whitespace validation also passed at the sight-geometry checkpoint.

A second concurrent commit, `0eb080e1ba1ca3682dcab81436fac0ae6a3c47b9`
(`newhistory`), captured the pending sight changes before the agent's commit
ran. It was preserved; `54037bd` consequently records only the observed
verification result despite its intended sight-fix subject. History was not
amended to conceal this overlap.

Actual 80×24 Continue/Z inspection found that the history panel truncated
testimony and never exposed its final forecast. Information panels now wrap
and support arrows, page keys, Home/End and optional mouse wheel, without
advancing time. The same PTY save was reloaded, paged to the full final
forecast and quit cleanly. The displayed secondary-contact delivery choice
also lacked an input-dispatch case; it now reaches the physical supply
transaction. Four focused panel/production-input tests passed in 0.743
seconds. Their first run exposed a missing `addnstr` method in the test sink;
that fixture was corrected, not the production rendering boundary.

The first benchmark compares 24 observations per ordinary scenario (five cold
starts/world generations). JSON evidence is in `performance-baseline.json`
and `performance-optimized.json`. Input-plus-layout median fell from 102.782
to 30.784 ms; p95 from 120.295 to 35.849 ms. Movement median fell from 72.212
to 10.504 ms; populated auto-pack from 24.663 to 3.790 ms; new-world generation
from 1201.263 to 625.172 ms. Save size stayed 281347 bytes and replay matched.
Cold-import median increased from 144.241 to 275.707 ms in this small sample;
that regression needs investigation/repetition, not concealment. The 25-ms
ordinary median target remains open. Rendering measurements use production
layout into a sink, not a terminal-driver timing claim.

Packing uses exact bitset connected-component scoring; regional reachability
uses bitset frontiers with explicit aligned vertical links. One-entry FOV and
bounded path-fragment caches are disposable, unpersisted, and keyed by mutable
geometry/occupancy. Regression tests compare independent flood-fill results,
placement scoring, changed terrain/smoke, and reconstructed paths. The first
fast developer command, `python -m jomon.checks fast`, passed 54 tests in
20.478 seconds before three further cache/reachability regressions were added.
The full suite after optimization passed **184 tests in 140.561 seconds**.

Format 7 now introduces bounded sparse material fields; format-6 migration
adds empty overlays without repainting any existing geography or issuing
replacement possessions. `F` opens contextual material handling. Ignition,
water handling, bracing, breaking, digging and coffer movement use finite
supplies/tools and one action; inspection and invalid/cancelled choices are
zero-time. Reactions are limited to 64 nearby cells per action, 512 sparse
cells per location, with reconstructible chunk selection and collapse due
events. Water/fire/mud, wet lime, wind/rising smoke, seasonal ice and warned
support failure have focused reducer tests; integrated PTY and full format-7
verification remain open. Ordinary off-duty named adults cannot die from
routine exposure resolution; injuries and interrupted work are recorded.

Material integration verification: **196 tests passed in 140.111 seconds**;
the focused material file passed 12 tests in 1.169 seconds. Compilation and
`git diff --check` passed. An initial cross-level smoke test failed because a
landing with both up/down connectors exposed only the first link; the opening
query now examines the actual pair, and the regression passes.

Real PTY so far (not full expansion play coverage): seed `material voyage`,
80×24, fresh world and immediate E departure, F inspection/cancel, two steps
west along the quay, ignition of adjacent timber, observed downwind spread,
two river-water extinguishing actions, physical return, save and clean quit.
The saved courier had 9/10 health and five remaining oil measures; wet timber
and support damage survived. At 100×32, Continue loaded that save, a second
departure revisited the changed quay, material inspection fit, and physical
return remained available. These are short reaction-loop checks, not complete
questline or build demonstrations.

The geographic expansion now exposes sixteen chart nodes and eight regional
moorings. The four added maps are lazy: Dunmire Peat Isles (96×56), Rillscar
Iron Gorge (112×52), Marlbank Clay Terraces (104×60), and Frostmere Braided
Estuary (108×58), each with aligned -1/0/1/2 levels, two underground entrances,
an elevated worksite, two contacts and eight physical containers. Their
terrain generators use jittered fen islands, folded gorge cuts, shifted
irrigation bands and braided channels respectively. Each has an initial
material claim with two recorded settlements. These are connected content,
not yet the requested finished ecology, history, elite or multi-line depth.
The added encounters currently reuse old behaviours and are not counted as
new mechanically distinct archetypes.

The loader's exact-four-region check would rebuild Greywash, Greenwold and
Whitecairn after a fifth region was saved. It now repairs only genuinely
missing legacy geography, extends only new chart legs, and preserves existing
maps, schedules, inventory positions, changed routes and quest outcomes.
Regression checks exercise eight-region round trips and changed format-6
routes. New regional processes leave sparse persistent damage rather than
replenishing it on every visit.

Verification at this point: the first expansion full run had **202 tests,
one failure in 157.571 seconds**: an old test required Hearthford to have
exactly two chart neighbours. Its expected set now includes the two new
connected destinations. The repeated full run passed **203 tests in
148.083 seconds**. Eight focused frontier tests then passed in **6.578
seconds**, including a further adjacent-contact regression. The later full
suite must include that additional test.

Real PTY seed `frontier voyage`: at 80×24, walked to the upper-deck chart,
selected Dunmire, watched travel, descended to the gangplank and departed.
Opened the immediately visible quay chest (wreck key, gloves, finite dressing),
accepted material alteration, crossed the fen circuit, fought the lookout
with guard/billhook, observed the protector and elevated bow ward, operated
the drying spill and physically returned with foot/leg injury. The original
hand-in was obstructed by contacts stepping off the exact interaction tile.
Adjacent same-level conversation now works and has a regression. Continued
the saved world at 100×32, revisited the changed region, handed in the prior
control work, chose the breached-bank settlement, returned and saved/quit.
This is one completed branch across two expeditions, not both branches or
coverage of all four new regions. The saved state records the actual outcome.
Chart labels spilling into the details panel at 80×24 were also observed;
edge labels now place to the left, with supported-size bounds tests.

The earlier material PTY also shrank below minimum (70×20), showed the
minimum-size warning, recovered to 100×32, and quit with terminal restoration.

The previous milestone manually completed only one full Hearthford branch;
its other branch, arc, and build claims have automated rather than complete
PTY coverage. This expansion must not inherit those as playtest claims.

Fresh research and its access limitations are in
[`systemic-world-research.md`](systemic-world-research.md). No runtime network
dependency is introduced. Canon is unchanged. New scope permits only bounded
fittings, institutions, histories and regional systems, not universal engines.
