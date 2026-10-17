# Kenney Micro Roguelike: selectable visual trial

This is a working trial of the **colored 8×8 Micro Roguelike 1.4** pack chosen
by the user. It is not a claim that every ROAG artwork slot has a unique new
sprite or that the visual direction has received human approval.

## Try it

```sh
make kenney
```

That command overrides the renderer for one launch and does not change the
saved preference. It opens the normal game title, with ordinary New Game and
Continue. For a persistent choice, select **Settings → Kenney Micro Roguelike
→ Enter**. In a run, use **Esc → 3 Settings**. All five pack choices fit the
settings panel and support keyboard, mouse and gamepad navigation.

`make game` still respects your saved choice; Automatic retains the existing
Dungeon art beneath any published Paint replacements. Classic, Dungeon and
Paint remain available. F5 revalidates visual resources without altering the
run. A damaged Kenney sheet falls back to built-in art on startup; a damaged
warm refresh is rejected while keeping the working visuals.

## Actual captures

These are screenshots of the real current LÖVE application:

- [Normal generated entrance](visuals/kenney-entrance.png)
- [Generated room](visuals/kenney-survey.png)
- [Genuine pending enemy strike](visuals/kenney-combat.png)
- [Settings selection](visuals/kenney-settings.png)

The room/corridor survey uses the explicitly labelled invulnerable spectator
run. Its camera movement is presentation-only; ordinary move/wait callbacks
then reach a genuine enemy windup. It is not a human playtest or ordinary-run
Victory. Agent inspection checked terrain continuity, source-pixel scaling,
actor silhouettes, the HUD icons and the dashed warning's clear interior.

## Integration and limits

Original source/license: [Kenney Micro Roguelike](https://kenney.nl/assets/micro-roguelike),
CC0. The untouched source sheet and license are under
`assets/kenney/micro-roguelike/`. Native 8px Quads use the **packed** sheet's 8px
stride, not the alternate spaced sheet's 9px stride. Current 32/40/48/56/64px
world zooms are integer multiples of eight. Nearest filtering stays crisp.

The trial covers six classes, 25 enemy/boss/pursuer bindings, native floor/water
art, 47 connected wall variants, currency/food/health, chests, doors, stairs,
exit, basic HUD icons and hit/boon art. Some enemies share source sprites;
attack/hurt reuse the static body. Five terrain mechanics use approximate
native props; mist retains its existing clear marker rather than being
misrepresented as ordinary floor detail. Boon-specific icons, most UI panels,
font, tactical overlays and some effects retain their existing rendering.
All seven biomes currently share the native palette. No animation, new game
content, biome reskinning or full UI makeover is claimed.

The initial raw mapping made wall interiors blend into the floor, so it failed
agent visual inspection. The adapter now constructs a bounded wall texture
from the pack's own border pixels and gray material. It handles convex and
concave corners and multiple missing diagonals. Unknown neighbors are treated
as connected, so edge selection cannot expose hidden geometry. Ground detail
uses a fixed coordinate pattern without gameplay RNG. Source art, existing
packs, collision, scheduling, profile and active-run schemas are unchanged.
The earlier unfinished generative-terrain experiment was parked as
`/var/tmp/roag-paused-terrain-expansion.patch` and
`/var/tmp/roag-paused-terrain-art-working.lua`; it is not part of this trial.

## Verification

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp xvfb-run -a sh tests/kenney_gui_smoke.sh
```

Previous verified headless total: **325/325**. Final: **329/329**, ranges
`1-150` (150), `151-260` (110), `261-1000` (69). Four new tests check native
regions/bindings, all 47 canonical wall masks, corruption/dimensions, hidden
neighbor handling, persistent selection and simulation isolation.

Three passing focused rounds: `/var/tmp/roag-kenney-accepted-1`, `...-2`, `...-3`.
Each runs four fresh processes, captures eight rendered frames plus the wall
texture, compares actual opaque player pixels against the native source,
checks every wall variant's opacity, switches Classic/Kenney without snapshot
changes, rejects a corrupt warm F5 candidate, and compares exact Save/Continue
snapshots. Cold corrupt-sheet fallback also resumes the same saved run.

All nine existing GUI harnesses passed (`/var/tmp/roag-kenney-regressions`).
The original-pack visual acceptance harness passed its four processes
(`/var/tmp/roag-kenney-original-pack-regression`), and motion GUI passed its
11-frame movement/Dash/camera checks. The existing pack-test helper now reads
the actual option list instead of assuming four choices. `git diff --check`
passed. No new assertion, hang or crash occurred in these accepted rounds;
this does not establish a fix for historical intermittent GUI/display issues.

LÖVE **11.5**, Fedora WSL, Xvfb, Mesa llvmpipe software rendering. All input was
callback-driven. No human keyboard/mouse/tablet or native GPU performance was
tested. The next step is the user's visual judgment of this trial before
expanding the pack's coverage or making it the default.
