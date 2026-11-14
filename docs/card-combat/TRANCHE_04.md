# Tranche 04: four-tile action board

Implemented a separate **Boggle Trial**: the existing arena on the left, a 6×6
action board on the right, and exactly four connected commands per committed
route. The arena forecast includes enemy turns, incoming damage, self-costs,
boons, protection, deaths and time between commands. It is a deterministic combat
trial, not a free player burst or an approximate sequence preview.

Baseline: `main`, `7eff0f74`, initially clean. The current Tranche 01–03 docs,
active Lua/LÖVE code and applicable instructions were inspected. Classic, Poker
Stamina and Fixed Stamina remain available. This tranche does not replace them.

## Launch and controls

Requires the existing LÖVE 11.5 / LuaJIT setup; no new dependencies.

```sh
./roag.sh --boggle-trial --trial-seed=202
make boggle-trial

./roag.sh --card-trial --trial-seed=202
./roag.sh --fixed-stamina --trial-seed=202
./roag.sh
```

The title also has **O BOGGLE TRIAL**, accessible by click or `O`. The original
Card Trial entry and shortcuts remain. Boggle setup has a seed carousel
(`101`, `202`, `303`; left/right) and Enter/Begin. Boggle always starts Adventurer
with **zero equipped boons**, independently of old-mode preset selections.

| Input | Result |
| --- | --- |
| Left press on any tile | Start a route; that tile is command one |
| Drag across shared edges | Add distinct orthogonal neighbours, up to four |
| Drag back to a selected tile | Truncate at that tile, removing the suffix and its forecast |
| Release | Leave the plan selected for inspection; never execute |
| Commit click / Enter | Execute the complete valid current forecast once |
| Arrows / WASD | Move the board cursor, never the actor |
| Space | Add the cursor tile, or backtrack to it |
| Backspace | Undo one tile |
| `1`–`4` / Tab | Focus a selected command's forecast state |
| `C` / Clear | Clear the route without cost |
| `Y` / Shuffle | Spend the one Shuffle use and one real Wait |
| Escape / right click | Clear a route; otherwise open the trial menu |
| Menu `E` | Emergency Reset, costing two sequential real Waits |
| F1 | Boggle controls help |
| Menu/result `R`, `P`, `T` | Replay same seed, choose seed, return to title |

Tile arrows command the hero independently of drag direction. An attack tile
does not open an aim picker. Legacy movement, attacks, utilities, developer
loadouts and stamina/card inputs are intercepted. Playback locks conflicting
input. Controller callbacks route D-pad to cursor, A to add, X to commit, B to
cancel, Y to Shuffle, shoulders to undo/focus and Start to menu; physical
controller usability remains untested.

The editor between fights uses `R` Rotate, `V` Swap, or `K` Skip. Click tiles or
use cursor + Space, then Enter/Apply. Escape cancels an unapplied selection;
Escape again opens the menu. An edit never executes merely from inspection.

## Route validity, timing and termination

A route is exactly four distinct available instance IDs connected orthogonally.
Any starting tile is legal geometrically. Each tile executes its printed effect
in order. Mouse, keyboard and authoritative validation share the same rules.
Every crossed grid cell is processed in pointer order, even between fast samples.
An exact diagonal/corner crossing is ambiguous and stops the drag rather than
inventing an L-shaped path. Leaving the board stops editing without commitment.

The whole four-command plan is validated through chronological execution on a
copy before any live command is accepted. A known blocked third Move therefore
rejects the entire route: no valid-prefix shortcut, time, RNG or resources spent.
Move never becomes a bump attack. Empty directional Strike/Sweep is a legal miss.
Dash uses the current resolver's positive travel, collision, terrain extension
and actual landing; zero travel is invalid.

Each accepted command pays its ordinary speed-scaled time. Base values at speed
100 are **100** for Move, Strike and all non-directional tiles; **130** for Dash;
**150** for Sweep. Existing enemy readiness, windups, impacts, recovery, statuses
and delayed effects resolve between commands. There is no extra Recover/Wait
after an ordinary route.

Legal lethal plans may commit. The HUD conspicuously identifies death and its
tile number. Death or fight clear stops execution immediately; the unexecuted
suffix remains unspent. A blocked suffix after an earlier terminal outcome does
not invalidate the already-terminal route. No old-route command carries into
newly spawned enemies.

Commit checks the current world/board witness and exact IDs. The witness includes
action/event/schedule/entity counters, RNG, positions/HP/stats, boons and pending
trial effects, including public spawn/stat changes. Duplicate/stale commitments
reject. An unexpected live resolver invalidation stops before that command,
preserves the suffix and displays an explicit defect message; it never substitutes
another action or target.

## Catalogue, supply and modifiers

`src/boggle_content.lua` contains the prototype values (`size`, `route_size`,
pool weight, protection, healing, slots) and the bounded tile definitions.

| Tile | Initial instances / ordinary weight | Base time | Lifetime / effect |
| --- | ---: | ---: | --- |
| Move N/E/S/W | 3 each, 12 total | 100 | Ordinary; one cardinal Step |
| Strike N/E/S/W | 3 each, 12 total | 100 | Ordinary; existing Sword Strike |
| Dash, one seeded direction | 1 | 130 | Ordinary; existing Adventurer Dash |
| Guard | 2 | 100 | Ordinary; refresh separate protection to 2 |
| Wound | 1 | 100 | Ordinary; pay 2 HP immediately |
| Dull Edge | 1 | 100 | Ordinary; next primary attack −1 damage |
| Enemy Fury | 1 | 100 | Ordinary; living enemies' next resolved primary attack +1 |
| Focus | 1 | 100 | Ordinary; next primary attack +1 damage |
| Mend | 2 | 100 | Finite; heal up to 4 HP, then retire |
| Sweep, one seeded direction | 1 | 150 | Finite; existing special sweep, then retire |
| Rhythm of Steel pickup | 1 | 100 | Finite; equip existing common boon, then retire |
| Detonation Bloom pickup | 1 | 100 | Finite; equip existing common boon, then retire |

There are **31 ordinary weight units and five finite instances**. Initial IDs
1–36 are shuffled by the independent `boggle:board/v4` stream. Seed 202 prints
Dash east and Sweep south. Finite IDs 32/33 are Mend, 34 Sweep, 35 Rhythm and
36 Bloom. Board positions are shuffled; these identities do not change.

Ordinary replacement samples the configured weighted pool **with replacement**.
There is no finite draw/discard deck. During planning, layouts and meanings stay
fixed. Only actually executed cells are consumed. If combat continues, those
cells receive newly identified ordinary instances in one transaction after the
route; unexecuted cells stay unchanged. Finite effects/pickups are never sampled.
Dead players get no replacements. Fight-clear holes wait for separate intermission
initialisation. Private forecasts can compute cloned replacement bookkeeping for
verification, but the renderer always displays the live board, never those future
random tiles or allocated IDs.

**Guard** belongs to its own pool on the trial state, capped at two. Refreshing
does not add beyond two. Existing defences, invulnerability, parries, pickup wards
and boon wards resolve first; Guard absorbs eligible remaining HP damage. It
does not overwrite another shield and persists across routes/fights until used.

**Mend** has no independent charge counter: the two finite instances are the
whole healing supply. At full health, it forecasts zero recovery and remains a
legal, time-consuming, retiring command. This differs intentionally from Mend
in the older stamina modes, whose full-health rejection and charges are unchanged.
It emits real healing with source `trial:boggle/mend`, never a Wait or pickup.

**Wound** pays two HP before its ordinary time advancement, bypassing all
defences/protection, clamped at zero and potentially lethal. It emits
`HealthCostPaid` and, if lethal, `Died`, without fabricating hostile damage or a
credited kill. Presentation maps that self-cost to the existing player hurt/death
cues, floating numbers and hit feedback. Damage/hostile-hit boons do not fire
merely because the cost was paid.

**Focus/Dull Edge** stack as separate visible pending counts. The next accepted
player primary attack captures their net modifier and clears both counts once,
even on a miss. The captured amount applies to the complete primary batch before
critical scaling and defence, clamped to nonnegative damage. Sweep's targets all
receive that command's modifier; secondary boon packets do not. Nonattacks,
Shuffle, refill and Reset do not consume or cleanse them.

**Fury** snapshots currently living enemy IDs. Each application adds one pending
point per such enemy. At primary attack resolution, including an already locked
windup, the bonus is bound to source/attack identity and consumed once. The
captured value applies throughout that batch, including multiple targets/impacts;
a resolved miss spends it. Movement, cancelled windups, DoT and secondary effects
do not. Newly spawned enemies do not inherit it. Existing cancellation and dead
source rules remain authoritative.

**Pickups** equip common Rhythm/Bloom and retire before their action's time
advances. The two slots start empty. Existing acquisition/activation events and
boon definitions drive presentation and reactions. Duplicate ownership or both
slots occupied makes the route invalid; no replacement modal appears mid-route.
Rhythm can affect a later Move/attack in the same route; Bloom can affect a later
credited kill. Merely crossing a board/controller tile emits no arena movement.

## Recovery and board customisation

**Shuffle:** one use across both fights. Acceptance spends it, clears the plan
and resolves one actual Wait with applicable Wait effects. If combat remains
active afterward, permute the existing instances using board RNG. Do not create,
retire, restore or cleanse anything. Death/clear stops before permutation, with
no refund. Hovering does not reveal the future permutation.

**Emergency Reset:** a secondary menu operation, legal without a route. Resolve
two actual Waits sequentially, stopping after either if the player dies or combat
ends. Only if still active, replace ordinary cells with fixed new ordinary
instances. Preserve finite instances in their positions, retirements, boons,
pool edits, protection, penalties and Shuffle allowance. Do not reseed or consume
board RNG for this fixed arrangement; combat RNG may advance normally.

The documented fixed arrangement cycles
`Guard, Strike N, Move E, Strike E, Move W, Strike W`, offset by row. In the first
row containing no finite tile, its first four cells instead become Guard. At
most five finite tiles across six rows guarantees such a row. Those four Guard
commands form a legal connected recovery route, not a promise of survival.
Their later consumption uses the ordinary edited refill pool normally.

Fight one ends **between routes**. Pending ordinary replacement cells initialise
exactly once, then offer **one** Rotate, Swap or Skip; there is no older-mode free
boon reward. Rotate selects one ordinary directional instance, rotates it 90°
clockwise and transfers one pool weight unit from its old type/direction to its
new one. Other visible instances stay unchanged; future refill odds change, with
total weight still 31. Swap exchanges any two visible positions, including finite
tiles, preserving IDs/effects/lifetimes and all weights. Skip changes neither.

Apply once, then enter fight two in the same chamber/clock with HP, Guard, boons,
unspent finite tiles, penalties, remaining Shuffle and timed/scheduled work intact.
Repeated callbacks cannot reinitialise or reapply an edit. Replay creates the
original pool, five finite powers, empty slots and one Shuffle again.

## Fixtures and independently verified examples

The arena is the existing 9×9 playable interior with its central two-cell pillar.
Both fights retain two Mossbound Guards and a Thornspitter, with their original
statistics, AI and timing. **Boggle only** moves fight one's second guard from
(5,5) to **(5,7)** to make harmful bridges relevant promptly. Other opening
positions remain (4,5)/(8,3); fight two keeps (3,8)/(9,4)/(3,2), using the normal
nearest-free spawn fallback. Coordinates include the outer wall. Classic and
older trial fixtures are unchanged.

Board coordinates below are row/column, one-based. With **seed 202**, before any
action, both routes are valid:

- **Ordinary alternative:** (5,2) → (4,2) → (3,2) → (3,1), or cells
  **26→20→14→13**: Strike W, Move E, Strike N, Dash E. The full scheduler forecast
  leaves **24 HP**, with **four enemy HP lost**, at time **430**.
- **Harmful bridge:** (2,4) → (2,3) → (3,3) → (3,2), or **10→9→15→14**:
  Wound, Strike N, Strike E, Strike N. Forecast leaves **19 HP**, with **six enemy
  HP lost**, at time **400**. The two-point self-cost and incoming hit are visible
  separately in the per-command HP timeline. These are full outcomes, including
  enemy responses/friendly fire, not claims that all enemy loss is player damage.

There are many routes, not one prescribed solution. Neither alternative is
universally better. Guards' existing one-point defence can completely absorb a
common one-point secondary boon packet; the Thornspitter has zero defence.
An activation therefore need not mean additional HP loss, and the full forecast
keeps that distinction. Later choice also changes with acquired boons: an independent
quiet fixture verifies **Rhythm pickup → Focus → Move W → Strike N** against a
full six-HP Thornspitter. Primary damage is five and common Rhythm supplies the
remaining one; without that bonus the enemy survives. Sweep's Focus bonus and
Fury's windup/multi-target bonus also have independent expected-HP assertions,
beyond merely comparing two executions of shared code.

## Forecast architecture and presentation

`src/boggle.lua` owns board/pool/finite state and route transactions.
`src/game.lua` still owns movement, attack geometry, damage, enemy AI, effects and
scheduler advancement. A private command token prevents raw actions bypassing the
board. Guard and attack-modifier hooks only act when `state.boggle` exists.

Forecast and commitment use the same chronological route executor and real
`Game.submit` calls. Forecasts deep-copy mutable game state, clone both RNG streams
and capture hypothetical events; they never dispatch presentation, saves, audio
or profile writes. A bounded 64-prefix cache reuses unchanged plans. Pointer
trajectories are processed immediately; superseded forecast work coalesces once
at the render boundary so even late callbacks get a current ghost in that frame.
Assets/content are reused, not recreated or serialized for pointer motion.

Comparison boundary: ongoing combat **after route + ordinary refill**; death or
fight clear **at that terminal boundary, before separate intermission work**.
Tests compare the complete canonical gameplay snapshot: entities/positions/HP,
statuses, wards, modifiers, boons, clocks/readiness/schedules, IDs, board/pool/
finite ownership, RNG and progression. Presentation/cache objects live outside
the snapshot. Intermission initialisation is tested separately.

The existing arena renderer/sprites and event playback remain. Inset numbered
movement markers and player/enemy ghosts show predicted positions. Actual Dash
traversal is distinguished from landing. Player/enemy attack footprints, enemy
HP transitions/deaths, overall HP/time, a four-step HP timeline, protection and
pending modifiers expose consequences immediately without waiting for animation.
`1`–`4` focuses the corresponding state. Enemy intentions remain visible. Boon
slots show upcoming acquisition with `+`; actual acquisition uses existing icons
and feedback. Dangerous tiles have `!` plus names; finite tiles have inner frames.
No poker, stamina, Keep, Exchange or unrelated currencies appear in this mode.

## Verification and measured responsiveness

Focused **31/31 Boggle tests** pass: geometry, whole-route rejection, stale
world/IDs, unexpected live invalidation, conservation/retirement, blocked Move,
empty attacks, modifiers/wards, boon ordering/slots, early death/victory, recovery
termination, edits, canonical fidelity/pure previews, persistence guards, ordered
pointer samples and keyboard construction/commit/cancellation. Existing Card
Trial **36/36** still pass. Relevant existing modules produced **162 passes and
one failure**; audio's 11 checks were also rerun after the final cue change.

The failure was reproduced on untouched `7eff0f74` before editing (109 relevant
passes / same one failure):
`dice declares six numbered faces, skull and miss with exact approved native bindings`.
`src/sprite_audit.lua:19` references missing
`assets/kenney/1-bit-pack/symbols.png`. Restored artwork was not changed to satisfy
that obsolete assertion. Asset validation passes **599 bindings / 67 unchanged
source PNGs**. Lua syntax and whitespace checks pass. The existing Classic GUI
save, load, preview-launch, ordinary-key and storage-isolation checks also pass.

The real renderer/public-callback journey completed both fights, Rotate, paid
Shuffle/Reset, harmful crossing, backtracking, release inspection, healing,
death/replay and old-mode launches. Its seed-202 policy took 19 routes and won
with 17 HP. All accepted route callbacks matched private real-logic snapshots.
The policy is test-only one-route lookahead, not an in-game planner or evidence
of human preference.

**Native X11/SDL input**, via `xdotool` against the actual rendered application,
separately completed slow/fast dragging, harmful crossing, backtracking, release/
commit, both Mends, finite Sweep, both boon pickups, utilities, death/replay,
Rotate and both fights: **14 routes, victory with 17 HP at time 5450**. Its Rotate
choice differed, so future refills and the journey differed. Poker, Fixed and
Classic menu launches/movement also passed. A passive observer recorded states;
it injected no gameplay callbacks or model edits. A separate native production
launch constructed/committed cells 1→2→3→4 using only keyboard: the preview's
**18 HP / time 400** matched the resulting screen exactly.

Measurements use a bounded 512-sample ring, from **UI input callback entry to
return from the actual `love.graphics.present` call containing the matching
selection/ghost**, including any present/vsync wait. This is **not physical
display latency**. Forecast timing covers only computation/cache lookup. PNG
capture occurs after the matching timing sample. Empty selections are excluded.

| Journey / measurement | Samples | p50 ms | p95 ms | Worst ms |
| --- | ---: | ---: | ---: | ---: |
| Native path feedback | 60 | 7.883 | 13.122 | 24.642 |
| Native forecast computation | 60 | 1.642 | 3.460 | 7.120 |
| Native matching ghost presented | 60 | 7.883 | 13.122 | 24.642 |
| Public-callback path feedback | 84 | 8.428 | 13.654 | 32.968 |
| Public-callback forecast computation | 84 | 1.176 | 4.682 | 10.637 |
| Public-callback matching ghost presented | 84 | 8.428 | 13.654 | 32.968 |

The p95 ≤50 ms target is met. Native samples include 14 changed live states,
18 four-tile forecasts and one cache hit; callback samples include 19 live states,
22 full routes and two cache hits. These include new starts, uncached routes,
cached backtracking, boon pickups and enemy responses, rather than replaying one
cached plan. Path and ghost timings coincide because both appear in that same
next rendered frame. An earlier update-only forecast path delayed late requests;
moving coalesced evaluation immediately before ghost drawing removed that lag.

Test environment: **1024×640**, LÖVE **11.5**, Linux/WSL2
`6.18.33.2-microsoft-standard-WSL2`, Intel i7-1355U (12 logical CPUs), Xvfb
1024×768, Mesa **26.2.5**, OpenGL **4.1 Core**, D3D12 Intel Iris Xe renderer.
`conf.lua` is nonresizable at 1024×640, so this is both normal and smallest
supported size. Screenshots were inspected for clipping, tile/ghost legibility,
HP consequences and board-edit explanations. No new font glyph dependency.

Reproduce:

```sh
make boggle-test
make card-test
make assets-check
xvfb-run -a -s '-screen 0 1024x768x24' sh tests/boggle_gui.sh /tmp/roag-boggle-check
xvfb-run -a -s '-screen 0 1024x768x24' sh tests/boggle_native.sh /tmp/roag-boggle-check
```

The native harness needs existing `xdotool` and `ffmpeg`. Its observer's F12
exits through SDL after the tests; it is absent from the production entry point.
Relevant regressions were run with the existing module runner interface:

```sh
luajit - <<'LUA'
local n,f=0,0
for _,m in ipairs({'boon_expansion','idea_vow','idea_dice','idea_cadence_boon',
 'motion','classic_catalog_visuals','pixel_ui','persistence','meta','title_menu','expansion_audio'})do
 require('tests.'..m..'_tests')(function(name,fn)
  local ok,why=pcall(fn);if ok then n=n+1 else f=f+1;print(name,why)end
 end,function(a,b)assert(require('src.persistence').encode(a)==require('src.persistence').encode(b))end)
end
print(n..' passed, '..f..' failed')
if f>0 then os.exit(1)end -- retains the known dice-art failure
LUA
```

Local ignored captures and receipts:

- [Opening board](../../artifacts/card-trial/tranche-04/checked-gui/native/native-opening.png)
- [Harmful route preview](../../artifacts/card-trial/tranche-04/checked-gui/native/native-costly-preview.png)
- [Same-route boon acquisition](../../artifacts/card-trial/tranche-04/checked-gui/native/native-route-1-preview.png)
- [Rotate preview](../../artifacts/card-trial/tranche-04/checked-gui/native/native-rotate-preview.png)
- [Second fight](../../artifacts/card-trial/tranche-04/checked-gui/native/native-fight-two.png)
- [Native victory](../../artifacts/card-trial/tranche-04/checked-gui/native/native-victory.png)
- [Keyboard before](../../artifacts/card-trial/tranche-04/native-keyboard-preview.png)
  / [after](../../artifacts/card-trial/tranche-04/native-keyboard-after.png)
- [Zero-heal / self-cost / incoming damage preview](../../artifacts/card-trial/tranche-04/native-heal-zero-preview.png)
  / [result](../../artifacts/card-trial/tranche-04/native-heal-zero-after.png)
- Timing and canonical receipts: `artifacts/card-trial/tranche-04/checked-gui/`
  and its `native/` subdirectory.

## Preservation, limits and human recipe

No assets, sprites, fonts, bindings, existing boon definitions, enemy statistics,
normal saves/profile schema or dependencies changed. Narrow shared changes are
trial dispatch, scoped combat modifiers, self-cost presentation/cues and the new
entry/overlay adapter. `src/boggle_content.lua`, `src/boggle.lua` and
`src/boggle_ui.lua` contain the new mode. Documentation stays here.

All GUI/save checks used disposable `XDG_DATA_HOME` roots, seeded with ordinary
active/profile/presentation files. Their inventory/hashes were unchanged after
playing, replaying, exiting and Classic Continue. The existing save/replace/
terminal/restore guards reject trial state; returning to title drops it. Current
real player storage was independently inventoried before/after: **35 files, all
unchanged**, rather than assuming a historical count. Existing terminal outbox,
corruption recovery and profile tests passed among the regression modules.

**README.md was never modified, even temporarily.** Initial and final SHA-256:

```text
Initial: 3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
Final:   3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
```

The known dice-art assertion remains. Native verification used X11 on Xvfb, not a
physical monitor or human tester. Other platforms and a physical controller were
not exercised; audio routing was tested, not subjectively listened to. No human
enjoyment or balance claim follows from scripted victories. The nearer guard is
the only fixture placement change; no other gameplay-scope deviation is required.

For a short human test, launch **seed 202**, inspect two different starts, trace
and backtrack a four-tile route, then commit. Compare the two opening alternatives
above only after the player has tried their own plan. Acquire a boon, spend a
finite power, try Shuffle, and make one intermission edit before fight two.
Ask without prescribing positive answers:

1. Can you understand a route's costs and consequences before committing?
2. Does a harmful route ever feel deliberately worthwhile?
3. Does the four-tile rule create satisfying sequences or compulsory filler?
4. Do acquired boons and board edits change the route you want?
5. Does dragging feel immediate, readable and trustworthy?

This completes the bounded experiment. No probabilistic enemy forecasts, actual
dice, planning deadline, new campaign, shop, extra movement system or general tile
rules language is implemented.
