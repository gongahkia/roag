# ROEG — Tranche 01 Adventurer

ROEG is a new LÖVE roguelike project. The current playable slice has the Adventurer's starting actions in a hand-authored scrolling test area. The authoritative design is in `docs/ROEG_SPEC_v1.0.md`. The original foundation brief is in `prompts/TRANCHE_00_FOUNDATION.md`.

## Run

Install LÖVE 11.x and run `love .` from this directory. The game was built with LÖVE 11.5. For headless tests, install LuaJIT or Lua 5.1+ and run `luajit tests/run.lua` (or `lua tests/run.lua`). No packages are required. On a desktop with LÖVE and `timeout`, `sh tests/gui_smoke.sh` runs a callback/render smoke check and prints a temporary directory with four screenshots to inspect; it does not replace manual keyboard testing.

## Controls

- Move one tile: WASD or arrow keys (100 time).
- Wait: Space or `.` (100 time).
- Sword Strike: F (100 time); Sweeping Slash: R (150 time); Dash: X (130 time).
- In an aiming mode, select a direction with WASD/arrows or numpad 2/4/6/8. Sword Strike and Sweep also accept Q/E/Z/C or numpad 1/3/7/9 for diagonals. Inspect the highlighted preview, then press Enter to commit. Escape cancels with no time cost. Direction selection never moves the player.
- Held-key OS repeats are ignored. A blocked normal move, blocked first Dash step, invalid direction, or canceled aim does not spend world time. Explicit attacks can hit empty or wall cells and still consume time.

Sweep's **provisional** geometry uses the compass order N, NE, E, SE, S, SW, W, NW. It hits three distinct adjacent cells in **counterclockwise, chosen, clockwise** order: N targets NW/N/NE; NE targets N/NE/E. It emits one immediate `AttackPerformed` event containing that ordered area, with no damage in this tranche.

Dash checks up to two cardinal cells in sequence. A blocked first cell rejects the action. If the first is open and the second is a wall, occupied, or off map, the Adventurer moves one cell and pays the full 130 time **once**. If both are open, Dash moves two cells and pays once. The two-cell preview colors open, blocked, and unreachable cells. Blocking entities count as obstacles, but no enemies or bump attacks exist yet.

## Layout and boundaries

- `main.lua` and `conf.lua`: keyboard input, targeting previews, rendering, diagnostics, window setup. The UI uses the 35×25 authored scrolling map; the original 15×11 room remains for foundation tests.
- `src/content.lua`: stable map IDs and validation, walkability used by the simulation.
- `src/game.lua` and `src/targeting.lua`: plain-data state, authoritative action validation and target geometry, ordered events, snapshot/restore at player decision boundaries.
- `src/camera.lua`: presentation-only smooth following and map clamping. It updates from frame time; world time and grid positions do not.
- `src/scheduler.lua`: integer timestamp ordering: committed effects first, then player ready turns, then other actors, with stable sequence/ID ties.
- `src/rng.lua`: simulation-owned serializable Park–Miller generator. Its integer products fit exactly in Lua's double number representation; it is suitable for deterministic gameplay draws, not cryptography. Seeds must be integers from 1 to 2147483646. Cross-runtime equivalence relies on ordinary IEEE-754 number arithmetic.
- `tests/run.lua`: headless checks using the same `Game.submit` path as the LÖVE game.

Snapshots reference the validated map by stable content ID and include schema/content versions, entity positions, world clock, scheduler queue/counters, action/event counters, and RNG state. `Game.snapshot` and `Game.restore` round-trip plain Lua data in memory. Content version advanced from `roeg-content/0` to `/1` for the added map, so older snapshots are rejected; there is no migration. Camera and aiming state are presentation only and are never serialized. Full on-disk save/resume and compatibility handling arrive in a later tranche.
