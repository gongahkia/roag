# ROAG Studio E04 — HUD, bitmap text, backgrounds and effects

Launch `love . --studio`; plain `love .` remains the game. E04 builds on E01's editable PNG artwork and E03's project workspace. It neither opens a gameplay run nor writes active-run or achievement records.

## Author and preview

1. In a Studio project, open **Artwork**. The **UI theme** group lists every drawable icon, panel, background, and effect; **Letters & text** lists individual printable ASCII characters, including all 26 uppercase and 26 lowercase letters. Select one, press **Enter**, paint its pixels, then **Save drawing** and **Use in Game**. Confirm the chosen binding; press **F5** in the running game to refresh. Each character is its own editable image (`core:glyph/ascii_NNN`, where NNN is its ASCII number). The old bitmap sheet remains usable for existing projects. Original imported bytes and E01 layers stay separate from published PNGs.
2. Press **U** (or click **U UI**) for the Presentation editor. Click or use Up/Down to select a semantic slot; Left/Right cycles compatible project images, including `(default)` to clear a binding. Slots cover the HUD backing panel, HP/XP/currency icons, title and pause backgrounds, a bitmap font, and `DamageTaken` / `BoonActivated` effects.
3. **I** edits panel nine-slice insets as `left,right,top,bottom`. **G** edits the legacy sheet's ordered ASCII glyph string; **M** edits `glyph width,height,advance,line height,spacing`, which also sets the display cell for individual letter drawings. **H** edits the mock HUD text sample; this preview text is not persisted gameplay data. Individual published characters take precedence over cells in the old sheet, then missing characters use LÖVE's font. Current game integration covers themed HUD labels and the HP readout; other game text still uses the built-in font. **B** cycles background `center`, `fit`, `cover`. **D** edits the selected hit/boon effect lifetime in milliseconds; **[ / ]** changes its scale. Effect lifetimes are presentation time only and are capped by the current playback event's beat.
4. **Tab** cycles HUD, title, chest and effect mock previews. **V** toggles 800×600 / 1024×640 mock viewports; **P** triggers a cosmetic effect preview. These mock values (HP 24/40, level 3, currency 12) are not gameplay state. **Ctrl+Z/Y** undo/redo metadata changes; **R** asks before reverting to the last saved theme. **S** saves, and **Esc** returns to assets after saving. Window close asks before discarding unsaved edits.

The source theme is inert, versioned plain data in checked rotating `studio/projects/<project-id>/theme-a.txt` / `theme-b.txt` records in the LÖVE save directory shown in Studio. It stores IDs and numeric parameters only. E01 images remain separately editable. An incompatible latest source record is reported rather than silently replaced with an older version.

## Publish and use in the game

From the game checkout, first publish the images, then publish the theme. Both commands are dry runs until `--apply` is given:

```sh
luajit tools/studio_publish.lua --save-dir '/absolute/save/directory/shown/in/Studio' --project my_project
luajit tools/studio_publish.lua --save-dir '/absolute/save/directory/shown/in/Studio' --project my_project --apply
luajit tools/studio_theme_publish.lua --save-dir '/absolute/save/directory/shown/in/Studio' --project my_project
luajit tools/studio_theme_publish.lua --save-dir '/absolute/save/directory/shown/in/Studio' --project my_project --apply
```

Use `--overwrite` for an occupied changed destination after reviewing its dry run. E04 checks every referenced published image ID, kind and PNG dimensions. The game reads the newest valid optional `assets/studio/theme-a.pack` or `theme-b.pack`. It caches images and glyph quads; the theme changes only rendering. Delete or disable both optional theme slots to restore the pre-E04 presentation. A corrupt newest theme falls back to an intact older generation; a valid incompatible newest theme is rejected. If the image manifest is missing or from a different Studio project, the entire theme falls back. The active run and achievement profile remain independent.

Asset, world and theme publication are **separate operations**, not one cross-pack atomic transaction. Publish images first. Neither LÖVE 11.5 `love.filesystem.write` nor these CLI writes provide a hardware fsync guarantee. The fixed effect styles use the existing fog-filtered `DamageTaken` and `BoonActivated` playback records; hidden positions, old floors and authoritative RNG are never consulted for cosmetic choices. E04 draws at most the current event's sprite, so the configured concurrent cap is a validation ceiling rather than a particle budget in this implementation.

## Verification and limits

Run `TMPDIR=/var/tmp luajit tests/run.lua`, `TMPDIR=/var/tmp sh tests/studio_e04_gui_smoke.sh`, the earlier Studio GUI scripts, and `TMPDIR=/var/tmp sh tests/repeat_gui_smokes.sh 3`. The E04 GUI smoke uses an isolated copied app and temporary `XDG_DATA_HOME`: it paints E01 icon and glyph pixels, reopens the theme in a second LÖVE process, dry-runs and applies both publishers, captures the real title/HUD/combat effect, rejects incompatible and corrupt optional packs, removes the pack, and continues the same authoritative saved run. Its printed temporary directory contains `.tga` captures and per-process logs. `sh tests/studio_e04_profile.sh` rebuilds that fixture and records callback draw duration and observed frame intervals under the available renderer; these are not native GPU performance claims.

This is a compact theme editor, not a full HUD layout designer or font editor. It supports one fixed-grid single-byte ASCII bitmap sheet with built-in font fallback, static backgrounds, a nine-slice HUD panel and two event-driven static effects. No animation timeline, atlas compiler, shader authoring, native pen pressure or arbitrary user-supplied Lua is included. Callback-driven GUI tests do not establish physical mouse/Wacom feel or native GPU frame rate.
