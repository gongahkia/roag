# ROAG Level Tools

This is the standalone home for developer tools that inspect or author level
content. It is separate from normal gameplay and never opens, autosaves, or
mutates `active_run.json`.

Launch from the repository root:

```console
love level_editor
love level_editor --room-editor
```

The first command opens the read-only Generation Inspector. The second opens
the writable Room Template Editor. The legacy developer launch routes
remain available for compatibility:

```console
love . --generation-inspector
love . --room-editor
```

## Generation Inspector

The inspector builds an isolated initial floor from the authoritative
generation pipeline. It does not advance turns or affect the active run.

- `[` / `]`: select a canonical biome/tier pairing.
- `,` / `.`: select a tier independently.
- Type a seed, then `Enter`: regenerate that exact floor; `R`: regenerate;
  `N`: next seed.
- Mouse wheel: zoom. Middle-drag, arrow keys, or `WASD`: pan. `F`: fit map.
- Left click: pin a cell; hover shows its coordinate.
- `1` terrain, `2` connectivity, `3` actors, `4` objects, `5` hazards,
  `6` liquids, `7` gas, `8` power, `9` objectives, `C` conductivity,
  `M` placement provenance, `T` authored dungeon room chunks/connectors.
- `H`: show/hide help. `Esc`: exit.

The inspector reports actual generated actors, objectives, media, device state,
and placement provenance. It does not invent hidden spawner or activation
systems that do not currently exist.

## Room Template Editor

The room editor works on one `content/rooms/<corpus>/*.room.json` template at a
time. It refuses invalid saves and only writes inside the selected corpus when
run from a source checkout. Packaged content is read-only. Press `C` to switch
between the Dungeon and Reactor corpora; the browser, new-room IDs, tags, and
material palette update together.

- Click or drag: paint the selected tile. The numbered palette at right shows
  the legal materials for the selected corpus (Reactor includes conductive
  deck plating and industrial bulkheads).
- Right click a boundary cell: add/remove its explicit connector.
- `[` / `]`: browse templates. `N`: new. `D`: duplicate. `I`: edit a new or
  duplicated room's semantic ID.
- `G`: cycle tag. `+` / `-`: change weight. `A`: toggle rotation permission.
  `R`: preview 0/90/180/270-degree rotations.
- `S`: save validated JSON. `Y`: confirm an unsaved-change discard.
  `Esc`: exit with the same protection.

Room templates use `roag.room_template` version 1: fixed 11×11 ASCII layouts,
semantic material legends, and explicit cardinal connectors. See the committed
room corpus for complete examples.

## Headless diagnostics

These read-only commands share the same generation and room data APIs:

```console
luajit tools/analyze_generation.lua --stage cave --seed 1000 --count 500
luajit tools/analyze_generation.lua --biome biome.legacy.forest --tier 2 --seed 3000 --count 100
luajit tools/validate_content.lua
luajit tools/validate_dungeon_rooms.lua 500
luajit tools/validate_reactor_rooms.lua 500
```
