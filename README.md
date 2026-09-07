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
- `Space`: mark or unmark an item; `*`: mark every item in the pane;
- `K`: mark the focused item's category;
- `T`: transfer the focused or marked items transactionally;
- `E`: equip from the pack;
- `1`–`6`: unequip head, torso, arms, hands, legs, or feet when space exists;
- `P`: pin or unpin an item; `O`: auto-pack without moving pinned items;
- `Z`: toggle deterministic auto-placement of new items;
- uppercase `D`, then `Y`: drop focused or marked items physically in the region;
- `[` and `]`: inspect paper-doll slots;
- `C`: confirm the complete repack; and
- `Escape`: restore the inventory exactly as it was when opened.

Where the terminal reports mouse events, left-click selects or places,
right-click rotates a held item, double-click quick-transfers, Shift-click
marks, and the wheel changes paper-doll selection. Mouse use is optional; every
operation has a keyboard path. A lifted item's coloured ghost shows its exact
rotated footprint, blockers, bounds, and resulting load before placement.

Opening and inspecting interfaces costs no time. A confirmed field repack and
accepted in-world actions advance the action clock; idle terminal time never
does.

## Aboard Jomon

Jomon has three aligned 64×22 decks and a dedicated 64×24 common tavern. The
lower deck holds cargo, locker, bilge, provisions, workshop, and berths; the
working deck holds the gangplank, galley, repair position, chronicle, cargo
access, and tavern entrance; the upper deck holds helm, chart, lookout, signal,
and exposed defensive positions. Physical hatches and stairs connect them.

The tavern has a bar, fireplace, serving store, tables, seats, six household
adults, visible visitors, and persistent bartender Sena Quill. Walk beside a
person and interact to inspect their role, technique, injuries, equipment
affinity, memories, and terms. Switching courier or recruiting a willing adult
happens through that person rather than a portrait menu. Named adults move
between actual work and social positions as accepted actions advance the
schedule; off-screen catch-up is bounded and deterministic.

A new world begins with Jomon's bargemaster standing on courier watch at the
gangplank. They already wear a modest working issue, carry a role-appropriate
weapon and secondary item, and have route-survey support, so pressing `E`
starts the first expedition immediately. Every eligible household adult and
new recruit receives their own one-time role-appropriate basic issue. These
are ordinary physical items—they can be changed, damaged, dropped, or lost—and
the tavern, paper doll, and pack remain available whenever deeper preparation
is useful.

Use `I` aboard to move shaped items between the 10×6 courier pack and 18×10
locker, equip one weapon and secondary item, and wear armour at six body
locations. Sena offers eight finite drinks with paired benefits and drawbacks;
buying or drinking costs credit and an action. The physical `P` chart opens a
12-node ASCII network. Arrows, `WASD`, or `HJKL` follow its edges, `Tab` cycles
information layers, `Enter` previews then confirms, mouse click selects when
reported, and `Escape` cancels. Travel costs and risks belong to each edge,
animation may be skipped without changing its result, and a sporadic raider,
river-creature, or original low-mysticism lure can interrupt a leg.

The persistent calendar uses 36 actions per day and 24 days per season.
Spring, summer, autumn, and winter affect daylight, route access, weather,
terrain exposure, schedules, and bar stock. Equinoxes and solstices are visible
in Jomon's bounded chronicle. Menus and real-world idle time never advance it.

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
5 deterministically migrates Python format-4 saves while preserving people,
regions, exploration, exact item layouts, cargo, contacts, markets, integrity,
and voyage history. The retained format-3 path chains through its prior safe
migration. Older room-graph and retired browser saves are rejected.

```console
python -m unittest discover -s tests -v
python -m compileall -q jomon tests
python -m jomon.audit
python -m jomon.living_audit
git diff --check
```

The audit samples 100 seeds across all three added regions and every pressure
band. See [`LORE.md`](LORE.md), [`PRODUCT.md`](PRODUCT.md), [`TODO.md`](TODO.md),
the [causal loop note](docs/causal-generation-and-loop.md), the
[four-region assessment](docs/regions-inventory-encounters-milestone.md), and
the [living-vessel assessment](docs/diegetic-vessel-seasonal-world-milestone.md).
The retired browser version remains recoverable from local branch
`archive/web-v19` and annotated tag `jomon-web-v19-final`, both targeting
`de1c1e8`.
