# Diegetic interface, living vessel, route chart, and seasonal world

## Baseline

Work begins from clean commit `79ed0c7f325bb7112e1716c244fa47b3ffa27368`.
The active format-4 build has four persistent regions, one compact 48×16
aboard map, an immediate four-destination chart menu, a populated but single-map
tavern, transactional manual spatial packing, and no calendar or named-person
schedule clock. Its recorded verification baseline is 92 tests, a clean
compile and whitespace check, a 100-seed encounter audit, and eight complete
PTY expeditions.

No discrepancy, uncommitted work, or concurrent commit was present at the
start of this tranche. `HEAD` and `origin/main` both named the expected commit.

## Bounded implementation plan

1. Replace direct destination selection with one seed-derived, persistent
   route graph whose nodes have distinct time, supply, market, weather, cargo,
   encounter, charting, integrity, and seasonal consequences. Render it as an
   ASCII chart and animate only its presentation; route resolution remains an
   explicit deterministic state transition.
2. Add a persistent calendar, three aligned vessel decks, a dedicated tavern,
   a named bartender and eight bounded drinks. Advance scheduled named adults
   and causal social incidents only when accepted actions move the world clock.
3. Improve the existing inventory rather than replace it: committed-source
   placement ghosts, item preview art, a six-location paper doll, pinning,
   deterministic auto-place, transactional auto-pack, bulk actions, and a
   central optional curses mouse-normalisation boundary.
4. Use one consistent choice treatment for conversations, recruits, bar,
   merchant, voyage, objective, environmental, and route decisions. Repair the
   startup warning with a pure, resize-safe centred wrapping calculation.
5. Increment the save to format 5 and deterministically migrate format 4,
   preserving people, regions, exploration, possessions, cargo, markets,
   consequences, vessel integrity, and voyage history while assigning route,
   deck, schedule, and calendar state without wall-clock input.
6. Run focused and full tests, the existing encounter audit, a deterministic
   route/schedule audit, compile and whitespace checks, then exercise normal
   play through real PTYs at required dimensions. Record only paths actually
   completed.

## Architectural limits

`GameState` remains the single persistence root. A small route module may own
the authored graph and route consequences; a small vessel module may own deck
maps, schedules, drinks, and bounded incidents. `inventory.py`, `travel.py`,
`people.py`, and `terminal.py` retain their existing responsibilities. There
will be no generic graph generator, schedule language, needs model, windowing
toolkit, story engine, real-time sailing loop, offline catch-up, fifth region,
or general social simulation.

## Assessment

Implementation and verification evidence will replace this section at the end
of the milestone. Until normal PTY trips exercise the chart, travel, vessel,
tavern, packing, schedules, incidents, calendar, and migration, this milestone
is not assessed as playable.
