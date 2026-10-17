# Kenney 1-Bit Pack 1.2 — ROAG supplement

- Original: <https://kenney.nl/assets/1-bit-pack>
- Official download, retrieved 2026-10-17:
  <https://kenney.nl/media/pages/assets/1-bit-pack/aa867a1f37-1677578516/kenney_1-bit-pack.zip>
- License: **CC0**, original distribution text preserved in `License.txt`
  (line endings/trailing whitespace normalized).
- `symbols.png` is the **unchanged**
  `Tilesheet/monochrome-transparent_packed.png` from that archive.
- Dimensions **784×352**, **49×22** regions, **16×16px**, packed stride **16**.
  The archive also contains spaced sheets; their 17px stride does not apply.
- Checked-content checksum: `3fc46971ed866cba`.
- SHA-256: `0d9abf9b812a441e1673bc166d61abb6236c0bc46b3775700bd4d4364ba170e0`.

This adds semantic boon icons and mechanical terrain markers to the existing
**Kenney** option. It is not another selectable game pack. Character sprites,
plain floors, connected walls and water still use Micro Roguelike.

`src/kenney_symbols.lua` maps every current boon and mechanical terrain ID to
a deliberately chosen native region. Color tints are applied at draw time;
the PNG is not recolored, regenerated or modified. Terrain markers are inset
32px images (integer 2× scale) over a subdued semantic backing in the fixed
40px tile. UI icons are 32px, chest details 64px and boon details 96px.

Source motifs are representative: for example, a skull for kill healing,
hourglass for a delayed echo, eye for distant observation, crate for a
barricade, and layered ripple strokes used as mist wisps. These are not new
terrain or boon mechanics. See `docs/ROAG_KENNEY_SYMBOLS.md` for the full map,
actual game screenshots and verification.
