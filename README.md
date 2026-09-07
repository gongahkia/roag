# Jomon

Jomon is a fullscreen, keyboard-driven terminal roguelike about a persistent
late-medieval vessel-household. One adult courier at a time leaves the vessel
to trade, investigate, negotiate, fight, and bring material consequences home.
People, injuries, deaths, equipment, opened stores, relationships, routes, and
regional markets persist.

The game is local, offline, deterministic from its readable seed and recorded
state, and uses only the Python standard library. Python 3.11 or newer is
required. Linux and macOS terminals are supported; Windows is supported through
WSL.

## Run

From the repository root:

```console
python -m jomon
```

The terminal must be at least 80 columns by 24 rows. Jomon shows a resize
message below that size and uses `curses.wrapper()` to restore the terminal on
normal exit and exceptions.

## Controls

World controls:

- arrows or `HJKL`: cardinal movement;
- `YUBN`: diagonal movement;
- `Enter` or `E`: interact, climb, open, speak, or operate;
- `A`: attack with the readied weapon;
- `G`: guard, brace, or continue a weapon reload;
- `X`: use a finite readied tool, supply, or relic;
- `V`: offer material terms;
- `R`: retreat when a physical route remains;
- `I`: open the spatial pack and nearby source;
- `?`: help;
- `S`: save while aboard Jomon;
- `Q`: quit with confirmation; and
- `Escape`: close or cancel an overlay.

Spatial-inventory controls:

- arrows or `WASD`: move the cell cursor;
- `Enter`: lift or place an item;
- `R`: rotate the held item 90 degrees;
- `Tab`: switch between pack and locker, ground, or container;
- `T`: transfer the selected item;
- `E`: equip from the pack;
- `1`–`6`: unequip head, torso, arms, hands, legs, or feet when space exists;
- uppercase `D`: drop an item physically in the region;
- `C`: confirm the complete repack; and
- `Escape`: restore the inventory exactly as it was when opened.

Opening and inspecting interfaces costs no time. A confirmed field repack and
accepted in-world actions advance the action clock; idle terminal time never
does.

## Aboard Jomon

Jomon remains a compact walkable vessel. Its tavern has a physical bar, tables,
six household adults, and visible visitors. Walk beside a person and interact
to inspect their role, technique, injuries, equipment affinity, memories, and
terms. Switching courier or recruiting a willing adult happens through that
person rather than a portrait menu. Jomon has nine adult berths.

Use `I` aboard to move shaped items between the 10×6 courier pack and 18×10
locker, equip one weapon and secondary item, and wear armour at six body
locations. The tavern `C` selects one of five crew preparations. The chart `P`
selects a region, and the physical `+` gangplank begins or ends an expedition.
Regional travel costs six world measures and may produce a sporadic raider,
river-creature, or original low-mysticism lure event.

## Four persistent regions

- **Hearthford Low Wood** is a 96×54 river settlement, floodplain, ruin,
  culvert, watch roof, and multi-level millworks.
- **Greywash Tidal Reach** is a 104×56 salt village, draining flat, dune road,
  wreck route, sea cave, signal mast, and tide-chain house.
- **Greenwold Charcoal March** is a 100×58 open woodland of connected
  clearings, resin work, root cellar, burnworks, watch tree, and canopy route.
- **Whitecairn Limestone Rise** is a 98×60 terrace and switchback landscape
  with quarry, limehouse, sink cave, ridge bridge, and bell tower.

Each uses aligned underground, ground, upper, and roof levels. The camera
follows the courier. Current line-of-sight is rendered normally, remembered
terrain is dim, and moving actors never remain in exploration memory. Terrain,
doors, elevation, smoke, weather, interiors, and caves change sight. Tides,
burn wind, quarry instability, patrol travel, and Hearthford flooding change
only after accepted actions.

Closed containers use `C`; depleted containers use `o`. Each new region has six
named containers with a build discovery, an armour item, and a supply. Pack
shape and weight are independent constraints. Light, laden, encumbered, and
overloaded states visibly affect noise, pacing, climbing, water, retreat, and
fragile footing.

Fourteen weapon families provide different actions, including reach control,
pulling, sweep guard, knockback, destruction, aiming, reload commitments,
height-sensitive sling casts, finite throws, and net restraint. Enemy plans use
limited sight, sound, last-known positions, morale, ammunition, allies, terrain,
and assigned goals. Ranged attacks telegraph their lane before a severe shot.

## Saves and verification

One atomic JSON save is stored at `$XDG_DATA_HOME/jomon/jomon-save.json`, or
`~/.local/share/jomon/jomon-save.json` when `XDG_DATA_HOME` is unset. Set
`JOMON_DATA_DIR` to override the directory for development or tests. Save format
4 deterministically migrates Python format-3 saves, placing overflow in Jomon's
bounded locker without silently deleting possessions. Older room-graph and
retired browser saves are rejected.

```console
python -m unittest discover -s tests -v
python -m compileall -q jomon tests
python -m jomon.audit
git diff --check
```

The audit samples 100 seeds across all three added regions and every pressure
band. See [`LORE.md`](LORE.md), [`PRODUCT.md`](PRODUCT.md), [`TODO.md`](TODO.md),
the [causal loop note](docs/causal-generation-and-loop.md), and the
[four-region milestone assessment](docs/regions-inventory-encounters-milestone.md).
The retired browser version remains recoverable from local branch
`archive/web-v19` and annotated tag `jomon-web-v19-final`, both targeting
`de1c1e8`.
