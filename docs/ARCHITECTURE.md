# Regolith architecture

The game uses C#/.NET 10 with MonoGame DesktopGL 3.8.6. The simulation has no MonoGame dependency; the native client submits commands and renders simulation state. The original Python project is archived under `legacy/python` and is not imported or launched by the new game.

## Projects and ownership

`Regolith.Core` owns `World`, `Simulation`, colonists, machines, work orders, contacts, expeditions, the seeded random stream, and persistence. `Regolith.Client` owns graphics resources, input, UI state, and the clock accumulator. Shared `Regolith.Graphics` now contains `SpriteLibrary` and `MapCamera`, used by both the colony and RPG clients. `Regolith.Tools` references only Core and runs without SDL/OpenGL. Tests reference only Core, so environmental and progression rules can be checked without opening a window.

`Lantern.Core` contains independent fantasy RPG rules; it does not reference colony simulation types. `Lantern.Client` uses the shared renderer utilities and its own manifest, map presentation, party interface, and atlas viewer. Texture loading is lazy for unbound atlas entries, so browsing all 7,115 registered sprites does not eagerly allocate 664 image pairs. See [the RPG guide](LANTERN.md) and [catalog schema](../assets/reference/CATALOG_V2.md).

There is currently one simulation thread. The client advances fixed logical ticks at 8 ticks/second, or 3×/8× that rate, and interpolates crew positions between updates. A frame limits how long it starts further ticks and caps accumulated backlog; under sustained load the simulation slows relative to real time. An individual tick is synchronous and may exceed that budget. Moving simulation to a worker would require commands and immutable/double-buffered render snapshots; directly sharing these mutable collections across threads would be unsafe.

## Cells and cellular updates

Cells use row-major integer indices, `y * width + x`. Terrain and visibility are byte arrays. Oxygen, heat, and fire are float arrays with reusable next-state buffers. World changes increment a topology version. The cached active-floor list and connected power graph rebuild when topology changes, instead of rebuilding each tick. Topology changes from many completed jobs are coalesced before the final power refresh.

Geology uses a seeded five-pass, eight-neighbor majority automaton followed by resource veins and an accessible starting site. Atmospheric fields use four-neighbor pairwise exchange from the old state into next-state buffers. Sealed floor-to-floor oxygen transfers are equal and opposite. Exterior vacuum is a fixed zero-oxygen, ambient-temperature boundary: leaked gas is discarded, rather than simulated across the entire wasteland. Hull and rock block exchange. Airlocks abstract a cycling pressure seal and block gas while passing people and power.

Only floor cells participate in atmosphere/fire updates. This improves the common case of a large map containing a small built colony; a nearly all-deck stress world still visits nearly every cell. Fire consumes oxygen, heats the cell, spreads stochastically to eligible neighbors, and extinguishes without oxygen. The model is deliberately game-like: heat has habitat relaxation, sunlight is an accelerated cycle, and no pressure momentum or mass units are modeled.

## Agents, work, and routes

Colonists and contacts have stable IDs. Work orders are indexed by tile and grouped into 16×16 spatial buckets. Workers consider a bounded nearby candidate set and prefer suitable professions. Construction reserves stock when ordered; cancellation refunds it. Releasing a worker clears ownership so another worker can take over.

The router uses breadth-first search with reusable queue, parent, target, and visited-stamp arrays. Paths are lists consumed with a cursor, avoiding front-removal shifts. The simulation allows at most 64 route requests per tick and rotates the crew processing order so agents receive turns. A single search can still visit the whole map; this is a request limit rather than a time/expansion limit. Local idle patrol avoids needless colony-wide searches. Routes are revalidated as people move, and impossible work stays queued.

This is a baseline for profiling, not a final solution to dense cities. Hierarchical navigation, connectivity/region caches, expansion budgets, and shared routes are plausible next changes if measurements identify routing as the bottleneck. There is no collision reservation or crowd avoidance; multiple people can occupy a cell.

## Rescues and colony progression

The opening has Ada alone and a reachable survivor. A map click or cursor confirmation dispatches a ready colonist and spends one rescue kit. Duplicate dispatches do not reserve a second worker or spend more stock. The expedition proceeds through outbound travel, emergency oxygen at contact, and a slower escorted return. The survivor becomes crew only after reaching pressurized shelter.

Unassisted survivors lose oxygen and then health. Failure releases the rescuer; an escorted living contact can become stranded again. Later contacts arrive over time. The charter requires three rescues, cycle 3, four living crew, 20 food, and a powered, pressurized Earth uplink. This milestone is covered by a full progression test involving actual work and rescues.

## Power and production

Connected deck/airlock cells form electrical islands. Machines draw only from their island's production and battery banks. Solar output follows daylight and storms; the starter RTG is constant. Supply allocation favors oxygen production, agriculture, medicine, and communications. Inspecting a network never advances stored energy. Life support consumes water and adds local oxygen/heat. Farms require power, water, pressure, and suitable temperature; nearby botanists help production.

## Sprite contract

`assets/catalog.json` schema 2 retains the original inventory and adds all 11,887 region records with current per-game usage and RPG assessments. `assets/sprites.json` remains the exhaustive frozen import-time audit, with stable IDs and crops. The source PNGs and colony binding selection are preserved. `assets/runtime-sprites.json` is the colony's smaller generated selection; `assets/lantern-bindings.json` selects the RPG roles and `assets/lantern-sprites.json` registers its bindings plus every occupied sprite for the native viewer.

`tools/build_runtime_assets.py` reads the audit and writes the runtime selection. The client loads PNGs directly, so building does not require MonoGame's content editor or Python. It strips black backgrounds and creates palette masks in memory, uses nearest-neighbor sampling, and draws Urizen's font with fixed advance. Source files are never recolored or repacked. Runtime output copies the needed folders and credits; the exhaustive catalogs, archives, and previews stay in the development repository.

## Saves and deterministic continuation

C# saves use format `regolith-csharp`, version 1, at `~/.regolith/colony-csharp.json` by default. They contain authoritative cell arrays, resources, people, machines, jobs, routes, survivor conditions, active expeditions, timing, logs, and explicit random state. Derived lookup tables and power connectivity are reconstructed on load. Input is validated before the colony is used.

Saving writes and flushes a uniquely named temporary file next to the destination, then atomically replaces the destination. A failed write leaves the previous save available. Tests compare exact serialized state after save/load and further ticks, including outbound and returning expeditions. Repeated runs use stable ordering and the saved random stream. Bit-for-bit floating-point equivalence across every CPU/runtime is not promised, and no old Python save conversion is provided.

## Extension boundaries

Keep game rules in Core; route UI actions through simulation methods rather than mutating world state from widgets. When using `World.SetTerrain`, topology caches invalidate automatically; bulk tools that write `Tiles` directly must call `InvalidateTopology`. Rendering should remain bounded by the viewport, with world summaries maintained by the simulation if their cost grows. Profile representative active colonies before adding concurrency, an ECS, or a chunked world.

Current simplifications include shared abstract resources, instant mined-stock delivery, automatic roof/radiator connections, no z-levels, no hauling, no combat, and no social simulation beyond needs and flavor traits. These are scope decisions rather than capabilities supplied automatically by MonoGame.
