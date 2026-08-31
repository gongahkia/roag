# Lore implementation plan

## Purpose and authority

`LORE.md` is the canonical fiction and content reference for Jomon. It supersedes prototype presentation and terminology where they conflict, but it does not by itself authorize gameplay changes. `ACTIVE-TODO-31-AUG.md` remains the delivery-order and verification handoff until a product decision replaces a conflicting requirement.

The implementation target is an endless, persistent industrial science-fiction courier roguelike. The Jomon is the independently governed carrier, its General Manifest is its durable institutional memory, and each expedition carries one individually described sealed package. Procedural content must create campaign-specific history while preserving hard canon, generated canon, reported truth, and protected mystery.

## Non-negotiable content rules

- Name the carrier **Jomon**, never “Jomon Voyager”; remove the destination-to-New-Edo campaign premise.
- Keep the route endless, causal, and non-chosen-one. A courier can die permanently while the Jomon, its crew, contracts, world state, and General Manifest persist.
- Treat packages as individual sealed custody objects, not generic cargo units. Their observable state, contract clauses, seal state, custody history, and outcomes must be durable and player-visible.
- Use industrial, material explanations. No literal magic, factual gods, universal species cultures, omniscient codex answers, or a universal postal authority.
- Preserve protected mysteries: especially the origins and relationships of Orphan Works and hyperlight. Record evidence and claims, not final explanations.
- Make every generated settlement legible through material environment, ecology, settlement/industrial logic, dependency, power, competing actors, route relationship, present crisis, residue, and ordinary life.
- Give every recurring faction, rival, and culture material interests, internal disagreement, memory, and a relationship to transport. Species is never a faction or a monoculture.
- Keep ASCII and detailed tiles as equivalent views of the same game state. They must expose the same occupancy, hazards, telegraphs, package state, interactions, and uncertainty.

## Current-state gaps and decisions to lock

| Topic | Current prototype | Lore requirement | Plan treatment |
| --- | --- | --- | --- |
| Carrier identity | Code and UI say “Jomon Voyager.” | The carrier is Jomon only. | Retheme all player-facing text and content before expanding systems; preserve serialized IDs only through an explicit migration. |
| Campaign premise | Generation carrier travels toward New Edo and can complete campaign tiers. | Endless postal route; no canonical final destination or terminal victory. | Remove/replace final-destination framing before presenting the new route loop; preserve only mechanics that support durable histories. |
| Cargo | Four generic unit types, automatically loaded/delivered. | Individually described sealed packages, negotiations, seals, custody, and multiple resolutions. | Replace the model through a versioned migration; do not layer package prose over automatic cargo. |
| Factions | Global prototype faction IDs with fixed modifiers. | Regional institutions with material dependencies and local memories. | Introduce generated faction records before replacing control/market logic; retain prototype data only for migration fixtures. |
| Simulation | Wall-clock reconciliation progresses a closed save. | Worlds act without the player; active play must make the change visible. | Use an explicit persisted simulation clock advanced by actions, rest, travel, and defined simulation ticks; no closed-save progression. |
| Local world continuity | Transit has a resident window, but connector arrival recreates destination-run state. | The physical delivery route and its consequences persist. | Establish partition persistence and world-space identities before content relying on revisitation. |
| Combat presentation | Current project roadmap locks turn-based grid combat. `LORE.md` calls for continuous real-time grid movement. | Both cannot be implemented together. | Keep turn-based combat pending an explicit product decision; this plan does not authorize a real-time conversion. Lore telegraphs map to visible windups and threatened tiles in either model. |

## Ordered implementation phases

### Phase 0 — Canonical content boundary and migration contract

Create a single content taxonomy for Jomon, couriers, contract parties, package classes, seal states, route intelligence, settlements, institutions, species, Orphan Works, hazards, equipment, and recurring actors. Maintain a terminology migration map for existing player-facing names and separately document any serialized ID that cannot be renamed without save migration.

Add content validation that rejects forbidden legacy player-facing terminology in newly authored material, while allowing a temporary explicit migration allowlist. Record provenance for every lore entry: hard canon, seed-generated canon, reported truth, or protected mystery. Do not generate art in this phase.

### Phase 1 — General Manifest and explicit active-session history

Replace the implicit mix of campaign records, chronicle events, cargo state, and courier data with a versioned General Manifest owned by Jomon. It records accepted/refused contracts, package custody, crew status, courier loss, route observations, disputes, standing permissions, sanctions, repairs, and site/actor events.

Introduce a seeded, inspectable simulation clock that advances only during active gameplay through actions, rest, travel, and scheduled simulation ticks. Migrate existing galaxy/site/cache/courier state; define corrupt-save handling, reset behavior, and deterministic replay expectations before changing persisted schemas.

#### M2 Route Reckoning decision record

`docs/ROUTE-RECKONING.md` records the implementation contract for this phase:
Route Reckoning is an integer, persisted active-session clock; a browser
monotonic clock requests fixed steps but never becomes canonical state; hidden,
unfocused, closed, restoring, and blocking-terminal time is discarded; legacy
wall-clock reconciliation is migration-only; and meaningful site/contract
changes are represented by bounded, structured General Manifest entries. M1
sealed-package deadlines migrate to the same clock without changing their legal
custody lifecycle.

### Phase 2 — Sealed package vertical slice

Replace one automatic cargo contract with one player-operated sealed package flow: inspect exterior symptoms and declared contract clauses; accept or decline; assign a courier; establish custody; carry it through a route; deliver, refuse, abandon, lose, return, or breach the seal; then write the result to the General Manifest.

The slice must surface the sender/intermediary, recipient qualification, custody chain, deadline, declared mass/handling class, permitted inspection, prohibited actions, payment/collateral, and failure rules. Implement seal states (`intact`, `stressed`, `compromised`, `opened`, `resealed`, `destroyed`) as explicit durable state. The contents remain unknown until a permitted or forced outcome reveals information.

### Phase 3 — Route Board, ship operations, and negotiated access

Replace the simple adjacent-site selector with the Route Board. It exposes partial, dated intelligence: package leads, operational costs, environmental/political hazards, known institutions/rivals, repair/recruitment/trade opportunities, and time-sensitive events. It must represent uncertainty without falsely presenting exact outcomes.

Add carrier operations necessary for the package slice: contract desk/custody terminal, cargo vault, archives/General Manifest, crew selection, repair and provisioning state, and route-access negotiation. Model the Jomon as an independent political community with a Ship Compact, not a menu or a universal-postal proxy.

### Phase 4 — Persistent partitions and living destinations

Give sites, docks, airlocks, connector routes, and destination approaches stable world-space identities. Stream resident partitions without losing hero state, local actor state, package state, opened machinery, environmental mutations, caches, time, or camera direction. Persist compact mutation records on eviction and restore them deterministically on return.

Expand procedural destination construction to require the twelve `LORE.md` place-grammar fields. Seed-generated facts become immutable campaign canon; public notices, contract claims, and witnesses remain reported truth. Establish a first complete authored/seeded anchor using the Nerida Pressure Chain only after the generic grammar works.

### Phase 5 — Institutions, ecology, rivals, and causal history

Replace global prototype factions with locally generated institutions defined by constituency, dependency, governing method, legitimacy, public objective, internal division, rival, transport relationship, violence threshold, and memory of Jomon. Replace species-wide assumptions with culture/personhood modules for humans, Orra, Taal, Vey, and any future people.

Make ecological, labour, control, access, supply, migration, repair, and custody events advance through the simulation clock. Every durable event must alter a later decision, a location, a relationship, an offer, or a Manifest record. Add recurring rivals with individual goals, positions, resources, relationships, memories, and non-combat paths; permit rank, injury, equipment, allies, dependants, disgrace, disappearance, or death.

### Phase 6 — Tactical hazards, equipment, and emergent elites

Retheme legacy magic/fantasy terms into industrial tools, material failures, biological systems, jurisdictional equipment, Orphan artifacts, modifications, and injuries. Each power requires inspectable material fiction plus a dependency, liability, or social consequence.

Tie tactical escalation to visible world causes: shifts, weather, maintenance cycles, patrol reports, active organisms, checkpoint closures, recipient movement, and rival preparation. Preserve readable windups and threatened tiles. After the combat-model decision is resolved, implement the chosen action cadence without weakening deterministic RNG, traceability, or grid readability.

Replace fixed biome guardians with persistent elites, security platforms, industrial machines, engineered organisms, or rival-led climaxes that emerge from histories where possible. Multi-cell actor/component work follows the existing roadmap once combat cadence is settled.

### Phase 7 — Renderer parity and stabilized art production

Update ASCII/rune grammar and detailed-renderer metadata together so neither mode leaks hidden information or changes canon. Add package/seal, faction/relationship, machinery, non-human silhouette, telegraph, and uncertainty cues to both views.

Only once all required terrain, props, items, actors, effects, and animation states are specified should transparent pixel-art sheets be generated and activated through the existing manifest/fallback pipeline. Retain fallback rendering until complete coverage is verified.

### Phase 8 — Full player-path verification and content governance

Extend seeded headless tests and Playwright UI flows for every visible capability: package negotiation, exterior inspection, loading/unloading, seal breach, every resolution outcome, access refusal, deadline failure, courier death/replacement, Manifest history, faction/rival memory, simulation progression, persistent streaming, renderer parity, and save migration/reload.

Add procedural validation for place grammar, institution grammar, culture modules, package legality, protected-mystery boundaries, and deterministic seeds. Require every new authored content family to declare affected stable IDs, persistence scope, simulation cadence, player-visible evidence, and migration impact.

## Recommended first implementation milestone

Implement **one sealed-package custody vertical slice on the existing Kestrel route**, preceded by Phase 0’s taxonomy and migration contract. It should replace one generic automatic contract with one individually described package and a Jomon custody interface; it must support accept/decline, exterior inspection, active courier assignment, intact delivery, deadline failure, death/cache recovery, and a General Manifest entry.

This is the smallest milestone that proves the new premise—carrying a sealed object creates durable history—without prematurely committing to faction generation, world streaming, art, or a combat-cadence change. It also exposes the required save-schema and UI requirements early enough to design subsequent phases safely.

### M1 custody transition and migration contract

M1 introduces a separately versioned sealed-package model alongside the existing generic cargo records. It does not reinterpret old `cargo`, `contracts`, or route-cache cargo: those records retain their current meaning and migration behavior. The M1 model contains authored package definitions, generated package-contract and package instances, explicit seal and custody state, and a versioned General Manifest.

The only legal M1 flow is `offered → accepted → completed`, `failed`, or `expired`; `offered → declined` is terminal. Acceptance creates a package at Jomon and immediately records assignment to the active available courier. Exterior inspection is non-destructive and never reveals contents. Seal violation requires confirmation, changes `intact → opened`, records the consequence, and only then reveals the authored content record. A package can move from assigned custody to a route cache on courier death, then to Jomon custody on recovery; recovery does not reopen a failed contract. Delivery is an explicit terminal action at the contract destination, never an arrival side effect, and distinguishes intact from tampered settlement.

Existing version-1 galaxy saves receive empty M1 package collections, an empty version-1 General Manifest, and empty package arrays on pre-existing route caches. This is additive and idempotent. M1 IDs and Manifest sequence numbers are derived from the campaign seed and persistent sequence state; no M1 state transition relies on wall-clock time or random runtime IDs.

#### M1 implementation checkpoint

The first implementation is one authored Kestrel calibration case. Its terminal exposes the sender, intermediary, recipient, destination, deadline, declared mass, hold use, handling class, permitted exterior inspection, prohibited actions, payment/collateral, seal state, custody, and relevant Manifest entries. It supports decline, acceptance with the current available courier, non-destructive inspection before or after acceptance, confirmed opening, explicit intact or tampered settlement after a physical Kestrel landing, refusal, abandonment, deadline expiry through an explicit lifecycle transition, and courier-loss cache recovery.

M1 deliberately leaves generic unit cargo and its existing automatic prototype path intact for compatibility; package decisions do not use it. Phase 1’s active-session simulation clock must become the only caller that advances M1 deadlines; it must not adopt the existing wall-clock reconciliation. A future phase must replace that legacy cargo path after the clock and broader operations board are ready. The single M1 package definition is a content-boundary example, not a taxonomy for later package classes, settlement contracts, or institutions.

## Required acceptance criteria

- Player-facing carrier text uses Jomon and no final-destination framing.
- The package, its contract, seal state, and custody chain are inspectable before departure and persisted after every outcome.
- A courier’s death preserves package/location/manifest consequences and permits a replacement courier to continue in the changed world.
- Active-session time, route conditions, deadlines, and history use seeded, inspectable simulation state; closing the game does not advance them.
- ASCII/rune and detailed tile renderers show equivalent information for any implemented package/hazard/interaction state.
- Focused deterministic tests pass first, followed by `npm run test:autoplay:tasks`, `npm run test:e2e`, and `npm run build` for each completed playable slice.

## Explicit exclusions until later phases

- No generated sprite activation before renderer requirements and coverage are stable.
- No generic cargo expansion, universal faction taxonomy, species monocultures, literal magic, final route ending, or lore-only loot justification.
- No schema-breaking rename or save migration without an approved migration/reset contract and fixtures.
- No real-time combat conversion without resolving the documented conflict with the active project roadmap.
