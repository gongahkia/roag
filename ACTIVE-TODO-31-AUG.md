# Active TODO — 31 Aug: Jomon open-world completion

## Purpose and non-negotiable direction

This is the authoritative handoff for the next Codex session. `Jomon` is both the game name and the name of the carrier; do not call the carrier “Jomon Voyager”. The game is a persistent, mostly procedural **planetary-frontier** roguelike: Jomon travels between planetary sites and settlements rather than treating space travel as a loading-screen menu.

The objective is not merely to add more maps. Player decisions must change a living sector through travel, cargo, trade, ecological incidents, faction control, combat, and failure. Preserve the current space retheme and use a coherent space-native taxonomy for every new player-facing concept.

Do not claim a subsystem is complete just because its data model or a smoke test exists. Verify its full player-facing path in deterministic headless tests and the browser UI.

## Current implementation: retain, but do not overstate it

- A sector graph, linked sites, generated connector runs, three-chunk transit residency, airlocks, contracts, route caches, simple market/site state, and a sector clock exist.
- Entering a connector currently creates a transit run and arriving recreates the destination run. This is not camera-continuous streaming across airlock, route, and landing.
- Contracts currently load on departure and deliver on arrival automatically. The hold capacity and recoverable cache foundations are useful, but player cargo operation is not implemented.
- `patrol`, `hazard`, `trader`, and `ecology` are generated route-situation labels, not yet a complete persistent encounter system.
- The renderer has sprite manifests and fallback drawing. Sheet URLs are deliberately unset, so no generated asset set is active.
- Every biome already has a guardian contract. These are not sufficient “actual bosses”: the requested system needs larger, varied multi-cell procedural guardians and authored encounter modules.

Read the relevant current engine, renderer, storage, tests, and build configuration before changing any of these systems. Preserve unrelated worktree changes.

## Delivery order

Complete the work as small vertical slices. Each slice must leave the game playable, include deterministic tests, and finish with the narrowest relevant checks followed by `npm run test:autoplay:tasks`, `npm run test:e2e`, and `npm run build`. Report skipped or failed verification plainly.

### 1. Space-native content foundation

- Audit legacy biome, enemy, item, boss, prop, UI, and narrative names. Replace them with one planetary-frontier content bible before adding systems that multiply old terms.
- Curate reusable content families: planetary regions, settlements, factions, site purposes, salvage, tools, hazards, fauna, equipment, cargo, and guardian themes.
- Keep the macro sector procedural. Do not make the game a fixed linear campaign; procedural locations must nevertheless satisfy authored composition, reward, objective, and lore rules.
- Treat the current mythic terminology as legacy to replace, not intentional cross-genre ancestry.

### 2. In-session living-sector clock and persistent state

- Replace wall-clock dependence with an explicit game simulation clock. It advances only while the game is running through actions, rests, travel, and simulation ticks; it must not progress while closed.
- Make the clock drive contract deadlines, prices, route conditions, ecological events, faction activity, and site recovery/decline.
- Persist site, route, cache, cargo, faction, and event state. Death creates a replacement-courier opportunity, but cargo, route damage, reputation, caches, site changes, and resolved events remain in the world.
- Keep simulation and combat randomness seeded and reproducible. Save enough state to explain deterministic outcomes after loading.

### 3. Sector operations and player-operated cargo

- Replace automatic acceptance/delivery with an interactable manifest and market terminal on Jomon and at compatible settlements.
- Let players inspect, accept, decline, load, unload, abandon, recover, buy, sell, and route cargo. Enforce clear hold capacity and show the consequences before confirmation.
- Build a sector operations screen with discovered routes, route length/risk, faction control, active contracts, cargo demand, prices, ecological alerts, integrity, and disruptions.
- Expand the current market model into deterministic supply, demand, stock, prices, shortages, competing carriers, route disruption, and faction control.
- Make effects durable and visible: deliveries can stabilize a site, loss/piracy can create scarcity, repair can reopen a route, and control can change access or offers.

### 4. True world continuity and resident streaming

- Introduce stable world-space coordinates and a resident-window system spanning site interiors, airlocks, docks, connector corridors, and destination approaches.
- Remove the boundary that discards the active run at connector completion. Walking through an airlock must retain hero state, facing, camera direction, local props, enemy states, cargo, route state, and game time.
- Stream and evict only sufficiently distant partitions while persisting their mutations. Re-entry must restore defeated actors, opened routes, altered props, caches, and resolved encounter state.
- Provide safe handling for missing/corrupt partition state and define save migration/reset behaviour explicitly before changing persisted schemas.

### 5. Route encounters, ecology, and revisitation

- Turn each route situation into a concrete encounter contract: setup, telegraph, choice, outcome, world mutation, and follow-up. Examples:
  - patrols: inspection, faction heat, access, or pursuit;
  - hazards: damage, lost time, tool use, salvage, or a route block;
  - traders: conditional offers and market intelligence;
  - ecology: radiation, debris, habitat failure, invasive organisms, evacuation, repair, or containment.
- Ecology must be a sector system, not only local enemy-stat modifiers. Incidents should span multiple sites, evolve using the simulation clock, and admit player intervention.
- Add reasons to revisit: changing markets, expiring work, crisis escalation, repair projects, faction relationships, companion leads, site transformation, and route discoveries.
- Require every persistent encounter outcome to appear in the operations view and affect at least one later decision.

### 6. Tactical combat overhaul and procedural guardians

Jomon remains a grid-based turn game. Do **not** replace it with continuous action combat, real-time hitboxes, or a Souls-style action renderer.

- Build a tactical Souls/Fear & Hunger hybrid: deterministic stamina, readable enemy intent, attack commitment, guard/parry resolution, RNG-based dodge/parry outcomes, posture/poise pressure, targeted components, scarce recovery, and meaningful preparation.
- Surface the chance, stamina cost, target, and consequence of each combat action in the UI and test trace. Random outcomes must come from the seeded game RNG, never ambient time or browser randomness.
- The inspiration is mechanical only. Use bio-industrial vulnerability, scarcity, and unsettling atmosphere, but exclude sexual violence and graphic sexual content.
- Replace one-tile guardian assumptions with multi-cell actors: a footprint, occupied cells, component targets, independent component states, movement constraints, telegraphs, arena interactions, and rewards.
- Make bosses procedural guardians assembled from authored modules: body plan, component set, attacks, phase transitions, arena hazards, resistances, reward, and sector consequence. Existing guardian contracts should be migrated into this module library.
- Ensure larger enemy types are common enough to validate the system outside boss fights.

Official design references are influence boundaries, not sources to copy: [Elden Ring’s official guide](https://en.bandainamcoent.eu/elden-ring/news/elden-ring-starter-guide-tips-know-playing-the-game) describes stamina, dodge rolls, and reading intent; [Fear & Hunger’s Steam page](https://store.steampowered.com/app/1002300/_/?l=english) describes turn-based combat. Research primary/official sources where useful, document conclusions briefly, and create original mechanics/content.

### 7. Curated levels, tools, items, and bosses

- Curate a broad, reusable library of planetary layouts, encounter rooms, set pieces, traversal problems, routes, tools, utility items, weapons/modules, cargo, settlements, hazards, fauna, and guardian components.
- Every tool must serve at least two contexts where possible: combat, environmental traversal, cargo recovery, route repair, hazard mitigation, or escape.
- Every generated site needs a readable purpose, a thematic resource/pressure, meaningful rewards, and a lore-grounded relationship to Jomon’s sector.
- Do not increase content volume without generation validation, encounter readability checks, reward distribution checks, and explicit test fixtures for new mechanics.

### 8. Complete test and autoplay coverage

- Extend the deterministic headless task catalogue to cover every player-visible capability: manifest choices, buying/selling, price changes, deadline failure, route recovery, ecological progression, persistent streaming, multi-cell enemies, guardian phases, death/recovery, and save/load.
- Add real Playwright flows for each capability. Browser tests must perform UI input and assert visible results; task-ID mapping alone is not coverage.
- Keep autoplay as a test heuristic, not a gameplay authority. Update its priority queue and fixtures whenever player controls or world outcomes change, and make impossible/unimplemented actions fail visibly.
- Keep test cases seeded, focused, and diagnosable. Add property/generation tests when new procedural contracts introduce validity invariants.

### 9. Deferred final phase: sprites and ImageGen

Do not generate or activate sprites until the rethemed content taxonomy, multi-cell actor contracts, combat actions, and renderer requirements are stable.

- Activate the existing manifest/fallback pipeline rather than building a parallel renderer.
- Use Codex ImageGen’s built-in image-generation workflow, one distinct transparent asset/sheet prompt at a time. The target is dark bio-industrial pixel art, with readability secondary only where it does not compromise playability.
- Produce transparent, animation-ready sheets that match the renderer’s established grid, frame, and manifest requirements. Inspect every output, preserve alpha, save approved files under the project, update manifest/URLs, and run the existing sprite validation.
- Do not partially enable sheets that leave terrain, props, items, actors, effects, or animation states uncovered. Retain ASCII/fallback rendering until coverage is complete.

## Research and implementation protocol

1. Start each phase by inspecting the current implementation and tests; do not assume this document matches the repository after other work lands.
2. Research only the decisions that need external evidence. Prefer official documentation and primary sources for technical choices. Cite concise findings in the relevant implementation notes; do not copy assets, text, UI, or proprietary gameplay content.
3. Propose a decision-complete implementation plan before a high-risk schema, renderer, or combat-loop change.
4. Make the smallest coherent diff per vertical slice. Do not refactor unrelated systems to make a feature easier.
5. Verify focused tests first, then the full required project checks. State uncertainty and incomplete verification rather than implying completion.

## Completion standard

Jomon can be called an open-world planetary-frontier roguelike only when a player can make meaningful cargo, route, combat, and intervention choices; travel physically and continuously between persistent world partitions; observe the living sector react; encounter varied multi-cell guardians; and exercise every capability through both deterministic headless and browser tests.
