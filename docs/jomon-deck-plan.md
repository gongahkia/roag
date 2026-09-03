# Jomon deck-plan contract

`src/medieval/jomon-deck-plan.ts` is the v1, compiled, renderer-independent static layout contract for a future walkable Jomon deck. It derives a small fixed plan from a validated `FoundationWorld` and its existing `FoundationJomon`; it does not create a second vessel, world, map, or mutable authority.

## Layout and ownership

`FoundationJomon` remains authoritative for the fixed eight partitions and existing props. The plan binds those owners to a canonical 18 by 8 local coordinate grid:

- `quay-approach` is a local shore-side approach only. It has no `FoundationJomon` partition, site, route, frontier fact, save record, or travel destination.
- `gangplank`, `tavern`, `chart-table`, `cargo-hold`, `repair-space`, `stores`, `berths`, and `galley` each bind to their existing partition.
- `prop:gangplank`, `prop:task-ledger`, and `prop:chart-table` bind to their existing kind, partition/area, and a bounded anchor. The contract invents no other props and no prop operation.

Areas, hull-boundary cells, and access edges are canonical ordinal-ID and row/column order. The quay joins the gangplank, and all vessel areas are reachable through explicitly verified orthogonal adjacency. Those edges are future movement input only: v1 implements neither movement nor collision, camera, interaction, task assignment, travel, cargo, repair, or crew switching.

## Semantic references, not materialized cells

The plan uses only existing entries from the closed `JOMON_ASCII_GLYPH_CATALOG` that are marked for `future-jomon-deck`: quay approach, gangplank, open-deck, and hull-planking semantics. It exposes the glyph reference, semantic palette token, paired non-colour cue, existing text equivalent/accessibility text, and content-safety classification. Raw colours and glyph ownership remain in the palette and glyph contracts.

This is not input to `createTerminalPresentationModel()` yet. The terminal map remains `reserved-unmaterialized` with zero cells, no deck/route/site/actor facts, and no browser rendering change. The detailed-renderer adapter remains a deferred parity seam. The next rendering task owns materializing only source-backed cells from a common map state.

## Validation and known-facts boundary

Derivation first validates the entire existing `FoundationWorld`, then verifies the Jomon partitions, prop identities, kinds, and partition bindings. The derived plan itself is fail-closed: it rejects malformed or stale source data; missing, unknown, duplicate, or noncanonical identifiers; unsafe classifications; unrecognised glyph references; invalid/out-of-bounds/overlapping coordinates; unexpected fields that could carry hidden world data; broken anchors; and disconnected or diagonal-only topology.

The plan contains no world time, manifest, initial-world, causal-history, known-fact, save, persistence, browser, randomness, worker, cache, or storage authority. It is deterministic, discardable derived data. No timing, locale, or random input participates in its output.

## Compatibility and deferred work

Deck-plan v1 changes no `FoundationWorld` or `WorldJomonState` field, manifest, replay, causal history, save envelope, IndexedDB v4 layout, valid-v3 loading behaviour, storage transaction, content catalogue, or migration path. It adds no gameplay content or public extension/mod surface.

Future movement, common map-state construction, primary ASCII rendering, detailed-renderer parity, prop interaction, and any world/persistence integration remain with their respective roadmap owners. They must retain the current semantic, accessibility, content-safety, provenance, and no-hidden-facts boundaries.
