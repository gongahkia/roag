# ROAG Sprite Editor

Launch this standalone editor from the repository root:

```console
love sprite_editor
```

The editor includes its own copy of the Kenney sheet, so it does not depend on
ROAG's runtime or need to mount the parent `assets/` directory.

Click a gameplay role in the left column, then click a tile in the Kenney sheet.

- Mouse wheel over the sheet: zoom at the cursor.
- Right- or middle-drag over the sheet: pan.
- `0`: reset the sheet's zoom and position.

- **Save JSON** serializes the mapping to `sprite_editor/mappings.json` beside this editor.
- **Load JSON** deserializes that same repository-local file.
- **Copy Lua** copies a replacement table for the game's `sprite_map.lua` to the clipboard.
- **Reset** restores the default mapping in the editor; save afterward to persist it.

The JSON schema is intentionally small:

```json
{
  "version": 1,
  "sprites": {
    "player": {"column": 25, "row": 1}
  }
}
```
