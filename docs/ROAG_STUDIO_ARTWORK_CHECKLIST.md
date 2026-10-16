# ROAG Studio artwork checklist

From this checkout, run `make studio`. The Make target passes
`--studio --current-project` to LÖVE 11.5. Studio reads this game's LÖVE
identity (`roag-foundation`) to select the `roag` editable project. If the
active published visual manifest names another project **and its editable
project is present**, Studio opens that project instead. If neither exists,
it creates and saves a new empty `roag` project under the Studio workspace
shown on screen. A corrupt existing project is reported and **not** replaced.
The original `love . --studio` project chooser remains available.

The first screen is **Artwork**. It reads the game's actual visual binding
registry and this project's current UI theme and World tile definitions. It
includes all four playable classes and every regular enemy/biome elite in
normal, attack and hurt poses; drops and chests; all
boon icons/cards; biome backdrops; automatic terrain shapes; UI theme slots;
and authored World tile assignments, including six mechanical terrain types
that can be added to a World pack from the checklist. These are destinations,
not hundreds of saved
blank images. They do not consume the 256-entry editable project limit until
you choose one to draw. Optional pose and terrain variants can fall back to
ordinary/base art. Unassigned destinations use the game's built-in visuals.

## Drawing from a destination

1. Use **Tab** to cycle Core / All / category groups. **M** shows only
   destinations without project art. Press **/** or **F** to search by name or
   stable ID; Enter closes search, Backspace edits it and Delete clears it.
2. Select a row with arrow keys, wheel or mouse. Press **Enter**, double-click,
   or click **Create artwork**. Enter `16x16`, `32x32` or another valid E01
   size (1–512 pixels per axis). The new editable sprite receives the selected
   game binding automatically. Existing bound art opens directly for editing.
3. Draw with E01 tools. Click **Save drawing** or press **S**. Click **Use in Game**
   or press **U**, choose the game destination, and confirm. Studio shows the
   precise binding and any existing art it will replace. Only that binding
   changes; other published assets remain. The earlier manifest is backed up.
   Press **F5** in an already running game to refresh the artwork. **Esc**
   returns to Artwork after saving if you want to keep the art unpublished.
4. For an **UI theme** or **World tile** destination, Enter opens the existing
   UI or World editor. Selecting a mechanical tile that is **not added** adds
   its semantic tile definition through the World editor's undoable action.
   Assign an E01 asset there; these use stable asset IDs rather than direct
   game visual bindings.
5. For a whole-project release, press **P** for Publish Readiness, **D** for a
   read-only file preview and **A** for the explicit loose-art confirmation.
   This advanced path still requires the current project to own the complete
   published manifest. The **Use in Game** path is the direct way to update one
   artwork slot when the existing manifest belongs to another project. World
   and theme packs publish separately; the E03/E04 guides give their commands.
   Saving alone never publishes.

**Missing/unmapped** means no artwork is assigned in this editable project;
**draft** means an asset entry has no pixels; **drawn, unpublished** means pixels
exist only in Studio; **changed since publish** means the source differs from
the published PNG; **published** means the current project source bytes match
the active loose published PNG. **Published elsewhere** means the active game
manifest contains the binding but this project has no editable source for it.
An E01 document with no visible nontransparent pixels stays **blank canvas**
after saving, so an empty file is not counted as finished artwork.
That label does not claim Git or Studio recovered another project's source.
Atlas selection and pack compatibility are detailed on Publish Readiness.

Existing E00/E01 projects remain separate and can be opened with **O** or the
Assets screen's **Open** button. No source pixels, imported PNGs, run saves or
profiles are copied into the auto-created project. The checklist is read-only
until you explicitly create/edit/save art. `love . --studio` without
`--current-project` retains the manual project chooser for isolated projects.

Validation: `luajit tests/run.lua` and
`TMPDIR=/var/tmp sh tests/studio_artwork_gui_smoke.sh`. The GUI script uses a
disposable LÖVE save directory, creates and paints one bound sprite, and
checks its exact pixel after a fresh-process reopen. It uses callback input,
not a human mouse or pen.
