# Catalog v3: coverage, meaning, and regeneration

[catalog.json](../catalog.json) now combines the original file inventory with the full sprite audit and current usage in **both** games. Every original inventory key is retained. The source images, archive hashes, stable sprite IDs, crop rectangles, and frozen [sprites.json](../sprites.json) audit are unchanged.

## Coverage

| Scope | Count |
| --- | ---: |
| Original PNG files | 664 |
| Original ZIP archives | 3 |
| Total indexed regions | 11,887 |
| Occupied sprite regions | 7,115 |
| Non-gameplay slots, separators, annotations | 4,772 |
| Colony-bound sprites, including glyphs | 154 |
| RPG-bound sprites, including glyphs | 190 |
| Bound by either game | 241 |
| Occupied sprites unused by both games | 6,874 |

The occupied regions comprise **5,676 Urizen cells** (main sheet plus 2-bit sample), **653 Godot icon variants**, and **786 keyboard/controller prompt regions**. Registration in the atlas viewer does not mark a sprite as used in gameplay. Counts include per-variant crops, not a claim that every region is a unique standalone object. Multi-cell images, repeated art, tiny marks, and source credit regions retain their original audit notes.

## Structure

| Field | Meaning |
| --- | --- |
| `schema_version` | `3` |
| `images`, `archives`, `packs`, `summary` | Original file inventory and provenance, retained for compatibility |
| `catalog_summary` | Current coverage, live usage, and broad affordance-family counts |
| `game_bindings` | Current semantic aliases to stable IDs, independently for colony and RPG |
| `sprites` | Every one of the 11,887 original region records, enriched below |
| `catalog_limits` | Explicit limits on identification, assemblies, animation, and review |

Each `sprites[]` entry preserves the audit's ID, source, crop, category, identity basis/confidence, historical matches, visual metrics, crop hash, existing colony proposals, future ideas, and any character-matrix/duplicate/fragment metadata. It adds:

| Per-entry field | Meaning |
| --- | --- |
| `import_audit` | Original `runtime_status` and `mapping_status` at import time |
| `usage.colony`, `usage.rpg` | Active aliases; `font:X` denotes a glyph. Empty lists mean no gameplay binding |
| `runtime_status` | `used` when either game binds it, `unused` for an unbound occupied sprite, `not_applicable` for non-gameplay regions |
| `assessment.family`, `assessment.layer` | Broad scene/UI/equipment/effect grouping and possible drawing layer |
| `assessment.rpg_role` | A game-use interpretation, rather than a new publisher identity |
| `assessment.ideas` | Possible implementations for unused/unmapped artwork as well as bound sprites |
| `assessment.adaptation` | Recoloring, assembly, scale, setting, or mechanics work needed; uncertainties remain visible |
| `tags` | Pack, broad family, layer, and original category for filtering |
| `review` | Identity evidence, confidence, explicit-label status, visual-review requirement, and scope of the generated assessment |

The proposed families include actors, equipment, consumables, environment, vegetation, props, effects, technology, symbols/assemblies, typography, interface icons, input art, and unresolved sparse regions. The proposals use category/region rules and preserved visual observations. **This is exhaustive per-entry coverage, not a claim that every motif has received a new confirmed individual name.**

## Generation metadata and focused visual review

Schema 3 is an additive extension of schema 2; this document retains its original filename for existing links. Every region now has `generation.status`, `role`, `rule_refs`, `adjacency_basis`, `required_before_new_use` and an optional `visual_review`. Status distinguishes `runtime_rule`, `candidate_requires_rules`, `not_world_generation` and `not_applicable`. Runtime rule references point into [lantern-generation.json](../lantern-generation.json); 43 distinct IDs currently participate. This is separate from general RPG bindings and atlas registration.

[urizen-generation-review.json](urizen-generation-review.json) holds 104 per-crop visual observations, decisions and future uses from a focused inspection of the largest Urizen sheet. [The generated Markdown table](urizen-generation.md) shows source identities alongside those observations and actual rule references. These observations supplement preserved identities and never upgrade uncertain publisher labels. The native and offline atlas show/search generation and visual-review notes.

Room sockets, fuel, safe-route masks, collision and hazard behavior are authored game data. Material indices in a sprite sheet are never interpreted as WFC sockets. Unused/unmapped sprites retain explicit future-use work and limitations.

## Identity versus casting

Historical Urizen identities retain their exact visible-RGB matching evidence. Godot icons retain filename identities. Input labels retain visual-reading confidence. Unknown figures, profession columns, body variants, and machinery fragments stay uncertain; being chosen for a game role does not upgrade their identity confidence.

For example, the RPG casts the author's **dog** sprite as a Briar hound; its author identity stays `animal / dog`. A figure with a cross-like held prop becomes Sable's appearance, without declaring that the publisher called it a vestal. A low-confidence chest/workshop fragment serves as a coffin-shaped crypt prop; that is a scene interpretation, not a recovered author label. The author-confirmed bow is used for the ranger's ability icon rather than silently relabeling an axe.

No column is treated as a confirmed animation frame, no row/color as a confirmed species, and no structural index as a connectivity bitmask. Bitmap glyphs are only added to the font when identified. Color variants do not imply gameplay rarity or faction. All such meanings belong in game data.

## Files to use

| Task | File |
| --- | --- |
| Complete current data | [catalog.json](../catalog.json), `sprites[]` |
| Search/preview/paint offline | [atlas.html](../atlas.html) |
| Lightweight native viewer metadata | [atlas-index.json](../atlas-index.json) |
| Colony selection | [runtime-sprites.json](../runtime-sprites.json) |
| Edit RPG selection | [lantern-bindings.json](../lantern-bindings.json) |
| Generated RPG runtime definition | [lantern-sprites.json](../lantern-sprites.json) |
| Review curated RPG aliases | [rpg-bindings.md](rpg-bindings.md) |
| Frozen import-time proposals and visual evidence | [sprites.json](../sprites.json), [original browser](../sprites.html), original per-pack Markdown tables |

The native runtime registers every occupied crop for the viewer but loads its image texture lazily. The offline HTML embeds metadata and original PNG bytes to support file-based canvas inspection/export without networking. Exported scene recipes store exact IDs by layer, and do not change the source sheets.

## Regenerate and check

Python 3 standard library only; the games do not require Python:

```sh
python3 tools/build_game_catalog.py
python3 tools/build_game_catalog.py --check
```

The generator reads the frozen audit, existing colony manifest, hand-curated RPG aliases, generation rules, and focused visual-review JSON. It updates catalog v3, the lightweight index, RPG manifest, binding table, and self-contained atlas HTML deterministically. `--check` verifies generated content and the original 664 PNG / 3 archive hashes. CI runs this check on Linux.

If the frozen import audit ever intentionally changes, its development-only Pillow generator remains `tools/build_sprite_reference.py`; afterward regenerate the colony selection with `tools/build_runtime_assets.py`, then this v2 layer. Re-review semantic choices after a changed sheet instead of transferring old atlas coordinates blindly. Current source PNGs are never recolored or repacked by these generators.
