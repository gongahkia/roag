# Seamless Hearthford milestone

## Bounded implementation plan

This owner-directed milestone replaces only the expedition's room graph. Jomon
remains a separate compact deck; household, tavern preparation, material trade,
relationships, defeat, succession, merchant, save, and action-clock rules
remain direct parts of the existing `GameState` and action path.

1. Generate one deterministic 96x54 Hearthford region on four aligned
   z-levels (-1, 0, 1, 2). Use small region-specific value fields, constrained
   river carving, anchored structures, direct road carving, and bounded
   underground excavation. Repair critical paths after validation.
2. Place the quay, settlement, floodplain and Reedwood, a small watch
   structure, stacked millworks, roof and gantry spaces, underground culvert,
   quiet travel, objective, and optional treasure in continuous coordinates.
3. Add pure camera, line-of-sight, exploration-memory, vertical-opening, and
   reachability functions in `world.py`. Curses shows current sight normally,
   remembered terrain dimly, unknown terrain hidden, and never remembers
   moving actors.
4. Keep one deterministic turn path. Add bounded patrol movement, sound across
   adjacent levels, periodic weather, local smoke and water propagation,
   pressure escalation, and one timed objective change. None advances while
   input is idle.
5. Deepen the six authored weapons through the existing attack and guard
   controls. Add a bounded passive catalogue, finite supply counts, persistent
   containers, and two finite rule-changing relics. Tavern preparation gains
   passive selection with an explicit carry limit.
6. Increment the development save schema to 3. Room coordinates and state are
   not reliably convertible to continuous `(x, y, z)` geography, so format 2
   saves are rejected with a clear message and no migration chain.
7. Add focused unit coverage, then complete three full expeditions in real
   PTYs, including multiple builds, vertical traversal, treasure, changed time
   state, save/reload, resize, and terminal restoration. Record only performed
   checks in this document's final assessment.

Excluded: additional regions, a reusable biome toolkit, general fluids,
structural engineering, physics, acoustics, item or encounter scripting,
onboard attacks, graphical tiles, distant simulation, and save-migration
infrastructure.

## Assessment

Pending implementation and verification.
