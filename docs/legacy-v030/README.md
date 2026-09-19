# DEEPWARD 0.3.0 — underground frontier and map laboratory

A finite side-view material colony game in Lua/LÖVE. This update adds spatial geology,
five cave-layout techniques plus a hybrid, and actual map-file interchange while
preserving the 0.2 colony simulation. The working title is not a novelty claim.

**The world is still unforgiving.** Dead settlers stay dead. Water is finite, routes
can fail, food can become inaccessible, and the settlement may be lost permanently.
Generation validates the starting footprints, not a guaranteed route to success.

## Upgrade and run

Back up the previous source folder and the `deepward_02` save folder first. Read
`UPGRADE.md`. LÖVE's save identity is intentionally unchanged: an existing readable
0.2 expedition resumes, with the same terrain and history. It will NOT acquire new
biomes retroactively. **N** opens the new generation lab without changing that run.

From this directory (the one containing `main.lua`):

```sh
love --version
love .
```

On macOS with the usual application location:

```sh
/Applications/love.app/Contents/MacOS/love .
```

Target: LÖVE 11.5, Lua 5.1-compatible source. No LuaRocks packages, external terrain
libraries, downloaded sprites, or font files. Do not launch with `lua main.lua`.
Plain Lua/LuaJIT is for the command-line tools and headless tests.

**Verification boundary:** native Lua 5.4 execution and mocked LÖVE wiring were
exercised here. Real LÖVE, LuaJIT, SDL file-drop input, and the GPU were unavailable.
The mock is not a substitute for your actual application run. See `TEST_REPORT.md`.

## Try the new terrain without losing the current colony

Press **N**. The new lab selects `frontier`. Press **Space** for a preview. Use
**Up/Down** to choose a different layout, then **Space** again. **V** switches the
preview between materials and geological regions. Changing settings invalidates the
old preview. The live colony is paused and unchanged throughout.

**Enter confirms the displayed preview.** If no preview exists, the first Enter
only generates it; a second Enter confirms it. Before adoption, the application
archives the previous live replay and commits the new save. A reported save failure
leaves the current in-memory colony selected. **Escape cancels the preview.**

The first fresh default is seed 12345, `frontier`, `hybrid`, `balanced`, 256 x 160.
The arrival chamber is near the centre rather than at the left edge. Terrain exists
above, below, and on both sides. Workers must reach it by excavation and construction;
there is no universal connectivity guarantee or recovery director.

For the default new map, build a farm at block **31,18** and a bed at **29,18**.
These are empty supported positions inside the arrival chamber. Press **C** or **B**,
click the block, then **Space**. Workers fetch resources, build, collect well water,
irrigate, harvest, eat, and rest. The well holds a finite starting supply.

For other sizes, use the inspector instead of assuming those coordinates. On original
192 x 112 cistern maps, the old starting positions remain farm **9,10**, bed **7,10**.

## Generation controls (inside N)

| Key | Action |
|---|---|
| Digits / Backspace | Edit the seed |
| Left / Right | Frontier or a legacy scenario |
| Up / Down | Hybrid, noise, cellular, worms, faults, vaults |
| B | Balanced, arid, frozen, volcanic biome profile |
| S | 128x80, 192x112, 256x160, 384x224, 512x256 |
| O | Openness setting: 0.35, 0.48, 0.60 |
| G | Region scale: 0.75, 1.0, 1.4 |
| Tab | Challenge / practice for the NEW expedition |
| Space | Generate an isolated preview |
| V | Preview materials / biome survey |
| F2 | Export the displayed map |
| F3 | Browse map files |
| Enter | Preview if missing; otherwise confirm that map |
| Escape | Cancel and keep the current expedition |

Openness is a generator parameter, not a promised air fraction. Larger region scale
means broader lateral geological regions. Legacy scenarios ignore the frontier-only
layout/profile/openness/region-scale settings. Bigger worlds cost more simulation,
pathfinding, rendering, and history memory; no interactive frame rate is promised.

## The new biomes

Geological regions have both lateral variation and depth bias, with warped boundaries.
They are not only horizontal stripes or palette swaps. Each influences the initial
material mix, pocket content, and (under hybrid generation) cavern shape.

| Biome | Initial character / consequences under existing rules |
|---|---|
| Rime galleries | Ice lenses and frozen pockets; liquid water can be scarce |
| Buried dunes | Loose sand and dry pockets; excavation can release falling material |
| Shale shelves | Stratified rock, galleries, shafts, relatively sparse ore |
| Loam hollows | Soil lenses and small water pockets |
| Drowned limestone | Larger finite reservoirs and potential flooding |
| Ferric seams | Ore-rich seams and rock passages |
| Basalt deeps | Lava pockets and the existing contact reactions / lethal exposure |
| Pale vaults | Tall halls and pillars, especially with hybrid/vault geometry |

The profile biases which regions appear at different depths. The seed controls their
lateral arrangement and shapes. Not every seed/profile must contain every biome.
**F7** toggles the biome survey; the inspector describes the selected region.

Biome membership stays attached to a location when its sand or water moves. The
material rules are unchanged. These are geological generation regions, NOT a new
weather model, temperature field, crop-bonus system, fauna, or complete ecology.
The entire finite map is generated in advance. There is no streaming, fog-of-war,
infinite extension, or additional Dwarf-Fortress-style z-axis in this update.

## Map files versus colony saves

**`.dwmap.json` is a terrain template.** It contains exact material cells, exact
region cells, dimensions, a start marker, title/seed, and optional generator metadata.
It does not contain your settlers, structures, orders, inventories, deaths, or history.
Importing it starts a NEW expedition with the standard supplies and three workers.

**`run.dat` / replay `.dw` retain the entire colony.** Continue using F5/F6 for those.
The map format is versioned independently of both the application and colony-state
schema. A recipe is informational provenance: normal import never reruns a generator.

### Export and import

- **F2 in normal play:** export the expedition's initial map, even after it has evolved.
- **Shift+F2 in play:** export the terrain at the inspected tick only. Structures,
  people and piles are omitted. A damaged/blocked arrival can cause explicit rejection.
- **F2 in the lab:** export that preview without starting or replacing a colony.
- **F3:** browse `maps/` in the save directory and bundled `maps/examples/`.
- **Drag a `.dwmap.json` file onto the window:** validate and open an import preview.
  It does not automatically replace the live run. Tab chooses challenge/practice;
  Enter confirms; Escape cancels.

Six same-seed example maps are bundled. Their biome geography is identical; their
layout methods differ. Use them for controlled visual comparisons rather than
comparing different seeds and several settings at once.

F2 displays a relative path. The full location is LÖVE's save directory plus that
path; **F6 prints the save directory** in its export notification. On Linux the usual
identity directory is `~/.local/share/love/deepward_02/` (environment overrides may
change it). Trust the application's reported directory rather than guessing.

The new JSON codec is data-only: imports never evaluate Lua. Limits cover bytes,
parse depth/nodes, dimensions, palettes, run counts, boundaries, and starting geometry.
That is defensive validation, not a security audit or authenticity guarantee.

See `docs/MAP_FORMAT.md`, `docs/GENERATION.md`, and `maps/README.md` for the precise
schema, command-line experiments, and examples.

## Main game controls

| Key / mouse | Action |
|---|---|
| Space | Pause/play live time |
| 1 / 2 / 3 | 1x / 2x / 4x scheduling speed, unchanged fixed simulation step |
| Q / right click | Inspect |
| D + drag | Excavate building blocks, one reachable material cell at a time |
| L / F / W | Ladder / floor / wall blueprint |
| B / S / C / P | Bed / stockpile / fungus bed / hand pump blueprint |
| X / E + drag | Dismantle / cancel |
| + / - | Order priority; selected/hovered open orders can be reprioritised |
| I / O | Configure selected completed pump's intake / outlet |
| T | Enable/disable selected pump |
| Tab | Materials / water-contact view / worker routes / biomes |
| F7 | Toggle biome survey |
| G | Construction grid |
| Wheel / middle drag / R | Zoom / pan / fit |
| Left / Right | Inspect or advance one tick |
| Shift + arrows | Inspect or advance 20 ticks |
| Home / End | Initial state / live frontier |
| Timeline click | Inspect explored history |
| N / F2 / F3 | Map lab / export map / map browser |
| F5 | Save the LIVE colony, including while inspecting the past |
| F6 | Export colony metrics, geology descriptors, chronicle, live replay, runtime context |
| F8 | Colony benchmark in isolated worlds |
| Shift+F8 | Generation / map-interchange benchmark |
| F10 | Core and map regression tests; pauses the application |
| F12 | Actual application screenshot |
| F1 | Manual |
| Escape | Cancel modal, benchmark, port placement, or active tool |
| V in practice play | Cycle fine-cell material brush |

Paused orders are queued for the next tick. Right advances one tick. Drag designations
are capped at 256 blocks. Open/completed job storage and the command log remain bounded.

## Preserved colony loop and failure rules

People occupy 2x3 material cells; construction blocks are 4x4. Workers walk, step,
climb ladders, and make limited drops. Material movement can invalidate navigation.
Supply planning checks for a known return route, but later world changes can still
trap a worker. Designated mining can remove the worker's own footing.

Construction resources must be reached, picked up, carried, and delivered. Blueprints
do not block floods until completed. Completing walls/floors cannot silently replace
water, a person, or a resource pile. Reservations prevent duplicate consumption.

| Structure | Cost / main rule |
|---|---|
| Wall | 16 stone; solid full footprint |
| Floor | 4 stone; solid bottom row |
| Ladder | 4 stone; permits climbing, permeable to materials |
| Bed | 6 stone; one user, faster rest, slow fed healing |
| Stockpile | 4 stone; hauling destination |
| Fungus bed | 8 soil; physically irrigated; three rations per mature harvest |
| Hand pump | 8 metal; needs operator; transfers actual water to an empty outlet |

A pump's endpoints must be within Manhattan distance 20 of its centre. There is no
pipe geometry or pressure solver; this is an explicit range-limited hose abstraction.
A farm consumes one water unit per 100 growth ticks and matures after 1200 growth
ticks. Flooding can reduce growth. Food must actually be reachable and consumed.

Hunger, fatigue, injury, drowning, burial, long falls, lava, and steam still matter.
Challenge mode forbids painting and historical edits. Rewind is read-only inspection;
it cannot recover dead workers. There are no new settlers, automatic supplies,
rescue incidents, compulsory escape routes, or promise of recoverability.

Deaths pause and attempt to save the committed loss. The game also saves every
200 live ticks, on F5, and on normal exit. Practice branching remains confined to
separate practice expeditions. Save policy is not tamper-proof ironman, and a crash
can lose progress since the last successful save.

Rock does not become a rotating rigid body when unsupported. Loose material falls;
structures remain anchored. No new combat, relationships, recruitment, temperature
field, plant ecosystem, or collapse physics was added in this generation update.

## Replay and compatibility

The live frontier stays separate from the inspected archive. Checkpoints cover
materials, biome fields when present, workers, structures, tasks, reservations,
inventories, rules, counters, and recent events. Ten additional checkpoints remain
cached by default, every 200 ticks; very old seeks replay from the retained initial
state. This bounds snapshot count, not total memory or seek latency. Command history
is capped at 20,000 accepted player commands.

The application is 0.3.0 but retains the 0.2.0 colony-state schema. A genuine 0.2.0
save fixture is loaded and advanced to an independently recorded original state hash
in the tests. This is an explicit compatibility check, not a promise for arbitrary
locally modified saves or a guarantee that downgrading to old code is supported.

Target reproducibility: same source/runtime/build. Cross-machine floating-point bit
identity is not promised. The data-only colony codec still bounds parsing and never
executes stored Lua. Existing unreadable saves are preserved rather than replaced by
an automatic fresh run. Persistence uses same-directory rename on the intended Linux/
macOS platforms; it is not an fsync-guaranteed transactional database.

## Headless tools and tests

Run from the repository root; `luajit` may replace `lua` on your machine:

```sh
lua tests/run.lua
lua tests/syntax.lua
lua tests/gui_smoke.lua
lua tests/map_gui.lua
lua tests/benchmark_smoke.lua
lua tools/headless.lua 12345 frontier 2000
lua tools/map.lua help
lua tools/generation_benchmark.lua generation-results.csv
lua tools/benchmark.lua
lua tools/replay.lua /path/to/replay.dw 1000
```

The two GUI tests are Linux-oriented mock adapters, not LÖVE screenshots or SDL tests.
Do not run test code that writes to your real save directory; these mock tests use
separate `/tmp/` directories. F10 runs the core suites through your actual LÖVE runtime.

Generation benchmarks time allocation/generation, map serialization, and map parsing/
restoration separately. They also record descriptive geometry/material measurements.
CLI timings use `os.clock` CPU time; LÖVE uses elapsed time. Do not pool them or treat
these results as FPS. Retained Lua-heap deltas are not peak, RSS, or GPU memory.

The six-layout benchmark compares settings that share geology, pockets, and a starter;
it does not scientifically rank algorithm families. Four-neighbour air connectivity
is not worker-body reachability or a quality score. See the accompanying manifest.

## Source map

```text
src/generate.lua              generation facade and legacy routing
src/generate_legacy.lua       original 0.2 terrain construction
src/biomes.lua                spatial geological regions
src/generation/layouts.lua    five masks plus biome-aware hybrid
src/generation/frontier.lua   mask -> material lenses -> pockets -> starter
src/expedition.lua            fresh standard colony / arrival validation
src/json.lua                  bounded, data-only JSON codec
src/mapfile.lua               versioned terrain-template schema and RLE
src/mapstore.lua              LÖVE map-file adapter
src/generation/report.lua     geometry, resources, region descriptors/differences
src/generation/benchmark.lua  isolated generation and map-interchange matrix
src/world.lua / history.lua   compatible colony state and replay
src/render.lua / main.lua     lab, map browser, biome view, existing colony UI
src/particles.lua             preserved material simulation
src/nav.lua / jobs.lua        preserved navigation and job execution
src/colonists.lua             preserved needs, movement, hazards, death
src/structures.lua / sim.lua  preserved construction, crops, pumps, tick order
```

See `PROJECT_STATE.md` when handing this repository back for the next iteration.
