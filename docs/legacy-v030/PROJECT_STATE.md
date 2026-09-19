# Project state — Deepward 0.2.0

## User direction

A finite side-view colony-management game with Noita-inspired fine material
simulation. Settlement engineering is primary; stories should emerge from
interlocking systems. Inspirations include RimWorld, Dwarf Fortress and Caves of
Qud. Difficult/unforgiving; recovery from every situation is explicitly NOT a
requirement. Full researched code is delivered for direct copying into files.
Breadth-first, algorithms implemented from scratch in Lua/LÖVE.

## This milestone

First interconnected playable prototype, not a finished game. Three starters;
mining, hauling, building, beds, farms, irrigation, pump operation, hazards, permanent
loss, record/replay, exports, a benchmark and tests. Challenge is default. Practice
is chosen only for a new run and allows branch editing and material brushes.

Original Terrain Lab 0.1 stays separate. This is a full world-model replacement,
not a patch that preserves old saves. Existing authored noise/PRNG primitives are
reused. There are no external gameplay libraries/assets/fonts.

## Authoritative conventions

Fine cells are one-based flat arrays, index `(y - 1) * width + x`.
One building block is 4 x 4 fine cells. Workers have a 2 x 3 footprint; position
is the left foot. Current default world is 192 x 112.

`world.height` is now the MAP ROW COUNT, not the old elevation array.
`world.mat[i]` stores material; update stamps prevent repeated same-tick motion.
Immutable-by-convention state is not shared between historical material snapshots.

Update order: tick/input -> material simulation -> item falling -> infrastructure
and crops -> colonists/work -> bounded housekeeping. No gameplay decisions use
render dt, clock measurements, `math.random` or `love.math.noise`.

Challenge history never replaces live state. Historical commands are rejected.
Practice branching truncates commands/checkpoints after the branch. Save/export
uses the live frontier even if the UI is viewing the archive.

## Verification

Read `TEST_REPORT.md` for exact executed checks and limits. Real LÖVE/LuaJIT/GPU
execution was not available in the authoring environment. Core Lua 5.4 tests and a
clearly labelled mock UI adapter were run. The software preview is not a screenshot
from LÖVE. Do not reuse container CPU timings as user-hardware frame rates.

## Next handoff

Run `love --version`, then `love .`. F10 tests under the user's actual runtime.
First verify the game window, construction, irrigation/harvest, and archive policy.
F6 exports replay/context/metrics/chronicle; F12 captures the real window.
Send the current source snapshot with any local changes, seed, tick, steps and
observations. Do not reconstruct a changed repository from conversational memory.

## Candidate next work (not promises)

First: actual-runtime fixes and playability feedback. Then: navigation/work-order
readability, varied generation and authored discoveries, richer infrastructure,
heat/gases with actual quantities, and only then broader society/ecology. Prioritise
mechanics with multiple existing connections. Do not equate harshness with bugs.

Supply planning uses a reverse reachability cache to reject known one-way pickup
trips. This is not global safety planning and does not promise recoverability.
