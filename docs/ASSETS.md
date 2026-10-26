# Asset ownership and authoring

Roag owns a complete local copy of all imported artwork, the exhaustive neutral catalog, mappings, authoring inputs, archives, credits and atlas tools. Ithica owns its own copy. There is no synchronization job or cross-repository path: edits in one do not silently change the other.

The schema-5 `assets/catalog.json.gz` stores every original inventory record and `sprites[].asset` description, subject claim/confidence, palette, type, layers, composition and evidence. It covers 11,887 regions / 7,115 occupied crops, 664 source PNGs, three ZIPs, and separately classifies 14 derived preview images plus eight documentation images. All original IDs, crops, source hashes and ambiguous identities are preserved.

## Current RPG inputs

| File | Purpose |
| --- | --- |
| `assets/ithica-expedition-bindings.json` | RPG semantic aliases to original stable IDs |
| `assets/ithica-generation.json` | Runtime generation/material/room/hazard rules |
| `assets/ithica-expedition-sprites.json` | Generated runtime manifest, glyphs and complete sprite index |
| `assets/reference/asset-semantics.json` | Authored neutral artwork descriptions and observations |
| `assets/sprites.json` | Frozen original import audit, identity/crop/evidence |
| `assets/atlas-index.json` / `assets/atlas.html` | Generated native/offline atlas |
| `assets/CREDITS.txt` | Publisher provenance and license declarations |

Historical `ithica-*` names and catalog fields are deliberately preserved. `usage.rpg` is the RPG binding projection. `ithica`, colony usage, lunar ideas and retained source guides describe origin metadata; they do not implement a colony in Roag. Mapping one sprite into a new role does not change its generic identity.

```sh
python3 tools/query_assets.py --type actor --color green --limit 10
python3 tools/query_assets.py --subject ghost --subject-confidence high --format ids
python3 tools/build_runtime_assets.py
python3 tools/build_game_catalog.py
python3 tools/build_game_catalog.py --check
python3 -m unittest discover -s tests/catalog -v
```

The first builder retains the inherited colony/font projection used as provenance and glyph input; it does not require a colony game checkout. All tools operate on this repository’s own inputs. The original inventory inside the tracked compressed catalog is also a generator input and must be preserved.

The full [inherited schema/query guide](../assets/reference/CATALOG_V2.md), [binding table](../assets/reference/rpg-bindings.md) and [offline atlas](../assets/atlas.html) remain available locally. The schema guide retains origin-specific wording and historical counts; use this document for current repository ownership.
