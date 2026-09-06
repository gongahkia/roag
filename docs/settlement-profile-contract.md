# Named settlement profile contract

`src/medieval/settlement-profile.ts` v1 owns one closed, compiled reference profile: **Hearthford Mill Quay**. It supplies the source identity for the separate one-contract local public-tally handoff without creating a generated settlement, a world site, a market, a route, a person, a map cell, or a general browser settlement surface.

## Canonical profile

Hearthford is a shared river mill reach: the mill race and working barges use one landing. Millers, quay carriers, and weir tenders perform its visible civilian labour. Mill leaseholders schedule the mill, while the quay ward maintains the public landing order. Its future services are a covered landing, public tally table, and witness ledger. It records demand only for closed catalogue `ironwork` and `salt-fish` IDs; it establishes neither stock, price, quantity, buyer, seller, loading opportunity, nor contract.

The profile’s local pressure is the narrow shared mill-race schedule: delayed unloading can hold up ordinary milling. Its civilian purpose is visible milling and river supply for nearby households. These are bounded authored descriptions, not a current route condition, weather fact, hazard, task, demand simulation, or a claim that the player has reached the place.

## Ownership and validation

The profile is application-owned compiled data. `namedSettlementProfile()` returns a fresh clone, and `validateNamedSettlementProfile()` accepts only the exact v1 record: canonical ID/order, closed water/labour/authority/service/pressure/purpose vocabularies, known commodity references, field shapes, text bounds, and content-safety classification are all fail-closed. Reordered, missing, added, unsafe, unclassified, unknown, malformed, or substituted data is rejected with stable diagnostics.

`internal-content.ts` v3 records this as a `settlement-profile` developer-authored family with `not-a-world-fact` visibility. This is not a content pack, runtime registry, user input, loader, plugin point, or public compatibility promise.

## Compatibility and deliberate limits

The compiled profile itself changes no `FoundationWorld`, manifest, initial-world generator, or seed/RNG stream. The separate local handoff adds bounded mutable/replay/presentation facts while retaining the profile's non-generated, non-frontier identity and does not reveal hidden-world information.

[`settlement-trading-contract.md`](settlement-trading-contract.md) now establishes the only known local source: the existing quay-approach public tally offers one ironwork handoff and accepts its return consequence. Markets, prices, stock, broader loading sources, destinations, route comparison, travel, people, and further contracts remain unimplemented.
