# ROAG sprite coverage and unused-art audit

## Current appearance

ROAG always uses **Kenney 1-Bit**, including the **13 explicitly approved
monochrome derivatives**. There is no art selector in title/pause settings,
launch environment or Make targets. `make game` and `make spectator` use the
same renderer. Old visual-preference records are ignored without touching
player saves. Automatic emergency shapes remain available only when artwork
cannot load; they are not a selectable style. F5 refreshes the fixed resources.

Sound, effects, font readability, text size and contrast remain adjustable.
The font choices are accessibility options, not alternate sprite packs.

Current content: fifteen classes, 39 regular enemies plus finite fragment and
optional champion, seven elites, pursuer/final boss, seven two-floor biomes,
six original terrain mechanics plus three optional mechanics, fourteen finite
item/room interactions, and **103 boons**. The 50-addition batch and combined engineering acceptance
are recorded in [the current acceptance guide](ROAG_IDEA_ACCEPTANCE.md).

## Active coverage

The live catalogue inspects the actual fixed resolver, then independently
checks source PNG bytes, dimensions, CRCs and nonempty sampled regions.
**599/599 required bindings resolve; none are missing.**

| Runtime category | Bindings | Artwork |
| --- | ---: | --- |
| Fifteen classes + 50 enemy forms, base/attack/hurt | 195 | Native 1-Bit; 63 body drawings for 65 identities |
| Drops, chests and other objects | 17 | Native 1-Bit |
| Reviewed scenery/objects | 29 | Native 1-Bit |
| Finite item/room objects, plaque and token marker | 16 | Native 1-Bit representative motifs |
| HP/XP/currency icons | 3 | Native 1-Bit |
| Hit, boon, Burning, block, attack center/impact | 6 | Native 1-Bit |
| Squad/corpse/alarm/oath/reflector/charge cues | 11 | Native 1-Bit |
| Original class weapon symbols | 5 | Native 1-Bit |
| 103 boon icons and detail aliases | 206 | Native 1-Bit |
| Eight last-roll dice cues | 8 | Native 1-Bit |
| Original terrain mechanic markers | 6 | Native 1-Bit symbols |
| Vent phases, arrows and fragile shortcut states | 10 | Native symbols plus monochrome Tiny vents |
| Approved room materials and props | 37 | Native counterparts and approved monochrome derivatives |
| Contextual action/status/recovery/impact/state cues | 29 | Native 1-Bit |
| Seven biome floor/wall/water bases | 21 | Derived native 1-Bit surfaces |

Walls and water cover **256 raw neighbor cases → 47 canonical masks**.
Derived page coordinates are deployment details, never authored identities.
There are **130 static attack/hurt aliases**, not animation frames. Boon detail
reuses its icon. Duelist/Oath Pike share body 124; Bone Splitter/fragments share
323, with fragments rendered smaller. Some objects also share representative
motifs. A valid mapping is not a claim of a bespoke silhouette for every prop.

## The 13 retained counterparts

| Artwork | Required bindings | Actual source |
| --- | ---: | --- |
| Vent cooldown/warning/burst | 3 | Tiny Dungeon 9/29, cached two-tone conversion |
| Drowned room water material | 1 | RPG outer banks 2–4 / 59–61 / 116–118; native concave 115/114/57/58 |
| Broken cavern wall material | 1 | Caves frame 38, center 39, accent 3 |
| Rubble variants | 2 | Caves 3/5 |
| Two column top/middle/base assemblies | 6 | Caves 146/175/204 and 147/176/205 |

All are neutral derivatives; original PNG bytes remain untouched. Native
fallback motifs remain available for optional missing counterparts. Removing
RPG/Caves would lose the two preferred environment material handles and cause
generic terrain fallback, so those sources remain required for the chosen look.
No Micro sprites are loaded by the game, spectator or current pixel preview.

Source provenance/licensing is retained in `assets/kenney/*/README.md` and
[the approved-source record](sprite-audit/review-art/README.md). All source art
is Kenney CC0. The historical mixed resolver was moved out of `src` into
`tests/legacy_kenney_visuals.lua` for archive/source regression assertions.

## Source inventory and remaining art

These are unique nonempty native cells sampled by the current renderer,
including partial composition inputs. They are not numbers of new mechanics.

| Current source | Nonempty cells | Referenced | Unused |
| --- | ---: | ---: | ---: |
| 1-Bit packed 16px | 1,077 | 296 | 781 |
| Caves & Dungeons, 16px / 1px gutters | 522 | 10 | 512 |
| Approved RPG, 16px / 1px gutters | 1,704 | 13 | 1,691 |
| Approved Tiny Dungeon, 16px / 1px gutters | 132 | 2 | 130 |
| **Total active source sheets** | **3,435** | **321** | **3,114** |

The full source sheets stay intact because their exact bytes/checksums are
verified and their approved regions feed deterministic compositors. Unused
regions are not automatically suitable or approved for additional gameplay.

Micro Roguelike's unchanged 159 nonempty cells remain **historical evidence**:
the original approved feedback and embedded review pages reference that PNG at
its original path. Deleting it would break those source-verification contracts.
It is excluded from current runtime inventory totals. The live catalogue has
a clearly labelled archive foldout preserving old Micro cell links; it does
not offer a different game renderer.

## Intentional drawn presentation

These are outside the 599 required sprite bindings:

- UI frames, focus indicators, health/XP/phase bars and menu layout.
- TTF text, numbers, boon descriptions and achievements.
- Minimap, fog, camera/screen feedback and dotted tactical attack outlines.
- Player decoy/snare markers, some clue strokes and feedback rings.
- Plain menu/title backdrops and developer overlays.

The fixed palette filter keeps terrain, players and UI neutral. Rewards/boons
use muted brown/gold, enemies blue-gray, hostile attack outlines red, and player
attacks/previews green. Blocked movement previews stay gray; cancelled windups
stay neutral. Damage numbers alone cycle through vivid hues with a gentle
brightness pulse; blocked zeros remain calm. Critical `!`, dark outlines,
visibility, lifetimes and the existing number limit are retained.

These accents are render-time inks keyed by semantic IDs, never edits to source
PNGs or atlas coordinates. Cached proxy handles preserve shared textures/Quads
without tinting unrelated uses of the same source cell. They retire with the
resource generation on F5. The shader converts source luminance before applying
the scoped ink, preserves alpha, and restores neutral ink after each draw.
Compilation/uniform initialization is cached and guarded; unsupported shaders
warn once and allow safe unshaded rendering. There is no art-pack selector.
Existing readability settings remain independent of gameplay.

## Review history is preserved

- [Original enemy review](sprite-audit/enemy-review.html): 30/30 decisions.
- [Original idea review](sprite-audit/ideas.html): 55 approve / 30 reject / 4 redirects.
- [Approved item/room detail review](sprite-audit/items-rooms.html).
- [Reviewed surplus discussion](sprite-audit/surplus.html).

These are frozen approval records, including their historical comparison
controls and original source hashes. They cannot change the game's appearance.
The original 324-binding and 399-binding review snapshots are not regenerated
from the current 599-binding runtime. Archive verification checks current
1-Bit assignments while retaining retired mixed-pack decisions as history.

## Inspect and verify

Open [the live catalogue](sprite-audit/index.html). Its **Source** filter
inspects the four current sheets; it does not select a game style. The
[CSV](sprite-audit/tiles.csv) and [JSON](sprite-audit/inventory.json) report
current usage. The [pixel gallery](visuals/environments/index.html) provides
nine offline source-pixel views, unchanged from the accepted 1-Bit look.
They are not LÖVE/GPU screenshots or physical-input acceptance.

```sh
make sprite-audit
make sprite-audit-check
make environment-preview-check
make item-room-review-test sprite-ideas-test enemy-review-check surplus-audit-check
node tests/sprite_surplus_page_test.js
sh tests/visual_packs_disk_smoke.sh
sh tests/idea_environment_disk_resume.sh
TMPDIR=/var/tmp luajit tests/run.lua
```

The catalogue's recorded mapping checksum is checked by the headless suite;
stale generated pages fail rather than silently reporting old coverage.
No LÖVE GUI application was launched during this fixed-look change.
