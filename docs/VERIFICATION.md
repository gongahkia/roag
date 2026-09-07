# Expansion verification record

## Baseline

At starting commit `d54102e`, content validation reported 25 crew, 155
techniques, 60 enemies, 89 encounters, 10 events, 11 biomes, six worlds, and
18 each of boons, curses, and items. All 54 tests passed. An 80x24 terminal run
covered crew selection, exploration, salvage, combat, an enemy phase, and exit.

## Automated and bounded checks

- `python -m dumbest_dungeon --validate-content`
- `python -m compileall -q dumbest_dungeon tests`
- `python -m unittest discover -v`
- A 200-seed generation sweep covered all six layouts and all eleven biomes.
  It produced 200 distinct ordered four-biome selections. Every generated
  objective and hazard was connected to the start, and every normal/elite room
  remained inside its threat budget.
- Across that bounded sweep, the encounter director produced all five plans:
  459 combo, 255 disruption, 258 screen, 11 sustain, and 17 pressure rooms.
  Unique ordered formations per biome ranged from 12 to 63. These counts show
  variety in the sampled generation; they do not establish a win rate or prove
  balance.
- A deterministic gated-run test navigates to two optional objectives, resolves
  hazards, discoveries, events, facilities, normal contacts, and the Core, then
  verifies victory without clearing the world.

## Terminal and scenario play

- Seed 13579 at 80x24 rendered the new mixed-biome map, `K` objective markers,
  weighted route cost, Core access progress, contextual `B` biome field page,
  and keyboard navigation. Auto-path cancellation was repeated with `X`.
- Seed 8 at 80x24 followed an 18-tick route into a moving Ash Foundry patrol.
  Combat visibly applied Heat Haze, displayed a three-actor combo plan and
  complete compact intents, accepted an on-sprite target, flashed 12 damage,
  executed the enemy phase, and presented the next coordinated intent set.
- A second deterministic scenario used Cryonaut, Horticulturist, Diver, and
  Archivist. It stacked an item, gained a boon and curse card, triggered a biome
  hazard, opened the Core through one safe and one forced objective, lost the
  Cryonaut during a normal fight, and continued with three crew. It then grew,
  upgraded, removed, and transformed cards; cleared an elite; round-tripped a
  mid-boss save; and reached victory. This scenario used forced HP setup to
  reach the death and fight transitions quickly, so it is a functional flow
  check rather than a balance result.

## Remaining balance uncertainty

Normal, elite, and boss round targets have not been measured across
representative unassisted winning runs. The work verifies deterministic rules,
formation diversity, legality, UI accuracy, and complete flows, but does not
claim a global win rate, run duration, universally viable party composition, or
final numerical balance.
