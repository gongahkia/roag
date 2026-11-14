# Tranche 01: custom-dice combat trial

A nonpersistent Adventurer combat experiment: see three results, choose one,
act immediately, resolve ordinary combat time, and replace all three results.
There is no resource preparation, held die, free reroll, Recover or healing action.
This is a functional prototype; automated victories do not establish fun.

## Launch and controls

Requires the project's LuaJIT / LÖVE 11.5 runtime:

```sh
./roag.sh --dice-trial --trial-seed=202
make dice-trial
make dice-test
```

The launch flag opens the encounter directly, with the starting build and one
complete roll already available. Existing title/dungeon, `--card-trial`,
`--fixed-stamina`, `--boggle-trial` and `--spectator` paths remain.

| Input | Behaviour |
| --- | --- |
| Arrows / WASD | Immediately use rolled Footwork; an enemy bump uses rolled Weapon |
| 1 / 2 / 3 | Select Footwork / Weapon / Tactics |
| F | Select rolled Weapon |
| Direction after selection | Immediately commit that action in that direction |
| Q / E / Z / C | Diagonal attack directions; movement remains cardinal |
| Action-panel click | Select result; Guard executes directly |
| Arena target click | Commit selected result; neutral enemy click uses Weapon, neutral adjacent empty cell uses Footwork |
| Pointer hover / result focus | Preview one action or its legal geometry |
| I / F1 | Inspect all six indexed faces, exact base costs, action rules and starting boons |
| Escape / right click | Cancel targeting, otherwise open trial menu |
| Menu R / T / Enter | Replay same seed / title / resume |
| Terminal R or Enter / T | Replay / title |

After fight one, choose a die with 1/2/3, a physical index with left/right,
and a replacement with up/down. All three choices also have clickable rows.
The replacement rows show the resulting counts out of six before confirmation.
Enter confirms one different compatible replacement; K skips. Escape cancels
the candidate. An identical replacement is disabled and rejected by the model.
Fight two opens immediately after confirmation or Skip, with one fresh roll.

## Physical faces and combat rules

Each initial entry below occurs at two physical indices, so each type initially
has probability 2/6. The three dice sample independently and uniformly over six
indices with the existing rejection-sampled RNG. Duplicate consecutive outcomes
remain valid.

| Die | Indices | Face | Base world time | Behaviour |
| --- | --- | --- | --- | --- |
| Footwork | 1–2 | Step | 100 | Ordinary cardinal adjacent movement |
| Footwork | 3–4 | Guarded Step | 100 | Same movement, then refresh trial Guard |
| Footwork | 5–6 | Quick Step | 70 | Same movement at this command's reduced cost |
| Weapon | 1–2 | Strike | 100 | Ordinary adjacent attack, base damage 4 |
| Weapon | 3–4 | Sweep | 150 | Existing Adventurer three-cell arc, base damage 3 |
| Weapon | 5–6 | Pierce | 100 | Two-cell ray, base Strike damage 4 per target |
| Tactics | 1–2 | Guard | 100 | Refresh trial Guard |
| Tactics | 3–4 | Dash | 130 | Existing cardinal two-tile Dash, stopping at blockers |
| Tactics | 5–6 | Heavy Strike | 150 | Adjacent Strike with +2 primary damage before crit/defence |

Costs use `Game.effective_cost(base, hero.speed + boon speed bonus)` once.
Quick Step never changes hero speed, shared movement costs or the next action.
The reused chamber has plain walkable terrain, so no terrain surcharge applies.

Movement uses real Moved/TileEntered hooks. Attacks use the existing primary
attack batch, critical-hit processing, flat defence, damage, hit and credited
kill hooks. Empty aimed attacks are legal misses which pay time. Dash requires
positive displacement; partial travel is legal. Blocked Footwork is invalid.
Pierce does not move the hero, passes through creatures, stops before blocking
terrain, and checks both orthogonal edges of each diagonal step, matching the
shared projectile corner convention. Its two cells resolve as one attack batch.
Sweep's geometry and base damage come from the existing class/ability resolver.

Trial Guard has capacity 2, refreshes rather than stacks, and expires at
`clock >= applied_time + 200`. Action resolution time counts toward this duration.
Guarded Step applies it after successful displacement and before enemy time.
Ordinary armour and boon wards absorb first, then this separate source. Boggle's
existing nonexpiring Guard policy is unchanged.

## Starting build and encounters

Both existing fights use the original compact chamber, central pillar, two
Mossbound Guards and one Thornspitter. No shared enemy stats, AI or spawn
catalogues were changed. The initial Adventurer has 24 HP, Strike 4 and Sweep 3.

Starting boons are **uncommon Rhythm of Steel** and **rare Detonation Bloom**.
These are the actual boon engine implementations, visible and inspectable at
opening. Rhythm primes on actual movement for 250 world time, then supplies
base 2 secondary physical damage after a primary hit. A Guard's defence 1 leaves
positive damage 1. Bloom supplies base 3 explosion damage in radius 1 on a
credited kill; a nearby Guard takes positive damage 2 after defence. Common
Bloom's base 1 would be absorbed by that defence, which is why rare was chosen.

The unmodified seed-202 fixture demonstrates positive Rhythm damage on action 3
and positive Bloom damage on action 6. The verification policy wins fight one
at time 1180 with 16 HP and both fights at time 2640 with 11 HP, in 25 combat
actions. It replaces **Footwork physical face 1, Step → Quick Step**. Counts become
Step 1/6, Guarded Step 2/6, Quick Step 3/6. Exactly one array entry changes.
Replaying reconstructs all original dice.

Intermission is paused world time, with no chest/boon reward stacked on the face
edit. HP, guard expiry, boon memory, schedule, ongoing effects, combat RNG and
clock carry into fight two. No roll is made on the fight-one finishing action;
one is made after the edit/Skip and spawn. Neither terminal outcome rolls.

## Implementation ownership and isolation

- `main.lua`: existing launch flags, renderer, scheduler playback adapter,
  persistence gate and LÖVE input callbacks; narrowly delegates dice presentation.
- `src/card_trial.lua`: existing arena construction, encounters and second-fight
  continuity; initializes the additional dice policy.
- `src/dice_trial.lua`: face catalogue, seeded physical dice, revisions,
  authoritative validation, timed Guard, edit and lifecycle policy. Dice stream:
  `RNG.derive(run_seed, 'dice-trial:faces/v1')`; combat RNG is separate.
- `src/game.lua`: existing action resolver, scheduler, damage batch and boon event
  pipeline. Accepts a validated dice plan; overrides only its cost, Pierce cells,
  Heavy Strike primary bonus, and separate guard absorption.
- `src/dice_ui.lua`: disposable aiming, result inspection, editor and overlays.
  Previous results remain displayed during playback. Revision changes clear
  stale targeting. Playback and the current callback batch reject action input;
  physical keys require release before reuse, repeats are ignored, and multiclick
  events cannot commit the following result set. No held-pointer polling commits.
- `src/card_trial_ui.lua`: shared host attachment and old-mode isolation.
- `src/rng.lua`, `src/classes.lua`, `src/targeting.lua`, `src/scheduler.lua`,
  `src/boons.lua`, boon content and `src/playback.lua`: reused without modification.

Forecasts execute the same `Game.submit` on a full `Game.snapshot`, including
scheduled impacts, enemy movement, statuses, wards, boon memory and both RNG
streams. Future dice results and dice RNG are removed from the returned forecast.
The UI caches the considered revision/die/direction. Gold geometry means legal,
not safe; red outlines show forecast enemy impacts. Hero landing ghosts, enemy
HP changes, displacement ghosts, resulting HP/Guard and time show consequences.
Inspection and cancelled input do not submit a world action or draw random values.

The trial retains the existing `card_trial` nonpersistence boundary. Save,
replacement, pending-terminal recovery and restore reject trial states. Native
GUI runs use disposable storage with real seeded save/profile/settings records;
all four record hashes are compared before and after. No normal player storage
is used by the GUI harness.

## Verification and reproduction

Automated checks:

```sh
make dice-test
make card-test
make boggle-test
make test
git diff --check
```

`dice_trial_tests.lua` covers opening, deterministic physical-index enumeration,
independent RNG, identical rolls, speed scaling of all nine faces, stale and
legacy command rejection, blocked movement, exact Guard expiry during scheduled
impacts, protection ordering, all attack geometries, critical Heavy Strike,
Dash partial travel, empty misses, repeated preview isolation and execution
parity, actual starting-boon damage, deterministic two-fight victory, one-index
editing, replay defaults, terminal roll counts, rejected persistence writes,
mouse/keyboard routing, cancellation, playback locking, repeats and multiclicks.
Forced results and index injection exist only in tests, never in the playable
roll policy. `dice_trial_policy.lua` considers only current actions and combat
forecasts; it cannot inspect future results.

Native GUI verification uses LÖVE's real renderer and native X11 events from
`xdotool` in an isolated Xvfb display at **1024×640**. The test-only observer loads
the production main and never submits a live command. It publishes suggestions
from copied state, checks native resulting states against the shared resolver,
and writes screenshots outside player storage. This is automated native input,
not a human preference playtest and not callback-only execution.

With LÖVE, Xvfb and xdotool available:

```sh
Xvfb :99 -screen 0 1024x640x24 -nolisten tcp &
DISPLAY=:99 SDL_VIDEODRIVER=x11 tests/dice_trial_gui.sh &
DISPLAY=:99 python3 tests/dice_trial_native.py
```

`ROAG_DICE_GUI_OUTPUT` selects a disposable output directory (default
`/tmp/roag-dice-gui`); `LOVE_BIN` and `XDOTOOL` override runtime executables.
This machine lacked LÖVE/Xvfb/xdotool. Verification extracted Arch packages into
`/tmp/roag-runtime` without system installation and used that directory's binaries
with `LD_LIBRARY_PATH=/tmp/roag-runtime/usr/lib`. A real Wayland window was also
opened and native inspection/movement checked, but the complete audited journey
used the isolated X display to avoid focus interference on the shared desktop.

Evidence: [opening](evidence/opening.png), [inspection](evidence/inspection.png),
[single-action preview](evidence/preview.png), [physical face edit](evidence/intermission.png),
[victory](evidence/victory.png), and [native input transcript](evidence/native-input.log).
The transcript records seed 202, all native commands, face edit, victory and
replay. Headless adapter tests separately exercise callbacks; they are not
counted as native play. The final results and preservation audit follow below.

## Limits and playtest questions

No physical controller usability assessment or human preference playtest was
performed. D-pad and action buttons route through the same adapter, but the
prototype is evaluated here with mouse and keyboard. The catalogue is deliberately
small and the two encounters are unchanged. Other seeds may have different
balance; the reproducibility checks do not claim broad balance coverage.

The policy used two backtracking Quick Steps while waiting for a ranged enemy;
paid movement can fish for another roll. Guard can similarly trade combat time
for new results. This is a balance observation to test with people, without
adding another resource or reroll restriction in this tranche.

- Could I recognise an opportunity and act immediately?
- Did different rolls create useful tactical choices, or mostly prevent actions I wanted?
- Did the face edit and starting boons make the build feel meaningfully different?

## Final results and preservation audit

Baseline checkout: `ba010b91`, initially clean. The working tree contained no
pre-existing user edits. Earlier tranche documents describe superseded systems;
current Lua code and launch paths were used as authority.

| Check | Result |
| --- | --- |
| `make dice-test` | 21 passed, 0 failed |
| `make card-test` | 36 passed, 0 failed (also 36/0 before implementation) |
| `make boggle-test` | 31 passed, 0 failed (also 31/0 before implementation) |
| `make test` | 817 passed, 14 failed |
| Clean-HEAD `make test` in `/tmp/roag-baseline` | Same 817 passed and exact same 14 failures |
| Asset check | 599 bindings, 67 unchanged source PNGs |
| `git diff --check`, LuaJIT compilation of changed Lua files | Passed |
| Native two-fight journey, seed 202 | Victory, 25 combat actions, time 2640, HP 11 |
| Native replay/title | Restored original six-face dice and 24 HP; title returned |
| Native launch smoke | Title, dice, poker, fixed, Boggle, spectator each remained live for 2.5 seconds without Lua error |
| Seeded normal-storage fixture | Four files unchanged after full GUI journey/replay/title |
| Actual player storage | Initial and final fingerprints equal (no files existed) |

The broad suite remains red because 14 old art checks request absent
`assets/kenney/1-bit-pack/symbols.png`. The clean-HEAD comparison was run during
this task, with README excluded from its extraction. The exact failing test
names and messages match; see [baseline failure evidence](evidence/baseline-failures.log).
Those unrelated asset tests were neither changed nor bypassed. No feature
regression was found by the checks above.

README.md was never edited, including temporarily. Initial and final SHA-256:

```text
3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
```

[Preservation evidence](evidence/preservation.json) records hash equality and
before/after seeded player-record fingerprints. Runtime packages, temporary
baselines and GUI output stayed under `/tmp`; no system package installation or
normal save/profile changes were made. The initial shared-desktop native attempt
was not counted as a completed journey; the isolated final journey passed all
observer assertions and produced the retained evidence.
