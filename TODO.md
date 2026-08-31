# Jomon — Authoritative Roadmap

## Current state

Jomon is being rebuilt as an original, no-magic late-medieval river-and-coast roguelike. `LORE.md` is the canonical setting reference. The current code is a superseded prototype: it may be inspected for reusable implementation ideas, but its space-fiction terms, lore, user flows, saves, and content are not product requirements.

The old documentation was removed deliberately. Use Git history if historical implementation evidence is necessary; do not recreate archived plans or treat them as active direction.

## Non-negotiable product decisions

- Jomon is a persistent itinerant household on a river-basin-to-coast network.
- The player inhabits rotating crew members. Crew selection happens physically at Jomon’s tavern; death and departure are permanent.
- Jomon’s tavern, chart table, cargo hold, repair space, stores, berths, galley, and gangplank are walkable map spaces. Major actions begin by operating represented props and use compact contextual key-choice prompts, not abstract hub screens.
- Gangplanks and quays replace airlocks and landing terminals.
- Trade uses physical commodities with capacity, condition, handling, loss, recovery, local supply, demand, and market consequences.
- Human conflict, wilderness, and operational hazards must receive equal systemic depth.
- Combat remains turn-based and grid-based, but its old content and progression model will be redesigned.
- There is no literal magic or supernatural causality. Religion is background culture only.
- Simulation time advances only during active in-game play. All consequential randomness is seeded and inspectable.
- The reset is a clean persistence break. Do not migrate the superseded space-era saves or reinterpret them as medieval campaigns.
- Do not copy lore, text, names, assets, or exact mechanics from other games. Historical and game references are influence constraints only.

## Ordered implementation phases

### 0. Documentation reset — complete when this roadmap lands

- Replace the old canon with `LORE.md` and make this root `TODO.md` the sole active roadmap.
- Remove superseded space-era plans and obsolete playtest documentation rather than archiving them.
- Keep README truthful: the implementation is entering a clean-break rebuild, not already a complete medieval game.

### 1. Clean save boundary and walkable Jomon deck

- Namespace or invalidate prior persistence before introducing medieval save state; do not write a lore conversion migration.
- Replace the abstract hub with a persistent, navigable Jomon deck plan.
- Add physical tavern, chart table, cargo hold, repair space, stores, and gangplank interactions with compact contextual prompts.
- Implement voluntary tavern crew switching and deterministic replacement selection after a crew death.
- Prove the full browser path: board from a quay, walk to the tavern, select a crew member, operate a vessel station, and leave through the gangplank.

### 2. Physical trade and settlement economy

- Define named commodities, cargo capacity and condition, loading/unloading, contracts, pricing, stock, demand, losses, and recoverable goods.
- Put trade interactions at Jomon’s hold and compatible physical settlement locations.
- Make contracts, shortages, repair projects, tolls, and successful deliveries create durable local consequences visible on later visits.

### 3. Waterway routes and settlement network

- Replace interplanetary topology with river, canal, estuary, and coastal routes between named settlement profiles.
- Represent route knowledge, seasonal limits, tolls, river height, weather, access rights, and competing traffic as persistent, seeded state.
- Make route selection a physical chart-table action and route departure a gangplank/quay operation.

### 4. Persistent world partitions and revisitation

- Give Jomon decks, quays, settlements, approaches, roads, waterways, and expedition sites stable world-space identities.
- Stream only distant partitions while retaining crew state, cargo, defeated or displaced actors, altered props, caches, weather consequences, time, and camera direction.
- Define corruption recovery and clean-save behaviour before changing persistent partition schemas.

### 5. Equal-depth hazards, conflicts, and regional ecology

- Create concrete encounter contracts for human conflict, wilderness, and operational hazards: readable setup, telegraph, choice, outcome, persistent mutation, and follow-up.
- Add seasonal and local ecology systems with intervention, trade, settlement, and route effects.
- Ensure every persistent outcome appears in a player-visible vessel or settlement surface and changes a later decision.

### 6. Medieval tactical overhaul and grounded guardians

- Redesign tactical actions around stamina or exertion, commitment, guard, parry, posture, targeted components, scarce recovery, and seeded chance where appropriate.
- Keep intent data renderer-independent and preserve meaningful positional responses.
- Replace legacy guardians with procedural, multi-cell grounded threats assembled from original body, component, attack, terrain, reward, and consequence modules.

### 7. Content families and procedural composition

- Build authored, reusable families for settlements, waterways, workshops, markets, hazards, wildlife, human groups, tools, cargo, contracts, crew roles, and guardians.
- Require every generated place to have a material purpose, local pressures, readable rewards, and links to the trade and history simulation.
- Expand content only with generation validity, encounter readability, reward-distribution checks, and seeded fixtures.

### 8. Coverage, playtesting, and art

- Extend headless automation and real browser coverage for every player-visible vessel, trade, route, persistence, combat, and death/replacement capability.
- Recreate the playtest protocol only after the medieval content families and player surfaces are stable.
- Defer sprites and art production until gameplay contracts, content taxonomy, and renderer requirements are stable. Retain ASCII/fallback presentation until visual coverage is complete.

## Delivery and verification rules

- Work in one small playable vertical slice at a time. Before a high-risk save, renderer, world-streaming, or combat change, write a decision-complete implementation plan.
- Preserve unrelated changes. Do not clean, migrate, or reuse old saves outside the explicit clean-break implementation.
- Start with focused tests, then run `npm run test:autoplay:tasks`, `npm run test:e2e`, `npm run build`, and `git diff --check` for completed slices.
- Browser coverage must execute the feature through actual UI input; task-ID mapping is not sufficient.
- Report failed, skipped, unavailable, or incomplete verification plainly.
