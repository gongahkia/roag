# COS-V01 — isolated playtest and integration hardening

## Status and worktree

**Status: automated tooling complete; human gameplay pending.** This pass adds no
gameplay system and makes no balance/rate/capacity change.

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
