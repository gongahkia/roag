# Player previews, simpler artwork choices and fixed camera

Current artwork choices supersede the original two-choice list below:
**Classic built-in**, **Kenney pixel art** (Micro actors/Caves terrain), and
**Kenney 1-Bit**. See [the current guide](ROAG_ONEBIT_PACK.md) and
[coverage audit](ROAG_SPRITE_AUDIT.md). Fixed camera/controls remain as described.

## Current behavior

- Sword, Sweep and Dash aiming use the **same dashed tile borders** as enemy
  windups. Tile interiors stay clear. Sword remains yellow, Sweep orange;
  Dash retains green/open, red/blocked and gray/unreachable feedback.
- The character panel omits Seed, Danger and Next Threat. Its remaining rows
  show level/XP, HP, attack/defense, speed/gold and boon count.
- Settings offers **Classic built-in** shapes and **Kenney Micro Roguelike**
  only. Classic is the fresh-install default; an existing valid Kenney choice
  remains selected. A saved choice for a removed pack safely uses Classic,
  without changing the active-run or profile files.
- The camera uses **40px tiles**, previously 48px: more of the map fits on
  screen. Wheel and +/- no longer zoom, including in spectator mode. Wheel
  navigation in menus still works. Fog range and simulation geometry are
  unchanged.
- Kenney ground uses its native plain floor region. The scattered wall-cap
  stamp was removed. Actual doors, stairs, walls and gameplay terrain markers
  still appear where their respective objects or mechanics exist.

Run `make game`, then Settings to select artwork, or `make kenney` for a
one-launch override. **F/R/X**, direction, then **Enter** aim/commit; **Esc**
cancels without advancing time. **I** opens boons/stats. **F5** revalidates the
selected pack while preserving authoritative state.

## Removed resources

The generated Dungeon PNG pack, its source/draft sheets, manifest, loader and
build scripts have been removed from ROAG. Automatic and Published Paint are
not available game renderer choices, and neither remaining choice activates
Paint visual, theme or atlas overrides. The sibling Paint project, game audio
and authored world geometry contracts are unchanged. Generic format readers
remain for those contracts and their independent tests.

A local reversible copy of the retired files was kept outside the repository
at `/var/tmp/roag-retired-art-20261017`; this is a temporary artifact, not a
runtime dependency. Historical art reports/captures remain labelled as such.

## Captures

These are actual callback-driven LÖVE frames:

- [Player Sweep preview](visuals/player-attack-preview.png)
- [Two artwork choices](visuals/menus-settings.png)
- [Character panel](visuals/menus-boons.png)
- [Wider view and compact HUD](visuals/menus-hud.png)

## Verification

Baseline before this cleanup: **332 headless tests**. Current suite:
**327/327**, executed as ranges `1-150` (150), `151-260` (110), `261-300`
(40), `301-1000` (27). Nine retired-Dungeon tests were replaced by two retained
generic resolver/wall checks; two new checks cover plain-ground mapping and
retired preference recovery. No gameplay tests were removed.

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp xvfb-run -a sh tests/attack_preview_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/menu_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/kenney_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/enemy_facing_gui_smoke.sh
TMPDIR=/var/tmp xvfb-run -a sh tests/repeat_gui_smokes.sh 1
TMPDIR=/var/tmp xvfb-run -a sh tests/motion_gui_smoke.sh
TMPDIR=/var/tmp sh tests/visual_packs_disk_smoke.sh
```

Accepted artifacts under `/var/tmp/`:

- `roag-fixed-preview-check`: five actual frames compare every previewed
  cell's interior against its baseline, verify border colors and unchanged
  snapshots, and exercise the removed wheel/+/- bindings.
- `roag-ui-polish-release`: three processes, 20 frames, exact Save/Continue,
  character text assertions, two-choice settings, menu input gates and 100
  open/close cycles without increased texture memory.
- `roag-trimmed-kenney-check`: four processes, source-pixel comparisons,
  Classic/Kenney switching, save/reopen, corrupt warm-refresh rejection and
  cold-start fallback; the survey confirms ground without wall-cap stamps.
- `roag-kenney-facing-check-2`: six frames, actual mirrored native enemy body
  pixels, clear warning interiors and a genuine enemy windup.
- `roag-fixed-ui-regressions`: **9/9** established GUI harnesses, including
  rewards, exploration, stages and persistence.
- `roag-motion-gui-d7rShQ`: 11 movement/Dash/camera frames passed.
- `roag-visual-pack-uYyFQM`: two-process settings/recovery passed.

The initial new preview test expected a nonexistent `blocked` status; the
actual contract uses `wall`, now checked. Adapting the old enemy pixel test to
8px Kenney art initially sampled its overlaid HP bar; the accepted check samples
the visible body above that bar. Assertions and failed logs remain in the
earlier `/var/tmp/roag-preview-outline-check` and `roag-kenney-facing-check`
directories. These were test-fixture errors, not game crash evidence.

LÖVE **11.5**, Fedora WSL/Xvfb, Mesa llvmpipe software rendering. No physical
keyboard/mouse/tablet or native GPU performance was tested. These passes do
not establish a fix for previously reported intermittent GUI hangs.
