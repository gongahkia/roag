# Kenney boon icons and mechanical terrain

> Background update: [Roguelike Caves & Dungeons](ROAG_CAVES_DUNGEONS.md) now replaces Micro terrain. The current setting is **Kenney pixel art**; characters and boon bindings are retained. This report preserves its earlier verification.

The existing Kenney choice now combines **Micro Roguelike 1.4** with Kenney's
**1-Bit Pack 1.2**. The latter's blocky transparent pixel symbols complement
the existing characters without adding another choice to Settings.

Official source: [Kenney 1-Bit Pack](https://kenney.nl/assets/1-bit-pack),
16×16, CC0. The original sheet and license are under
`assets/kenney/1-bit-pack/`. Only one unchanged 17,497-byte PNG is bundled.

## See it in the game

```sh
make kenney
```

For a persistent choice, select Settings → Kenney Micro Roguelike → Enter.
Open a chest to see each offer's icon and the selected offer's larger image.
After acquiring a boon, **I** opens its icon in the boon list; **Enter** opens
the same artwork in Details. All **52 current boons** have explicit, distinct
bindings, rather than an arbitrary icon selected by list position. Classic
still uses the original shapes/text presentation.

Actual captures:

- [Chest offers and selected art](visuals/kenney-boon-chest.png)
- [Boon list](visuals/kenney-boons.png)
- [Class transformation icons](visuals/kenney-boons-class-kits.png)
- [Six mechanical terrain types](visuals/kenney-terrain-mechanics.png)

The terrain capture's row, left to right: bramble, bog, ice, barricade, mist,
crystal. This is an explicitly assisted rendering fixture, not a legal-action
run. The sprites are drawn by the actual normal game renderer. Names/effects,
collision, LOS, scheduling, generation, run data and RNG are unchanged.

## Terrain mapping audit

Plain floors, connected walls (47 variants) and water retain Micro Roguelike's
existing rendering. No discarded wall-cap stamps return to ordinary ground.
All six mechanical terrain symbols now use the additional native sheet:

| Terrain | Earlier representation | Current native symbol |
| --- | --- | --- |
| Bramble | Gravel-like Micro tile | Thorn bush (104) |
| Sticky bog | Plant prop | Uneven ripple strokes (268) |
| Glazed ice | Water edge | Blue ice-star symbol (616) |
| Barricade | Rug-like prop | Cross-braced crate (121) |
| Obscuring mist | Drawn circle, no sprite binding | Pale layered wisps (267) |
| Resonant crystal | Generic Micro gem/icon | Faceted crystal (523) |

These are representative symbols, not a new set of literal painted terrain
surfaces: the mist uses native layered ripple strokes as wisps, for example.
Each has a subdued backing and a 32px native marker inset four pixels into the
40px tile, keeping the 16px source at exact integer 2× scale. Remembered terrain
retains the existing dimming. No hidden geometry/entities are exposed.

## Runtime and source separation

`src/kenney_symbols.lua` contains deliberate stable-ID mappings, source region
indices, motif descriptions and tints. The packed sheet uses **16px stride**,
not the alternate spaced sheet's 17px stride. `src/kenney_visuals.lua` validates
and caches it as one extra texture with shared Quads, alongside the existing
three Micro textures. Icons are 32px, chest details 64px and boon details 96px:
integer multiples of their native 16px size.

`main.lua` applies optional tints without changing source pixels. All art stays
within the existing Kenney choice; no Paint override or removed Dungeon pack
is reinstated. A missing/corrupt supplement fails closed to Classic on cold
startup. A corrupt F5 candidate retains the current complete working visuals.
Its checksum is part of the presentation fingerprint. There is no decode or
texture allocation per icon draw, and no simulation/schema change.

## Verification

Baseline **327/327**; final **330/330** headless checks, ranges `1-150` (150),
`151-260` (110), `261-1000` (70). Three new checks assert complete/distinct boon
coverage, all six terrain bindings/integer placement, and missing/corrupt/
wrong-size/out-of-range supplement rejection. Existing native-image tests now
check all four cached textures. The full gameplay/generation/save suite remains.

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp xvfb-run -a sh tests/kenney_symbols_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/kenney_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/menu_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/attack_preview_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/repeat_gui_smokes.sh 1
```

- `roag-boon-terrain-symbols-final` under `/var/tmp/`: 11 game frames,
  all 52 boon icons over eight list pages, all six terrain sprites/backings,
  corrupt-then-restored same-process F5, exact snapshot/RNG equality.
- `roag-symbols-fallback-verified`: five fresh processes; actual source/player
  pixel comparisons, saved settings/run resume, warm-corrupt rejection, cold
  corrupt Micro sheet fallback and cold corrupt supplement fallback.
- `roag-boon-art-menu`: three processes, 20 frames; real chest and detail UI,
  keyboard/mouse callbacks, exact Save/Continue and menu resource reuse.
- `roag-symbols-preview-verified`: clear dotted attack/Dash previews and fixed
  camera controls still pass.
- `roag-symbols-regressions`: **9/9** established GUI harnesses passed.
- Headless output: `/var/tmp/roag-symbols-headless-{1,2,3}.log`.

The symbol pixel test compares every opaque native icon pixel at its actual
screen coordinate, with a maximum **two channel-byte** tolerance for tint
quantization in vertex colors and the RGBA8 framebuffer. The first 1.1-byte
tolerance detected a 1.22-byte difference on llvmpipe; the spatial/source checks
were retained. Untinted existing actor comparisons retain their prior exact
tolerance. The initial fixture also used the wrong test grant name, corrected
to the existing `Game.set_boon_stacks`. Failed logs remain in the earlier
`roag-boon-terrain-symbols*` directories; no assertions were hidden.

Environment: LÖVE **11.5**, Fedora WSL/Xvfb, Mesa llvmpipe. Inputs were
callback-driven; no physical mouse, keyboard, Wacom or native GPU performance
was tested. No accepted run hung or crashed; historical GUI instability is
not claimed fixed.

## Boon source map

Indices are zero-based in the unchanged 49-column packed sheet. All references
remain `core:boon/<slug>/icon` and `/detail`.

| Boon | Native tile (zero-based) | Source motif |
| --- | ---: | --- |
| Storm Conductor | 616 | radiating spark |
| Detonation Bloom | 618 | burst |
| Thorn Mirror | 233 | spiked shield |
| Static Footsteps | 1012 | winged boot |
| Resonant Wounds | 708 | clustered stars |
| Temporal Echo | 629 | hourglass |
| Crescent Reach | 563 | curved slash |
| Turncoat Spark | 617 | small spark |
| Emberbrand | 567 | flame |
| Flashpoint | 566 | fire burst |
| Grave Dividend | 622 | skull |
| Opening Cut | 326 | dagger |
| Crowdbreaker | 384 | great axe |
| Harrier's Response | 623 | target corners |
| Perfect Guard | 184 | round shield |
| Final Spark | 619 | fading spark ring |
| Pulse Step | 705 | pulse ring |
| Packed Powder | 486 | bomb |
| Longblade Geometry | 378 | long sword |
| Phantom Lunge | 564 | long slash |
| Guardian's Vow | 236 | ward shield |
| Prospector's Heart | 818 | coins |
| Circuit Chorus | 575 | linked nodes |
| Crownfire | 141 | crown |
| Stonewake | 262 | stone block |
| Bramble Compact | 104 | thorn bush |
| Froststep Catalyst | 707 | frost sparks |
| Fracture Dividend | 2 | stone fragments |
| Mist Veil | 717 | drifting flecks |
| Crystal Harmony | 522 | faceted gem |
| Sunderer's Mark | 383 | pickaxe |
| Counterweight | 230 | mace |
| Bloodrush | 1013 | rushing boot |
| Pain Dividend | 532 | marked heart |
| Executioner's Thread | 381 | execution axe |
| Shared Agony | 531 | paired hearts |
| Windup Hunter | 624 | target reticle |
| Crossfire Omen | 569 | crossed blades |
| Pursuit Spark | 1010 | movement arrow |
| Ordered Ranks | 187 | formation shield |
| Far Witness | 771 | eye |
| Betrayal Ward | 185 | quartered shield |
| Rhythm of Steel | 823 | musical notes |
| Cluster Rupture | 830 | scattered burst |
| Splitflight | 282 | drawn bow |
| Recurve | 333 | recurve bow |
| Expanding Sigil | 709 | circular sigil |
| Alchemical Flip | 669 | potion |
| Earthshaker | 380 | war hammer |
| Battle March | 89 | marching boots |
| Aftershock | 706 | shock burst |
| Reprisal Nova | 716 | radial burst |
