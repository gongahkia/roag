# ROAG expansion: Windows/WSL to Mac handback

This document is for the single autonomous expansion goal. It does not change
Git history, active-run saves, permanent profiles or Studio projects.

## Source and personal data are separate

The Mac→Windows source handoff was verified from the user's GitHub push/pull
confirmation and Git ancestry. The exact preflight evidence is in
[`ROAG_EXPANSION_PROGRESS.md`](ROAG_EXPANSION_PROGRESS.md). The current
expansion working-tree edits are **local changes**. They will not reach another
computer until the user deliberately transfers them through a chosen source
workflow. This project does not automatically push, commit, reset or merge.

Git does not transfer ignored or uncommitted files, nor the LÖVE save directory.
The active run, permanent profile and Studio project workspace remain under
`love.filesystem.getSaveDirectory()` with identity `roag-foundation`; Studio
shows its actual workspace path. Back up or transfer those directories
separately if the same personal state is wanted on the Mac. Do not copy a
newer or incompatible save over a known-good record without a backup. Game
content versions are checked on Continue; no cross-version migration is
promised. Separate visual, world, theme, atlas and audio publishes are not a
single atomic deployment.

## Reproduce the engineering gates

From the ROAG checkout with LuaJIT and LÖVE 11.5:

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp bash tests/repeat_gui_smokes.sh 3 /var/tmp/roag-game-gui
TMPDIR=/var/tmp bash tests/expansion_audio_gui_smoke.sh
TMPDIR=/var/tmp bash tests/finale_disk_resume.sh
luajit tests/expansion_full_victory_trace.lua 9 /var/tmp/roag-adventurer.trace adventurer
luajit tests/expansion_full_victory_trace.lua 9 /var/tmp/roag-ranger.trace ranger
luajit tests/expansion_full_victory_trace.lua 45 /var/tmp/roag-arcanist.trace arcanist
luajit tests/expansion_full_victory_trace.lua 28 /var/tmp/roag-berserker.trace berserker
love --version
love .
love . --studio
```

`tests/expansion_generation_profile.lua` validates and times 100 generated
floor pairs per biome. `tests/expansion_profile.lua 9 adventurer` measures a
legal full run; `tests/profile_headless.lua 12` measures a synthetic crowd and
boon chains. `tests/profile_gui.sh` and `tests/studio_e05_profile.sh` sample
the current renderer. Profiles are measurements, not pass/fail promises.

The suite uses temporary LÖVE save directories for GUI/disk fixtures. Never
point these commands at a personal Studio/save directory. GUI harnesses send
callbacks and capture frames; they do not replace human keyboard, mouse,
tablet or listening checks.

## Ten-to-twenty-minute physical playtest

1. On the actual Mac or Windows desktop, launch `love .`, choose each class
   with **1–4**, start a normal run with **N**, and verify move, eight-way aim,
   special, Dash/step and 80-ms board-piece slides. If a controller is
   available, try shoulder-button class selection, Y New Run, D-pad menu
   choice, A confirm and B cancel; gamepad play aims cardinally while keyboard
   input retains diagonals. Note any lost key while playback gates input.
2. Traverse a stair in each direction. Inspect map, fog, remembered rooms,
   terrain cues and an enemy pursuing across floors. Test a ranged corridor:
   record seed, biome, floor, approximate tile and whether a lateral/cover
   response was readable under strict telegraph timing.
3. Buy a paid chest, try two boon choices, and verify activation text/sound;
   fight a biome miniboss, claim the free reward and make a route choice.
   Continue toward the Hollow Regent if time permits. Check that a defeat or
   Victory screen names the correct cause and route.
4. Save & Quit, relaunch and Continue. In Studio, audition a WAV/Ogg sound,
   assign a cue, save/reopen, publish after reviewing its author/license, then
   press **F5** in a running game. Confirm audio balance on actual speakers.
   If using a Wacom pen, check ordinary mouse-emulation drawing separately;
   pressure/tilt is not implemented.

For a defect, report OS, LÖVE version, native GPU or WSLg, seed, class, route,
biome/floor, action or screen, expected versus observed result, and whether
published Studio packs were enabled. A screenshot and any captured log help.

## Known evidence limit

Engineering checks ran on Fedora WSL2 with LÖVE 11.5 and Mesa llvmpipe. No
physical keyboard, mouse, Wacom pen, native GPU, Mac runtime or speaker
listening acceptance is claimed. The original synthesized sounds and numerical
balance remain provisional; actual player feedback should guide later polish.
