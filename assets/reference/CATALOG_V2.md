# Ithica catalog: schema 4, coverage and regeneration

[catalog.json](../catalog.json) retains the original inventory and every region from all three packs. Schema 4 adds explicit **current Ithica casting and phase usage**, separately from retained RPG preview bindings. Source pixels, original IDs, crop rectangles, hashes and publisher-identity confidence are preserved. This guide retains its historical filename for existing links.

## Coverage

| Scope | Count |
| --- | ---: |
| Original PNGs / ZIP archives | 664 / 3 |
| Total indexed regions | 11,887 |
| Occupied sprite regions | 7,115 |
| Non-gameplay slots, separators and annotations | 4,772 |
| Ithica-selected IDs, including shared font/UI | 166 |
| Colony-selected IDs, including font/UI | 154 |
| Current field aliases / distinct IDs | 14 / 14 |
| Retained preview-selected IDs, including font | 191 |
| Preview-only IDs, outside current Ithica selection | 75 |
| Union of current and preview selections | 241 |
| Occupied regions unbound by either selection | 6,874 |

Occupied regions comprise **5,676 Urizen cells**, **653 Godot icon variants**, and **786 input-prompt regions**. Counts refer to crops/variants, including repeated art and fragments, rather than guaranteed unique standalone objects. Registering all 7,115 crops in the atlas does not make them gameplay bindings.

## Schema and compatibility

| Field | Meaning |
| --- | --- |
| `schema_version` | `4` |
| `images`, `archives`, `packs`, `summary` | Original inventory and provenance retained |
| `catalog_summary` | Coverage and selection counts, including current Ithica and preview-only counts |
| `game_bindings` | Preserved colony and RPG-preview alias maps; their keys retain compatibility meaning |
| `ithica_casting` | Authored current lunar casting source, limits and generator ownership |
| `sprites` | All 11,887 original region records, enriched without changing their source identity |
| `generation_rules` | Retained preview/intermediate-generation rule metadata |
| `catalog_limits` | Identification, animation, assembly and review limits |

Schema 4 is additive to schema 3. Existing `usage.colony` and `usage.rpg` are retained; `rpg` refers to the preview selection, not a claim that every fantasy binding appears in the lunar game. New consumers should use `sprites[].ithica` for current game usage. The original import audit remains frozen in [sprites.json](../sprites.json).

Each record preserves source, crop, original identity/confidence, exact-match evidence, visual observations, duplicate/fragment metadata and import-time proposals. It also carries:

| Per-record field | Meaning |
| --- | --- |
| `ithica.status` | `bound`, `preview_only`, `unused`, or `not_applicable` |
| `ithica.usage.colony`, `.expedition` | Context-specific alias lists; `font:X` means a bitmap glyph |
| `ithica.roles`, `.limits`, `.generation` | Current lunar interpretation, adaptation limits and placement category |
| `ithica.future_ideas` | Lunar possibilities for occupied crops, including unused and preview-only art |
| `ithica.evidence` | Explicit separation between game casting and publisher identity/review |
| `usage`, `runtime_status` | Historical combined-selection view retained for compatibility |
| `assessment` | Broad family/layer, retained RPG possibilities, ideas and adaptation limits |
| `generation` | Rule references, candidate status, adjacency basis and optional focused visual review |
| `review` | Identity evidence/confidence and the actual extent of visual inspection |

Every occupied crop has future-use proposals; non-gameplay regions remain marked as such. The new lunar proposals use evidence-scoped family/category rules and existing observations. **This is exhaustive per-entry coverage, not a new manual identification of all 7,115 motifs.**

## Current lunar use and retained preview use

[ithica-sprites.md](ithica-sprites.md) lists all 14 current field aliases and their limits. [ithica-casting.json](../ithica-casting.json) is the authored source. Examples:

- The boulder crop is blocking surface rubble in Ithica; the legacy preview can use the same crop as nonblocking scenery. Collision belongs to the map's explicit ground/object data.
- A crystal crop marks actual recoverable components. Rewards enter carried cargo and reach home inventory on return.
- A leaf-like crop is a symbolic interference/exposure overlay; it does not establish outdoor lunar vegetation or supernatural lore.
- Resident and unidentified figures retain their existing source evidence. Field roles, suit protection and enemy behavior are game data.
- Ordinary vegetation belongs only in a suitable enclosed habitat. Ordinary spreading fire requires oxygen; lunar sites do not inherit the fantasy preview's outdoor fire or spores.

[rpg-bindings.md](rpg-bindings.md) retains the full preview binding inventory. Fantasy companions, monsters and scenery there are identified as preview casting. Their descriptions do not create lunar mechanics or proper names.

The generator preserves authored WFC sockets on 7×7 room chunks, CA terrain rules and global route/quest checks. Its intermediate/preview rules reference **43 IDs**. The current lunar generator recasts terrain, objects and constraints explicitly; atlas indices never imply sockets, animation or collision.

[urizen-generation-review.json](urizen-generation-review.json) retains **104 individually inspected crop observations**, shown in [urizen-generation.md](urizen-generation.md). Observations do not upgrade uncertain publisher identities. Current lunar roles are listed separately from those retained preview decisions.

## Authoring and derived files

| Task | File |
| --- | --- |
| Complete source of truth | [catalog.json](../catalog.json), `sprites[].ithica` and preserved evidence |
| Current lunar casting and family ideas | [ithica-casting.json](../ithica-casting.json) |
| Field/preview alias-to-ID selection | [ithica-expedition-bindings.json](../ithica-expedition-bindings.json) |
| Preview/intermediate generation rules | [ithica-generation.json](../ithica-generation.json) |
| Colony selection | [ithica-colony-sprites.json](../ithica-colony-sprites.json), generated by the colony builder |
| Generated field/viewer manifest | [ithica-expedition-sprites.json](../ithica-expedition-sprites.json) |
| Current field casting table | [ithica-sprites.md](ithica-sprites.md) |
| Native viewer metadata / offline browser | [atlas-index.json](../atlas-index.json) / [atlas.html](../atlas.html) |
| Frozen import audit / original browser | [sprites.json](../sprites.json) / [sprites.html](../sprites.html) |

Colony and field aliases are separate contexts and can intentionally refer to different crops, such as `wall`. Combining dictionaries by overwrite would corrupt those mappings. Original publisher filenames and credits remain intact during the Ithica rebrand.

The native viewer loads source textures lazily. The offline atlas embeds original PNG bytes for file-based inspection and scene-lab export without networking. Exported recipes contain exact IDs by layer; runtime recipe import is not implemented.

## Regenerate and verify

Python 3 standard library only; Python is not a game runtime dependency:

```sh
python3 tools/build_runtime_assets.py
python3 tools/build_game_catalog.py
python3 tools/build_game_catalog.py --check
```

The catalog builder reads the frozen audit, original inventory retained in the existing catalog, colony manifest, field/preview bindings, current casting, generation rules, focused review and HTML template. It deterministically regenerates schema 4, the viewer index, field manifest, Markdown tables and offline HTML. **Keep the existing catalog's original inventory; it is also an input.** `--check` verifies freshness and all 664 original PNG / 3 archive hashes. CI runs it on Linux.

Edit authoring inputs, then regenerate derived files. Do not hand-edit generated records/tables or transfer coordinates blindly after an intentional sheet change. The optional original-audit development tool uses Pillow; its use would require a deliberate audit migration. No generator recolors or repacks source PNGs.
