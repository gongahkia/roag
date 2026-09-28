# New Jomon design record

**Status:** canonical product direction for the next game.  This document is
deliberately separate from the current implementation baseline.  A statement
marked **DECIDED** is an approved target, not evidence that the current engine
already provides it.  **OPEN** items need a later product decision.  A
**PROVISIONAL** item is a reversible implementation recommendation, not canon.

The repository currently ships an engine and a non-playable template pack, as
described in [ENGINE_BASELINE.md](../ENGINE_BASELINE.md).  It does not ship this
game yet.

## Decision ledger

### DECIDED — identity, world, and time

- Jomon is a turn-based, step-by-step, open-world cyberpunk immersive
  simulation.  The player directly controls one operative rather than a squad.
- The world persists after an operative dies.  A surviving crew member with
  their own prior history and relationships can be selected as a successor.
- Personal histories and world history vary procedurally.  Live systems should
  generate stories from causal state, rather than add unsupported explanatory
  prose after the fact.
- The game starts with a few deep, revisitable districts and grows outward.
  Terrain and placement vary.  Urban, ruined, and altered-natural regions are
  eventual categories, not named geography.
- The eventual game has explicit in-game time, individual NPC schedules, and
  inhabitants with ordinary lives and meaningful relationships despite danger.
- Autonomous developments may connect local incidents to wider consequences
  while the player is elsewhere.  Their outcomes require causal support in
  state and rules.
- The atmosphere is grounded and bleak, with strange cultures and technology.
  Environmental oppression must be a mechanical condition, not merely prose.

### DECIDED — crew, survival, and work

- The operative is an equal crew member.  The game has no permanent explicit
  leader.
- Collective decisions normally use voting.  Members normally commit to an
  approved decision even if they dissented.
- A shared home, supplies, and emergency fund coexist with personal supplies
  and money.  The engine must not automatically pool every possession.
- Housing, food, fatigue, health, equipment upkeep, and related pressures are
  part of ordinary survival.  Their exact meters and rates are not fixed.
- Ordinary work, trade, scavenging, and paid assignments are viable livelihoods.
  Combat work is not the only sustainable source of income.
- Routine work consumes an in-game time block and may be interrupted by
  meaningful events; the player need not manually enact every repetitive step.
- The opening operative begins at the crew base with an assigned mission and
  then goes on it.  This does not create a crew commander or decide how that
  assignment was agreed.

### DECIDED — operations, bodies, and skills

- Missions are outcome-oriented and support multiple genuinely workable
  methods.  An employer, discovered information, relationships, or personal
  initiative can motivate an objective.
- Combat is tactically substantial but not the only centre of play.  Preparation,
  positioning, visibility, smoke, weapon differences, technology, and physical
  interaction matter.
- Hacking approaches coexist.  A separate network space is the intended main
  hacking experience and connects digital information, credentials, and
  physical-world access and interactions.
- The game has explicit upgradeable statistics and a skill tree.  Bodily
  augmentation alters capability and interaction; it is distinct from tools or
  learned skills.

### DECIDED — death and inheritance

- A successor may inherit a deceased operative's neural implant and/or physical
  body components.
- Neural inheritance prioritises memories and genuinely learned abilities.  It
  can include information, credentials, relationship knowledge, travel
  knowledge, and traces of personality that occasionally remark on events.
- Retention is selective and player-controlled: it is an inventory-like memory
  management choice, never automatic accumulation of every predecessor's full
  mind.
- Material not retained is lost unless preserved in physical storage or another
  explicitly acquired mechanism.
- Drives, discs, or setting-appropriate equivalents can archive material.
  Recovering the deceased implant is the normal recovery route.
- A previously acquired licensed neural uplink or service may support remote or
  cloud recovery.  It is a fictional local simulation capability; it requires
  no real cloud service, subscription, Bluetooth integration, or network API.
- A successor still chooses what to retain.  This neither resurrects a body nor
  automatically transfers social trust.

## Architecture commitments for this game

The existing engine boundary remains authoritative:

```text
content/system definitions + stable IDs
                ↓
 GameSession → authoritative reducer → GameState
                ↓
 immutable renderer-neutral views + transient runtime events
                ↓
 Debug Pygame renderer and ASCII Pygame renderer
```

- Rules, costs, prerequisites, access, hostility, schedules, resources, RNG,
  progression, and consequences belong in explicit engine-owned systems.
- Stable semantic IDs, including entity, method, outcome, route, and action
  IDs, identify mechanics.  Names, descriptions, glyphs, menu positions, pixel
  positions, and lore text never identify a rule.
- A content pack owns concrete definitions and presentation.  `lore.json` is
  non-mechanical; `connections.json` is non-mechanical narrative context.  A
  relationship that changes access, reputation, behaviour, routes, or costs
  needs an owning mechanical representation.
- Both Pygame renderers consume the same session, commands, views, events, and
  selected pack.  Different text, glyphs, fonts, assets, or renderer choice may
  never change RNG, outcomes, or a save's mechanical meaning.
- Runtime events are renderer feedback.  They are transient and cannot become
  a hidden durable chronicle.  Durable history, causal records, and memory
  packages require explicitly persisted game state.
- Fictional organisations, hacking, and remote neural recovery are local game
  simulation features.  Runtime LLM generation, a grammar library,
  event-sourcing infrastructure, and real external services are out of scope.

## OPEN decisions

These are intentionally not settled by this record or by an implementation
agent:

| Area | Open decision |
| --- | --- |
| Crew governance | Crew size; voting threshold, ties, absences, refusal exceptions, task allocation, property and access permissions. |
| Survival | Exact meters, rates, recovery, upkeep, food, housing, fatigue, and environmental-pressure rules. |
| Economy | Initial shared/personal resources, inheritance of private assets, ordinary-work detail, and pricing model. |
| Continuity | No-survivor fallback, memory capacity/package size, procedure for copying/loading, integration rules, whether personality traces learn after death, remote-sync rules. |
| Character growth | Exact stats, skill-tree structure, competency taxonomy, augmentation categories, and interaction rules. |
| World | First-district identity and lore, geography, cultures, organisations, and the exact off-screen simulation resolution. |
| Network play | Network-space topology, action grammar, discovery/credential rules, trace/security rules, and how it connects to physical systems. |
| Opening frame | How the opening assignment was approved by an equal crew. |

Deferred work is not rejected work.  An absent feature must not be filled with
an assumed default merely because it appears familiar to another cyberpunk game.

## REJECTED for the present design

- Reintroducing the deleted pre-reset fictional world, its Dullest Dungeon
  subsystem, old browser or curses frontends, or archived roadmaps as current
  requirements.
- A permanent crew commander as a shortcut for the opening assignment.
- Automatic total pooling of property, automatic full-memory inheritance,
  bodily resurrection, or automatic social-trust transfer.
- Treating narrative `connections.json` rows, displayed prose, glyphs, or
  presentation assets as mechanical authority.
- Requiring real cloud, network, subscription, Bluetooth, or LLM services for
  fictional hacking or neural recovery.

## PROVISIONAL implementation recommendations

These recommendations only guide the early slices described in
[new-jomon-first-playable.md](new-jomon-first-playable.md).  They may be
replaced without retconning the approved design.

1. Use authored variation over structured facts for early histories and local
   consequences.  Add generative systems only when a concrete causal model
   needs them; do not add runtime LLM generation.
2. Build one compact local operation first.  Its location and proper setting
   name are **OPEN**; early stable IDs and concise provisional presentation are
   implementation scaffolding, not world canon.
3. Make the first operation demonstrate an equipped-tool path and a combat path
   through explicit state and interactions.  The fuller physical/network split
   follows as a dedicated network-access slice because the current baseline has
   no network system.
4. Retain save format 16 for the first extension if optional, defaulted state
   fields can keep the baseline synthetic save readable.  Bump only when a
   tested migration boundary requires it.

## Earlier ideas that remain PROPOSALS, not canon

Snapshot backups, separating stored skills from integrated skills, knowledge
freshness markers, an illustrative pumping-facility extraction, and any
particular district/faction/person name are proposals only.  They must be
reviewed as product choices before becoming content or mechanics.

## Non-negotiable authoring checks

Before adding a system or content definition, ask:

1. What stable ID identifies the entity, action, method, outcome, or relation?
2. Which owning system carries its rules and persistence?
3. Which fields are presentation and may be rewritten by a pack?
4. Can the same seed, state, and semantic commands reproduce the outcome?
5. Can Debug and ASCII render the resulting immutable view without reading
   mutable state or parsing prose?
6. Does a save store durable mechanical identity and state, while leaving
   events, panel state, fonts, and renderer settings out?

If a proposed relationship changes play but can only be expressed in lore or a
narrative connection, stop and add or design the appropriate typed mechanical
system instead.
