# Review-only candidate sheets

These PNGs are copied unchanged from official Kenney archives for the third
and fourth rounds of the [sprite review](../review.html). The approved cells
are now loaded by ROAG from copies in `assets/kenney/approved/`. The review
page embeds these source sheets so it works offline and shows exact cells.

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

Round-four candidate actor cells from the same Tiny Dungeon sheet:

- Hookrunner: **111** masked figure, with **84** as a more hooded alternative.
- Trapper: **112** green scout, with **88** as a quieter alternative.

The earlier proposed 1-Bit counterparts were **80/78** for Hookrunner and **77/222**
for Trapper. Approved cells **80** and **77** are now bound; alternatives
**78** and **222** remain unbound. Unlike existing
Micro actors, these Tiny Dungeon figures are 16px source cells; approval of
their silhouettes does not waive a game-scale visual check.

These are visual readings and proposed assemblies, not official per-cell
labels. The completed [feedback record](../review-feedback.md) approves the
gray RPG vault set, both timed-gate sets, Tiny #111 for Hookrunner and Tiny
#112 for Trapper. The runtime binds only those selected cells. Other
shortlisted alternatives remain unapproved.

The next [enemy combination review](../enemy-review.html) proposes additional
unused cells from these same stored sources. Its
[audit and handoff](../enemy-review-audit.md) lists exact assignments and
existing-role overlaps. Candidate selection is not runtime publication or
user approval; no extra sheet download or art replacement is performed.
