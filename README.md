# `Jomon`

Jomon is a fullscreen, keyboard-driven terminal roguelike about a persistent late-medieval vessel-household. One adult courier at a time leaves the vessel to trade, investigate, negotiate, fight, and return with consequences for the household, regional routes, markets, and relationships.

The game is local and deterministic from its seed and saved state, and uses only the Python standard library.

## Requirements

- Python 3.11 or newer
- An 80-column by 24-row terminal
- Linux or macOS; Windows is supported through WSL

## Usage

From the repository root:

```console
python -m jomon
```

## Verification

```console
python -m jomon.checks fast
python -m unittest discover -s tests -v
python -m compileall -q jomon tests
python -m jomon.verification content
git diff --check
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
| Tab | Choose the tug at Jomon's gangplank or open a vehicle interior |
| `?` / `S` / `Q` / Escape | Help / save aboard Jomon / quit / close or cancel |

## Reference

`Jomon` is thoroughly inspired by [Bob Nystrom](https://github.com/munificent)'s [Hauberk](https://github.com/munificent/hauberk).
