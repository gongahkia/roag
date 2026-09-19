# Repository handoff — after the final chat implementation pass

This is a complete runnable 0.4.0 tree, built on the shared complete 0.3.0 release.
It contains the existing colony/material loop plus workforce control, living frontier,
dungeon encounters, charges and wards. It does not require previous chat history.

## Start with these files

`README.md` (run/play), `PROJECT_STATE.md` (current status), `TEST_REPORT.md` (what was
actually executed), `docs/ARCHITECTURE.md` (state and module boundaries), `AGENTS.md`
(change discipline), `docs/WORKFORCE.md`, `docs/ECOLOGY_RULES.md` (developer spoilers),
`docs/MAP_FORMAT.md` (interchange). Inherited documents are under `docs/legacy-v030/`.
Do not mistake prior-version evidence for current tests.

## Development commands

```sh
make test LUA=lua
make gui LUA=lua
make soak LUA=lua
make run
```

Without make, use the commands in README. No dependency install step or build tool
is necessary for the game. The source intentionally avoids Lua 5.2+ syntax. Tests
need a Lua runtime; real graphics need LÖVE. F10 runs the core regression suites inside the actual LÖVE runtime;
`tests/*gui*.lua` remain explicit input/draw-contract mocks, not real GUI tests.

## Current module seams

- Labour and direct work: `labor.lua`, `colony_commands.lua`, `jobs.lua`, `ui/crew.lua`.
- Ecology and knowledge: `catalog.lua`, `content.lua`, `ecology.lua`, `signals.lua`.
- Field tasks and demolition: `fieldwork.lua`, `blasts.lua`, `structures.lua`.
- Terrain/content generation: `generation/frontier.lua`, `layouts.lua`, `wonders.lua`.
- Persistence: `history.lua`, `mapfile.lua`, `codec.lua`, `json.lua`, `storage.lua`.
- Tests: original core, map suite, expansion suite, three GUI mocks, soak and benchmarks.

## Deliberate limits, not hidden claims

The unfamiliar ecosystem has authored finite archetypes; it is not an infinitely
procedural taxonomy or chemistry engine. Creatures do not have full worker pathfinding.
Dungeons are physically embedded room compounds, not a separate adventure/combat mode.
Melee interactions are elementary contact/worker cull/counterattack. No weapons,
projectiles, equipment, crafting-chain explosives or colony guard AI exists yet.
Wards and signal ranges are deliberate abstractions. Materials use local falling-sand
rules, not pressure, temperature diffusion, rigid bodies or support stress. No online
services, language models, cloud saves, multiplayer or monetisation are involved.

## Useful next work, in order of evidence

1. Validate the real Linux/LÖVE window and input paths, especially crew panel, file drop,
   initial-camera readability and hotkeys. Run F10 on the intended LuaJIT build.
2. Profile worker planning and active material updates on the target machine. Cache or
   incrementally rebuild only when equivalence tests can protect replay and geometry.
3. Improve player feedback: an explicit jobs/assignment list, navigation overlay for
   blocked field tasks, target-selection cycling for overlapping encounters, and visible
   population/ecology histories. These are more valuable than another inert resource.
4. Balance food/water throughput versus 3/6/9 workers through actual play. Do not make
   every seed survivable; separate harsh content from mechanical bugs.
5. Deepen one connected system at a time: equipment/expedition logistics, power/heat,
   colony defence, or ecology-driven production. Add accounting and replay first.

These are a handoff roadmap, not promises of background development.

## Reporting a local failure

Record source commit/release, LÖVE version, OS, map seed/layout/profile/crew, mode,
tick and the exact action sequence. F6 exports selected-state metrics, recent events,
context and a full **live** replay. F12 captures a real screenshot. Include modified
source files. A map template alone does not preserve jobs, dead workers or active fuses.

Back up the original failing run before experimenting. Reproduction should happen
in a copied save directory or headless tool, never by blindly overwriting the user's
current colony. Legacy fixtures cover genuine old versions; add a new fixture when
changing a rule version rather than rewriting an old expected result.
