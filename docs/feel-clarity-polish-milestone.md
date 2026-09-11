# Feel and clarity polish milestone

Started 11 September 2026 from
`6f47406d76398e0e1ea1d42e2e283d3278131b9f` on `main`, nine commits ahead
of `origin/main`, with a clean worktree. Python 3.14.7 runs the Python 3.11+
game. Save format remains 7: this pass should present and pace existing facts,
not invent another persistent subsystem.

## Purpose

This pass implements the six highest-value polish priorities from the Qud-style
possibility audit: movement feel, combat readability, contextual discovery,
24-build encounter coverage, narrative pacing, and minimum-terminal/mouse/
restoration finishing. It must preserve immediate Hearthford departure and the
ordinary movement, combat, material, relationship, quest and save reducers.

## Measured baseline

The machine-readable 20-sample baseline is
`docs/performance-feel-clarity-baseline.json`. Ordinary movement has a 12.362 ms
median and 24.449 ms p99; input-to-layout has a 16.323 ms median and 25.674 ms
p99; 80x24 rendering has a 9.131 ms median and 15.676 ms p99; enemy-heavy turns
have a 36.798 ms median and 43.033 ms p99; environment-heavy turns have a
2.065 ms median and 2.770 ms p99. World creation has a 487.300 ms median, load
209.423 ms, save 24.148 ms, and deterministic replay matches. Peak RSS is
35,612 KiB and the benchmark save is 383,285 bytes.

Boarding input/render produced one 129.565 ms p99 sample and lazy frontier
entry one 286.128 ms worst sample. Both remain bounded but are explicit tails
to remeasure rather than conceal.

## Production design

- Make the 80x24 status panel capacity-aware. Health, equipment, position,
  load/ammunition, urgent visible intent, current condition and the most useful
  contextual action must fit; larger terminals retain the detailed breakdown.
- Add a zero-time look cursor, usable by keyboard and mouse, that distinguishes
  visible fact, remembered ground and unknown space. It explains actors,
  material reactions, structure danger, traversal and immediate consequences.
- Derive one compact combat forecast from actual hostile intent, target cells,
  timing and authored counters. Reuse it in the map, status, look and observed-
  actor presentation so they cannot contradict one another.
- Surface only contextually legal manoeuvres, movement actions and material
  opportunities. Hints preview requirements and whether an action spends time;
  they never execute hidden work.
- Audit all 24 documented builds against steady, strained, critical, elite,
  voyage and environmental pressures using capabilities derived from their real
  production equipment and reducers. Tune exposed dead or fake choices before
  changing damage numbers.
- Bound later voyage echoes to one substantial callback per arrival and compact
  repetitive event presentation while retaining exact injury, loss and death
  causes.
- Finish minimum-size command discovery, mouse inspection parity, repeated
  resize behavior, POSIX suspend/resume and terminal restoration tests.

## Limits

- No auto-exploration, real-time input thread, hidden simulation, generic
  planner, combat-log loss, universal tooltip framework or new save version.
- Inspection and forecast functions are pure, bounded projections of saved
  production state; rendering never advances time.
- Existing route following remains safe local walking through the real move
  reducer and still stops on danger, discoveries, material change or new facts.
- Mouse support remains optional. Every operation retains a keyboard path and
  semantic glyph/text fallback in 8-, 16-, 256-colour and monochrome terminals.

## Acceptance gates

- A minimum 80x24 production render shows urgent threat intent, ammunition or
  relevant resources, current harmful condition and one useful contextual verb.
- Look inspection covers visible actors, remembered/unknown cells, materials,
  structures, movement consequences and vertical travel without spending time.
- Hostile forecasts expose origin, path or area, target, timing and counter when
  that information is observable; dangerous off-screen facts remain hidden.
- All 24 builds have actual answers across the six pressure families, no family
  is monopolised by one build, and every weapon family retains a recurring
  tactical purpose.
- One arrival cannot dump multiple substantial voyage echoes. Exact causal loss,
  injury and death messages survive event-feed compaction.
- Base, look, targeting, inventory and scrolling interactions have tested mouse
  behavior; 80x24, 100x32, below-minimum resize/recovery and suspend restoration
  are covered.
- Focused, fast and full suites, compileall, diff check, content/encounter/
  persistence/replay audits, benchmark and memory soak pass without more than a
  20% ordinary-scenario regression.

## Live ledger

- Planning: production input, render, targeting, information, route-following,
  build-scenario and voyage-echo paths inspected. The first concrete defect is
  that only 14 of 17+ status lines fit at 80x24, clipping threat, ammunition,
  conditions and combos despite the nominal minimum-size support.
- Decisions: presentation will reuse current reducers and sparse facts; save
  format stays 7; combat forecasting will not reveal actors outside courier
  sight; event pacing will preserve the saved message ledger and change only
  bounded delivery/presentation.
- Movement and inspection: blocked steps now name the physical obstruction and
  a recovery; `;` opens a zero-time keyboard/mouse look cursor whose FACT,
  REMEMBERED and UNKNOWN labels prevent information leaks. The compact panel
  retains health, kit, objective, load, ammunition, danger, status and one
  current action at 80x24.
- Combat: one pure forecast projects visible origin, target, path or area,
  timing and authored counterplay into the map, status, look, targeting and
  observed-life views. Callers share one FOV snapshot, so the added clarity is
  bounded and hidden actors remain hidden.
- Discovery and pacing: contacts give one current systemic lesson; the base
  legend exposes look, route-following, mastery, aim and actor inspection;
  routine event runs compact without hiding exact deaths, injuries, losses or
  collapses; arrivals deliver at most one substantial deferred voyage echo.
- Balance: the production-derived audit covers all 24 documented builds against
  steady, strained, critical, elite, voyage and environmental pressure. Every
  build has a real answer to every family (144 matchups), and all 36 weapon
  families have a non-damage tactical identity. Frontier ranged kits were
  separated into sling, longbow and crossbow identities; no blanket damage
  increase was needed.
- Performance: an early version exposed a render-path route scan and repeated
  sight calculations. Removing those and caching immutable enemy definitions
  produced `docs/performance-feel-clarity-final.json`: movement 8.655 ms median
  / 12.736 ms p99, input-to-layout 14.225 / 17.177 ms, 80x24 render
  6.479 / 64.319 ms, enemy-heavy turns 30.524 / 39.876 ms, and boarding
  input/render 31.579 / 42.116 ms. All normal medians improved over baseline;
  peak RSS moved from 35,612 to 37,456 KiB (+5.2%) and replay still matches.
- Verification: the fast suite passed 114 tests in 51.095 seconds and the full
  suite passed 559 in 368.185 seconds. The 50-seed packaged audit found no
  failures across content, 450 encounters, 400 region/quest combinations,
  generation, persistence, living-world state, 50 deterministic replays, 4,800
  situation opportunities and a 50-leg memory soak. Generation produced 50
  unique geographies for each of eight regions; the soak grew 6,658 bytes after
  warm-up. The pre-existing expansion's 1,000-seed audit remains the topology
  soak; this presentation pass did not change geography.
- Manual PTY: completed new-world startup, immediate Hearthford departure,
  keyboard and raw xterm mouse look, material inspection and clean quit at
  80x24; completed 100x32 rendering, 60x18 below-minimum warning and resize
  recovery; completed a real POSIX suspend/continue cycle without action-clock
  movement and clean terminal restoration.
- Remaining gate: native play was exercised on macOS only. Linux and WSL were
  not available in this workspace; monochrome and reduced-colour behavior is
  covered by automated semantic-fallback tests rather than a physical terminal.
