# ROEG — Codex Tranche 00: Functional Foundation

You are the implementation agent working on **ROEG**, a **brand-new** LÖVE2D/Lua roguelike. The user has selected **GPT-6 Sol High** for this work. This tranche should implement a **thin, functional foundation**, not an abstract engine.

## Required starting context

1. Locate and read `docs/ROEG_SPEC_v1.0.md` in this repository **in full**. It is the authoritative product and architecture specification. This prompt governs the limited scope of Tranche 00. Where the specification identifies provisional values, keep them easy to adjust.
2. Confirm the repository/workspace is for **ROEG**, not the pre-existing **ROAG** game or another project. **Do not touch ROAG, ROEG predecessors, sibling repositories, or unrelated files.** If the expected specification is missing or this is clearly the wrong repository, stop and report the issue rather than inventing the requirements or changing a different project.
3. Inspect existing files, Git status and available tools before writing. Preserve any pre-existing user changes. Prefer LÖVE **11.5** if available, but detect and report the actual installed version. Use Lua syntax compatible with LÖVE 11.x / LuaJIT. Avoid unnecessary external dependencies.

## Objective

Deliver a small game that opens in LÖVE and lets a player move around a hand-authored tile grid, with **the same move/wait/attack simulation runnable headlessly** from a CLI test harness. Put clean seams in place for deterministic turn scheduling, RNG, events and future save state without implementing the full roguelike yet.

### Must implement

**A. Runnable LÖVE smoke game**
- Minimal `main.lua` and configuration as appropriate.
- Render a simple legible ~15×11 tile test room: floor, blocking walls and a distinguishable Adventurer placeholder. Basic shapes are enough; no external art or unnecessary shaders.
- Map WASD/arrows to one-tile cardinal movement. Make movement discrete, with no frame-rate-dependent repeat of authoritative actions. A successful movement should be visible.
- Provide a simple Wait control (document it) and explicit attack control supporting eight-direction targeting, including diagonals; use sensible keyboard bindings and show the controls in a small on-screen hint. No enemies in this tranche, so attacks are allowed to strike empty tiles and must still consume time.
- Attempting to move into a wall consumes no time. Canceling attack targeting consumes no time. The simulation must remain static indefinitely while no valid command is committed.
- Show basic diagnostic feedback on screen: current player grid position, integer simulated time, and most recent committed action. Presentation may use `love.update(dt)` for UI, but **not** for world-time advancement.

**B. LÖVE-independent simulation**
- Put pure Lua game-state, map collision, action dispatch/validation, and scheduling under a cohesive `src/` namespace. **Do not reference `love.*` inside simulation modules**, even indirectly.
- Use integer logical simulation time and a deterministic scheduler with event/actor due timestamps and stable tie ordering. Establish these rules: committed scheduled effects at the same timestamp resolve before fresh actor decisions; player wins equal-timestamp ready-actor ties against enemies; any remaining ties have an explicit stable sequence/ID order.
- Implement the Adventurer's `move`, `wait`, and adjacent eight-direction `basic_attack` intents. Use baseline action cost 100 for each. A committed attack resolves immediately and applies the recovery cost afterward. For now, it may simply emit an `AttackPerformed` event; do not implement enemies/damage yet.
- Provide a lightweight component/state representation with stable runtime entity IDs and floor-aware coordinates. Do not implement ECS archetypes, networking or a general action DSL.
- Make a reproducibly seeded, serializable simulation-owned RNG interface. It must work without LÖVE. Never call unseeded global `math.random` in game rules. Use a reasonable small pure-Lua algorithm or equivalent cross-environment deterministic approach, and clearly document its limitations.
- Produce ordered **plain-data** gameplay events usable later for animation and boon evaluation. Keep these events observational; no unrestricted global event bus.

**C. Persistence seam and validation**
- Define the minimal serializable state shape for this tranche: schema/version ID, active floor/map or reference, player and entity state, integer world clock, next scheduled events with deterministic order, next ID counters, and RNG state.
- Demonstrate a round trip through a plain-data snapshot and restore API, with identical subsequent simulation results. If a durable file codec is quick and dependency-free, add it; otherwise do not invent an unsafe serializer or a massive save framework. Clearly state that full on-disk save/resume arrives later.
- Include content definition identity and validation for at least the hand-authored test map, even if the full content registry is postponed. Never embed Lua closures in snapshot data.

**D. Real tests and docs**
- Provide a one-command, non-graphical test entry point using installed Lua/LuaJIT (document prerequisites). No global `love` variable is allowed in tests.
- Test: idle simulation unchanged; four-direction legal movement; wall collision no-time; explicit empty attack consumes 100; targeting cancel no-time; deterministic replay with fixed seed; scheduler tie priorities; RNG snapshot/restore; state round-trip then identical future actions.
- Supply a short `README.md` describing how to start with LÖVE, run tests, controls, project layout and which architecture boundaries this tranche implements.
- Run every test command actually available. If LÖVE/GUI or a Lua runtime is unavailable, report the exact missing prerequisite and **do not claim the blocked check passed**.

### Out of scope — do not implement

Enemies, combat damage, AI, complex telegraphs, boons, probabilities/proc chains, XP/currency, chests, procedural map generation, secrets, asset pipelines, WFC, multiple floors, save menus, full editor, shader systems, controller/mobile support, network features, and elaborate menu UI. Do not copy architecture or content from ROAG. Don't add interfaces, dependency frameworks or placeholder modules solely for hypothetical future use.

### Engineering boundaries

- Prefer simple Lua modules and explicit dependencies over global mutable state.
- Keep simulation state plain-data and deterministically enumerable. No dependence on Lua table/hash iteration order where observable ordering matters.
- The renderer reads simulation state/events; it must not mutate authoritative gameplay state except by submitting validated player intents.
- A future input layer can swap keyboard for controller without changing simulation action semantics.
- Avoid overfitting to current wall-clock FPS. Future camera smoothing and sprite batching are for later tranches.
- Do not perform destructive Git operations, change unrelated configuration, or create commits without an explicit user request.
- Finish this **one tranche only**. Do not continue autonomously to the next tranche.

## Acceptance criteria

1. The user can launch the game with `love .` from the ROEG root (assuming LÖVE is installed) and visibly move in the room, Wait, and attack eight directions without enemies.
2. The simulated world clock changes only on valid committed actions, and is displayed.
3. The CLI tests use the same authoritative action functions used by LÖVE, without graphics initialization.
4. Repeating the same seed and intent sequence yields identical state and event ordering; snapshot/restore does not alter future results.
5. There is no game-logic dependency on LÖVE and no accidental changes outside the ROEG project.

## Completion report format

When finished, respond with:

1. **Verdict:** COMPLETE / PARTIAL / BLOCKED (be strict).
2. **Implemented:** actual interactive behavior and architectural boundaries, not aspirations.
3. **Files changed:** grouped by responsibility.
4. **Verification:** exact executed commands, pass/fail counts, and any GUI steps you did or could not perform.
5. **Design deviations/risks:** anything that differs from the spec, plus newly discovered integration questions.
6. **Next tranche recommendation:** only what Tranche 01 should do, without implementing it.

Do not claim 60 FPS, platform portability, future editor compatibility or full serialization without having measured/tested those claims.
