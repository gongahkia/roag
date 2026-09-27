# Frontends and assets

Jomon has one Pygame application with two renderer modes. Both consume the same `GameSession`, semantic commands, immutable views, runtime events, content pack, and format-15 saves.

```bash
uv sync
uv run python -m jomon
uv run python -m jomon --renderer debug
uv run python -m jomon --renderer ascii
```

The Debug renderer is the fresh-install default. `--renderer graphical` remains a compatibility alias for Debug. `--new`, `--seed`, `--load PATH`, and `--save PATH` are shared startup options. `--font PATH` and `--icon-font PATH` are local presentation overrides; neither is persisted. Run `uv run python -m jomon --help` for the current full CLI.

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

`jomon.font_stack.FontStack` is Pygame-only. It never downloads a font. Jomon vendors the pinned `BigBlueTermPlusNerdFontMono-Regular.ttf` from Nerd Fonts v3.5.1 under CC BY-SA 4.0; see [`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md). Its text lookup order is a valid explicit `--font` path, the bundled BigBlueTerm Nerd Font Mono, installed BigBlueTerm or BigBlueTerminal family aliases, installed Nerd Font aliases, then a local monospace family. Icon lookup similarly accepts `--icon-font`, then the bundled font, installed Nerd Font aliases, then text fallback.

The bundled font is the normal ASCII path after `uv sync`; a system installation and network access are not required. Explicit local font paths still override it. If the bundled resource is unavailable in a damaged installation, Jomon remains playable with a deterministic local monospace fallback.

## Frontend settings and saves

`AppSettings` is separate from `GameState` and format-15 saves. It persists the renderer preference as small JSON: Linux uses `${XDG_CONFIG_HOME:-~/.config}/jomon/settings.json`, Windows uses `%APPDATA%/Jomon/settings.json`, and macOS uses `~/Library/Application Support/Jomon/settings.json`. The priority is an explicit CLI renderer, then stored settings, then Debug.

Normal saves are also outside the repository: `${XDG_DATA_HOME:-~/.local/share}/jomon/saves` on Linux, `%LOCALAPPDATA%/Jomon/saves` on Windows, and `~/Library/Application Support/Jomon/saves` on macOS. `--load PATH` and `--save PATH` remain explicit overrides. A corrupt settings file safely falls back to Debug; it never affects a loaded game.

The title screen provides Join Game, Continue when the default save exists, Load Game, Settings, and Quit. Join Game opens a full application-page character setup before ordinary play. It holds only a frontend-local draft of stable setup IDs and point allocations: no `GameSession`, `GameState`, world map, camera, world media cache, or simulation RNG exists while that page is open. Confirming setup calls `GameSession.create_configured(seed, CharacterSetupCommand(...))`, which creates one deterministic world and commits those IDs. Escape discards the draft and returns to Title. Continue and Load attach an existing format-15 session directly and never show setup. Escape during ordinary play opens a frontend-only pause menu with resume, save, settings, return-to-title, and quit. Save/load selectors only operate on the user save directory or an explicit CLI path; their selections, title state, and menu cursors are never persisted in a game save.

Changing Debug/ASCII in Settings updates `AppSettings` and replaces only the active renderer strategy. The same `GameSession`, state, RNG, current activity, selected save path, and frontend command route continue unchanged. A CLI renderer choice is per-launch and does not rewrite the stored preference.

`ASCII_ICONS` centralizes renderer-chrome concepts including courier, NPC, threat, health, armour, inventory, equipment, weapon, ranged, ammunition, magic, chemistry, production, preparation, circuit, vehicle, vessel, travel, quest, warning, success, locked, inspect, interact, save, load, tavern, Draw, and Dice. Each has a plain-text/Unicode fallback such as `HP`, `INV`, or `QUEST`. Icons are never identities and a missing glyph cannot make a game state unreadable or alter a rule.

## ASCII semantic theme

`jomon.pygame_ascii.ASCII_THEME` is a renderer-only palette. It assigns stable visual roles to semantic terrain/features (terrain, wall, path, water, interactable, item, vessel, and travel), actors (courier, friendly, neutral, hostile, disabled), and UI state (health, armour, objective, magic, chemistry, production, circuit, success, warning, failure, and selection). The renderer derives a role from `CellView`/`ActorView`; it never sends colours back to the engine.

Visible cells use their normal semantic role. Remembered cells use a dimmed version of that same role and expose only remembered terrain; unseen cells are not drawn. Selection adds a renderer-local background and border, so it does not conceal the glyph or reveal an actor. Altering this theme, a font, or an icon cannot change state, RNG, runtime events, fingerprints, or save contents.

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
