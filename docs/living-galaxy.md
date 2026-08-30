# Jomon living galaxy

Jomon worlds are persistent chronicles, not finite campaigns. A world begins with the Jomon Voyager and one reachable landing. There is no terminal victory condition: a courier can die, retire, or be replaced while the same galaxy retains its people, territory, structures, routes, and history.

## World model

The seed creates an origin cluster plus twenty outer sector clusters. Each sector has ten site slots, giving 210 stable site identities. Existing biome generators are site archetypes; sector name, faction control, ecological pressure, integrity, construction, and route position make each placement distinct. The whole graph exists from world creation, while only discovered sites are available to the player.

Sites connect through physical route records. The bridge chart is an intelligence display, not a teleport menu: only the current landing and its discovered neighbors can be selected. Selecting a neighbor materializes a deterministic one-to-five-partition airlock route; its partitions are stitched into one camera-continuous walkable space, with three as the usual length. A surveyed landing reveals adjacent approaches. Route state is saved as a normal run, so leaving and returning does not replace the route with a timed cinematic.

Only active and visited sites retain full floor snapshots. Distant sites retain lightweight simulation state and materialize their local maps when reached. This keeps save sizes and memory proportional to play while preserving a fixed galaxy topology.

## Simulation clock and history

One sector day passes per real hour. The world resolves a major simulation turn every six sector hours. On load, elapsed wall-clock time is reconciled for at most seven real days; the cap stops an abandoned save from resolving an unbounded number of destructive events in one step.

Major changes create a Voyager chronicle event. Site integrity, ecology, control, and construction can change; named couriers can change affinity, become injured, or die while on autonomous work. The chronicle is the authoritative explanation for an off-screen change. News and dialogue may summarize an event, but arriving at a site remains the primary way to understand local consequences.

Event resolution must be seeded by world seed, simulation tick, and entity ID. Content may add outcomes, but must not use non-deterministic randomness or mutate a site without emitting a matching chronicle event.

## People, factions, and equipment

New Voyagers begin with five playable specialists. The active specialist is selected at the carrier; inactive specialists retain a role, routine, affinity, status, location, and personal gear. Routines are autonomous by default and later content may expose player-set preferences as biases, never as a guarantee. Death is permanent. If no specialist remains, recruitment from settlements and rescue contacts continues the same world.

General equipment belongs in the carrier stash; signature gear and keepsakes belong to the courier. Any authored item that is not explicitly personal should be transferable at the Voyager. Factions own territory and influence; rivals, crews, commerce, settlement construction, and ecological changes should use the shared galaxy state instead of storing private copies of control or relationship data.

## Content contract

New content adds one or more of the following: a site archetype, a faction action, a courier routine outcome, a construction project, an ecology transition, a route/gate condition, a combat encounter context, or a chronicle template. Every addition must declare its affected stable IDs, simulation cadence, reversible/irreversible effects, and player-visible evidence.

Combat remains tactical and local. Its persistent inputs are site control, ecology, integrity, active courier traits, rival identity, and construction. Current site conditions alter hostile health, attack, defense, and landing terrain: Voidborn control increases combat pressure, poor ecology produces hazardous spores, and low integrity produces collapse debris. Completing a landing pays a control-sensitive salvage yield and improves that site's supplies, integrity, ecology, and construction, while recording the outcome in the chronicle.

Long routes retain a three-partition resident window and shift it as the courier crosses internal seams. It has persistent route situations (quiet, patrol, hazard, trader, or ecology) and every surveyed link produces a contract lead. Market stock and prices evolve with the sector clock; inactive specialists on the trade routine can improve market availability without changing player cargo.

Every landing floor carries physical airlocks for charted neighbors and the Voyager. Operating an airlock loads an eligible contract only when hold capacity allows, then starts the matching route; arrival delivers active cargo and pays its fee. A courier's death on a route leaves contract cargo in a marked recoverable cache. Another courier must walk to and operate that cache to retrieve its cargo; failed contracts remain failed.
