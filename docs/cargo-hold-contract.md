# Cargo hold contract

`src/medieval/cargo-hold.ts` v1 owns Jomon's bounded physical cargo-lot state. It consumes the closed compiled commodity catalogue and owns no commodity definition, deck geometry, proximity rule, market, buyer, route, location, renderer, browser state, persistence transaction, timer, RNG, or content generation.

## Bounded state

`MedievalWorldState` v16 contains Jomon state v3's `cargo` projection. It has at most 24 canonical ID-sorted lots. A lot contains only a causal lot ID, a closed commodity ID, quantity (1–12), condition (`sound`, `spoiled`, or `damaged`), and status (`in-hold` or `lost`). It contains no prop copy, coordinate, source place, buyer, price, person, possession, route, manifest, hidden data, or arbitrary text.

Hold use is derived from the commodity catalogue's existing closed weight/bulk values: light/medium/heavy contribute 1/2/3 units and bulky contributes one additional unit. Only `in-hold` lots consume the existing Jomon cargo capacity; a lost lot remains bounded evidence for recovery but consumes no capacity. Loading, unloading, damage/spoilage/loss, and recovery never create a commodity definition, market fact, price, cargo source, location, or operation history.

## Physical transition boundary

The public reducers in `world.ts` accept only an active, fully validated world with its active courier exactly at the validated `prop:cargo-hold-rack` anchor. They append exactly one typed causal command:

- `vessel-cargo-loaded` for a known catalogue commodity and bounded quantity;
- `vessel-cargo-unloaded` for an in-hold lot;
- `vessel-cargo-failure-resolved` for a catalogue-compatible spoilage, damage, or loss; or
- `vessel-cargo-recovered` for a retained lost lot.

All four are zero-time transitions. They preserve the temporal minute/action sequence, immutable Jomon/manifest/seed/RNG evidence, people, navigation, and unrelated prop state. The cargo-hold prop's existing latest-action record is updated through that same command; no second feedback log or tavern command exists. The current contextual surface presents the validated hold readout. It does not mint cargo: a later physical trade/source owner must supply a legitimate loading opportunity.

Rejected source, active-courier, anchor, capacity, commodity, quantity, condition, status, duplicate, stale, unsafe, replay, or forged-world input throws before changing the supplied world.

## Replay and compatibility

Causal history v6, replay projection v8, checkpoint v6, and segments v6 reconstruct the cargo projection exactly through retained-tail compaction. Validation is closed over command payloads, tokens, content safety, causal ordering, cargo capacity, canonical lot order, and full replay equality.

`FoundationWorld` remains v15 and manifest remains v6. Strict read-only conversion accepts a valid `FoundationWorld` v15 / mutable-state v15 / Jomon v2 / causal v5 source only after validating its original no-cargo state and replay. It adds the empty cargo projection, rebases its checkpoint/journal to the v16/v6/v8 contracts, replays the full source, and validates the resulting current envelope. Invalid, ambiguous, stale, or forged input fails closed; reads never rewrite stored data. A later ordinary save may persist the upgraded full envelope. Navigation stays v1 and IndexedDB layout stays v4.

## Presentation boundary

Station readout v2 exposes only capacity used, capacity total, and bounded commodity/quantity/condition/status summaries for current hold lots; it intentionally omits causal lot IDs. Terminal presentation v13 and detailed-renderer adapter v7 forward that validated readout and the existing bounded latest-action status/message feedback with semantic palette, paired non-colour cue, concise accessible text, safety classification, and source/time/freshness evidence. The canvas and future detailed renderer consume that presentation bundle only and have no cargo, world, timer, input-execution, replay, or persistence authority.

