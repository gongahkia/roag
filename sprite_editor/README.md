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

- **Save JSON** serializes the mapping to `sprite_editor/mappings.json` beside this editor.
- **Load JSON** deserializes that same repository-local file.
- **Close** exits the editor without changing the file; Escape also closes it.

ROAG reads `sprite_editor/mappings.json` on launch and whenever its window
regains focus. Save your selections here, then start or refocus ROAG; no manual
copying into `sprite_map.lua` is required.

The packaged `roag.love` build also prefers this same sidecar JSON file when it
is launched beside the repository's `sprite_editor/` directory.

The JSON schema is intentionally small:

```json
{
  "version": 1,
  "sprites": {
    "player": {"column": 25, "row": 1}
  }
}
```
