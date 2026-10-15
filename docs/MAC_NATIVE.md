# Intel macOS run and test guide

Use LÖVE 11.5, LuaJIT 2.1, and Python 3.9 or newer on x86_64 macOS. The tested LÖVE archive is the
[official 11.5 macOS release](https://github.com/love2d/love/releases/tag/11.5),
`love-11.5-macos.zip`. Its observed SHA-256 is
`6795bb3a1656af6a2fdfe741e150787b481886d3a280327a261a3fdded586913`.
The archive is unsigned; the matching checksum verifies the downloaded archive
against the tested copy, but is not a code signature. A user-level app can live
at `~/Applications/ROAG-Love-11.5.app`. If macOS blocks its first launch, review
it in System Settings → Privacy & Security and approve it there. Do not remove
quarantine or disable Gatekeeper. No administrator install is required.

From the checkout root, after the app is approved:

```sh
make game
make studio
make test
make gui
```

The Makefile uses the installed x86_64 LÖVE 11.5 app on this Mac. The direct
commands are:

```sh
export ROAG_LOVE_BIN="$HOME/Applications/ROAG-Love-11.5.app/Contents/MacOS/love"
/usr/bin/arch -x86_64 "$ROAG_LOVE_BIN" .
/usr/bin/arch -x86_64 "$ROAG_LOVE_BIN" . --studio
luajit tests/run.lua
luajit tests/finale_playthrough.lua 3 /tmp/roag-seed3.trace
```

For callback-driven GUI tests, put the opt-in wrappers first on `PATH` in the
test shell. They accept only disposable app copies under macOS temporary
directories, replace the copied `conf.lua` identity with a stable
`roag-mac-test-*` identity, and run the selected native x86_64 executable.
They never launch the checkout with the normal `roag-foundation` identity.
The `timeout` wrapper implements the GNU `timeout DURATION` and
`timeout -k GRACE DURATION` forms used by these scripts, with the requested
duration and exit code 124 on a timeout. These wrappers are macOS opt-in; the
Linux scripts retain their normal `love` and GNU `timeout` behavior.

```sh
export PATH="$PWD/tests/mac-bin:$PATH"
sh tests/repeat_gui_smokes.sh 3
for h in studio_gui_smoke studio_e01_gui_smoke studio_e03_gui_smoke \
         studio_e04_gui_smoke studio_e05_gui_smoke studio_e06_gui_smoke; do
    sh "tests/$h.sh"
done
sh tests/tranche15_gui_smoke.sh
sh tests/tranche15_chain_gui_smoke.sh
sh tests/biome_disk_resume.sh
sh tests/finale_disk_resume.sh
sh tests/profile_gui.sh
```

Studio and game fixtures write to their isolated LÖVE save identities. Use
`love.filesystem.getSaveDirectory()` or the `save_dir.txt` files emitted by
Studio fixtures to find their real macOS path; `XDG_DATA_HOME` does not move
LÖVE's macOS save directory. The renamed game uses the `roag-foundation`
identity, whose expected location on this Mac is
`~/Library/Application Support/LOVE/roag-foundation`; Studio displays the
actual path. Earlier game saves and Studio sources under the previous identity
are not loaded by this version. No external save directory was deleted or
merged during the name change. The separate `LOVE/roag` identity belongs to
another application and remains untouched.

The following publisher commands are **dry runs**. Supply the save directory
reported by the Studio project, and review the plan. Only an explicit
`--apply` writes a published pack into the checkout. Keep editable source
projects and layers in the LÖVE save directory; published PNGs and atlases are
derived copies.

```sh
luajit tools/studio_publish.lua --save-dir "$STUDIO_SAVE_DIR" --project my_project
luajit tools/studio_world_publish.lua --save-dir "$STUDIO_SAVE_DIR" --project my_project
luajit tools/studio_theme_publish.lua --save-dir "$STUDIO_SAVE_DIR" --project my_project
luajit tools/studio_atlas_publish.lua --save-dir "$STUDIO_SAVE_DIR" --project my_project
```

The matching Makefile shortcut is `make publish-preview PROJECT=my_project`.
Use `PUBLISHER=world`, `theme`, `audio`, or `atlas` for the other packs; compile
an atlas candidate first with `make atlas-build`. Applying a reviewed plan
requires `make publish-apply PROJECT=my_project APPLY=yes`, with
`OVERWRITE=yes` only when the publisher requires it. Set `SAVE_DIR` explicitly
if Studio shows a different location. Studio's own **P → D → A** flow publishes
loose visual art without a shell command.

These tests use synthetic GUI callbacks, screenshots, and temporary art. They
do not establish physical keyboard, mouse, or Wacom feel. The renamed identity
starts fresh; automated fixtures are not personal Studio projects.
