# Jomon — Authoritative Development Plan

## Authority and maintenance

This file is the source of truth for Jomon’s delivery process: product constraints, work order, task status, acceptance criteria, and open decisions. `LORE.md` remains the canonical setting and content reference; if the two documents conflict on execution, update this file to record the resolved decision before implementing.

Status markers have a strict meaning:

- `[x]` complete: implemented where applicable and verified against its stated acceptance criteria.
- `[-]` active: the single current implementation slice.
- `[ ]` planned: not yet started. Do not infer implementation from a planned item.
- `Decision`: needs explicit product-owner direction; do not fill it with an unrecorded assumption.

At the end of every completed slice, update this document: mark only verified work complete, add the verification actually run, split the next item when it becomes clearer, and record any decision that changes scope or direction. Do not recreate superseded plans or treat Git history as active direction.

## Current state

Jomon is being rebuilt as an original, no-magic late-medieval river-and-coast roguelike. The checked-in game is a substantial but superseded space-fiction prototype. It may inform architecture and testing technique, but its user-facing lore, vocabulary, assets, progression, save migrations, routes, and content are not requirements for the medieval game.

The documentation reset is complete. No medieval gameplay slice has been implemented or verified yet. The active implementation starting point is Phase 1.1.

## Non-negotiable product decisions

- Jomon is a persistent itinerant household on a river-basin-to-coast network.
- The player inhabits rotating Jomon crew members. Crew selection happens physically at Jomon’s tavern; a truly lost courier is permanent, subject only to exceptionally rare and explicit mystical safeguards.
- Jomon’s tavern, chart table, cargo hold, repair space, stores, berths, galley, and gangplank are walkable map spaces. Physical props use compact contextual key-choice prompts. An always-available, deliberately comprehensive management screen complements these surfaces; it does not replace their direct action or situated information.
- Gangplanks and quays replace airlocks and landing terminals.
- Trade uses physical commodities with capacity, condition, handling, loss, recovery, local supply, demand, and market consequences.
- Human conflict, wilderness, and operational hazards must receive equal systemic depth.
- Combat remains turn-based and grid-based, but its old content and progression model will be redesigned.
- Jomon is low-mysticism medieval fantasy. Rare relics, totems, boons, curses, rites, omens, and other uncanny forces can have real mechanical effects, but there is no generic mage class or unlimited free-form spellcasting.
- Simulation time advances only during active in-game play. All consequential randomness is seeded and inspectable.
- Jomon is an open-ended persistent world simulation, not a finite campaign to be won. Short-term goals provide direction; exploration, relationships, and the changing household/world provide the long-term play.
- The player is not bound to a single protagonist. On an active courier’s death, control transfers to another eligible crew member; voluntary perspective changes occur at the tavern.
- An active courier directly controls only themself. They can delegate work to crew and NPCs through conversation; delegation depends on the courier’s `conversation` stat and the other person’s relationship, role, capacity, interests, and current situation.
- Failure is causal and diegetic. A courier death, lost cargo, unpaid debt, damaged route, broken relationship, or ruined vessel can permanently change the world; the irreversible collapse or loss of Jomon ends that world’s play.
- Jomon can grow through new rooms, refitted workspaces, tools, crew capacity, and small craft. Every expansion is physical, persistent, grounded in available materials/labour, and balanced by upkeep, space, staffing, cargo, route, or social trade-offs.
- A continuing world has escalating difficulty eras: base, NG+, and NG++. Active-play progression in the same persistent world moves it toward later eras without a reset or finite campaign ending. Each era adds durable, diegetic pressures and new possibilities rather than merely increasing enemy numbers.
- The initial release target is a desktop browser game for players who enjoy ASCII graphics and keyboard-first control.
- Every instantiated person has a full individual persistent record—including family, work, needs, relationships, injury, possessions, birth, and death—while distant people advance through deterministic summary simulation at an appropriate fidelity level.
- The world has an expanding procedural frontier. Newly explored regions are generated causally and become persistently explorable as Jomon travels; rumours, charts, travellers, and trade can establish their existence before arrival.
- No user mod/content-pack compatibility is planned. Internal content remains data-driven and documented for maintainability, but does not promise a public extension API.
- The reset is a clean persistence break. Do not migrate the superseded space-era saves or reinterpret them as medieval campaigns.
- Do not copy lore, text, names, assets, or exact mechanics from other games. Historical and game references are influence constraints only.

## Rogue-inspired visual and interaction direction

Jomon takes clear, original inspiration from **Rogue (1980)**: a terminal-first, keyboard-led, turn-based grid game whose map is the primary visual explanation of play. This is an aesthetic and interaction constraint, not permission to copy Rogue’s copyrighted presentation or fantasy content.

- ASCII is a first-class, release-quality presentation—not a temporary fallback. The detailed renderer must expose exactly the same consequential state.
- Favor a sparse character-cell grid, limited purposeful colour, strong contrast, and immediate silhouettes over decorative pixels, painterly scenes, or interface clutter.
- The map communicates position, architecture, actors, cargo, props, hazards, routes, and discoveries directly. Every symbol must have a stable, original, documented meaning.
- Use a compact, persistent status readout and terse message feedback. The player should normally understand the current danger and affordances without opening a separate screen.
- Keep direct keyboard control and compact contextual prompts. Support familiar eight-direction movement where it improves play, alongside discoverable/remappable controls and accessible alternatives.
- Use original glyph assignments, palette, layout, copy, assets, sounds, names, and mechanics. Do not reproduce the Epyx/DOS graphics, box art, interface layouts, source code, or Rogue’s fantasy fiction.
- The medieval material world is never weakened to imitate Rogue: Jomon’s readable symbols must describe vessels, work, people, weather, cargo, tools, and grounded danger rather than spells or monsters.

## Emergent world and people direction

Jomon’s world should feel alive in the sense of Dwarf Fortress Adventure Mode, Caves of Qud, RimWorld, Rogue, and Risk of Rain: it has a remembered past, people pursue their own material and mystical goals, the player can build tactical power through legible combinations, and the player’s actions become part of later situations. The player experiences the world through a courier on Jomon, supported by both situated in-world surfaces and an always-available comprehensive management interface.

- The world evolves only while the player is actively playing. It does not advance while the browser is closed or the game is paused.
- A world begins with a causally generated history: geography and waterways first; then ecology, resources, settlements, institutions, people, routes, trade, and historical events. Its starting state must be inspectable. The world continues into a deterministic expanding frontier as Jomon travels rather than ending at that initial generated region.
- World generation aims for Dwarf Fortress-like depth and configurability, adapted to Jomon’s scale. Advanced settings cover region size, history length, climate, terrain and waterways, settlement and population density, political fragmentation, resource scarcity, ecology, dangers, era pace, and simulation fidelity. Players can select, save, inspect, and reproduce presets and advanced settings; every generated world records its seed, resolved settings, generator version, and validation/rejection result.
- Every instantiated person has persistent identity, location or home, role, material interests, family, work, needs, relationships, injury, possessions, birth, death, memories, and a readable history of consequential encounters.
- NPC actions must arise from original needs, opportunities, relationships, and local conditions—not an authored sequence disguised as simulation. Nearby and important people receive detailed simulation; every distant person retains full individual state and advances through deterministic scheduled summaries.
- The world must surface change through physical places, conversations, ledgers, rumours, goods, routes, visible work, and the always-available management screen. These surfaces are complementary and must not merely duplicate one another. A simulation that players cannot discover or act upon is out of scope.
- Jomon expansion and exploration are the provisional long-term motivations. Every future vessel upgrade must add a physical space, a new material capability, or a meaningful new trade-off.
- Short-term goals must always be available through local pressures, contracts, favours, shortages, discoveries, threats, or crew needs. They guide play without creating a mandatory campaign finish line.
- Escalation must keep the world generative after NG++: later eras remix and extend the same systemic content families, change world conditions and relationships, and create new material problems, opportunities, and vessel choices. They do not require a new save or a campaign restart.
- Base, NG+, and NG++ are tracked world eras, not separate worlds. Accumulated in-world active-play time and Jomon’s growth advance the era; Jomon and surviving crew remain continuous while the wider world changes. NG++ plateaus in raw escalation and continues to remix systemic content instead of becoming numerical inflation.
- Shadow of Mordor is only a high-level reference for the feeling that remembered people can react and recur. Do not implement or market a “Nemesis System,” reproduce its hierarchy/vendetta design, or derive from its protected implementation. Use an original, documented social-memory model instead.

## Ordered implementation phases

### 0. Documentation reset `[x]`

- [x] Replace the old canon with `LORE.md` and establish root `TODO.md` as the governing delivery plan.
- [x] Remove superseded active plans and obsolete playtest documentation rather than reviving or archiving them as current direction.
- [x] Keep `README.md` truthful: the checked-in game is a prototype and the medieval implementation begins with a clean break.
- [x] Record Rogue-inspired terminal presentation, emergent-world direction, and non-copying boundaries as product constraints.

Verification: manual repository review on 2026-09-01; documentation only. No build, test, or browser execution was run for this documentation slice.

### 1. Foundational world systems

#### 1.1 Clean game boundary `[-]`

- [ ] Define the new medieval save namespace, version, and invalid-save behavior. Existing prototype saves must be ignored or explicitly invalidated, never migrated.
- [ ] Establish medieval domain types for the vessel, crew, deck partitions, quay, vessel props, and active-play time.
- [ ] Define seeded game creation and inspection surfaces for the first vessel slice.
- [ ] Remove prototype terminology from all user-facing surfaces reached by the new-game path.
- [ ] Add focused unit tests for creation, determinism, invalidation, and no-migration behavior.

Acceptance: a fresh medieval game can be created deterministically; a prototype save cannot load as one; no space-era term appears anywhere on the new-game path.

#### 1.2 Deterministic world generation and configuration

- [ ] Define a versioned `WorldGenerationConfig`, named presets, advanced settings, validation constraints, and deterministic rejection/retry rules.
- [ ] Build the dependency-ordered generation pipeline: watershed geography and hydrology; climate and seasons; resources and ecology; settlement sites; institutions and people; then routes, trade, and pre-play history.
- [ ] Record a reproducible world manifest containing seed, resolved configuration, generator version, generated-world identifiers, and validation/rejection history.
- [ ] Add settings UI for preset selection, advanced configuration, seed entry/display, world-creation progress, result inspection, and saving/loading settings.
- [ ] Add generation snapshots and property/fuzz tests for determinism, valid geography-to-settlement dependencies, bounded generation, and readable diagnostics.

Acceptance: the same seed and resolved configuration reproduce the same valid world; changing a documented setting predictably changes the intended world property; a player can inspect and share the world manifest.

#### 1.3 Active-play world simulation and persistence kernel

- [ ] Define the simulation clock, event ordering, seeded random streams, and deterministic scheduler. It must advance only during active play.
- [ ] Define versioned world state and persistence contracts for geography, sites, routes, markets, people, institutions, history, and Jomon.
- [ ] Define deterministic fidelity tiers for loaded places, nearby named people, recurring agents, and distant settlement summaries.
- [ ] Define a versioned world-era model for base, NG+, and NG++ states; its active-play progression, transition conditions, inspectable causes, persistence, and configuration hooks must be deterministic.
- [ ] Add event sourcing or an equivalent inspectable causal history so world changes can be explained, replayed, and persisted within bounded storage.
- [ ] Add focused tests for reload equivalence, simulation determinism, paused/closed-session time, corruption recovery, and bounded state growth.

Acceptance: equivalent active-play time produces the same world state and era across replay and reload; the world never advances while inactive; a visible change or escalation has an inspectable causal record.

#### 1.4 People, conversation, and delegated work foundation

- [ ] Define persistent-person state: identity, household/site, role, material interests, skills, relationships, memories, current work, capacity, health, and commitments.
- [ ] Define the active courier’s `conversation` stat and how it changes delegation eligibility, negotiation, task clarity, trust, risk, and outcome without becoming supernatural persuasion.
- [ ] Define a constrained task/delegation model for crew and NPCs: offer, agreement/refusal, assignment, progress, interruption, outcome, and later memory.
- [ ] Make autonomous choices arise from original needs, opportunities, relationships, and local conditions. Distant people use deterministic summary simulation; nearby and recurring people use richer state and behaviour.
- [ ] Add original social-memory records and player-readable evidence through physical surfaces, messages, conversations, ledgers, rumours, goods, routes, and visible work. Do not reproduce proprietary named-system hierarchies or vendettas.
- [ ] Add deterministic tests for delegation, refusal, task interruption, memory, recurrence, and no-player-control autonomy.

Acceptance: the active courier can delegate a task through a conversation; the recipient’s response and outcome follow inspectable state; a later encounter visibly reflects the remembered result.

#### 1.5 Terminal presentation and interaction foundation

- [ ] Define a renderer-independent map, glyph, palette, status, message, prompt, input, and accessibility contract.
- [ ] Create an original ASCII glyph vocabulary for terrain, vessel parts, people, goods, work, hazards, weather, and routes; validate unique/legible use in a character cell.
- [ ] Implement keyboard-first eight-direction movement, compact contextual prompts, remapping, focus handling, command help, and accessible text labels.
- [ ] Implement the detailed renderer as an equal-information presentation of the same world state, with automated parity checks against ASCII output.
- [ ] Add renderer and browser tests for glyph meaning, prompt cancellation, status/message visibility, keyboard control, and ASCII/detailed parity.

Acceptance: all consequential state can be understood through the ASCII map, status, and message surfaces; the detailed view neither hides nor invents gameplay information.

### 2. Physical Jomon foundation

#### 2.1 Walkable Jomon and quay

- [ ] Create a compact, original ASCII deck plan with a connected quay approach, gangplank, tavern, chart table, cargo hold, repair space, stores, berths, and galley.
- [ ] Render the plan in both ASCII and detailed modes from the same map state; document its original glyph vocabulary.
- [ ] Implement grid movement, collision, camera/focus behavior, visibility rules if used, and inspectable seed state.
- [ ] Add a player-visible map legend/help surface without replacing in-world readability.

Acceptance: the entire vessel/quay plan is navigable and every required space is identifiable from map symbols alone.

#### 2.2 Crew continuity

- [ ] Define the initial household roster, roles, personal equipment, eligibility, and active-crew representation.
- [ ] Implement tavern-based voluntary switching through an operated physical prop.
- [ ] Implement permanent courier death/departure and deterministic transfer of the active perspective to an eligible surviving courier.
- [ ] Display crew availability and loss consequences in a physical vessel surface.

Acceptance: switch active crew in the tavern; lose an active courier; continue as the same eligible successor after reload; observe the lasting household consequence.

#### 2.3 Physical vessel interactions

- [ ] Implement proximity/operation rules for every Phase 2 vessel prop.
- [ ] Add compact contextual key-choice prompts, cancellation, keyboard remapping compatibility, and accessible text labels.
- [ ] Make the chart table, hold, repair space, stores, berths, galley, tavern, and gangplank each expose a distinct minimal action or readout.
- [ ] Persist prop state and show action feedback in the message/status surfaces.

Acceptance: every named space has a represented prop, an operation affordance, an accessible label, and a tested outcome.

#### 2.4 Quay-to-vessel browser proof

- [ ] Start at a quay; board by gangplank; walk to the tavern; select another crew member; operate a vessel station; return to and leave through the gangplank.
- [ ] Add actual-browser coverage for the full path and focused headless determinism coverage.
- [ ] Verify the same flow in ASCII and detailed modes.

Acceptance: the browser test completes this exact path through real user input with no abstract hub or terminal shortcut.

### 3. Physical trade and settlement economy

- [ ] Define a small, named commodity set with source, use, weight/bulk, condition, handling requirement, failure mode, and buyer for every entry.
- [ ] Add cargo capacity, loading, unloading, spoilage/damage/loss, recovery, and vessel-hold interaction.
- [ ] Build one named settlement profile with water relationship, labour, authority, services, demand, local pressure, and visible civilian purpose.
- [ ] Add a physical settlement trading location and one contract that creates a material burden, refusal outcome, delivery outcome, and later visible consequence.
- [ ] Model price/stock/demand changes as seeded persistent local state—not a global money-only shop.

Acceptance: acquire cargo at one physical location, transport it, deliver or fail it at another, and observe a durable market or relationship change on return.

### 4. Waterway routes and settlement network

- [ ] Define the first connected river/canal/estuary/coastal settlement network with named route profiles.
- [ ] Add seeded route knowledge, river condition, weather/season constraint, toll/access condition, and competing traffic where relevant.
- [ ] Make route comparison a chart-table action and departure/arrival a gangplank/quay action.
- [ ] Implement active-play-only route time and an inspectable route-reckoning readout.
- [ ] Make a route outcome change cargo, access, conditions, or local history in a later decision.

Acceptance: choose a route for a visible reason, travel only while actively playing, arrive through a quay, and see a persistent consequence on a revisited route or settlement.

### 5. Persistent world partitions and revisitation

- [ ] Give the vessel, quays, settlements, approaches, roads, waterways, and expedition sites stable world-space identities.
- [ ] Apply the foundation simulation-fidelity tiers to partition streaming, including deterministic catch-up and replay of distant settlement summaries.
- [ ] Persist crew, cargo, altered props, caches, actors, weather effects, route consequences, time, and camera direction within the new medieval schema.
- [ ] Stream only distant partitions; specify and test the resident partition budget.
- [ ] Define corruption recovery, incompatible-save behavior, and schema evolution before each persistence change.

Acceptance: alter a place, leave it, reload, return through normal play, and find the same consequential state while an unseen settlement and recurring agent have advanced deterministically without loading unrelated partitions.

### 6. Equal-depth hazards, conflicts, and regional ecology

- [ ] Apply the social-memory model to human-conflict content so the same people can recur as allies, rivals, witnesses, employers, or consequences without scripted plot progression.
- [ ] Exercise the active-play simulation scheduler through changing named people, settlements, markets, and routes.
- [ ] Populate the foundation history surfaces at Jomon and settlements with witness accounts, ledgers, rumours, notices, cargo evidence, and changed work sites.
- [ ] Implement one complete human-conflict contract: setup, readable intent, choice, resolution, persistent mutation, and later follow-up.
- [ ] Implement one complete wilderness contract using weather, terrain, exposure, animal behavior, or disease without supernatural cause.
- [ ] Implement one complete operational-hazard contract involving vessels, cargo, works, or infrastructure.
- [ ] Establish common telegraph, consequence, and follow-up interfaces so all three danger families have comparable depth.
- [ ] Add seasonal and local ecology state that changes trade, settlement life, routes, or tactics.

Acceptance: each danger family has a distinct playable scenario with readable counterplay and a visible later-world effect; at least one named person reacts to a remembered outcome through the original social-memory model.

### 7. Medieval tactical overhaul and grounded guardians

- [ ] Define the player’s grounded action vocabulary: movement, exertion/stamina, commitment, guard, parry, posture, targeted components, retreat, recovery, and seeded chance.
- [ ] Implement renderer-independent intent data, target previews, and positional responses for both presentation modes.
- [ ] Replace prototype magical abilities, monsters, and guardians in the new path with people, animals, machinery, wrecks, siege works, fortified positions, and industrial hazards.
- [ ] Create procedural multi-cell grounded guardians from original body, component, attack, terrain, reward, and aftermath modules.
- [ ] Test tactical determinism, readability, defeat/escape consequences, and interaction with crew/cargo/world state.

Acceptance: a player can read a telegraphed tactical problem, choose a grounded response, and carry its material consequence back to Jomon or a settlement.

### 8. Content families and procedural composition

- [ ] Create reusable authored families for settlements, waterways, workshops, markets, hazards, wildlife, human groups, tools, cargo, contracts, crew roles, and guardians.
- [ ] Create vessel-expansion families whose upgrades visibly alter Jomon’s deck, capabilities, upkeep, crew work, cargo decisions, or route options.
- [ ] Create era-aware content variants and generative combinations so base, NG+, and NG++ create new pressures and possibilities from the same grounded systems without content exhaustion or numerical-only scaling.
- [ ] Require generated places to declare material purpose, water/terrain/season relationship, local pressures, rewards, services, and downstream world links.
- [ ] Add seeded fixtures, validity checks, encounter-readability checks, and reward-distribution checks for each family.
- [ ] Grow content only after the family has a player-visible purpose in the vessel/trade/history simulation.

Acceptance: generation sweeps produce valid, distinct locations whose content influences a real later choice.

### 9. Coverage, playtesting, and art

- [ ] Cover every player-visible vessel, crew, trade, route, persistence, tactical, and death/replacement capability with focused tests and actual browser input.
- [ ] Recreate the playtest protocol only after the relevant medieval player surfaces and content families are stable.
- [ ] Produce original Rogue-inspired visual assets only after the map grammar, glyph taxonomy, and renderer requirements are stable.
- [ ] Verify ASCII and detailed modes convey identical consequential information and meet accessibility requirements.
- [ ] Run full verification and record exact pass/fail/skip status for each completed slice.

Acceptance: browser playtests and automation cover the complete core loop without prototype terminology or presentation dependencies.

## Open product decisions

These decisions should be recorded here with their answer before work depends on them.

- Decision — **World-generation settings:** which concrete world properties must users control in the first advanced settings screen, and which should remain generator-internal until their simulation meaning is proven?
- Decision — **Delegation visibility:** how should pending delegated work be shown on the map and in vessel/settlement surfaces without turning Jomon into an omniscient management UI?
- Decision — **Failure calibration:** which irreversible consequences should be common, rare, or guarded by explicit warning? Jomon’s collapse is terminal, so its causal chain must be readable and preventable in proportion to its severity.
- Decision — **Escalation cadence:** which inspectable combination of active-play time, in-world time, household growth, discoveries, or world events advances a world into NG+ and NG++? How much should users be able to configure this at world creation?
- Decision — **NG++ continuity:** after reaching NG++, how should ongoing escalation, recovery, and newly generated opportunities work without turning the world into unwinnable numerical inflation?

## Delivery and verification rules

- Work in one cohesive, maintainable system unit at a time. Keep exactly one roadmap item marked `[-]`; build foundations and their tests before expanding player-facing content.
- Before a high-risk save, renderer, world-streaming, or combat change, add a decision-complete implementation plan beneath the relevant item or in a narrowly scoped linked design document.
- Prefer documented domain types, small modules with explicit ownership, deterministic/pure simulation functions, bounded state, and narrow public APIs. Do not add a content exception that bypasses a foundation contract.
- Keep the code navigable for humans and AI agents: name data by its game meaning, state invariants beside their types, provide seeded fixtures, and update this plan when an architecture decision changes.
- Preserve unrelated changes. Do not clean, migrate, or reuse old saves outside the explicit clean-break implementation.
- Each implementation task needs focused tests first. Completed slices also run, when available: `npm test`, `npm run test:autoplay:tasks`, `npm run test:e2e`, `npm run build`, and `git diff --check`.
- Browser coverage must execute the feature through actual UI input; task-ID mapping is not sufficient.
- Record failed, skipped, unavailable, and incomplete verification plainly. A task is not complete merely because it compiles, has a test file, or resembles prototype capability.
- Keep consequential randomness seeded and expose enough state to reproduce a reported play session.
