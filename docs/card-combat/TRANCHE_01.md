# Card combat: Tranche 01

This is a playable, nonpersistent experiment in the active Lua/LÖVE game. It uses
the Adventurer, the existing combat resolver and variable-cost scheduler, the
current sprite catalogue, and existing playback, telegraphs and boon reactions.
The ordinary dungeon remains available through New Run and Continue. The full
boon catalogue is unchanged.

## Launch and controls

With LÖVE 11.5 and the repository's existing assets available:

```sh
make card-trial
# Reproducible custom seed:
./roag.sh --card-trial --trial-seed=202
# Ordinary title screen/dungeon:
./roag.sh
```

On the ordinary title screen choose **CARD TRIAL**, or press `T`. Choose Bare,
Movement or Burning with `1`–`3`, up/down, or a click. Left/right changes the seed
among **101, 202, 303**. Enter begins. A valid custom seed supplied on the command
line is retained until changed; the supported integer range is 1–2147483646.

| Input | Trial action |
| --- | --- |
| `1`–`5`, or click a card | Toggle that physical card's selection |
| `M` (`X` alias), or Move button | Choose Step or Dash according to the recipe |
| `F` (`R` alias), or Attack button | Choose Sword Strike or Sweeping Slash |
| `H`, or Heal button | Preview healing and charge cost |
| Space / `.`, or Wait & Redraw | Choose a paid exchange of selected cards |
| WASD / arrows | Aim; movement is cardinal |
| `Q/E/Z/C` | Aim attacks diagonally |
| Click a board tile | Aim toward it; existing action geometry determines reach |
| Enter, or Confirm | Commit the previewed action |
| Escape | Cancel aiming without spending cards; otherwise open trial menu |
| Backspace | Clear selection and aiming |
| `I` / `V` | Existing boon/stat inspection |
| F1 | Trial control help |

Cancellation keeps selected cards available. Selection and aiming never advance
world time. Input is locked during combat playback. Classic movement, bump
attacks, utility bindings and the developer `B` loadout cannot bypass cards.

At the reward, `1`–`3`/arrows/click chooses, Enter claims. Escape closes the view;
Enter or **REOPEN REWARD** reopens the same offers. Result/menu controls are `R`
to replay the complete trial with the same preset and seed, `P` to choose another
preset, and `T` to return to title. Enter resumes a paused trial or replays a result.
There is no trial Save/Continue.

Controller callbacks support shoulders to focus a card, A to select/confirm,
Y Move, X Attack, right-stick click Heal, left-stick click Redraw, D-pad aim,
B cancel, Back inspect and Start menu. Physical controller hardware was not tested.

## Implemented rules

The deck contains 52 distinct physical cards, four suits and A–K, with a five-card
opening hand. A **Single** is exactly one card; a **Pair** is exactly two equal
ranks. Suits and rank magnitude add no other effects. Invalid selections never
automatically become a smaller recipe. Any one card works for basic movement or
attack when its ordinary target is legal.

| Persistent action item | Single | Pair | Base action time at speed 100 |
| --- | --- | --- | --- |
| Boots | One cardinal tile | Existing two-tile Adventurer Dash | 100 / 130 |
| Adventurer sword | Sword Strike, base damage 4 | Three-cell Sweeping Slash, base damage 3 | 100 / 150 |
| Healing flask | Restore 4 HP | Restore 8 HP | 100 / 100 |

These are the existing pre-modifier weapon values. Targeting, collision, defence,
special/basic identity, primary-hit ordering and boon geometry still use the
ordinary resolver. Dash previews show its actual path and landing, can stop at
obstacles under existing rules, and grant no new invulnerability. Time scales
through `Game.effective_cost`; enemy windups, impacts, recovery and status effects
continue in scheduler order. There is no additional Wait after a technique.

Successful techniques discard exactly their selected cards and refill those
slots. All unplayed cards retain their identities and slots. When drawing needs
more cards, only the discard pile is shuffled into a new draw pile; retained
cards never enter it. The current action's discarded cards can participate when
that reshuffle is necessary. Hand, draw and discard always conserve all 52 cards.
The deck has its own derived RNG stream, independent of combat/world randomness.

There are **two healing charges for both fights together**. Either healing recipe
uses one charge and caps at maximum HP (24 initially). Full-health healing and
healing with no charges are invalid. Healing emits a genuine `Healed` event with
source `trial:equipment/healing_flask`, not Wait/pickup events. Boon healing does
not replenish charges. Complete replay resets charges.

Wait & Redraw accepts any one to five selected cards, retains all others, and
resolves one actual Wait at its ordinary scaled cost, including Wait-specific
boon triggers. Empty selection does nothing. Preview, inspection, cancellation
and rejection leave cards, RNG, HP, charges, time and boon state unchanged.

`Game.submit` validates the card recipe, ownership, action, target and healing
resources before payment. Card payments produce a separate
`CardTechniqueCommitted` event; refills do not impersonate combat or pickups.

## Fixtures and progression

`src/card_trial_content.lua` is the small tuning layer for seeds, recipes, hand
size, equipment, charges, presets, arena and encounters.

| Preset | Existing starting boons |
| --- | --- |
| Bare | None |
| Movement | Uncommon `rhythm_of_steel` + uncommon `ring_of_reversal` |
| Burning | Uncommon `emberbrand` + uncommon `shared_agony` |

All IDs above have the `core:boon/` prefix. These rarities make secondary damage
visible against the fixtures' flat defence without changing the boons or enemies.

The Overgrown Ruins arena has a 9×9 playable interior, enclosed walls and a
two-tile central pillar with east/west routes. Coordinates below include the
outer wall, using one-based tiles. The Adventurer starts at (6,8).

| Fight | Mossbound Guards | Thornspitter |
| --- | --- | --- |
| The split approach | (4,5), (5,5) | (8,3) |
| Crossing fire | (3,8), (9,4) | (3,2) |

These use unchanged `core:enemy/mossbound_guard` (10 HP, defence 1, damage 3,
speed 70) and `core:enemy/thornspitter` (6 HP, defence 0, damage 2, speed 90).
The latter supplies the existing locked thorn-lane telegraph. Spawn placement
uses the nearest free arena tile if the player or another blocker occupies an
authored position, with row/column tie-breaking.

After the first complete committed combat resolution, choose one **rare** boon:
`pulse_step`, `detonation_bloom` or `grave_dividend`. The existing free-chest UI
and `Rewards.claim` acquisition path apply it exactly once. Offers never reroll.
Real deaths and kill credit remain intact; trial kill rewards suppress unrelated
XP growth, gold and supply drops.

Fight two is a reinforcement wave in the same chamber and clock. This deliberate
fixture choice preserves HP, position, owned boons, hand/piles/card RNG, charges,
timed player effects and pending scheduled work without rebasing or dropping
effects. No transition healing occurs. Death takes precedence over rewards and
victory after the full action resolves, including simultaneous lethal outcomes.
Resolved fights block movement, healing and redraw farming.

## Isolation and preservation

Trial state lives only in memory. It never loads the active dungeon run or
allocates a permanent run identity. Checkpointing skips trials; persistence
save/replace/terminal-outbox calls reject trial state before writing, and restore
rejects trial snapshots. Presentation preferences may be read. Entering, replaying
and leaving do not write normal saves, profiles or preferences.

A narrow shared flow change defers pending terminal-award recovery at title
startup to the existing explicit `R` recovery command. Merely visiting the title
to enter a trial must not commit normal profile awards or tombstone a normal run.
Ordinary New Run still waits for that recovery. Normal save schemas are unchanged.

The renderer still supplies the board, camera, current Adventurer/enemy sprites,
intent cells, movement/attack playback, particles, hit feedback and damage numbers.
The trial sidebar adds cards and preview controls; boon feedback explains the
latest two activations. Suit shapes are drawn geometrically with ASCII S/H/C/D
labels; ranks use verified supported glyphs. No existing artwork was replaced.
The application remains fixed at 1024×640: its normal and smallest supported
window sizes are the same. The arena and hand fit side by side.

Main implementation boundaries:

- `src/cards.lua`: deck identities, selections, deterministic shuffle and conservation.
- `src/card_trial.lua`: trial construction, authoritative preview, encounter/result rules.
- `src/game.lua`: card payment and healing integrated into existing action resolution.
- `src/card_trial_ui.lua`, `main.lua`, `src/pixel_ui.lua`: menu, input, hand and previews.
- `src/rewards.lua`, `src/persistence.lua`: narrow trial reward/storage guards.

There are no additional poker recipes, suit powers, equipment replacement,
refunding boons, shops, other-class conversions or persistent trial saves.

## Verification and observed play

Run the focused tests and actual GUI harness with:

```sh
make card-test
xvfb-run -a -s '-screen 0 1024x768x24' make card-gui
# Existing classic UI/save/Continue regression:
xvfb-run -a -s '-screen 0 1024x768x24' make gui
make assets-check
```

- **19 focused tests passed**: physical-card conservation across exhaustion,
  retention, RNG independence, invalid/no-op boundaries, exact action timing,
  paid Wait triggers, healing source, Movement/Burning interactions, reward
  carryover including scheduled effects, lethal resolution, replay, storage
  rejection and ordinary actions.
- Relevant existing suites (`boon_expansion`, `idea_vow`, `idea_dice`,
  `idea_cadence`, `motion`, `classic_catalog_visuals`, `pixel_ui`): **109 passed,
  one pre-existing failure**, reproduced before and after the implementation.
  The dice native-art assertion calls `src.sprite_audit` expecting removed
  `assets/kenney/1-bit-pack/symbols.png`. Restored artwork was left intact.
  The full broad suite was not run.
- Asset verification passed: **599 bindings, 67 unchanged source PNGs**. Lua
  compile and whitespace checks passed.
- Actual LÖVE GUI harness passed all three presets at seed **202** using
  production keyboard/mouse callbacks and rendered playback. It exercised both
  recipes for movement/attack/healing, retention, active-enemy redraw, input
  locks, Ring priming/spending, Shared Agony, fixed reward close/reopen, both
  fights, natural death, replay and classic controls. Controller callback checks
  exercised bypass protection; this is not a hardware usability assessment.
- Scripted completions: Movement 27 actions, 24 HP, one charge left; Burning
  23 actions, 21 HP, one charge left; Bare 48 actions, 24 HP, two charges left.
  These are legal one-action-lookahead policy playthroughs, not human balance
  scores. A separate stationary redraw/heal exercise exhausted both charges
  and died naturally.
- Manual `xdotool` inputs exercised Movement selection, single movement and an
  aimed attack. Screenshots were visually inspected at 1024×640. Existing
  classic GUI checks passed Save/Load/Continue, preview and ordinary controls.
- Isolated normal active-run/profile/preferences fixtures were byte-for-byte
  unchanged after trial play/replay/exit. A separate custom-seed **777** CLI
  smoke test also left a pending terminal outbox untouched. All **35 actual
  user storage files** matched their before-investigation hashes.

Local receipts and screenshots are under the ignored
`artifacts/card-trial/tranche-01/verified-gui/` directory; they are generated test
output, not required runtime assets. Useful images:
[dash preview](../../artifacts/card-trial/tranche-01/verified-gui/move_pair-preview.png),
[Burning interaction](../../artifacts/card-trial/tranche-01/verified-gui/burning-synergy.png),
[reward](../../artifacts/card-trial/tranche-01/verified-gui/movement-reward.png),
[second fight](../../artifacts/card-trial/tranche-01/verified-gui/movement-fight-2.png).

Observed: singles remained useful for exact repositioning, triggering Rhythm,
and direct hits. At seed 202, the first westward single step produces a pair in
slots 1–2; spending another slot retains it. Pairs then offer competing Dash,
Sweep and stronger healing uses. The scripted Bare policy could kite to victory
without healing. Burning's linked damage was clearest after deliberately sweeping
two adjacent guards; isolated single-target hits can kill before a link matters.
The screenshots kept intentions visible beside the hand and displayed the
activation reason. None of these checks establishes fun or pair dominance.

Human follow-up checklist: compare all three presets on seed 202; retain the
opening earned pair across several singles; try each pair use; redraw under a
telegraph; spend both charges; choose each reward; finish/die/replay and return
to Continue. For a reproducible Movement interaction, step west with slot 1,
north with slot 3, redraw slot 4, step south with slot 5, then attack northwest
with slot 3, waiting for playback between commits. For Burning, step west with
slot 1, north with slot 3, then sweep northwest with slots 1–2.

Physical controller use, Windows/macOS, subjective audio quality and sustained
human balance/playability evaluation remain unverified. The tests do not claim
the trial is fun.

Questions for the next mechanics discussion:

1. Does automatic refill make holding pairs too easy compared with spending singles?
2. Are Dash, Sweep and eight-HP healing sufficiently competing uses for a pair?
3. Does this open arena create the intended positional pressure, given the observed kiting?
