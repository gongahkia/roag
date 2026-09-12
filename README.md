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

Dullest Dungeon is played from the marked seat at a table in Jomon's tavern.
It is an in-world office-fantasy card game against named tavern patrons; the
original generated-map expedition now hosts one competitive file-capture mode.
The imported Dullest Dungeon package launches Jomon rather than a separate
campaign. See [the game and lore guide](docs/dullest-dungeon.md).

The terminal must be at least 80 columns by 24 rows. Jomon shows a resize
message below that size and uses `curses.wrapper()` to restore the terminal on
normal exit and exceptions.

## Controls

World controls:

- arrows or `HJKL`: cardinal movement;
- `YUBN`: diagonal movement;
- `T`: follow a seen local landmark, link, or marked store over remembered ground;
- `Enter` or `E`: interact, climb, open, speak, or operate;
- `A`: open the zero-time combat preview and target cursor for any readied
  weapon; `Tab` cycles only legal targets and `Enter` commits the chosen action;
- `G`: guard, brace, continue a weapon reload, or hold position and listen;
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

At the tavern's `D` seat, `Enter` or `E` opens Dullest Dungeon. In its lobby,
`J/K` chooses a patron, `1`–`4` selects an office worker, `[` and `]` change
jobs, `P` changes company policy, `D` edits the deck, and `Enter` starts a
match. On the generated map, arrows or `W/A/D/X` move the cursor, `Tab` cycles
through neutral sites, `1` aims at your home, `2` at the rival file, `3` at
the patron party, and `4` at your own file. `Enter` auto-walks toward the
cursor for the remaining weighted 18-tick route orders, stopping at a choice
or rival contact; each party has four route orders per turn.
Contact opens ranked 4v4 card combat. There, arrows or `H/L` select a hand
card, `Enter` chooses a legal target and plays it, `C` inspects the full card,
and `R` inspects both rosters. `E` ends a map or combat turn, `S` saves Jomon
and the match, and `Q` returns to Jomon with the match still in progress.
Starting a match advances one Jomon action; expedition turns do not advance
Jomon time.

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

## Colour and accessibility

Jomon uses the terminal's reported capabilities rather than assuming one fixed
palette. A 256-colour terminal receives 46 distinct foregrounds across 57
semantic roles; a 16-colour terminal receives 14; an 8-colour terminal retains
the original seven-colour scheme; and a terminal with no colour support uses
bold, underline, reverse video, glyphs, and explicit labels alone. Pair
allocation never exceeds the terminal's reported capacity, and Jomon keeps the
terminal's default background.

The eight regions have separate ground colours, while vegetation, roads,
timber, stone, mud, shallow and deep water, ice, fire, smoke, collapse, and
material coatings have consistent physical colours. Actors, item families,
frames, status groups, recent outcomes, target previews, and factual, rumoured,
forecast, remembered, or warned information also have distinct accents. Colour
is never the only cue: actor glyphs and intent, terrain glyphs, material
inspection, textual provenance prefixes, choice markers, and warning labels
remain authoritative.

Known-route following is an input convenience, not a separate travel system:
each tile uses the ordinary movement or climb reducer and advances the same
action clock as manual play. It only crosses terrain already remembered by the
current region. Newly visible danger, an alert, an off-route sound, weather,
injury, load-state change, or a visible material hazard interrupts it, and any
key cancels before another step.

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
schedule; off-screen catch-up is bounded and deterministic. After two returned
expeditions that particular adult—not a global crew level—earns a visible
seasoned-role practice with extra carrying capacity and a reinforced guard.

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

The latter four are generated on first visit. The systemic expansion's exact
acceptance and play evidence are recorded in the
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

Thirty-six protective garments cover the six body locations. Frontier work
clothing includes floating cork coats, kiln aprons, limeworker sleeves,
winter padding, peat pattens and ice cleats. Protection is local, condition
matters, and weather can make absorbent clothing heavier. A visor narrows
sight; a kiln apron wears while taking heat; a floating coat stops helping
against current when its bearer carries too much. These are optional finds,
not changes to the ready-to-depart starting issue. Fresh frontier stores and
counted visiting merchant stock supply them; existing saved chests are not
refilled. See [working clothing](docs/working-clothing.md).

Fresh frontier maps also contain one of two [working elite claims](docs/frontier-elites.md).
Critical pressure wakes the local situation. Reverse `!` marks warn of material
actions; `O` shows observed charges and intent. Water, bracing, separation and
regional controls offer alternatives to direct strikes. Surviving named
claimants can return once with local supplies, or accept a witnessed settlement
through the secondary contact. Their rewards remain physical ground items.

Combat targeting includes visible actors across open z-levels. `Tab` cycles
legal attacks and reach-weapon reactions; `<` and `>` inspect another level,
while the arrow/WASD/HJKL cursor
moves within that level. The projected marker and `[ABOVE]`/`[BELOW]` label
identify off-level targets. Range, cover, physical ammunition and preparation
have separate rows at 80×24. `G` can brace a spear, pike, glaive or staff on the
selected engaged actor: the reaction fires only if that actor enters or attacks
through valid measure during the guarded action, otherwise it visibly expires.
Fog, smoke and intact floors restrict both target inspection and actual shots;
Escape costs no action.

Thirty-six weapon families provide different actions, including reach control,
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
a secondary-contact service. Four original regions add a second physical
undertaking, giving 12 regional lines. Refusing the opening request changes the
later choice instead of abandoning the line. A later physical revisit adds one
ending-specific aftermath line per region: two finite contracts, 16 distinct
objective topologies, and supply or field answers bring the total to 20. Five
cross-region arcs compare working marks, flood banks, winter soundings, repaired
scars and low-water refuges through physical records. Their eleven endings
trade route safety, household credit, market pressure, institutional
obligations and local authority rather than presenting one unqualified outcome.

Twelve institutions now connect that local work: the eight regional accounts
plus Common Bank Measures, Wreck and Span Witnesses, Burn Shelter Runners and
Cold Road Sounders. Each travelling interest has two named, physically
scheduled adult witnesses. A real dependency lot earns network trust; trust
can open one persistent shelter mark that reduces cargo or weather exposure on
connected routes while recording an obligation. Their services, opposition,
relationships and witnessed acts appear in the regional ledger and contact UI.

Thirty-two mechanically active techniques now include 16 reciprocal learned
practices. Trusted travelling witnesses teach eight; completing both finite
aftermath contracts in a region teaches eight more. They alter movement,
material work, sight, salvage, elevation, guard, climbing and recoverable
equipment through the same production reducers used by ordinary play.

Sixteen additional one-use field preparations are physical rewards from the
16 aftermath contracts. `X` opens a contextual readiness list when any are
packed: invalid choices are zero-time, while committed use consumes the named
item and alters water, fire, smoke, structures, equipment, footing, hostile
intent, recovery, cold, or fatigue through shared reducers.

Sixteen finite relics include four rewards determined by the endings of the
two aftermath arcs. These rarer tools deliberately break a narrow rule—joint
equipment/support repair, reciprocal physical disarmament, temporary storm
shelter, or a cargo-costed water-lane crossing—and always expose a material
drawback before use.

Jomon's working decks also accept eight optional physical refits. Inspect `V`
at the galley, bilge, repair, storage, helm, berth or lookout station to preview
the exact cargo lot, credit, three-action fitting cost, benefit and drawback.
Installed fittings change the ordinary shared systems: fire and meals,
floodwater and pumping, storm work, cargo theft, shoals, winter exposure,
signals and persistent injury treatment. None is required before departure.

Every voyage family can now take one causally stronger form. Current shortage,
cargo, obligation, aftermath, route memory, fire history, season, weather,
wear, fitted signals or a prior outcome can change actors, sparse hazards,
costs, cargo selection, rewards or settlement terms. The voyage panel labels
the cause, changed rule and counters; exact variant outcomes persist by voyage
number.

## Saves and verification

One atomic JSON save is stored at `$XDG_DATA_HOME/jomon/jomon-save.json`, or
`~/.local/share/jomon/jomon-save.json` when `XDG_DATA_HOME` is unset. Set
`JOMON_DATA_DIR` to override the directory for development or tests. Save format
7 deterministically migrates Python format-6 saves while preserving people,
regions, exploration, exact item layouts, cargo, contacts, markets, integrity,
and voyage history. New sparse material fields, travelling accounts and
regional witnesses do
not repaint migrated geography or reissue lost equipment. The retained
format-3, format-4 and format-5 paths chain through their
prior migrations. Physical pack items are authoritative for finite ammunition
and bottled drinks. Older room-graph and retired browser saves are rejected.

```console
python -m jomon.checks fast
python -m unittest discover -s tests -v
python -m compileall -q jomon tests
python -m jomon.audit
python -m jomon.living_audit
python -m jomon.systemic_audit --seeds 1000
python -m jomon.verification content
python -m jomon.verification encounter --samples 100
python -m jomon.verification generation --samples 1000
python -m jomon.verification quest --samples 25
python -m jomon.verification persistence
python -m jomon.verification replay --samples 100
python -m jomon.verification living --samples 100
python -m jomon.verification soak --samples 80
python -m jomon.benchmark --samples 20
git diff --check
```

The focused commands emit JSON and fail nonzero when a represented invariant
breaks. The slow systemic audit constructs all eight
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
