# Content authoring from the template

1. Copy the template pack and select it with
   `JOMON_CONTENT_PACK=/path/to/pack` while authoring.
2. Keep `manifest.json` structurally valid. Set `playable` only when a world and
   all four setup option groups exist.
3. Add concrete mechanics to `systems.json` using stable IDs. Its core sections
   are `world`, `setup`, `items`, `actors`, `quests`, `routes`, and `recipes`.
   Its `activities` section contains independent empty lists for production,
   progression, preparation, chemistry, magic, circuits, vehicles, vessel,
   crises, situations, worklines, Draw, and Dice. Instantiate only systems the
   pack genuinely supports.
4. A world may add typed `features`: `base`, `maintenance_latch`, `access_gate`,
   and `objective_cache`. A latch opens a declared access ID only through a
   local interaction with its declared equipped item. A gate uses that access ID
   for collision. An objective cache names its owning operation and item.
5. `operations` is optional. Each operation explicitly references its quest,
   objective feature/item, return base, and methods. A method requires exactly
   one declared physical fact—an opened access ID or a defeated actor—and has a
   stable consequence ID. Do not put these rules in descriptions or
   `connections.json`.
6. Add display-only history and setting material to `lore.json`, non-mechanical
   relationships to `connections.json`, and optional visual/audio bindings to
   `assets.json`.
7. Run the automated suite under both Pygame renderers and verify a save/load
   continuation for any operation-bearing pack.

Optional setup rows may carry an engine-defined `health_bonus` integer. This is
mechanical and belongs in `systems.json`; names remain presentation. Item rows
may set `initial: false` for an item that enters inventory only through a
validated system interaction.

Never use names, descriptions, menu positions, pixel coordinates, or glyphs as
references. A system-owned route belongs in `routes`; a recipe dependency
belongs in `recipes`; an equipment rule belongs in an item definition.
`connections.json` cannot control gameplay.

`lore.json` entries use stable IDs and `{title, summary, body, tags}`.
`connections.json` rows use `{id, from, relation, to, tags}`. Both endpoints
must be declared stable IDs (a system entity or lore entry). Both are
presentation-only: changing them cannot affect RNG, commands, the mechanical
fingerprint, or saves.
