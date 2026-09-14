# Final content and release audit — 2026-09-14

This pass covers the Python 3.11+ Jomon game run from the repository root,
including its authored catalogs, terminal presentation, vessel and regional
stories, and the tavern games. Historical milestone reports describe earlier
trees; the results below were checked during this pass.

## Content and replay breadth

The current content verifier reports eight regions, 20 regional lines, five
cross-region arcs, 24 mixed situations and 24 mutable micro-sites, eight
sanctums, 24 elite situations, 80 distinct standard enemies, 12 voyage families
with 12 stateful variants and 12 later echoes, 38 persistent nonhostile adults,
and three tavern games. The physical catalog includes 94 containers, 72
weapons, 36 armour pieces, 48 passives, 32 techniques, 16 relics, and 11
vessel refits. Its 24 executable build scenarios cover six pressure families.
The content verifier reports no failures.

In 25 sampled worlds, the quest audit found 200 distinct, reachable regional
geographies, 40 regional endings across the 20 lines, and 11 cross-region
endings across the five arcs, with no reported failures. A separate 100-world
systemic audit found a distinct geography for every region in every sampled
world and 315 distinct production encounter compositions, with no reported
failures. These results support meaningful route, opposition, build, and
decision variation; they do not measure entertainment or playtime.

No ten-hour duration is certified. The existing shortest
landing–objective–landing fixture ranges from 110 to 182 ordinary in-world
actions per region, but input compression, reading, combat, optional work, and
player decisions make action counts unsuitable as wall-clock estimates. A
timed human playthrough of the complete campaign and a replay with materially
different choices are still required before claiming 10+ hours of replayable
content. Record active play time separately from idle terminal time, plus
completed lines, side work, deaths, returns, and repeated routes; judge whether
the second run changes decisions and consequences rather than merely adding
walking time.

## Player-facing language

The pass reviewed active terminal and HUD strings, descriptions, conversation
and consequence text, notifications, and authored JSON. Overt implementation
language such as generated layouts, seeded events, objective topology, skill
nodes, configuration branches, and grid cells was recast as places, work,
household records, witnessed terms, and physical consequences. Controls,
costs, ranges, and save errors remain explicit where concealment would make
the interface less usable or a failed save harder to diagnose. The imported
Dullest Dungeon catalog is presented through its in-world office adaptation;
its historical raw catalog is not the player-facing copy path.

An 80×24 terminal session covered the landing, character acceptance, working
deck, and clean quit using a temporary data directory. Static inspection and
that short session cannot certify every rare conversation or late-campaign
screen; a complete manual campaign remains the coverage gap.

## Verification and release limits

- Python 3.11 compilation, the content verifier, and current-format save
  round-trip passed.
- Focused audits passed: 100 replay samples, 100 encounter samples (900 plans,
  257 unique production compositions), 25 quest samples, 100 living-route
  samples, and an 80-leg memory soak. All reported zero failures.
- The 100-world systemic audit passed with zero failures. The documented
  1,000-world run was not repeated on this text-focused final tree.
- The fast suite passed all 118 tests. The full suite result is recorded in the
  final handoff once its current run completes.
- `git diff --check` and `compileall` passed. An unconfigured `ruff check jomon
  tests` diagnostic reports 76 style/unused-code findings; Ruff is
  not a declared project check or CI gate.

The game runs from the repository root on the tested Linux terminal. A complete
all-region human campaign, timed replay, and native macOS/WSL terminal checks
remain unverified. Until those are done, “ready to ship” is supported for the
tested technical paths and breadth of authored content, not for the requested
ten-hour experience or every advertised platform.
