# ROAG Combat-Run Gameplay Progress

This is the live implementation record for the combat-first roguelike run
direction. It supersedes older character-construction and persistent-progression
plans for the normal run entry flow. The regional world and persistent courier
remain available as setting and save data; a selected combat class and its
boons belong only to one disposable run.

## Requirements and status

| Requirement | Status | Main locations | Evidence / remaining work |
| --- | --- | --- | --- |
| Quick class entry, stated five-boss objective, remembered selection | Implemented | `roag/main.py`, `roag/profile.py`, `roag/run_classes.py`, `roag/terminal.py` | New run starts immediately after one class choice; no stat screen. Need terminal playthrough check. |
| Four fixed class kits: Breaker, Marksman, Trickster, Sapper | Implemented | `roag/run_classes.py`, `roag/actions.py` | Basic attacks use the class weapon; `B` is movement and `X` signature. Focused class tests pass. |
| Direct attack flow and one authoritative world response | Implemented | `roag/terminal.py`, `roag/actions.py`, `roag/session.py` | `A` fires immediately with one legal target, otherwise opens targeting. Class acts use one time result. Need interactive input check. |
| XP choice-of-three, stack feedback and safe reward queue | Implemented and verified | `roag/run_rewards.py`, `roag/session.py`, `roag/terminal.py` | Threshold offers queue, resolve at zero time, flush buffered terminal input, and block all other committed session commands until chosen. |
| Shared, stackable boons that never add active slots | Implemented and verified | `roag/run_items.py`, `roag/run_rewards.py` | Existing hook-based pool is filtered to effects with a common route across all classes; build overlay shows current/next stack effect, and capped stacks are never offered. |
| Chests, elites, bosses and terrain discoveries as rewards | Implemented and verified | `roag/run_loot.py`, `roag/run_progression.py` | Stage chest, combat/elite XP and drops, boss awards, plus one bounded terrain discovery per stage. |
| Meaningful destruction, displacement and delayed devices | Implemented and verified | `roag/actions.py`, `roag/run_progression.py`, `roag/terrain_actions.py` | Breaker slam and Sapper charges clear only soft ordinary terrain; charge displacement, decoy targeting, and terrain-caused credit have focused coverage. |
| Varied small tactical encounters with occasional crowds | Implemented baseline | `roag/run_progression.py`, `roag/enemy_ai.py` | Encounter families schedule swarm, crossfire, pincer, hazard, elite hunt and reinforcement pressure. Human balance/readability playtesting remains. |
| Five-stage run, final boss, death and clean retry | Implemented and verified | `roag/main.py`, `roag/run_progression.py`, `roag/profile.py` | A settled run returns to a retry/class screen and constructs a fresh world; five-stage progression and reset suites pass. |

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
- Known unrelated baseline failure: `tests/test_tactical_rewards.py` expects
  the exact lower-case substring `prepare handgonne`, while current content
  renders `prepare Powder handgonne`. It is outside this run work and remains
  unmodified.

The class-selection screen and opening Trickster board were inspected in a
curses terminal: ASCII rendering, the class ability labels, objective, class
controls and encounter notice rendered correctly at the supported 80-column
layout. A full hands-on combat/readability pass is still required; automated
checks do not establish combat feel.

## Next concrete action

Do a human playthrough of target selection, reward focus and a mixed encounter
before any balance tuning.
