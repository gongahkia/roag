# M6 delivery pressure, buildcraft, and tactical-risk implementation plan

## Scope and authoritative boundaries

M6 extends the existing sealed-package, Route Reckoning, destination, and institution systems. It does not replace package custody, M3 Route Board transit, M4 destination partitions, M5 institutions/rivals, or turn-based grid combat. The authoritative carrier name remains **Jomon**; all new player-facing terminology follows `LORE.md`.

One accepted sealed delivery is one M6 delivery run. Its pressure is derived exclusively from the canonical Route Reckoning already advanced by `advanceGalaxyRouteReckoning`. Browser wall time, rendering, opening a panel, and save hydration do not mutate it. The loaded floor keeps the actors, elite composition, hazards, and pending intents it was created with; pressure changes only influence later materialization.

## Durable v6 model

`GalaxyState` moves from v5 to v6. It receives an optional, bounded `deliveryRun` record, created only by accepting a sealed package and resolved permanently by its package outcome. The record is keyed by the stable sealed contract ID and contains:

- `id`, `contractId`, `courierId`, acceptance mark, deterministic run seed, elapsed marks, pressure tier, and resolved threshold IDs;
- item-acquisition provenance, persisted deterministic offers, resolved/declined offers, active-equipment cooldowns, and bounded expedition and elite history;
- final resolution and mark, so closed deliveries can never keep accumulating pressure or reopen;
- bounded exactly-once Manifest/institution feedback IDs.

M6 items remain ordinary `Hero.inventory` entries. `deliveryRun.equipment` records only the run-bound provenance and stack count needed to remove or archive those entries at resolution; it is not a second inventory. New M6 item IDs cannot collide with existing starter or legacy loot IDs. Existing equipment slots continue to work unchanged. Courier modifications, injuries, and learned techniques live on `Hero`, which already follows the named courier through the galaxy and is copied into a saved active run.

The model is bounded: four pressure tiers, at most 12 run-bound item copies, six persisted offers with at most one awaiting a choice, one active equipment cooldown per item, six pending tactical intents, three active M6 elites, 16 retained resolved intents, eight encounter-history entries, three injuries, two modifications, and three learned techniques. Migration gives v1–v5 saves no active delivery run and normalized empty courier M6 fields; it does not replay marks, create offers, repair deaths, or reopen contracts.

## Pressure model

Pressure is `max(0, routeReckoning - acceptedAtRouteReckoning)` while the associated contract remains accepted. It is capped at 1,920 marks. Tiers are evaluated in stable order. The first Nerida approach costs 660 canonical marks, so the values intentionally leave its optional intake expedition in a readable compression state rather than forcing the cap before its first hazard:

| Tier | Marks | New tactical effect |
| --- | ---: | --- |
| **working load** | 0–359 | baseline M6 expedition composition |
| **compression** | 360–719 | one compatible elite trait and improved common-offer weighting |
| **cavitation** | 720–1,199 | an additional compatible elite trait or a second hazard schedule |
| **cascade** | 1,200–1,920 | capped high-risk hazard combination, rare-offer weighting, and custody-risk pressure |

Crossing 360, 720, or 1,200 resolves once, appends a single meaningful Manifest entry, and never changes a materialized `RunState`. `advanceGalaxyRouteReckoning` is the only evaluator. The existing active-session clock already advances in unobstructed hub, sector, and playing-level states and pauses during hidden, unfocused, transit-presentation, story, modal, and hub-action states. Route Board transit already advances by its stored duration through the same function.

## Authored buildcraft content and modifier order

The delivery registry is data-driven and extends content definitions with stable ID, name, description, source pool, tags, glyph, rarity weight, stack cap, persistence category, passive/active behavior, stack rule, and exclusions. Registry iteration is sorted by item ID before evaluation. New content is industrial and setting-native:

| Item | Category / cap | Effect and player-visible tradeoff |
| --- | --- | --- |
| **Pressure-Weave Liner** | stackable passive / 3 | reduces major hazard damage; later stacks use diminishing protection |
| **Route-Current Capacitor** | stackable passive / 4 | safely crossed hazard tiles charge damage reduction; charge decays after use |
| **Custody Seal Mesh** | stackable passive / 2 | intact package custody improves offer quality and reduces custody-risk escalation |
| **Vector Skates** | mobility / 1 | grants one reposition response after a major intent, but makes repeated movement easier for a tracking elite to read |
| **Tissue-Stitch Patch** | defensive / 2 | converts the first persistent-injury event into a consumed patch at reduced health cost |
| **Archive Tether** | custody / 1 | records a safe package handoff/interruption and improves intact-delivery recovery |
| **Grounding Spindle** | hazard interaction / 1 | grounds a relay-discharge intent and converts it into capacitor charge |
| **Pulse Reverser** | active / 1 | cancels one adjacent/intended machinery hazard on a six-turn cooldown |
| **Orphan Phase Sample** | strange artifact / 1 | amplifies charged capacitor effects, but can inflict deterministic instability damage when overcharged |

Two explicit synergies are tested: Grounding Spindle plus Route-Current Capacitor converts a relay response into a stronger charge, and Vector Skates plus Pressure-Weave Liner reduces the cost of escaping a declared major pattern. Archive Tether plus Custody Seal Mesh is a legible custody synergy.

Derived delivery stats are centralized in `delivery-buildcraft.ts` with inspectable provenance. Evaluation order is: courier base state; learned techniques; injuries; permanent modifications; sorted run-item base effects; sorted stack additions; conditional effects; explicit floors/caps; encounter-local temporary effects. Effects are queried by combat/hazard code rather than scattered item-ID checks. New player-facing stack descriptions state cap and diminishing returns.

## Offers, modifications, and lifecycle

At accepted-delivery creation, a deterministic three-choice requisition offer is generated from the run seed and persisted before UI display. After resolving the current offer, later Route Board arrivals can create a bounded additional transit-salvage offer. This makes legal item stacks obtainable through play without allowing an unbounded reroll loop. Offers exclude illegal combinations and exhausted caps. The player explicitly chooses `1`–`3` or declines; opening/closing, save/reload, and insertion order cannot reroll it. The selected item is appended once to `Hero.inventory` and its delivery provenance record; unselected choices remain absent.

The carrier hub exposes a **Delivery Load** panel on `P`. It shows pressure, elapsed marks, next threshold, item stacks, item details, active cooldown, and the persisted offer. The same panel offers a modification installation only when the active run has reached the Nerida opportunity. Installation requires explicit confirmation and consumes one existing `Vital Gel` (`tonic` item) from the owning courier rather than inventing currency.

Two modifications are authored and courier-owned:

- **Subdermal pressure baffles**: reduce decompression/contaminant harm, but each health recovery restores one less health.
- **Relay-marrow conduit**: lets electrical hazard grounding charge active equipment, but a fully charged conductor takes deterministic instability damage at turn end.

They are mutually exclusive, persist only on the installing courier, and append an exactly-once Manifest installation event. They do not transfer to a replacement courier. Successful, failed, abandoned, refused, expired, or death-closed runs archive their run-bound equipment by removing its recorded copies from that courier inventory. Existing non-M6 inventory and equipment remain untouched. Route-cache recovery after closure creates at most one inert-salvage Manifest record; it neither restores active bonuses nor reopens the run. Courier loss first moves the package through the existing cache path, then closes and archives the delivery loadout so it cannot exist in both locations.

## Injury and technique slice

`Hero` holds bounded M6 injury and technique IDs. A major unresolved contamination or machinery intent deterministically applies **pressure scarring** if no Tissue-Stitch Patch intercepts it; it reduces later healing and is announced before the threatened intent resolves. Clearing the Nerida intake objective alive grants the owning courier **intake routing**, a learned technique that improves the first hazard response each expedition. Neither personal field transfers through courier death; the M5 vessel/institution/rival history does.

## Domain intents, hazards, and elites

The existing `Telegraph` type is extended rather than replaced. It becomes renderer-independent tactical intent data with declaration turn, resolution turn, source kind, category, severity, response rules, stable ID, and pending/resolved/cancelled lifecycle. Existing enemy telegraphs receive compatible defaults. Pending intents save with the floor. Resolution is ordered by `(resolution turn, ID)`, records the ID once, and removes it. Source-actor death or an explicit hazard shutdown cancels matching intents once. Both ASCII and detailed rendering continue to consume `Floor.telegraphs`; neither relies on animation completion.

The authored M6 hazard schedules are deterministic from run/encounter seed and are placed only after a viable adjacent/escape response check:

- **pressure vent sweep**: a line pattern; move out or use pressure protection.
- **relay discharge**: a pulsing area; leave the field, ground it with the spindle, or exploit it for capacitor charge.
- **intake shear**: cycling machinery cells; reposition, interrupt with Pulse Reverser, or accept a clear scarring risk.

Nerida pump condition and known Port restriction influence whether the relay schedule is activated and whether its warning is exact or partial. Each effect is declared at least one player turn before major resolution.

Elites are composed from a normal hostile actor, a stable encounter ID, delivery tier, Nerida partition condition, and known institutional state. Compatible, sorted traits are **relay-bound** (protected until a discharge is grounded), **sweep marshal** (projects a pressure line), **intake tracker** (reacts to repeated movement), and **custody clamp** (raises known inspection/custody risk after escape). They change positional choices rather than simply multiplying health. A materialized actor stores stable elite identity and traits, so reload cannot reroll it. Elite defeat or escape records bounded history and deterministic reward identity.

## Nerida vertical slice and causality

Landing at Nerida with an active sealed delivery can create one deterministic **intake-service expedition** without replacing M4’s pump watch, bypass, or stabilization. Its objective is to stabilize a pump intake diagnostic and return through the existing airlock. It contains at least a pressure vent plus relay discharge, an eligible persistent transit-salvage offer, the modification opportunity, and one generated elite. It is optional for Kestrel delivery settlement.

Known Port Office activity can activate the relay schedule or downgrade warning accuracy. Grounding the intake and surviving the elite writes one bounded destination consequence/report, adjusts the appropriate M5 standing through an explicit causal event, records a relevant Iren memory without combat, and appends a General Manifest event. Hidden actions remain hidden until existing observation/report paths reveal them.

## Player surface and renderer parity

Hub panel `P` and level sidebar/readout expose pressure tier, elapsed marks, next threshold, stacks, active cooldown, modification benefit/cost, injury warning, and offer details. The level sidebar gives each pending intent its source, category, severity, response, and turns remaining; elite name/traits/counters appear in the same textual stream. Detailed mode adds the existing telegraph outlines/reticles; ASCII uses the same cells plus labelled status. No gameplay state is available only through colour.

## Implementation order and verification

1. Add v6 types, migration/normalization, Manifest reference extensions, and delivery-run pressure lifecycle; preserve all current v5 behavior.
2. Add the data-driven registry, centralized modifiers, deterministic offers, lifecycle actions, and hub panel.
3. Extend domain intents and implement the three hazards, response/cancellation paths, injuries, and active equipment.
4. Add deterministic elite composition plus the optional Nerida intake expedition and M4/M5 feedback.
5. Add focused unit/migration/renderer tests, tactical soak, autoplay task, browser flow, then regressions and bundle validation.

Focused tests cover the 38 M6 requirements in the handoff: Route Reckoning-only pressure and chunking; offer persistence/selection; order-independent stacking/synergies/lifecycle; courier-specific modification/injury/technique persistence; intent/hazard avoidance/save cancellation; deterministic elite composition/rewards; M4/M5 feedback/rival memory; renderer parity; M1–M5 regressions; v5-to-v6 migration; and absence of new legacy aliases. The soak compares whole/chunked marks and insertion order across seeded survival/death expeditions, reporting deterministic fingerprints and every bounded collection maximum.
