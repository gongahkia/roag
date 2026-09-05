# Named settlement profile contract

`src/medieval/settlement-profile.ts` v1 owns one closed, compiled reference profile: **Hearthford Mill Quay**. It establishes the first material settlement identity for later physical trade work without creating a generated settlement, a world site, a market, a route, a person, a contract, a map cell, or a browser surface.

## Canonical profile

Hearthford is a shared river mill reach: the mill race and working barges use one landing. Millers, quay carriers, and weir tenders perform its visible civilian labour. Mill leaseholders schedule the mill, while the quay ward maintains the public landing order. Its future services are a covered landing, public tally table, and witness ledger. It records demand only for closed catalogue `ironwork` and `salt-fish` IDs; it establishes neither stock, price, quantity, buyer, seller, loading opportunity, nor contract.

The profile’s local pressure is the narrow shared mill-race schedule: delayed unloading can hold up ordinary milling. Its civilian purpose is visible milling and river supply for nearby households. These are bounded authored descriptions, not a current route condition, weather fact, hazard, task, demand simulation, or a claim that the player has reached the place.

## Ownership and validation

The profile is application-owned compiled data. `namedSettlementProfile()` returns a fresh clone, and `validateNamedSettlementProfile()` accepts only the exact v1 record: canonical ID/order, closed water/labour/authority/service/pressure/purpose vocabularies, known commodity references, field shapes, text bounds, and content-safety classification are all fail-closed. Reordered, missing, added, unsafe, unclassified, unknown, malformed, or substituted data is rejected with stable diagnostics.

`internal-content.ts` v3 records this as a `settlement-profile` developer-authored family with `not-a-world-fact` visibility. This is not a content pack, runtime registry, user input, loader, plugin point, or public compatibility promise.

## Compatibility and deliberate limits

The profile changes no `FoundationWorld`, manifest, initial-world generator, seed/RNG stream, mutable state, causal/replay contract, terminal/detailed presentation, persistence envelope, IndexedDB layout, or browser UI. It is not automatically attached to a generated initial-world settlement and does not reveal hidden-world information.

The following physical trading-location task must establish where and how Hearthford becomes known, situated, and actionable. It must supply its own persisted/replay/provenance and player-knowledge decisions. Markets, prices, stock, loading source, unloading destination, cargo transfer UI, route comparison, travel, people, and contracts remain unimplemented here.
