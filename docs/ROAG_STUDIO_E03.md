# ROAG Studio E03 — room and biome authoring

Launch `love . --studio`; plain `love .` remains the game. Studio files live under the save directory shown in the editor, at `studio/projects/<project-id>/`. The World editor does not write active-run or achievement records.

## Create a room and use painted tile art

1. Open or create a Studio project. Use E01's **C** sprite workflow to paint a tile image, save it, return to asset metadata, set its kind to `tile` with **3**, and give it a stable ID with **2**. Save again. Imported E00 PNGs can be used the same way; their original bytes remain intact.
2. With that tile asset selected, press **W** (or click **W World**) to enter World / Room Editor. Press **N** for a `9x7`, `15x11`, `23x17`, or odd custom room up to 23×17 tiles. New rooms begin with four open sockets, an enemy, chest and secret anchors, so the generator has a valid starting template.
3. Choose a tool by clicking its top-bar label or use **1–0** for floor, wall, water, void, authored tile, editable mask, protected mask, socket, enemy anchor and chest anchor. The last two top-bar tools place the secret approach and wall clue. Left-drag paints; rapid drags fill skipped cells. Mouse wheel zooms, and middle-drag or Space-drag pans. **Ctrl+Z / Ctrl+Y** undo or redo a complete drag.
4. **Tab** cycles tile types. **A** adds the six mechanical terrain tiles to an existing three-tile project. **I** assigns the selected Studio tile asset to the current tile type; **U** clears that assignment. Paint with the Tile tool (**5**) to store the tile's stable ID in cells. **K** adds a named custom tile type, **F3/F4** edit its name/ID, and **Z** cycles the selected tile's terrain semantics. **E** paints the selected asset as a decoration without changing collision; **Backspace** selects the visual eraser. Use **O** to cycle movement, sight and projectile overlays. Tile art is separate from its gameplay properties.
5. Click a boundary with the Socket tool to relocate that side's socket and open its inner approach. Click with Enemy, Chest, Secret Anchor or Secret Wall to toggle a marker. Editable cells may vary during generation; all other authored geometry is protected by default. Explicit protected cells show in orange, editable cells in cyan. Void corners are outside the room footprint.
6. **F1/F2** edit room display name/stable ID. **R** cycles the primary room role; **C/L/Q** add or remove treasure, landmark and secret roles. **B** cycles a single assigned biome. **F** cycles the biome preview, and **J** toggles that biome in the room's eligibility set. **+/-** adjust selection weight from 1 to 20. **T/M** change allowed rotations/mirroring. **P/H** preview transformed geometry, sockets, markers, masks and art; transformed previews are read-only until returned to source orientation.
7. Press **V** for a diagnostic. The validator checks IDs, references, socket placement and inner clearance, role anchors, masks, center clearance and cardinal paths among required sockets/anchors using the generator's room contract. Fix the reported cell/anchor before saving. **G** runs the real seeded biome generator with this project included. The overview outlines placed authored rooms; **[ / ]** change seed and **F** changes preview biome. A valid room may not be selected by every seed.
8. Press **S** to save. **Esc** exits a generated preview first; after saving, Esc returns to asset metadata. **Up/Down** or the room list selects another room; **D** duplicates and **X** asks before deleting. Closing with unsaved edits asks before discarding. Reopen the project in a fresh `love . --studio` process to continue editing.

## Publish into the game

Studio writes only within LÖVE 11.5's platform save directory. From the game checkout, publish visuals and world source separately. Both commands default to a dry run:

```sh
luajit tools/studio_publish.lua --save-dir '/absolute/save/directory/shown/in/Studio' --project my_project
luajit tools/studio_world_publish.lua --save-dir '/absolute/save/directory/shown/in/Studio' --project my_project
luajit tools/studio_publish.lua --save-dir '/absolute/save/directory/shown/in/Studio' --project my_project --apply
luajit tools/studio_world_publish.lua --save-dir '/absolute/save/directory/shown/in/Studio' --project my_project --apply
```

Use `--overwrite` only after reviewing a dry run when an occupied published slot or visual manifest must change. The world publisher writes a checked generation to the inactive `assets/studio/world-a.pack` or `world-b.pack`; normal `love .` loads the newest valid generation. The source document stays in its separate checked `world-a.txt` / `world-b.txt` Studio files. The published pack is inert escaped text, never Lua code. Remove or temporarily disable both published world slots to use only built-in room pools again. Existing published visuals may remain; without a matching authored room, the game draws its ordinary terrain. Changing or disabling a pack while an active run depends on it causes that run to be reported incompatible rather than silently resuming against different room definitions. The separate achievement profile is unaffected.

The default E03 catalogue still starts with floor, wall and shallow water for old-project compatibility. **A** adds six validated mechanical tiles: bramble, bog, ice, breakable barricade, mist and crystal. Their movement, sight and projectile flags are explicit and checked against the game terrain definitions. See [terrain expansion rules](ROAG_TERRAIN_EXPANSION.md). The biome selector now includes seven identities, including Observatory, Archive and Forge. The editor assigns rooms to biome pools; it does not author new topology algorithms.

## Verification and limits

Run `luajit tests/run.lua`, `TMPDIR=/var/tmp sh tests/studio_e03_gui_smoke.sh`, the E00/E01 smoke scripts, and `sh tests/repeat_gui_smokes.sh 3`. The E03 GUI test creates an E01 painted PNG, edits a room through callbacks, saves and reopens it in a new LÖVE process, publishes through both dry-run/apply CLIs in a copied checkout, renders the authored tile in a normal generated stage, checks fallback, then saves and continues that authored run in two fresh processes. It writes screenshots and logs to the temporary path printed by the script. The test uses disposable `XDG_DATA_HOME`; it never touches normal player saves. Callback-driven pointer tests do not establish physical mouse, Wacom hardware or native GPU feel.

World document and published-pack writes verify readback and retain the prior valid slot when the inactive write tears. They do not promise hardware fsync or an atomic cross-file transaction with the separately published visual manifest. E02 animation, atlas compilation, a WFC editor and arbitrary new biome topology algorithms remain deferred.
