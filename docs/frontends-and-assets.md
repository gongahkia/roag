# Frontends and assets

Jomon has one Pygame application with two renderer modes. Both consume the same `GameSession`, semantic commands, immutable views, runtime events, content pack, and format-15 saves.

```bash
uv sync
uv run python -m jomon
uv run python -m jomon --renderer graphical
uv run python -m jomon --renderer ascii
```

`--new`, `--seed`, `--load PATH`, and `--save PATH` are shared startup options. `--font PATH` and `--icon-font PATH` are local presentation overrides; neither is persisted. Run `uv run python -m jomon --help` for the current full CLI.

## Shared application vs renderer

Shared frontend/application logic owns session lifecycle, semantic input to command translation, selection by stable ID/`Position`, save/load, panels, activity/tavern controllers, and runtime-event dispatch.

The graphical renderer owns tile/sprite drawing, camera, graphical effects, and interpolation. The ASCII renderer owns glyph-grid layout, text panels, terminal-style borders, and ASCII feedback. Neither renderer owns mechanics. They may map a mouse position to a local selected cell or option, but submit only the resulting stable entity ID, `interaction_id`, `action_id`, or map coordinate.

## Graphical and ASCII topology

Both render `WorldView` and `CellView` semantic identities. The graphical renderer resolves logical assets through `jomon.assets`; the ASCII renderer uses `ascii_glyph(semantic_cell_id, fallback)`.

```text
cell.vessel.wall / terrain.vessel.wall
  -> selected pack's glyph or resource binding
  -> '#' or a tile in one renderer
```

The arrow never runs backwards. A glyph cannot change collision, field of view, route validity, or an event. `legacy_token` in semantic topology is format-15 compatibility data, not a renderer contract to treat as mechanics.

## Logical assets

The selected pack's `assets.json` maps semantic IDs to logical asset IDs and logical IDs to descriptors. `jomon.assets` resolves bindings and descriptors without loading bytes. Only frontend-local Pygame code loads an image or sound, caches its Pygame object, and falls back if it is missing or unsupported.

Assets and fonts are absent from state, runtime events, fingerprints, and saves. Changing a sprite, audio file, glyph, font, or renderer mode must not alter commands, RNG, collision, state, or an ordered event stream. Runtime events remain asset-free: an `AttackResolved` is mapped to an animation/audio binding by the presentation layer after it has occurred.

## BigBlueTerm and Nerd Font icons

`jomon.font_stack.FontStack` is Pygame-only. It never downloads a font. Its text lookup order is a valid explicit `--font` path, installed BigBlueTerm or BigBlueTerminal family aliases (including Nerd Font variants), then a local monospace family. Icon lookup similarly accepts `--icon-font`, then installed Nerd Font aliases, then text fallback.

When BigBlueTerm is locally available, it is the preferred primary text font. When it is unavailable, Jomon remains playable with a deterministic local monospace fallback. The repository does not bundle BigBlueTerm or a Nerd Font; users may provide locally licensed files through the CLI paths.

`ASCII_ICONS` centralizes renderer-chrome concepts including courier, NPC, threat, health, armour, inventory, equipment, weapon, ranged, ammunition, magic, chemistry, production, preparation, circuit, vehicle, vessel, travel, quest, warning, success, locked, inspect, interact, save, load, tavern, Draw, and Dice. Each has a plain-text/Unicode fallback such as `HP`, `INV`, or `QUEST`. Icons are never identities and a missing glyph cannot make a game state unreadable or alter a rule.

## Renderer chrome versus content

Renderer-owned chrome includes `Back`, `Close`, key hints, mouse instructions, panel geometry, ASCII borders, generic save errors, BigBlueTerm layout, and Nerd Font UI icons. These change interaction and appearance, not Jomon fiction.

Pack-owned content includes fictional item/region/NPC names, quest prose, combat narration, setting labels, and semantic asset/glyph bindings. A simple decision rule: if a rewrite changes the fictional world, it usually belongs in a pack; if it changes how a Pygame UI works or is laid out, it belongs in the renderer.

## Renderer author checklist

- Query only `GameSession` views and submit commands; do not read or mutate `_state`.
- Use stable IDs and semantic topology for selection and rendering.
- Use runtime events for transient feedback, never message parsing.
- Keep frontend timing, camera, selection, hover, loaded resources, and panel state local and unpersisted.
- Verify a feature under both `graphical` and `ascii` if it is active gameplay.
- Treat audio/media/font failures as presentation fallbacks only.
