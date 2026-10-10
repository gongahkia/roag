# ROAG Combat-Run Gameplay Progress

This is the live implementation record for the combat-first roguelike run
direction. It supersedes older character-construction and persistent-progression
plans for the normal run entry flow. The regional world and persistent courier
remain available as setting and save data; a selected combat class and its
boons belong only to one disposable run.

## Requirements and status

| Requirement | Status | Main locations | Evidence / remaining work |
| --- | --- | --- | --- |
| Quick class entry, stated five-boss objective, remembered selection | Implemented; partial interactive check | `roag/main.py`, `roag/profile.py`, `roag/run_classes.py`, `roag/terminal.py` | `python -m roag` opens title, seed prompt, four-class choice and board. Class choice and opening message now state the sanctum/claimant route. Full human playthrough still needed. |
| Four fixed class kits: Breaker, Marksman, Trickster, Sapper | Implemented | `roag/run_classes.py`, `roag/actions.py` | Basic attacks use the class weapon; `B` is movement and `X` signature. Focused class tests pass. |
| Direct attack flow and one authoritative world response | Implemented; partial interactive check | `roag/terminal.py`, `roag/actions.py`, `roag/session.py` | Live `A` with no legal target opened a zero-time target cursor; scripted target commit, cancellation and class actions passed. Ability preview/Tab now use class legality, and attack target text names the action. Full human mixed combat still needed. |
| XP choice-of-three, stack feedback and safe reward queue | Implemented and verified | `roag/run_rewards.py`, `roag/session.py`, `roag/terminal.py` | Threshold offers queue, resolve at zero time, flush buffered terminal input, and block all other committed session commands until chosen. |
| Shared, stackable boons that never add active slots | Implemented; representative effects verified | `roag/run_items.py`, `roag/run_rewards.py`, `roag/data/run_items.json` | Forty-five universal definitions; 36 eligible on a fresh default run (31 nonboss). Dormant threshold copies, baseline Cheap Key, unused reroll credit and late route hints were corrected or excluded. Per-effect current/next preview shows actual cadence or magnitude. Representative Quarry Song x2 + Powder Echo chain passed. Catalogue JSON remains unchanged so existing saves load. Individual balance needs playtesting. |
| Chests, elites, bosses and terrain discoveries as rewards | Implemented and verified | `roag/run_loot.py`, `roag/run_progression.py` | Stage chest, combat/elite XP and drops, boss awards, plus one bounded terrain discovery per stage. |
| Meaningful destruction, displacement and delayed devices | Implemented and verified | `roag/actions.py`, `roag/run_progression.py`, `roag/terrain_actions.py` | Breaker slam and Sapper charges clear only soft ordinary terrain; charge displacement, decoy targeting, and terrain-caused credit have focused coverage. |
| Varied small tactical encounters with occasional crowds | Implemented baseline; feel unverified | `roag/run_progression.py`, `roag/enemy_ai.py` | Encounter families schedule swarm (3), crossfire/pincer/hazard/reinforcement (2), elite hunt (1); decoy attracts investigation and ranged/flanker roles have distinct goals. Full human balance/readability playtesting remains. |
| Five-stage run, final boss, death and clean retry | Mechanically verified with prepared route; human route unverified | `roag/main.py`, `roag/run_progression.py`, `roag/profile.py`, `roag/sanctums.py` | Reachable shrines/entries, breach, boss spawn, owned kill, reachable threshold/branch and final victory passed across five stages with position/health setup. Death-to-fresh-world reset passed. Unassisted end-to-end play remains. |

## Implemented design decisions

- The four classes retain exactly three manual actions: basic `A`, movement
  `B`, and signature `X`. Shared rewards attach to hits, movement, kills,
  terrain and other stated conditions; none create an extra button or hotbar
  slot.
- Breaker charges a visible nearby enemy and slams adjacent enemies/soft cover.
  Marksman vaults to a short clear landing and fires a piercing cardinal lane.
  Trickster exchanges with a visible target and places a short-lived `?` decoy.
  Sapper hops, places a visible `!` timed charge, and detonates after two
  further world turns.
- Cooldowns and decoy/charge timing use authoritative world steps. A committed
  action advances the world once; reward menus, inspection and selection do
  not advance it.
- Existing physical terrain still owns collision, sight and replacement state.
  Class destruction is intentionally limited to soft, ordinary, replaceable
  tiles so it cannot erase exits, links, objectives or protected structure.
- XP begins at three points, presents three deterministic eligible options,
  and queues every crossed threshold. The pool deliberately excludes
  effect-only ranged, close-combat and circuit rewards that would be dead for
  one of the four starting kits. One per-stage terrain discovery is an
  additional bounded reward route, not an infinite farm.
- Any player-owned delayed charge, terrain shockwave, or bounded secondary
  strike receives the same kill credit as a direct hit. Secondary kills cannot
  recursively create another secondary blast, preventing duplicate rewards or
  runaway chains.
- On victory or death, `R` begins a new generated world using the remembered
  class, while `C` returns to class choice. This deliberately recreates world
  state instead of attempting to scrub a settled simulation in place.

## Validation evidence

- `python -m roag.checks --pattern test_combat_run_classes.py` — 12 tests pass:
  every fixed kit, all actions, one-step class action scheduling, immediate
  reward blocking, stack caps, indirect kill credit, terrain mutation, and
  reset on a new run.
- `python -m roag.checks --pattern test_roguelike_run.py` — 12 existing
  five-stage run, boss, death and persistence tests pass after the changes.
- `python -m compileall -q roag tests` passed after the initial implementation.
- `python -m roag.checks fast` — 152 tests pass after the final reward-boundary
  change (93 seconds).
- Initial working tree was clean. No unrelated changes were overwritten.
- The tactical-rewards assertion expected `prepare handgonne`, while the
  established display string includes the item name `Powder handgonne`.
  `git show 7cfd2022` confirms the same assertion and text predate this pass.
  The assertion now matches the displayed weapon name; its ammunition,
  smoke and reload behaviour checks remain.

The class-selection screen and opening Trickster board were inspected in a
curses terminal: ASCII rendering, the class ability labels, objective, class
controls and encounter notice rendered correctly at the supported 80-column
layout. A full hands-on combat/readability pass is still required; automated
checks do not establish combat feel.

## Focused acceptance pass (2026-10-10)

Acceptance validation after the targeted fixes: `python -m unittest
tests.test_combat_run_classes -q` passed 23 tests; `python -m
roag.checks --pattern test_roguelike_run.py` passed 13 tests;
`python -m roag.checks --pattern test_tactical_rewards.py` passed 3 tests;
`python -m roag.checks --pattern test_targeting_visibility.py` passed 8
tests; `python -m roag.checks fast` passed 153 tests. Content verification,
compilation and `git diff --check` passed. Full `python -m unittest
discover -s tests -q` ran 1,090 tests and failed with 66 failures and 11
errors; a second full discovery after the last test addition ran 1,091
tests with the same failure counts. The clean pre-pass parent (`9e607232`)
ran 1,079 tests and had 67
failures and 11 errors. Failure identities match exactly apart from the
pre-existing tactical-rewards wording assertion corrected here; no new
full-suite failures appeared. The inherited failures affect older catalogue,
save-format, character/succession, workshop, navigation, and production
fixtures and remain outside this focused pass. Full-suite gate remains red.

- Normal entry: `python -m roag` opened the curses title, seed prompt,
  four-class chooser and Hearthford board at 80x24. Live `A` with no visible
  legal target opened the cursor; Escape returned to the board. Target feedback
  initially said “cast” for an attack and has been corrected. Live combat with
  each class was not performed in the terminal; class combat evidence below is
  deterministic scripted gameplay, not a human feel verdict.
- Scripted fixed kits: all four basic attacks, movement actions and signatures
  work before boons. Breaker displaces and cracks soft terrain; Marksman vaults
  a blocked middle cell and pierces a clear cardinal lane; Trickster swaps and
  draws investigation with a decoy; Sapper hops and owns a two-turn charge.
  The class target handler commits one world step, cancels without time, and
  now previews legal lanes/landings and cycles ability targets independently
  of weapon range. Scripted attack targeting (`A` dispatch inspected, Tab,
  Enter) and signature confirmation each cost one turn for all four classes.
  A wall now stops later piercing targets.
- Reward/ownership: multiple XP thresholds queue and block the next action;
  reward selection and inspection are zero-time. A successful selection flushes
  buffered input, including between queued rewards. Queued capped choices
  refresh to eligible alternatives. Two simultaneously due prepared charges
  killed eight enemies, credited eight kills once, queued two choices and
  advanced one recorded world step. This test prepares device positions and
  due time directly; it does not simulate placing both through the UI.
- Stacks/availability: threshold values now improve on every offered copy;
  `backwater-map` caps at its actual two-lead effect; `greed-lure` stacks alter
  both drop cadence and pressure through its third copy. `cheap-key` appears
  only when the current challenge has a salvage cost it can reduce; late
  `backwater-map` is withheld after stage three; `reclaiming-mark` is excluded
  because no reward UI spends its recycler credits. Boss wet movement now
  grants a numerical barrier so repeated stacks are meaningful. Run display
  wording now matches the implemented effects. A live launch exposed a save
  fingerprint mismatch after editing the mechanical catalogue, so that file
  was restored unchanged. `load_game()` then opened the existing chronicle.
  Effective caps and text overrides live in `roag/run_items.py`.
- Representative build: Breaker Slam destroys reeds. Quarry Song x1 leaves a
  two-health enemy alive; Quarry Song x2 kills it, then Powder Echo kills a
  second enemy two cells farther away. Both kills earn one XP each and the
  activation messages identify the shockwave and defeat burst. Secondary
  blast kills cannot launch another blast.
- Terrain: three ordinary cut actions on standing timber changed `T` to `.`;
  collision, line of sight and region reachability all changed from blocked
  to open. The stage sanctum landmarks remained present. Stage loot placement and branch
  reachability use the same region topology; human navigation remains open.
- Encounter sample (`acceptance encounter route` seed, stage transitions
  prepared): Hearthford hazard opening had a shooter and suppressor;
  Greenwold pincer had a protector and flanker; Greywash reinforcement
  pressure had a thief and shooter; Dunmire elite hunt put one elite near
  the landing; Marlbank hazard had a suppressor, controller and thief nearby.
  These are local patrol counts, not all enemies in the whole region. AI
  goal tests cover flanking, pursuit, decoy investigation, smoke/water retreat
  and ranged positioning; actual mixed-encounter feel remains unverified.
- Progression route: reach each region's shrine, open its sanctum (the breach
  option works without a commodity), enter the stair to spawn the named
  claimant, defeat it, collect boss reward/XP choice, climb the newly attached
  threshold and select a physical branch. The fifth owned keeper kill sets
  `run.status == victory` and `world_ended`, with five boss credits and no
  pending reward. The route test checks shrine/entry/branch reachability in
  all five stages but jumps to each site and lowers each boss to one health.
  It is a route and ordering test, not an unassisted survival proof.
- Death/retry: a fatal hit ends the run; constructing the next generated world
  with the remembered class clears boons, pending rewards, charges, decoy,
  cooldowns and old terrain mutations. The terminal retry screen itself was
  inspected in code but not reached interactively during this pass.

### Shared boon audit

The shared pool is class independent. All four classes can attack, move, take
damage, kill enemies, open drops, and use the packed terrain tool; conditional
smoke, wet ground, height and pressure are world rules available to all. Each
named definition is in `roag/data/run_items.json`; two run-only effective caps
and corrected descriptions are in `roag/run_items.py`. Current and next
effects are rendered by `boon_effect_summary` in the reward and build views.
The following groups cover every currently shared effect route:

| Trigger/route for every class | Shared boons and stack property |
| --- | --- |
| Owned attacks and qualifying hits | River Edge (damage 1–5), Claimant's Mark (elite/boss damage 1–5), Forked Current (arc 1–5), Storm Chain (arc 3–12), Measured Execution (execute threshold 5–20%), High-Ground Rule (elevated damage 1–5), Unbroken Route (move-chain damage cap 1–8), Winter-Eye Prism (arc 4–16, unlocked boss). |
| Kills and retaliation | Powder Echo (blast 1–6), Coalheart Wake (blast 4–16), Red Thread (heal 1–4), Red-Water Covenant (heal/noise 2–10), Barbed Account (retaliate 1–6). |
| Movement and field conditions | Ferryman's Step (guard every 5→3 moves), Crosswind Step (exposure refund every 6→4 moves), Reed Sole (three terrain-delay categories), Weather Eye (vision 1–4), Backwater Map (one or two later route leads), Smoke Memory (smoke armour 1–4), Flood-Hook Regulator (wet guard/barrier 2–10). |
| Taking damage, guarding and health | Iron Hide (armour 1–4), Deep Lung (health 1–10), Brace Knot (guard armour 1–5), Last Plank (one to three lethal saves per run), Weathered Blood (low-health armour 1–5), Borrowed Heart (hurt barrier 2–10), Kiln-Seal Crucible (hurt barrier 3–15). |
| Terrain work or destruction | Work Gloves (terrain power 1–5), Quiet Wrap (sound reduction 1–3), Stone Wedge (stone salvage 1–4), Coppice Hook (vegetation salvage 1–4), Quarry Song (shockwave 1–5), Root Engine (vegetation heal 1–4), Fault Reader (support-loss reduction 1–3), Walking Quarry (terrain blast 3–12), Crown-Tooth Flywheel (terrain power 3–12), Fault-Bell Resonator (terrain shockwave 4–16). |
| Loot, pressure and run economy | Salvage Tally (kill salvage 1–6), Cheap Key (salvage discount 1–4, challenge gated), Greed Lure (drop every 4→2 kills and pressure +1→3), Field Dressing (pickup heal 1–5), Risk Dividend (pressured kill salvage 2–12), Double Lot (two or three combat-drop candidates), Golden Pressure (loot roll/pressure 4–16), Replicating Ledger (duplicate chance 10–50%), Wreck-Chain Dividend (kill salvage 4–16). |

The table records routes and disclosed limits, not exhaustive per-class
playtesting of every item. The initial XP pool deliberately excludes legacy
circuit-only, melee-only, ranged-only and unused reroll-credit definitions.

### Acceptance verdict and next action

Partial: focused combat-run behavior and the prepared five-stage victory and
retry routes pass, with no newly failing broad-suite tests. Full live combat
with each class, reward-focus usability under held input, an unassisted
five-stage victory/death retry, and overall tactical feel still need human
playtesting. The repository-wide suite remains red on inherited failures.
Next, play the normal `python -m roag` run through mixed encounters with all
four classes, then make only fixes supported by observed failures. Triage the
older broad-suite failures separately from the combat-run acceptance work.
