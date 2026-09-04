# Jomon deck-plan contract

`src/medieval/jomon-deck-plan.ts` is the v1, compiled, renderer-independent static layout contract for Jomon's bounded local deck. It derives a small fixed plan from a validated `FoundationWorld` and its existing `FoundationJomon`; it does not create a second vessel, world, map, or mutable authority.

## Layout and ownership

`FoundationJomon` remains authoritative for the fixed eight partitions and existing props. The plan binds those owners to a canonical 18 by 8 local coordinate grid:

- `quay-approach` is a local shore-side approach only. It has no `FoundationJomon` partition, site, route, frontier fact, save record, or travel destination.
- `gangplank`, `tavern`, `chart-table`, `cargo-hold`, `repair-space`, `stores`, `berths`, and `galley` each bind to their existing partition.
- `prop:gangplank`, `prop:task-ledger`, and `prop:chart-table` bind to their existing kind, partition/area, and a bounded anchor. The contract invents no other props and no prop operation.

Areas, hull-boundary cells, and access edges are canonical ordinal-ID and row/column order. The quay joins the gangplank, and all vessel areas are reachable through explicitly verified orthogonal adjacency. `jomon-navigation.ts` v1 consumes those edges as the only local movement geometry; it owns no geometry itself. Movement is cardinal or diagonal only when both orthogonal paths are valid, so it cannot cut corners or bypass the gangplank.

## Primary ASCII projection and glyph vocabulary

`createTerminalPresentationModel()` v7 derives one common, renderer-independent terminal map from this validated plan. Its 18 by 8 materialized viewport retains exactly 113 static plan area/hull cells in canonical row/column order and, after zero-time courier selection, overlays one source-backed `@` active-courier marker at the persisted local coordinate. The same projection carries the bounded task-ledger courier-switch prompt, including its prop-binding source and current-time evidence; it exposes only the household selection fields needed by the renderer. The browser canvas consumes that projection; it owns no deck geometry, glyph character, world fact, or map-state authority. The v1 terminal map legend derives its current visible glyph entries, meanings, accessibility text, and source/time/freshness evidence from those source-backed cells and the closed glyph catalogue; it never receives copied geometry or hidden world knowledge. The v2 detailed-renderer adapter receives the exact map, legend, and prompt projection, including marker, focus, accessibility text, and safety data, and remains a deferred parity adapter, not a detailed renderer.

The closed `JOMON_ASCII_GLYPH_CATALOG` remains authoritative. Only these existing `future-jomon-deck` entries appear in the current plan:

| Character | Semantic meaning | Palette role | Non-colour cue | Text/accessibility equivalent | Ownership |
| --- | --- | --- | --- | --- | --- |
| `)` | Quay approach | `route` | `READY` | Known quay approach route; local source-backed access area | Glyph catalogue and deck plan |
| `/` | Gangplank | `route` | `READY` | Vessel gangplank; source-backed vessel access area | Glyph catalogue and existing Jomon prop/partition |
| `=` | Open deck space | `bodyText` | `KEY` | Named static deck space | Glyph catalogue and existing Jomon partition |
| `#` | Hull planking boundary | `mutedText` | `KEY` | Hull boundary, structural static deck cell | Glyph catalogue and deck plan |
| `@` | Active adult courier | `selectedText` | `ACTIVE` | Known active courier position and fixed camera focus | Glyph catalogue; authoritative `WorldDeckNavigationState` |

These characters are semantic references with paired textual accessibility descriptions and affirmative content-safety classifications; colour never carries their meaning alone. Raw colours remain owned by the v2 semantic palette, while glyph characters and authored text remain owned by the closed glyph catalogue. The map contains only the active-courier marker in addition to static deck structure: no cargo, other people, terrain, route-travel, hazard, or interaction state is materialized.

## Legend and in-world readability

The normal ASCII world view keeps the map itself primary and permanently shows a compact key derived from the same terminal legend entries. Opening remappable command help (default `?`) supplements that key with the full current visible-glyph legend, effective bindings, and this truthful movement context: the map is a fixed known local deck; a successful local step advances one action minute; blocked hull, boundary, non-walkable, or diagonal-corner steps leave world state and time unchanged. `Escape` closes help with the existing zero-time cancellation behaviour.

The legend states its current limits plainly: the tavern task ledger alone supports a zero-time courier switch at its exact anchor; cargo, NPCs, hazards, travel, fog, rest, conversation, loss, succession, and every other prop action remain unavailable. Each entry keeps its glyph reference, semantic palette role, paired non-colour cue, accessibility equivalent, safety classification, and evidence. The canvas resolves a glyph character only from that entry's closed glyph reference; it does not hold copied symbols, meanings, deck geometry, world state, persistence, timer, or input-execution authority. A future detailed renderer receives the same legend/help/prompt data and effective-control help through the adapter, but cannot execute input, advance time, mutate a world, or persist anything.

## Validation and known-facts boundary

Derivation first validates the entire existing `FoundationWorld`, then verifies the Jomon partitions, prop identities, kinds, and partition bindings. The derived plan itself is fail-closed: it rejects malformed or stale source data; missing, unknown, duplicate, or noncanonical identifiers; unsafe classifications; unrecognised glyph references; invalid/out-of-bounds/overlapping coordinates; unexpected fields that could carry hidden world data; broken anchors; and disconnected or diagonal-only topology.

The plan contains no world time, manifest, initial-world, causal-history, known-fact, save, persistence, browser, randomness, worker, cache, or storage authority. It is deterministic, discardable derived data. `WorldDeckNavigationState` v1 is instead part of the full authoritative `FoundationWorld` envelope and causal replay projection. It holds only the current active courier ID and a validated walkable coordinate; it has no copied geometry or browser-only state. Selection assigns the canonical tavern anchor at zero action time. At that exact source-bound ledger anchor only, the separate tavern contract may replace the active courier and retain the anchor at zero time. Each accepted step journals typed `deck-moved` evidence and advances exactly one canonical action minute. A blocked step returns a bounded collision reason and changes neither state, time, history, nor storage.

## Compatibility and deferred work

The movement addition changed the medieval envelope from `FoundationWorld` v13 / `MedievalWorldState` v11 to v14 / v12. The current ledger addition retains `FoundationWorld` v14, immutable ID, seed, manifest, generation provenance, navigation v1, and IndexedDB layout v4 while changing mutable state to v13 and replay projection to v5. A strict read-only v14/v12 conversion sets active courier equal to the valid selected initial courier, preserves the valid local coordinate, rebinds the affected causal checkpoint, and proves current replay. Corrupt, stale, ambiguous, or incompatible data fails closed and is not overwritten. Valid older layout-v3 envelope storage remains untouched by this schema conversion until an explicit normal save.

The current fixed full-deck camera follows the active courier without panning, streaming, or a larger grid. All static cells are already known; fog/visibility rules are deliberately deferred. The only current prop interaction is tavern crew switching through `prop:task-ledger`; cargo, repair, travel, quay departure, rest, conversation, loss, succession, other actors, tactical play, and wider spatial state remain with later roadmap owners. The quay remains a local layout area, not a world site or travel route. Those owners must retain the current semantic, accessibility, content-safety, provenance, and no-hidden-facts boundaries.
