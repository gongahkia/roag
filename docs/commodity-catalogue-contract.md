# Closed commodity catalogue

`src/medieval/commodity-catalogue.ts` v1 owns Jomon’s small, compiled trade-goods definition set. It is application-owned TypeScript data, not a content-pack format, generator input, market, cargo container, settlement record, or public extension surface.

## Canonical material definitions

The catalogue has exactly eight ID-sorted entries. Each has one name, source category, material use, weight and bulk class, required condition, handling requirement, failure mode, buyer category, and content-safety classification.

| Commodity | Source | Use | Weight / bulk | Required condition and handling | Failure mode | Buyer |
| --- | --- | --- | --- | --- | --- | --- |
| Charcoal | Woodland kiln | Forge fuel | Light / bulky | Dry; sealed sacks | Crushing | Forge master |
| Grain | River mill | Household bread | Heavy / bulky | Dry; covered sacks | Damp spoilage | Town baker |
| Ironwork | Yard forge | Tools and fittings | Heavy / compact | Sound; secured crates | Rust | Shipwright |
| Lime | Lime kiln | Building mortar | Heavy / compact | Dry; sealed casks | Slaking | Mason |
| Paper | Paper mill | Records and letters | Light / compact | Dry; wrapped bundles | Water damage | Scribe house |
| Salt fish | Coastal curing yard | Preserved provisions | Medium / compact | Salt-cured; dry packing | Damp spoilage | Inland provisioner |
| Timber | Riverside yard | Vessel and building work | Heavy / bulky | Sound; lashed bundles | Warping | Shipwright |
| Wool | Sheepfold | Cloth making | Medium / bulky | Dry; covered bales | Moth damage | Clothier |

These source and buyer categories are not instantiated locations, institutions, people, market quotes, or contracts. Weight/bulk and handling/failure labels are closed catalogue semantics. The separate cargo-hold owner may derive bounded capacity, current lot condition, and compatible failure/recovery transitions from them, but does not create a source, buyer, price, market, or contract.

## Validation and safety

The owner returns a fresh clone of the single canonical catalogue. `validateCommodityCatalogue()` is strict: it requires the exact version, eight canonical IDs and order, exact fields and values, no unknown fields, and valid safety classification for every entry. Reordered, duplicate, missing, unknown, extended, malformed, unsafe, or unclassified values fail closed with stable diagnostics. Validation has no mutation, loading, generation, persistence, browser, map, timer, or replay effect.

Each definition is audited as `data` that may later support player-facing text. Its classification affirms that sexual violence, slavery, torture, and harm or endangerment of children are excluded. The catalogue has no generated or user-authored text path.

## Compatibility and deferred economy

The catalogue's own v1 addition changed only internal compiled content. The later cargo-hold contract now advances mutable state to v16/Jomon v3 and causal replay to v8 while retaining `FoundationWorld` v15, manifest v6, navigation v1, and IndexedDB layout v4. That state may reference only a closed commodity ID plus bounded lot quantity/condition/status; it does not copy catalogue definitions or create market, buyer, price, stock, demand, route, location, or trade facts. `WorldMarketState.commodityStates` remains the deliberately empty closed placeholder.

The compiled Hearthford Mill Quay profile v1 references only `ironwork` and `salt-fish` through these closed IDs; it does not instantiate a market or alter this catalogue. Later market and settlement owners must reference this closed catalogue rather than inventing a parallel commodity vocabulary, and must make their own version/replay/provenance decisions before persisting their facts.
