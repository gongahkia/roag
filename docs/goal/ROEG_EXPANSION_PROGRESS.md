# ROEG autonomous expansion progress

Updated: 2026-10-12. This is a checkpoint for the single goal in
[`../ROEG_AUTONOMOUS_EXPANSION_GOAL.md`](../ROEG_AUTONOMOUS_EXPANSION_GOAL.md).
The goal is **active**. M0 is verified; M1's connected-floor implementation is
working in normal New Run and has passed focused verification. M2's six
mechanics are integrated and have passed focused verification; broader
full-run acceptance is still open. M3's three additional biome geometries and
route choices are integrated. M4's ten regular enemies and six new biome
minibosses are integrated and have passed focused checks. The full expansion
is not complete.

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
| M2 | Six editor-authorable terrain mechanics | Integrated in connected-floor generation and E03; 100-seed-per-biome, canonical damage, save, GUI and Studio reopen checks pass | Carry wider tactical/balance review into M8; proceed to M3 |
| M3 | Seven distinct two-floor biomes and route choices | Three new strategies/template families and a three-choice route graph are integrated; 100 paired seeds each, GUI and disk checks pass | Prove default two-floor legal full run under M8 |
| M4 | Sixteen regular enemies; seven biome-specific minibosses | Ten new tactical roles and six new elites integrated in normal pools/arenas; focused AI, damage, phase, GUI and disk checks pass | Carry balance and ordinary two-floor survival into M8; proceed to M5 |
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

## M2 — Mechanical terrain: implemented and focused checks passed

`src/terrain.lua` defines six additional tiles and receives canonical
`TileEntered`, `AttackPerformed` and `DamageTaken` events through the existing
resolver. Bramble contact uses the normal damage pipeline; bog changes
speed-scaled recovery; ice enables a fully previewed three-cell Dash; attacks
break barricades; mist blocks player and ranged AI sight while passing
projectiles; crystals arc a positive lightning hit to another nearby hostile
through ordinary damage, kill credit and rewards. Built-in tile colors/glyphs,
fog filtering, short presentation cues and E03 World Editor authoring are
included. Exact provisional rules and controls are in
[`../ROEG_TERRAIN_EXPANSION.md`](../ROEG_TERRAIN_EXPANSION.md).

The first floor places bramble, bog and ice; the second places barricade, mist
and crystal. Placement consumes a derived terrain-only RNG stream. A 100-seed
corpus for each of the four inherited biomes validated **800 connected-floor
maps** after terrain placement. Retry/repair/fallback counts per biome were:

| Biome | Retries | Repairs | Fallbacks |
|---|---:|---:|---:|
| Overgrown Ruins | 0 | 0 | 0 |
| Fungal Hollows | 0 | 0 | 0 |
| Sunken Aqueduct | 0 | 0 | 0 |
| Ashen Bastion | 1 | 0 | 0 |

An initial Ashen corpus had four fallback maps: some side-alcove barricade
candidates overlapped critical corridor cells, and repair removed the
barricade. Candidate filtering now excludes every recorded corridor cell;
the rerun above has no fallback. The existing repair rules were not weakened.

The full suite passed **307/307** (`295/295` after M1). New tests cover all six
semantic properties, 800 generated maps, player/enemy contact timing, movement
costs, partial/extended Dash, barricade collision and restored breakage,
mist/AI sight, lightning-to-crystal chains and credited secondary kills,
old connected-floor snapshot compatibility and E03 authoring. Eight established
game GUI harnesses passed **24/24** over three isolated rounds. New terrain
game and two-process Studio smokes passed **3/3 each**; Studio E03 passed
**3/3**. The connected-floor fresh-process disk resume passed. Focused game,
Studio and disk smokes were repeated once after the final terrain refactor;
`git diff --check` passed. The GUI checks are callback-driven, not human play.

M2's remaining integration risks are balance and discoverability: each new
terrain appears once per paired biome in this first distribution, only the
bramble cell received an explicit rendered-frame action check, and no physical
playtest has judged whether mist and crystals are immediately understood.
These are M8 acceptance items; do not claim a two-floor legal full Victory from
the older single-floor driver.

## M3 — Three additional biomes and route graph: layout slice verified

Glacial Observatory, Fractured Archive and Sundered Forge now have distinct
plain-data authored room families, generation strategies, visual palettes and
terrain distribution. Observatory uses a two-loop radial/star-node pattern
with ice; Archive uses staggered shelf aisles and parallel lanes with mist;
Forge uses offset furnace bays, widened asymmetric passages and crystals.
The stage-one menu retains Fungal/Aqueduct as its first two choices and adds
Observatory. Stage-three menus offer three deterministic unvisited identities,
making all seven reachable across runs. Existing two-offer extraction menus
remain cached and loadable from old snapshots. The Thorn Warden and inherited
regular enemies are **temporary M3 encounter pools**; seven unique minibosses
and ten new regular roles remain M4. See
[`../ROEG_BIOME_EXPANSION.md`](../ROEG_BIOME_EXPANSION.md).

The new 100-seed-per-biome corpus validated **600 new concrete floors** (300
paired maps) with 1/0/0 retries for Observatory/Archive/Forge, zero repairs and
zero fallbacks. Combined with M2's four-biome corpus this is **1,400 floors**
across seven biome identities. New strategy metrics over the 100 paired seeds:
Observatory 400 loop edges and 2,461 widened star nodes; Archive 272 loops and
1,037 parallel connections; Forge 400 loops and 2,172 widened bays. These are
structural differences, not palette-only claims. All pairs retain 12 rooms,
five paid chests, two secrets, reciprocal stairs and a lower-floor elite.

Verification includes a prepared two-handoff route fixture (Ruins →
Observatory → Forge) using actual zero-time `Game.submit` confirmation, exact
snapshots for all three new biomes, a seed-3 **ordinary-action** Observatory
entry/one-Lancer kill in the retained *single-floor* guide (371 actions, 11 HP),
and the unchanged legacy legal Victory (991 actions, 5 HP, world time 98,800).
The single-floor guide is **not** proof of a default two-floor full Victory.
The new GUI smoke captured each new biome on both floors in **3/3** isolated
rounds; all established game GUI harnesses passed **24/24**; Studio E03 passed
**3/3**; separate two-process disk Continue checks passed for all three new
biomes. The final M3 full headless run passed **314/314**; `git diff --check`
passed.

The temporary M3 encounter note above records that milestone's historical
state; M4 now replaces those new-biome pools and non-Ruins elites. Carry M1's
full two-floor legal Victory and stage pacing risk into M8, and preserve old
cached routes on Continue.

## M4 — Regular tactical roles and biome elites: integrated, focused checks passed

The roster now contains the original six regulars plus **exactly ten** new
regular enemy identities. Their roles are cover breaker, ranged skirmisher,
locked space denial, pack coordinator, delayed ambusher, anti-cluster
artillery, healer/support, charger, bounded summoner and positive-hit
retaliator. All are normal generated/pressure-pool content. The Thorn Warden
is exclusive to Overgrown Ruins; Fungal, Aqueduct, Bastion, Observatory,
Archive and Forge each have a distinct elite. Each new elite has two locked
footprints, phase-two speed/pattern change, the existing reward/extraction
lifecycle and normal kill credit. Source definitions and provisional numbers
are documented in [`../ROEG_ENCOUNTER_EXPANSION.md`](../ROEG_ENCOUNTER_EXPANSION.md).

The `tests/expansion_encounter_tests.lua` checks each roster/pool, alternating
elite cells, phase behavior, exact pending-windup restoration, all ten regular
roles, real barricade break, allied healing, bounded summoning, charge path,
same-batch artillery friendly fire and retaliation after HP loss but not
blocked damage. The seven-biome 100-seed headless corpora remain in the full
suite. The new encounter GUI smoke captured an actual Mycelial Matriarch
telegraph/health bar and Forge Conjurer summon intent in **3/3** isolated
rounds; the prepared screenshot positions are assisted fixtures. The new
two-process LÖVE disk smoke restored a pending Matriarch attack and matched
its next authoritative snapshot exactly. Studio E03 edit/publish/fallback
remained **3/3**. The established game GUI suite first produced **3/24
stage-harness timeouts** because the new dynamic elite label lost the original
uppercase `THORN WARDEN` text expected by that callback. The log showed a
Lua assertion at `main.lua:83` inside the harness, not evidence of a native
segfault. Restoring that label made the isolated stage smoke pass; the full
established suite then passed **24/24** over three fresh-process rounds.

The seed-3 ordinary-action *legacy single-floor* guide reaches Victory with
the new Fungal and Bastion elites in **945 actions, 3 HP, total simulated time
94,200**; replay trace: `/var/tmp/roeg-m4-seed3.trace`. The baseline 991/5/
98,800 figures remain historical, and the exact-count meta test now expects
945 because the encounter content changed. A default two-floor full Victory
and human tactical/readability acceptance are still unproven. No pressure
cap, player action cost, scheduler tie rule, boon trigger or save/profile
schema changed. The final M4 headless rerun passed **323/323**; `git diff
--check` passed.

Next: M5 playable class selection/action identities, then M6 boons and M7
Studio audio. M8 retains the default two-floor legal Victory, balance,
fresh-process and physical-playability limits.
