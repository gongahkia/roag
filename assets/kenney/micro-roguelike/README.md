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

`src/kenney_visuals.lua` uses this sheet for characters, items and basic HUD
symbols. One sprite texture keys the native background `#222323` to transparent
when loading. The original PNG is unchanged.

Background floors/walls/water now come from **Roguelike Caves & Dungeons** under
`assets/kenney/caves-dungeons/`; the earlier Micro terrain/wall adapter was
removed. Boon icons/details and all six mechanical terrain markers use the
1-Bit Pack under `assets/kenney/1-bit-pack/`. The combined **Kenney pixel art**
choice caches four textures and retains the existing `kenney` preference ID.

Some enemies/bosses share a native Micro sprite, and attack/hurt retain their
static body. No animation, generative repainting or gameplay changes. See
`docs/ROAG_CAVES_DUNGEONS.md` for the current background and verification.
