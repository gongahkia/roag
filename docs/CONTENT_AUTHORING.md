# Content authoring from the template

1. Copy the template pack.
2. Keep `manifest.json` structurally valid. Set `playable` only when a world and all four setup option groups exist.
3. Add concrete mechanics to `systems.json` using stable IDs. Its core sections are `world`, `setup`, `items`, `actors`, `quests`, `routes`, and `recipes`. Its `activities` section contains independent empty lists for production, progression, preparation, chemistry, magic, circuits, vehicles, vessel, crises, situations, worklines, Draw, and Dice. A future game can instantiate only the systems it uses.
4. Add display-only history and setting material to `lore.json`.
5. Add non-mechanical narrative relationships to `connections.json`.
6. Add optional visual/audio bindings to `assets.json`.
7. Run validation and tests under both Pygame renderers.

Never use names, descriptions, menu positions, pixel coordinates, or glyphs as references. A system-owned route belongs in `routes`; a recipe dependency belongs in `recipes`; an equipment rule belongs in an item definition. `connections.json` cannot control gameplay.

`lore.json` entries use stable IDs and `{title, summary, body, tags}`. `connections.json` rows use `{id, from, relation, to, tags}`. Both endpoints must be declared stable IDs (a system entity or lore entry). Both are presentation-only: changing them cannot affect RNG, commands, the mechanical fingerprint, or saves.
