# Generation experiments

The `frontier-v1` pipeline is:

```text
seed + dimensions + biome profile + region scale
  -> depth-biased, jittered region sites with warped boundaries
  -> immutable per-cell geological region field

seed + dimensions + layout + openness + region field
  -> open-space mask
  -> material lenses / ore distribution
  -> finite pockets / small silhouette features
  -> closed bedrock boundary
  -> centred arrival chamber and standard colony
```

No stage repairs the entire world to ensure connectivity or a recoverable colony.
The fixed starter makes early interaction possible; surrounding resources can still
be inaccessible or dangerous. The rules for erosion, gravity, water, jobs, injuries,
food and death are not replaced by biome-specific hidden shortcuts.

## Layouts

| Identifier | Mechanism | Intended visual distinction |
|---|---|---|
| noise | Warped multi-octave value-noise density | Broad irregular connected shapes and masses |
| cellular | Buffered 4-pass cellular automaton on a half-resolution grid | More cellular, clustered cave silhouettes |
| worms | Correlated walkers with variable ellipse carving | Winding connected tunnels and widened pockets |
| faults | Wavy strata galleries with vertical fissures | Long shelves, seams and shafts |
| vaults | Elliptical halls, connecting cuts and retained pillars | Larger chambers and columns |
| hybrid | Choose from the five masks using each cell's biome | Spatially different geometric character in one map |

These are five hand-written layout techniques plus a combination strategy. They
share geology, pocket placement, noise/random primitives, and arrival construction.
They are not six independent physics systems and are not claimed as novel algorithms.
The noise here is VALUE noise, not mislabeled Perlin/simplex noise.

## Geography independent of layout

Biome placement uses 3 depth bands and roughly 3..6 lateral region sites per band.
Site types are selected from depth-biased profiles; the seed rotates the lateral
arrangement. Noise warps region boundaries before nearest-site assignment. Depth is
not the only input, so exploring horizontally also crosses geological regions.

Balanced, arid, frozen and volcanic are generation profiles, NOT dynamic weather.
Region scale influences horizontal site count and warp scale. It does not represent
physical metres or tune a validated ecological model. Every profile has hazards and
finite resources; no every-biome-on-every-map guarantee is made.

Changing ONLY layout retains the same seed-derived biome geography. This makes
visual/metric comparisons more useful than repeatedly changing all settings together.
Changing the climate/profile, size, seed, or region scale may change geography too.
Pockets and the starter also affect final void counts, so `openness=.48` does not
mean exactly 48% air.

## Repeatable CLI workflow

Commands run from the repository root. Output directories must exist. Existing
output files are refused rather than overwritten. Example:

```sh
mkdir -p experiments
lua tools/map.lua generate experiments/noise.dwmap.json --seed 12345 --layout noise --climate balanced --width 256 --height 160
lua tools/map.lua inspect experiments/noise.dwmap.json
lua tools/map.lua regenerate experiments/noise.dwmap.json experiments/worms.dwmap.json --layout worms
lua tools/map.lua compare experiments/noise.dwmap.json experiments/worms.dwmap.json
lua tools/map.lua roundtrip experiments/noise.dwmap.json experiments/noise-copy.dwmap.json
```

Drag either map into LÖVE or copy it into the save directory's `maps/` and press F3.
Imports use exact cell data. `regenerate` is the explicit exception that reruns the
recipe. To choose a different seed or dimensions, use `generate` rather than trying
to silently alter them during an import.

Other flags: `--openness 0.25..0.70`, `--biome-scale 0.65..1.5`, and
`--preset cistern|dunes|frost` for old scenarios (frontier-only flags are ignored there).
All terrain algorithms and the JSON/map codec are in repository source.

## Measurements

`inspect` reports per-material and per-biome counts, air fraction, four-neighbour
AIR components and largest-air-component fraction, and adjacent horizontal/vertical
biome transitions. Solid region boundaries and disconnected air voids are allowed.
These are descriptors, not plausibility scores or guarantees of worker access.

`compare` requires equal dimensions and counts differing material and biome cells.
It does not declare a winner. The included six maps provide a same-seed example of
holding geography fixed while varying layout.

`lua tools/generation_benchmark.lua experiments/results.csv` runs 6 layouts x 2 sizes
x 3 seeds, with one discarded warm-up per layout/size. Shift+F8 runs that experiment
inside LÖVE while leaving the live colony paused. CSV and manifest explain timing
boundaries, GC policy, and units. The 36 cases use the balanced profile, not every
possible climate or tuning combination. Edit the benchmark options for wider sweeps.

Generation includes world allocation, region assignment, geometry/materials, and
starting colony placement. Serialization covers packing, schema validation and JSON
encoding. Deserialization covers JSON parsing, schema checking, RLE expansion and
fresh starter placement. The measured import is deliberately NOT just a file read.
Disk I/O, descriptors, hashes, explicit collection and drawing are outside timers.

Normal GC remains enabled. Memory is a post-collection retained Lua-world delta,
not peak allocation, process RSS, graphics memory, or checkpoint cost. CLI CPU time
and LÖVE elapsed time are different measurements. A single warm-up does not establish
JIT steady state. No FPS or speedup promise is derived from these exploratory runs.

## Boundaries and extension points

Worlds remain finite, 128x80..512x256, in multiples of four. All cells are generated
at expedition start. This update does not implement chunk streaming, fog-of-war,
new simulation materials, temperature, fauna, music, or biome-specific production.

To add a layout: register it in `src/generation/layouts.lua`, keep the signature,
then add repeatability/geometry tests. To add a biome: update its registry, selection,
material pass, hybrid policy, map palette and schema compatibility deliberately.
Unknown map schema versions/material names must not silently fall back to rock.

Future recipe revisions should get new generator identifiers. Retain baked map
loading separately, so an old experimental map need not depend on an old generator.
