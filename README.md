# Jomon

Jomon is a fullscreen, keyboard-driven terminal roguelike about sending one
member of a persistent late-medieval vessel-household into Hearthford and its
dangerous river edge. Couriers trade, negotiate, evade, fight, return hurt, or
die; cargo, relationships, routes, markets, and succession persist.

The game is local, offline, deterministic from its readable seed and saved
state, and uses only Python's standard library. Linux and macOS terminals are
supported; Windows is supported through WSL. Python 3.11 or newer is required.

## Run

From the repository root:

```console
python -m jomon
```

The terminal must be at least 80 columns by 24 rows. Jomon safely shows a
resize message below that size and uses `curses.wrapper()` to restore the
terminal on normal exit and exceptions.

## Controls

- arrows or `HJKL`: cardinal movement
- `YUBN`: diagonal movement
- `Enter` or `E`: interact
- `A`: attack
- `G`: guard or reposition against readable hostile intent
- `X`: use finite readied gear
- `V`: negotiate when the courier has credible terms
- `R`: retreat when a route remains available
- `I`: inventory and cargo
- `?`: help
- `S`: save while aboard Jomon
- `Q`: quit with confirmation
- `Escape`: close or cancel an overlay

Keys are case-insensitive where appropriate. Movement and accepted in-world
actions advance time. Inspection, help, blocked movement, and cancelled choices
do not.

## Playable loop

Use the tavern `C` to choose an eligible courier, weapon, secondary item,
crew support, available finite relic, and up to the passive-bulk limit in one
zero-time preparation menu. The former `L` and `P` stations are optional
readouts. Inspect the hold at `H`, then leave through the `+` gangplank.

Hearthford is one continuous 96x54 landscape with four spatially aligned
levels: buried culvert, ground, upper works, and roofs. A camera follows the
courier across the settlement, riverbank, roads, Reedwood loop, old watch,
ruin, cave, and millworks. Current line-of-sight is bright, previously seen
terrain is dim, and unknown terrain and out-of-sight actors remain hidden.
Stairs and ladders use the same world coordinates on adjacent levels.

Meet the named contact at `M`; accept, refuse, or materially alter the request.
Explore optional routes and persistent containers for practical discoveries,
control floodwater and furnace smoke across levels, break a marked floor, and
observe or distract the river-road patrol. Human, animal, mixed, ranged, and
operational threats support direct combat and situated non-combat resolutions.
Recover objective cargo at `R`, report to the contact, and physically travel
back to Jomon's gangplank. Weather, a material deadline, finite supplies, and
valuable optional treasure make route length consequential without real-time
timers.

Returned equipment and eligible discoveries persist. After some completed
expeditions, a deterministic visiting merchant appears at `$` aboard Jomon
with three condition- and outcome-derived lots. Purchases use bounded trade
credit and remain available for later preparations.

The four visible pressure contributors—elapsed time, depth, noise, and carried
valuables—change alert distance, pursuit speed, crossing risk, material loss,
and the delivery's market timing.

## Saves and tests

One atomic JSON save is stored at `$XDG_DATA_HOME/jomon/jomon-save.json`, or
`~/.local/share/jomon/jomon-save.json` when `XDG_DATA_HOME` is unset. Set
`JOMON_DATA_DIR` to override the directory for development or tests. The
current development format is version 3. Version 2 room-graph saves cannot be
mapped reliably into the seamless geography and are rejected with a clear
startup notice; no migration chain exists.

```console
python -m unittest discover -s tests -v
python -m compileall -q jomon tests
git diff --check
```

See [`LORE.md`](LORE.md), [`PRODUCT.md`](PRODUCT.md), [`TODO.md`](TODO.md), and
the [causal loop note](docs/causal-generation-and-loop.md). The retired browser
version is recoverable from local branch `archive/web-v19` and annotated tag
`jomon-web-v19-final`, targeting archived commit `de1c1e8`; retained historical
notes are under [`docs/archive/`](docs/archive/).
