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
- Baseline full suite: rerun in progress. The previous assessment's 177 tests
  in 567.579 seconds is historical, not this milestone's measurement.
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

The previous milestone manually completed only one full Hearthford branch;
its other branch, arc, and build claims have automated rather than complete
PTY coverage. This expansion must not inherit those as playtest claims.

Fresh research and its access limitations are in
[`systemic-world-research.md`](systemic-world-research.md). No runtime network
dependency is introduced. Canon is unchanged. New scope permits only bounded
fittings, institutions, histories and regional systems, not universal engines.
