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
- `A`: attack with the readied weapon; ranged weapons open a target cursor;
- `G`: guard, brace, or continue a weapon reload;
- `X`: use a finite readied tool, supply, or relic;
- `V`: offer material terms;
- `R`: retreat when a physical route remains;
- `I`: open the spatial pack and nearby source;
- `F`: inspect and handle nearby materials, with finite supplies and previews;
- `Z`: read the regional working ledger, evidence and forecast;
- `O`: inspect presently visible actors, duties, resources and counterplay;
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

The lower `W` workshop at (39,5,-1) offers optional counted fittings and
equipment repair. `E` opens it; `1`–`8` choose worn/readied equipment, `P`
opens loose kits, lettered choices preview work and `F` confirms. Escape
backs out without spending anything. Weapons accept one structural fitting
and one treatment; armour accepts one lining. Fitted parts remain physical,
add weight, wear independently, and follow the parent through loss or theft.
Removal costs one credit and needs pack room. Repair costs two credit for
35 condition; fitting or removing takes two action-clock steps. See
[workshop trade-offs](docs/workshop.md) for the eight finite kit types.

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
16-node ASCII network. Arrows, `WASD`, or `HJKL` follow its edges, `Tab` cycles
information layers, `Enter` previews then confirms, mouse click selects when
reported, and `Escape` cancels. Travel costs and risks belong to each edge,
animation may be skipped without changing its result. Twelve bounded voyage
families can interrupt a leg. Eight offer `P` to take the physical decks:
raiders, boarders, hold thieves, a territorial grazer, a failing stay, galley
fire, a split seam, and bilge flooding. Ordinary combat, aimed lanes, hatches,
materials and physical dropped possessions apply there; `R` abandons the
claim with disclosed cargo/hull losses. The other four involve sounding a
shoal, retrieving drifting timber, cargo inspection, or a rare answering-hull
lure. See [counted vessel work and crises](docs/voyage-crises.md).

The persistent calendar uses 36 actions per day and 24 days per season.
Spring, summer, autumn, and winter affect daylight, route access, weather,
terrain exposure, schedules, and bar stock. Equinoxes and solstices are visible
in Jomon's bounded chronicle. Menus and real-world idle time never advance it.

## Eight persistent regional destinations

- **Hearthford Low Wood** is a 96×54 river settlement, floodplain, ruin,
  culvert, watch roof, and multi-level millworks.
- **Greywash Tidal Reach** is a 104×56 salt village, draining flat, dune road,
  wreck route, sea cave, signal mast, and tide-chain house.
- **Greenwold Charcoal March** is a 100×58 open woodland of connected
  clearings, resin work, root cellar, burnworks, watch tree, and canopy route.
- **Whitecairn Limestone Rise** is a 98×60 terrace and switchback landscape
  with quarry, limehouse, sink cave, ridge bridge, and bell tower.
- **Dunmire Peat Isles**, 96×56, connects bog islands, raised fuel racks,
  drying banks and buried drains with a causeway circuit.
- **Rillscar Iron Gorge**, 112×52, uses folded river cuts, two bridges,
  industrial spans and a connecting underground drain.
- **Marlbank Clay Terraces**, 104×60, follows irrigation bands through grain
  fields, a potters' court, kilnworks and buried water routes.
- **Frostmere Braided Estuary**, 108×58, follows three channels through gravel
  islands, a net settlement, winter loft and sheltered crossings.

The latter four are generated on first visit. The systemic expansion is still
in progress; its acceptance and play evidence are recorded in the
[live assessment](docs/systemic-world-milestone.md).

Each uses aligned underground, ground, upper, and roof levels. The camera
follows the courier. Current line-of-sight is rendered normally, remembered
terrain is dim, and moving actors never remain in exploration memory. Terrain,
doors, elevation, smoke, weather, interiors, and caves change sight. Tides,
burn wind, quarry instability, patrol travel, and Hearthford flooding change
only after accepted actions.

Closed containers use `C`; depleted containers use `o`. Hearthford has nine;
Greywash, Greenwold and Whitecairn have seven each; each frontier has eight,
for 62 persistent containers when all destinations have been visited. Contact
clues and working records can mark optional treasure in exploration memory.
Pack shape and weight are independent constraints. Light, laden, encumbered, and
overloaded states visibly affect noise, pacing, climbing, water, retreat, and
fragile footing.

Eighteen weapon families provide different actions, including reach control,
pulling, sweep guard, knockback, destruction, aiming, reload commitments,
height-sensitive sling casts, finite throws, net restraint, arcing staff-sling
shots, recoverable hooked javelins, animal-bracing boar spears, and a loud
two-step handgonne. Ranged targeting shows the chosen actor, exact projectile
path, effective range, cover, and physical ammunition before `Enter` commits;
`Escape` cancels without time passing. Enemy plans use limited sight, sound,
last-known positions, morale, ammunition, allies, terrain, and assigned goals.
Ranged attacks telegraph their lane before a severe shot.

Each region has a direct three-stage material quest with two persistent
endings, an optional task, a named cache lead, a changed local threat duty, and
a secondary-contact service. Refusing the opening request changes the later
choice instead of abandoning the line. Completing any two regional lines opens
the five-part Working Marks compact across Greywash, Greenwold, Whitecairn,
and Hearthford. Its open-compact, Jomon-surety, and local-marks endings trade
route safety, household credit, and local authority rather than presenting one
unqualified outcome.

## Saves and verification

One atomic JSON save is stored at `$XDG_DATA_HOME/jomon/jomon-save.json`, or
`~/.local/share/jomon/jomon-save.json` when `XDG_DATA_HOME` is unset. Set
`JOMON_DATA_DIR` to override the directory for development or tests. Save format
7 deterministically migrates Python format-6 saves while preserving people,
regions, exploration, exact item layouts, cargo, contacts, markets, integrity,
and voyage history. New sparse material fields and working-history records do
not repaint migrated geography or reissue lost equipment. The retained
format-3, format-4 and format-5 paths chain through their
prior migrations. Physical pack items are authoritative for finite ammunition
and bottled drinks. Older room-graph and retired browser saves are rejected.

```console
python -m unittest discover -s tests -v
python -m compileall -q jomon tests
python -m jomon.audit
python -m jomon.living_audit
python -m jomon.systemic_audit --seeds 1000
git diff --check
```

The older encounter audit samples 100 seeds across the three original added
regions and every pressure band. The slow systemic audit constructs all eight
destinations, checks production placements, history references, seasonal chart
return routes, deterministic regeneration and exact save round trips. It emits
JSON and explicitly lists checks it does not perform; `--start` supports
reproducible batches. See [`LORE.md`](LORE.md), [`PRODUCT.md`](PRODUCT.md), [`TODO.md`](TODO.md),
the [causal loop note](docs/causal-generation-and-loop.md), the
[four-region assessment](docs/regions-inventory-encounters-milestone.md), and
the [regional-quest assessment](docs/regional-questlines-tactical-content-milestone.md).
The retired browser version remains recoverable from local branch
`archive/web-v19` and annotated tag `jomon-web-v19-final`, both targeting
`de1c1e8`.
