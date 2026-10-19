# Terrain generation

New worlds use generator **5**. Existing saves keep generator 1, 2, 3, or 4, including their original terrain, caves, and trees. Saving an old world does not upgrade its generator.

Start a separate world to see the new terrain:

```sh
python3 scripts/build.py run -- --save saves/geography-v5.vox --generator 5
```

`--seed N` selects a new world's seed. A save's recorded seed remains authoritative. `--generator 1|2|3|4|5` can select a comparison generator when creating a world; an explicit version conflicting with an existing save stops startup without writing that save. Without this flag the recorded version is loaded normally.

Generator 5 adds biome-specific landforms, directional dunes, terraced badlands, alpine troughs, volcanic mushroom islands, and connected downhill rivers/lakes. See the [geography guide](geography.md) for the biome table, algorithm limits, and matched-coordinate benchmarks. The sections below document the retained v3/v4 foundations.

## Landscapes and biomes

The world remains 256 blocks tall, split into 32³ chunks, with sea level 52. A broad continental field determines ocean and land, a separate mountain field supplies relief, and a moisture field varies forest and sandy regions. Generator 3 uses seven region labels: ocean, coast, plains, forest, desert, mountains, and sky islands. Generator 4 adds the climate biomes below. Heights blend continuously before rounding to blocks; surface materials and vegetation distinguish the regions.

Four noise octaves use decreasing amplitudes, increasing frequencies, and rotated domains. Analytical value-noise derivatives are transformed back into the base coordinate frame and accumulated to attenuate finer detail. This suggests erosion visually; it does not simulate rainfall, sediment transport, or river drainage. The accumulated gradient is a shaping/surface roughness signal, not the exact derivative of the final attenuated function.

Generator 3 uses Q16 fixed-point arithmetic with 64-bit intermediates for block decisions, including the noise, rotation, formation geometry, and cave distances. The old floating-point generators remain separate. Generator 4 uses the same integer primitives. Native CI tests assert the same golden chunk hashes on every platform; running only a cross-compiler does not establish native runtime agreement.

Ordinary ground heights are bounded to 18–189. Forests are denser than plains, sandy regions lack trees, and steep/high mountain ground exposes stone. Existing materials, ore depth rules, textures, and fluid states are reused.

## Generator 4 biomes and materials

Independent low-frequency temperature and moisture fields select coherent climate regions. A smaller variation field separates groves and forest types; a rare separate field selects mushroom fields. Continuous wetland and mesa masks adjust heights before integer rounding, so terrain does not jump at a material boundary. This is an original climate model, not Minecraft's biome algorithm. Ground is bounded to heights 18–211.

| Biome | Surface and vegetation |
|---|---|
| Ocean / coast | Sand, water; cold surface water freezes into ice |
| Plains / forest | Grass and oak trees, with different tree densities |
| Desert | Sand and cactus patches |
| Mountains | Exposed stone and sparse vegetation |
| Snowy plains | Snow-covered open ground |
| Taiga | Snow, buried podzol, tall tapered spruce trees |
| Savanna | Golden dry grass and bent, flat-canopy acacias |
| Jungle | Lush grass and tall, broad-canopy jungle trees |
| Swamp | Muted grass, mud, shallow ponds, and oak trees |
| Badlands | Raised mesas with white, red, yellow, and natural terracotta bands; cacti |
| Birch forest | Pale patterned birch trunks and light green canopies |
| Cherry grove | Broad pink canopies and cherry wood |
| Mushroom fields | Purple mycelium and giant red or brown mushrooms |
| Snowy peaks | High snowy stone summits |
| Sky islands | Existing floating grass islands above ordinary ground |

All new materials are mineable, placeable, and saved, including in older worlds. **Tab** cycles five palette pages; **1–9** select a slot. See [materials](materials.md) for the slot layout. Snow, cacti, trunks, leaves, and mushroom caps use full voxel blocks. Ice is a solid translucent block: it blocks movement and fluid flow. Freezing happens during generation; snow layers, weather, melting, ice slipperiness, cactus damage, flowers, and mobs are not simulated.

Vegetation uses bounded immutable descriptors, with species-specific trunks and canopy shapes rather than a saved block array. A one-cell horizontal halo covers the widest four-block canopy; a sixteen-block vertical bound includes the tallest jungle tree. Preparation and sampling use the world's generator version. Column and feature cache keys include that version, preventing stale data when a world is loaded or a comparison changes generators.

The material field expands from five to six bits **within the same eight-byte vertex**. A 75-layer 16×16 RGBA texture array (about 100 KiB including mipmaps) supplies distinct grass, bark, leaves, mushroom, snow, ice, cactus, and terracotta textures. Ice shares water's existing blended draw pass. No extra framebuffer or world-sized climate textures are allocated.

## Fantasy terrain

One in twelve 512-block region cells is eligible for a sky-island biome. A blended circular influence flattens its underlying ground to a lowland landscape. The ground remains present: falling from an island returns the player to the ordinary world.

Within selected regions, 128-block cells can contain irregular ellipsoid-footprint islands. Tops are broad, undersides taper, interiors are stone, and surfaces use grass/dirt and sparse trees. Island tops are below 220; trees and formations remain below 240. Fixed feature radii prevent neighboring island volumes from overlapping horizontally. Eligibility checks the whole island footprint against the lowland region so a mountain cannot join its underside. Island edges can extend into a biome's blend area.

Elsewhere, one in sixteen 128-block cells attempts an arch, overhang, or spire. Terrain height, water, and sky-region checks reject unsuitable locations; arches additionally check their two footings. These landmarks use bounded analytic shapes rather than region-scale erosion simulations or random walks. Everything is addressed by world coordinates, so formations can cross chunk borders without depending on generation order.

Use the existing creative building controls or F4 spectator flight to reach floating islands. The island traversal controls are unchanged.

## Caves and lakes

64-block spatial cells own jittered tunnel junctions. Canonically hashed shared faces give neighboring cells identical connection points. Two capsule segments create each bent tunnel, with radii 2–4 blocks. Some cells also contain ellipsoid caverns, and selected near-surface cells create an entrance. The network has branches and dead ends; it does not promise every underground chamber connects to every other chamber.

Selected deeply buried chambers contain water lakes. A two-block rock shell below the waterline takes precedence over all cave carving, including neighboring tunnels. Natural water starts above the lava zone. Lakes are initially contained, but their walls can be mined normally; existing bounded fluid updates then propagate water through the opening. Deep tunnel/cavern cells at heights 6–12 contain lava. Bedrock and the bottom protective layers remain solid.

Untouched natural water stays asleep until nearby changes notify the fluid system. Generation does not settle the whole world's fluid network or create a permanent dense array of water blocks.

## Sampling, spawning, and performance

Workers build one temporary 34³ block buffer, including the one-block neighbor border. Each job prepares its tree, formation, and cave descriptors once and shares column calculations down each vertical column. Bounding-box checks reject expensive geometry early. The main thread uses bounded caches of 256 procedural columns and eight chunk feature sets for collision and targeting. Edits are always checked before cached procedural data.

Spawn selection searches at most 1,089 positions within a 1,024-block-wide area for dry ground, gentle local height variation, support, and body clearance. If no site works, a 3×3 platform and headroom clearing are saved as 36 ordinary edits. Existing saves retain their camera position and do not run this search. The generator-3 test covers 256 spawn seeds; generator 4 additionally covers 128 seeds. The fallback is checked separately.

Generation remains on CPU workers; OpenGL 3.3 rendering uses the existing compact greedy voxel meshes. No compute shaders, persistent regional heightmaps, DLA, hydraulic erosion, or world-height increase is required. Sparse saved edits, memory admission, automatic radius reduction, and upload limits remain active.

## Validation and previews

```sh
python3 scripts/build.py test
python3 scripts/smoke.py
python3 scripts/benchmark.py --scene terrain --generator 3 --seconds 10
python3 scripts/benchmark.py --scene sky --generator 3 --seconds 10
python3 scripts/benchmark.py --scene caves --generator 3 --seconds 10
python3 scripts/benchmark.py --scene flight --generator 3 --seconds 52
```

The new benchmark scenes locate reproducible mountains, an island, and a lake chamber from the selected seed. Their camera/target coordinates are printed at startup. Smoke previews are written to `build/terrain-v3-{terrain,sky,caves}.png`. The CSV/JSON reports include generator version, completed job counts, mean generation/meshing time, and peak generation time in addition to memory, frame percentiles, uploads, and fluid counters. Job aggregates describe completed resident uploads; canceled/stale work is not included in those totals.

Additional fixed-view scenes are `plains`, `forest`, `desert`, `coast`, `arch`, `overhang`, and `spire`. Use `--radius 4` or higher for wider previews; the radius-2 stress scenes intentionally fog distant terrain to match their shorter draw distance.

Tests cover analytical noise derivatives, extreme/negative coordinates, all region/landmark types, 256 spawn seeds, fallback clearance, lake containment and wall mining, island collision, generated-water swimming, shared cave ports, feature edges, worker/direct sampler equality, visitation order, concurrent workers, golden block hashes, and legacy saves. Software-rendered smoke tests check the 16 MiB application graphics and 32 MiB managed CPU budgets. These counters exclude driver/runtime allocations; physical low-memory GPU validation is still required.

For generator-4 biome previews:

```sh
python3 scripts/benchmark.py --scene birch --generator 4 --seconds 8 --radius 4
python3 scripts/benchmark.py --scene cherry --generator 4 --seconds 8 --radius 4
python3 scripts/benchmark.py --scene mushrooms --generator 4 --seconds 8 --radius 4
python3 scripts/benchmark.py --scene snowy-peaks --generator 4 --seconds 8 --radius 4
```

The other new scene names are `snowy-plains`, `taiga`, `savanna`, `jungle`, `swamp`, and `badlands`. The original terrain/island/cave/landmark scenes also accept generator 4. Fixtures locate regions from the selected seed and print their coordinates. Generator-4 tests check every biome's worker buffer against main-thread queries, 17 golden padded-chunk hashes, all vegetation kinds and face connectivity, tall canopy boundaries, ice over water, extreme coordinates, concurrent worker equality, six-bit materials, palette bounds, and new-material save/load across all four supported generators.

Local validation images are written to `build/biome-preview-v4-{scene}.png`. The final v4 snapshot includes 51 passing core tests, all ten new biome scenes within the 16/32 MiB stress allowances, and a 52-second repeated flight with stable managed-memory peaks. See [performance](performance.md) and [measurements](measurements.json) for actual settings and accounting limits.
