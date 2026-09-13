# Small-vehicle navigation

The vessel chart remains Jomon's long-distance, crew-scale travel system. A
small craft now gives the active courier a separate, steerable local journey.
At the working-deck gangplank, `E` still walks ashore immediately, while `Tab`
opens a shore-or-tug choice. The tug launches
at `J` on a seeded 78×28 water map; `L` marks the shore of Jomon's current
region. The central sounded lane is connected for every seed, while islands
and shoals vary the surrounding route. Arrows or `HJKL` steer the tug up to
three cells per action; `E` at `L` starts the ordinary regional expedition.
Returning through the regional landing puts the courier back aboard the tug
at `L`. Sail to `J` and moor to complete the household return and unload cargo.
An unfinished tug return can be saved and resumed without teleporting home.

Four local vehicles occupy visible map cells. Walk onto one and press `E` to
board; press `E` again on walkable ground to leave it there. Hearthford's horse
cart follows roads and firm floor; Rillscar's steam crawler is another road
vehicle; Greenwold's living rootwalker handles mud, thicket, and shallow
water; Greywash's aether glider can cross water and vegetation but cannot
land where a walking courier cannot stand. Rillscar and its crawler are
generated together on first arrival. These are single map glyphs rather than
multi-cell collision bodies, so they fit the existing camera and encounter
rules. The courier remains vulnerable to ordinary regional hazards and actors.

Each helm order costs one world action and spends finite charge for each cell
actually travelled. Rough or shallow cells cost an extra charge and accumulate
frame wear. Road vehicles stop at water, walls, and actors; diagonal moves
cannot cut a pinched corner. The tug can row at zero coal, and the glider can
trim its wings without aether: one cell per two actions, so either can reach a
landing after its charge is spent. `Tab` zooms into
the vehicle's ASCII interior. There, `R` services the reserve and `F` repairs
the frame. A horse or rootwalker rests for three actions; the glider consumes
two courier mana for twelve charge; tug and crawler use one carried charcoal
lot or buy a counted charge for one credit at a mooring or stand. A repair at
a stand costs one credit and two actions, restoring four frame points. An
anywhere three-action jury-rig restores one point when no paid repair is
available. Invalid or blocked commands spend neither charge nor world time.

Vehicle positions, charge, wear, and the tug's outward/return leg are saved in
Jomon format 11. Format-10 saves gain parked vehicles without changing the
courier's existing location or progress. The vehicle catalogue and interior
art are in `jomon/data/vehicles.json`; traversal and validation live in
`jomon/vehicles.py`. No Dullest Dungeon or tavern-game rules are changed.
