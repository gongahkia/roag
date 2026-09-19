# Cosmonauts 0.4.0 — the living frontier

A from-scratch Lua/LÖVE side-view material colony prototype. Keep a settlement
functioning while excavation, water, food, labour, ruins and living things interact.
The working title is not a novelty claim. The rules are fictional game systems,
not calibrated physics or biology.

**Losses stand.** Challenge mode has no resurrection, replacement settlers,
automatic rescue or promised recovery route. Historical rewind remains read-only.
Your surviving crew may have no viable way forward.

## Install and run

Keep your existing source folder and back up the legacy deepward_02 save directory.
Extract this complete release into a separate folder. Do not merge a handful of
files into the old project. From the directory containing `main.lua`:

```sh
love --version
love .
```

Target: **LÖVE 11.5**, Lua 5.1-compatible source. On macOS the direct invocation is
`/Applications/love.app/Contents/MacOS/love .` when installed at that location.
No LuaRocks packages, downloaded art, shaders, physics engines or external libraries
are required. Ordinary `lua` is for tests/tools, not `lua main.lua`.

**Actual verification boundaries:** the core was executed against native Lua 5.4.
Input and graphics calls were tested with a mock adapter. Real LÖVE, LuaJIT, SDL,
GPU rendering and actual desktop file dropping were not available here. A mock
image is not a game screenshot. See `TEST_REPORT.md` for exact executed checks.

## Existing colony or fresh frontier?

Cosmonauts retains the legacy save identity deepward_02 so readable v0.2 and v0.3 colonies continue;
their terrain is not regenerated and no creatures are retroactively injected.
Crew controls and new construction are available in those worlds.

**N** opens the isolated generation lab. **Space** previews; **Enter** starts the
preview as a new expedition after archiving the old run. With no preview, the first
Enter only generates one. Escape keeps the current colony. **End** returns from
an inspected past to the live colony; **Space** runs or pauses it.

New saves use the **0.4.0 envelope** so old applications explicitly reject them.
The original base-world version remains 0.2.0, with validated optional extensions.
Keep backups to downgrade. The loader does not overwrite an unreadable run with a
fresh colony. See `UPGRADE.md`.

## The first settlement

The default is seed 12345, frontier/hybrid/balanced, 256x160 cells, three workers,
living contents, paused. Close F1. Order a farm with **C** at block **31,18** and a
bed with **B** at block **29,18** in the arrival chamber. These coordinates apply
only to the default dimensions/start. Use the inspector for other maps.

Space starts simulation. Workers fetch real materials, build, collect actual water,
irrigate, harvest, eat and rest. Water in the starting well is finite. More workers
mean more mouths, labour demand and water consumption; they are not free difficulty
reduction. Only starting food scales with the selected initial crew. Stone, metal,
soil and the well remain finite and unchanged.

Dig with **D**, and build ladders **L** for vertical access. Construction footprints
are 4x4 fine cells; settlers occupy 2x3 cells. A water opening may not fit a person.
Floors and walls block fluids; ladders do not. Most functional buildings require
supported, empty footprints. Construction does not silently delete water.

## Directing people: H, Y, M and J

**H: crew panel.** The first page has per-worker priorities for mining,
construction, hauling, food/crops, pumps and fieldwork. Click a cell to cycle OFF,
low, normal, high; arrows and 0–3 are keyboard alternatives. Click the role column
to pin a role. Shift-click a duty also pins it. R resets the selected row.

Tab opens the workforce page. Q enables quotas. Adjust percentages in 5-point
steps, or use presets 1–4. Enter queues the draft for the next simulation tick;
Escape discards it. The panel shows the resulting actual worker assignments.

Percentages apply to living **AUTO** workers, not pinned workers, and allocate
whole people using largest remainder. They are not percentages of simulated work
time. An impossible disabled-duty allocation leaves a vacancy rather than silently
changing the request. Quotas do not automatically relax when that category has no
work. Food, rest and immediate danger still override ordinary work.

Click a settler, then **Y**, to reserve subsequent orders for that person. Y clears
the target. **Shift+Y** assigns the open order at the inspected/hovered block to the
current target, or clears its owner when no worker is selected/targeted. A named
order overrides a pinned role or quota, but NOT an explicitly OFF duty. A dead or
trapped assignee does not automatically get replaced; clear/reassign the order.

**M** then a map click rallies the selected worker. With none selected, or Shift+M,
it rallies everyone. Workers hold near a reachable standing position, not an exact
floating pixel. **J** releases the selected worker; no selection or Shift+J releases
all. Hunger, fatigue and immediate hazards may interrupt a rally. This is a move/
hold order through the normal pathfinder, not teleportation or direct platformer
control. See `docs/WORKFORCE.md`.

## Generation lab: eleven layouts, twelve geological regions

Original layout choices remain: hybrid, noise, cellular, worms, faults and vaults.
Five more are available: **karst**, **labyrinth**, **roots**, **chasms** and **crystal**.
They respectively build stacked basin/shaft systems, coarse depth-first mazes,
branching passages, deep rifts with ledges, and angular fracture networks.

The original eight regions remain. **Mycelial folds, glass fractures, buried
ossuary and breathing faults** add soil, ice/mineral seams, ore strata and contact
hazards. Geology varies laterally and with depth. Profiles: balanced, arid, frozen,
volcanic, overgrown, ruined and abyssal. These are generation biases, not weather.

| Lab key | Effect |
|---|---|
| Digits / Backspace | Seed |
| Left / Right | Frontier or original cistern/dunes/frost scenario |
| Up / Down | Layout method |
| B / S | Biome profile / dimensions |
| O / G | Openness / geological region scale |
| F / X | Living, ruins-only or none / encounter density |
| K | Three, six or nine starting settlers |
| Tab | New expedition's challenge/practice mode |
| Space / V | Generate preview / material-biome view |
| Enter / Escape | Confirm preview / cancel |

Legacy scenarios ignore frontier-only generation settings, including crew size.
The world is finite, generated in advance, with closed borders. There is no infinite
extension, off-screen freezing or fog of war. Larger maps and crews cost more; no
interactive frame rate is promised for 512x256 or nine workers.

## Ruins, creatures and a strange ecology

Seeded multi-room compounds include cisterns, ossuaries, archives, forges and
nurseries. Their geometry, finite loot, liquids and occupants are placed into the
same physical world as the colony. They are not loading-screen dungeon instances.
Their routes are not guaranteed to fit settlers or connect to the arrival room.

Living maps contain three growth archetypes, four creature archetypes, and four
kinds of encounter object. These are bounded, fixed rule sets, not arbitrary runtime
scripts or an infinite species generator. Local steering means creatures can get
stuck, starve, be stranded or die to the environment.

Workers sight nearby, unobstructed encounters. **U** designates a survey, **Z** a
salvage operation, and **K** a cull. A worker must reach the target. **Fieldwork** in
the crew panel covers these tasks and charge arming. **F4** shows field notes:
unobserved types are absent, sightings are unidentified, and completed surveys
reveal reliable descriptions. The map itself is visible; this is knowledge gating,
not a fog-of-war system. Spoiler rules for development are in `docs/ECOLOGY_RULES.md`.

Growth consumes finite water and stores edible biomass. Grazers eat wild growth or
ripe colony crops. Predators hunt and respond to disturbance. Dormant machinery
reacts to mining, blasts or living pulses. Clearing an area, draining a reservoir,
salvaging a nursery or breaching a ruin can therefore change several systems.

The universe is unfamiliar to the colony, not inconsistent to the engine. Field
notes are authored descriptions unlocked by an actual task; they do not invent
psychological stories or claim a complete causal explanation for every incident.

## Demolition charges and wards

**A** designates a demolition charge (4 metal). It is inert after construction.
Select the completed charge and press **T** to order a field worker to arm it.
Arming requires work, then starts an **80-tick fuse** (four simulated seconds at
normal scheduling). The countdown and maximum blast radius appear on the map.
Nearby workers attempt to move away; escape is not guaranteed. An armed charge
cannot be disarmed, cancelled back into safety, or dismantled.

The blast damages workers/creatures, destroys terrain and infrastructure, changes
water/ice state, releases ruin stock, triggers nearby charges, and emits disturbance.
Bedrock stops propagation; other obstacles attenuate it. These are stylised grid
rules, not a realistic blast/rigid-body simulation. Most mined material in the blast
is lost; every declared loss appears in the accounting ledger. No explosive
chemistry, fuel recipe or real-world construction procedure is simulated.

**F9** designates a resonance ward (10 metal). Food/crop workers deliver actual
water. While supported, enabled, supplied and unflooded it deters hostile approach
and suppresses nearby hostile attacks/growth exposure. It consumes one stored water
unit per 120 ticks. It is not invulnerability against lava, drowning, starvation,
falling or bombs. T toggles a completed ward.

## Main controls

| Control | Action |
|---|---|
| Space / 1–3 | Pause or run / 1x, 2x, 4x scheduling |
| Q / right click | Inspect |
| D / L / F / W | Dig / ladder / floor / wall |
| B / S / C / P | Bed / stockpile / farm / pump |
| X / E / +/- | Dismantle / cancel / job priority |
| H / Y / Shift+Y | Workforce panel / target new orders / reassign a block |
| M / J | Rally / release (Shift applies to all) |
| U / Z / K | Survey / salvage / cull |
| A / F9 / T | Charge / ward / operate selected special structure |
| I / O | Selected pump intake/outlet; range 20 cells |
| F4 | Field notes |
| Tab / F7 / G | Layers / biome survey / building grid |
| Wheel / middle drag / R | Zoom / pan / fit map |
| Left / Right / Shift+arrows | Inspect or step / 20-tick batch |
| Home / End | Initial state / live frontier |
| N / F2 / Shift+F2 / F3 | Map lab / initial map / current terrain template / maps |
| F5 / F6 | Save live colony / diagnostic export with full replay |
| F8 / Shift+F8 | Colony benchmark / generation and map benchmark |
| F10 / F12 / F1 | Regression tests / actual screenshot / manual |

Orders entered while paused wait for the next tick. Right single-steps. A worker
panel is a draft, not an out-of-tick simulation mutation. Practice-only V retains
the fine-cell material brush. Challenge archive rejects all new command families.

## Maps and saves

`.dwmap.json` schema **2** stores exact terrain, exact geology and baked encounter
placements plus initial crew size. Schema 1 still imports; old frontier-v1 recipes
still have their preserved generator. Import never reruns the recipe: metadata is
provenance. F2 exports the original map; Shift+F2 exports selected current terrain
and surviving encounters as a NEW starting template, resetting creature health,
activation and colony knowledge. Structures/items/workers/history are not retained.
A blocked starting footprint can reject current-terrain export.

Use **F5/F6** for a full colony, not map interchange. The full snapshot includes
assignments, orders, fuses, creatures, growth, knowledge, inventories and history.
Map files cannot resurrect the old live challenge colony: adopting one creates a
separate expedition. See `docs/MAP_FORMAT.md` for bounds and schema details.

## Tests and continuing development

```sh
lua tests/syntax.lua
lua tests/run.lua
lua tests/gui_smoke.lua
lua tests/map_gui.lua
lua tests/expansion_gui.lua
lua tests/benchmark_smoke.lua
lua tools/expansion_soak.lua soak.csv 800
lua tools/headless.lua 12345 frontier 2000
```

`luajit` can replace `lua` when installed. F10 runs core tests in actual LÖVE on your
machine. Mock GUI tests use isolated `/tmp/` directories and do not access live saves.
The full suite is synchronous and may take time. CLI CPU timings are not FPS, GPU
costs or elapsed-time results. Lua-heap deltas are not process RSS or peak memory.

Start future repository-based development with **`AGENTS.md`**, **`HANDOFF.md`**,
**`PROJECT_STATE.md`**, and **`docs/ARCHITECTURE.md`**. This is a tested prototype,
not a finished colony game: there is no equipment economy, ranged combat, diplomacy,
procedural narrative generator, advanced social simulation, pressure solver or
structural collapse engine. Preserve the functioning loops before widening scope.
