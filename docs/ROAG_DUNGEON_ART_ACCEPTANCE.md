# Dungeon art: rendered acceptance, 2026-10-17

The previous motif pack failed visual acceptance. It mixed sparse transparent
stamps with flat fallback tiles, stretched a 64px backdrop across the viewport,
and mapped the player to an unsuitable crouched silhouette. A settings picker
did not solve those asset and rendering problems.

## Result and how to use it

Run `make game`. Automatic uses published Paint art where available and the
bundled Dungeon art otherwise. A saved Classic choice is respected; select
**Title → Settings → Dungeon pixel art → Enter**, or **Esc → 3 Settings** in a
running game. Saved `generated` choices already refer to the replacement pack.
Source IDs, combat, collision, fog, scheduler, RNG, run saves and profiles remain
unchanged. This is a static art replacement, without animation frames.

The active generation is `82841362c2cbce75`: 21 opaque, repeating 32×32 terrain
tiles, six class sprites and 25 regular/elite/boss/pursuer sprites. Each new
raster uses at most four opaque colors. All actors have transparent cell
borders, a consistent baseline and rebuilt pixel outlines. The seven biomes
use distinct terrain palettes. Existing boon/drop/chest/HUD icons remain from
the earlier icon sheet; this pass does not claim newly authored icons or unique
architectural motifs for every biome.

Every generated floor/wall/water tile now draws at full coverage. Generated
terrain no longer reveals flat fallback squares through transparent margins.
Wall faces connect and receive a small bevel where a known adjacent cell is
open. Unknown cells cannot affect that bevel. Mechanical terrain keeps its
separate semantic tint and symbol. Remembered tiles darken after drawing the
opaque surface. The stretched generated backdrop and old fallback doodles are
omitted for this tiled pack. Published Paint textures keep their own behavior.

## Actual frames

These are captures from LÖVE, not imagegen mockups. The same seed-1 normal
entrance is shown before and after. The combat frame is a labelled spectator
run with invulnerability; ordinary move/wait inputs reach a genuine pending
enemy strike. It is not a human playtest or an ordinary-run Victory claim.

- [Before](visuals/dungeon-before.png)
- [Normal game after](visuals/dungeon-after.png)
- [Generated room after](visuals/dungeon-room.png)
- [Real committed telegraph](visuals/dungeon-combat.png)
- [Settings](visuals/dungeon-settings.png)

Visual inspection checked all seven biome captures, all six class captures,
normal fog, the real pending strike, Continue and corrupt-atlas fallback. The
accepted result has a continuous floor, legible connected walls, an upright
player and visible red attack tiles. This is an engineering/agent visual
acceptance judgment; physical keyboard/mouse/tablet and native GPU performance
remain unverified.

## Reproducible gates

```sh
# Build real lossless PNGs and validate decoded pixels without touching checkout
TMPDIR=/var/tmp sh tools/build_generated_art.sh
# Explicit activation after the complete generation validates
TMPDIR=/var/tmp sh tools/build_generated_art.sh --apply
# Actual LÖVE processes, isolated save directories and screenshots
TMPDIR=/var/tmp xvfb-run -a sh tests/visual_acceptance_gui.sh
# All headless tests; ranges are available for bounded execution environments
TMPDIR=/var/tmp luajit tests/run.lua
```

The compiler checks PNG readback, exact decoded terrain bytes, full alpha on all
21 tiles, all opposing edge pixels, four-color limits and clear actor cell
borders. Repeated unchanged builds produce the same PNG bytes and manifest.
It activates the manifest after all four matching pages are copied/read back.
A torn or mismatched pack fails checksum validation and uses safe built-in art.
Old generation files and the original imagegen sheets are preserved.

The visual harness runs four fresh LÖVE processes: normal Settings/New Run and
pack switching; Save & Quit/Continue; intentionally corrupt terrain with the
same saved run; and a seven-biome/six-class spectator survey plus legal-input
combat approach. Pack switching, F5 and camera survey compare authoritative
snapshots exactly. Continue compares the full saved snapshot. Screenshots and
failure logs are retained under the printed `/var/tmp` output path.

Runtime: LÖVE 11.5 / LuaJIT, Fedora WSL, Xvfb, Mesa llvmpipe OpenGL 4.6. This
software renderer is not a native-GPU FPS measurement.

Final verification:

- Headless: **321/321** (previous verified total 319, two new coverage/fog-border
  tests). Executed ranges `1-150`, `151-260`, `261-1000`: 150 + 110 + 61 passes.
- Visual acceptance: **three passing rounds**, four fresh LÖVE processes per
  round, 21 actual screenshots per round. Output directories:
  `/var/tmp/roag-dungeon-accepted-1`, `...-2`, `...-3`. The final round also
  rejects a corrupt warm F5 reload, keeps the live generated visuals, restores
  valid data and refreshes again without a snapshot change.
- Established GUI group: **9/9** in the final round, results/logs under
  `/var/tmp/roag-dungeon-regression-final`.
- Targeted motion, boon build, biome and finale GUI suites: **4/4**. Biome
  durable resume: six fresh LÖVE processes; finale resume/terminal tombstone:
  three fresh processes. Logs under `/var/tmp/roag-dungeon-targeted` (the
  corrected motion/biome checks use `-final.log`).
- Rebuild: the same `82841362c2cbce75` fingerprint, with byte-for-byte `cmp`
  equality for all four PNGs and the manifest. Dry run left the active manifest
  checksum unchanged. Syntax compilation and `git diff --check` passed.

## Regression investigation

The first established-suite run exposed stale harness assumptions from earlier
UI work: the fog test sampled using a 32px tile despite 48px default zoom, and
persistence tests searched for the previous title's Continue wording. Targeted
tests also expected a fixed 11px player token, untransformed enemy draw
coordinates and mixed-case biome HUD names. The fog sampler now uses the actual
camera and excludes the actual minimap; its pixel/fog assertion remains. Motion
checks use the real zoom and graphics transform. Continue and biome checks use
the current menu labels; exact save, next-action, movement and telegraph
assertions remain. Assertions in the affected motion/fog/persistence/biome
screenshot harnesses now log and exit instead of escaping callbacks. Initial
failing evidence is retained in `/var/tmp/roag-dungeon-regression`,
`...-fixed` and `/var/tmp/roag-dungeon-targeted` (original logs). These were
harness assertion failures/timeouts; no LÖVE signal-139 root-cause fix is claimed.

An early new Continue fixture also failed because it tracked New Run but did
not track `Game.restore`, so it compared `nil` against the saved run. Tracking
restore and submitted actions fixed that fixture; disk inspection independently
confirmed the saved snapshot itself was exact. That failed log remains in
`/var/tmp/roag-visual-final-round1`.

## Follow-up: enemy facing and clear attack borders

Enemy generated/published sprites now mirror for the horizontal movement leg or
locked attack currently being presented. Vertical motion and symmetric area
attacks retain facing; forced displacement does not turn an actor. Completed
facing stays in presentation memory. Continue reconstructs it from the saved
pending attack or existing AI movement memory, without changing save formats.

Windup, impact, cancellation and live pending-strike tiles use dashed borders
instead of filled tiles. Amber means windup playback, red means impact/live
pending strike and gray means cancellation. The old source ring no longer
overprints attack sprites. Other effect overlays retain their existing style.

Reproduce the focused actual-render check with:

```sh
TMPDIR=/var/tmp xvfb-run -a sh tests/enemy_facing_gui_smoke.sh
```

The test compares opaque atlas sprite pixels against actual captured left/right
renders, verifies clear warning interiors, checks movement/vertical retention
and reaches a genuine guard windup using legal wait inputs. Directed left/right
pose checks are explicitly assisted presentation fixtures. Three isolated runs
passed (18 captures). See [left](visuals/enemy-facing-left.png),
[right](visuals/enemy-facing-right.png) and
[generated-floor pending strike](visuals/dungeon-outline-combat.png).

Headless regression: **325/325** (previous **321/321**), split into ranges
1–150, 151–260 and 261–1000. Motion GUI passed its 11 frames. The full visual
acceptance harness passed its four fresh processes including Save/Continue,
classic/generated selection and corrupt-art fallback (21 captures). Nine
established GUI harnesses passed in a sequential retry round. The first round
had one enemy-harness failure reporting only `X connection to :99 broken`;
its log remains at `/var/tmp/roag-facing-regressions/1-enemy_gui_smoke.log`.
The isolated enemy rerun also passed. No root cause for that display failure
is claimed. An initial new-test pixel mismatch came from the old source ring
painting over the sprite and passed after removing that overlay for attacks.

Runtime: LÖVE 11.5, Mesa llvmpipe software rendering under Xvfb. All input was
callback-driven; no physical keyboard/mouse/Wacom or native GPU was tested.
