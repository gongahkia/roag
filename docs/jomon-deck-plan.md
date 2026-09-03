# Jomon deck-plan contract

`src/medieval/jomon-deck-plan.ts` is the v1, compiled, renderer-independent static layout contract for the visible-but-not-yet-walkable Jomon deck. It derives a small fixed plan from a validated `FoundationWorld` and its existing `FoundationJomon`; it does not create a second vessel, world, map, or mutable authority.

## Layout and ownership

`FoundationJomon` remains authoritative for the fixed eight partitions and existing props. The plan binds those owners to a canonical 18 by 8 local coordinate grid:

- `quay-approach` is a local shore-side approach only. It has no `FoundationJomon` partition, site, route, frontier fact, save record, or travel destination.
- `gangplank`, `tavern`, `chart-table`, `cargo-hold`, `repair-space`, `stores`, `berths`, and `galley` each bind to their existing partition.
- `prop:gangplank`, `prop:task-ledger`, and `prop:chart-table` bind to their existing kind, partition/area, and a bounded anchor. The contract invents no other props and no prop operation.

Areas, hull-boundary cells, and access edges are canonical ordinal-ID and row/column order. The quay joins the gangplank, and all vessel areas are reachable through explicitly verified orthogonal adjacency. Those edges are future movement input only: v1 implements neither movement nor collision, camera, interaction, task assignment, travel, cargo, repair, or crew switching.

## Primary ASCII projection and glyph vocabulary

`createTerminalPresentationModel()` v4 derives one common, renderer-independent terminal map from this validated plan. Its 18 by 8 materialized viewport contains exactly the plan's area and hull cells in canonical row/column order. The browser canvas consumes those cells; it owns no deck geometry, glyph character, world fact, or map-state authority. The Phase 1 detailed-renderer adapter receives the exact same terminal projection and remains a deferred parity adapter, not a detailed renderer.

The closed `JOMON_ASCII_GLYPH_CATALOG` remains authoritative. Only these existing `future-jomon-deck` entries appear in the current plan:

| Character | Semantic meaning | Palette role | Non-colour cue | Text/accessibility equivalent | Ownership |
| --- | --- | --- | --- | --- | --- |
| `)` | Quay approach | `route` | `READY` | Known quay approach route; local source-backed access area | Glyph catalogue and deck plan |
| `/` | Gangplank | `route` | `READY` | Vessel gangplank; source-backed vessel access area | Glyph catalogue and existing Jomon prop/partition |
| `=` | Open deck space | `bodyText` | `KEY` | Named static deck space | Glyph catalogue and existing Jomon partition |
| `#` | Hull planking boundary | `mutedText` | `KEY` | Hull boundary, structural static deck cell | Glyph catalogue and deck plan |

These characters are semantic references with paired textual accessibility descriptions and affirmative content-safety classifications; colour never carries their meaning alone. Raw colours remain owned by the v2 semantic palette, while glyph characters and authored text remain owned by the closed glyph catalogue. The map explicitly contains no courier position, cargo, person, terrain, route-travel, or interaction state.

## Validation and known-facts boundary

Derivation first validates the entire existing `FoundationWorld`, then verifies the Jomon partitions, prop identities, kinds, and partition bindings. The derived plan itself is fail-closed: it rejects malformed or stale source data; missing, unknown, duplicate, or noncanonical identifiers; unsafe classifications; unrecognised glyph references; invalid/out-of-bounds/overlapping coordinates; unexpected fields that could carry hidden world data; broken anchors; and disconnected or diagonal-only topology.

The plan contains no world time, manifest, initial-world, causal-history, known-fact, save, persistence, browser, randomness, worker, cache, or storage authority. It is deterministic, discardable derived data. No timing, locale, or random input participates in its output.

## Compatibility and deferred work

Terminal-presentation v4 is a derived rendering-contract change only; it changes no `FoundationWorld` or `WorldJomonState` field, manifest, replay, causal history, save envelope, IndexedDB v4 layout, valid-v3 loading behaviour, storage transaction, content catalogue, or migration path. It adds no gameplay content or public extension/mod surface.

Future movement, collision/camera/visibility, prop interaction, map legend/help, and any world/persistence integration remain with their respective roadmap owners. The current visible map is not navigable or interactive, and it must not be treated as a world site, travel route, or mutable spatial state. Those owners must retain the current semantic, accessibility, content-safety, provenance, and no-hidden-facts boundaries.
