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
| arrows or `HJKL`; `YUBN` | Move cardinally or diagonally |
| `E` or Enter | Interact with the nearby world |
| `A` / `G` | Attack or choose a target / guard, brace, reload, or listen |
| `T` / `R` | Follow a known regional route / retreat |
| `I` / `F` / `X` | Open the pack / inspect materials / use a preparation or relic |
| `V` / `M` | Offer terms / open combat mastery |
| `P` / `D` / `W` | Skill tree / spellbook and shrine vigil / crafting and flasks |
| `\\` / `Z` | Circuit work / regional ledger |
| `C` / `O` / `;` | Character sheet / observe visible life / inspect without spending time |
| Tab | Choose the tug at Roag's gangplank or open a vehicle interior |
| `?` / `S` / `Q` / Escape | Help / save aboard Roag / quit / close or cancel |

## Reference

Name-wise, `Roag` pays homage to [Rogue (1980)](https://en.wikipedia.org/wiki/Rogue_(video_game)). Duh. 

In its gameplay, `Roag` takes heavy inspiration from [Bob Nystrom](https://github.com/munificent)'s [Hauberk](https://github.com/munificent/hauberk).

![](./asset/logo/hauberk.png)
