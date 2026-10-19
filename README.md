[![](https://img.shields.io/badge/roag_1.0.0-passing-green)](https://github.com/gongahkia/roag/releases/tag/1.0)

# `Roag` 🗡️

A silly [roguelike](https://en.wikipedia.org/wiki/Roguelike) with some [secrets](https://www.reddit.com/r/animalWell/comments/1e5ufby/has_anyone_actually_found_every_secret_in_the/).

## Stack

* *Script*: [Lua](https://www.lua.org/), [LÖVE2d](https://love2d.org/)
* *Font*: [BigBlue Terminal](https://int10h.org/blog/2015/12/bigblue-terminal-oldschool-fixed-width-font/)
* *Sprites*: [1-Bit Pack](https://kenney.nl/assets/1-bit-pack) by Kenney

## Screenshots

![](./asset/reference/1.png)
![](./asset/reference/2.png)
![](./asset/reference/3.png)
![](./asset/reference/4.png)

## Usage

The below instructions are for locally running `Roag`.

1. First clone the repository.

```console
$ git clone https://github.com/gongahkia/roag && cd roag
```

2. Then run the below from repo root.

```console
$ make game
```

Artwork is fixed to Kenney 1-Bit with the approved monochrome Kenney
counterparts. Settings adjust sound, effects and accessibility; there is no art
pack selector. `F5` refreshes artwork/audio. See the [sprite coverage guide](docs/ROAG_SPRITE_AUDIT.md).
Rewards use muted brown/gold ink, enemies blue-gray, hostile attack outlines red
and player previews green. Damage numbers smoothly cycle through vivid colors;
they can be disabled under Settings → Effects → Floating damage numbers.

In **Settings → Sound**, click or drag each volume slider; the percentage appears
on its right and saves when released. Arrow keys adjust the selected volume too.
**Settings → Accessibility → Text size** cycles through Small, Normal and Large.

## Controls

On the title screen, select **Continue** for the saved run or **New Run** to
choose a class. A new run replaces the single active save after confirmation.
The camera has a fixed zoom.

| Key | Action |
| --- | --- |
| `W` / `S` or arrows, then Enter | Select and confirm menu options |
| Title: `C` / `N` / `U` | Continue the saved run / quick-start the selected class / view achievements |
| Class chooser: arrows or `W` / `A` / `S` / `D` | Cycle all fifteen classes; `1`–`9` select the first nine, Enter starts the selected class |
| `W` / `A` / `S` / `D` or arrows | Move one cardinal tile; bump a hostile to attack or a chest/exit to inspect |
| `F` / `R` / `X` | Aim the class's basic attack / special / movement skill; Wayfinder's `X` returns immediately when a beacon exists |
| Direction, then Enter | Choose aim and commit; `Q` / `E` / `Z` / `C` also aim diagonally |
| `P` or controller LB while aiming | Toggle Pilgrim's Vow when owned; its HP price is paid only on a committed attack |
| Space / `.` / keypad `5` | Wait one action |
| `I` / `V` | Open boons and character stats / open or advance the boon page |
| Boon panel: up/down, left/right or wheel, Enter | Select a boon, change pages, inspect it; `I` or Escape closes the panel |
| `E` while exploring | Inspect nearby room objects and services |
| `U` | Inspect or use the carried utility; when aiming an Escape Scroll, `1`–`3` choose distance |
| Chest/route: `1`–`3` or arrows, then Enter | Select and confirm an offer/route; Escape cancels without spending |
| Room service: arrows, then Enter | Select, inspect and confirm; Escape goes back |
| `Tab` | Show or hide the minimap |
| `F1` / `F3` / `F5` | Contextual help / diagnostics / refresh artwork and audio without changing the run |
| Escape | Cancel aiming, close a panel or open Pause |
| Pause: `S` or `2` / `3` | Save & Quit / open Settings |
| Settings: left/right, `Tab` or `]`, `[` | Adjust the selected option, next category, previous category |
| Left click / drag | Select menu options and nearby objects / adjust volume sliders; double-click a boon to inspect it |
| Controller D-pad, A / B, Start / Back | Move or aim; confirm / cancel; pause / minimap |
| Controller X / Y / RB | Aim basic attack / special / movement skill |
| Controller LB / left-stick click / right-stick click | Open/advance boons (toggle Vow when aiming) / nearby services / utility |

Attacks resolve immediately; committed recovery and enemy telegraphs still
advance through the deterministic scheduler. During visual playback, wait for
the input gate to clear before committing the next action.

**Developer shortcut:** `B` changes the showcase loadout and makes the run
ineligible for permanent achievements. It is not a bomb command.
`make spectator` opens disposable artwork inspection without using the active
save: `F6` toggles the free camera, `F7` cycles biomes, `F8` changes floors,
`F9` changes classes, and movement keys pan the free camera (Shift pans faster).
See the [additional boon guide](docs/ROAG_IDEA_BOONS.md) for the current pass and
its verification limits.

## Reference

Name-wise, `Roag` pays homage to [Rogue (1980)](https://en.wikipedia.org/wiki/Rogue_(video_game)). Duh. 

Credits to [Bob Nystrom](https://github.com/munificent)'s [Hauberk](https://github.com/munificent/hauberk) also.

![](./asset/logo/hauberk.png)
