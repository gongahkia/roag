# Review-only candidate sheets

These PNGs are copied unchanged from official Kenney archives for the third
round of the [sprite review](../review.html). They are **not loaded by ROAG** and
do not approve any art for runtime use. The review page embeds them so it works
offline and can show exact source cells.

| Local PNG | Official source | Archive path | Grid | SHA-256 |
| --- | --- | --- | --- | --- |
| `roguelike-rpg-sheet.png` | [Roguelike/RPG pack](https://kenney.nl/assets/roguelike-rpg-pack) | `Spritesheet/roguelikeSheet_transparent.png` | 57 × 31, 16px cells, 1px gutters | `808c8d734480bc949b284f3bcc252e87e1ee53b24178791a6838cce6a319c439` |
| `tiny-dungeon-tilemap.png` | [Tiny Dungeon](https://kenney.nl/assets/tiny-dungeon) | `Tilemap/tilemap.png` | 12 × 11, 16px cells, 1px gutters | `5653222ac495d89e942f9b636300759b3f38e85b26e9b888676f2e9ab834095a` |

Both archives state CC0. Their license texts are preserved here as
`roguelike-rpg-license.txt` and `tiny-dungeon-license.txt`.

Zero-based, row-major cell IDs considered in round three:

- RPG vault: gold key **672** with barred door **159/216**, or gray key **671**
  with barred door **158/215**.
- RPG timed gate: raised/pressed floor-plate candidates **959/961** with the
  teal barred door **161/218**. A gate opening can show the underlying floor.
- Tiny Dungeon timed gate: plate-like cells **54/55** with door **45**. This is
  a style alternative; its darker palette may stand apart from the current
  Caves terrain.

These are visual readings and proposed assemblies, not official per-cell
labels. The user should approve or reject them from the review page before
they become gameplay bindings.
