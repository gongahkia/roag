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

## Rogue-inspired visual and interaction direction

Jomon takes clear, original inspiration from **Rogue (1980)**: a terminal-first, keyboard-led, turn-based grid game whose map is the primary visual explanation of play. This is an aesthetic and interaction constraint, not permission to copy Rogue’s copyrighted presentation or fantasy content.

- ASCII is a first-class, release-quality presentation—not a temporary fallback. The detailed renderer must expose exactly the same consequential state.
- Favor a sparse character-cell grid, limited purposeful colour, strong contrast, and immediate silhouettes over decorative pixels, painterly scenes, or interface clutter.
- The map communicates position, architecture, actors, cargo, props, hazards, routes, and discoveries directly. Every symbol must have a stable, original, documented meaning.
- Use a compact, persistent status readout and terse message feedback. The player should normally understand the current danger and affordances without opening a separate screen.
- Keep direct keyboard control and compact contextual prompts. Support familiar eight-direction movement where it improves play, alongside discoverable/remappable controls and accessible alternatives.
- Use original glyph assignments, palette, layout, copy, assets, sounds, names, and mechanics. Do not reproduce the Epyx/DOS graphics, box art, interface layouts, source code, or Rogue’s fantasy fiction.
- The medieval material world is never weakened to imitate Rogue: Jomon’s readable symbols must describe vessels, work, people, weather, cargo, tools, and grounded danger rather than spells or monsters.

## Ordered implementation phases

### 0. Documentation reset `[x]`

- [x] Replace the old canon with `LORE.md` and establish root `TODO.md` as the governing delivery plan.
- [x] Remove superseded active plans and obsolete playtest documentation rather than reviving or archiving them as current direction.
- [x] Keep `README.md` truthful: the checked-in game is a prototype and the medieval implementation begins with a clean break.
- [x] Record Rogue-inspired terminal presentation, interaction, and non-copying boundaries as product constraints.

Verification: manual repository review on 2026-09-01; documentation only. No build, test, or browser execution was run for this documentation slice.

### 1. First playable vessel loop

#### 1.1 Clean game boundary `[-]`

- [ ] Define the new medieval save namespace, version, and invalid-save behavior. Existing prototype saves must be ignored or explicitly invalidated, never migrated.
- [ ] Establish medieval domain types for the vessel, crew, deck partitions, quay, vessel props, and active-play time.
- [ ] Define seeded game creation and inspection surfaces for the first vessel slice.
- [ ] Remove prototype terminology from all user-facing surfaces reached by the new-game path.
- [ ] Add focused unit tests for creation, determinism, invalidation, and no-migration behavior.

Acceptance: a fresh medieval game can be created deterministically; a prototype save cannot load as one; no space-era term appears anywhere on the new-game path.

#### 1.2 Walkable Jomon and quay

- [ ] Create a compact, original ASCII deck plan with a connected quay approach, gangplank, tavern, chart table, cargo hold, repair space, stores, berths, and galley.
- [ ] Render the plan in both ASCII and detailed modes from the same map state; document its original glyph vocabulary.
- [ ] Implement grid movement, collision, camera/focus behavior, visibility rules if used, and inspectable seed state.
- [ ] Add a player-visible map legend/help surface without replacing in-world readability.

Acceptance: the entire vessel/quay plan is navigable and every required space is identifiable from map symbols alone.

#### 1.3 Crew continuity

- [ ] Define the initial household roster, roles, personal equipment, eligibility, and active-crew representation.
- [ ] Implement tavern-based voluntary switching through an operated physical prop.
- [ ] Implement deterministic successor selection and permanent crew loss/departure state.
- [ ] Display crew availability and loss consequences in a physical vessel surface.

Acceptance: switch active crew in the tavern; mark one unavailable; reload; observe the same eligible successor and household consequence.

#### 1.4 Physical vessel interactions

- [ ] Implement proximity/operation rules for every Phase 1 vessel prop.
- [ ] Add compact contextual key-choice prompts, cancellation, keyboard remapping compatibility, and accessible text labels.
- [ ] Make the chart table, hold, repair space, stores, berths, galley, tavern, and gangplank each expose a distinct minimal action or readout.
- [ ] Persist prop state and show action feedback in the message/status surfaces.

Acceptance: every named space has a represented prop, an operation affordance, an accessible label, and a tested outcome.

#### 1.5 Quay-to-vessel browser proof

- [ ] Start at a quay; board by gangplank; walk to the tavern; select another crew member; operate a vessel station; return to and leave through the gangplank.
- [ ] Add actual-browser coverage for the full path and focused headless determinism coverage.
- [ ] Verify the same flow in ASCII and detailed modes.

Acceptance: the browser test completes this exact path through real user input with no abstract hub or terminal shortcut.

### 2. Physical trade and settlement economy

- [ ] Define a small, named commodity set with source, use, weight/bulk, condition, handling requirement, failure mode, and buyer for every entry.
- [ ] Add cargo capacity, loading, unloading, spoilage/damage/loss, recovery, and vessel-hold interaction.
- [ ] Build one named settlement profile with water relationship, labour, authority, services, demand, local pressure, and visible civilian purpose.
- [ ] Add a physical settlement trading location and one contract that creates a material burden, refusal outcome, delivery outcome, and later visible consequence.
- [ ] Model price/stock/demand changes as seeded persistent local state—not a global money-only shop.

Acceptance: acquire cargo at one physical location, transport it, deliver or fail it at another, and observe a durable market or relationship change on return.

### 3. Waterway routes and settlement network

- [ ] Define the first connected river/canal/estuary/coastal settlement network with named route profiles.
- [ ] Add seeded route knowledge, river condition, weather/season constraint, toll/access condition, and competing traffic where relevant.
- [ ] Make route comparison a chart-table action and departure/arrival a gangplank/quay action.
- [ ] Implement active-play-only route time and an inspectable route-reckoning readout.
- [ ] Make a route outcome change cargo, access, conditions, or local history in a later decision.

Acceptance: choose a route for a visible reason, travel only while actively playing, arrive through a quay, and see a persistent consequence on a revisited route or settlement.

### 4. Persistent world partitions and revisitation

- [ ] Give the vessel, quays, settlements, approaches, roads, waterways, and expedition sites stable world-space identities.
- [ ] Persist crew, cargo, altered props, caches, actors, weather effects, route consequences, time, and camera direction within the new medieval schema.
- [ ] Stream only distant partitions; specify and test the resident partition budget.
- [ ] Define corruption recovery, incompatible-save behavior, and schema evolution before each persistence change.

Acceptance: alter a place, leave it, reload, return through normal play, and find the same consequential state without loading unrelated partitions.

### 5. Equal-depth hazards, conflicts, and regional ecology

- [ ] Implement one complete human-conflict contract: setup, readable intent, choice, resolution, persistent mutation, and later follow-up.
- [ ] Implement one complete wilderness contract using weather, terrain, exposure, animal behavior, or disease without supernatural cause.
- [ ] Implement one complete operational-hazard contract involving vessels, cargo, works, or infrastructure.
- [ ] Establish common telegraph, consequence, and follow-up interfaces so all three danger families have comparable depth.
- [ ] Add seasonal and local ecology state that changes trade, settlement life, routes, or tactics.

Acceptance: each danger family has a distinct playable scenario with readable counterplay and a visible later-world effect.

### 6. Medieval tactical overhaul and grounded guardians

- [ ] Define the player’s grounded action vocabulary: movement, exertion/stamina, commitment, guard, parry, posture, targeted components, retreat, recovery, and seeded chance.
- [ ] Implement renderer-independent intent data, target previews, and positional responses for both presentation modes.
- [ ] Replace prototype magical abilities, monsters, and guardians in the new path with people, animals, machinery, wrecks, siege works, fortified positions, and industrial hazards.
- [ ] Create procedural multi-cell grounded guardians from original body, component, attack, terrain, reward, and aftermath modules.
- [ ] Test tactical determinism, readability, defeat/escape consequences, and interaction with crew/cargo/world state.

Acceptance: a player can read a telegraphed tactical problem, choose a grounded response, and carry its material consequence back to Jomon or a settlement.

### 7. Content families and procedural composition

- [ ] Create reusable authored families for settlements, waterways, workshops, markets, hazards, wildlife, human groups, tools, cargo, contracts, crew roles, and guardians.
- [ ] Require generated places to declare material purpose, water/terrain/season relationship, local pressures, rewards, services, and downstream world links.
- [ ] Add seeded fixtures, validity checks, encounter-readability checks, and reward-distribution checks for each family.
- [ ] Grow content only after the family has a player-visible purpose in the vessel/trade/history simulation.

Acceptance: generation sweeps produce valid, distinct locations whose content influences a real later choice.

### 8. Coverage, playtesting, and art

- [ ] Cover every player-visible vessel, crew, trade, route, persistence, tactical, and death/replacement capability with focused tests and actual browser input.
- [ ] Recreate the playtest protocol only after the relevant medieval player surfaces and content families are stable.
- [ ] Produce original Rogue-inspired visual assets only after the map grammar, glyph taxonomy, and renderer requirements are stable.
- [ ] Verify ASCII and detailed modes convey identical consequential information and meet accessibility requirements.
- [ ] Run full verification and record exact pass/fail/skip status for each completed slice.

Acceptance: browser playtests and automation cover the complete core loop without prototype terminology or presentation dependencies.

## Open product decisions

These decisions should be recorded here with their answer before work depends on them.

- Decision — **Campaign shape:** is the primary experience an open-ended household career, a finite campaign with a definite ending, or a hybrid? What constitutes a successful run?
- Decision — **Failure contract:** when the active crew member dies, does play immediately continue as a selected successor, return to the vessel for succession, or offer another structure? What vessel/cargo loss is possible?
- Decision — **Tactical scope:** is the player always one active crew member, or can expeditions eventually include controllable companions? If companions exist, how much direct control is desirable?
- Decision — **First vertical-slice scope:** should Phase 1 remain vessel/quay only, or must it already include a short shore expedition and one hazardous return route to prove the game’s core feeling?
- Decision — **Audience/platform:** is desktop keyboard-first browser play the target release platform, or should touch/controller support shape the initial interaction design?

## Delivery and verification rules

- Work in one small, playable vertical slice at a time. Keep exactly one roadmap item marked `[-]`.
- Before a high-risk save, renderer, world-streaming, or combat change, add a decision-complete implementation plan beneath the relevant item or in a narrowly scoped linked design document.
- Preserve unrelated changes. Do not clean, migrate, or reuse old saves outside the explicit clean-break implementation.
- Each implementation task needs focused tests first. Completed slices also run, when available: `npm test`, `npm run test:autoplay:tasks`, `npm run test:e2e`, `npm run build`, and `git diff --check`.
- Browser coverage must execute the feature through actual UI input; task-ID mapping is not sufficient.
- Record failed, skipped, unavailable, and incomplete verification plainly. A task is not complete merely because it compiles, has a test file, or resembles prototype capability.
- Keep consequential randomness seeded and expose enough state to reproduce a reported play session.
