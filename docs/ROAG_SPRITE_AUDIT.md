# ROAG sprite coverage and unused-art audit

**Enemy review accepted:** all 30 decisions were approved on 2026-10-18.
The [original review](sprite-audit/enemy-review.html) is preserved as an archive;
[the implementation guide](ROAG_ENEMY_SQUADS.md) records the exact selected art and behaviour.
The live catalogue includes these bindings.

Scope: **current** native renderer, classes, enemies, biome/terrain content,
boon catalogue, props, menu/HUD, playback effects and fallback paths. Current
game content is nine classes, 26 regular enemies, seven elites, pursuer/final
boss, seven two-floor biomes, six mechanical terrains and **53 boons**.
Updated 2026-10-18 after the approved sprite/room expansion. The earlier
242-binding catalogue was not regenerated with that expansion; this report
and the saved HTML/JSON/CSV now reflect the current 324 requirements.
The audit itself does not modify gameplay mechanics.

**Current surplus discussion:** [visual shortlist](sprite-audit/surplus.html)
and [reasoning/recommendations](sprite-audit/surplus.md), checked against the
324-binding runtime. This supersedes the older proposal tables for what is
still unused; earlier approvals and rejections remain in force.

**New idea approval page:** [89 grouped proposals](sprite-audit/ideas.html),
covering all unused cells with separate idea/art votes, preferences, comments
and downloadable feedback. See the [review guide](sprite-audit/ideas-guide.md).

## The choices being compared

| Settings choice | Source artwork | Status |
| --- | --- | --- |
| Classic built-in | Code-drawn tokens and terrain | Intentional fallback; no external sprites |
| Kenney pixel art (`kenney`) | Micro characters/props + Caves surfaces + 1-Bit symbols + approved Tiny Dungeon actors/fixtures and RPG keys/gates | Mixed Kenney option |
| Kenney 1-Bit (`onebit`) | 1-Bit Pack only, with derived connected surfaces | New standalone third choice |

Thus there are **two Kenney presentation choices**, not two copies of the
same pack. Five original sheets are present. Each remains unmodified and
CC0; provenance/checksums are in `assets/kenney/*/README.md` and
[the approved-source record](sprite-audit/review-art/README.md).

## Active sprite coverage

The audit runs both actual native resolvers with headless texture/Quad handles,
then independently decodes every source PNG cell and rejects referenced empty
cells. Requirements come from registered content IDs, not merely the mappings
that happen to exist. **324/324 active sprite bindings resolve in each choice**:

| Actual runtime category | Binding count | Mixed Kenney | Standalone 1-Bit |
| --- | ---: | --- | --- |
| Nine classes + 35 enemies, base/attack/hurt | 132 | 36 distinct bodies for 44 actors | 44 distinct bodies |
| Drops 3, chests 2, other objects 6 | 11 | Native Micro art | Native 1-Bit art |
| Reviewed object/scenery bindings | 29 | Micro/Caves/RPG/Tiny art | Native 1-Bit art |
| HP/XP/currency HUD icons | 3 | Native Micro art | Native 1-Bit art |
| Hit, boon, Burning, block, attack center/impact | 6 | Native Micro art | Native 1-Bit art |
| Squad field/heal/snare/shield/rally cues | 5 | Native 1-Bit art | Native 1-Bit art |
| Class weapon symbols | 5 | Native Micro art | Native 1-Bit art |
| 53 boon icons + their detail aliases | 106 | 52 1-Bit motifs + Micro reversal ring | 53 1-Bit motifs, monochrome |
| Mechanical terrain markers | 6 | 1-Bit symbols + colored semantic wash | 1-Bit symbols + subdued wash |
| Seven biomes × floor/wall/water bases | 21 | Caves native floor / composite surfaces | 1-Bit composite surfaces |

Both terrain resolvers cover all **256 raw neighbor cases → 47 canonical
masks** for walls and water. The legacy 24 named directional authoring slots
are not the native connectivity path; absent named-slot images therefore do
**not** mean missing runtime corners. Dynamic Quads select connected surfaces.
Barricades are mechanical markers over their ground/collision tile, not a
missing wall variant.

### Art reuse and semantic approximations

Both choices have **88 static pose aliases**: attack/hurt reuse each actor's
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
| Secret clue accents | Crack clues now have a native sprite plus drawn crack strokes; spiral clues still use drawn circles |
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
324 binding requirements above. Decoy/snare markers are also still drawn
geometry; the new class bodies are sprite-backed.

## Exact unused/excess inventory

Counts are **unique native grid cells**, not binding counts, files, animated
frames or new mechanics. “Referenced” includes a cell sampled partially as a
wall/water composition material. This does not mean its whole artwork is
shown intact. Transparent key/background is accounted for in Micro.

| Original sheet | Grid cells | Nonempty | Mixed referenced / unused | Standalone referenced / unused | Unused by either |
| --- | ---: | ---: | ---: | ---: | ---: |
| Micro Roguelike 8px | 160 | 159 | 52 / **107** | Not loaded | **107** |
| Caves & Dungeons 16px, 1px gutters | 522 | 522 | 68 / **454** | Not loaded | **454** |
| 1-Bit 16px packed | 1,078 | 1,077 | 61 / **1,016** | 147 / **930** | **930** |
| Approved Roguelike/RPG 16px, 1px gutters | 1,767 | 1,704 | 7 / **1,697** | Not loaded | **1,697** |
| Approved Tiny Dungeon 16px, 1px gutters | 132 | 132 | 15 / **117** | Not loaded | **117** |

Across the five stored sheets: **3,594 nonempty cells**, **289 referenced by
either choice**, **3,305 unreferenced**. Across just the original three sheets,
there are **1,491** unreferenced cells. The additional source sheets increase
the total inventory; this is not an increase in missing game mappings.
These counts are grid occupancy, not a claim of 3,305 unique useful objects:
borders, variants and multi-cell assemblies
contribute to that number. For example, Caves' official “520 files” description
is not the same measurement as its 522 nonempty packed-sheet regions; the
audit counts decoded sheet cells, and does not count archive files.

Explore **every cell** in the self-contained [visual catalogue](sprite-audit/index.html).
Choose a pack and **Unused nonempty** to see its exact surplus. Hover/scroll
cells for associated stable IDs. It works offline; PNGs are embedded, no
browser network access needed. [CSV](sprite-audit/tiles.csv) includes index,
source coordinates, alpha/key occupancy and bindings for each choice;
[JSON](sprite-audit/inventory.json) includes resolver coverage and shared actors.

## Earlier opportunity discussion (historical)

Use the [current shortlist](sprite-audit/surplus.html) for checked unassigned
candidates after the approved enemy teams. The broad suggestions below predate
those implementations and include decisions already resolved by the review.

| Source surplus | Potential next use | What still needs a design decision |
| --- | --- | --- |
| Caves pillars, supports, beams, rails, ladders, bridge sections | Room dressing and more readable connections | Decoration-only versus collision; multi-tile placement; avoid painting door frames onto floor |
| Caves alternate floors, cracks, ore/crystals, foliage, water details | Biome identity and secret-clue art | Pure visual variants versus interactable objects; preserve fog and known clues |
| 1-Bit shields, swords, bows, staves, axes, armor, potions, keys, jewels | More precise boon/prop identity or cosmetic options | Many current skills already have these motifs; art availability alone does not justify a new item system |
| 1-Bit extra plants/beasts/characters and architectural pieces | Better enemy silhouettes, monochrome biome variants | Threat identification and motif suitability need review first |
| 1-Bit panels, arrows, symbols, numerals/capital letters, card/dice motifs | Sprite-skinned UI and consistent visual vocabulary | Glyph coverage/fallback, layout, slicing and readability; no existing font automatically becomes complete |
| Micro unused environment/props/creature/equipment cells | Additional small prop or actor alternatives | Micro environment was intentionally replaced by Caves; do not reintroduce mismatched floors blindly |
| 1-Bit modern/industrial/vehicle/space imagery | Mostly outside the current setting | Surplus, not a recommendation to add unrelated mechanics |

The approved review has since implemented several of these suggestions,
including props, new classes and sidequests; see
[the implementation/decision record](sprite-audit/review-feedback.md).
The catalogue's unused filter is authoritative for present source usage;
the opportunity table above remains a design discussion, not a claim that
every pictured asset is still unused. This refresh changes audit files only.

For current candidates supporting the **same idea in both selectable Kenney
looks**, see the [unused-art shortlist](sprite-audit/surplus.html). The
[cross-pack opportunity audit](sprite-audit/overlap.md) and
[interactive sprite review](sprite-audit/review.html) preserve earlier proposals
and feedback; they are not a fresh approval request. Serve the current page
with `python3 -m http.server 8080` from the repository root and visit
`http://127.0.0.1:8080/docs/sprite-audit/surplus.html`, or open its local HTML
file directly. Its sprite images are embedded.

## Reproduce and maintain the audit

```sh
make sprite-audit
make sprite-audit-check  # read-only; rejects stale HTML/JSON/CSV
make surplus-audit       # curate/rebuild current unused-art shortlist
make surplus-audit-check # read-only shortlist and live usage checks
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

`make test` checks all three generated files before running Lua tests. Direct
`luajit tests/run.lua` also checks the inventory's mapping checksum against
the current resolvers. Previously only live resolver coverage was tested,
which let an older catalogue remain unnoticed. These checks do not regenerate
files or overwrite feedback; run `make sprite-audit` explicitly when needed.
