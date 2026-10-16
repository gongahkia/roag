# ROAG Studio E00

Studio is a separate mode of the existing LÖVE 11.5 application:

```sh
love . --studio
```

Plain `love .` still starts the game. Studio does not construct a gameplay run or read/write active-run and achievement records. The two modes share the established `roag-foundation` LÖVE identity so published visuals can be tried without moving player saves, but Studio files live only in `studio/projects/<project-id>/` under the save directory. The exact absolute workspace is printed inside Studio. LÖVE's platform save base is typically `%APPDATA%\LOVE\roag-foundation` on Windows, `$XDG_DATA_HOME/love/roag-foundation` (usually `~/.local/share/love/roag-foundation`) on Linux/WSL, and `~/Library/Application Support/LOVE/roag-foundation` on macOS. Use the displayed `love.filesystem.getSaveDirectory()` path when these defaults differ.

## First asset

1. Press **N**, type a project name, then Enter. The name becomes a safe project folder ID; the display name can differ from future asset IDs.
2. Drag a PNG onto the Studio window, or press **I** to import the bundled 16×16 Adventurer sample. **A** creates a draft metadata entry; dropping a PNG onto a selected draft attaches source pixels while retaining its stable ID.
3. Left-click an asset to select it. Double-click its row, click its preview, or click **Edit pixels** to draw. Right-click a row for **Edit pixels**, **Duplicate & edit…**, **Rename…**, and **Use in Game…**; the same actions appear beside the preview. Save the drawing, choose its game artwork slot, and confirm to publish that one binding. A named duplicate has independent pixels and an empty game binding. **Tab** filters by category. Press **1–5** to edit display name, stable asset ID, kind, game binding and nearest/linear sampling; Enter validates, Esc cancels. The older **B** shortcut only sets the Adventurer binding in project metadata; use **Use in Game…** to publish.
4. Click **Save** or press **S**. The image preview shows the decoded pixels over an alpha checkerboard. The footer reports validation/write errors. **Open** lists saved projects, which can be clicked. New/Open and window close ask before discarding unsaved edits. Close the window and launch Studio again; **Open** reopens the project and original PNG bytes.

The manifest is deterministic, human-readable, escaped tab-separated text in rotating `manifest-a.txt` and `manifest-b.txt` records. Each record has sequence, size and checksum. A failed/truncated write leaves the preceding valid generation available. Sources are immutable, separately stored PNG bytes under `sources/`; each import receives a distinct path. If the newest source becomes unreadable, Studio reports that fault and reopens an intact older project generation when one exists. Import uses LÖVE's actual PNG decoder and validates dimensions. E00 limits an image to 8 MiB and 1024×1024 pixels, and a project to 256 assets / 64 MiB of referenced image bytes. A draft can be saved without pixels; it cannot be published with a live game binding. Replacing pixels in an existing imported entry remains E01 work.

Each asset stores a stable namespaced ID, display name, kind, relative source and format, dimensions, alpha intent, sampling, optional description/tags, game binding, and reserved frame/animation/atlas metadata. Source filenames and future atlas positions are not identities. The manifest is inert data; it is never executed as Lua. Paths with root prefixes, `..`, backslashes, colons or unsafe segments are rejected. E00 currently recognizes one game binding, `core:actor/adventurer`; unknown or duplicate bindings fail validation visibly.

## Explicit development publish

LÖVE 11.5 writes within its save directory. Studio does **not** write into the game checkout. From the checkout, copy a validated project to the optional loose-source game directory with:

```sh
luajit tools/studio_publish.lua --save-dir '/absolute/path/shown/in/Studio' --project my_project
luajit tools/studio_publish.lua --save-dir '/absolute/path/shown/in/Studio' --project my_project --apply
```

The first command is a dry run listing created/changed files. `--apply` writes only `assets/studio/manifest.txt` and content-named PNGs under `assets/studio/sources/`; changed existing files require `--overwrite`. The game-facing manifest is derived from, and separate from, the editable project manifest. Images are copied before the game-facing manifest. No project string is passed to a shell command. Publishing does not touch run saves, profiles, gameplay content definitions or simulation snapshots. Review the dry run before `--overwrite` because publishing a new project intentionally replaces the optional active development manifest. The CLI runs from the game checkout and accepts the platform save directory as an absolute path, including a Windows drive path.

Normal `love .` resolves `core:actor/adventurer` once through the optional loose-source registry and draws that sprite for the player. Without the manifest, with missing pixels, or on decode error, it uses the preexisting gold token drawing. This presentation-only lookup does not consume game RNG or change the run snapshot. A future atlas builder can replace the loose loader behind the same stable binding.

## Verification and limits

Run `luajit tests/run.lua` for pure-Lua schema, source, failure-injection, publish and fallback tests. Run `TMPDIR=/var/tmp sh tests/studio_gui_smoke.sh` for four isolated LÖVE processes: create/import/save, reopen, publish/render and fallback. The smoke test uses a copied application and disposable `XDG_DATA_HOME`; it never writes a player's normal save. The keyboard and file-drop checks are callback driven, not physical mouse/tablet testing.

LÖVE 11.5 does not provide a portable atomic rename for these files or a native PNG `ImageData:encode` path. Source imports preserve original PNG bytes. There is no hardware fsync guarantee. E01 now adds a mouse/Wacom-as-mouse pixel canvas, layered source documents, undo/redo and verified PNG output; see [E01 pixel drawing](ROAG_STUDIO_E01.md). Pen pressure/tilt is not promised by standard LÖVE mouse callbacks. E00 does not implement drawing, frames, animation, room editing, atlases or hot reload.
