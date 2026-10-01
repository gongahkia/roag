# ROAG Art Packs

This directory carries the original downloadable source material for every
third-party art pack selectable from ROAG's **ART PACKS** title-menu screen.
The selected pack is a presentation preference only: it is stored separately
as `art_pack_settings.json` and never changes simulation, save data, or an
active run.

| Runtime ID | Source | License / required credit |
| --- | --- | --- |
| `art_pack.loveable_rogue` | [Loveable Rogue](https://opengameart.org/content/loveable-rogue) | CC0, by surt |
| `art_pack.dawnlike` | [DawnLike 16x16 Universal Rogue-like Tileset](https://opengameart.org/content/dawnlike-16x16-universal-rogue-like-tileset-v181) | CC-BY 4.0. Credit DragonDePlatino and DawnBringer. The supplied `README.txt` contains the source attribution request. |
| `art_pack.kenney_micro_roguelike` | [Kenney Micro Roguelike](https://kenney-assets.itch.io/micro-roguelike) | CC0, by Kenney |
| `art_pack.kenney_roguelike_indoors` | [Kenney Roguelike Indoors](https://kenney.nl/assets/roguelike-indoors) | CC0, by Kenney |
| `art_pack.kenney_roguelike_modern_city` | [Kenney Roguelike Modern City](https://kenney.nl/assets/roguelike-modern-city) | CC0, by Kenney |
| `art_pack.kenney_roguelike_caves_dungeons` | [Kenney Roguelike Caves & Dungeons](https://kenney.nl/assets/roguelike-caves-dungeons) | CC0, by Kenney |
| `art_pack.kenney_roguelike_rpg_pack` | [Kenney Roguelike/RPG Pack](https://kenney.nl/assets/roguelike-rpg-pack) | CC0, by Kenney and Lynn Evers |
| `art_pack.kenney_roguelike_characters` | [Kenney Roguelike Characters](https://kenney.nl/assets/roguelike-characters) | CC0, by Kenney |

Each Kenney directory includes its upstream `License.txt`. DawnLike retains
its upstream `README.txt`, which identifies its CC-BY 4.0 terms. The runtime
catalog lives in `src/rendering/art_packs.lua`; it supplies bounded starter
role maps for every renderer sprite. The original ROAG 1-bit mapping remains
the one supported by the standalone `sprite_editor`, because its JSON schema
is intentionally tied to the original 49×22 sheet.
