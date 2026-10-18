# Architecture and limits

## World and creative edits

World positions are signed integer block coordinates; floor division handles negative chunk coordinates. Chunks are 32³, the vertical range is eight chunks, and horizontal terrain is generated on demand. Camera-relative float rendering avoids losing local precision far from the origin. The camera is bounded to ±100 million blocks horizontally.

Seeded interpolated height fields create hills and mountains; a continuous 3D value field carves underground caves. Tree positions come from seeded spatial cells. The worker shares terrain-height and tree calculations down a column. The random-access world query and the padded worker sampler must agree, including negative coordinates and chunk borders.

Untouched chunks have no persistent dense block allocation. The world stores only edited positions, bucketed by chunk. Reads overlay these edits on procedural terrain. A mesh job snapshots edits from the 27 touching buckets, restricted to its 34³ padded sampling region. Job generation needs no mutable access to the live world.

`Edit_Command` carries a monotonically increasing `u64` sequence, integer position, and block ID. `world_apply` rejects stale sequences, invalid block IDs, protected bottom-layer edits, out-of-height edits, and edits beyond memory/entry limits. There are at most 262,144 distinct edited positions. New edits are also refused when application heap use approaches the configured CPU allowance after worker/save headroom. Re-editing a position replaces its saved value.

`world_get`, `world_apply`, `world_snapshot`, and the generation functions contain no graphics or networking calls. Their representations are internal Odin APIs, not a network wire protocol. Future P2P work can translate validated network commands into this interface.

## Mesh generation

Each worker samples one 34³ block buffer: a 32³ interior with a one-voxel halo. Only solid-to-air faces render. Face direction, material, and four vertex ambient-occlusion samples determine compatibility. Rectangular faces merge greedily when shading is uniform and compatible. Nonuniform AO faces stay separate to preserve shading.

Vertices are eight bytes: three local `u16` positions and a packed face/material/AO field. Four vertices and six 32-bit indices form each quad. Triangle winding faces outward for all six directions, with AO-based diagonal selection. The shader computes repeated texture coordinates from local positions, so a merged face preserves block-scale pixel textures.

A preliminary exposed-face count reserves a bounded output capacity once. No result grows its vertex/index arrays during generation. A checkerboard chunk has 98,304 quads and about 5.25 MiB of mesh data; the test suite verifies complete coverage. The general cap is six faces per interior voxel. A fixed 16 MiB allowance per admitted worker covers the padded buffer, output, and the maximum local edit snapshot, with conservative extra headroom.

The texture array has ten 16×16 RGBA layers with mipmaps. Grass and wood select different side/end layers. All blocks are opaque. Rendering uses depth testing, back-face culling, chunk-frustum culling, near-to-far chunk submission, and fog. There is no occlusion-query system, compute dependency, shadow map, transparent pass, or terrain LOD in this tranche.

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

`VOX1` is an explicit little-endian format; it never writes raw Odin struct padding:

| Field | Size |
|---|---:|
| Magic `VOX1` | 4 bytes |
| World seed | 8 bytes |
| Latest edit sequence | 8 bytes |
| Camera X/Y/Z as IEEE-754 doubles | 24 bytes |
| Distinct edit count | 4 bytes |
| Each edit: signed X/Y/Z, block ID | 13 bytes |

The loader checks file size before reading the payload, validates exact length/count, finite/bounded player coordinates, block IDs, and editable height. It constructs a candidate world and swaps it in only after validation succeeds. An existing invalid save stops application startup; it is never silently overwritten. Writes go through a sibling temporary file and rename over the destination. This protects against ordinary interrupted writes; full power-loss durability/fsync is not implemented.

The file captures the current generator implicitly through its format version. A change that alters terrain generation must introduce a migration/version decision before using existing saves or network peers. Untouched generated terrain is not serialized. No cross-version compatibility promise exists yet.
