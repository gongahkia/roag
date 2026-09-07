# Expedition systems expansion

This plan records the verified baseline and the acceptance criteria for the
deckbuilding, encounter, and biome expansion. It is intentionally based on the
repository at `d54102e`, not on approximate prompt counts.

## Verified baseline

- Content validation reports 25 heroes, 155 technique cards, 60 enemies, 89
  encounters, 10 events, 18 boons, 18 curses, 18 items, 11 biomes, and six
  world layouts.
- All 54 existing `unittest` tests pass on Python 3.11.
- A manual 80x24 terminal run covered the hub, route selection, auto-pathing,
  salvage, combat, card play, an enemy phase, and clean exit.
- The save format is version 7 and serializes Python's random state.

## Findings

1. A hero death immediately sets `phase = "defeat"`; cards owned by a dead
   hero therefore have no cleanup path. Confirmed routes are driven entirely by
   the UI loop and do not poll for cancellation between tile steps.
2. Card quantity overstates upgrade variety: 148 of 155 upgrades preserve the
   base effect structure and only seven alter it. Rewards sample uniformly from
   the selected archetypes, with no duplicate pressure, setup/payoff awareness,
   or build-shaping choice.
3. The original derelict pool contains 35 enemies, while each newer biome has
   only two or three. Authored templates mask this scarcity for a while, but
   repeated biome fights draw from very small functional pools.
4. Biomes currently differ in names, glyphs, rooms, cards, enemies, and
   encounter lists, but their JSON has no mechanical traversal, visibility,
   patrol, facility, hazard, or battlefield rules.
5. Encounter assembly scores roles, threat, synergy labels, and formation, but
   enemy targeting is mostly resolved at execution. At 80x24, even three
   current intents can truncate, obscuring setup/payoff information.
6. The Overseer Core is not gated by expedition objectives, so a generated
   route can reach the final fight without the intended exploratory commitment.

## Milestones and acceptance criteria

### 1. Audit and design record

- Preserve the passing baseline and clean tree.
- Record inspected counts, observed defects, research sources, and implementation
  constraints in versioned documentation.

### 2. Expedition rule foundations

- A hero death removes that hero's cards from every combat zone and the
  persistent deck, collapses surviving ranks, and does not end combat.
- Only a full-party wipe ends the expedition; death state round-trips through
  save/load and remains visible in combat and exploration UI.
- `x` or Escape cancels auto-pathing between atomic tile steps; right-click is
  the optional mouse equivalent. No subsequent light, patrol, hazard, or
  movement update occurs.
- Deterministic tests cover continued combat, dead-card cleanup, a wipe,
  route cancellation, interruption, patrol timing, and save/load.

### 3. Build-shaping deck rewards and upgrades

- Card rewards remain restricted to living selected archetypes, but explicitly
  surface a setup/payoff bridge, a low-duplication option, and a seeded wildcard.
- Card tags are derived and validated from effects plus optional authored tags;
  reward generation never produces duplicate choices or a dead-owner card.
- At least one defining card per archetype receives a structural upgrade or an
  upgrade with a meaningful tactical tradeoff, rather than negligible scaling.
- Deterministic tests cover reward composition, upgrades, removal, curses,
  boons, item stacks, rank legality, and save/load.

### 4. Encounter director and intent contract

- Procedural assembly selects a readable plan (pressure, setup/payoff, screen,
  disruption, or sustain) and validates role, synergy, formation, and budget.
- Add biome enemies only where a missing role or combo partner is demonstrated;
  enemy defensive actions can guard allies and repeated-action weighting reduces
  monotonous loops without making intent selection deterministic.
- Targets chosen for an intent are serialized and displayed. Execution remains
  actor-bound through rank collapse, and any legal retarget/skip rule is shown.
- A four-enemy formation has complete, compact intents at 80x24.

### 5. Biome mechanics: industrial group

- Derelict, Foundry, Reactor, and Cryogenic each receive distinct hazards,
  route costs, patrol rules, visibility, facilities/objectives, and combat rules.
- Generation stays connected and mechanics round-trip deterministically.

### 6. Biome mechanics: organic group

- Hydroponic, Fungal, Flooded, and Ossuary meet the same dimensional criteria,
  using mechanics that interact with wounds, movement, healing, stress, and
  Death's Door rather than renamed damage tiles.

### 7. Biome mechanics: information group and boss access

- Archive, Storm, and Void meet the same dimensional criteria through card-flow,
  timing, light, visibility, and formation effects.
- Four seeded biome objectives are reachable; completing any two unlocks the
  Overseer Core. The UI states requirements and progress, and full clearing is
  unnecessary.

### 8. Integration and balance hardening

- Validate all JSON references and the new save schema.
- Exercise every biome alone, representative mixed four-biome seeds, all six
  layouts, encounter plans, objective reachability, and deterministic replay.
- Run the complete suite and manual terminal playthroughs covering death,
  cancellation, deck growth, facilities, normal/elite/boss combat, and save/load.
- Document only measured outcomes; do not claim a run duration, global win rate,
  or universal party viability.

