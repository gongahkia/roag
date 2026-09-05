# Vessel station readout contract

`src/medieval/vessel-station-readout.ts` v1 owns the bounded, renderer-neutral zero-time readout values for the seven non-ledger static Jomon props. It consumes a validated `FoundationWorld` and the validated v2 vessel-proximity result at an exact plan-derived anchor. It owns no deck geometry, prop binding, mutable prop state, world transition, causal command, persistence, replay, input, timer, browser state, or content generation.

## Exact station scope

Each record exposes only a physical binding identity/kind/area, a closed label/value shape, semantic palette token and paired non-colour cue, content-safety classification, concise text/accessibility text, and the authoritative fact source identifier. It has no coordinate, area footprint, cargo contents, sites, routes, weather, person/household data, relationship, possession, memory, location, world time, manifest, persistence, or hidden-world field. Terminal presentation adds current-time/freshness evidence only where it turns the record into the renderer-visible prompt.

| Static prop | Bounded readout | Explicit deferral |
| --- | --- | --- |
| `prop:chart-table` | Route comparison is not implemented | No routes, destinations, conditions, or travel facts |
| `prop:cargo-hold-rack` | Existing Jomon cargo capacity only | No cargo contents, empty-hold claim, loading, unloading, trade, or state |
| `prop:repair-space-rack` | Existing current/max Jomon integrity only | No repair action or mutation |
| `prop:stores-rack` | Provisions and inventory are not modeled | No site-market substitution or inventory system |
| `prop:berth` | Existing berth-slot capacity only | No rest or recovery action |
| `prop:galley-hearth` | Meals, rations, and cooking are not modeled | No cooking or supplies action |
| `prop:gangplank` | Existing `moored` operational status | No quay departure, arrival, destination, or travel action |

`prop:task-ledger` is deliberately absent: [`tavern-courier-switch-contract.md`](tavern-courier-switch-contract.md) remains its sole operation/readout owner. Its zero-time switch eligibility and canonical household availability/loss ledger are unchanged.

## Prompt and renderer boundary

Terminal presentation v11 creates one `vessel-station-readout` prompt only when the active courier occupies the exact source anchor. It carries one disabled Enter inspection option and Escape cancellation. Opening, inspecting, pressing Enter, and cancelling are all zero-time and leave the authoritative world unchanged. Away from every anchor, terminal presentation supplies only its existing bounded no-prop result.

The canvas renders this validated prompt and derives its ARIA summary from its accessibility text. Detailed-renderer adapter v5 forwards the same prompt bundle, source, semantic cue, safety, and evidence for parity. Neither renderer receives or owns a world, persistence, replay, timer, browser input execution, geometry, or prop policy.

## Validation and compatibility

Validation is closed over canonical prop identity, kind, area, label, value kind/shape, optional deferred reason, fact source, exact semantic palette/non-colour pairing, safe content classification, canonical text, and no unexpected fields. The full terminal projection regenerates and compares the readout from its validated world, so stale or forged current integrity/capacity/operational-status values fail closed.

`FoundationWorld` v15 adds only the deterministic static eight-prop provenance. Strict read-only conversion first fully validates a v14 source with its original three props, then adds the fixed new props and re-proves current replay/state validation. Manifest v6, mutable `MedievalWorldState` v14 / courier v3, navigation v1, replay projection v6, and IndexedDB layout v4 remain unchanged. No mutable prop record, causal command, migration, store, cache, worker, generator stream, or RNG output is added.
