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
