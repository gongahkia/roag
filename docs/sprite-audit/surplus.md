# Current unused Kenney art: inventory and next choices

Audit date: **2026-10-18**, after the approved five enemy combinations.
Current native mapping fingerprint: `1202bc99da0ba099`.
This is a read-only design audit. No sprite assignments or gameplay rules
have changed. Candidates are proposals, not approvals.

**Open the [visual shortlist](surplus.html)** for enlarged native art beside
each idea, or the [complete catalogue](index.html) for every source cell.
Both pages embed their PNGs and work offline. From the repository root:

```sh
python3 -m http.server 8080
# http://localhost:8080/docs/sprite-audit/surplus.html
```

## What is actually unused?

The two settings choices are **mixed Kenney** and **standalone 1-Bit**.
The mix loads five original sheets, including the two sources added during
the approved review. Standalone loads only the 1-Bit sheet. Classic is the
code-drawn fallback, so it has no source-sheet surplus to measure.

Both choices resolve **324/324 current required sprite bindings**. That is
separate from the following count of **distinct source cells**:

| Source sheet | Nonempty cells | Mixed uses | Standalone uses | Unused by either |
| --- | ---: | ---: | ---: | ---: |
| Micro Roguelike | 159 | 52 | Not loaded | **107** |
| Caves & Dungeons | 522 | 68 | Not loaded | **454** |
| Shared 1-Bit | 1,077 | 61 | 147 | **930** |
| Approved Roguelike/RPG | 1,704 | 7 | Not loaded | **1,697** |
| Approved Tiny Dungeon | 132 | 15 | Not loaded | **117** |
| **Union** | **3,594** | **203** | **147** | **3,305** |

Per selected choice, mixed has **3,391** unused nonempty cells; standalone
has **930**. Do not add those totals: both can use the shared 1-Bit sheet.
All 61 1-Bit cells the mix references are also referenced by standalone.
Consequently, 86 cells are unused in the mix but already used in standalone.

The sheets contain 3,659 grid cells, including 65 empty ones. Empty cells are
excluded. A used cell includes partial sampling for connected terrain; its
whole drawing need not be displayed intact. Whole PNG sheets are already
loaded, so surplus means **unmapped art**, not missing downloads or new files.

**3,305 cells does not mean 3,305 new objects or mechanics.** Wall fragments,
panel borders, equipment, palette variants and pieces of larger assemblies
account for much of it. Art availability alone does not establish readable
combat silhouettes, complete animations or a viable gameplay feature.

### What each source can realistically provide

| Source | Useful surplus | Limits / interpretation |
| --- | --- | --- |
| Micro | Small props, equipment, effects, terrain and UI motifs | Its complete character-body region is already assigned. Do not count the remaining 107 cells as new enemies. |
| Caves | Alternate masonry, corners/edges, water, rails, supports and bone dressing | Primarily environment parts. Many must be assembled, aligned and checked for connectivity; gates/props must not become floor textures. Existing runtime corners already resolve. |
| 1-Bit | More bodies, weapons/staves, rings, graves, furniture, frame pieces, numbers and symbols | Only source directly available to both choices. Also contains modern/space motifs outside ROAG's setting. No complete lowercase font is established here. |
| Roguelike/RPG | Bookshelves, graves, altars, barrels, signs and many building/material variants | The largest count is mostly environment/UI variation, not a large new enemy roster. Multi-cell furnishings require a placement policy. |
| Tiny Dungeon | **Nine unassigned complete figures**, plus crates, chests, equipment and fixtures | The figures are `85, 88, 89, 99, 100, 109, 121, 122, 123`. The other 108 unused cells are not additional complete characters. |

Visual motif names in this report are interpretations of the pixels, not
official asset names. The exact cell, native stride, source checksum and
current usage are available in [the checked JSON](surplus.json) and
[full CSV](tiles.csv).

## Fit against the existing game

The game already has nine classes, 26 regular enemies, seven biome minibosses,
a pursuer and final boss, seven biomes with two floors each, 53 boons and six
mechanical terrain types. It already has food/health/currency drops, ward
flasks, ore, braziers, keys/vaults, timed plates/gates, sidequests and scenery.

The approved wizard/bodyguards, healer/rushwings, trapper/artillery,
shield/archer and captain/followers combinations are also implemented.
Reskinning these is different from inventing another support role with the
same decision. Relevant contracts were checked in `classes.lua`,
`regular_ai.lua`, `enemy_squads.lua`, `squad_ai.lua`, `terrain.lua`,
`review_features.lua`, `game.lua`, room/generation content and native resolvers.

### Best next uses, in order of risk

| Direction | Available examples in the visual shortlist | Existing overlap and meaningful difference | Work / remaining decision |
| --- | --- | --- | --- |
| **Separate existing mixed-pack bodies** | Tiny's nine spare figures | Mixed currently has 36 silhouettes for 44 actors, with eight shared pairs. Standalone already has 44 distinct bodies. | **Small; strongest immediate use.** Review specific replacements. The current Bastion Bulwark Micro `30` also reads like a column. No new combat rules needed. |
| **Make existing biomes recognisable** | RPG `725–727, 839–841`; 1-Bit `299, 346` shelves | Archive room dressing; preserve approved fungal/tree art. Standalone shares its base terrain materials across all seven biomes. | **Small–medium.** Choose wall/prop ownership and placement without changing collision or putting furniture on ground tiles. |
| **Better existing boon / supply art** | Shared 1-Bit `338–340` rings; RPG `23–27` barrels; Tiny `63,75` crates; 1-Bit `650,651` barrels | Ring of Reversal already has Micro `89` / 1-Bit `337`. Additional rings could distinguish existing boon motifs. Containers can dress existing supplies. | **Small.** Identity review only. A barrel does not imply explosives or a new inventory system. |
| **One-use HP-offering font** | Tiny `32,74`, RPG `563`; 1-Bit `690,691` vessel/urn alternatives | Shrine room templates exist, but there is no HP-payment altar. A visible HP-for-boon choice creates a decision rather than another free heal. | **Medium.** Agree on cost, confirmation, lethal-use policy and one-use persistence. Use the current frozen boon pool; avoid mandatory route tolls. |
| **Haunted cache enemy** | Tiny `90–92` chest forms (including mimic-like open `92`) + `121` ghost; 1-Bit `303` chest + `417` ghost | Vaults can already produce an ambush. This needs an inspectable chest that wakes into a telegraphed fight, not merely another random vault roll. | **Medium.** Define tell, awakening action and normal locked attack. Standalone would use a chest-to-ghost transformation rather than a matching toothy-chest drawing. |
| **Heavy rammer / shoving enemy** | Tiny `89` blocky figure or `109` heavy humanoid; 1-Bit `276` insect or `129` armour | Glass Charger moves itself; Hookrunner pulls enemies. Neither is a player-shoving enemy. Could interact with terrain or existing snare teams. | **Medium–high.** Player displacement needs explicit wall, occupied-cell, stairs, hazard and telegraph rules; current generic displacement is AI-only. No guaranteed unavoidable shove damage. |
| **Finite reanimator** | Tiny `85,88` hooded figures + RPG `564–566` graves; 1-Bit `318,173,539,72,628` | Living-unit healing and capped summoning exist; corpse revival does not. Occupying a marked corpse tile or killing the caster could counter a committed revival. | **High.** Corpses, ownership, save state, spawn occupancy and reward eligibility need rules. One revival cap and no repeated XP/currency farm. |
| **Crypt biome** | Caves `58,60,61` bones, RPG graves and Tiny ghost; 1-Bit grave/skull/ghost motifs | Visually plausible eighth environment; seven biomes already exist. Spare graves alone do not create biome identity. | **High.** Requires a distinct traversal/combat rule, proper floor/wall materials, generation tests, routing and miniboss acceptance. Start with dressing or one room if desired. |
| **Sprite UI skin** | Shared frame pieces, digits, arrows | Current UI frames, meters and text are deliberately procedural. | **Medium; presentation only.** Complete scalable borders and text fallback need checking. Do not promise a complete font or add cards/dice mechanics merely because those symbols exist. |

The shortlist supplies both-look alternatives for every direction. Some
candidate cells appear in several ideas; these are **alternatives, not
simultaneously reserved assignments**. Source indices remain presentation
details. Future gameplay must retain stable content IDs.

My recommendation is to improve existing silhouettes/biome landmarks first.
For new gameplay, the font is the smallest interaction to define; the haunted
cache is the smallest focused enemy proposal. Shoving and revival have more
interesting combat implications but also more deterministic/save-state work.

## Earlier feedback still applies

These decisions from [the review record](review-feedback.md) have not changed:

- Hittable spore pods were **rejected**; mushroom scenery was approved.
  1-Bit `315–317` reads as hanging slime, not an approved mushroom replacement.
- Bomb mechanics were **rejected**. Micro `89` and 1-Bit `337–340` were identified
  as rings; 1-Bit `570` as a fireplace. Spare ring art is not evidence for bombs.
- Moving carts/trains were **not selected**; abandoned rail scenery was approved.
- Micro `123` was excluded as a flask, and 1-Bit `740` as a brazier.
  Do not silently replace those decisions because the cells are still unused.

Keys/vaults, plates/gates, braziers, wards, reviewed classes/sidequests and
the five approved enemy teams are **already implemented**, not fresh proposals.
The original [overlap analysis](overlap.md) is a historical snapshot and must
not be read as a current unused list.

## Reproduce the evidence

```sh
make sprite-audit-check     # full HTML/JSON/CSV against live resolvers and pixels
make surplus-audit-check    # shortlist, source pixels, compatibility and freshness
make enemy-review-check    # preserves earlier approved review / exact assignments

# Regenerate deliberately after a mapping change:
make sprite-audit
make surplus-audit
```

`tools/build_sprite_surplus.py` checks the current mapping fingerprint, PNG
CRC/dimensions, cell bounds and nonempty pixels, verifies every proposed cell
is unassigned in **both** runtime choices, and embeds unchanged original PNGs.
It rejects unloaded source sheets for standalone 1-Bit. Checks write nothing.
If a proposed cell later becomes assigned, curate `surplus-candidates.json`
before rebuilding the shortlist rather than preserving a false unused label.

The catalogue now filters by **loaded sheets** as well as binding usage; selecting
standalone 1-Bit no longer counts the unloaded Micro/Caves/RPG/Tiny sheets as
its unused art. This audit requires no LÖVE window, player save or source edit.

### Verification of this audit change

- Current catalogue and shortlist freshness checks passed; approved 30/30
  enemy-review decisions and archived source checksums remain unchanged.
- **3/3** existing headless sprite-audit tests and **5/5** review data/state
  checks passed. The full gameplay suite was not rerun for this documentation
  and catalogue-filter change; no game module or source PNG was modified.
- Headless Chromium checked **12** loaded-pack/filter combinations, all
  candidate links and embedded source PNG bytes, shortlist filters/search,
  mobile layout and absence of JavaScript exceptions. Rendered screenshots
  were inspected under `/var/tmp/roag-surplus-audit/`.
- No LÖVE window, physical input or gameplay playtest was used for this audit.

```sh
node tests/sprite_surplus_page_test.js
# Optional rendered-page check (an isolated headless browser, not the game):
node tests/sprite_surplus_page_test.js --browser /path/to/chrome /var/tmp/roag-surplus-audit
```
