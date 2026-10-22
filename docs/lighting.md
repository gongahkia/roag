# Voxel lighting

Lighting is enabled by default, including in existing worlds. Skylight reaches
open surfaces, is blocked by roofs and terrain, and spreads into entrances.
Enclosed caves are dark. Leaves and ice attenuate light; water absorbs more with
depth. Lava sources and flowing lava illuminate their surroundings with warm
light. A solid **Lamp** is available on **palette page 5, slot 7** (four Tab presses
from page 1, then 7). Mine or place blocks as usual to change the lighting.

The settings menu offers **Full**, **Fast**, and **Off** lighting. Full retains
the original illumination. Fast keeps direct vertical skylight, roof shadows,
and propagated lamp/lava light, but omits indirect skylight scattering. Off uses
uniform daylight for the diagnostic baseline. `--lighting full|fast|off` selects
the initial mode; the Potato preset chooses Fast. Switching modes invalidates
resident revisions and replaces meshes asynchronously within existing budgets.

Lighting is derived from the generator and saved block edits. It needs no saved
light grid, save migration, or new terrain generator version. Lamp IDs are
appended; existing block IDs and generated terrain stay unchanged.

## Rendering and memory

Each light sample contains independent four-bit skylight and block-light
channels. Direct sunlight keeps its strength through air; each scattered step
costs at least one level. Level-15 emitters therefore have a finite range.
Face vertices average neighboring light samples, avoiding diagonal light leaks
through two opaque sides. Ambient occlusion remains for contact shadows. The
shader combines cool daylight and warm emitted light and keeps unlit cave fog
dark.

Vertices remain **eight bytes**. The low six bits of X hold the local position;
the next eight hold both light channels. Material, face, AO, and fluid-height
packing stay intact. Light gradients participate in greedy-mesh compatibility,
so varying light can increase the number of quads. There are no additional GPU
light volumes, textures, shadow maps, framebuffers, or rendering passes.

Each worker uses a transient 64³ neighborhood, including a 16-cell halo around
its 32³ chunk. This contains every nonzero scattered-light contributor to the
mesher's sampling halo. Column metadata includes all natural roofs and trees;
saved roof edits above unloaded chunks are included up to world height 255.
The sampler skips guaranteed empty sky and evaluates underground voxels lazily
when reached by light. Natural lava's known height range seeds underground
emission, and edited emitters seed at any height. New natural emissive materials
must extend this source enumeration.

A fixed ring queue permits at most one queued entry per voxel. All lighting
scratch is released before mesh allocation. Resident chunks keep only their
packed meshes. Worker memory remains within the existing 16 MiB reservation;
the tests also exercise the maximum edit snapshot with checkerboard geometry.

The flood starts at light frontiers rather than queueing uniform sunlight.
It rejects neighbors that cannot improve even at minimum absorption before
sampling procedural blocks. Full-mode mesh digests match the original solver
over the benchmark fixture matrix. Fluid edits within a frame share one
invalidation revision per affected chunk, avoiding repeated invalidation work.
World edits use separate allocation tracking within the total CPU budget, so
concurrent worker scratch cannot incorrectly refuse block or fluid edits.

## Updates and limits

Block and fluid edits invalidate nearby chunk revisions and affected chunks
below them. Workers rebuild those local lighting neighborhoods from immutable
snapshots, handling both added and removed sources. Old mesh/light results cannot
overwrite a newer revision. The render thread continues drawing the previous
mesh until its replacement has finished uploading. Busy scenes can therefore
show a short delay while geometry and lighting catch up. After a job is made
stale by repeated edits, scheduling waits for a 50 ms quiet period for that
chunk, allowing unaffected terrain to load instead of repeatedly discarding
work on the closest chunk. Ordinary first edits start immediately.

This is Minecraft-style voxel illumination. Sunlight travels vertically, with
attenuated six-neighbor scattering. It does not simulate angled moving sun
shadows, colored light transport, reflected global illumination, bloom, or a
day/night cycle. Light values are deterministic; the graphics palette supplies
the warm/cool colors. It is compatible with the project's future peer-to-peer
direction, but networking itself is not implemented.

## Reproducing checks

```sh
python3 scripts/build.py test
python3 scripts/smoke.py
python3 scripts/lighting_benchmark.py --graphics --flight
python3 scripts/build.py run -- --benchmark light-room --seconds 30
```

The `light-room` benchmark is a fixed-camera visual check; it disables mouse
look and WASD. For interactive play, launch without `--benchmark`, for example
`python3 scripts/build.py run -- --fps-limit 60`. Lamps are available in the
normal creative palette, including in existing worlds.

The headless benchmark alternates lit/unlit jobs at identical coordinates, with
warmup and repeated samples spanning all 17 biomes and underground slices. It
checks deterministic mesh digests and the worker memory ceiling. Graphical
comparisons use 640×360, radius 2, 16 MiB graphics reservations, 32 MiB managed
CPU, and 64 KiB uploads. `--no-lighting` provides a diagnostic baseline. F3 and
benchmark CSVs include separate lighting time; measurements and limitations are
recorded in [performance](performance.md) and [measurements](measurements.json).
