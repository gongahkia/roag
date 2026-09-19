# Architecture and invariants

## Tick order

`sim.step` increments the integer tick, applies queued commands, refreshes labour,
advances fuses/blasts, steps materials, settles piles, steps structures, advances
ecological rules, then updates settlers. Periodic cleanup follows. Runtime elapsed
time is used only by the outer scheduler and measurements, never by world decisions.

Blast waves are calculated before terrain mutation. Source lists of fauna/plants are
length-captured before offspring append. Destructive list removal is deferred. The
particle engine retains its alternating sweep and update stamps. These are stable
sequential rules, not a claim of physically simultaneous or order-independent motion.

## Ownership

- `world.lua`: data, coordinate conversion, structures, identity and event helpers.
- `particles.lua`: existing material movement/contact rules (unchanged in this pass).
- `nav.lua`: existing worker-body geometry and reachable graph (unchanged).
- `jobs.lua`: reservations, supplies, paths, candidate scoring and ordinary execution.
- `labor.lua`: pure policy validation/apportionment, assignment, task eligibility.
- `fieldwork.lua`: worker path/reservation adapter for new encounter tasks.
- `colony_commands.lua`: validated extra commands. `commands.lua` routes all player input.
- `structures.lua`: original buildings plus charges/wards; physical tanks and costs.
- `blasts.lua`: arming, fuse, wave, destruction and accounting. No real-world chemistry.
- `catalog.lua`: pure content definitions. `content.lua`: bounded template/state logic.
- `ecology.lua`: interactions, movement, needs, sites, reproduction and observations.
- `signals.lua`: bounded, tick-stamped stimulus records.
- `metrics.lua`: explicit water/mineral/food accounting, colony/encounter descriptors.
- `history.lua`: live/archive separation, snapshots, command log, replay and envelope.
- `mapfile.lua`: exact JSON terrain+encounter templates, independent schema 1/2.
- `json.lua` / `codec.lua`: local bounded data codecs; neither evaluates stored code.
- `generation/frontier.lua`: current v2 generation. `frontier_v1.lua`: preserved recipe.
- `generation/layouts.lua` / `biomes.lua`: geometry and geological metadata.
- `generation/wonders.lua`: bounded room/encounter placement before protected starter.
- `main.lua` / `render.lua` / `ui/`: LÖVE adapters and draft interfaces only.

## Boundaries to preserve

World mutations go through deterministic simulation/commands. Rendering must not
mark something observed, consume a random number, increment an ID, or mutate a work
policy. World data is plain serializable Lua tables; live caches starting with `_`
are deliberately omitted by cloning/encoding. Do not hide authoritative state there.

Use stable array/order iteration for mutations. `pairs` order is not a foundation for
replay; current order-insensitive queries/sums can use it. Do not introduce global
`math.random` streams. Use local generation streams or coordinate/seed/tick functions.

Food, water and mineral transfers need one owner at every point: terrain, cache,
pile, carried stack, construction escrow or installed structure. Never count both
representations. Losses/conversions need a ledger entry. No unlogged gameplay resource
injection. Population caps must not silently drop resources when spawning fails.

Death remains permanent in live challenge time. UI archive input rejection is backed
by `history.queue`, not only disabled buttons. New mechanics must participate in full
save, rewind and replay. An initial-map template is not a full save or an undo.

## Extensions and compatibility

Application/save envelope 0.4.0, base world 0.2.0, map schema 2, feature template 1,
current generator frontier-v2. Loader also reads old envelope 0.2.0/map schema 1.
The genuine 0.2 and 0.3 fixtures must still advance to their recorded expected hashes.
Do not rewrite fixtures to hide a replay regression. Preserve old generator branches
when exact recipes are advertised. Unknown future envelope/schema versions reject.

## Performance

Fine maps are finite, entirely active and single-threaded. A tick cannot yield halfway
through material updates. Sparse bounded encounters still share the scheduler with
full-grid particles and worker navigation. Large crews increase planning cost.

Existing colony benchmarks and new soak tools record CPU/elapsed clocks explicitly.
Rendering/checkpoint inclusion is documented per tool, not casually combined. Lua
heap is not total RSS, GPU memory, peak allocation, or a prediction for another machine.
Do not optimise away entire off-screen ecology regions without defining equivalence.

## Primary extension seams

Add a layout through `layouts.names`, `descriptions`, implementation, menu/tests/docs.
Add a biome by appending a stable registry ID/key, updating placement and map tests.
Add content by registry + validated template + simulation + labour adapter where
needed + map/save roundtrip + accounting tests. Increase caps only with profiles.
Add a command with validation, archive rejection, deterministic replay and UI tests.
