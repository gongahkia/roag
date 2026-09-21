# COS-G04 — industry, power, machines, conveyors, and automation

## Status

Implemented as the versioned `industry=1` current-frontier feature. It requires
the existing physical equipment and safe-excavation features. Older campaigns
remain feature-off: their rules, manual mining yield, starter state, visibility,
and replay behavior are not migrated.

## Implemented model

`src.industry` owns the compact G04 model: recipe registry, physical machine
buffers, revisioned derived power topology, solar exposure, battery allocation,
Fabricator cycles, Mining Rig contributions, conveyor/bin transfer, lamp sources,
and industrial validation/destruction release. It is advanced from the normal
site simulation phase before worker actions; renderer/UI access only queries it.

The current registry contains:

| Recipe | Input | Output | Powered ticks |
| --- | --- | --- | --- |
| Machine Component | 2 metal | 1 component | 40 |
| Pickaxe | 2 metal | 1 pickaxe | 50 |
| Rope coil | 1 metal | 1 rope coil | 30 |

The Tool bench adds the slow bootstrap Component recipe, using the same two
metal and 120 ordinary work actions. Components count as two mineral-equivalent
units. `src.equipment`, `src.jobs`, `src.logistics`, `src.metrics`, and campaign
validation preserve exactly one custody across loose/carry/escrow/craft/machine
owners.

Industrial structures are declared in `src.structures`: Small Solar Array,
Power Pole, Battery, Fabricator, Mining Rig, Industrial Bin, Conveyor, and
Electric Lamp. Construction/removal follows normal Build work, resource escrow,
support, destruction, and fog-gated ordering. Conveyors require solid footing
but do not block a valid 2×4 walking body.

Power links are site-local. Poles join at six build blocks; structures attach to
a pole within three. Solar panels generate three units each when their vertical
representative column is open to the map top. Batteries hold 0–200 and move at
most six per tick. Consumers are granted all-or-nothing power by priority then
stable lifetime ID. Fabricator/Rig wear reaches maintenance after the 600th
productive tick, where a worker consumes one Component through 60 real actions.

Mining Rigs work only pre-existing player dig designations and use an outward
drill-mouth segment. The designated solid endpoint is intentionally permitted;
the rig’s own structure and support are not mistaken for a blocker. It never
discovers terrain, creates a designation, or grants a person a safe descent.

## Presentation and persistence

`src/ui/action_hud.lua`, `src.render.lua`, `src.campaign_commands.lua`, and
`main.lua` expose industry through the existing live Build/Inspect surfaces.
Recipe, enabled, priority, direction, and bin-mode commands bind stable
structure IDs. A removed/rebuilt structure rejects a stale ID command. The
renderer reports physical buffers, plain-language blocked states, power state,
wear, and battery charge; powered lamps enter G01’s ordinary illumination query
at radius 18.

Authoritative industrial state is included in clone/codec/history: battery
charge, buffers, recipe progress/in-process escrow, wear, conveyor/bin cargo,
configuration, and topology revision. The actual connected components and render
overlay are derived caches and are not serialized. Machine Components are part
of normal cargo and share the existing 24-unit craft capacity.

## Automated evidence

`tests/g04.lua` covers feature/legacy boundaries, real Tool-Bench bootstrap,
power geometry/allocation/solar/battery bounds, Fabricator cycles, rig output,
one-segment belt/bin movement, wear/lamp behavior, cargo schema, save clone, and
malformed buffer rejection. `tests/g04_gui.lua` is explicitly **MOCK UI** for
live industrial configuration and stale identity rejection.

`tools/g04_soak.lua 9804 25000` combines the real G02/G03/P04 route with
`industry=1`: it manufactures a component at the real Tool Bench, carries it in
the shuttle, unloads it at Moon I, continues whole-campaign history to tick
25,000, and separately exercises a labelled stable factory fixture through
power, Fabricator, belt/bin, Rig, lamp, battery, and codec validation. The
factory fixture is HEADLESS diagnostic evidence, not a normal generated-map or
human-play claim.

Native LÖVE and human gameplay are separate evidence and remain **NOT RUN**
unless a person completes the updated playtest route.

## Verification in this checkout

The final headless verification passed `luajit tests/syntax.lua` (111 Lua
files), `luajit tests/run.lua` (184 groups / 121,274 assertions),
`luajit tests/g04_gui.lua /tmp/cosmonauts-g04-gui-final` (**MOCK UI**),
`luajit tools/g04_soak.lua 9804 25000` (**HEADLESS**),
`luajit tests/maximum_size.lua`, and both `git diff --check` forms.

The 128×80 new G04 regional campaign encoded to 1,640,218 bytes. The G04 soak
finished its whole-campaign route at tick 25,000 (3,345,849 bytes, one unloaded
Moon-I component) and the separate factory fixture at tick 1,020 (208,040
bytes): battery charge 200, eight Fabricator components in the receiving bin,
and sixteen Rig stone units in its receiving bin. The 512×256 maximum-map check
is a terrain/map-accounting smoke test, not an industrial FPS benchmark.

## Deferred work

G04 does not add factions/cultures, combat overhaul, autonomous interplanetary
routes, power networks between sites, pipes, trains, robots, splitters,
underground belts, circuits, generic production trees, new planetary physics,
or relic travel. The next planned direction after G04 review is factions and
cultures, not an automatic G05 implementation.
