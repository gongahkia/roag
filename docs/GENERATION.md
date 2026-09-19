# Generation experiments in 0.4

Current recipe `frontier-v2` owns terrain construction, geological regions and baked
encounter placement. The original `frontier-v1` path and example maps are preserved.
`src/generate_legacy.lua` retains cistern, dunes and frost. No existing expedition is
regenerated on load. Visual examples are comparison inputs, not guaranteed safe starts.

## Parameters

Seed 0..2147483646; dimensions multiples of four, width128..512 and height80..256.
Layout: hybrid, noise, cellular, worms, faults, vaults, karst, labyrinth, roots,
chasms, crystal. Profile: balanced, arid, frozen, volcanic, overgrown, ruined, abyssal.
Openness 0.25..0.70, region scale0.65..1.5. Features living/ruins/none, density0.5..1.5,
crew3/6/9. UI exposes a few representative values; CLI supports the stated ranges.

Geology is seeded separately from geometry so changing only layout preserves its
region field. Openness is an input, not a promised traversable/air fraction. Ruin
placement has bounded attempts and starter/overlap exclusions; some maps have fewer
than the target. Crew footprints are checked, not universal access or survivability.
All fine cells continue simulating when outside the current camera.

## CLI

```sh
mkdir -p experiments
lua tools/map.lua generate experiments/roots.dwmap.json --seed 12345 --layout roots --climate overgrown --crew 6 --features living --density 1.0
lua tools/map.lua inspect experiments/roots.dwmap.json
lua tools/map.lua roundtrip experiments/roots.dwmap.json experiments/copy.dwmap.json
lua tools/map.lua regenerate experiments/roots.dwmap.json experiments/karst.dwmap.json --layout karst
lua tools/map.lua compare experiments/roots.dwmap.json experiments/karst.dwmap.json
```

Existing files are not overwritten by these commands. `regenerate` is explicit and
separate from import. A v1 recipe remains on v1; it does not accept new v2-only options.
`compare` measures cell differences/descriptors, not a terrain quality score.

Shift+F8 benchmarks generation, JSON serialization and parsing/start placement with
an isolated matrix. CLI equivalent: `lua tools/generation_benchmark.lua output.csv`.
Default: 11 layouts x2 sizes x3 seeds =66 measured worlds plus warm-ups. All restored
cell arrays and encounter templates are compared directly. Tests are outside timing.
Rows include actual counts, not a promise that every requested archetype will occur.

CLI CPU seconds and LÖVE elapsed seconds are not interchangeable. Retained Lua heap
is not peak/RSS/GPU. One warm-up does not establish JIT steady state. These tests do
not measure the frame rate of a populated large settlement.
