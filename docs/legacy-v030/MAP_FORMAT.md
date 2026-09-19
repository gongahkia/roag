# Deepward terrain template, schema version 1

Extension: `.dwmap.json`. Encoding: UTF-8 JSON. Serialization is separate from full
colony `run.dat` / `.dw` persistence. No `load`, `loadstring`, evaluator, or Lua code
execution is used to read a map. JSON syntax is implemented locally in `src/json.lua`.

## Fields

| Key | Required | Meaning |
|---|---|---|
| format | yes | Exact string `deepward-map` |
| version | yes | Integer 1 |
| title | yes | 1..96 printable ASCII bytes |
| width, height | yes | Multiples of 4; width 128..512, height 80..256 |
| seed | yes | Integer 0..2147483646; provenance |
| preset | yes | 1..32 ASCII alphanumeric/underscore/hyphen label |
| arrival | yes | Object with left, right, floor fine-cell coordinates |
| material | yes | Named palette and row-major RLE data |
| biome | yes | Named palette and row-major RLE data |
| recipe | no | version, layout, climate, openness, biomeScale |

Unknown object keys are rejected in this schema, rather than silently executing or
preserving arbitrary rules. There are no worker, inventory, structure, or script fields.
JSON object keys must be unique. Encoded output sorts keys for stable bytes.

The following is a STRUCTURAL EXCERPT, not a complete loadable map:

```json
{
  "format": "deepward-map",
  "version": 1,
  "title": "My caves",
  "width": 256,
  "height": 160,
  "seed": 12345,
  "preset": "frontier",
  "arrival": {"left": 97, "right": 160, "floor": 73},
  "material": {"palette": ["air", "bedrock", "rock"], "runs": [2, 512]},
  "biome": {"palette": ["unclassified"], "runs": [1, 40960]},
  "recipe": {
    "version": "frontier-v1", "layout": "hybrid", "climate": "balanced",
    "openness": 0.48, "biomeScale": 1.0
  }
}
```

The material runs in that excerpt deliberately do NOT fill 40960 cells and will be
rejected. Use `maps/examples/*.dwmap.json` for complete valid documents.

## Row-major run-length encoding

Index `(y - 1) * width + x`, one-based x/y. Scan left-to-right, then top-to-bottom.
`runs` alternates `[palette_index, count, palette_index, count, ...]`.
Palette indices are **one-based**, unlike the engine's zero-based material IDs.

Example with palette `["air", "rock"]`: `[2,3,1,2]` expands to three rock cells then
two air cells. There is no special row delimiter; a run may cross a row boundary.
Every count is an integer 1..area. Expanded counts must total exactly width*height.
All run counts and palette references are checked BEFORE expanded allocation.

Allowed material names:
`air`, `bedrock`, `rock`, `soil`, `sand`, `water`, `ore`, `lava`, `steam`, `ice`.

Allowed biome names:
`unclassified`, `rime`, `dunes`, `shale`, `loam`, `aquifer`, `iron`, `basalt`, `vault`.

Palettes may be subsets and reordered. Names must be recognised and nonduplicated.
The writer emits complete canonical palettes; input palettes are not tied to numeric
engine IDs. Biomes are static location metadata, independent of mobile material.

## Geometry and spawn validation

The outer two fine-cell rows/columns must be bedrock. There is no implicit repair
or clipping of an invalid file. The arrival marker must align to the 4-cell building
grid and fit within bounds. All three standard workers must have empty 2x3 footprints
and solid initial footing; the initial stockpile/supply space must also be empty
and supported. Validation is intentionally NOT an escape-route or survivability test.

After validation, `Map.toWorld` expands the exact material/biome arrays and places
standard settlers and supplies using `src/expedition.lua`. It does not stamp a new
cave, inject a reservoir, or call any generator. Budget baselines are subsequently
captured by the normal history/simulation initialization path.

## Recipes and compatibility

The actual cell layers are authoritative. Import succeeds with unknown recipe
identifiers provided their schema is valid: recipes are metadata, not executable
code. `tools/map.lua regenerate` is a SEPARATE, explicit command; it currently needs
`frontier-v1` and supported layout/profile identifiers. A future generator update
can leave old baked maps loadable even if it no longer regenerates the same seed.

Recipe openness must be 0.25..0.70; biomeScale 0.65..1.5. Recipe identifiers are
printable ASCII strings of at most 32 bytes. Unknown schema versions are rejected;
no automatic migration or cross-game format import is implemented.

The printed fingerprint is the existing deterministic diagnostic table hash. It is
not cryptographic, collision-resistant authentication, a signature, or anti-cheat.
Equal fingerprints alone are not used as proof that cells round-trip; tests compare
arrays directly.

## Limits and error behaviour

Maximum encoded size: 6 MiB. JSON depth: 24. Nodes: 600,000. Decoded string length:
4096 bytes (the map schema further restricts labels). Invalid UTF-8, invalid numbers,
nonfinite values, malformed escapes/surrogates, duplicate keys, sparse arrays and
trailing data are rejected. A maximum-sized alternating-cell document fits within
the count design, but repetitive runs normally make our maps substantially smaller.

Library files and dropped files have their reported sizes checked before reading;
the parser repeats the byte bound. Negative/zero/overflowing RLE counts, unknown
materials, bad dimensions, missing layers and invalid starts all reject. These
checks are defensive, not a claim of an independently audited hostile-input parser.
Avoid treating untrusted map bundles as inherently safe executable software.

File parse failure leaves the current colony unchanged. A successfully parsed map
opens an import preview. Confirming starts a SEPARATE colony, archives the previous
live bundle and commits the new save first. Escaping the preview leaves the existing
world untouched. This is not an in-run challenge rewind or resurrection tool.

## What current-terrain export excludes

Shift+F2 packs only the material cells and region cells at the selected tick. Placed
walls/floors live in the structure layer and are absent in the imported template.
Workers, item piles, crops, pumps, jobs, rules and the timeline are also absent.
Moving materials may therefore evolve differently after import. A dug-out or blocked
arrival can make export fail. Use F5/F6 to preserve an actual functioning colony.

F2 without Shift instead packs the original terrain, which is normally the best
artifact for comparing generators and sharing starting maps.
