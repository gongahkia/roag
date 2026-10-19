[![](https://img.shields.io/badge/roag_1.0.0-passing-green)](https://github.com/gongahkia/roag/releases/tag/1.0)

# `Roag` 🗡️

A silly [roguelike](https://en.wikipedia.org/wiki/Roguelike) with some [secrets](https://www.reddit.com/r/animalWell/comments/1e5ufby/has_anyone_actually_found_every_secret_in_the/).

## Stack

* *Script*: [Lua](https://www.lua.org/), [LÖVE2d](https://love2d.org/)
* *Font*: [BigBlue Terminal](https://int10h.org/blog/2015/12/bigblue-terminal-oldschool-fixed-width-font/)
* *Sprites*: [1-Bit Pack](https://kenney.nl/assets/1-bit-pack) by Kenney

## Screenshots

<div align="center">
  <img src="./asset/reference/1.png" width="45%">
  <img src="./asset/reference/2.png" width="45%">
</div>

<div align="center">
  <img src="./asset/reference/3.png" width="45%">
  <img src="./asset/reference/4.png" width="45%">
</div>

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

## Controls

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

## Reference

Name-wise, `Roag` pays homage to [Rogue (1980)](https://en.wikipedia.org/wiki/Rogue_(video_game)). Duh. 

Credits to [Bob Nystrom](https://github.com/munificent)'s [Hauberk](https://github.com/munificent/hauberk) also.

![](./asset/logo/hauberk.png)
