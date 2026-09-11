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
- Four ending-specific relics from the two new aftermath arcs bring the finite
  relic total to 16. Common-work rivet repairs equipment and supports at a
  fatigue cost; counterclaim lodestone applies physical disarmament to both
  sides; lee-cloth brooch creates an eight-action storm shelter shared by
  movement, sight and ranged preparation while reporting its position; and
  channel-surety shuttle crosses a real water lane while leaving the heaviest
  cargo at the launch. Each branch issues exactly one physical, inspectable
  relic and tests its production, drawback, persistence and cross-system use.
- Eight optional refits now attach to seven real working-station glyphs aboard
  Jomon. Each consumes one physical cargo lot, accountable credit and three
  actions at the matching station; preview and cancellation are zero-time.
  Galley cover, bilge strainers, storm backstay, cargo netting, keel shoes,
  hatch felt, signal shutter and sling cot alter fire, flooding, exposed work,
  theft, shoals, winter exposure, navigation/inspection and injury treatment.
  Their effects and drawbacks are inspectable, persistent and covered by eight
  focused tests plus the retained vessel, material and deck-crisis suites.
- Concurrent commit `3fe2d7b64b8c493a2e61db986eb0a04b9b9836ec`
  (`newoneadded`) captured the vessel-refit implementation while focused tests
  were running and also appeared at `origin/main`; this agent did not create or
  push it. Follow-up `2167780` makes the winter test explicitly remove warm
  clothing so it proves the refit's cold-water effect rather than protection
  from the starting outfit.
- All 12 retained voyage families now have one deterministic stateful variant
  caused by current markets, cargo, institutional obligations, aftermath,
  route memory, regional fire history, season, weather, vessel wear, fitted
  signals or prior outcomes. The voyage panel states cause, changed rule and
  counters. Variants alter real actors, targeted cargo, sparse fire/water/
  support fields, action costs, hull damage, rewards or obligations; outcome
  IDs and consequences persist by voyage number and corrupt active IDs are
  rejected. Seven focused tests exercise all 12 variants and the retained 27
  deck-crisis tests still pass.
- Concurrent commit `b4a7d0f45ef27ea3dfe41ba02eef42eab75454b4`
  (`provisionnewdesk`) captured the initial variant catalogue and active
  travel marker while integration was in progress and also appeared at
  `origin/main`; this agent did not create or push it.
- During closing verification, the local remote-tracking reference advanced
  independently from `b4a7d0f` to the already-existing local documentation
  commit `80f8761`. It introduced no new commit or file change, required no
  merge, and this agent performed no push.
- The first final benchmark exposed pathfinding repeatedly resolving display
  glyphs, scheduled adults and containers for passability. Regional path
  queries now use the equivalent base-terrain, ice and combatant rules while
  vessel queries retain full furniture/actor collision. A focused regression
  prevents display scans from returning to this hot path. Enemy-heavy turn
  time fell below the starting baseline and standalone pathfinding improved by
  roughly one third.

## Performance result

The same Python 3.14.7 interpreter, macOS host, seed and 20-sample command were
used for the preserved start and final measurements. Full median, p95, p99 and
worst-case data is machine-readable in `performance-aftermath-baseline.json`
and `performance-aftermath-final.json`.

| Scenario | Start median | Final median | Change |
|---|---:|---:|---:|
| ordinary input + 80×24 layout | 12.525 ms | 12.655 ms | +1.0% |
| ordinary movement | 7.684 ms | 7.982 ms | +3.9% |
| boarding input + layout | 29.355 ms | 29.923 ms | +1.9% |
| boarding turn | 14.222 ms | 13.941 ms | -2.0% |
| enemy-heavy turn | 37.674 ms | 26.808 ms | -28.8% |
| sparse environmental turn | 11.311 ms | 14.859 ms | +31.4% |
| pathfinding | 20.852 ms | 11.868 ms | -43.1% |
| new world | 342.410 ms | 497.498 ms | +45.3% |
| save / load | 14.225 / 151.994 ms | 16.479 / 155.274 ms | +15.8% / +2.2% |
| inventory open / auto-pack | 3.749 / 3.513 ms | 5.518 / 4.452 ms | +47.2% / +26.7% |
| 80×24 render | 4.277 ms | 4.700 ms | +9.9% |

The ordinary responsiveness scenarios remain within the 20% gate. New-world
creation remains below half a second and far below the three-second ceiling
despite instantiating the expanded physical enemy kits, institutions,
witnesses and aftermath state. Inventory opening/packing and the artificial
64-cell reaction fixture exceed 20% proportionally because they now snapshot
and process that additional physical state; their final medians remain 5.518,
4.452 and 14.859 ms and are the documented user-visible cost of exact item
ownership and richer material work. The heaviest gameplay p99 is 32.363 ms for
boarding input/layout; the enemy fixture is 28.561 ms and ordinary regional
input/layout is 13.444 ms. Save size rose
18.5%, from 318,795 to 377,814 bytes, while peak resident memory fell 13.3%,
from 42,208 to 36,580 KiB. Deterministic replay remained exact.

## Closing automated evidence

- The serial suite passed 505 tests in 322.355 seconds immediately before the
  final path-query optimization. After that optimization, 38 navigation,
  reachability, material, cover and frontier tests passed in 12.909 seconds.
  The exact committed tree then passed all 506 tests in 384.377 seconds.
- The final fast developer suite passed 62 tests in 32.391 seconds, under its
  90-second target.
- Four independent, deterministic batches covered seeds 0–999. All 1,000
  worlds and 8,000 regional instances passed with zero failures. Every batch
  produced 250/250 unique geographies for every regional family, and the
  slowest seed was 9.502 seconds while all four batches contended for the same
  machine. Unique production compositions per batch were 389, 375, 373 and
  397.
- The final encounter audit checked 900 plans: 246 unique compositions, 241
  production compositions, 317 elite occurrences, zero invalid groups, zero
  unreachable actors and zero unavoidable opening attacks.
- The final quest audit checked 800 generated regions, 20 regional lines, 40
  regional endings, five cross-region arcs and eleven arc endings with no
  unreachable or invalid path.
- Content verification reports the exact target counts below with no failures.
  Format-7 round trip, deterministic format-6 migration, old region and item
  identity preservation, and corrupt-save rejection all passed.
- One hundred deterministic replay samples of twelve actions passed. The
  100-sample living-world audit found no disconnected or nondeterministic route
  graph, schedule failure or vessel overlap.
- A 100-leg long-session soak remained between 302,490 and 305,336 serialized
  bytes. Traced post-warm-up growth was 7,460 bytes; detailed voyage provenance
  is limited to twelve entries while older occurrences fold into bounded
  summaries. Material, message, history, item, sound and alert bounds passed.

### Verified production counts

| Content | Count | Content | Count |
|---|---:|---|---:|
| major regions | 8 | standard archetypes | 72 |
| mechanically distinct standard signatures | 72 | elite situations | 24 |
| named recurring rivals/boss figures | 8 | weapons | 36 |
| armour/protective clothing | 36 | passives | 48 |
| inherent/learned techniques | 32 | active passives + techniques | 80 |
| tools, supplies and drinks | 51 | finite relics | 16 |
| persistent containers/caches | 62 | generated legendary objects | 8 |
| regional questlines | 20 | cross-region arcs | 5 |
| institutions | 12 | persistent non-hostile adults | 38 |
| commodities | 8 | voyage families | 12 |
| tactical voyage families | 8 | stateful voyage variants | 12 |
| demonstrated build scenarios | 24 | vessel refits | 8 |

The eight hostile named figures fill rival, regional-leader and boss roles in
ordinary narrative space; there is deliberately no separate arena-boss
catalogue. Counting those figures with the 38 persistent non-hostile adults
gives 46 named persistent people, but the verifier keeps hostile and
non-hostile counts separate.

## Recorded real-terminal evidence

Four actual `curses` sessions used an 80×24 PTY and restored the alternate
screen cleanly on normal quit:

1. A fresh `after audit` world began at the gangplank, departed immediately
   with `E`, walked Hearthford, previewed a remembered route, followed it to a
   witness, interrupted a return on queued input without taking an extra
   action, resumed to the landing, returned, saved and quit.
2. A controlled winter Hearthford save crossed shallow floodwater without warm
   protection. The courier became wet and chilled, and contextual inspection
   showed soil/coating, water depth, support, predicted reaction and all eleven
   material verbs before a clean quit.
3. A controlled Hearthford equipment encounter previewed a toll watch's
   intent, morale, protection and counters, defeated it, and inspected the
   exact staff, cap and working coat on the ground. This session exposed and
   led to correction of ambiguous defeat prose.
4. A controlled Dunmire session completed Veyra Reedlock's full lifecycle:
   visible retreat, return funded by two units of actual charcoal stock,
   retained injury and changed intent, permanent defeat, exact physical kit
   drop, unique Ebbglass Spindle reward, return aboard and save.

Automated production scenarios exercise all sixteen new objective topologies,
both outcomes of each new line, all eight aftermath families, all 24 build
identities, the four arc relic outcomes, every refit and all voyage variants.
Those are automated scenarios, not claimed as end-to-end manual play. This
closing pass did not manually replay every quest branch, every build in a full
expedition, native mouse input, a complete all-region campaign, macOS/WSL
platform variants, or an injected terminal failure. Existing targeted layout
tests cover 80×24, 100×32, undersize resize recovery and mouse/keyboard parity;
those remaining manual paths are confidence work, not hidden implementation
claims.

## Acceptance assessment

All implementation, numeric-content, deterministic generation, persistence,
bounded-work, immediate-play and automated correctness gates are closed. The
Qud comparison is intentionally a scoped possibility-density assessment, not a
claim of parity with its map count, mutation catalogue, faction volume or
fifteen-plus years of authored content. The most valuable later pass is the
mixed-situation and active-mastery programme in `docs/qud-parity-audit.md`, not
another label-count expansion.
