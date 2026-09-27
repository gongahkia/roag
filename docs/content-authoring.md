# Content-pack authoring

This guide is for authors who want to rewrite or reskin Jomon without changing the game rules. Read [architecture.md](architecture.md) first.

## Pack layout and selection

The shipped pack is `jomon/content_packs/default/`. Its `manifest.json` has format version **1** and identifies a `catalog_root`; the shipped default points to the repository's `jomon/data` catalog set. A selected external pack is loaded through `jomon.catalog.load_content_pack()` before main-world catalogs are imported. Startup selects `JOMON_CONTENT_PACK` when it is set; otherwise it uses the bundled default pack.

There is no separate pack-validation command. Validate a pack by loading it through `load_content_pack(path)` or by running the focused content-pack tests. The loader rejects duplicate JSON keys, malformed documents, unsupported format versions, missing required presentation files, invalid IDs, and contract violations. It also requires every catalog named by the manifest's `catalog_root`.

To make a mechanically compatible reskin:

1. Copy `jomon/content_packs/default/` to a new location.
2. Give `manifest.json` a valid lower-case pack ID and display name. Retain a catalog root containing the same compatible mechanics unless making an intentional mechanics change.
3. Rewrite presentation files and allowed `assets.json` bindings only.
4. Load the pack with `load_content_pack()` and run an alternate-pack test.
5. Start each renderer with `JOMON_CONTENT_PACK=/path/to/pack` set before importing main-world content.

Pack selection is intentionally early: `select_content_pack()` rejects a switch after main-world catalogs have been loaded. Restart the process to test another pack.

## Presentation domains

The default pack currently contains the following presentation files. Each is strictly validated by `jomon.catalog`; do not add arbitrary keys or use a file as a free-form UI-string dump.

| Domain | File | Owns |
| --- | --- | --- |
| People and setup | `characters.json` | Character, role, and person presentation |
| Items | `items.json` | Item labels and descriptions |
| World and quests | `regions.json`, `quests.json`, `history_text.json`, `topology_text.json` | Region, quest, history, discovery, and topology wording |
| Actions and aftermath | `action_text.json`, `aftermath_text.json`, `interference_text.json`, `legendary_text.json` | Player-facing result and aftermath prose |
| Crafting/progression | `chemistry_text.json`, `production_text.json`, `preparation_text.json`, `progression_text.json`, `equipment_text.json`, `material_text.json` | Labels, templates, and explanations for those domains |
| World systems | `travel_text.json`, `vessel_text.json`, `ship_crisis_text.json`, `vehicle_text.json`, `sanctum_text.json`, `situation_text.json`, `circuit_text.json`, `ecology_text.json`, `worklines.json` | Domain-specific fictional presentation |
| Tavern | `tavern_games.json` | Draw and Dice terms, cards, and messages |
| Frontend-facing semantic text | `ui_text.json` | Contracted semantic labels, not layout or input rules |
| Dormant compatibility | `dullest_dungeon/text.json`, `dullest_dungeon/visuals.json` | Legacy DD presentation only |
| Assets | `assets.json` | Logical media bindings and ASCII glyph choices |

These files do **not** own entity IDs, map geometry, recipes, damage, costs, RNG, progression rules, or availability. Those remain in the catalog root and engine state. Existing domain documents such as `action-content-packs.md`, `production-content-packs.md`, and `vehicle-content-packs.md` describe their specific contracts.

## Templates

Contracted templates use Python-style named fields supplied by the engine. The contract declares which placeholders are permitted for each semantic slot. Keep the required placeholders exactly as the relevant contract requires; do not add attributes, indexing, conversions, format specifications, expressions, or undeclared fields.

For example, the default UI domain contains a template such as:

```json
"ui.start.save_path": "Save: {path}"
```

The engine supplies `path`; an author may rewrite the surrounding prose but not make `{path}` determine a file, rule, or action. Treat template values as already-resolved display values. They are not a query language and must not be used to reach into arbitrary game objects.

## Asset manifest

`assets.json` has `asset_manifest_format: 1` and exactly these top-level sections: `glyphs`, `resources`, `animations`, and `bindings`.

```text
stable semantic ID -> logical asset ID -> pack-relative resource descriptor
terrain.vessel.floor -> image.terrain.floor -> media/developer_tile.png
```

`resources` currently supports `image` and `audio`; a resource may omit a path for a renderer fallback. `animations` reference declared image resources. Bindings are grouped as `actors`, `terrain`, `features`, `items`, `actions`, `events`, `ambience`, `tavern`, or `dullest_dungeon`. Glyph keys are semantic cell IDs such as `cell.vessel.wall`; they are a Pygame ASCII presentation, not map authority.

Paths must be relative to the selected pack and safe. Absolute paths, traversal, malformed resources, unknown binding categories, and references to undeclared resources/animations are rejected. `jomon.assets` only resolves logical descriptors; Pygame's frontend-local resource cache opens media bytes. Missing optional media falls back visually and never changes a mechanic.

## Alternate-pack regression pattern

Build Pack B from Pack A with substantially different names, narration, templates, glyphs, and asset bindings while retaining the same mechanical catalogs. From identical seed/state/commands compare:

- state and stable IDs;
- RNG state;
- `main_world_mechanical_fingerprint()`;
- ordered runtime event types and fields;
- semantic views and topology.

Presentation, logical assets, and glyphs may differ. Mechanics, persistence, and events must not. `tests/test_content_packs.py` and `tests/test_topology_assets.py` contain loader and alternate-pack fixtures to adapt for a new domain.

## Author checklist

```text
Content concept:
Existing semantic IDs:
Presentation files changed:
Template slots/placeholders:
Logical assets or glyph bindings changed:
Mechanical changes: NONE / explicitly listed elsewhere
Validation: load_content_pack(...)
Alternate-pack proof:
Graphical and ASCII inspection:
```

Stop and involve a mechanics contributor if an edit needs to change an ID, recipe, collision, damage, route cost, availability, state mutation, random selection, or save meaning. A pack is allowed to reskin the same game; it is not a second rules engine.

## Fresh-setting handoff recipe

An author or coding agent replacing Jomon's setting should work in this order:

1. Read [architecture.md](architecture.md), this guide, and [frontends-and-assets.md](frontends-and-assets.md). Keep `jomon/data/` as the compatible mechanical catalog root unless the task explicitly changes rules.
2. Copy `jomon/content_packs/default/`, give `manifest.json` a new valid pack ID, and preserve its format version and compatible `catalog_root`.
3. Rewrite the presentation files listed above: people/setup, items, regions/quests/history/topology, action and system narration, tavern terms, and the active system text domains. Preserve JSON keys and declared template placeholders.
4. Change `assets.json` bindings, pack-relative media, and semantic ASCII glyphs if the setting needs a different visual language. Do not change renderer-chrome icons such as Save, Back, or Settings.
5. Load the pack in a fresh process with `JOMON_CONTENT_PACK=/absolute/path/to/pack`, then exercise both `uv run python -m jomon --renderer debug` and `uv run python -m jomon --renderer ascii`.
6. Run the focused alternate-pack and invariance coverage: `uv run python -m unittest tests.test_content_identity_14e tests.test_topology_assets tests.test_mechanical_compatibility tests.test_save_content_compat`.
7. Save under one compatible pack and load under the other. Current semantic presentation re-renders from stable IDs; historical rendered logs intentionally remain frozen records.

Keep legacy-looking engine strings only when they are stable IDs, deterministic compatibility tokens, save migration data, diagnostics, or frozen history. For example, an old spell display value may remain as an engine-only damage-seed token while `magic_text.json` supplies every current visible spell name. Do not use such compatibility values as a source for new visible prose.
