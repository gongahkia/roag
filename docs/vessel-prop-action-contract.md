# Vessel prop action-state and feedback contract

`src/medieval/vessel-prop-action.ts` v1 owns the bounded mutable projection of the latest authoritative outcome at each immutable Jomon prop. It owns neither immutable prop provenance, deck geometry, proximity, station values, people, cargo, routes, locations, input, persistence transaction, replay reduction, timer, browser state, or player-visible free text.

## State and command boundary

Jomon v3 in `MedievalWorldState` v17 contains exactly eight records in immutable `FoundationJomon` prop order: berth, cargo-hold rack, chart table, galley hearth, gangplank, repair-space rack, stores rack, and task ledger. A record contains only its prop ID and, after an accepted action, the latest closed action kind, authoritative world minute, and causal sequence. It has no action history, coordinate, source value, roster/person data, cargo-lot copy, route, location, browser log, or arbitrary text.

The non-ledger props accept `station-readout-recorded` through the typed zero-time `vessel-station-readout-recorded` command after the full world, source binding, exact anchor, and current readout validate. The cargo-hold rack additionally accepts only its four typed cargo action kinds from [`cargo-hold-contract.md`](cargo-hold-contract.md): loaded, unloaded, failure resolved, and recovered. The local public-tally delivery command may reuse the existing loaded latest-action result only after it has validated the accepted burden; it does not create a second prop command. The task ledger accepts only `tavern-courier-switched`, through its existing typed courier-switch command; no duplicate tavern command exists. Every action replaces only its own latest record, retains the world minute and temporal action sequence, and is rejected without changing the supplied world when evidence, sequence, source, replay, or state is invalid. Opening a prompt, moving its selection, cancellation, unavailable operation, and no candidate remain non-mutating.

## Replay, upgrade, and persistence

Causal history v7 and replay projection v9 reconstruct the eight records from the checkpoint plus command tail and verify exact equivalence after compaction. Structural validation rejects missing, duplicate, reordered, unknown, impossible-kind, unsafe, future-time, stale-sequence, unexpected-field, and replay-inconsistent records. `FoundationWorld` v15, manifest v6, immutable generator/RNG output, navigation v1, and IndexedDB layout v4 do not change.

Strict read-only upgrade validates and replays a valid v15/state-v14 source under its original causal/replay contract before deriving the current state. Retained legacy tavern-switch tail evidence can populate the ledger record; evidence compacted before this state existed is not invented. Corrupt, ambiguous, stale, or forged legacy sources fail closed. Repository reads never rewrite storage; a later normal save writes the validated upgraded full envelope through the existing transaction.

## Renderer feedback boundary

Terminal presentation v14 projects the newest latest outcome as one status item and all touched prop outcomes as canonical prop-order messages. Each is regenerated from typed state, uses existing semantic palette/non-colour pairings, concise safe text, accessibility text, and authoritative source/current-time/freshness evidence. The canvas and detailed-renderer adapter v8 consume only this validated presentation bundle. They cannot create an action, change time, mutate a world, persist data, access a timer, or gain hidden data.

The recorded acknowledgements do not implement repair, inventory, meals, rest, route comparison, travel, departure, weather, people, hazards, or any durable message system beyond the bounded state-derived feedback above. Cargo transitions remain bounded vessel-hold state only; beyond the one explicitly source-bound public-tally receipt they do not implement acquisition, markets, buyers, prices, trade, routes, or travel.
