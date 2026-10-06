# Loveable Rogue runtime mapping

## Source and extraction

- Source: `assets/art_packs/loveable_rogue.png`
- Source format: indexed PNG, 1024×1024
- SHA-256: `72f82654c82f0ddd91ebbfffd662f7e526561f226bd27e7baaba48a31dc3ecd7`
- Source structure: a collage/contact sheet. Its upper-left 256×256 panel is
  the clean native-pixel source panel used by ROAG; the rest contains examples
  and reference composition and is never sampled by the runtime.
- Extraction: `tools/extract_loveable_rogue_atlas.sh` crops `(0,0,256,256)`
  using ffmpeg without resizing. It writes `assets/visual/loveable_rogue_atlas.png`.
- Runtime metadata: `content/presentation/loveable_rogue_atlas.json`.

The extractor is deterministic. The runtime atlas SHA-256 after VIS-01 is
`63635a2a3688c2d0b39eaab50f6d5689340381e480cb7f2326f4ffc73f98e731`.
Runtime filtering is `nearest` and Expedition board cells are selected only at
whole 16 px atlas multiples, so source pixels are never bilinearly sampled or
fractionally scaled.

## Provenance note

The historical repository art-pack metadata that accompanied this local file
recorded a source URL, `CC0`, and the credit `surt / OpenGameArt`. VIS-01 did
not independently verify that claim or fetch anything from the network. The
local file and this note are therefore the repository's available provenance,
not a newly established license determination.

## Atlas regions

Coordinates below are in the 256×256 runtime atlas. The JSON file is the
authoritative machine-readable mapping.

| Region | Coordinates | Runtime use |
|---|---:|---|
| Bitmap glyph rows | x 0–207, y 0–95, 8×8 cells | Normal game UI text |
| Dungeon walls | x 0–79, y 160–191, 16×16 cells | Centre/cardinal/corner wall tiles |
| Dungeon floor/effects | x 0–223, y 192–207 | Floors, liquid, gas, spikes, fire, electricity, pickups/projectiles |
| Objects | x 160–255, y 160–191 | Door states, chest/cache, infrastructure, traversal and reinforcement objects |
| Creatures/actors | x 0–255, y 208–223 | Enemy roles, boss, player classes, special actors |

The sprite source panel contains some visually ambiguous adjacent examples.
ROAG binds only the reviewed cells listed in metadata; it does not infer a grid
from screenshots or crop pixels from gameplay examples.

## Semantic bindings

| ROAG semantic | Loveable Rogue atlas ID |
|---|---|
| Floor / alternate floor | `terrain.floor` / `terrain.floor_alt` |
| Wall faces/corners | `terrain.wall_*` |
| Liquid, gas, spikes, fire, electricity | `effect.liquid`, `effect.gas`, `effect.spikes`, `effect.fire`, `effect.electricity` |
| Closed/open door | `object.door_closed` / `object.door_open` |
| Cache/chest/crate | `object.cache`, `object.chest`, `object.crate` |
| Infrastructure | `object.generator`, `object.breaker`, `object.station` |
| Traversal/reinforcement | `object.traversal`, `object.connection`, `object.reinforcement` |
| Cash / generic pickup | `item.cash` / `item.pickup` |
| Normal/explosive projectile | `projectile.normal` / `projectile.explosive` |

Player bindings are `player.gunner`, `player.bruiser`, `player.conductor`, and
`player.demolitionist`. Enemy definitions resolve through explicit kind and
role maps in the JSON. Elites retain their base source creature with the
existing thin presentation cue. Bosses resolve to `boss.default`; their
existing component/provider overlays remain presentation feedback rather than
procedural actor bodies.

## Bitmap font

The source uses 8×8 pre-coloured glyph rows. ROAG maps amber, blue, white, and
grey lower-case, upper-case, and symbol rows. Normal runtime strings are
uppercased and rendered at integer nearest-neighbour scale. Supported visible
ASCII includes A–Z, a–z, digits, `. , : ! ? / + - ( ) @ _`. Unsupported
characters normalize to a source `?`; no system font is mixed into a line.

## Validation

`src/rendering/loveable_rogue_assets.lua` validates schema version, rectangle
bounds, all required visual semantics, class/role/object bindings, glyph rows,
required characters, and duplicate glyph mappings. `tools/validate_content.lua`
loads the same metadata. Missing bindings are errors, never a shape fallback.
