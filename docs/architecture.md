# Architecture and limits

## Code organization

Odin builds `src/` as one `sandbox` package. Files are organized by responsibility; the existing build commands, CLI options, shader paths, and save formats continue to apply. The entry point in `main.odin` parses configuration and starts the application.

| Responsibility | Source files |
|---|---|
| Application ownership and resource lifetime | `main.odin`, `app.odin` |
| Ordered frame phases and framebuffer admission | `app_loop.odin` |
| CLI profiles and shared memory reservation policy | `config.odin`, `budgets.odin` |
| Input snapshots, menus, shortcuts, camera controls, and native pointer visibility | `input.odin`, `cursor_{linux,other}.odin` |
| Player movement, targeting, interactive edits, and simulation coordination | `gameplay.odin` |
| Runtime loading and saving of the world/settings | `session.odin` |
| Shared values, coordinates, materials, and creative selection | `types.odin`, `coordinates.odin`, `blocks.odin`, `creative_palette.odin` |
| Legacy generation, fixed-point landscapes/formations/caves, and immutable jobs | `noise*.odin`, `terrain*.odin`, `mesh_jobs.odin`, `spawn.odin` |
| Sparse world edits, binary save codec, and block picking | `world.odin`, `world_save.odin`, `raycast.odin` |
| Player collision and source-fed fluid rules | `player.odin`, `fluids.odin` |
| CPU meshing, chunk residency/uploads, and worker lifetime | `mesh.odin`, `stream.odin`, `workers.odin` |
| Graphics device/frame lifecycle, shader compilation, and texture creation | `render.odin`, `render_shader.odin`, `render_texture.odin` |
| GPU mesh buffers/cache and terrain/water submission | `gpu_mesh.odin`, `render_world.odin` |
| Vector/matrix operations, projection, and frustum tests | `math.odin`, `camera.odin` |
| UI primitives/font, hotbar, diagnostics, and settings | `ui_draw.odin`, `hud.odin`, `benchmark_hud.odin`, `settings_menu.odin` |
| Settings serialization and OS process measurements | `settings.odin`, `telemetry.odin`, `telemetry_{linux,darwin,windows,unix}.odin` |
| Display detection, pacing, platform sleep, and screenshots | `frame_pacing.odin`, `frame_sleep_{unix,windows}.odin`, `capture.odin` |
| Repeatable benchmark scenes and CSV reports | `benchmark_scenes.odin`, `benchmark_report.odin` |

`App` owns the world, renderer, streaming registry, player/fluid state, settings, input history, telemetry, and reusable draw list on the main thread. Its lifetime is explicit in `app_run`. Core world, generation, meshing, player, and fluid procedures keep explicit subsystem arguments. Scene submission receives a `Render_View` and references to the world, stream, and reusable draw list. UI and benchmark reporting read application state through the owner.

The frame loop polls events and samples time, processes controls, advances gameplay and fluid edits, admits framebuffer/chunk work, draws the world and UI, then swaps/paces and records the completed frame. Input history is committed after gameplay consumes its edges. Worker stop requests are sent to every worker before joining any of them; workers finish before renderer/window/world teardown. The allocator remains valid throughout that lifetime.

Tests are grouped into matching `*_test.odin` files for world storage, generation, saves, raycasts, CPU/GPU meshes, streaming, player movement, fluids, settings, frame pacing, and telemetry. Legacy terrain assertions explicitly select their original generator, and generators 3/4 have additional compatibility, geometry, determinism, and gameplay coverage. `scripts/smoke.py` runs the graphical/persistence/budget scenarios; `scripts/smoke_input.py` owns the isolated X11 interaction fixtures. `scripts/build.py` and the CI entry commands are unchanged.

## Player movement and collision

Normal play starts in walking mode. The player has a 0.6×1.8 block axis-aligned body and an eye height of 1.62 blocks. The renderer and saves keep eye coordinates; collision bounds derive from them. Solid materials, including ores and leaves, block the body. Water/lava do not; waist immersion slows movement and enables Space/Ctrl swimming.

Walking uses fixed 120 Hz steps, at most twelve per frame, with normalized horizontal input, gravity, terminal velocity, jumping on a fresh key press, and a sprint multiplier. Each axis sweeps the body through every crossed voxel plane and clips travel to the closest solid surface. Remaining axes continue to slide along walls. The queries use the live world generator and edit map, independently of mesh generation or GPU upload; invisible or unfinished chunks cannot cause fall-through. The controller allocates no per-frame collision buffers.

F4 toggles spectator mode, which retains free flight, noclip, and creative editing. `--spectator` chooses it at startup; benchmark camera paths also use this mode. Leaving spectator, loading an embedded old camera position, or a fluid reaction creating a block inside the body triggers a bounded upward search for free space. Mode changes reset velocity and pending jumps. The save format remains unchanged and stores position rather than mode or velocity. Normal play therefore starts with collisions unless explicitly overridden.

Manual solid placement is rejected when it intersects the walking player's body. Settings pause the player without advancing physics or accumulating catch-up time, while streaming and fluids continue. The hotbar labels WALK/NOCLIP and the full diagnostics panel includes physics update time. Tests cover grounding, wall sliding, high-speed sweeps, negative chunk boundaries, jump/head collisions, frame-rate equivalence, swimming, body overlap, and switching out of embedded spectator positions. The graphical smoke suite also performs actual wall, jump, placement, and F4 input checks.

## World and creative edits

World positions are signed integer block coordinates; floor division handles negative chunk coordinates. Chunks are 32³, the vertical range is eight chunks, and horizontal terrain is generated on demand. Camera-relative float rendering avoids losing local precision far from the origin. The camera is bounded to ±100 million blocks horizontally.

New worlds use generator 5's biome geography and bounded downhill drainage, alongside generator 4's climate biomes and vegetation and generator 3's fixed-point regional landscapes, derivative-aware layered noise, floating-island biome, rare grounded formations, connected tunnel/cavern cells, and contained underground lakes. Worker sampling and direct block queries share the same rules and bounded descriptors. See [terrain generation](terrain.md) for algorithms, controls, spawning, and reproducible validation. Legacy generators 1/2 retain their original floating-point height/noise functions.

Generator 2 introduced deterministic ellipsoid ore veins, sea-level water, and deep lava; generator 3 retains these materials and ore depth rules. Generator 1 remains available for existing saves. See [materials and fluids](materials.md) for controls and flow rules.

Untouched chunks have no persistent dense block allocation. The world stores only edited positions, bucketed by chunk. Each bucket also indexes its positions for bounded incremental fluid recovery. Reads overlay these edits on procedural terrain. A mesh job snapshots edits from the 27 touching buckets, restricted to its 34³ padded sampling region, and carries the generator version. Job generation needs no mutable access to the live world.

`Edit_Command` carries a monotonically increasing `u64` sequence, integer position, and block ID. `world_apply` rejects stale sequences, invalid block IDs, protected bottom-layer edits, out-of-height edits, and edits beyond memory/entry limits. There are at most 262,144 distinct edited positions. New edits are also refused when application heap use approaches the configured CPU allowance after worker/save headroom. Re-editing a position replaces its saved value.

`world_get`, `world_apply`, `world_snapshot`, and the generation functions contain no graphics or networking calls. Their representations are internal Odin APIs, not a network wire protocol. Future P2P work can translate validated network commands into this interface.

## Mesh generation

Each worker samples one 34³ block buffer: a 32³ interior with a one-voxel halo. Opaque faces bordering air, fluids, or ice render. Shared ice faces are removed. Internal faces of matching fluids disappear; different flow heights expose only the higher cell's side strip. Face direction, material, fluid heights, and four vertex ambient-occlusion samples determine compatibility. Rectangular faces merge greedily when shading and heights are uniform and compatible. Nonuniform AO faces stay separate to preserve shading.

Vertices remain eight bytes: three local `u16` positions and a packed field containing face (3 bits), material (6), AO (2), fluid height (4), and a top-vertex flag (1). Fluid tops and exposed side bottoms use eighth-block offsets in the vertex shader. Four vertices and six 32-bit indices form each quad. Triangle winding faces outward for all six directions, with AO-based diagonal selection. The shader computes repeated texture coordinates from local positions, so a merged face preserves block-scale pixel textures. Flat pools and full-height waterfalls merge into large quads.

A preliminary exposed-face count reserves a bounded output capacity once. No result grows its vertex/index arrays during generation. A checkerboard chunk has 98,304 quads and about 5.25 MiB of mesh data; the test suite verifies complete coverage. The general cap is six faces per interior voxel. A fixed 16 MiB allowance per admitted worker covers the padded buffer, output, and the maximum local edit snapshot, with conservative extra headroom.

The texture array has seventy-five 16×16 RGBA layers with mipmaps, including six ore textures and animated water/lava. Grass and wood select different side/end layers. Rendering uses depth testing, back-face culling, chunk-frustum culling, near-to-far opaque chunk submission, and fog. Water and ice indices occupy the tail of the same mesh buffers and draw afterward, sorted by chunk from far to near, with alpha blending and depth writes disabled. Water is visible from both sides; ice renders exterior faces to prevent back-face compositing seams. Ice remains solid to gameplay and fluids. Lava is opaque with emissive shading. Underwater/lava camera positions use a tint and shorter fog range. No extra framebuffer, reflection map, shadow map, occlusion-query system, or terrain LOD is allocated. Transparent surfaces within a chunk are not sorted individually.

## Fluid simulation

Source blocks feed downward flow before finite horizontal spread. Water has seven horizontal levels and 100 ms neighbor delays; lava has three and 400 ms delays. Flow retracts when its supply disappears. Two neighboring water sources regenerate a source over solid support. Water touching a lava source forms obsidian; contact with flowing lava forms cobblestone. Solids block flow, and opening or closing paths wakes neighboring fluid cells.

The main thread owns simulation. A deduplicated queue holds at most 8,192 events within five-by-five chunk columns around the player, through the full world height. Each frame examines at most 128 events, checking a 1 ms execution target every eight due updates. This is a work/time target, not a hard real-time guarantee. Unchanged cells do not continuously propagate notifications. Untouched natural lakes/pools remain asleep until affected by edits.

Flow states are compact block IDs, so snapshots and saves preserve sources, levels, and waterfalls. On load, movement, and periodic recovery, the simulator scans saved edit positions incrementally (16 entries per frame) to wake local flows and disturbed natural fluids. Events outside the active area pause and are recovered on returning. Queue saturation increments a diagnostic counter and recovery revisits local edits; memory is bounded. The existing world-edit memory/entry limits also apply to simulation writes. Refused writes appear as `FLUID LIMIT` in the full HUD. Fluid changes invalidate the same chunk revisions and AO halos as manual edits. Fluid queue size, work counts, changes, refused writes, overflows, and update time are also exported to benchmark CSV.

## Streaming, ownership, and jobs

The main thread owns the world edit maps, chunk registry, graphics handles, and upload state. A bounded pool of one to eight worker threads owns generation/meshing while a job runs. Each slot holds at most one job/result. A semaphore sleeps idle workers; a mutex hands off jobs and results. Workers never call OpenGL.

Chunks receive unique increasing revisions. Edits invalidate every chunk touched by the edited voxel's AO halo, including diagonal neighbors at corners. Results whose coordinate was unloaded, recreated, or edited are discarded. Recreated coordinates receive new revisions, preventing an old job from overwriting a new resident.

Nearest incomplete chunks are scheduled first; remeshing an existing visible chunk receives extra priority. Distant chunks unload outside a one-chunk hysteresis margin. The render loop does not wait for generation. Completed meshes upload in pieces under a per-frame byte ceiling, then replace the old mesh. Both meshes count against the budget until the old one is retired.

Evicted/replaced GPU buffers can enter a best-fit reuse cache capped at 32 meshes and 2 MiB. Cached allocations remain charged to the graphics budget. Reuse accepts a buffer between one and two times the requested capacity. The vertex/index capacity ratio is fixed by quad geometry. Memory pressure drains the cache before reducing residency.

Shutdown wakes and joins all workers before freeing renderer resources. Both completed results and jobs stopped before execution release their snapshots. The allocator test exercises immediate shutdown and shutdown during generation, repeatedly checking for zero outstanding tracked allocations.

## Budget admission and failure behavior

Graphics admission uses allocated buffer capacities, not the live vertex count. It reserves 2 MiB for small renderer resources plus 16 bytes per framebuffer pixel for window color/depth storage, in addition to every mesh allocation. This framebuffer figure is deliberately conservative but remains an estimate of driver-owned storage, not a physical memory query.

A replacement is admitted before allocation. Upload staging remains in its bounded worker slot. GPU upload bytes are limited globally per frame, including partially uploaded large meshes. GPU timer queries are collected only when available; the renderer does not block for query results.

Increasing the framebuffer memory allowance restores deferred chunks and the configured radius. Memory pressure shortens the horizontal radius and releases distant geometry. At minimum radius, budget-driven eviction can defer additional chunks until movement or edits make them relevant again. A resized framebuffer reduces the available mesh budget, including at minimum radius. If the framebuffer itself cannot fit, the window size is reduced. If one mesh cannot fit, it is skipped and the diagnostic count increases. A driver allocation failure reduces the internal device allowance rather than requesting an unbounded retry.

CPU admission conservatively reserves worker scratch space before dispatch, including space for other busy slots. World-edit allocation leaves worker and save headroom. CLI memory values are bounded and incompatible settings fail before window creation. This is application allocation control, not a process-wide memory sandbox. OS stacks, runtime temporary arenas, allocator metadata, graphics-driver allocations, and deferred driver frees are outside the reported managed heap/VRAM totals.

## Local save format

`VOX2` is an explicit little-endian format; it never writes raw Odin struct padding:

| Field | Size |
|---|---:|
| Magic `VOX2` | 4 bytes |
| World seed | 8 bytes |
| Latest edit sequence | 8 bytes |
| Camera X/Y/Z as IEEE-754 doubles | 24 bytes |
| Distinct edit count | 4 bytes |
| Terrain generator version (1, 2, 3, 4, or 5) | 4 bytes |
| Each edit: signed X/Y/Z, block ID | 13 bytes |

The loader checks file size before reading the payload, validates exact length/count, finite/bounded player coordinates, block IDs, and editable height. It constructs a candidate world and swaps it in only after validation succeeds. An existing invalid save stops application startup; it is never silently overwritten. Writes go through a sibling temporary file and rename over the destination. This protects against ordinary interrupted writes; full power-loss durability/fsync is not implemented.

The header is 52 bytes. The loader also accepts original 48-byte-header `VOX1` saves with their original block IDs, assigns generator 1, and writes subsequent saves as `VOX2` with generator 1 retained. Existing generator 2 worlds remain version 2. New worlds use generator 5, and `--generator` can explicitly choose a supported version when creating a world. An explicit mismatch against an existing save is rejected. Unknown generator versions and invalid fluid states are rejected. Untouched generated terrain is not serialized; older binaries cannot open newer generator worlds or saves containing unsupported material IDs. Existing generator-3 worlds retain their original samples.

Generator 5 shares a prepared drainage set across padded terrain, plant anchors, formations, and caves. Its fixed-point profiles, strict downhill graph, protected beds, memory bounds, compatibility rules, and benchmarking entry points are documented in [geography](geography.md).
