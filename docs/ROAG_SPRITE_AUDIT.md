# ROAG sprite coverage and unused-art audit

Scope: **current** native renderer, classes, enemies, biome/terrain content,
boon catalogue, props, menu/HUD, playback effects and fallback paths. Current
game content is six classes, 16 regular enemies, seven elites, pursuer/final
boss, seven two-floor biomes, six mechanical terrains and **52 boons**.
No new gameplay mechanics are implemented by this audit.

## The choices being compared

| Settings choice | Source artwork | Status |
| --- | --- | --- |
| Classic built-in | Code-drawn tokens and terrain | Intentional fallback; no external sprites |
| Kenney pixel art (`kenney`) | Micro Roguelike characters/props + Caves & Dungeons surfaces + 1-Bit boon/mechanic symbols | Existing mixed Kenney option |
| Kenney 1-Bit (`onebit`) | 1-Bit Pack only, with derived connected surfaces | New standalone third choice |

Thus there are **two Kenney presentation choices**, not two copies of the
same pack. Three original sheets are present. Each remains unmodified and
CC0; provenance/checksums are in its `assets/kenney/*/README.md`.

## Active sprite coverage

The audit runs both actual native resolvers with headless texture/Quad handles,
then independently decodes every source PNG cell and rejects referenced empty
cells. Requirements come from registered content IDs, not merely the mappings
that happen to exist. **242/242 active sprite bindings resolve in each choice**:

| Actual runtime category | Binding count | Mixed Kenney | Standalone 1-Bit |
| --- | ---: | --- | --- |
| Six classes + 25 enemies, base/attack/hurt | 93 | 23 distinct bodies for 31 actors | 31 distinct bodies |
| Drops 3, chests 2, other objects 6 | 11 | Native Micro art | Native 1-Bit art |
| HP/XP/currency HUD icons | 3 | Native Micro art | Native 1-Bit art |
| Hit, boon, Burning, blocked-hit symbols | 4 | Native Micro art | Native 1-Bit art |
| 52 boon icons + their detail aliases | 104 | 52 distinct 1-Bit motifs, semantic tints | Same 52 motifs, monochrome |
| Mechanical terrain markers | 6 | 1-Bit symbols + colored semantic wash | 1-Bit symbols + subdued wash |
| Seven biomes × floor/wall/water bases | 21 | Caves native floor / composite surfaces | 1-Bit composite surfaces |

Both terrain resolvers cover all **256 raw neighbor cases → 47 canonical
masks** for walls and water. The legacy 24 named directional authoring slots
are not the native connectivity path; absent named-slot images therefore do
**not** mean missing runtime corners. Dynamic Quads select connected surfaces.
Barricades are mechanical markers over their ground/collision tile, not a
missing wall variant.

### Art reuse and semantic approximations

Both choices have **62 static pose aliases**: attack/hurt reuse each actor's
base image. No animation is provided. Boon detail reuses its icon. Currency
drop and HUD gold deliberately share a motif. Effect sprites are generic, not
one custom visual for every boon/element/weapon.

The existing mix has these eight pairs sharing body images:

| Micro cell | Shared identities |
| ---: | --- |
| 6 | Arcanist / Index Keeper |
| 7 | Alchemist / Cinder Mender |
| 9 | Thorn Warden / Hollow Regent |
| 10 | Sentinel / Prism Curator |
| 13 | Forge Conjurer / Ember Crucible |
| 14 | Spore Herald / Mycelial Matriarch |
| 15 | Cinder Bellkeeper / Ash Marshal |
| 22 | Canal Lancer / Canal Lockmaster |

The new pack avoids shared **actor** silhouettes, but many identities remain
representative motifs: a knight for a guard, wizard for a caster, plant/beast
for a hostile. Distinct native art is not proof that a boss visually conveys
its exact attack mechanics. Both packs use representative stair/gateway and
effect motifs. Human readability acceptance remains a separate check.

Mixed Kenney uses seven ground assignments, two wall and two water families.
Standalone 1-Bit shares one base material set across all seven biomes; it
covers them technically but lacks bespoke biome-specific environment art.

## Things still rendered without a pack sprite

These are **real procedural visuals**, not silently counted as sprite coverage:

| Rendered element | Current implementation / decision |
| --- | --- |
| Menu/HUD/chest/route/settings frames and focus indicators | `pixel_ui.lua` frames plus `main.lua` layout; palette changes, no sprite skin or nine-slice from these packs |
| Text, boon descriptions, damage numbers, title/results/achievement labels | BigBlue Terminal TTF via `ui_fonts.lua`; no Kenney bitmap font/glyph mapping |
| HP/XP bars, enemy HP, elite/final-boss status/phase bars | Drawn meters/text reflecting real state |
| Minimap cells, fog/exploration dimming | Intentional code rendering; not missing floor art |
| Dotted committed enemy attacks and player aiming | Intentional tactical outlines, preserve clear interiors |
| Fire/pulse/echo affected-cell flashes, status ticks, door/terrain/secret feedback | Colored cells/rings in `draw_history_event`, alongside generic hit/boon/block sprites; no individual elemental/boon effect sheets |
| Screen damage flash and camera shake | Presentation math/fullscreen overlay, not sprite resources |
| Secret crack/spiral clues | Drawn lines/circles in `draw_world`; native clue-specific art not assigned |
| Elite activation marker | Drawn concentric rings |
| World/title/pause backdrops | Plain fields and frames, no native background illustrations |
| Spectator cursor / debug diagnostics | Developer overlays, intentionally procedural |
| Classic and missing-pack tokens | Explicit safe fallback primitives |

Before this change, Burning and blocked-hit had image bindings but unused
render hooks. They now use their native sprites in both Kenney choices; the
Classic fallback and gameplay events remain intact. Other colored effects
listed above are still procedural; this is **not** a claim of complete
sprite-only presentation.

### Dormant contracts are not current game art gaps

The retained ID registry includes 95 printable ASCII glyph slots, optional
HUD/title/pause/bitmap-font slots, biome backdrop slots, currency detail and
legacy terrain shape names. The native game choices disable Paint theme/loose
overrides. Those contracts are not automatically active sprites just because
their IDs exist. In particular the game currently uses TTF text, not missing
letter sprites. `core:drop/currency/detail` is an optional conditional detail
image, not a required token render. The catalogue separates these from the
242 binding requirements above.

## Exact unused/excess inventory

Counts are **unique native grid cells**, not binding counts, files, animated
frames or new mechanics. “Referenced” includes a cell sampled partially as a
wall/water composition material. This does not mean its whole artwork is
shown intact. Transparent key/background is accounted for in Micro.

| Original sheet | Grid cells | Nonempty | Mixed referenced / unused | Standalone referenced / unused | Unused by either |
| --- | ---: | ---: | ---: | ---: | ---: |
| Micro Roguelike 8px | 160 | 159 | 40 / **119** | Not loaded | **119** |
| Caves & Dungeons 16px, 1px gutters | 522 | 522 | 52 / **470** | Not loaded | **470** |
| 1-Bit 16px packed | 1,078 | 1,077 | 56 / **1,021** | 101 / **976** | **976** |

Across the three stored sheets: **1,758 nonempty cells**, **193 referenced by
either choice**, **1,565 unreferenced**. This is grid occupancy, not a claim
of 1,565 unique useful objects: borders, variants and multi-cell assemblies
contribute to that number. For example, Caves' official “520 files” description
is not the same measurement as its 522 nonempty packed-sheet regions; the
audit counts decoded sheet cells, and does not count archive files.

Explore **every cell** in the self-contained [visual catalogue](sprite-audit/index.html).
Choose a pack and **Unused nonempty** to see its exact surplus. Hover/scroll
cells for associated stable IDs. It works offline; PNGs are embedded, no
browser network access needed. [CSV](sprite-audit/tiles.csv) includes index,
source coordinates, alpha/key occupancy and bindings for each choice;
[JSON](sprite-audit/inventory.json) includes resolver coverage and shared actors.

## Useful art for a subsequent design discussion

| Source surplus | Potential next use | What still needs a design decision |
| --- | --- | --- |
| Caves pillars, supports, beams, rails, ladders, bridge sections | Room dressing and more readable connections | Decoration-only versus collision; multi-tile placement; avoid painting door frames onto floor |
| Caves alternate floors, cracks, ore/crystals, foliage, water details | Biome identity and secret-clue art | Pure visual variants versus interactable objects; preserve fog and known clues |
| 1-Bit shields, swords, bows, staves, axes, armor, potions, keys, jewels | More precise boon/prop identity or cosmetic options | Many current skills already have these motifs; art availability alone does not justify a new item system |
| 1-Bit extra plants/beasts/characters and architectural pieces | Better enemy silhouettes, monochrome biome variants | Threat identification and motif suitability need review first |
| 1-Bit panels, arrows, symbols, numerals/capital letters, card/dice motifs | Sprite-skinned UI and consistent visual vocabulary | Glyph coverage/fallback, layout, slicing and readability; no existing font automatically becomes complete |
| Micro unused environment/props/creature/equipment cells | Additional small prop or actor alternatives | Micro environment was intentionally replaced by Caves; do not reintroduce mismatched floors blindly |
| 1-Bit modern/industrial/vehicle/space imagery | Mostly outside the current setting | Surplus, not a recommendation to add unrelated mechanics |

Prioritize **identity/readability fixes and visual-only variants** before
adding mechanics merely to consume art. Review this inventory together and
choose a few source indices; no balance, inventory, classes, enemies, terrain
rules or world generation were changed in this work.

## Reproduce and maintain the audit

```sh
make sprite-audit
# equivalent: Python stdlib + LuaJIT, no LÖVE window or player data access
python3 tools/sprite_audit.py
# inspect machine inventory only, no output files
luajit tools/sprite_audit.lua
```

`src/sprite_audit.lua` inspects real native resolvers and explicit material
references, with sorted output. `tools/sprite_audit.py` verifies PNG CRCs,
decodes RGBA8/indexed 1/2/4/8-bit data and PNG row filters, scans cells at
each sheet's correct stride, rejects referenced empty cells, and writes only
the three generated catalogue files. Stable IDs remain authored identities;
native indices are presentation mappings. Regenerate after changing mappings
or sheets. Procedural categories above require render-code review and are
not inferred by counting registry placeholders.
