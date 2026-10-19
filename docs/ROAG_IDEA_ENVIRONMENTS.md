# Approved environment and presentation batch

This implements the approved room-art and presentation directions after the
[three terrains](ROAG_IDEA_TERRAINS.md). **Stop here before the additional-boon
batch.** There are still seven biomes, fifteen classes and 53 current boons.
No crafting, durability, new biome, animation system or UI redesign is added.

## See the changes

Run `make game` when convenient. **Kenney 1-Bit is now the fixed and only
appearance**, including the 13 approved monochrome counterparts. There is no
artwork selector or launch override. Settings contain Sound, Effects and
Accessibility. F5 refreshes resources; missing/corrupt required art uses the
non-selectable emergency fallback or retains the previous good resources.
I opens stats/boons; the F/R/X bar has contextual ability icons. Existing E
inspection, dotted attacks, visibility and controls remain in use. Dressing
also applies to existing compatible concrete maps without entering a save.

Open [the offline pixel gallery](visuals/environments/index.html) for eight
generated-room views in the fixed look and the small semantic icon family.
It executes the **actual `main.lua` world pass**, native resolver and image
compositors with a CPU raster adapter and the original source pixels. This is
**not a LÖVE screenshot**, GPU equivalence test or human playtest. Text is
omitted, spectator visibility is enabled for art inspection, and CPU primitive
edges do not reproduce GPU antialiasing. It leaves the run snapshot/RNG intact.
No LÖVE application was opened, as requested.

```sh
python3 tools/environment_preview.py          # rebuild disposable previews
python3 tools/environment_preview.py --check  # compare exact PNG/HTML bytes
```

## Room artwork

| Approved direction | Actual use |
| --- | --- |
| Crypt + pale Sanctuary | One combined room family in existing Hollows/Observatory/Archive side rooms. Pale pebbled floor, carved masonry, wall-attached graves/bones/windows. Existing memorial/font rooms select this look. **No eighth biome.** |
| Observatory mosaic | Pale geometric ground with coherent cool stone wall pieces. |
| Root garden | Ruins side-room variant: a bounded three-by-three flower-bed patch and wall vegetation. Existing blocking trees keep their existing collision. |
| Drowned sanctum | Connected water banks in Aqueduct rooms, using all outer and native concave corners. Arches/alcoves are wall decoration. Water mechanics remain unchanged. |
| Forge masonry | Existing Bastion/Forge rooms receive dark framed masonry and iron panel/bar dressing. |
| Archive carpet | A bounded three-by-three carpet patch, assembled from its complete border family, under actors and tactical overlays. No fake blocking carpet. |
| Broken cavern | Rough connected wall masses/caps plus rubble attached to existing walls. No door image used as ground. |
| Ruins masonry | Two coherent neutral/carved wall families selected deterministically by existing room. |
| Camp furniture | Existing camp contexts get table/seat dressing on room walls. No new inn interaction or collision. |
| Workshop fixtures | Existing attunement forge/trader contexts get supplies, hearth or seat art. No crafting economy. |
| Archive shelves | Full/empty shelf **visual variants** on walls; no invented loot-storage mechanic. |
| Chapel windows | Wall-attached windows/alcoves in eligible room families. |
| Prison dressing | Ironwork and bar motifs on existing Forge/Bastion walls. No new prison rule or impassable sprite. |
| Stone columns | Existing pillar blockers use inspected top/middle/base assemblies, each component separately fog checked. No additional blocker placed. |
| Secret murals | Existing crack/spiral clues gain local face/sigil/leaf motifs while preserving their drawn clue strokes and interaction. |

Room dressing has at most **two props per room**. It uses wall cells beside
real open ground, avoids reserved secret cells and explicit authored visual
overrides, and never places an invisible collision object. Material choices
use a separate deterministic derived seed; they do not draw from `state.rng`.
The plan is cached until floor/event identity changes, not rebuilt per tile.

### Source provenance and exact native regions

All source PNG bytes and archived user reviews remain unchanged. The mixed
RPG ground/carpet choices below are historical review evidence; current ground
and masonry use the native monochrome alternatives. Only the specified water,
rough wall, rubble, columns and vents retain cross-sheet counterparts. Native cells
are zero-based, with the audited 17-pixel stride on RPG/Caves and 16 on 1-Bit.

* Pale ground: RPG **8**; mosaic: Caves **512**.
* Ruins walls: RPG **704–706 / 761–763 / 818–820**. Carved: **707–709 /
  764–766 / 821–823**. Cool stone: **710–712 / 767–769 / 824–826**.
* Flower bed: RPG **344–346 / 401–403 / 458–460**.
* Carpet: RPG **922–924 / 979–981 / 1036–1038**. The first inspected range
  started two columns too far right and included masonry; the pixel gallery
  exposed that error before this gate. Its actual center **980** now has a
  source-coordinate regression assertion.
* Water banks: RPG **2–4 / 59–61 / 116–118**; concave NW/NE/SE/SW
  **115 / 114 / 57 / 58**.
* Rough walls use Caves frame **38**, center **39**, rubble **3/5**; iron
  accent **79**. Pillar columns: **146/175/204** and **147/176/205**.
* Native monochrome wall grid: 1-Bit **18–20 / 67–69 / 116–118**; pale
  ground **5**, mosaic **341**, carpet **852** with connected border.

Monochrome uses native counterparts where available. The expressly approved
drowned banks, broken cavern and carved columns use cached neutral derivatives
of their original cells. Water conversion distinguishes water from land rather
than erasing their boundary with one brightness threshold. Original alpha is
retained on props. Floors are subdued for actor/outline contrast. Room surfaces
are fixed-size disposable pages with integer Quads and nearest filtering;
they do not edit source pixels or repack authored asset IDs.

## Presentation

* **Weapon/action icons:** F/R identify the actual class attack; X uses a
  movement arrow. Existing trap/bulwark/familiar/sentry/beacon objects are reused
  for the relevant class action. No new weapon family uses excluded Micro 78/79.
* **Status/supply icons:** existing Burning, Ward, Focus and Stormcaller charge
  values have matching icons and readable text. Charge shows its actual 0–3
  fill state. A sprite does not grant or alter a status.
* **Static combat cues:** physical, fire, lightning, spirit/echo, blocked hit,
  boon and real recovery choose small semantic artwork. The previous duplicate
  large generic hit splash/full red-cell fill is removed for these cues.
  The existing effect cap remains **eight** per beat; native cues occupy **0.4
  tile** and respect the visible floor/cell. Missing art uses the small ring
  fallback. Tactical dotted outlines remain visible and are drawn above
  scenery/reward markers. Existing authored theme effects keep their path.
* **Trap/state markers:** vent/shortcut, pressure-plate and quest-plate art has
  small armed/safe/spent badges based on actual state. Arrows retain direction.
* Actual `Healed`/`SupplyCollected` playback can use an **80 ms** shared beat;
  it does not change simulation time or wait separately for every chain event.
* Existing frames, TTF text, bars, minimap and intentional tactical primitives
  stay drawn. This is not a claim that every rendered pixel has a pack sprite.

Ten material identities, 27 prop identities and 29 semantic symbols add **66**
stable bindings: the fixed native renderer resolves **491/491**. The live
[catalogue](sprite-audit/index.html) audits each material's actual sampled
cells; derived page coordinates are never mistaken for authored source cells.

## Safety and verification

`idea_environment_content.lua` is curated plain metadata;
`idea_environment.lua` owns pure planning, cached image/Quad resolution and the
fixed surface compositor. `idea_presentation.lua` only selects semantic art.
`Game`, scheduler, entity definitions, profile and `roag-run/11` have no
environment-schema changes. No resource handles enter a snapshot.

Optional resource failure keeps native old materials/props where possible;
missing/corrupt required sheets retain the existing whole-pack fallback and
F5 previous-good rejection behavior. New resolver signatures include optional
source availability so restoration is not mistaken for an unchanged pack.
No arbitrary image path, callback or shader is read from a run.

```sh
TMPDIR=/var/tmp ROAG_TEST_RANGE=610-629 luajit tests/run.lua
sh tests/idea_environment_disk_resume.sh
sh tests/idea_terrain_disk_resume.sh
sh tests/idea_interaction_disk_resume.sh
python3 tools/environment_preview.py --check
TMPDIR=/var/tmp luajit tests/run.lua
make item-room-review-test sprite-ideas-test enemy-review-check surplus-audit-check
git diff --check
```

Twenty focused cases cover real generated seven-biome room families, all
256 connectivity cases, source-pixel sampling, native/monochrome fallback,
bounded cached resources, snapshot/RNG invariance, save reopening, genuine
damage-event art, and the actual world/HUD draw path with minimap hidden and
fog intact. A draw-pass inspection found dressing/rewards erroneously placed
in the minimap pass; they now render independently of minimap visibility.
The diagnostic setter retires LuaJIT traces after changing closed fixture
upvalues; ordinary gameplay does not use that injection mechanism.

Separate processes verify exact room plans/handles in the fixed look, 25 resolver
replacements each, exact legal-action events/RNG and checked save generations
1→2. These are headless resource tests, not live GPU F5 tests. The full-suite
result and retained logs are recorded in the
[progress ledger](goal/ROAG_IDEAS_IMPLEMENTATION_PROGRESS.md). Real LÖVE GUI,
human contrast/readability, physical mouse/keyboard/Wacom and native GPU
performance remain outstanding. Do not infer a historical crash fix from
these headless passes.

## Human checks when convenient

1. Start the game. Verify the fixed 1-Bit look, existing character choice,
   room ground, connected walls, water banks and furnishings remain readable.
2. Toggle **Tab**: pickups, terrain markers and wall dressing should stay in
   the world while only the minimap changes. Hidden dressing must stay hidden.
3. Aim F/R/X near a furnished wall and fight normally: dotted locked cells,
   actor silhouettes and HP should remain clear through the smaller cues.
4. Check **I** status labels against actual Burning/Ward/Focus/charge values.
   Visit an existing camp/forge/memorial and inspect the contextual art.
5. Save & Quit, Continue, and use F5; the run/floor must stay intact. Missing art must retain safe emergency drawing without exposing an alternate
   selectable style.

Report class, biome/floor, action context, sprite choice, screenshot and whether
this ran under WSLg/software rendering or native hardware. These manual checks
are **outstanding**; no physical keyboard, mouse or Wacom was operated here.
