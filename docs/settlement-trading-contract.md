# Local settlement trading contract

`src/medieval/settlement-trading.ts` v1 owns one closed, local physical freight handoff. It is not a market, a settlement simulation, a route system, a price/stock/demand model, or a second source of world authority.

## Source, contract, and bounded state

The only source is the existing `quay-approach` area's exact plan-derived anchor, presented as **Hearthford Mill Quay public tally**. The trade owner exposes no coordinate, deck geometry, site map, person, frontier, route, timer, persistence, or browser data. It accepts a validated deck plan and authoritative navigation coordinate only to answer exact source occupancy.

`settlement-location:hearthford-mill-quay` contains exactly one canonical location record. `settlement-contract:hearthford-mill-ironwork` contains exactly one canonical contract record in one of four closed states:

- `offered`: the public tally offers one ironwork case;
- `accepted`: one fixed `commodity:ironwork` quantity-one burden awaits delivery at `prop:cargo-hold-rack`;
- `refused`: the handoff was refused and the case remains at the quay;
- `delivered`: Jomon received the exact causal-sequence-owned ironwork cargo lot.

Every resolved state retains only its authoritative recorded world minute and causal sequence. There is no unbounded operation history, free text, copied prop/area data, source coordinate, cargo contents beyond the one closed commodity and quantity, price, stock, demand, buyer, destination, route, weather, person, location, or hidden-world record.

## Transition and replay ownership

At the exact public tally, an active courier may accept or refuse the sole offered contract. Acceptance and refusal append one typed, safety-classified zero-time causal command. A refusal is final. Acceptance creates only the bounded material burden; it does not mint cargo.

At the exact existing cargo-hold rack, the accepted burden may be delivered. The `settlement-trade-delivered` command is the sole receipt transition. Its shared pure reducer changes the contract to `delivered`, loads exactly one sound in-hold canonical ironwork lot, and replaces the existing cargo-hold latest-action record through the normal cargo projection. It creates no second cargo command or separate prop authority.

All three transitions fully validate the envelope, current active/navigation correspondence, source binding, exact anchor, current contract, causal evidence, replay projection, safety audit, and the relevant cargo state before producing a new envelope. Unknown, stale, repeated, malformed, reordered, forged, unsafe, off-source, replay-inconsistent, or direct-mutated input fails closed and leaves the supplied source unchanged. They preserve world minute, temporal action sequence, immutable world/manifest/household/initial-world evidence, RNG output, and unrelated people. Ordinary deck movement remains the existing one-minute action.

On return to the exact tally, terminal presentation derives the bounded durable consequence from the validated contract/replay state: the accepted case awaits hold delivery, the refusal left it at the quay, or Jomon received it and the mill-race work can proceed. It does not infer a consequence by comparing IDs, invent a cause, or reveal causal IDs, destination, route, price, cargo details, people, or frontier data.

## Presentation and renderer boundary

Terminal presentation v14 supplies the public-tally and accepted cargo-hold prompts only from validated current state plus the corresponding source contracts. The offered prompt has canonical `accept` then `refuse` keyboard choices; resolved tally and delivery prompts have one bounded Enter acknowledgement/action. Every prompt and choice carries closed semantic/non-colour pairing, content safety, concise accessibility text, and source/time/freshness evidence. Escape cancels without mutation or time. Away from these sources the existing no-prop contextual result names no tally or cargo action.

The canvas only renders the validated prompt and sends its existing typed intent to the app. The detailed-renderer adapter v8 forwards the same presentation bundle for parity. Neither renderer owns coordinates, route or trade policy, raw world state, replay, storage, timer, input execution, or hidden facts.

## Compatibility and upgrades

`FoundationWorld` remains v15, manifest v6, immutable generator/RNG output, household evidence, `WorldDeckNavigationState` v1, and IndexedDB layout v4. `MedievalWorldState` advances from v16 to v17; causal history advances from v6 to v7 and replay projection from v8 to v9. The bounded state is part of the existing full envelope; no store, index, migration, cache, worker, or read-time write is added.

The strict state-v16 bridge first validates and fully replays its original no-contract source, derives exactly the canonical offered state, updates only the required current content audit/checkpoint projection, then re-validates the v17 result. Corrupt, ambiguous, stale, forged, or incompatible records remain unavailable and are not overwritten on read. A later ordinary save may persist the validated current envelope.

This is intentionally the complete local handoff slice. Price, stock, demand, broader market relationships, loading source selection, cargo transfer UI, route comparison, travel, settlement partitions, and additional contracts remain deferred.
