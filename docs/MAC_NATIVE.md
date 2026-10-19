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
make test
make gui
```

The Makefile uses the installed x86_64 LÖVE 11.5 app on this Mac. The direct
commands are:

```sh
export ROAG_LOVE_BIN="$HOME/Applications/ROAG-Love-11.5.app/Contents/MacOS/love"
/usr/bin/arch -x86_64 "$ROAG_LOVE_BIN" .
luajit tests/run.lua
luajit tests/finale_playthrough.lua 3 /tmp/roag-seed3.trace
```

Studio authoring belongs to the separate Paint project. It is not included in
this checkout; ROAG has no `make studio` target or editor launch mode.

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
sh tests/tranche15_gui_smoke.sh
sh tests/tranche15_chain_gui_smoke.sh
sh tests/biome_disk_resume.sh
sh tests/finale_disk_resume.sh
sh tests/profile_gui.sh
```

Game fixtures use isolated LÖVE save identities. `XDG_DATA_HOME` does not move
LÖVE's macOS save directory; the wrappers keep fixtures separate from the normal
`roag-foundation` identity at `~/Library/Application Support/LOVE/roag-foundation`.
No personal save directory is deleted or merged by these commands. ROAG keeps
one active run and a separate permanent profile.

Publisher commands and editor GUI harnesses moved out of this game project.
The fixed artwork, optional published world/audio readers and presentation-only
F5 remain. See [current acceptance](ROAG_IDEA_ACCEPTANCE.md) for the full current
verification and [audio runtime](ROAG_AUDIO_EXPANSION.md) for pack contracts.

These tests use synthetic GUI callbacks and screenshots. They do not establish
physical keyboard, mouse, controller, Wacom or listening acceptance. The latest
integration measurements were made on Fedora WSL/llvmpipe; this guide describes
how to run macOS checks, not a claim that the latest expansion was tested there.
