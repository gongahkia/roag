# Kenney Roguelike Caves & Dungeons

- Source: <https://kenney.nl/assets/roguelike-caves-dungeons>
- Official archive, downloaded 2026-10-17:
  <https://kenney.nl/media/pages/assets/roguelike-caves-dungeons/5195ceb8ca-1677694831/kenney_roguelike-caves-dungeons.zip>
- Original artist: Kenney Vleugels / Kenney. **CC0**, license text preserved
  in `License.txt` (line endings/trailing whitespace normalized).
- `terrain.png` is the **unchanged** original
  `Spritesheet/roguelikeDungeon_transparent.png`, 31,160 bytes.
- Sheet dimensions **492×305**, **29×18 cells**, native **16×16px**,
  **1px gutters**, origin **0,0** and source stride **17**. Some cells are blank.
- Checked-content checksum: `974b7d14df86b1d7`.
- SHA-256: `3abd8d294c523fe78078e2123146a2b9d2c0f2dd3cfbbd60dec8f508c3fe8c2c`.

The sheet, Preview.png, Sample.png and spritesheetInfo.txt were inspected from
the official archive. Numbered source-tile contact sheets were made under
`/var/tmp/roag-kenney-caves/` to distinguish wall pieces, ordinary floor,
water edges, concave shoreline corners, pillars, doors and other props.

`src/kenney_terrain.lua` maps only appropriate surface regions into terrain.
Native source quarters/edge pixels form a cached derived surface page at load:
47 connected wall masks in stone and earth, plus 47 shoreline masks in blue
and green water. Native water corners come directly from this pack's matching
corner tiles. Floor-center tiles render directly from this untouched sheet.
The derived texture is disposable, not an editable source or published atlas.

ROAG has solid wall cells extending beyond explored paths, whereas Kenney's
example renders finite wall borders around rooms. The adapter fills those
masses with native wall material and uses native frame pixels for exposed
edges, isolated pillars and inward/outward corners. Unknown neighbors count
as connected, preventing the edge art from disclosing hidden passages.

The fixed 40px game tile remains unchanged. Native 16px terrain uses nearest
filtering at 2.5×, with shared integer tile positions and exact region bounds;
the pixel spacing consequently alternates between two and three screen pixels.
No filtering blur, source rescaling, AI artwork or gameplay change is involved.
See `docs/ROAG_CAVES_DUNGEONS.md` for actual game captures and verification.
