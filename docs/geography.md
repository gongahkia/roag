# Generator 5: biome geography

New worlds use generator **5**. Existing saves retain generators 1–4 and their original terrain. Start a separate world:

```sh
python3 scripts/build.py run -- --save saves/geography-v5.vox --generator 5
```

The goal is recognizable geography with coherent terrain, water, and vegetation under the existing streaming budgets. Landforms are analytical approximations of geological results. The game does not simulate tectonic plates, moving glaciers, sediment transport, or a planet's climate history. Mushroom ecosystems and floating islands remain fantasy features.

## Landforms by biome

| Biome | Geography in v5 |
|---|---|
| Ocean | Continental shelves transitioning into deeper sediment-covered basins |
| Coast | Sandy low-relief shores; exposed rocky shores where mountain relief reaches the sea |
| Plains | Gentle, low-amplitude rolling ground with incised river corridors and sediment banks |
| Forest | Stronger rolling and dissected hills with deeper soil |
| Birch forest | Gentler wooded slopes and rounded hills |
| Cherry grove | Elevated foothill benches and broad, softly terraced slopes |
| Desert | Directional, asymmetric dunes with long windward ramps and shorter slip faces |
| Badlands | Terraced sedimentary benches, steep risers, layered terracotta, and river-cut canyons |
| Savanna | Broad rolling uplands interrupted by isolated rocky hill shapes |
| Jungle | Wetter, more deeply dissected hills with thicker soil and muddy river margins |
| Swamp | Humid low-relief basins near the water table, shallow ponds, and mud |
| Snowy plains | Low-amplitude cold plains with gentle surface undulations |
| Taiga | Rounded glacial hill shapes elongated along a common direction, with lakes and spruce forest |
| Mountains | Broad mountain chains, derivative-shaped ridges, exposed rock, and drainage valleys |
| Snowy peaks | Higher alpine ridges, broad-bottomed troughs, an altitude/temperature snow line, and high or cold frozen water |
| Mushroom fields | Rare volcanic island/dome shapes with summit depressions, rock beneath mycelium, and giant mushrooms |
| Sky islands | Retained bounded floating formations above ordinary, low-relief ground; rivers continue below them |

Temperature, moisture, continental position, and regional relief determine continuous profile weights. Shared warped noise fields drive the profiles, with extra work only where a profile needs it. Heights blend before block quantization. Choosing a new biome material does not select a disconnected height function.

The dune, badlands, and glacial shapes are inspired by the landforms described in [USGS eolian processes](https://pubs.usgs.gov/gip/deserts/eolian/), [NPS badlands geology](https://www.nps.gov/articles/000/badl-geologic-formations.htm), and [NPS glacial valleys](https://home.nps.gov/articles/ushapedvalleysfjordshangingvalleys.htm). These references inform visual shape choices; the implementation does not reproduce those physical processes.

## Rivers, lakes, and drainage

A jittered 128-block grid samples a coarse terrain potential. Every node chooses its lowest strictly lower neighbor. The strict decrease prevents cycles. Connected reaches share their endpoint positions and water grades, so adjacent chunks independently reconstruct the same network. Terminal inland nodes receiving flow become lakes; ocean reaches merge into sea-level water.

Each source with incoming flow continues downstream, while a bounded two-step rainfall/runoff estimate controls channel width. This is a local approximation, not the total area of an unbounded catchment. Some river systems terminate in closed inland lakes; reaching the ocean is not guaranteed. Wind and rainfall fields are spatial proxies, not seasonal weather.

Each reach has two bent segments, a graded bed, and banks. River corridors broaden with regional relief; terminal lakes have wider bowls. The bed and immediate shore are protected from cave carving so an untouched river cannot drain into a procedural cave. Adjacent underground lakes are checked against the same final surface. Mining these beds and banks still creates ordinary edits and wakes the existing bounded fluid simulation.

Generated water is an initial, mostly sleeping voxel state. It uses the existing source-fed water rules when disturbed. This does not model discharge, pressure, continuous river velocity, lake overflow, or erosion during play. Snow uses full blocks, and freezing is static at generation time.

## Cost and determinism

Generation uses Q16 integer fields and bounded descriptors. No persistent heightmap, biome texture, global erosion buffer, or growing drainage graph is allocated. A padded chunk prepares at most 100 coarse nodes and retains at most sixteen reaches, sharing them across columns, tree anchors, and cave/formation checks. Tree and drainage halos are included before meshing. All sampled block decisions remain functions of seed, generator version, and coordinates.

Workers still use one temporary 34³ block buffer and produce the same eight-byte vertices. GPU materials, texture layers, and the blended water/ice pass do not grow. The existing graphics, CPU, upload, residency, and edit admission limits remain active. The main-thread column/feature caches include the generator version.

Source responsibilities:

- `terrain_geography.odin`: climate weights, landform profiles, and dry/final columns.
- `terrain_drainage.odin`: bounded downhill graph, connected reach descriptors, beds, banks, and lake bowls.
- `terrain_geography_blocks.odin`: geology, soil, sediment, snow, ice, and cave sealing.
- `terrain_sampling.odin`: shared descriptors and versioned query caches.
- `benchmark_generation.odin`: matched-coordinate, display-free generation measurements.

## Reproducible validation

```sh
python3 scripts/build.py test
python3 scripts/smoke.py
python3 scripts/geography_benchmark.py --repeats 3
python3 scripts/geography_benchmark.py --skip-build --graphics --flight
python3 scripts/geography_benchmark.py --skip-build --graphics --wide
```

The headless benchmark alternates v4/v5 order over matching coordinates, after warmup. It checks deterministic digests across repeats and a 16 MiB worker heap ceiling. Its default generation regression threshold is 1.5× the previous generator's median total; `--max-generation-ratio` can change that threshold. Meshing time is reported separately because the terrain geometry changes. Timing thresholds depend on host load and are not part of the native unit test suite.

The graphical suite checks all seventeen biomes, rivers, lakes, and caves. Stress scenes use 640×360, radius two, 16 MiB application graphics reservations, 32 MiB managed CPU, and 64 KiB uploads. Wide previews use 960×540, radius four, and 64 MiB graphics/CPU allowances. `--flight` checks memory/residency over two repeated route cycles. Linux runs use a private Xvfb display with software OpenGL when available. A software-renderer run does not establish physical GPU memory use or hardware frame rates.

Reports are saved under `benchmarks/geography-v5-{stress,wide}.json`; CSV files preserve the frame/job measurements. Images are `build/geography-v5-{scene}.png`. Native `--benchmark river`, `lake`, and `ocean` scenes join the existing biome previews; `--no-hud` produces unobstructed screenshots. The default gameplay HUD is unchanged.

The v5 tests cover all biome distributions, relative terrain elevations, exact worker/query equality including padded borders, seventeen golden chunk hashes, versioned caches, strict downhill node potentials, canonical shared reaches, graded water, lake containment, ordinary edits, 128 spawn seeds, descriptor bounds, extreme coordinates, and concurrent workers. Older generator hashes and save roundtrips remain in the same suite. See [performance](performance.md) and [measurements](measurements.json) for measured results and platform coverage.
