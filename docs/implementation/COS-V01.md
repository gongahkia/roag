# COS-V01 — isolated playtest and integration hardening

## Status and worktree

**Status: headless and mocked-UI verification complete; current native-window and
human gameplay remain pending because this checkout environment has no `love`
executable.** This pass adds no gameplay system and makes no balance/rate/capacity
change.

Starting checkout: `aac4cbca6cd90d2edce6d24884929c5f11bc65ae`. It differs from the
relayed `df5b2eb`; no reset was performed. The starting tree already contained the
uncommitted contextual-HUD work in `README.md`, `main.lua`, `src/render.lua`,
`tests/knowledge_gui.lua`, `tests/education_gui.lua`, `tests/syntax.lua`, and
`src/ui/action_hud.lua`. It was preserved. This pass adds the isolated launcher,
storage guard/probe, fixture emission/loading, tests, and this guide. No commit,
push, dependency installation, real-save access, or system setting change was
performed.

Owner-side commits advanced the shared branch during this pass. Ending observed
HEAD: `0d33f55af263f1f6b8fb6d0b4fc3073d50478014`; I made no commit or push.

The bounded tested-source fingerprint is recorded by SHA-256 in the V01 handoff
command output and covers `conf.lua`, `main.lua`, `src/storage.lua`,
`src/mapstore.lua`, `src/playtest.lua`, `src/render.lua`, and `tools/playtest.sh`.

## Current verification addendum — 2026-09-21

This follow-up began at clean `317a919b84024c5253484c40b235d0c055e216cc`.
`df5b2eb..317a919b` changes the earlier reported source only through committed
documentation. The current working-tree delta is deliberately limited to this
handoff, the human guide, `README.md`, `main.lua`, `src/render.lua`,
`src/ui/action_hud.lua`, `tools/playtest.sh`, `tests/education.lua`, and
`tests/gui_smoke.lua`; no owner change was present to preserve at the start, and
no commit or push was made.

The 18-file simulation/tool/test fingerprint is
`9294287077d84944071cb73be8273a48a04e03b3736dd2d514e504037ef5f782`.
It includes the campaign, history, logistics, travel, knowledge, education,
storage, renderer, launcher, soak and P06 test seams.

The launcher fixture path no longer relies on `awk`: its Bash code reads
`MemAvailable` directly from `/proc/meminfo`. On this host the final runs used a
child-only 50% CPU quota, 1,615,897 KiB `MemoryMax`, 1,211,922 KiB `MemoryHigh`,
and a 300-second timeout. The final core suite passed in 126.83 seconds with
78,804 KiB maximum RSS. No LuaJIT or LÖVE process remained after the checks.

### Current P06 evidence matrix

All headless entries below are named groups in `tests/education.lua`; P06-P also
uses `tests/education_gui.lua`.

| ID | Current evidence and outcome |
| --- | --- |
| P06-A | Physical four-stone/two-metal construction and feature-off rejection — PASS. |
| P06-B | Local living source and ordinary Field-duty policy — PASS. |
| P06-C | 120 attended recording actions and immutable local record — PASS. |
| P06-D | Complete pair reservation and interruption release — PASS. |
| P06-E | Pre-reward XP arithmetic and ten-tick evaluation — PASS. |
| P06-F | Record study, source loss, and retained earned tuition — PASS. |
| P06-G | Communicated provenance without eyewitness copying — PASS. |
| P06-H | Actual prepare/load/assemble/launch/400-tick arrival preserves tuition — PASS. |
| P06-I | Save, replay, seek, and branch with partial school work — PASS. |
| P06-J | Unviewed education progression — PASS. |
| P06-K | No partial teacher reservation without a learner — PASS. |
| P06-L | Stale school revision/rebuild command rejection — PASS. |
| P06-M | Learner-owned tuition and school-local draft ownership — PASS. |
| P06-N | Same-tick finalizer guard — PASS. |
| P06-O | Malformed expertise and duplicate-record bounds — PASS. |
| P06-P | Revision-bound school policy UI command — PASS, MOCK UI. |
| P06-Q | Physical record then attended teaching — PASS. |
| P06-R | Per-person deterministic expertise and feature-off boundary — PASS. |
| P06-S | Repeated policy churn stays bounded — PASS. |

P06-H was strengthened in this pass. Its real traveller, person 2, kept exactly
17/200 personal tuition units from departure at tick 2,222 through destination
arrival at tick 2,621; the newly created destination worker has no local school
task. This adds four arrival assertions to the existing regression rather than
changing simulation behavior.

### Fresh controlled trace and retained artifacts

The real 20,000-tick education trace passed under that guard. Its private marked
root is `/tmp/cosmonauts-v01-final-evidence.n1C6L0Oc`; the log is
`logs/education-soak.log`, and `fixtures/index.sha256` covers its five campaign
artifacts. The root contains no normal save, credentials, or bundled font asset.

| Fixture | Tick | Bytes | Relevant verified state |
| --- | ---: | ---: | --- |
| `initial` | 40 | 3,177,845 | No tested fact, school, tuition, record, or craft cargo. |
| `partial-copy` | 1,540 | 3,199,800 | A has two facts; school 1 has a 23/120 record draft. |
| `partial-lesson` | 2,721 | 3,202,703 | B has 64/200 teaching tuition; both records exist. |
| `transit` | 7,221 | 3,203,114 | A is aboard outbound craft 1 with food=2, metal=1 after maintenance. |
| `partial-record-study` | 9,002 | 3,205,267 | C has 16/200 record tuition; A is on Moon I; two home records remain. |

A (person 1) receives identification at tick 102 and the operational fact at
tick 410, then gains fieldwork XP 12→116. School 1 consumes four stone and two
metal (home ground becomes stone=80, metal=20), records identification at tick
1,734 and the operational fact at tick 2,240, and A's teaching XP rises
145→230. B (person 2) has 64 teaching units at the partial-lesson milestone and
later receives identification at tick 3,060 and the operational fact at tick
4,370 by `taught` provenance. A departs/arrives at ticks 7,022/7,421 while B
remains at Home Planet. C (person 3) gains identification from a record at tick
8,700, has 16 record units at tick 9,002, and receives the operational fact at
tick 9,670 by `record` provenance with no copied eyewitness observations. The
final 20,000-tick history is 3,206,041 bytes, has two records/eight checkpoints,
and reports a 104,869.2 KiB Lua heap sample; all saves are below 32 MiB.

### Current V01 outcomes

| Check | Outcome |
| --- | --- |
| V01-A | PASS — the matrix above and the real P04 tuition-arrival regression run in the current core suite. |
| V01-B | HEADLESS fixture guard PASS; launcher shell syntax PASS. Fresh LÖVE probe/launcher suite NOT RUN because `love` is absent; the unchanged launcher has the earlier historical pass. |
| V01-C | PASS — normal feature-off and default mock paths remain in the 150-group core suite. |
| V01-D | PASS — five real-path, SHA-256-indexed milestones were generated below a fresh marked root. |
| V01-E | PASS — discovery, records, teaching, flight, record study, save/reload, and replay pass in the fresh trace. |
| V01-F | HEADLESS and all eight MOCK UI adapters PASS; NATIVE RENDER / SCRIPTED WINDOW and HUMAN GAMEPLAY NOT RUN. |
| V01-G | PASS — physical construction/cargo values and bounded save/state counts above reconcile. |
| V01-H | PASS — runs were serial, scoped, timed, logged below private roots, and left no task process. |
| V01-I | Guide and exact launcher commands are ready; current execution is NOT RUN until LÖVE is installed. |
| V01-J | PASS — no simulation-rule repair. Tool portability, P06-H regression coverage, Ctrl+Q quit, bounded camera panning, block-area delegation, and Windows-compatible save replacement were the narrow changes. |

Fresh commands: `luajit tests/syntax.lua` (93 files), the 19-group/57-assertion
P06 suite, all eight mocked GUI adapters, the guarded `luajit tests/run.lua`
(150 groups / 121,083 assertions), the guarded education soak, and both diff
checks all passed. Ctrl+Q now requests LÖVE's ordinary save-on-quit event from
every in-game overlay. Inspect-mode left drag selects bounded build blocks,
right-clicking inside them applies one ordinary order per block, and middle-drag
pans only a zoomed camera view; `tests/gui_smoke.lua` covers each path. `bash
tools/playtest.sh --check` correctly exited 1 with
`LÖVE runtime not found: love`; that is an environment limitation, not a passed
isolation probe. No source changed during the final guarded core run.

### Current implementation map

`src/campaign.lua`, `src/campaign_history.lua`, and `src/campaign_codec.lua` own
campaign state, timeline, and persistence. `main.lua`, `src/render.lua`, and
`src/ui/*` own session/view/input. `src/logistics.lua` and `src/travel.lua` own
physical custody and journeys. `src/knowledge.lua` and `src/education.lua` own
personal facts and field schools. `src/playtest.lua`, `src/storage.lua`,
`src/mapstore.lua`, and `tools/playtest.sh` are the isolated-playtest seam.
Culture, industry, additional ecology, physics, relic progression, and P07 remain
deferred.

### Windows save-replacement update

An actual Windows local-save exit reproduced the direct replacement failure: a
valid `run.tmp` could not be renamed over an existing `run.dat` and the game
correctly remained open. Both files were decoded before recovery; the temporary
file held tick 1,052 while the prior primary save held tick 0. The older primary
save was copied to a timestamped recovery file and the validated temporary save
was restored as the new primary. The real save location is intentionally omitted
from this shareable handoff.

`src/storage.lua` now attempts the direct same-directory rename first, retaining
POSIX atomic replacement. Where overwrite rename is unavailable, it moves the
prior target to a unique same-directory recovery name, promotes the temporary
file, then removes the recovery copy. A failed promotion restores the prior save
and reports the retained temporary state. The same helper serves local and
campaign saves; it does not alter save formats or simulation state.

`P01-I` now simulates Windows-style overwrite refusal for both save names and
verifies that the replacement loads and leaves no recovery copy after success.
Focused syntax and P01 campaign checks passed. A fresh guarded full suite passed
with 150 groups / 121,091 assertions in 79.52 seconds and 78,624 KiB maximum
RSS under a 50% CPU, 1,615,897 KiB `MemoryMax`, 1,211,922 KiB `MemoryHigh`,
300-second child scope. Its exact source state is owner commit
`c9aad533397805f4988bb63292e27727b43a368c` plus uncommitted
`src/storage.lua` and `tests/campaign.lua`; its 19-file fingerprint is
`1929e92db42d052c56cca4543872c53306438e0b56479a1341c45eb6e4caa06f`.
The owner commit occurred during this follow-up; no agent commit or push was
made.

## Isolation and launch contract

`tools/playtest.sh` creates a mode-700 `mktemp` root outside the checkout and
stores a mode-600 marker bound to the current canonical repository path. It sets
only child `XDG_DATA_HOME`, `XDG_CONFIG_HOME`, `XDG_CACHE_HOME`, and
`COSMONAUTS_PLAYTEST_ROOT`; normal shell state and LÖVE identity remain unchanged.
The actual probe used the existing `deepward_02` identity and resolved:

```text
root: /tmp/cosmonauts-v01.VOJBRRF4
save: /tmp/cosmonauts-v01.VOJBRRF4/data/love/deepward_02
probe: /tmp/cosmonauts-v01.VOJBRRF4/data/love/deepward_02/playtest-probe.txt
```

The probe disables LÖVE graphics/window modules, runs before `Store.load`, writes
only `playtest-probe.txt` through LÖVE, and verifies the effective writable adapter
root is beneath the marker root. `Store.load`/`loadCampaign` additionally resolve
an existing `run.dat`/`campaign.run.dat` origin with `getRealDirectory` and reject
an outside-root candidate before reading it. Save, map export, archive, screenshot
and diagnostic writes use LÖVE's verified save root; the ordinary Lua-I/O tools are
not runtime gameplay paths.

Exact owner commands are in [PLAYTEST_FIRST_FRONTIER.md](../PLAYTEST_FIRST_FRONTIER.md).

## P06 evidence linkage

The current source maps P06-A through P06-S one-for-one to the named groups in
`tests/education.lua`; P06-P additionally runs `tests/education_gui.lua` as MOCK
UI. P06-H includes the actual P04 prepare/load/assemble/launch passenger path;
`tools/education_soak.lua` covers the full record/teach/flight/record-study chain.
The table below records current source linkage; P06's detailed assertions remain
in that test rather than duplicated here.

| Cases | Actual evidence |
| --- | --- |
| A–D | `P06-A` construction; `P06-B` local source; `P06-C` 120 attendance; `P06-D` paired release |
| E–H | `P06-E` XP cadence; `P06-F` source loss; `P06-G` communicated provenance; `P06-H` clone and actual flight |
| I–L | `P06-I` save/replay/branch; `P06-J` unviewed work; `P06-K` no-pupil reservation; `P06-L` stale rebuild policy |
| M–P | `P06-M` learner/draft ownership; `P06-N` same-tick guard; `P06-O` bounds; `P06-P` policy command plus `tests/education_gui.lua` |
| Q–S | `P06-Q` record/teach integration; `P06-R` deterministic XP; `P06-S` allocation churn |

## Fresh controlled trace and bounds

`bash tools/playtest.sh --fixtures /tmp/cosmonauts-v01.VOJBRRF4` completed the
actual 20,000-tick scenario under a child-only 50% CPU, 2 GiB maximum / 1.5 GiB
high scope and 300-second timeout. It retained:

```text
initial                  3,177,845 bytes
partial-copy             3,199,800 bytes
partial-lesson           3,202,703 bytes
transit                  3,203,114 bytes
partial-record-study     3,205,267 bytes
final encoded history    3,206,041 bytes
records/checkpoints      2 / 8
final Lua heap           107,329.9 KiB
```

The fixture has A/B/C person IDs 1/2/3. A studies the glass-reed
steam-to-water fact at tick 580; school 1 holds two records. B learns the
operational fact by teaching at tick 4370. A departs/arrives at ticks 7022/7421,
retaining the fact and expertise while the records remain at home. C learns the
same fact from a record at tick 9670 with method `record`. The generated artifact
index is `/tmp/cosmonauts-v01.VOJBRRF4/fixtures/index.sha256`; loading
`partial-lesson` copied byte-identically to that root's empty isolated
`campaign.run.dat` and a second load was refused.

These are one controlled diagnostic trace and heap sample, not a normal-speed
performance claim or a procedural-balance result.

## V01 outcomes

| Check | Outcome |
| --- | --- |
| V01-A evidence linkage | PASS — P06 groups and actual P04 tuition transport located above. |
| V01-B isolation | PASS — probe, spaces/sentinel/invalid-marker/symlink/conflict/missing-runtime launcher tests pass. |
| V01-C default compatibility | PASS — activation is absent unless the environment root is set; syntax/core regressions exercised normal mock startup. |
| V01-D reproducible fixture | PASS — real-path milestone generation and SHA-256 index under one marked root. |
| V01-E continuity | PASS — fresh trace reaches record, teaching, flight, home record study, save/reload and replay verification. |
| V01-F commands/presentation | PASS for HEADLESS/MOCK UI; NATIVE input and HUMAN gameplay remain NOT RUN. |
| V01-G bounds/accounting | PASS — values above remain below 32 MiB; fixture uses normal construction/travel accounting. |
| V01-H process hygiene | PASS — serial scoped fixture run; no persistent LÖVE/LuaJIT child; all generated artifacts stayed below the root. |
| V01-I playthrough readiness | PASS for launcher/guide; HUMAN GAMEPLAY NOT RUN. |
| V01-J repair evidence | PASS — no pre-existing gameplay defect was claimed; the narrow repair is launch isolation before save discovery. |

Fresh final checks were `luajit tests/syntax.lua` (93 Lua files),
`tests/playtest_launcher.sh`, the full `luajit tests/run.lua` suite under the
bounded child scope (150 groups / 121,079 assertions), all existing GUI mocks,
and both diff checks. All passed. The core suite used a 300-second timeout with
50% CPU, 2 GiB `MemoryMax`, and 1.5 GiB `MemoryHigh`; it completed normally.

## Evidence categories and remaining work

`HEADLESS`: fixture generation, replay, codec, and existing suite. `MOCK UI`:
existing GUI adapters including the contextual HUD. `NATIVE RENDER / SCRIPTED
WINDOW`: fresh `--new` and marked-root `--resume` sessions opened with
DISPLAY/Wayland available. Each was bounded to a few seconds; no LÖVE process was
left running and generated cache/log data remained root-local. This was not human
input validation. `HUMAN GAMEPLAY`: **NOT RUN**.

No conclusion about the earlier host shutdown follows from this pass. The scope
used a verified child resource limit for heavy automation; that is a workload
precaution, not a diagnosis or guarantee.

The next required action is a person completing the finite checklist in
`docs/PLAYTEST_FIRST_FRONTIER.md`, recording PASS/FAIL/NOT RUN for each point.
Do not begin another feature tranche from these automated results alone.
