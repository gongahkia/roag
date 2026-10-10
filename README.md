[![](https://img.shields.io/badge/roag_1.0.0-passing-light_green)](https://github.com/gongahkia/roag/releases/tag/1.0.0)

# `Roag` 🛖

A [persistent](https://cavesofqud.com/) roguelike where you're a [late-medival mailman](https://en.wikipedia.org/wiki/Jōmon_people), played entirely in the [CLI](https://dwarffortresswiki.org/index.php/Command_line).

Start a combat run by choosing Breaker, Marksman, Trickster, or Sapper. Each
has a fixed three-action kit and pursues a five-claimant route ending in the
final boss. Pick up stackable discoveries from XP choices, chests, elites, and
bosses to change those actions without adding more active buttons.

## Stack

* [Python 3.11](https://www.python.org/) *(but newer is fine)*
* [curses](https://docs.python.org/3/library/curses.html)

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
$ python -m roag
```

3. Optionally run the below commands for verification.

```console
$ python -m roag.checks fast
$ python -m unittest discover -s tests -v
$ python -m compileall -q roag tests
$ python -m roag.verification content
$ git diff --check
```

## Controls

| Key | Action |
| --- | --- |
| arrows or `HJKL`; `YUBN` | Move cardinally or diagonally |
| `E` or Enter | Interact with the nearby world |
| `A` / `G` | Attack or choose a target / guard, brace, reload, or listen |
| `B` / `X` | During a combat run: class movement / signature ability; otherwise use preparation or relic controls |
| `T` / `R` | Follow a known regional route / retreat |
| `I` / `F` | During a combat run: inspect current build / inspect materials; otherwise open the pack / inspect materials |
| `V` / `M` | Offer terms / open combat mastery |
| `P` / `D` / `W` | Skill tree / spellbook and shrine vigil / crafting and flasks |
| `\\` / `Z` | Circuit work / regional ledger |
| `C` / `O` / `;` | Character sheet / observe visible life / inspect without spending time |
| Tab | Choose the tug at Roag's gangplank or open a vehicle interior |
| `?` / `S` / `Q` / Escape | Help / save aboard Roag / quit / close or cancel |

## Reference

`Roag` is thoroughly inspired by [Bob Nystrom](https://github.com/munificent)'s [Hauberk](https://github.com/munificent/hauberk).

![](./asset/logo/hauberk.png)
