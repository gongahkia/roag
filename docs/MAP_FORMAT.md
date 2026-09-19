# Terrain templates — schema 2

Extension `.dwmap.json`, UTF-8 JSON. The writer emits version2; version1 still reads.
Map files are data only. Imports do not call a generator or evaluate Lua/bytecode.
Full colony saves are separate envelopes, not interchangeable with this schema.

## Top-level fields

`format="deepward-map"`, `version=2`, printable `title`1..96, width/height, seed,
printable short `preset`, aligned `arrival={left,right,floor}`, material and biome
layers, optional recipe and optional features. Unknown object keys are rejected.
Dimensions are multiples of4, width128..512, height80..256. The outer two cells are
bedrock. Start geometry must fit the requested workers and supply footprint.

Each layer is `{palette=[names...], runs=[palette_index,count,...]}` in row-major
order. Palette indexes start at1. Counts are positive integers; expanded count must
exactly equal area. No clipping/repair of malformed RLE or implicit rows occurs.
Palette names are stable; numeric engine IDs are not the interchange contract.

Materials: air, bedrock, rock, soil, sand, water, ore, lava, steam, ice.
Biomes: unclassified, rime, dunes, shale, loam, aquifer, iron, basalt, vault,
mycelium, glass, ossuary, vent. Unknown names reject. Subset/reordered palettes are
accepted. Empty/sparse palettes and count overflow reject before expansion.

## Recipe metadata

`version`, `layout`, `climate`, `openness`, `biomeScale` retain their v1 meanings.
Schema2 optionally adds `features` (living/ruins/none), `density`0.5..1.5, and crew3/6/9.
Recognised current generation is frontier-v2; frontier-v1 regeneration remains in its
preserved module. Unknown recipe identifiers can still be kept as valid metadata;
the loaded cells are authoritative. Regeneration is a separate explicit operation.

## Optional feature template

`features` is an object with exactly:

- `version:1`, `crew:3|6|9`;
- `flora`: array, at most128 `{kind,x,y,food,water,phase}` records;
- `fauna`: array, at most64 `{kind,x,y,food,phase}` records;
- `sites`: array, at most48 `{kind,x,y,stock,phase}` records;
- `ruins`: array, at most32 `{kind,x1,y1,x2,y2,name}` records.

All placements use fine-cell integer coordinates. Phase is0..3599, flora food0..12,
water0..6; fauna food0..24. Site stock may contain stone, soil, metal, food and water,
each0..1000. Ruin bounds are ordered and in-map. Names are printable ASCII up to80.
Known kinds come from `src/catalog.lua`; arbitrary scripts/properties are rejected.
JSON bounds:6MiB, depth24,600000nodes,4096-byte strings plus tighter field rules.
This is defensive validation, not an independently audited hostile-input boundary.

Import creates fresh workers/supplies, instantiates baked encounters, and records
new starting budgets. It does not copy original IDs, health, orders, fuses, activity,
knowledge or living history. Encounter positions/stock/phase/crew are preserved;
creature health and machinery activation initialise from the current rule version.
A template without features creates the original three-worker, ecology-free start.

F2 exports the expedition's initial template. Shift+F2 exports the selected current
terrain and surviving encounter records as a new start: placed buildings, carried
items, resource piles, bodies, growth in farms, signal history, knowledge and jobs
are excluded. Creature wounds reset; dormant-state rules initialise again. A destroyed
arrival can reject export. These semantics are intentionally not save/restore.
Use F5/F6 when the actual settlement's full state must survive.

Both versions round-trip their material/biome cells. A version1 file re-exported by
this writer can have a version2 envelope and an expanded canonical biome palette;
that does not change its expanded cells. Compare arrays, not only JSON spelling.
The printed rolling fingerprint is diagnostic, not a cryptographic authenticity check.
