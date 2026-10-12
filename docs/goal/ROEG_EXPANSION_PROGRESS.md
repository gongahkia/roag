# ROEG autonomous expansion progress

Updated: 2026-10-12. This is a checkpoint for the single goal in
[`../ROEG_AUTONOMOUS_EXPANSION_GOAL.md`](../ROEG_AUTONOMOUS_EXPANSION_GOAL.md).
The goal is **active**. M0 is verified; M1's connected-floor implementation is
working in normal New Run and has passed focused verification. M2 terrain is
next. The full expansion is not complete.

## M0 — Git handoff and baseline: VERIFIED

The user explicitly confirmed that the intended Mac source was pushed to GitHub
and pulled into this Windows/Fedora WSL checkout. An archive or transfer marker
was intentionally not used. On 2026-10-12:

- `git fetch origin main` succeeded. `origin/main` and `FETCH_HEAD` were
  `05a292b3c98ce849c8c224d7518c6f22bf3484c6`.
- `git merge-base --is-ancestor origin/main HEAD` succeeded. Local HEAD was
  `377478ff671470d2e63a64027c4a27c06f8772ba` and was one commit ahead.
- `git diff --name-status origin/main..HEAD` showed only the addition of
  `docs/ROEG_AUTONOMOUS_EXPANSION_GOAL.md`. There were no unexpected source
  changes in the local-only commit.
- `git status --porcelain=v1 --untracked-files=all` was empty **before** this
  progress documentation was written. Required game source, specification,
  Studio modules, tests and publisher files were present.

This verifies the **committed repository source** under the user's stated Git
handoff. Git does not establish the transfer of uncommitted or ignored Mac files,
nor external LÖVE Studio project, run-save or profile data. Those resources are
not claimed recovered. The project identity in `conf.lua` is `roeg-foundation`.
An isolated LÖVE 11.5 probe of `love.filesystem.getSaveDirectory()` returned
`$XDG_DATA_HOME/love/roeg-foundation`; the probe used `/var/tmp` and did not
write the player's normal save directory. Never use the normal player save
directory for expansion tests.

Baseline on Fedora WSL2, LuaJIT 2.1, LÖVE 11.5:

| Check | Actual result |
|---|---|
| `TMPDIR=/var/tmp luajit tests/run.lua` | 283 passed, 0 failed; existing four-biome 100-seed corpora passed |
| `TMPDIR=/var/tmp sh tests/repeat_gui_smokes.sh 1 /var/tmp/roeg-expansion-m0-game-gui` | 8/8 isolated game GUI harnesses |
| Studio E00, E01, E03, E04, E05, E06 GUI scripts once each | 6/6 isolated Studio harnesses |
| `TMPDIR=/var/tmp sh tests/boon_disk_resume.sh` | Passed, fresh-process Burning/Temporal Echo continuation |
| `TMPDIR=/var/tmp sh tests/biome_disk_resume.sh` | Passed, six LÖVE processes |
| `TMPDIR=/var/tmp sh tests/finale_disk_resume.sh` | Passed, fresh-process finale and tombstone |
| `TMPDIR=/var/tmp luajit tests/finale_playthrough.lua 3 /var/tmp/roeg-expansion-m0-seed3.trace` | Legal Victory: 991 actions, 5 HP, time 98,800 |

The historical 272/272 count was superseded by this checkout's 283/283. Baseline
GUI coverage above is one round; repeated rounds remain a later integration gate.
The older intermittent GUI timeout/signal-139 history is not claimed fixed.

## Feature and milestone matrix

| Milestone | Required outcome | Current evidence/status | Next action |
|---|---|---|---|
| M1 | Two connected, traversable floors per generated biome; stairs, frozen inactive floors, saves and E03 markers | Working normal-run two-floor slice; seed, legal stair, GUI, save, Echo, windup, pursuer and route checks pass; broader legal full-run/pacing acceptance remains open | Keep the paired-floor path under M8 legal Victory and pacing review |
| M2 | Six editor-authorable terrain mechanics | Not implemented | Add validated semantic tiles and canonical event hooks after M1 |
| M3 | Seven distinct two-floor biomes and route choices | Four single-floor biomes inherited | Add three templates/strategies and 100-seed corpus per biome |
| M4 | Sixteen regular enemies; seven biome-specific minibosses | Six regular enemies and reused Thorn Warden inherited | Extend AI/content through shared damage and telegraph pipeline |
| M5 | Four playable classes | Adventurer inherited | Add class selection and three distinct action sets |
| M6 | Fifty working boons, four rarities each | Twenty-four inherited | Add 26 validated effects and class-aware chest eligibility |
| M7 | Studio contextual audio import, picker, binding, transitions, publication and runtime playback | No complete audio workflow | Build editor and presentation-only audio pipeline |
| M8 | Full legal-action, GUI, save, seed and performance acceptance | Baseline only | Run full acceptance matrix after integrated content |

## M1 — Connected floors: implemented and focused checks passed

Normal title **New Run** now requests two generated floors per stage. Route
handoffs preserve this mode. Historical `Game.new_stage(seed)` fixtures and old
single-floor saves remain supported; explicit
`Game.new_stage(seed,{multifloor=true})` exercises the new mode headlessly.
Each floor has six authored/template rooms; the whole biome has five paid chests,
two ordinary secrets and one lower-floor Warden. Reciprocal stairs consume one
movement recovery. Floor-local AI, impacts and delayed effects park on inactive
floors and resume with due times shifted by exactly the time away. Stage pressure
and extraction deadline stay global. The single pursuer follows stair changes
without cloning or resetting its deadline. E03 accepts an optional protected,
transform-aware stair marker; old world source files without it remain valid.

Focused evidence on 2026-10-12:

- 100 seeds × four inherited biomes × two floors validated via
  `Generation.generate_pair`: 800 concrete floors, 12 rooms and five paid
  chests per biome; retries 0/0/0/1 (Overgrown/Fungal/Aqueduct/Bastion),
  zero fallbacks. The built-in four-biome legacy corpus also remains in the
  full headless suite.
- `tests/expansion_floor_tests.lua` covers legal seed-3 entrance-to-stair
  movement (**106 submitted moves**, 10,600 simulated time, 16 HP), authored
  transformed stair placement, both directions, stage route, frozen enemy work,
  actual Temporal Echo, pending locked windup, pursuer/deadline ping-pong and
  blocked/alternative landings. Warden defeat/route in this focused test uses
  an **assisted fixture** and is not a normal-play Victory trace.
- `TMPDIR=/var/tmp sh tests/expansion_floor_gui_smoke.sh` passed three fresh
  callback/render processes; `tests/expansion_floor_disk_resume.sh` passed a
  two-process isolated LÖVE Save & Quit/Continue with exact state and next
  action equality. Screenshots are under the printed `/var/tmp/roeg-floor-*`
  paths. The disk script checks that normal New Run requested connected floors.
- Eight established gameplay GUI harnesses passed **24/24** in three isolated
  rounds. Studio E03 edit/reopen/publish/game/fallback passed **3/3**.
  Historical seed-3 single-floor legal Victory remains **991 actions, 5 HP,
  time 98,800**; that driver is not a two-floor full-run proof.
- A pursuer test found a real Lua queue-iteration defect: both landing and
  pursuer searches examined only the initial queue element. They now traverse
  their bounded breadth-first queues. A failed compressed-spacing experiment
  violated protected room/corridor geometry and was reverted; generation keeps
  the validated 104×68 compact floor slots.

`TMPDIR=/var/tmp luajit tests/run.lua` passed **295/295** (baseline 283/283).
`git diff --check` passed. Existing 24 boons, saves, profiles, Studio art and gameplay rules
are unchanged. The active-run schema remains `roeg-run/11` because optional
paired-floor fields coexist with the fully validated single-floor snapshot.

## Open risks and next action

- The generated critical path to the Warden is longer in the four measured
  seeds: two-floor distances 179/211/194/176 tiles versus legacy
  156/165/151/165. The tighter geometry trial failed validation. The user
  needs a larger legal-action pacing corpus and possibly more meaningful
  encounters near stairs; do not trade away protected geometry for speed.
- The existing full legal-action Victory driver still uses the preserved
  single-floor fixture path. A non-assisted two-floor Warden/finale Victory
  trace is required before full-goal acceptance.
- Older real active saves should continue to load; fresh-process paired-floor
  disk resume passed in an isolated save directory. No live player save or
  Studio source data was read or written.
- The Studio visual and room publication formats must remain readable while E03
  gains stair markers. Preserve authored source and built-in fallback.

Next: implement M2's six semantic terrain interactions through a small shared
terrain contract, renderer and E03 tile authoring. Preserve all M1 tests and
recheck the legal two-floor path after terrain generation is enabled.
