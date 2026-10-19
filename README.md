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

## Controls

| Key | Action |
| --- | --- |
| `W` / `A` / `S` / `D` | **Campaign:** cardinal movement only; the most recently pressed held direction wins. Movement sets facing. |
| `E` | **Campaign:** attack with the active quick weapon in the facing direction. A carried tool in that slot strikes enemies or the faced material; empty ranged magazines reload instead of firing. |
| `R` | **Campaign:** swap quick weapon; a successful field swap costs one turn |
| `Q` | **Campaign:** use the active quick ability (Dash is a default ability binding) |
| `B` | Arm a bomb |
| `F` | Light a flare; it stuns enemies in its blast |
| `X` | **Campaign:** swap quick ability; a successful field swap costs one turn |
| `I` | Open carried inventory; press `Tab` in Campaign to assign weapon/ability quick slots while the world is paused. `Delete`/`Backspace` or the explicit drop target drops selected cargo at your feet. |
| Mouse drag in inventory | Repack cargo with a live valid/invalid placement preview. In Campaign corpse salvage, drag body parts and cargo from the fallen grid into your inventory grid. |
| `C` | Open the campaign construction menu |
| `U` | **Campaign:** use exactly the faced tile: doors, storage, services, stations, connections, loose cargo, or corpse salvage. Ordinary resource/ammo stacks are collected by walking over them. |
| `Tab` / `W` / `S` / Enter / `R` / `F` in reconstruction | Switch body/inventory focus, select, install or uninstall, rotate cargo, and finish reconstruction |
| `W` / `S` or arrow keys | Select a class, boon, curse, or shop item in a menu |
| Enter or `E` | Confirm a menu choice or leave the shop for the boss |
| `B` / `V` in the shop | Buy / sell the selected item |
| Escape | Quit the game |

## Reference

Name-wise, `Roag` pays homage to [Rogue (1980)](https://en.wikipedia.org/wiki/Rogue_(video_game)). Duh. 

Credits to [Bob Nystrom](https://github.com/munificent)'s [Hauberk](https://github.com/munificent/hauberk) also.

![](./asset/logo/hauberk.png)
