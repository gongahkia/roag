# Project state — Cosmonauts 0.4.0

## Intent

Independent from-scratch Lua/LÖVE side-view colony management with fine material
simulation, engineering-led emergent stories, and unforgiving failure. Preserve
finite maps, deterministic history, evaluation tools and real resource transport.
This is the final chat-delivered feature pass; the repository is the next source of truth.

## Implemented in this release

Existing v0.3 loop retained: particles, body-aware workers, digging/hauling/building,
food/water/beds/pumps, needs/deaths, read-only challenge archive, practice branching,
preview/import/export, actual maps and regression fixtures.

Added 5 layout choices (11 total), 4 geological biomes (12 total), 3 profiles (7 total),
3/6/9-person frontier starts, living/ruins/none content and density control. Five seeded
multi-room compound types with finite loot and hazards. Three growths, four creature
types, four site types, observation/survey notes, physical salvage and cull jobs.

H per-person priorities and pinned roles, AUTO percentage quotas with exact headcounts,
Y named work orders, Shift+Y block reassignment, M/J move-hold/release. A worker-built,
worker-armed finite-fuse charge can destroy terrain/structures and kill people or fauna.
A water-consuming ward interacts with ecology. All states and commands replay/save.

## Versions

Application/save envelope 0.4.0; base world 0.2.0; optional feature template 1;
map schema 2 (reads 1); current generator frontier-v2, preserved frontier-v1 path.
Legacy save identity deepward_02 is retained for compatibility. Actual local user edits were not available; this tree was
built on the shared v0.3.0 ZIP. The earlier top-down Terrain Lab was not modified.

## Known constraints

Native Lua 5.4 and mocked interface checks are recorded in TEST_REPORT. Actual LÖVE,
LuaJIT, SDL and GPU validation remain local tasks. Fixed bounded populations; local
creature steering; elementary combat; no fog of war, infinite worlds, heat/pressure
solver, structural stress, equipment or full social model. Colony destruction is a
valid outcome, not a failed test by itself. Game balance needs real play sessions.

Read HANDOFF.md and AGENTS.md before the next implementation change.
