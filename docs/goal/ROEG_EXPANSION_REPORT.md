# ROEG autonomous expansion report (working)

Status: **IN PROGRESS**. The committed-source Git handoff and M0 baseline are
verified in [`ROEG_EXPANSION_PROGRESS.md`](ROEG_EXPANSION_PROGRESS.md). This
report is not a completion claim.

## Acceptance ledger

| Goal criterion | Evidence required | Current state |
|---|---|---|
| Two floors in each of seven biomes | Normal generated stages, 100 seeds per biome, stairs, pursuer, snapshot and GUI checks | Seven biomes generate validated paired floors; 100 seeds per biome, focused GUI/disk and all-class legal full Victories pass. |
| Six functional terrain mechanics | Canonical event, AI/path/LOS, editor, generated play and save tests | Six integrated in paired-floor generation; 800-map seed corpus, headless mechanics, Studio/game GUI and snapshot/disk gates passed. Physical readability/balance remains untested. |
| Sixteen regular enemies and seven unique minibosses | Registry, encounter pools, tactical combat, telegraph and reward tests | Ten new regular roles plus six unique non-Ruins elites integrated; focused headless, GUI and pending-windup disk checks pass. Human balance remains untested. |
| Four playable classes | UI selection, distinct attacks/mobility, legal progress and save tests | Four kits and title selection pass. Exact-replay six-floor legal Victory traces pass for Adventurer seed 9, Ranger seed 9, Arcanist seed 45 and Berserker seed 28. |
| Fifty functional boons | Four rarities each, ordinary chest rolls, synergy and chain tests | 26 new, 24 inherited; class-aware offers, ten cross-system synergies, focused GUI/disk and ordinary paid-chest activation pass. |
| Studio contextual audio | Fresh-process import/preview/publish, runtime cue and music transitions, fallback | 272 contextual cues, Studio browser/picker/mixer and music transitions integrated; headless and 3/3 fresh-process audio GUI pass. Physical listening untested. |
| End-to-end release acceptance | Existing/new headless and GUI, deterministic replay, legal victories, disk continuation, performance | 351/351 integrated headless, all-class legal Victory and M7 game/Studio/audio GUI rounds pass; final focused GUI/disk/profile and handback audit underway. |

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

## M2 checkpoint

Six gameplay terrain symbols are implemented through `src/terrain.lua`,
`src/content.lua`, `src/generation.lua`, `src/game.lua`, `src/ai.lua` and
presentation in `main.lua`/`src/playback.lua`. E03 tile authoring uses the same
semantic flags without migrating existing three-tile projects. The full
headless suite is **307/307**; 100 paired seeds per inherited biome yielded
**800 valid floors**, one retry and no repairs/fallbacks after excluding
barricades from critical corridors. Established game GUI **24/24**, terrain
game **3/3**, terrain two-process Studio **3/3**, E03 Studio **3/3**, and
two-process connected-floor disk Continue passed. A generated barricade break
restores exactly, and a crystal secondary kill awards ordinary XP/currency.
See `docs/ROEG_TERRAIN_EXPANSION.md` for rule values and editor controls.
The full goal remains **IN PROGRESS**: ten regular enemies, six additional
minibosses, three additional classes, 26 boons and Studio audio remain.

## M3 checkpoint

Glacial Observatory, Fractured Archive and Sundered Forge have separate
template families, geometry strategies, palettes and route choices. Across all
seven biomes, 100 paired seeds each produced **1,400 valid concrete floors**;
the three new biomes had one retry total, no repairs and no fallbacks. The
headless suite passed **314/314**. The established game GUI suite passed
**24/24** across three isolated rounds, Studio E03 **3/3**, new biome GUI
**3/3** and three two-process disk Continue checks passed. A single-floor
ordinary-action trace entered Observatory and killed a regular enemy; the
legacy single-floor seed-3 legal Victory still passed in 991 actions. These do
not prove full Victory under the new default two-floor mode. New-biome regular
encounters and all non-Ruins elites still use temporary inherited content;
M4 must replace it. See `docs/ROEG_BIOME_EXPANSION.md` and the progress log.

## M4 checkpoint

The temporary M3 encounters above are superseded. Six new biome elites and
ten regular archetypes now use biome-specific stage spawning, unique elite
footprints, ordinary locked windups, phase changes, canonical friendly fire
and rewards, and serialized support/summon/charge follow-ups.
`docs/ROEG_ENCOUNTER_EXPANSION.md` lists their IDs, tactical roles and
provisional values. Focused headless tests cover pending elite continuation,
all roles, barricade destruction, healing, summoning, charge traversal,
same-batch friendly fire and retaliation conditions. The established game GUI
suite passed **24/24** on rerun after correcting a Warden label regression;
new encounter GUI **3/3**, Studio E03 **3/3**, and a fresh-process
pending-elite disk Continue passed. The seed-3 ordinary-action legacy
single-floor Victory changed to **945 actions, 3 HP, 94,200 simulated time**
under the new Fungal/Bastion elites. The initial stage-GUI timeout was an
explicit callback assertion from changed text, not a proven native crash.
The default two-floor full-run Victory remains unestablished. The entire goal
remains **IN PROGRESS** while classes, 26 boons, Studio audio and M8
acceptance remain. Exact final headless count is recorded in the progress log.

## M5 checkpoint

All four selectable class kits now use the normal title → New Run path and the
same deterministic action pipeline. Ranger bow/volley/evasive Dash, Arcanist
lightning/fire sigil/phase step and Berserker heavy crush/broad cleave/armored
lunge are genuinely different from the unchanged Adventurer. Their IDs and
stats survive route/floor/finale transitions and fresh-process Save & Quit /
Continue. The full headless suite passed **330/330**, established game GUI
**24/24**, class GUI **3/3** four-class isolated rounds, and a Ranger disk
continuation matched exact snapshots before/after the next committed action.
All four won an ordinary first encounter on default paired-floor seed 3.
The old single-floor guided stage-one route reached stage two for three
classes; Arcanist's melee-oriented guide hit a navigation limit. These are
not default two-floor victories. M6 class-specific transformations and
class-aware offers are still required. See `docs/ROEG_CLASS_EXPANSION.md`.

## M6 checkpoint

The required 26 new tactical/class boon identities are integrated, bringing
the registry to 50. New Run's frozen ordinary pool is 46 (20 original + 26
new); the older four achievement boons still require permanent unlocks.
Class-specific offers are filtered for each starting class, and old
Adventurer-only transformations no longer appear for the other three classes.
Generic stateful prime/speed/ward/mark effects and event predicates power
terrain, enemy-originated and chain interactions; six class transformations
modify the ordinary ability pipeline. See `docs/ROEG_BOON_EXPANSION.md` for
the full catalogue and provisional strengths.

The headless suite reached **341/341**. Focused tests activate each new boon,
check ten mixed old/new synergies, exact save/restore, generation chest
eligibility over 400 paired stages and deterministic rejection of blocked
transform geometry. A real seed-50 paired-floor ordinary-action trace buys
Common Rhythm of Steel through a paid chest and uses it in 59 actions.
The new callback GUI smoke passed **3/3**; a two-process LÖVE run restored
new primed boon state and its next action exactly. The legacy single-floor
legal Victory remains 945 actions, 3 HP, 94,200 simulated time. Default
two-floor all-class Victory and human balance remain M8 work. M7 Studio audio
is still outstanding at this checkpoint.

## M7 checkpoint

`src/audio_cues.lua` registers 272 data-driven contexts for four classes,
all 50 boons, regular enemies, seven elites, six terrain mechanics, seven
biomes, room templates and UI/world events. `src/studio_audio*.lua` provides
versioned WAV/Ogg source storage, original sound synthesis, a visible Studio
library/cue picker/mixer, checked dry-run/explicit publication, bounded LÖVE
voices, transitions, F5 refresh and safe built-in fallback. See
`docs/ROEG_AUDIO_EXPANSION.md` for exact user steps and provenance rules.
The integrated suite passed **351/351**. The eight established game GUI
harnesses passed **24/24**, six Studio harnesses **18/18**, and the new audio
fresh-process GUI smoke **3/3**. The audio smoke includes import/audition,
reopen, contextual room/theme editing, published playback, corrupt-pack
fallback and Save & Continue. These are callback-driven, not a human hearing
or physical-input test. Separate pack publication remains non-atomic across
audio, visuals, worlds and themes.

## M8 checkpoint

All four classes have exact-replay **ordinary submitted-action** six-floor
Victories against the Hollow Regent: Adventurer seed 9 (949 actions, 22 HP),
Ranger seed 9 (1,050 actions, 8 HP), Arcanist seed 45 (1,269 actions, 6 HP)
and Berserker seed 28 (1,037 actions, 35 HP). Each includes three paid chest
purchases. Trace artifacts are `/var/tmp/roeg-m8-<class>.trace` and the
headless suite asserts all four. No player/enemy HP mutation, teleport,
invulnerability or forced route unlock is used by the driver. Final focused
GUI/disk regression, performance, compatibility and portable handback review
remain underway; this is not a complete-goal claim yet.
