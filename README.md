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

On the title screen, choose **Continue** for the saved run or **New Run** to
choose a class. A new run replaces the single active save after confirmation.

| Key | Action |
| --- | --- |
| `W` / `A` / `S` / `D` or arrows | Move one cardinal tile; bump a hostile to attack or a chest/exit to inspect |
| `F` / `R` / `X` | Aim the class's basic attack / special / movement skill |
| Direction, then Enter | Choose aim and commit; `Q` / `E` / `Z` / `C` also aim diagonally |
| `P` or controller LB while aiming | Toggle Pilgrim's Vow when owned; its HP price is paid only on a committed attack |
| Space | Wait one action |
| `I` / `V` | Open boons and character stats / advance the boon page |
| `E` while exploring | Inspect nearby room objects and services |
| `U` | Inspect or use the carried utility item |
| `Tab` | Show or hide the minimap |
| `F1` | Show contextual game help |
| `F5` | Refresh artwork/audio without changing the run |
| Escape | Cancel aiming, close a panel, or open Pause; Save & Quit and Settings are in Pause |
| `W` / `S`, arrows, Enter | Select and confirm menu options |

Attacks resolve immediately; committed recovery and enemy telegraphs still
advance through the deterministic scheduler. During visual playback, wait for
the input gate to clear before committing the next action.

**Developer shortcut:** `B` changes the showcase loadout and makes the run
ineligible for permanent achievements. It is not a bomb command. Use
`make spectator` for disposable artwork inspection without using the active save.
See the [additional boon guide](docs/ROAG_IDEA_BOONS.md) for the current pass and
its verification limits.

## Reference

Name-wise, `Roag` pays homage to [Rogue (1980)](https://en.wikipedia.org/wiki/Rogue_(video_game)). Duh. 

Credits to [Bob Nystrom](https://github.com/munificent)'s [Hauberk](https://github.com/munificent/hauberk) also.

![](./asset/logo/hauberk.png)
