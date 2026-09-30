[![](https://img.shields.io/badge/roag_1.0.0-passing-green)](https://github.com/gongahkia/roag/releases/tag/1.0)

# `Roag` 🗡️

A [persistent](https://cavesofqud.com/) roguelike where you're a [late-medival mailman](https://en.wikipedia.org/wiki/Jōmon_people).

## Stack

* [Lua](https://www.lua.org/)
* [LÖVE2d](https://love2d.org/)

## Screenshots

![](./asset/reference/1.png)
![](./asset/reference/2.png)
![](./asset/reference/3.png)
![](./asset/reference/4.png)

## Usage

> [!NOTE]  
> `Roag` minimally requires a 80-column by 24-row terminal.

The below instructions are for locally running `Roag`.

1. First clone the repository.

```console
$ git clone https://github.com/gongahkia/roag && cd roag
```

2. Then run the below from repo root.

```console
$ love .
```

## Controls

| Key | Action |
| --- | --- |
| `W` / `A` / `S` / `D` | Move; hold one vertical and one horizontal key together to move diagonally |
| `E` | Fire in the facing direction |
| `Q` | Dash two tiles in the facing direction |
| `B` | Arm a bomb |
| `F` | Light a flare; it stuns enemies in its blast |
| `G` | Salvage a nearby corpse |
| `I` | Open carried inventory |
| `X` | Open installed body abilities; activation requires a second confirmation |
| `Tab` / `W` / `S` / Enter / `R` / `F` in reconstruction | Switch body/inventory focus, select, install or uninstall, rotate cargo, and finish reconstruction |
| `W` / `S` or arrow keys | Select a class, boon, curse, or shop item in a menu |
| Enter or `E` | Confirm a menu choice or leave the shop for the boss |
| `B` / `V` in the shop | Buy / sell the selected item |
| `P` on the title screen | Open Sprite Lab; preview and live-assign Kenney tiles, then copy the shown coordinates into `sprite_map.lua` to keep them |
| Escape | Quit the game |

## Reference

Name-wise, `Roag` pays homage to [Rogue (1980)](https://en.wikipedia.org/wiki/Rogue_(video_game)). Duh. 

In its gameplay, `Roag` takes heavy inspiration from [Bob Nystrom](https://github.com/munificent)'s [Hauberk](https://github.com/munificent/hauberk).

![](./asset/logo/hauberk.png)
