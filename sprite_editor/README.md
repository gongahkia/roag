# ROAG Sprite Editor

This is the supported sprite-mapping editor. Normal gameplay no longer ships
an in-game Sprite Lab, so editing mappings here cannot affect an active run.

Launch this standalone editor from the repository root:

```console
love sprite_editor
```

The editor includes its own copy of the Kenney sheet, so it does not depend on
ROAG's runtime or need to mount the parent `assets/` directory.

This editor deliberately targets **ROAG 1-BIT (DEFAULT)** only. Select any
bundled third-party visual pack from the normal game's **ART PACKS** title-menu
entry; those packs use their own declarative built-in role maps and are not
overwritten by `mappings.json`.

The current **Sprite Workbench** is deliberately click-first:

- Filter roles by **Core**, **Terrain**, **Enemies**, **Elites**, **Reactor**,
  or **Boss**, then type `F` to filter by role name/semantic role.
- Click a role, then click a sheet tile to assign it. The assignment panel has
  precise column/row steppers, Reset, and Clear for optional wall faces.
- The panel also shows whether the hovered tile is already used by another
  role; shared tiles are intentional and visible rather than hidden.
- Mouse wheel over the sheet zooms at the cursor. Right- or middle-drag pans.
  Wheel over the role list scrolls it. `Home` fits the sheet.
- `⌘/Ctrl+S` saves, `⌘/Ctrl+Z` undoes, `⌘/Ctrl+Y` redoes, arrow keys adjust the
  chosen role's column/row, `R` resets, and Delete clears an optional role.

The role list includes every current player, object, ordinary enemy, elite,
Reactor mapping, and four optional wall faces: **left**, **right**, **up**, and
**down**. Assigning a wall face makes terrain walls use that tile when the
corresponding side faces passable terrain. Unassigned wall faces keep the
normal procedural fallback, so you can introduce them one at a time.

- **Save** serializes the mapping to `sprite_editor/mappings.json` beside this editor.
- **Reload** deserializes that same repository-local file.
- **Close** exits the editor; Escape also closes it.

The editor and all standalone authoring tools use the bundled Kenney Cursor
Pack for pointer, picker, hand, and pan states. Its CC0 license/source lives
under `assets/cursors/kenney/`.

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
