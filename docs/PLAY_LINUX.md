# Play Roag on Linux

This release is a turn-based dungeon run: start with one of 15 fixed classes,
recruit up to four adventurers, find boons and utilities, clear three paired
stages and confront the Hollow Regent. There are seven dungeon floors. Cleared
floors persist and you can walk back. Defeating the Regent leaves the run playable.
Equipment/cooperation, NPC rescue, survival, living-world systems, profile unlocks
and the additional campaign bosses remain later-tranche work.

## Packaged game

Extract `roag-linux-x64-<commit>.tar.gz`, then run:

```sh
cd roag-linux-x64
./roag
```

The package includes .NET, SDL2, OpenAL and all game art/fonts. It needs a Linux
x64 desktop with OpenGL support and normal system graphics/audio libraries.
It needs no SDK, Lua, LÖVE, Python, network connection or sibling repository to
play. `BUILD.json` identifies its sources and SDK; `sha256sum -c SHA256SUMS`
checks the extracted files. The archive's adjacent `.sha256` verifies transport.

## Source build

Use the repository .NET wrapper (install the SDK with `./tools/setup-dotnet.sh`
if necessary):

```sh
./roag.sh
./roag.sh --new --seed 42 --save "$HOME/.roag/playtest.json"
./roag.sh --load --save "$HOME/.roag/playtest.json"
python3 tools/package-linux.py
```

Packaging uses the pinned project dependencies and records the actual SDK.
The first publish may download .NET's Linux runtime packs. It writes a new
archive under `dist/` and preserves existing archives. Published files contain
no generated combat reference project. Run `./tools/dotnet.sh test tests/Roag.Core.Tests/Roag.Core.Tests.csproj` for the native/reference regression;
reference checks do not establish completed native gameplay acceptance.

## Title menu

A normal launch opens the title menu over a separately generated, unsaved dungeon.
Its camera pans while native enemies move, attack and produce damage numbers.
Continue (or Begin Run for a new slot) enters your selected character; New Run
asks before replacing an existing run. Settings applies the same presentation
preferences as gameplay. Quit leaves the selected save untouched.

Use the mouse or arrows/Enter; controller D-pad/A also selects and confirms.
Reduced motion holds the background camera still and removes actor interpolation.
The in-game menu's Return to title saves your actual run before opening the preview.
Explicit `--new`, `--load` and `--skip-intro` enter gameplay directly; `--title`
forces the menu when needed. The preview never advances your character's clock.

## Controls

Gameplay uses the keyboard/controller shortcuts below. The persistent instruction
strips and bottom action-button grid are removed; that space now shows more dungeon.
F1 or Menu → Controls and help opens the control reference.

- Choose your class before the first paid action; it is then fixed for the run.
  Each class card shows its character sprite and a short description.
- WASD or arrows: one tile per press. Holding a direction does not repeat.
- F: aim the basic attack. R: aim the class special. X: aim its movement skill.
  Use a direction key to execute, or click an attack direction. Mouse clicks
  never walk the character. Escape cancels aim without spending time.
- G then a direction: Guard. Space: Wait. Both consume an action.
- Enter: interact with an adjacent chest/service/vault, select an available
  stage route or return through stairs. Entering stairs moves you individually.
- U: carried utility. I/V: inspect boons. Tab: toggle the explored-floor minimap.
  Hover tiles/enemies for inspection.
- F1: help. Escape: menu or close panel. Arrows select; Enter confirms.
- F5 or Save: save the continuing run. Menu → Save and quit exits safely.
- Xbox-style controller: D-pad/left stick moves; X basic, Y special, right
  shoulder movement, left shoulder Guard, right trigger Wait, A interact,
  right-stick press utility, Start menu/cancel, Back map, left trigger boons. Menus use D-pad,
  A confirm, B close. Physical controller verification is still pending.

Thinking, menus, aiming, inspection and rendering do not advance the timeline.
Orange locked cells show enemy attack impacts; move out before their due time.
Watch the timeline and class recovery. Doors take a paid opening action before
entry. Boon chests show three eligible offers and their cost before confirmation.
After an elite falls, return to its marked stage exit: Enter or an adjacent
click opens route choices. Opening a route costs an action; then walk down
the new stairs. The marker remains at the exit even if the elite moved.

## Saves, settings and defeat

The default slot is `~/.roag/campaign.json`, with its companion `.chunks`
directory. Keep both together when moving a save. Normal launch offers Continue for it;
`--load` requires it to exist. Use `--save PATH` for an independent slot.
An earlier prototype/content-version save cannot resume this dungeon game.
It is preserved, and the startup error prints commands for an unused separate
slot. For example, if `native-playtest.json` does not already exist:

```sh
./roag --new --save "$HOME/.roag/native-playtest.json"
./roag --load --save "$HOME/.roag/native-playtest.json"
```

Use `./roag.sh` for source builds. Keep using the selected `--save` path when
resuming; a bare launch still selects the original default slot.
There is no in-game rollback. A terminal death is recorded before a fresh run
can replace it; a failed write exposes a retry and preserves the ended run.
A new run replaces the character, build and dungeon. Presentation settings are
kept separately as `dungeon-presentation.json` beside the selected save.

The menu offers separate master/music/effects volume, fonts, text size,
contrast, reduced motion, shake, hit flashes, combat effects and damage numbers.
Attacks and hits have brief directional accents, recoil, sparks and sound cues;
chests spray gold coins from both sides when opened, with a larger burst and
a metallic cha-ching on boon acquisition. The acquired-boon banner remains
visible. Coins are decorative; rewards and chest prices follow the normal rules. These
effects animate while you think and never advance the turn clock.
The retained original audio consists of simple synthesized cues/loops.

For diagnostics, local recordings normally live under `~/.roag/sessions`.
`--no-recording` disables them. Preserve a problem save and describe the seed,
class, floor and action that caused the issue when reporting it.


Party controls: start alone and inspect the fixed-class adventurers in safe rooms. Joining costs one action and no gold, up to four surviving members. B/menu opens portraits, HP, readiness, floor and group/rescue controls. 1–4 changes the viewed member; T returns to the ready member. Viewing someone else grants no action. Only the actual opener receives a chest boon; bring that member beside it on their turn, confirm the recipient, then choose an offer. Gold and medicine are shared, including across floors; builds/boons/utilities remain individual.

Outside local combat, grouped followers spend their own turns moving or waiting. In the party page, Lead group changes the selected member's group leader, Regroup nearby gathers eligible companions within six tiles, and Split selected restores individual commands (splitting a leader also detaches their followers). These policy controls consume no time and move nobody. Stairs move only the actor and separate their group; companions remain on their tiles. H aims adjacent medicine rescue, or use the party page's Revive button: one medicine/action, 25% max HP, five own scheduled turns to bleed out; damaging hits shorten the timer. Party positions, builds, recovery, groups, placed class resources and bleed timers continue through the same save. Permanently dead members retain their identities and assigned boons; their slot becomes available for a later recruit.

Followers respect occupied bodies. If your group blocks a narrow turn or reversal, select the front/rear member and use Lead group, or Split selected to command them individually. Follow steps are paid on each member's own turn; stairs never pull companions along. The sidebar shows the controlled member's class resources; visible placed objects carry their owner's roster number and hover details. The timeline may be ready for a different member: press T to return to them before acting.
