# ROEG Studio E01 — pixel drawing

Launch with `love . --studio`. Plain `love .` still launches the game. Studio uses `studio/projects/<project-id>/` under the absolute workspace path displayed in the window (`love.filesystem.getSaveDirectory()`); it does not write active-run or achievement files.

## Draw and save

1. Press **N** for a project name, then **C** and enter `16x16`, `32x32`, `64x64`, `128x128`, or a custom `widthxheight` from 1–512 pixels per axis. This creates a transparent sprite. Alternatively, drag a PNG into a project or press **I** for the sample, select it, and press **E** to create an editable layer from its decoded pixels. Imported PNG bytes remain untouched in `sources/`.
2. In the pixel editor, left-drag to draw. **P** pencil, **E** eraser, **I** composite eyedropper, **F** four-connected fill. The pencil uses deterministic source-over RGBA compositing; eraser clears all four channels. Pick one of 16 swatches or press **H** (or click the RGBA field) and enter eight hexadecimal digits `RRGGBBAA`. **[** and **]** choose 1, 2, 4 or 8-pixel square brushes.
3. Use the layer panel or **L** add, **Delete** remove, **R** rename, **V** show/hide, **K** lock/unlock, **PageUp/PageDown** reorder. Layers are listed bottom to top. Click a layer to select it; scroll the panel to reach additional layers. Locked or hidden layers cannot be painted. Click Undo/Redo or press **Ctrl+Z / Ctrl+Y** (Ctrl+Shift+Z also redoes). One complete drag is one undo step.
4. Mouse wheel zooms around the cursor, using integer scales from 100–3200% plus 25%/50% overview levels for large sprites. Middle-drag or Space+left-drag pans; **0** fits the sprite. The canvas uses nearest filtering. The status line shows tool, RGBA, brush, layer, cursor, dimensions, zoom and unsaved status. **S** commits the document and its derived PNG as a verified project generation. **Esc** returns to asset metadata after saving; it will not discard an unsaved edit. New/Open and window close retain E00 discard confirmations.
5. In asset metadata, select the sprite, press **B** to bind the pilot Adventurer visual, and use **2** to assign the stable ID `core:actor/adventurer` if desired. Save again. The stable ID and binding are independent of source filenames and future atlas placement.

A Wacom pen configured by its desktop driver to emulate mouse press/move/release uses the same path. Pressure and tilt are unavailable through this E01 input path and have not been hardware verified. Window coordinates use LÖVE's logical input/graphics coordinates. The Studio window has a 1024×640 minimum to keep E00 metadata controls visible. Enlarging it recalculates the canvas viewport; it does not rescale source pixels.

## Source and publish

`manifest-a.txt` and `manifest-b.txt` are the rotating, checked E00 project records. E01 adds optional relative `document` and `derived` fields to each asset; old E00 records still open. Each save writes a new immutable `documents/layers-XXXX.rgba` source and `derived/painted-XXXX.png` before switching the manifest. The source document is versioned `ROEG-LAYERS/1` with exact RGBA8 planes, stable layer IDs, names, order, visibility, lock state, selected layer and checksum. The derived PNG is flattened from visible layers at save time. Studio validates both on reopen and visibly reports a recovered older complete generation when newer referenced data is torn. There is no hardware fsync guarantee. Older immutable document generations remain in the workspace for recovery and can consume disk space over many edits.

The PNG writer in `src/studio_png.lua` emits RGBA8 PNG with stored DEFLATE/zlib blocks, Adler-32 and PNG CRCs; it does not call the LÖVE 12-only `ImageData:encode('png')`. It is lossless, larger than a compressed art PNG, and decodes in LÖVE 11.5. Editable source stays separate from both imported PNG and derived output. Documents are limited to 512×512, 16 layers, 16 MiB encoded each and 128 MiB of actively referenced project data. History is bounded to 32 actions or 32 MiB; saving establishes a clean checkpoint while undo/redo can still cross it in the current session. The editor does not flatten on each pointer event. Very large, many-layer projects and uncollected old generations may need future storage/performance work.

Publishing is still **explicit** and defaults to dry-run:

```sh
luajit tools/studio_publish.lua --save-dir '/absolute/path/shown/in/Studio' --project my_project
luajit tools/studio_publish.lua --save-dir '/absolute/path/shown/in/Studio' --project my_project --apply
```

`--overwrite` is required when replacing an existing changed published manifest. The publisher chooses the asset's derived painted PNG when present and emits the existing game-facing stable-ID manifest. It never writes run/profile saves. Removing or disabling `assets/studio/manifest.txt` restores the original game fallback token.

## Checks

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp sh tests/studio_gui_smoke.sh
TMPDIR=/var/tmp sh tests/studio_e01_gui_smoke.sh
TMPDIR=/var/tmp sh tests/repeat_gui_smokes.sh 3 /var/tmp/roeg-e01-game-gui
```

The E01 GUI smoke uses disposable `XDG_DATA_HOME`, creates and edits a two-layer transparent sprite through LÖVE callbacks, captures before/mid/after canvas frames, reopens it in another process, edits and reopens an imported PNG, validates decoded PNG pixels, dry-runs and applies publishing in an isolated app copy, and checks normal-game sprite selection plus fallback. Callback tests are not physical mouse or pen testing.
