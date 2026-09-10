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

- Planning and baseline recorded.
- Interruptible remembered-landmark route following and the one-action
  pass/listen verb are integrated and replay-tested.
- Human threats now carry persistent physical weapons and protection; shared
  harm applies wear, injuries, material conditions, disarming, recovery and
  exact surviving-kit drops.
- All eight regional endings now produce one of two later-visit aftermath
  configurations, each with three changed sites plus population,
  institutional, route and market consequences.
- Sixteen finite aftermath contracts are playable through a physical witnessed
  copy and either a real commodity delivery or material field work. Settlement,
  paper loss, failure, ledger inspection and mid-contract round-trip are tested.
- Their field paths now use sixteen named objective topologies across drainage,
  fire/fuel, structural support and physical recovery families. Those paths
  alter material cells plus demand, route safety, firebreak state or a named
  treasure lead rather than sharing one cosmetic repair reducer.
- Every readied weapon now opens the same zero-time target cursor. Legal
  melee/reach targets cycle explicitly; minimum range, cover, intended effect,
  hostile intent, physical protection and known counters are previewed before
  committing the exact selected target.
- Six reach families can prepare one named, one-action reaction from that
  cursor or ordinary guard. It fires only when the selected actor enters or
  attacks through valid measure, uses shared harm/equipment rules, and visibly
  expires otherwise; hostile reach attacks retain the same warn/commit rhythm.
- The eight ending-derived aftermath lines now bring the regional total to 20.
  Two additional physical-record arcs unlock only after three specified
  aftermaths are actually completed: Scars Kept in Use changes material scars,
  institutions and route risk; Refuges at Low Water changes winter access,
  weather exposure, trust, freight time or cargo risk. Together with the three
  retained arcs, the story network now has five arcs and eleven endings.
- The standard roster now contains 72 validated roles, nine per region. The 24
  additions are ordinary production actors using finite quenching, bracing,
  drainage, kindling, support-cutting, escort, hunting and physical scavenging
  reducers; Hearthford places one of its three additions on every new seed.
  All 72 have distinct mechanics-driving signatures and display glyphs.
- The elite roster now contains 24 situations: 23 catalogue entries plus the
  map-authored crown wheel. Eight aftermath-only encounters add backwash,
  physical salvage, firebreak, counterfall, siphon, cover-lever, clay-slip and
  ice-boom rules. Four are named, persistent rivals, bringing that total to
  eight; their retreat, stocked return, injury, lost kit, witnessed settlement,
  permanent death and distinctive physical reward use the retained lifecycle.
- Four travelling material interests connect pairs of regions, bringing the
  institutional total to 12. Eight new physically scheduled adult witnesses
  bring persistent non-hostile characters to 38. Real dependency transfers
  earn separate network trust; that trust opens a one-time route shelter,
  lowers connected cargo/weather exposure and records an obligation. The
  relationships, opposition, service and acts are inspectable and migrate into
  existing format-7 states without moving retained contacts.
- Sixteen additional learned practices now bring the technique total to 32.
  Eight are taught by trusted travelling witnesses and eight by completing
  both contracts in a regional aftermath. Their distinct reducers affect wet
  and muddy movement, noise, difficult salvage, structural guard, smoke and
  storm sight, loaded crossings, elevation range, ice, tool-free material
  work, injured climbs, reach withdrawal and physical net recovery. Practice
  descriptions are inspectable on the courier and every acquisition is
  remembered; focused production and behavior tests cover all 16.
- Sixteen finite aftermath preparations bring tools/supplies/drinks to 51.
  Every settled contract issues the preparation taught by its exact work
  topology. The contextual field-use menu previews readiness and selects a
  named physical supply without spending time on a failed choice. The 16
  distinct reducers lower water, repair weapons, tend lamps and smoke, recover
  ground items, quench or start fire, mark stores, bind footing, plug banks,
  relieve survival pressure, brace structures, break aimed lanes, drain mud,
  create ash smoke, freeze shallows or thaw ice. Each consumes its packed item,
  records the consequence and round-trips through ordinary format-7 state.
- The remaining relic, vessel and voyage content multiplication gates remain
  open.
