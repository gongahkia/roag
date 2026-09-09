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
| Builds | 36 weapons, 36 armour, 64 passives/techniques, 32 tools/supplies, 12 relics, 24 demonstrated builds | open |
| Treasure | 60 persistent containers, history-backed objects, clue trails | open |
| Stories | 12 substantial regional lines, three arcs, alternate outcomes | open |
| Vessel/voyages | optional stations, twelve families, six physical deck encounters | open |
| Migration | real format-6 preservation, corruption and mid-reaction round trips | open |
| PTY | region/branch/build/campaign/crisis/resize/mouse paths requested by owner | open |

## Measurement and limitations log

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
