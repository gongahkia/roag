# ROEG Studio E06 — draft preview, publish readiness and visual refresh

Launch Studio with `love . --studio`. The title bar and workspace path identify
the editor; plain `love .` still starts the game. Studio projects remain under
`love.filesystem.getSaveDirectory()/studio/projects/` and never write run or
achievement records.

## Preview and status

Open a project with **O**, choose an asset, and press **E** for the pixel editor.
Brush strokes appear immediately on its canvas. **Tab** opens a read-only
side-by-side preview: current draft pixels at 1× (large images are clipped),
enlarged draft pixels, and the published version for the same stable ID or a
fallback label. Unsaved strokes appear there before saving or publishing.
Tab, Escape or a click closes the overlay. It never writes files. The E01
layer, tool, color, zoom, pan and undo controls remain available outside it.

**W** opens the E03 room editor. Painted cells, markers, sockets, masks and
authored art are a draft preview; **P/H** cycle allowed transforms and **F**
opens a deterministic generated-biome preview. **U** opens the E04 theme
editor; its **Tab** views include title, HUD at 800×600 and 1024×640, menu and
effect samples. These are draft presentation values, not a running game.

From the project view, **P** opens read-only **Publish Readiness**. It reports
project/selected IDs, save status, validated source pixels, changed loose
publication files, world/theme source versus published packs, atlas
compatibility and each visible asset's selected backing (`atlas`, `published
loose`, or `built-in fallback`). **P** rescans after an external publish;
**Esc** returns. Errors name the missing/bad resource. **F10** shows contextual
shortcuts in all Studio views; E03 keeps **F1** for room-name editing. A read-only report does not repair, save or
publish anything.

## Save, publish and refresh

Press **S** in Studio and inspect Publish Readiness. Copy the absolute save
directory shown in the Studio window, then run the applicable commands from
the ROEG checkout. Each first command is a dry run; review its changes before
the separately authorized apply. Add `--overwrite` only when a publisher says
an existing destination needs replacement.

```sh
luajit tools/studio_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project
luajit tools/studio_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project --apply
luajit tools/studio_world_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project
luajit tools/studio_world_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project --apply
luajit tools/studio_theme_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project
luajit tools/studio_theme_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project --apply
love . --atlas-build
luajit tools/studio_atlas_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project
luajit tools/studio_atlas_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project --apply
```

World/theme commands are needed only when those documents change. Publish
loose visuals before dependent world/theme packs. The atlas builder needs a
short LÖVE 11.5 process; it writes an inactive candidate into the Studio save
directory. The atlas publisher activates only a complete checked generation.
The visual, world, theme and atlas publishes are **separate transactions**.
If a later step fails, the game keeps validated earlier packs and the
atlas → loose → built-in fallback hierarchy. Correct the problem, dry-run and
apply the affected step, then rebuild the atlas if loose PNGs changed.

In an already running normal game, press **F5**. The game validates replacement
visual handles, theme and world pack before swapping presentation references
between frames. The HUD/title reports `refreshed`, `checked`, fallback or a
rejection reason. A stale/corrupt atlas uses compatible loose PNGs; a broken
new visual/theme/world candidate is rejected and the current good presentation
remains. Repeated F5 does not submit a game action or advance the simulation.
The **current concrete floor never regenerates**: changed world templates are
held for the next New Run. Existing floor tile IDs can use newly published
art immediately. Saves and snapshots do not contain GPU resources.

If a publish has a damaged manifest or missing ID, inspect the Studio status
view, restore the previous valid published generation or publish a corrected
one, then press F5. For intentionally disabled atlases, use the dry-run/apply
`--disable` and `--enable` commands in the E05 guide. The last valid active run
and profile are independent of this workflow.

## Verification and limits

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp sh tests/studio_e06_gui_smoke.sh
TMPDIR=/var/tmp sh tests/repeat_gui_smokes.sh 3 /var/tmp/roeg-e06-game-gui
```

The E06 GUI smoke uses isolated LÖVE data and app copies. It captures an
unsaved stroke, reopens its layered document in a fresh process, then changes
an actor and HUD icon **inside one running game process**. Frame pixels prove
the change after F5, atlas staleness/fallback, a rebuilt atlas, corrupt-page
fallback and recovery. It compares authoritative snapshots around every
refresh and rejects invalid theme/PNG candidates. These callback-driven tests
are not a physical mouse, keyboard or Wacom test.

The status view scans on request. It does not continuously watch the filesystem
or execute shell commands from LÖVE. Preview on the largest 512×512 E01
documents may still have the existing full-image upload cost for an edited
frame; the overlay reuses that texture. Repeated refresh replaces old Lua
Image/Quad references and lets garbage collection release GPU resources; it
does not implement a texture pool. Static art only: E02 animation and gameplay
Tranche 15 remain separate work.
