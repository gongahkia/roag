# Vessel proximity and operation contract

`src/medieval/vessel-proximity-operation.ts` owns v2 of the bounded, renderer-independent proximity assessment for every current static Jomon prop. It derives only from a validated `FoundationWorld`, `jomon-deck-plan.ts` v1's canonical prop bindings, and a validated active-courier/navigation projection. It has no geometry, world mutation, replay, persistence, input, browser, timer, RNG, cache, or content-generation authority.

## Exact occupancy and closed scope

Proximity is exact occupancy of a plan-derived anchor. It is not adjacency, a radius, line of sight, shared-area interaction, pathfinding, mouse targeting, or a new movement rule. The plan remains the sole owner of the fixed 18 by 8 geometry and all anchor coordinates.

The assessment returns the existing bindings in canonical binding order and exposes only binding identity, prop kind, area identity, `at-anchor` or `away-from-anchor`, a closed availability classification, a bounded reason when unavailable, and safe template classification. It never exposes coordinates, area footprints/cells, current people, roster data, world time, manifest, save, causal history, route facts, or browser metadata.

| Canonical binding | At exact anchor | Away from anchor |
| --- | --- | --- |
| `prop:berth` / berth / berths | `readout`: berth-slot capacity only | `unavailable`: not at prop anchor |
| `prop:cargo-hold-rack` / rack / cargo-hold | `readout`: cargo capacity only | `unavailable`: not at prop anchor |
| `prop:chart-table` / table / chart-table | `readout`: route comparison is not implemented | `unavailable`: not at prop anchor |
| `prop:galley-hearth` / hearth / galley | `readout`: meals, rations, and cooking are not modeled | `unavailable`: not at prop anchor |
| `prop:gangplank` / gangplank / gangplank | `readout`: Jomon is moored; quay travel is not implemented | `unavailable`: not at prop anchor |
| `prop:repair-space-rack` / rack / repair-space | `readout`: current/max integrity only | `unavailable`: not at prop anchor |
| `prop:stores-rack` / rack / stores | `readout`: onboard provisions and inventory are not modeled | `unavailable`: not at prop anchor |
| `prop:task-ledger` / ledger / tavern | `implemented`: existing tavern courier-switch source | `unavailable`: not at prop anchor |

Terminal presentation v11 consumes this assessment to create a compact physical station-readout prompt at every non-ledger `at-anchor` result. Each has its source identity, one disabled Enter option, closed semantic/non-colour cue, source/time/freshness evidence, safe bounded accessibility text, and zero-time Escape cancellation. Readout values are defined by [`vessel-station-readout-contract.md`](vessel-station-readout-contract.md), not by the canvas or this proximity owner. Away from every anchor, the contextual result remains bounded and names no prop.

## Validation and tavern integration

The contract fails closed for an invalid foundation source; missing, unknown, duplicate, reordered, mismatched-kind, mismatched-area, malformed, or stale deck-plan binding; invalid/missing active courier; navigation/courier mismatch; or a non-walkable coordinate. It validates its public assessment strictly: canonical eight-entry order, exact binding identities, closed state/reason combinations, no unexpected fields, and the required content-safety classification.

`tavern-courier-switch.ts` now obtains the ledger binding and exact-anchor decision through this common contract. Tavern switching still solely owns household/person eligibility, candidates, the zero-time causal transition, replay, and persistence semantics. Its established replay source keeps the existing `4,4` coordinate, but that coordinate is not a new public proximity-assessment field and is no longer independently derived from deck-plan geometry by the tavern owner.

## Compatibility and deferrals

This is a pure discardable assessment. `FoundationWorld` v15 makes the eight static props immutable deterministic provenance. Valid v14 three-prop sources are fully proved as v14 before a strict read-only conversion adds exactly the five deterministic static props and current validation/replay proof accepts the v15 result. Mutable state remains v14 / courier v3; navigation remains v1; replay remains v6; terminal presentation is v11 and the detailed-renderer adapter v5 solely for the expanded discardable prompt shape. Manifest shape/version, generator/RNG output, and IndexedDB layout v4 remain unchanged. The existing contextual control/remapping and Escape/Enter protections are reused. No mutable prop state, causal command, storage field, browser authority, route comparison, travel, cargo contents, repair, stores, rest, or cooking action is introduced.
