# ROAG Sprite Editor

Launch this standalone editor from the repository root:

```console
love sprite_editor
```

Click a gameplay role in the left column, then click a tile in the Kenney sheet.

- **Save JSON** serializes the mapping to `mappings.json` in the editor's LÖVE save directory, displayed in the application.
- **Load JSON** deserializes that file.
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
