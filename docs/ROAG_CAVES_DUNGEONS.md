# Kenney Caves & Dungeons background replacement

The **Kenney pixel art** setting now uses
[Roguelike Caves & Dungeons](https://kenney.nl/assets/roguelike-caves-dungeons)
for ordinary floors, walls and water. This replaces the Micro Roguelike
background rendering. Micro characters/items and the existing 1-Bit boon and
mechanical-terrain symbols remain. Settings still has only Classic and Kenney;
the stored `kenney` ID is unchanged.

## Use it

```sh
make kenney       # one-launch override
make game        # respects your saved selection
make spectator   # disposable Kenney inspection, no run/profile saves
```

To keep the choice, select **Settings → Kenney pixel art → Enter**, or
**Esc → 3 Settings** while playing. F5 validates and refreshes the selected
art without changing the active run. Camera scale remains fixed at 40px.

## Source inspection and mapping

The original archive's sheet, sample scene, preview and tile-spacing notes
were inspected, along with enlarged numbered contact sheets. It is a **16px
sheet with 1px gutters**, not the packed 8px sheet used by the characters.
Native region coordinates therefore use `column*17, row*17, 16,16`. The
unchanged 492×305 PNG, license and provenance are in
`assets/kenney/caves-dungeons/`.

| Biome | Ground (zero-based native indices) | Wall material | Water |
| --- | --- | --- | --- |
| Overgrown Ruins | 67, plain speckled stone | stone | blue |
| Fungal Hollows | 212, plain earth | earth | green |
| Sunken Aqueduct | 335–338, gray floor pattern | stone | blue |
| Ashen Bastion | 364–367, warm floor pattern | stone | blue |
| Glacial Observatory | 306–309, pale stone | stone | blue |
| Fractured Archive | 422–425, sandy floor pattern | stone | blue |
| Sundered Forge | 393–396, earth floor pattern | earth | green |

These are cosmetic material assignments over the existing generated geometry.
They do not introduce new biomes. The initial source inspection/render caught
stripes from edge-bearing floor regions; ordinary stone/earth rooms now use
the appropriate plain center tiles. Door frames, pillars and railings are
never scattered onto walkable floor as decoration.

Wall faces use native frame/material pixels, adapted to ROAG's solid wall
cells. Water uses the native edge and inner/outer corner quarters, including
one-cell pools, narrow canals and L bends. Each of the two wall and two water
families has all **47 canonical connectivity masks** (188 cached regions).
Unknown neighbors are treated as connected; fog and remembered tiles keep
their established visibility rules. No actual map cell is modified.

The 1-Bit mechanical markers remain at integer 2× scale in their tiles. Their
backings now use a 42% tint wash, retaining the new ground texture underneath
instead of covering it with an opaque dark square. Icons, attack outlines,
actor facing, camera position, scheduler, RNG and saves keep their contracts.

## Actual game captures

- [Normal fogged entrance](visuals/caves-entrance.png)
- [Overgrown Ruins](visuals/caves-overgrown.png)
- [Fungal Hollows](visuals/caves-fungal.png)
- [Sunken Aqueduct / shoreline corners](visuals/caves-aqueduct.png)
- [Glacial Observatory](visuals/caves-glacial.png)
- [Mechanical markers over native floor](visuals/caves-mechanics.png)

Biome surveys use the real procedural generator and the explicitly labelled
spectator camera; they are not human playtests. Normal entrance/combat and
save/reopen captures are also available in the test artifacts below.

## Architecture and limits

`src/kenney_terrain.lua` owns native source validation, style tables, headless
neighbor/mask selection and source-pixel composition. `src/kenney_visuals.lua`
delegates terrain lookup to it and retains stable actor/item/boon IDs. The
previous gray Micro wall-composition code and floor/water mapping were removed.
`main.lua` uploads the new surface page once per load/refresh. There is no
decode, compositing or Quad allocation per terrain draw. Four textures remain
cached for the combined Kenney presentation.

The source image is untouched. Derived wall/shore textures live only in memory;
this is not a new atlas build/publication workflow. Missing/corrupt terrain
fails closed to Classic at startup; a bad F5 candidate preserves the current
complete visual state. The source checksum participates in its fingerprint.
Classic remains selectable and the fresh-install default. Audio, Paint source
projects, world content, simulation snapshots and player profile files are
unchanged.

16px terrain fills the existing 40px tile with nearest filtering. This is a
2.5× scale with alternating two/three-pixel widths, not an integer pixel scale.
All tiles share integer screen boundaries; no source gutters enter a Quad.
No native GPU performance claim is made.

## Verification

Baseline **330** headless checks; final **333/333**, ranges `1-150` (150),
`151-260` (110), `261-1000` (73). Three added tests check dimensions/17px
stride and rejected inputs; every wall/water neighbor mask in all seven
biomes, including unknown-neighbor privacy; and non-overlapping surface
assembly with exact native quarter pixels and an untouched source.
Existing Kenney tests now validate the new floor whitelist and source pages.
No gameplay regression tests were removed.

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp xvfb-run -a sh tests/kenney_terrain_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/kenney_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/kenney_symbols_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/menu_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/attack_preview_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/enemy_facing_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/repeat_gui_smokes.sh 1
```

Actual artifacts under `/var/tmp/`:

- `roag-caves-biomes-verified`: seven generated biome captures and two refresh
  captures. Unobstructed native floor pixels match across all seven; aqueduct
  shoreline pixels match the compiled native regions. Camera/F5 never changes
  the authoritative snapshot or RNG. Rejected F5 retains visible old artwork;
  restored F5 renders successfully in the same process.
- `roag-caves-save-refresh-verified`: six fresh processes, normal game render,
  exact actor pixels, persistent selection, exact Save/Continue snapshot,
  warm rejection and separate cold corrupt Micro/symbol/terrain fallback.
- `roag-caves-symbols-verified`: all 52 boon icons and six terrain mechanics
  still match source pixels over 11 rendered frames, including refresh.
- `roag-caves-menus-verified`: 20 frames across three processes, menus/chests/
  details, save/reopen, two pack choices and repeated open/close resource reuse.
- `roag-caves-outlines-verified`: unchanged dotted player previews and no zoom.
- `roag-caves-enemy-facing`: six real enemy-facing/outline frames.
- `roag-caves-regressions`: **9/9** established GUI harnesses passed.
- `roag-caves-headless-{1,2,3}.log`: complete headless/generation/continuation
  results, including existing legal-action full-run checks.

LÖVE **11.5**, Fedora WSL, Xvfb, Mesa llvmpipe software rendering. Inputs were
callback-driven. No physical keyboard/mouse/Wacom or native GPU was tested.
No accepted run asserted, crashed or hung; this does not establish a fix for
historical intermittent GUI issues. Temporary source inspections and the first
striped-floor captures remain in `/var/tmp/roag-kenney-caves` and
`/var/tmp/roag-caves-first-render` for inspection.
