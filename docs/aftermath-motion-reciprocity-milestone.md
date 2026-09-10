# Aftermath, Motion and Reciprocity — live assessment

## Starting evidence

- Start: `6f2e6abe56bfac32c2324c3e8ffa7b353e87544d`, `main`, clean and
  equal to local `origin/main` (0 behind, 0 ahead). No fetch or push was
  performed.
- Python 3.14.7 is the active shell interpreter. The application remains
  Python 3.11+ and save format 7.
- The production content verifier passes at the start with eight destinations,
  48 standard enemies, 16 elite situations, four named rivals, 36 weapons,
  36 armour pieces, 48 passives, 16 techniques, 35 tools/supplies/drinks,
  12 relics, 62 containers, 12 regional questlines, three cross-region arcs,
  30 persistent non-hostile adults, eight commodities and 12 voyage families.
- The preceding final suite and slow audit remain the starting correctness
  evidence: 421 tests in 175.189 seconds and 1,000 seeds / 8,000 regions with
  zero failures. They will be rerun on the finished tranche rather than treated
  as evidence for new work.
- A ten-seed shortest-path audit measured a landing–objective–landing median
  of 148 manual movement commands, with a 116–182 range, before contacts,
  optional sites, encounters or safer detours. Local travel has no route-follow
  command or deliberate pass/listen action.
- Standard enemies have 48 validated production signatures, but those rows
  reduce to four broad profiles, ten roles and four ranged-kind values. Threats
  carry scalar health/morale and selected item IDs rather than the courier's
  physical equipment and body-condition model.
- The prior manual assessment explicitly did not cover every quest approach,
  arc ending, build expedition, winter expedition, all-region campaign or a
  rival's complete retreat/return/death sequence. These are confidence gates,
  not implementation claims.

## Product decision

This tranche targets Caves-of-Qud-like decision density in Jomon's own bounded
setting, not raw map, object, faction or ability counts. The eight current
regions stay. Existing direct reducers, action-clock determinism, immediate
departure, physical inventory, terminal legibility and low-mysticism canon
remain authoritative.

Depth will come from four multipliers:

1. safe action-by-action route following and clearer contextual affordances;
2. physical equipment and bounded body conditions shared by courier and
   trained opponents;
3. completed regional outcomes generating visible aftermath play; and
4. new actors, stories, practices and vessel work that consume those systems.

No background simulation, generic ECS, quest DSL, skill tree, full anatomy,
new magic, infinite contracts or additional major region is planned.

## Implementation sequence

1. Add deterministic route-follow planning, interruption and replay tests;
   add a pass/listen action and context inspection without new key sprawl.
2. Add bounded shared combatant conditions, physical hostile loadouts and
   melee/reach target preview while keeping existing saves stable.
3. Add one visible prepared reaction, active practices and broader
   enemy/environment equipment use.
4. Add two ending-derived aftermath configurations per region, three bounded
   micro-sites per region and state-derived contracts with physical objectives.
5. Add eight regional aftermath questlines and two cross-region arcs using
   varied objective topologies rather than another delivery/control template.
6. Expand to 72 standard enemies, 24 elite situations and eight named rivals;
   new rows must exercise the shared physical systems, not only unique labels.
7. Add four cross-regional material interests, eight named adults, 16 active
   practices, 16 tools/preparations, four relics, eight vessel refits and 12
   voyage variants. Hold weapon, armour, container and major-region counts
   unless play exposes a concrete missing role.
8. Optimize, migrate additive format-7 state, audit content and determinism,
   run the full suite and seed audits, then complete recorded PTY scenarios.

## Acceptance gates

### Motion and interaction

- A remembered-landmark route command reduces safe manual movement commands
  by at least 60% on the eight landing/objective/return fixtures.
- Every automatic step uses the ordinary movement reducer and yields the same
  serialized state as the equivalent manual action sequence.
- Route following stops before newly perceived actors, sounds, incoming intent,
  material danger, injury, unexpected weather, changed burden or unexplored
  terrain. It never advances from real-time idleness.
- A zero-time preview exposes the route, next consequence and stop reason at
  80×24. A deliberate pass/listen action advances exactly one accepted step.

### Combat reciprocity

- Every trained standard human has a physical readied weapon and worn
  protection or an explicit material reason not to. These items can be worn,
  damaged, stolen, dropped, recovered and round-tripped.
- Courier and threat head/arms/legs/core conditions use the same bounded
  mechanical meanings. No exhaustive anatomy is introduced.
- Melee, reach and control attacks can select and inspect any legal target.
- Each major encounter presents at least combat, material/movement and
  social/evasion answers; severe reactions remain telegraphed.

### Aftermath and repeated play

- Each of eight regions has two ending-derived aftermath configurations. Each
  changes a physical route or worksite, a population relationship and a real
  service, market or contract.
- Eight new regional lines use at least six distinct objective topologies.
  No more than two share the same stage structure.
- Two additional arcs consume physical evidence and at least three regional
  outcomes each. Death, succession, defeat and item loss do not dead-end them.
- Sixteen bounded contracts arise from actual shortages, damage, migration,
  occupation, theft, fire, flood or weather. Participants, objects and sites
  are validated before presentation.

### Content multiplication

- Final targets: 72 standard enemies, 24 elite situations, eight named rivals,
  20 regional questlines, five cross-region arcs, 12 institutions, 38 named
  non-hostile adults, 32 techniques, 51 tools/supplies/drinks, 16 relics,
  eight vessel refits and 12 stateful voyage variants.
- Every new entry has a production path, inspection text, deterministic
  persistence, at least three system consumers and focused behavior coverage.
- Existing 36 weapons, 36 armour pieces and 62 containers gain additional
  users and changing contexts before receiving raw-count additions.

### Quality and performance

- Immediate `E` departure remains unchanged.
- Normal input/render and movement remain within 20% of the preceding
  same-machine readings; unusual heavy turns remain under 250 ms.
- The fast suite stays under 90 seconds. The full suite, 1,000-seed audit,
  content/encounter/quest/persistence/replay/living/soak audits, compileall and
  whitespace checks pass on final code.
- Real PTY coverage includes every new quest topology, all eight aftermath
  regions, physical hostile equipment recovery, winter travel, route-follow
  interruption, a complete rival lifecycle and an extended all-region save.

## Work log

- Planning and baseline recorded. Implementation gates remain open.

