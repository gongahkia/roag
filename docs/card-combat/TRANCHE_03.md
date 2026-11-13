# Tranche 03: poker stamina and a fixed-budget comparison

The existing **Card Trial** now uses one poker hand to fund several ordinary
combat actions. This supersedes Tranche 02's technique-card economy; the older
documents describe historical experiments. There is still one trial entry, with
a labelled **Fixed Stamina** comparison using the same combat implementation.

Inspected baseline: `main`, `0c8d0a6c`, initially clean. The active game remains
Lua/LÖVE 11.5. Existing targeting, Knight Hop, combat presentation, full-resolution
death checks, storage guards and classic terminal recovery were retained. No
dependencies, sprites, boon definitions, enemy statistics or arena fixtures changed.

## Launch and controls

Use the repository's existing LÖVE/LuaJIT installation:

```sh
make card-trial
./roag.sh --card-trial --trial-seed=202

make card-fixed
./roag.sh --fixed-stamina --trial-seed=202

./roag.sh                           # normal title / dungeon
```

`love .` accepts the same flags. The normal title retains **CARD TRIAL** (`T`).
Setup has Bare, Movement and Burning presets (`1`–`3`/arrows/click), a seed carousel
(`101`, `202`, `303`; left/right), and **Tab** to switch Poker/Fixed Stamina.
Enter/Begin starts. Custom command-line seeds remain supported. Trial progress
is explicitly nonpersistent; no trial Save/Continue is exposed.

The opening explanation is: **Cash a hand for stamina. Spend stamina on actions.
Recover to draw again; enemies advance.** Poker opens with eight cards and zero
stamina. The highlighted combination and Cash button are usable immediately.
Fixed Stamina opens with four stamina, so direct combat works immediately.

| Control | Result |
| --- | --- |
| Enter / Cash button, while preparing | Cash the displayed gold-outlined contributors |
| Card face / `1`–`8` | Toggle a candidate pool; the evaluator highlights its actual contributors |
| Backspace / Auto | Clear the candidate pool and recommend from all unused cards |
| `Y` / Exchange | Exchange 1–3 selected instances, once before cashing |
| Keep beneath a card / `T` | Toggle retention on the focused unused instance, maximum two |
| Tab | Focus the next unused card without changing authoritative state |
| WASD / arrows | One funded cardinal Step; bumping an enemy costs a Strike |
| Adjacent board click | One Step onto an empty cardinal tile, or Strike an adjacent enemy |
| `F` / Strike | Arm an aimed basic attack |
| `X` / Dash | Arm Dash, or Knight Hop after Knight Boots |
| `R` / Sweep | Arm the existing Adventurer special sweep |
| `H` / Mend | Self-targeted healing, committed directly if legal |
| `G` / Recover | Discard leftover stamina, keep marked cards, resolve one real Wait |
| Hover action, then board | Inspect possibilities and exact result without committing |
| Target click | Commit the armed action once; no further confirmation |
| Directions, then Enter | Keyboard aim/commit; `Q/E/Z/C` aim diagonally where supported |
| Arrows while Knight Hop is armed | Cycle legal L-shaped landings; Enter jumps |
| Escape / right click | Cancel aiming/candidate selection; otherwise open trial menu |
| `I` / `V`, F1 | Boon/stat inspection; phase-aware controls help |

An armed action owns direction inputs; cancel it to resume direct Steps. No input
programs multiple actions, auto-pathfinds, or charges internal movement steps.
Selection, inspection, cancellation and invalid inputs are free. Playback blocks
repeat keys, clicks, controller callbacks and the classic `B` developer loadout.
Mouse Keep, card-face, Exchange, Cash and physical-action hit regions are disjoint.

Rewards use `1`–`3`/arrows/click, followed by Enter/Claim. Escape can close/reopen
the boon view without rerolling it. Deck-edit Escape leaves the mandatory choice
open; Skip is an explicit choice. Menu/results: `R` replays the same mode, preset
and seed, `P` returns to setup, `T` returns to title. Enter resumes a menu or
replays a terminal result. Replay restores the original 52 cards and two charges.

Controller callbacks still route through the same authority: D-pad Step/aim,
X Strike, Y Sweep, left-stick press Dash, right-stick press Mend, Back Recover,
A Cash/confirm, B cancel, Start menu, shoulders focus. The full hand-building
workflow is designed for mouse/keyboard; physical controller usability was not
tested and controller-only exchange/retention is not supplied in this tranche.

## Resource cycle and exact poker rules

Each physical instance has a stable numeric ID separate from its rank/suit face.
The initial standard deck has 52 instances; four suits, A through K, no jokers.
The independent `card-trial:poker/v3` stream shuffles/deals eight at time zero.

| Combination | Contributing cards | Stamina |
| --- | ---: | ---: |
| Single / high card | 1 | 3 |
| Pair | 2 | 4 |
| Two pair | 4 | 5 |
| Three of a kind | 3 | 6 |
| Straight | 5 | 7 |
| Flush | 5 | 8 |
| Full house | 5 | 9 |
| Four of a kind | 4 | 10 |
| Straight flush, including royal | 5 | 11 |

The evaluator exhaustively checks contributing subsets of the available eight.
Highest payout wins. Equivalent hands use lexicographically ascending physical
IDs, independently of hand order or rank magnitude. A Single is therefore one
card for three stamina; a higher printed rank has no larger payout. Straights
need five distinct consecutive ranks, with Ace low in A–5 or high in 10–A; no
wraparound. Two pair/full house need distinct rank groups. A flush needs five
distinct physical instances of the same suit, even if some faces are duplicated.
Five matching ranks can provide a four-card subset; there is no five-of-a-kind.

With no selected pool, Cash recommends the strongest available combination.
Selecting cards overrides the pool: select a pair or single to cash that weaker
combination deliberately. A larger pool still highlights exactly which instances
contribute. **Gold outline + CASH** means the card will disappear; a selection
asterisk alone does not. No irrelevant kicker is consumed. Cash moves only those
contributors to discard, grants the displayed stamina, costs no world time and
cannot succeed again this cycle. Unused cards stay visible. Physical actions
never draw, discard or refill cards.

Exchange is optional and free, but allowed exactly once per dealt hand, only
before cashing, for 1–3 distinct owned instances. Unselected instances retain
their positions. Exchanged IDs are quarantined until all replacements are drawn,
so even a necessary reshuffle cannot return them in that operation. They join
discard afterward and may return in a later reshuffle. Invalid requests change
nothing, including the exchange allowance and RNG.

Keep marks apply only to unused cards, at most two. They can be changed before
or after cashing. If a marked instance contributes to Cash or is exchanged, its
mark clears; the preparation footer explicitly explains that Cash clears Keep
on contributors. Marking a card does not exclude it from the displayed Cash
recommendation. This avoids a hidden exception to the contributing-card preview.

Recover is legal before cashing, with spare stamina, or at zero. It discards all
remaining stamina and all unmarked unused cards, keeps marked IDs, and performs
one existing Wait, including Wait boons and normal scheduler work. Only after the
full resolution, if the player is alive and enemies remain, does it refill to
eight and reset exchange/Keep marks. Retained cards are ordinary cards in the new
hand. Death or fight completion during Recover never opens another active hand.
The UI previews lost stamina, number retained and enemies/effects advancing.

Authoritative resource phases are `prepare`, `spend`, `recovering`, `ended`;
encounter phases are combat, boon reward, deck reward, death and victory. Closing
panels, selection changes and repeated callbacks cannot reset these gates.
Conservation accounts for every ID across hand, draw and discard, including ended
encounters and the modified deck. Held cards never enter a discard reshuffle.

## Combat costs, effects and previews

| Action | Stamina | Existing base world time at speed 100 |
| --- | ---: | ---: |
| Step | 1 | 100 |
| Strike, including enemy bump | 2 | 100 |
| Dash / Knight Hop | 2 | 130 |
| Sweep | 3 | 150 |
| Mend | 2 + one healing charge | 100 |
| Recover | 0; loses leftover stamina | 100 |

Normal speed scaling and scheduler ordering remain in force. Every accepted
physical command advances world time, even when one Cash funds several commands.
The player does not act an entire budget before enemies respond. Dash pays once
despite multiple `TileEntered` events; boons, secondary packets, forced movement
and knockback never incur another stamina charge. Bumping an enemy is classified
as Strike before budget validation. Raw classic movement, attack, ability,
utility, Wait and old trial technique intents cannot bypass the trial wrapper.

Step, Sword Strike (base 4 before defence), Dash (normally two straight tiles,
with existing collision/terrain rules) and Sweeping Slash (base 3, existing arc)
reuse `Game.submit`. Hit/kill attribution, basic/special identity, path movement,
terrain, windups, impact/recovery and boon ordering are preserved. Recover emits
one real `Waited`. Mend restores up to four HP with the established healing source
and `Healed` event; it never emits Wait or pickup events. Its two charges span both
fights, do not regenerate, and remain independent of boon healing. Full health
and zero charges reject without cost. There is no Mend card or retirement pile.

The retained previews distinguish legal targets (hollow circles), exact footprint
(inset outlines), actual Dash path (small squares), and landing (ghost player).
Knight Hop has eight potential L-shaped offsets, ignores intervening cells and
requires a free walkable landing. It retains its distinct jump identity, origin/
landing reactions, no path, no intermediate pickups/hazards and no immunity.
Rhythm, Ring escape recognition and Pulse Step can apply; effects requiring
`actual_dash`, including Phantom Lunge/Froststep Catalyst, do not gain a fake path.

Attack labels show primary HP loss obtained from the real resolver on a private
copy. The curated presets have no random crit/dice build; conditional boon chains
and later status ticks are explicitly excluded from the displayed number. Enemy
telegraphs remain visible during preview. Mend shows its recoverable HP segment,
resulting HP and charge use. Costs show stamina remaining and **Time … (enemies
advance)**, rather than an unexplained `+100`. Invalid/underfunded actions explain
why; target geometry can still be inspected without funds. Legal never means safe.
No preview promises a complete future enemy/cascade or multi-action sequence.

## Same trial, one deck edit and Fixed Stamina

The 9×9 playable interior and two-cell pillar are unchanged. Each fight retains
two `core:enemy/mossbound_guard` and one `core:enemy/thornspitter`. Fight one starts
at player (6,8), guards (4,5)/(5,5), ranged enemy (8,3); fight two's preferred
spawns are (3,8)/(9,4)/(3,2). Coordinates include the outer wall. Reinforcements
use the existing nearest-free fallback if occupied. Trial XP/loot suppression
still preserves genuine kill events. No broad pressure or damage rebalance.

Presets retain Bare, uncommon Rhythm of Steel + Ring of Reversal, and uncommon
Emberbrand + Shared Agony. Fight one's fixed reward remains **Knight Boots**, rare
**Pulse Step**, or rare **Detonation Bloom**, using the existing acquisition path
for boons. Knight Boots now changes the funded Dash action into Knight Hop; it
does not modify poker faces or grant cards. Its reward still displays both shapes.
The full ordinary boon catalogue is unchanged.

Poker then offers two distinct faces from the separate
`card-trial:deck-reward/v3` stream, fixed at trial creation independently of
performance: **add three copies** of either face, or **Skip**. Preview shows deck
size, identical-face count, rank count and suit count. Seed **202** offers **9
Hearts** and **7 Hearts**. Either changes 52→55 cards, that face 1→4 copies, its
rank 4→7, Hearts 13→16. New IDs are 53–55, real run-scoped instances.

Confirming the deck choice applies it once and consolidates/shuffles the current
deck into a fresh eight-card opening for fight two. This explicit free opening
reset clears old stamina, selection, Keep marks and restores one exchange. HP,
charges, boons, position, world clock and ongoing schedules remain in the same
simulation. Skip still gets this opening reset, with 52 cards. Duplicate callbacks
cannot reapply rewards or redeal. Intermission/resolved fights prohibit combat
and Recover farming. Full committed resolution precedes rewards; death takes
precedence over victory, including simultaneous lethal outcomes.

**Fixed Stamina** differs only in resource policy: four to start each fight;
Recover loses leftovers, Waits normally, then restores four if combat is active.
No card, exchange, retention or deck-reward steps are displayed. The same action
resolver, costs, targets, enemies, presets and ordinary boon reward remain. Fight
one is the primary comparison, before deck editing. Different budgets make win
rates alone unsuitable evidence of better interaction.

## Implementation and isolation

| File | Responsibility |
| --- | --- |
| `src/card_trial_content.lua` | Small tunable payout, hand, resource and action constants; unchanged combat fixtures |
| `src/cards.lua`, `src/poker.lua` | Physical instances/piles/independent RNG; pure best-subset evaluation |
| `src/card_trial.lua` | Authoritative phases, budgets, exchange/retention, validation and one-time rewards/transitions |
| `src/game.lua` | Accept through trial validation; pay once, use existing combat resolver/scheduler; finish Recover after resolution |
| `src/card_trial_ui.lua` | Disposable candidate selection/aiming, eight-card rendering, previews and comparison UI |
| `main.lua`, `Makefile` | Existing entry/input/presentation adapter; free bookkeeping avoids combat playback; comparison launch flag/target |
| `tests/card_trial_*` | Model, callback, controlled-preview and disposable-storage verification |

All card bookkeeping is event-free: no pickup imitation or fabricated gameplay
actions. Failed actions leave cards, stamina, charges, HP, boons, schedules, clock
and all RNG unchanged. Renderer/hover/evaluation use no live random draws.

The existing nonpersistent host adapter and `src/persistence.lua` guards remain.
Trial sessions cannot save, replace, checkpoint or write terminal records, and
trial snapshots cannot load as classic runs. Normal menus/settings remain outside
this policy. Tests seed disposable normal active/profile/settings records, then
hash them across launching, fighting, replaying, returning and classic Continue.
Native launches used private `XDG_DATA_HOME` roots. All **35 files** in the real
player storage root were separately fingerprinted before/after and are unchanged.
Classic save/load, terminal handling and ordinary controls remain available.

`README.md` was **never edited**, including temporarily. Initial and final SHA-256:

```text
Initial: 3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
Final:   3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
```

## Verification evidence

- Baseline `make card-test`: 31 passed. Current: **36 passed**, covering all poker
  categories/Ace boundaries/duplicates, exact subset and conservation across 80
  cycles, exchange quarantine, retention, pure previews/RNG independence, gates,
  input hit regions, invalid/no-cost paths, one-time stamina/timing, healing,
  Recover death/completion, rewards/deck additions, transition and isolation.
- Seven existing relevant modules (boon expansion, Vow, dice, cadence boons,
  motion, classic catalogue visuals, pixel UI): **109 passed, one failed both
  before edits and after**. The unchanged failure is `dice declares six numbered
  faces, skull and miss with exact approved native bindings`:
  `src/sprite_audit.lua:19` references absent
  `assets/kenney/1-bit-pack/symbols.png`. Current restored artwork was not changed
  to satisfy that obsolete assertion.
- Existing classic GUI regression: save, load, preview launch, keys and save
  isolation passed. Asset check: **599 bindings / 67 unchanged source PNGs**.
  Lua syntax and `git diff --check` passed.
- Existing in-memory persistence, meta/profile and title tests: **42 passed**,
  including corrupt-slot recovery, pending terminal outbox restart/idempotence,
  failed profile commit, tombstone retry and ordinary Continue selection.
- Full LÖVE renderer/public-callback journeys passed for all six seed-202
  mode/preset combinations, through boon reward, optional deck stage, second fight
  and victory. A separate technical journey exercised exchange, retention, weaker
  Single override, zero/early Recover, both healing charges, death and replay.
  During playback, extra keyboard, mouse, repeat and controller callbacks were
  verified inert. All accepted callbacks were compared with private real-logic
  resolution snapshots; no live actor/deck edits supplied those victories.
- A separately labelled controlled renderer fixture arranged enemies/terrain to
  verify Sweep's primary-hit labels and a pillar-crossing Knight Hop: invalid
  occupied landing rejected; bramble/coin/Pulse/Rhythm landing effects applied;
  intermediate pickup/path boon did not. These setups verify geometry/effects,
  not ordinary encounter pressure or human understandability.

| Callback journey, seed 202 | Commands* | Final HP | World time |
| --- | ---: | ---: | ---: |
| Poker / Movement | 42 | 9 | 3260 |
| Poker / Burning | 34 | 10 | 2660 |
| Poker / Bare | 66 | 3 | 5290 |
| Fixed / Movement | 30 | 6 | 3030 |
| Fixed / Burning | 29 | 4 | 2990 |
| Fixed / Bare | 50 | 2 | 5110 |

*Commands include free Cash in poker; these are reachability checks using a
test-only single-action policy, not comparative human performance or proof of fun.*

Reproduce the focused/rendered checks with:

```sh
make card-test
make assets-check
xvfb-run -a -s '-screen 0 1024x768x24' sh tests/card_trial_gui.sh /tmp/roag-tranche03-gui
xvfb-run -a -s '-screen 0 1024x768x24' sh tests/classic_restore_gui.sh /tmp/roag-classic-gui
```

Native X11 input was also exercised with `xdotool`, distinct from calling LÖVE
callbacks. In Movement/202, Cash Two Pair→5 at time zero funded Step west, Step
north, a mouse Strike north, then Step south: stamina zero at time 400, unchanged
unused cards. Keep buttons marked J Diamonds and 7 Diamonds. Recover reached time
500 and retained them; exchanging 6 Hearts, A Clubs and 5 Hearts produced five
Diamonds. Native Enter cashed that Flush→8 at the same world time, consuming
exactly its five contributors. This was an agent-directed attempt to complete a
suit, not a human observation of voluntary retention/exchange. A separate native
Fixed launch performed Step (4→3 stamina) and early Recover (time 200, back to4).
Its final card-free sidebar/help were visually inspected after the final UI edit.

Eight cards, rank/suit shapes and ASCII suit labels, all controls, HP preview,
primary-hit labels, reward/deck details and enemy intentions were inspected at
**1024×640**. `conf.lua` uses that fixed, nonresizable size; it is both the normal
and smallest supported size. There are no new font glyph dependencies. Existing
sprites, card frames, integer rendering and combat playback remain in use.

Local captures/receipts are ignored verification artifacts, not game assets:

- [Eight-card opening](../../artifacts/card-trial/tranche-03/verified-gui/eight-card-opening.png)
- [Zero-stamina Recover preview](../../artifacts/card-trial/tranche-03/verified-gui/zero-recover-preview.png)
- [Sweep footprint/primary loss](../../artifacts/card-trial/tranche-03/verified-gui/sweep-primary-preview.png)
- [Deck edit](../../artifacts/card-trial/tranche-03/verified-gui/deck-edit-details.png)
- [Fight two](../../artifacts/card-trial/tranche-03/verified-gui/fight-two-fresh-hand.png)
- [Native retained/exchanged Flush](../../artifacts/card-trial/tranche-03/native-after-exchange.png)
- [Native Fixed Stamina](../../artifacts/card-trial/tranche-03/native-fixed-final.png)

## Human comparison still needed

No human playtester was available in this implementation session. Native input
proves the controls execute; scripted journeys prove reachability. Neither proves
that the hand work earns its interruption cost. Audio output and a physical
controller were not subjectively evaluated; other platforms were not exercised.
The known dice-art assertion remains. No gameplay-scope deviations were required.

Play **Movement, seed 202, fight one** in Poker, then the same fixture in Fixed
Stamina (optionally reverse the order for another tester). Give only the short
opening explanation. Let the player choose freely; do not require an exchange,
retention or weaker combination. Observe one full budget and at least one Recover
in each mode, then continue to the boon/deck reward if useful.

Ask the player to explain:

1. How do you obtain stamina?
2. What does Recover cost, including enemy time and leftover stamina?
3. Why did you exchange or retain those particular cards, or choose not to?
4. How did you spend one budget across several actions?
5. Did a boon or deck edit change your choices?

Record whether exchanges were voluntary, retention had an articulated reason,
the player overrode the recommendation, or hand building changed their intended
action sequence. Also note confusion between selected-pool, CASH and KEEP marks,
and whether primary-hit labels were mistaken for complete cascade predictions.
These are observations, not required positive outcomes.

**Did building the hand create an interesting plan, or mostly interrupt actions
you already wanted to take?**

Stop this tranche here. Sequence previews, dice pools, obstacle clearing, new
movement forms, general rule editing and broader campaign/balance work remain
outside the implemented experiment.
