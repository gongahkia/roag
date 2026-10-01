# ROAG room-template editor

Launch the isolated authoring tool from a source checkout:

```bash
love level_editor --room-editor
```

The legacy `love . --room-editor` route remains available. Press `C` in the
editor to switch between the Dungeon and Reactor corpora. See
[`level_editor/README.md`](../level_editor/README.md) for the consolidated
level-tool documentation.

The editor never opens an active run or writes `active_run.json`. It edits one
`content/rooms/dungeon/*.room.json` template at a time and refuses to save a
template with validation errors. A packaged `.love` archive is read-only for
this tool; run it from the checkout when authoring content.

## Controls

- Click or drag: paint the selected tile (`1` masonry wall, `2` open floor).
- Right-click a boundary cell: add/remove its explicit connector.
- `[` / `]`: browse templates; `N`: new; `D`: duplicate.
- `I`: edit the semantic ID for a new or duplicated template.
- `G`: cycle the primary room tag; `+` / `-`: adjust selection weight.
- `A`: toggle rotation permission; `R`: preview 0/90/180/270 degrees.
- `S`: save validated JSON; `Y`: confirm an unsaved-change discard; `Esc`:
  exit (with the same unsaved-change protection).

## V1 schema

Every production room has this envelope:

```json
{
  "format": "roag.room_template",
  "version": 1,
  "id": "room.dungeon.standard.corner_01",
  "biome": "dungeon",
  "tags": ["standard"],
  "weight": 1,
  "allow_rotation": true,
  "width": 11,
  "height": 11,
  "connectors": [{"side": "north", "offset": 5}],
  "legend": {"#": "material.structure.masonry", ".": "material.terrain.air"},
  "layout": ["##########"]
}
```

`layout` contains exactly eleven strings of eleven ASCII glyphs, listed from the
north row to the south row. The legend maps each glyph to a semantic material
ID. A connector is a one-cell passable opening on the matching cardinal room
boundary. Other perimeter cells must remain solid. All v1 passable cells and
connectors must form one cardinally connected area.

Validate the corpus and a 500-seed dungeon sample with:

```bash
luajit tools/validate_content.lua
luajit tools/validate_dungeon_rooms.lua 500
```

The generation inspector (`love . --generation-inspector`) shows the produced
room slot, template ID, rotation, local cell coordinate, chunk boundary, and
connector points with its `T` room overlay.
