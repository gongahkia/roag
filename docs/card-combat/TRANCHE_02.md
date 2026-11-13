# Tranche 02: named techniques and Knight Boots

The existing **Card Trial** now uses named techniques. This replaces its poker
interaction; there is one experimental trial entry. Tranche 01's document is a
historical record. No separate 01A document was present in the inspected checkout
(`main`, `20b331fd`, initially clean). Existing compatibility fixes, fixed rewards,
full-resolution death checks, replay and nonpersistent storage guards were retained.

`README.md` was never edited. Its initial and verified final SHA-256 is:

```text
3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
```

## Launch and immediate controls

Requires the repository's existing LuaJIT/LÖVE 11.5 setup; no new dependencies.

```sh
make card-trial
./roag.sh --card-trial --trial-seed=202
./roag.sh                       # normal title / dungeon
```

The normal title still offers **CARD TRIAL**, with `T` as its shortcut. Choose
Bare, Movement or Burning with `1`–`3`, arrows or clicks, then Enter/Begin. The
seed carousel remains **101, 202, 303**; a valid custom command-line seed also
works. Seed 202 was used for the full GUI comparisons.

The opening hand is always Dash, Sweep, Mend. **Movement works immediately**:

- WASD/arrows perform one cardinal Step. A blocked enemy tile uses the normal
  bump Strike semantics. These inputs do not require selecting a card.
- Click an adjacent empty cardinal tile to Step, or an adjacent enemy to Strike
  (including diagonal enemies). Hovering a legal basic target previews it.
- `F` arms an aimed basic Strike; direction + Enter is the keyboard alternative
  to clicking its adjacent target. `Q/E/Z/C` select diagonal attack directions.
- Basic actions keep every technique card and do not advance the card RNG.
  Distant clicks never pathfind or queue actions.

For techniques, **hover a card → inspect possibilities; select it → hover an
exact target; click that target → play**. There is no subsequent action category
or confirmation button. Mend has a self target: clicking its face activates it.

| Control | Meaning |
| --- | --- |
| Card face / `1`–`3` | Select that slot's named technique; Mend activates directly |
| Board hover | Exact footprint/path/landing for the armed technique |
| Target click | Commit once |
| Directions, then Enter | Aim/play an armed Dash or Sweep |
| Arrows, then Enter | Cycle legal Knight Hop landings and jump |
| Tab | Inspect the next card without arming or spending it |
| Replace button beneath each card | Replace just that instance; pays one Wait |
| `R` | Replace the armed/focused card; identical paid operation |
| Escape / right click | Cancel selection/inspection; otherwise open trial menu |
| `I` / `V`, F1 | Boon/stat inspection, trial control help |

An armed card displays **ARMED**. While armed, direction inputs aim that technique;
cancel to resume direct Steps. Hover/focus, aiming and cancellation are free.
Input is swallowed during playback, including classic utilities and the `B`
developer loadout. Replace and activation have disjoint hit regions.

The reward uses `1`–`3`/arrows/click to select, Enter/click Claim to acquire. Escape
closes it; Enter/Reopen Reward restores the same offers. Trial menu/result:
`R` replays the same preset/seed, `P` chooses another preset, `T` returns to title.
Enter resumes the menu or replays a result. There is no trial Save/Continue.

Controller callbacks retain D-pad Step/aim, shoulders card focus, A select/play,
X aimed Strike, Y paid Replace, B cancel, Back inspection and Start menu.
Physical controller usability remains untested.

## Deck, actions and resources

The small data layer is `src/card_trial_content.lua`: four Dash instances, four
Sweep instances and one Mend, each with a stable numeric ID. The three-card
opening uses one of each; only the remaining six are independently shuffled.
The `card-trial:techniques/v2` stream never shares combat/world randomness.

| Action | Effect | Base world time at speed 100 |
| --- | --- | --- |
| Basic Step | Existing one-tile cardinal movement / ordinary bump behaviour | 100 |
| Basic Strike | Existing aimed Adventurer Sword Strike, base damage 4 | 100 |
| Dash card | Existing Adventurer Dash, normally two straight tiles, stopping at blockers | 130 |
| Sweep card | Existing three-cell Sweeping Slash, base damage 3 before defence | 150 |
| Mend card | Restore up to 4 HP; use one healing charge | 100 |
| Replace | Discard one card, draw one; actual existing Wait | 100 |
| Knight Hop card | Jump to a free L-shaped landing | 130 |

Normal speed scaling, terrain rules, attack modifiers and scheduler ordering
remain authoritative. Dash retains terrain extension and applicable path boons.
Sweep retains special-attack and primary-hit identity. These actions do not add
an extra Wait or make all enemies act once per player input.

A technique discards only its instance, refills that slot, and retains the other
two cards in place. Replacement does the same but resolves one real Wait,
including applicable Wait boons. Its visible hover explanation explicitly says
that enemies and scheduled effects advance. Basic Steps/Strikes never cycle cards.
When drawing requires it, shuffle only the discard pile into the draw pile.
A just-discarded instance may therefore be drawn again during a necessary shuffle.

Two healing charges are shared across both fights. Mend caps at maximum HP;
**Full health** and **Healing exhausted** reject without spending anything. It
uses the existing real `Healed` event/source and never emits Wait or pickup
events. The successful second Mend retires its instance into an accounted-for
`retired` pile and draws a replacement in the same action. It cannot return in
later shuffles. The sidebar marks the resource **MEND EXHAUSTED**. Replay restores
the nine instances, opening hand and two charges. Boon healing does not refresh
these charges. Healing occurs before scheduled enemy responses; final HP can be
lower than the previewed recovery if an incoming attack resolves during its cost.

## Knight Boots and the unchanged comparison board

Starting presets remain Bare, uncommon Rhythm of Steel + Ring of Reversal, and
uncommon Emberbrand + Shared Agony. Enemy definitions, timings, spawns and arena
are unchanged: two Mossbound Guards and one Thornspitter per fight, a 9×9 playable
interior, and the two-cell central pillar. Trial kill rewards still suppress
unrelated XP/stat growth and loot without changing real death/kill attribution.

The fixed first-fight reward now offers **Knight Boots**, rare **Pulse Step**, or
rare **Detonation Bloom**. The ordinary boons use their existing acquisition path.
Knight Boots is a trial upgrade, not a new entry in the boon catalogue. The reward
view compares straight Dash destinations with the eight L-shaped destinations.

Knight Boots changes every Dash instance's definition to **Knight Hop** across
hand/draw/discard. Identity, pile order, card RNG, HP and charges stay unchanged.
Names/descriptions/previews update immediately. There are no extra cards or heals.
The full boon catalogue, including Grave Dividend, remains intact; only this
trial's curated offers changed.

Knight Hop accepts offsets `(±2,±1)` and `(±1,±2)`. It ignores intervening cells,
but the landing must be inside the arena, walkable and unoccupied. There is no
additional invulnerability. It emits one `Moved` with identity
`trial:ability/knight_hop`, `movement_kind='jump'`, no traversed path, then one
`TileEntered` and pickup check at the landing. Existing origin/landing reactions,
Rhythm, Ring escape recognition and Pulse Step can apply. Intermediate terrain,
pickups and path-adjacent attacks cannot apply. Phantom Lunge, Froststep Catalyst
and effects requiring `actual_dash` do not treat this jump as Dash. Playback uses
the existing displacement snap behaviour rather than a walking interpolation.

Code-verified positions, with one-based coordinates including the outer wall:

- From **(6,8)**, Dash to **(8,8)** is legal and useful for straight repositioning;
  Knight Hop cannot land there.
- From **(6,8)**, Knight Hop to **(7,6)** is legal; Dash cannot reach that offset.
- From **(5,6)**, Knight Hop reaches **(7,5)** across the pillar at **(6,5)/(6,6)**.
  Eastward Dash is blocked by **(6,6)**. This is a real difference in board use,
  not a universal ranking of the two techniques.

Fight two still enters the same chamber/clock after the full first-fight
resolution. HP, position, boons, all card piles/definitions/RNG, charges and ongoing
schedules carry. Resolved fights block farming; simultaneous death takes
precedence over rewards or victory.

## Preview and implementation boundaries

Hollow circles mark legal possibilities, inner outlines mark the exact footprint,
small path squares mark actual Dash traversal, and a ghost marks the landing.
Knight Hop draws no intermediate trail. Enemy telegraphs remain visible outside
the inset preview outlines. Mend previews the recoverable HP segment and the
charge to be consumed, without claiming the action is safe.

Sweep/Strike show **HIT N** primary HP loss above affected enemy sprites. The
forecast calls the existing resolver on a private snapshot; it does not duplicate
damage calculations or change live RNG/state. It excludes secondary boon chains
and later ticks from the advertised total. Random crit/dice builds are not
advertised as exact forecasts; the curated trial builds have neither.

`src/cards.lua` owns instance/pile operations. `src/card_trial.lua` owns preview,
legal-target enumeration and trial flow. `src/game.lua` validates and resolves
basic/technique/Replace intents through the existing machinery. `src/card_trial_ui.lua`
owns disposable hover/selection state; `main.lua` adds only input/overlay hooks.
`src/playback.lua` recognizes jumps and upgrade grants. `src/visual_juice.lua`
accepts the upgrade chest's explicit reward icon. No assets, normal save schemas,
boon definitions, enemy definitions or normal controls were replaced.

## Verification and actual observations

```sh
make card-test
xvfb-run -a -s '-screen 0 1024x768x24' make card-gui
xvfb-run -a -s '-screen 0 1024x768x24' make gui
make assets-check
```

- **31 focused tests passed**: basic-action retention, exact consumption/refill,
  reshuffle conservation, independent RNG, ownership/target rejection, Mend
  retirement, upgrade identity/pile preservation, all Knight landing cases,
  intermediate/landing effects, boon identities, forecast/commit agreement,
  timing, reward carryover, lethal resolution, UI hit regions, cancellation,
  replay and persistence rejection.
- Relevant existing regressions: **109 passed, one unchanged pre-existing
  failure**, established before edits and reproduced afterward. The old dice
  art assertion expects removed `assets/kenney/1-bit-pack/symbols.png`; restored
  art was not changed. The broad full suite was not run.
- Existing classic GUI Save/Load/Continue, preview and controls passed. Asset
  verification passed **599 bindings from 67 unchanged source PNGs**.
- **Native X11/xdotool input** in the actual application exercised direct opening
  movement, card hover, Dash selection/target-click, Sweep selection/target-click,
  and Mend hover/activation. Native Sweep advanced time from 200 to 350 and
  discarded/refilled one instance without another Enter press.
  Native Mend then used one charge and 100 time: its capped three-HP recovery
  was offset by a three-HP enemy hit, leaving net HP unchanged. The recovery bar
  describes the heal itself; the existing intention cells/timers show that risk.
- **Public LÖVE callback harness** completed Movement, Burning and Bare at seed
  202: 22/21/32 actions and final HP 11/9/6, respectively. It checked all technique
  hovers, paid replacement, reward close/reopen, Knight Boots, jump preview and
  click, invalid landing, both charges/retirement, death/replay, input locking and
  a normal classic launch. These are scripted legal-input checks, not human fun
  or balance scores.
- A separately labelled **controlled GUI fixture** demonstrated Sweep's exact
  primary labels and the pillar jump. The jump landed on a test bramble, took
  one contact damage, collected only the landing currency, triggered Pulse Step
  and Rhythm once, and did not trigger Phantom Lunge. An occupied L destination
  rejected unchanged. This fixture adds hazards/pickups only in test construction;
  the playable trial board remains unchanged.
- Normal active-run/profile/preferences fixtures remained byte-for-byte unchanged
  after GUI play/replay/exit. All **35 actual user storage files** also retained
  their original hashes. README hash checks passed. Screenshot inspection used
  the fixed **1024×640** window, also the smallest supported size.

Local generated receipts/screenshots are ignored artifacts under
`artifacts/card-trial/tranche-02/verified-gui/`. Useful evidence:
[primary preview](../../artifacts/card-trial/tranche-02/verified-gui/sweep-primary-preview.png),
[Knight Boots reward](../../artifacts/card-trial/tranche-02/verified-gui/knight-boots-choice.png),
[pillar jump preview](../../artifacts/card-trial/tranche-02/verified-gui/pillar-hop-preview.png),
[landing result](../../artifacts/card-trial/tranche-02/verified-gui/pillar-hop-after.png).
These artifacts are local test output, not required game assets.

Observed interaction: direct movement no longer waits for a card/category
sequence; card selection followed by a target click executes immediately.
Holding the other instances does not interfere with basic movement. The reward
visibly changes the card name and destination pattern. A visual pass caught and
fixed labels hidden beneath sprites and text overlapping the armed indicator.

No mechanic or enemy-pressure deviations were introduced. Remaining clarity
limits: Sweep targets an adjacent direction, not any distant enemy; armed
directions aim instead of stepping until cancelled; a refill can change a slot's
technique; and an HP recovery preview is not a prediction of net HP after enemy
responses. Windows/macOS, physical controllers, subjective audio and a fresh
human usability/balance playtest remain unverified. Scripted victories do not
establish that the controls are understandable to a new player.

The next human check is deliberately narrow: can a player predict each hovered
card, Step and play a technique without a control sequence, and use the pillar
differently after choosing Knight Boots? No additional movement forms, obstacle
clearing, arbitrary editing or broader balance changes are included.
