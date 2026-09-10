# Pass 3 verification record

This is a running evidence record, not a declaration that the expansion is done.
The expansion gate and remaining acceptance tests are in
[the implementation plan](IMPLEMENTATION_PASS_3.md).

## Milestone 0 — corrected calibration

Starting commit: `cd534cc86ae77798e00a125d384b3c9f3601353b`, branch `main`.
The only initial untracked file was the user-authored
`Dullest_Dungeon_Expansion_Research.md`; its contents have been preserved.
No external commits have been observed through `5006663`.

The live repository contained seventeen Pass 2.5 commits beyond the supplied
Pass 2 SHA. They already corrected cluster transfers, Flooded displacement and
Core/objective navigation. They did not contain a verified natural clear.

### Executed checks

| Check | Observed result |
| --- | --- |
| Initial discovery | 202 tests collected, content schema 20, save 26 |
| Initial complete-suite attempts | Incomplete: tutorial input hang, interrupted; not counted as passing |
| Policy tests | 5 passed in 8.509 s |
| Casualty/Flooded/policy checks | 8 passed in 11.481 s; casualty regression failed before correction |
| Menu UI checks excluding reproduced tutorial hang | 65 passed in 29.186 s; new long-menu regression failed before correction |
| Tutorial repair | 3 passed in 1.815 s |
| Canonical census | 2 passed in 0.028 s; census CLI completed |
| Real PTY fixture | 3 passed in 3.169 s: 80×24, 140×60, resize during inspection; snapshots unchanged |
| Warning-enabled compilation and whitespace | Passed for each source change |
| Corrected complete suite | 210 passed in 780.906 s at the tutorial-fix source state; the five subsequently added census/PTY tests also passed separately |

The tutorial failure came from ordinary-objective waypoint priority replacing
its authored training contact. The casualty failure came from moving a rear
survivor into a rank beyond the shorter living formation. Both were reproduced
before changing their respective production behavior. No combat coefficients
were adjusted during these corrections.

### Corrected normal-rule cohort

Executed seeds 0–9 × three squads × three route policies, 90 runs. Squads were
Bulkhead Basics, Breach Protocol and Unstable Research; policies were rusher,
explorer and greedy. Every 23 commands, the runner loaded a serialized snapshot
and checked its canonical state hash. There was no HP, energy, reward, inventory,
objective or route injection. [The retained cohort](evidence/pass3/calibration-cohort.json)
contains each outcome, resources, crew, encounters and final hash.

| Route policy | Wins / runs | Travel ticks min / median / max | Median total combat rounds |
| --- | --- | --- | --- |
| Rusher | 29 / 30 | 132 / 189 / 317 | 29.5 |
| Explorer | 28 / 30 | 165 / 248.5 / 344 | 25.5 |
| Greedy | 30 / 30 | 255 / 341.5 / 589 | 23 |

These policies share a myopic combat valuation. They are correlated regression
instruments, not independent players or a representative win-rate sample. They
do not inspect future draw order or random state. The greedy policy takes
reachable power detours but declines bargains; it is not literally every
possible detour. Its clean sample is a pressure-system hypothesis to challenge,
not proof of universal dominance. Some policy labels differ only modestly in
combat valuation; they are not seven proven independent playstyles.

All eleven biomes and five layouts appeared. The additional corrected seed-19
Unstable Research rusher clear covers fracture, the sixth layout. In the 90-run
cohort, 63 runs ended with four survivors, 22 with three, two with two, and three
with a wipe. The earlier fifteen-run pilot is superseded: its seed-19 loss
depended on the casualty rank bug and is not fair-defeat evidence.

Total recorded policy execution time was 172.833 seconds, excluding initial
world creation. Fight rounds min/median/max: 217 normal contacts 2/4/13;
62 elites 4/7/14; 21 objective contacts 3/4/10; 90 bosses 6/10/17. These are
observations, not enforced duration limits. The current content census retains
25 crew, 190 techniques, six curse cards, 70 enemies and 109 templates. Ten
non-derelict biomes have only 3–4 normal enemies and no elite-only native
definitions. Sixty-seven enemies have two actions, three have three. There is
one shared finale and no distinct native guardian. These are semantic expansion
gaps; listing the same Core as compatible with eleven biomes does not fill them.

Retained reproducible narratives:

- **Seed 2, Bulkhead Basics explorer, defeat:** reactor contact injured the
  Warden, then a four-body elite killed the Warden in round five. The remaining
  crew finished that fight. They reached the Core with Medic 1 HP, Engineer 16,
  Scout 24 and no supplies. The repair support sustained the Core; the Medic
  died in boss round two, Engineer in six, Scout in eight. Earlier healing,
  protection and avoiding the elite are available decisions. This is a fair
  attrition example, not an unavoidable-loss claim.
- **Seed 4, Bulkhead Basics rusher, defeat:** a coordinated Reactor elite killed
  the Warden; the other three continued through three more normal encounters.
  At the Core the party had Medic 6 HP, Engineer 28, Scout 13. They removed the
  support and reduced the Core to 31 HP, then repeated Purge Protocol phases
  finished them in round fourteen. Two unused supplies show a policy weakness:
  its pre-combat healing threshold was too low for this attrition state.
- **Seed 0, Bulkhead Basics explorer, victory:** two casualties still left a
  legal, playable deck and a clear. The full command/hash record is retained.
- **Seed 0, Breach Protocol greedy, victory:** 530 travel ticks and a casualty
  did not stop the eventual clear. This illustrates the current low cost of
  long power detours; the expansion must measure its new pressure tradeoff.
- **Seed 19, Unstable Research rusher, victory after correction:** Foundryman
  died before the Core. Cryonaut, Reactor Saint and Voidwalker won in fourteen
  boss rounds, ending at 31, 9 and 22 HP respectively. The prior bug-dependent
  loss is withdrawn, rather than being retained to satisfy a quota.

Compressed JSON transcripts for these examples are under `docs/evidence/pass3/`.
They are ordinary JSON compressed with deterministic gzip timestamps. Reproduce
a cohort member on its recorded engine commit with:

```sh
python3 -m dumbest_dungeon.policies --seed 2 --squad bulkhead_basics --policy explorer --checkpoint-every 23 --output /tmp/run.json
python3 -m dumbest_dungeon.content_audit > /tmp/census.json
```

### Actual terminal expedition

Seed 44, Breach Protocol, Pelagic Grave/ring, 80×24 actual curses PTY. Normal
new-game selection, keyboard routing, ordinary inventory and enemies. The run
completed the Mycelial culture approach and Archive forgery approach, fought
the Archive ghost pair in three rounds, and the Core in eight rounds. It spent
233 weighted ticks over 209 physical travel steps. A route event granted Flare
Phosphor; the combat reward granted Ion Lance. A flare and curse treatment spent
supplies, and an incidental cache replenished supplies en route. Final light
64, supplies zero, all four alive at 41/32/26/18 HP. The terminal displayed
`EVACUATION COMPLETE` for seed 44.

Travel and the first fight were keyboard decisions. From Core round two onward,
the same disclosed myopic policy assisted card selection and converted legal
commands to keyboard input. The real UI still executed every action, target and
enemy phase. Successive saved states matched the ordinary-engine continuation
hashes for the checked rounds. No save was edited or injected. The retained
pre-final-play snapshot allows checkpoint replay; it is not falsely labeled a
post-victory save. The existing ending has no save button.

The PTY process lasted approximately nineteen minutes including concurrent
research and test work. That is not a controlled human play-duration study.
**[Inference]** A median explorer's 25.5 combat rounds at 45–65 seconds of reading
and deciding per round, plus 10–15 minutes of routing, drafting and inspection,
gives approximately 29–43 minutes. This makes the requested base path plausible,
not measured or guaranteed. The short seed-44 route also shows why adding all
eleven guardians to a base run would be the wrong pacing response.

### Current limitations

There is no pressure director, detailed source telemetry, profile or loop system
yet. Damage attribution, actual offer/play rates across full cohorts, intent
error distributions and human fun claims cannot be verified from these initial
reports. Scenario/PTY fixtures are explicitly constructed. The current census
reports structural similarity and rank access, not semantic equivalence or
actual clogging. The larger expansion must add and measure those systems.

## Foundation verification in progress

The repeated warning-enabled full suite at the calibration boundary passed
**215 tests in 730.198 seconds** (`/tmp/dullest-pass3-m0-215-suite.log`). This run
preceded the strict JSON, atomic-save and immutable-runtime commits; it does not
claim to cover those later changes. Strict input's five tests and the 25 existing
content tests passed separately. Atomic storage passed all **22 save tests in
7.501 seconds**, including simulated file-sync/replacement failure and retention
of the previous save. Warning-enabled compilation and `git diff --check` passed.

An initial immutable-content focused command used two nonexistent test names
and reported two discovery errors, with the other two tests passing. The names
were corrected; this was an invocation error, not a suppressed test failure.
The corrected command passed **37 tests in 8.365 seconds**, covering content,
runtime contracts, altered-balance scenarios and normal-command replay. The
extended strict-input cases passed **5 tests in 0.345 seconds**. Manifest tests
passed **3 tests in 0.142 seconds**, including order invariance and sensitivity
to meaningful effect/balance changes.

### External reference update

On rechecking refs during the schema-27 migration, `origin/main` had advanced
from `cd534cc86ae77798e00a125d384b3c9f3601353b` to
`7e186436ec317154baeb8ae0206c318c3377e8c2`. The local remote-tracking reflog records
`update by push` at **2026-09-10 01:13:59 +0800**. This was an external action;
the implementing agent issued no push. The referenced commit was already an
ancestor of local `main`; no new external commit, divergence or overlapping edit
was introduced. No history was rewritten and the reference was left untouched.

Schema-27 migration, save and policy regression checks passed **30 tests in
16.137 seconds**. The retained version-26 terminal fixture migrates without
changing durable game/RNG state and reaches victory with the same legal commands
before and after another save/load boundary. Unknown versions and altered or
missing manifests are rejected. This currently supports the recorded schema-26,
content-20 baseline, not arbitrary historical content.

The derived synergy/tag/content/migration group passed **33 tests in 0.661s**.
The current graph derives **1,005 edges** across base and upgrade layers. It
reports Static Choir's orphan mark setup and Wound Ward's Biologist-dependent
marked payoff, alongside thin wound producer pools; these are design diagnostics,
not automatic rejection of the calibrated starting catalog.

Ledger serialization/replay passed **11 tests in 10.589s**; decision instrumentation
passed **11 in 15.639s**; the arithmetic/replay group passed **13 in 13.771s** and
its expanded focused arithmetic tests passed **8 in 2.843s**. World-decision and
replay instrumentation passed **13 in 16.526s**, followed by **9 focused tests in
3.158s**. History plus atomic-file tests passed **8 in 1.138s**. These are successive
overlapping test groups, not additive unique-suite totals.

The expanded real-PTY group passed **5 tests in 3.098s**, including morgue scrolling
at 80×24 and 140×60 and inspection resize without a simulation change. A fake-screen
history test initially exhausted its input because it supplied scroll keys to a
short notice; the owning test process was interrupted and the fixture changed to
contain an actual long history. No gameplay code was altered to accommodate it.

The initially untracked user-supplied `Dullest_Dungeon_Expansion_Research.md` was
preserved verbatim in a dedicated commit. Its SHA-256 remains
`363b2ac5e0be10a0feefaca1715abe71e3db842c83aaa504c170dd68d0eb1802`.
A pre-commit `git diff --no-index --check /dev/null` reports its existing final
blank line at line 371. That original whitespace was retained to honor the
instruction not to alter user work. Agent-authored changes continue to pass
`git diff --check`; the supplied artifact is the documented provenance exception.

The first full foundation suite completed **255 tests in 608.328s: 254 passed,
one error**. The failing objective-combat scenario mutated a nested mission through
a local alias. It now copies that mission and installs an isolated catalog fixture;
the scenario and assertions are unchanged. Its focused rerun, together with
immutable-content, acquisition, trigger and stack contracts, passed **17 tests in
0.965s**. A complete rerun is still required before declaring this gate passed.

The corrected full foundation rerun passed **271 tests in 579.461 seconds** with
`PYTHONWARNINGS=error python3 -m unittest discover -s tests -v`
(`/tmp/dullest-pass3-foundations-final-suite.log`). This includes the real PTYs,
legacy transcript replay, cross-process hash seeds, strict content, migration,
history and contract tests collected before the standalone queue component.
The queue and trigger component group then passed **15 tests in 0.026s**, including
phase-by-phase checkpoint continuation, listener mutation, limited cycles and
sealing while independent automatic healing and mandatory cleanup finish.
The queue is not yet wired into live combat; this is component evidence.

Queue-save integration passed **45 tests in 8.932s**, followed by **3 focused
save/reference tests in 1.087s**. Primary queue integration initially produced two
natural-replay errors when later compound-card effects referenced a now-dead
target. The resolver now retains actor identity but filters dead targets at
dispatch. The corrected replay/migration/telemetry group passed **20 tests in
23.585s**. The all-card/all-enemy opcode scenarios and queue-save group passed
**5 tests in 159.386s**; they did not expose the dead-target case that the actual
command transcripts caught. A dedicated compound-target regression was added.

Queue payload migration 29→30 passed **18 tests in 2.192s**, warning-enabled
compilation, content validation and `git diff --check`. An initial validator
invocation used the nonexistent `dumbest_dungeon.validate` module; the documented
`python3 -m dumbest_dungeon --validate-content` command then passed.

The first queued-riposte group passed 31/32 tests; the constructed checkpoint test
had overlooked its biome's starting block, so its one-damage hit was absorbed and
correctly produced no riposte. The fixture now clears that block explicitly. The
expanded corrected group passed **36 tests in 13.513s**
(`/tmp/dullest-pass3-riposte-fixed.log`), including actual guarded targets,
proc-family exclusion, phase checkpoint continuation, casualty handling,
animation neutrality and the complete legacy command transcript. Warning-enabled
compilation, full content validation and `git diff --check` passed.

Combat-resolution inspection passed **17 PTY, queue-save and ordering tests in
11.308s**, including actual scrollable curses screens at 80×24 and 140×60. The
source-arithmetic and telemetry group passed **10 tests in 3.508s**. Damage records
now distinguish target modifiers, block and first-hit deflection instead of
combining those stages. Warning-enabled compilation, content validation and
`git diff --check` passed. These PTYs use constructed combat fixtures.

Deferred continuation and save migration 30→31 passed **21 tests in 2.851s**,
including checkpointing every dispatch step while a continuation waits for an
echo descendant. Warning-enabled compilation, content validation and
`git diff --check` passed. Live card sequencing is the next integration boundary.

Whole-card root integration passed **23 replay, queue-save, migration and telemetry
tests in 14.086s**. The first three constructed phase tests used Shield Rush from
an illegal starting rank and errored; the fixture now places its owner in rank 2.
The corrected phase/legacy-passive group passed **7 tests in 4.433s**. A casualty
assertion initially expected an owned Baton Strike to remain in the draw pile;
that contradicted the required owner-death removal. The assertion now checks that
only the surviving Engineer's card remains. Every dispatch boundary is restored
through the actual engine, including post-hit death and reward cleanup; no custom
callbacks are substituted for continuation. These are constructed scenarios.
The corrected complete card-boundary group passed **4 tests in 3.854s**.
Warning-enabled compilation, content validation and `git diff --check` passed.

The first draw/curse group passed 27/28 tests. The card-root assertion still
expected the earlier unqueued opening draw; it now explicitly checks an opening
`round:draw` root followed by exactly one card-play root. The corrected group,
including all six bound curses restored at every dispatch boundary, passed
**29 tests in 20.694s**. Warning-enabled compilation, content validation and
`git diff --check` passed. The legacy ordinary transcript still matches.

Reactive-passive integration initially had one fixture error in each of the
30-test and expanded 34-test groups: Mercy Circuit requires already-owned boon
tags, so changing the recipient's crew class did not make it eligible. The fixture
now acquires Second Wind, which supplies the documented medicine/defense tags,
before acquiring Mercy Circuit through the normal acquisition API. The corrected
group passed **34 tests in 19.261s**. This includes a deliberately tiny queue budget
that seals an automatic branch while the initiated card completes and its save
remains valid, plus guarded injuries, attributed healing responses, every-phase
casualty continuation, legacy replay and morgue persistence. Warning-enabled
compilation, content validation and `git diff --check` passed.

Activation-based limiter accounting passed **29 queue, card, save and ordering
tests in 14.623s**, including a once-per-turn condition that fails on one root,
activates on the next and then remains spent. The engine reports passive
nonactivations explicitly. Warning-enabled compilation, content validation and
`git diff --check` passed.

The explicit engine-0.2.0/save-32 transition passed **26 migration, manifest,
queue-save, telemetry and ordering tests in 10.925s**. Historical synthetic
fixtures now correctly carry their historical engine marker rather than today's
marker. No durable gameplay data or fingerprint is regenerated by the transition.

## Kernel full-suite gate

At `9253590a7dac994d55cf3b7bdaea2d97ea8506fb`, the complete command
`PYTHONWARNINGS=error python3 -m unittest discover -s tests -v` passed
**299 tests in 616.022 seconds**. The exact log is
`/tmp/dullest-pass3-kernel-full-suite.log`. The tested commit remained unchanged
throughout the run. Coverage includes every existing technique/enemy action,
complete card and curse dispatch checkpoints, guarded reactions and casualty
continuation, branch sealing, trigger cycles and activation limits, historical
migration/replay, three PYTHONHASHSEED processes, generation diagnostics, and
real 80×24/140×60 terminal inspection/history screens. These scenarios do not
establish expansion balance; no expansion difficulty/content has landed yet.

The pre-stack audit finds **54 existing persistent effect rows**. Eight items
have a zero-effect first copy: Capacitor Bank, Spare Magazine, Quiet Bearings,
Reward Index, Salvage Magnet, Sterile Filter, Reserve Cell and Oracle Relay.
A temporary integer-policy prototype compared stacks 0–10 and exposed nine
possible curve changes. Its all-hyperbolic assignment is not accepted for
conditional damage/recovery bonuses; these need effect-specific decisions.
Legacy content must remain available under its recorded fingerprint before live
content edits. No historical result may silently inherit new stack curves.

The archived content-20 loader passed **11 archive, manifest, migration and
ordering tests in 5.468s**. Its three files were copied byte for byte from the
calibrated bundle. SHA-256: game
`f6f33e6a33e2a94b94556fa6b4ebafc588f88e9885932b94897594cc9eff17da`, art
`d342302b8cd5208437d950e2543f70641cbd16d8b43f0eef21d1ca4dce69bfb1`, metadata
`508900d419149b738d469962c146cea25fd7b75cf1def3baa559b06b7e76fd67`.
The effective content fingerprint remains the recorded `b6b8c6fe…f5502b`.

Exact archived-manifest selection and terminal catalog synchronization passed
**13 tests in 5.809s**. A changed installed hand-size rule cannot alter the saved
five-card rules: the recorded archive is selected, while an unknown fingerprint
is rejected. Warning-enabled compilation, content validation and staged
`git diff --check` passed.

Typed persistent effects, exact unit conversion and live stack application passed
**40 content/contract/archive tests in 1.811s**. The complete UI group plus the new
contract tests passed **73 tests in 26.746s**, including stack inspection text.
A four-test focused rerun passed in 0.563s before the final punctuation adjustment,
which is covered by the 73-test group. Warning-enabled compilation, full content
validation and `git diff --check` passed. No shipped effect curve changed here.

Self-contained save rules passed **41 persistence, content, queue and ordering
tests in 26.142s**; the final error-boundary rerun passed **7 tests in 1.797s**.
New schema-33 saves embed strictly validated effective rules under their manifest
fingerprint. The exact one-version 32→33 migration references only the retained
content-20 archive. Reordered rules replay identically; changed, missing,
non-finite and unknown-opcode rules are rejected. An initial 30-test run had one
assertion failure because an error message omitted “manifest”; the corrected
message and expanded group passed. A preliminary content-21 experiment exposed
three synthetic historical fixtures carrying current rules; those fixtures now
use the actual archive. The content transition is deferred until this persistence
boundary is committed. Warning-enabled compilation, content validation and
`git diff --check` passed. No shipped stack curve changed in this commit.

Content schema 21 assigns exact linear policies to Bulkhead Laminate, Med Gel
Ampoule, Flare Phosphor, Auto Suture, Survey Relay and Deflection Foil. Counts
0/1/2/5/10/100 retain their previous results. The focused persistence, passive,
archived replay, queue and telemetry group passed **36 tests in 14.747s**. The
historical 344-command calibration replay explicitly uses its archived bundle;
current-content reordering and cross-process checks still use current content.

Opening/reserve stacks passed **43 content, card-queue, stack and ordering tests
in 22.646s**, including constructed first-copy, threshold, turn-reset and
save/load scenarios. An initial 34-test attempt had one test-import error
(`model` instead of the repository's `engine` module), corrected before the
expanded run. Warning-enabled compilation, content validation and diff checks
passed. These are mechanic checks, not natural engine-acquisition evidence.

Utility threshold revisions passed **34 tests in 5.197s**. First-copy offer,
wound and patrol effects are nonzero. Constructed Salvage Magnet acquisitions
verify that the bonus uses the pre-pickup count and does not recursively multiply
its own award. Warning-enabled compilation, content validation and diff checks
passed. Patrol range retains its disclosed four-tile minimum.

The final three legacy item curves now use exact hyperbolic basis-point policies.
Trauma Mesh, Nerve Dampener and Targeting Prism retain their 2%, 3% and 4%
first-copy effects and disclose 20%, 30% and 35% soft caps. The focused live-stack,
contract, saved-rule and archive group passed **18 tests in 7.051s**;
warning-enabled compilation, content validation and scoped diff checks passed.
The changed later-stack curves require a fresh natural-policy comparison at the
Milestone 3 gate; historical saves retain their recorded rules.

Nine linear boon effects moved from legacy numeric curves to typed count or
basis-point rules without changing results at any stack count. This includes
the values consumed by the finite Second Wind, Adrenal Coil and Mercy Circuit
responses; their queue timing and limits are unchanged. The focused passive,
live-stack, card-resolution, queue-save, saved-rule and archive group passed
**34 tests in 20.199s**. Warning-enabled compilation, content validation and
scoped diff checks passed.

The three diminishing percentage curses now use hyperbolic basis-point rules.
Their first-copy penalties are unchanged and subsequent values are monotonic
toward disclosed 45%, 50% and 60% soft caps. The passive, curse-card, stress,
saved-rule and archive group passed **20 tests in 14.899s**. Warning-enabled
compilation, content validation and scoped diff checks passed; changed later-
stack values remain subject to the Milestone 3 policy cohort.

The final four special curses now publish their sequencing, movement and
opening-vulnerability behavior as typed rules. Runtime vulnerability reads the
strongest applicable authored result, preserving the prior non-additive Lead
Feet/Brittle Guard interaction. The passive, live-stack, card-resolution,
Frayed Focus/Tremors, saved-rule and archive group passed **35 tests in 21.297s**.
Warning-enabled compilation, content validation and scoped diff checks passed.
A programmatic census reports zero legacy effects across all 18 live items,
18 boons and 18 curses.

Persistent inspection now includes a closed disclosure for every effect opcode:
timing, finite scope and whether descendants can retrigger it. The registry's
coverage equals the complete EffectKey enum, and every live definition renders
both `Trigger:` and descendant text. The passive, fake-screen, real 80×24 PTY,
saved-rule and archive group passed **22 tests in 5.779s**. Warning-enabled
compilation, content validation and scoped diff checks passed; inspection reads
records and contracts without simulation changes.

Four percentage boons now use exact hyperbolic basis-point rules. Their first
copies retain their prior values; later stacks diminish toward disclosed caps,
including Last Word's Death's Door resistance. A first focused invocation named
two nonexistent EngineTests and therefore reported **15 passes and 2 discovery
errors**; no production failure was hidden. The corrected passive, casualty,
pickup, saved-rule and archive group passed **17 tests in 4.656s**. Warning-enabled
compilation, content validation and scoped diff checks passed. Later-stack balance
remains part of the pending natural-policy comparison.

The remaining five special-rule boons now expose the exact values consumed by
their runtime rules. Quick Hands and Vigilance no longer bypass their authored
contracts; Vigilance retains its opening dodge and 0/2/4/6/8 additional-block
progression. The first constructed assertion overlooked the biome's five opening
block and produced **30 passes and 1 failure**; comparing against an identical
no-boon combat corrected the fixture without changing production behavior. The
expanded corrected group passed **31 tests in 22.536s**. Warning-enabled
compilation, content validation and scoped diff checks passed.

Ten linear curse penalties and the inert Dead Channel marker now use typed
count policies. This preserves caps for exploration taxes, opening conditions,
card costs and all five active bound-card responses; Dead Channel explicitly
returns zero while remaining an unplayable owned card. The passive, all-bound-
curse checkpoint, Frayed Focus/Tremors, saved-rule and archive group passed
**27 tests in 17.170s**. Warning-enabled compilation, content validation and
scoped diff checks passed.

Live technique generation now consumes the typed lane filter. A constructed
catalog with one normal-lane card proves that elite-only cards cannot leak into
the offer, and the ledger records the resolved lane and eligible count. The
first broad focused run exposed a historical replay change: the filter's lexical
result had replaced the archived manifest enumeration, producing **29 passes and
1 failure**. The generator now restores filtered IDs to the catalog's frozen
order. The corrected acquisition, ordering, policy, telemetry, migration and
saved-rule group passed **28 tests in 42.587s**, including the 344-command
calibration transcript and cross-process hash check. Warning-enabled compilation,
content validation and scoped diff checks passed. Current legacy cards remain
broadly eligible until curated per-owner lanes land.

## Milestone 3 gate

At `3894b90970713193fbf59be3867b9147d329d7a9`, the complete warning-enabled
suite passed **328 tests in 608.488s** (609.01s wall time). The shell wrapper
then attempted to assign zsh's read-only `status` parameter and printed an error;
the unittest log itself ended in `OK`, and the source commit stayed fixed for
the run. While it ran, the user committed deletion of the redundant prompt file
as `93f525b2ff26c327cab8e1006441d7332894c49c`; that commit is documentation-only
and descends from the tested source. This is an external/user commit, not work
claimed by the implementation agent.

Seeds 0, 2 and 4 were run with Bulkhead Basics under rusher, explorer and greedy
policies, checkpointing every 23 commands. All used ordinary commands and normal
rules; eight won and seed-4 rusher wiped at the Core after 49 combat rounds.
Explorer and greedy won that same world in 25 rounds, so no route policy was
universally best in this small sample. Greedy still won all three while spending
255–542 travel ticks, reinforcing the need for Pressure rather than establishing
dominance. The compact cohort with exact final hashes is retained at
`evidence/pass3/milestone3-policy-cohort.json`. Concurrent suite load invalidates
its CPU timings as pacing evidence. These nine correlated policy runs do not
prove human balance or fun.

## Expedition Pressure foundation

Engine 0.3.0/save 34 adds pure 33→34 migration and durable pressure state; the
migration discloses its unavailable pre-migration history instead of inventing
it. Weighted travel, committed enemy phases, completed objective stages and
facility procedures now advance Pressure exactly once. Route forecasts and
status queries are state-neutral, recent causes are bounded and validated, and
a threshold checkpoint round-trips exactly. The migration, save, manifest,
saved-rule and ordering group passed **37 tests in 13.435s** after its archived
state projection was updated for the new durable fields. The broader pressure,
engine, save, migration and ordering regression group passed **126 tests in
450.277s**. Warning-enabled compilation and diff checks passed. This foundation
does not yet spend Pressure on encounter composition or rewards; that director
work remains open.

The 80x24 HUD now names the current band and exact route Pressure alongside
light; objective and Core previews include the same route price. `T` opens a
scrollable band/threshold/forecast/cause screen from exploration or combat, and
the historical-gap disclosure is visible after migration. The focused fake-
screen and real curses group passed **79 tests in 28.606s**, including state-
neutral Pressure inspection at both 80x24 and 140x60. The pre-correction UI run
reported **76 passes and 1 assertion failure** because a newly expanded Core
route sentence no longer contained the old `light total` phrase; restoring that
useful phrase, while retaining exact Pressure, corrected the compatibility
assertion. No simulation rule changed in this UI commit.

Save schema 35/engine 0.4.0 freezes encounter-entry Pressure, module IDs and
reinforcement tickets before opening intents. Encounter start/end records retain
the frozen profile; terminal outcomes clear active director state. Migration
34→35 maps an already displayed combat to QUIET rather than reinterpreting it at
its current Pressure. The first broad fixture run passed 47 checks and exposed
**2 historical-fixture errors**: version-28/29 projections still carried fields
that did not exist in those schemas. Removing those fields from the synthetic
inputs and expecting the disclosed Pressure history gap corrected the fixtures;
the focused migration/save/queue/ordering group then passed **40 tests in
14.177s**. Strict migration checks were retained.

The first live director slice consumes the typed profile in four separate jobs:
patrol cadence/aggression, biome-hazard reach, card-reward choice count, and
frozen encounter coordination/HP/damage. The top envelopes remain +10% HP and
+5% damage; QUIET is byte-for-byte base arithmetic. The detail screen discloses
every current value and explicitly distinguishes the active combat's frozen band.
Archived content-schema-20 runs remain on QUIET semantics while still recording
Pressure. A mistyped focused test name caused **1 discovery error**, and the
first archived transcript then failed at a legal target before that legacy
guard was added. The corrected director, hazard, patrol, intent, reward, save and
ordering group passed **17 tests in 48.203s**. This slice does not yet claim the
mutation, reinforcement, threat-vector, or repetition requirements.

Content schema 22 adds 16 immutable, namespaced mutation definitions with 16
distinct registered tactical effects. Validation closes their fields, visible
ASCII markers, bands, integer amounts/priorities, biome and encounter-kind
compatibility, exclusions and references. Embedded schema-21 rules load with an
empty mutation catalog rather than installed definitions. The first broad run
reported **40 errors** because the authored stable priorities 100–160 exceeded
an unnecessarily narrow validator maximum of 99; widening the non-power ordering
field to 999 fixed the contract rather than rewriting those IDs. The corrected
content/JSON/manifest/saved-rule/archive/runtime-contract group passed **48 tests
in 2.938s**. Definitions are not counted as live behavior until selection and
all 16 effect handlers pass separately.
