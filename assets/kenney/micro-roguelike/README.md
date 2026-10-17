# Kenney Micro Roguelike 1.4

Original art by Kenney, **Creative Commons Zero / CC0**. The included
`License.txt` preserves the distribution's license text (line endings and
surrounding whitespace normalized). No AI artwork is used in this pack.

- Source: <https://kenney.nl/assets/micro-roguelike>
- Downloaded 2026-10-17 from the official page's `kenney_micro-roguelike.zip`.
- `tiles.png` is the **unchanged** `Tilemap/colored_tilemap_packed.png`.
- Native sheet: 128×80 pixels, 16 columns × 10 rows, **8×8**, **no spacing**.
  `Tilesheet.txt` describes the alternate spaced sheet, not this packed PNG.
- Runtime checksum (the project's checked-content checksum): `5407eab40dd3457c`.

`src/kenney_visuals.lua` documents the source tile indices and stable bindings.
Two adaptations happen once when the pack loads: a separate sprite texture
keys the native background `#222323` to transparent, and a separate wall page
assembles 47 connected variants from native border pixels over the native gray
wall material. The source PNG stays untouched. Wall construction is needed
because ROAG uses solid wall cells, while Kenney's sample uses outlines around
black space. A small cache shares the three textures and their Quad handles.

No manual rescaling, generative repainting, source atlas overwrites, animation
frames or gameplay changes. Some enemies/bosses share a native sprite, and all
biomes currently share this pack's palette. See `docs/ROAG_KENNEY_TRIAL.md`.

The Kenney option now also loads the separately licensed 1-Bit Pack under
`assets/kenney/1-bit-pack/`: one additional cached native sheet supplies all
52 boon icons/details and six terrain mechanics, replacing the earlier
approximate prop mappings. The three Micro Roguelike textures are unchanged.
