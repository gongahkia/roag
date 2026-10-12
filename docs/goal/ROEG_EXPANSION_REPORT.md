# ROEG autonomous expansion report (working)

Status: **IN PROGRESS**. The committed-source Git handoff and M0 baseline are
verified in [`ROEG_EXPANSION_PROGRESS.md`](ROEG_EXPANSION_PROGRESS.md). This
report is not a completion claim.

## Acceptance ledger

| Goal criterion | Evidence required | Current state |
|---|---|---|
| Two floors in each of seven biomes | Normal generated stages, 100 seeds per biome, stairs, pursuer, snapshot and GUI checks | Four inherited biomes now generate validated paired floors in normal New Run; 100 seeds each, focused GUI/disk passed. Three new biomes and two-floor full-run Victory remain pending. |
| Six functional terrain mechanics | Canonical event, AI/path/LOS, editor, generated play and save tests | Pending |
| Sixteen regular enemies and seven unique minibosses | Registry, encounter pools, tactical combat, telegraph and reward tests | Pending |
| Four playable classes | UI selection, distinct attacks/mobility, legal progress and save tests | Pending |
| Fifty functional boons | Four rarities each, ordinary chest rolls, synergy and chain tests | Pending |
| Studio contextual audio | Fresh-process import/preview/publish, runtime cue and music transitions, fallback | Pending |
| End-to-end release acceptance | Existing/new headless and GUI, deterministic replay, legal victories, disk continuation, performance | Pending |

## Handoff and baseline

The Mac→Windows transfer used GitHub by the user's explicit confirmation.
Fetched `origin/main` is an ancestor of local HEAD; the one local-only commit
adds only the autonomous goal specification. Before this goal's edits the working
tree was clean. Git does not prove that external Studio saves or uncommitted Mac
files were transferred. Baseline: **283/283 headless**, **8/8 game GUI** and
**6/6 Studio GUI** in one isolated round, three disk-resume scripts passed, and
seed 3 reached legal Victory in **991 actions**. See the progress log for commands
and exact revision evidence.

Milestone findings, schema IDs, evidence and handback instructions will be added
as implementation proceeds. Do not infer completion from an empty pending row.

## M1 checkpoint

Connected-floor code is in `src/floors.lua`, `src/generation.lua`,
`src/game.lua`, `src/scheduler.lua`, `src/stage.lua` and `main.lua`. E03's
optional stair authoring is in `src/studio_world.lua`,
`src/studio_world_editor.lua` and `src/rooms.lua`. The current test suite is
**295/295 headless**; established game GUI **24/24** across three rounds,
focused floor GUI **3/3**, Studio E03 GUI **3/3**, and isolated two-process
connected-floor Save/Continue passed. One seed-3 legal action trace entered
floor 2 in 106 moves with 16 HP; an assisted Warden fixture verified the route
handoff. This does **not** prove a normal two-floor complete Victory or resolve
the measured longer critical path. See `ROEG_EXPANSION_PROGRESS.md` and
`docs/ROEG_MULTIFLOOR_EXPANSION.md` for exact mechanics, limits and commands.
