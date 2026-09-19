# COS-P01 implementation handoff

## Scope delivered

Implemented the headless campaign foundation above the legacy local simulator:
strict one-site campaign state, stable `personId` assignment, typed site command
envelopes, staged campaign ticks, campaign-wide archive/history, canonical campaign
save/load, explicit bounded replay verification, and campaign acceptance coverage.
No travel, second settlement, campaign UI, map-schema migration, legacy-save import,
or real-save migration is included.

Starting Git state: `4f607699d0263d3c109ad7ceb81c41f5124a2b77` with unrelated owner
untracked design/reference files. During this work the checkout advanced to
`f4f175b54946e4da81b9e60f039d886bdf2b82cb`; no `git commit` command was issued
by this implementation pass. The final tree must be reviewed as an uncommitted
working tree rather than represented as a clean committed revision.

## Source changes

- `src/sim.lua`: exposes `begin` and `body`; `step` preserves the prior local order.
- `src/world.lua`: validates an optional campaign marker and marker-gated person IDs.
- `src/campaign.lua`: strict campaign construction/validation, IDs, site ordering,
  campaign extinction, and staged stepping.
- `src/campaign_commands.lua`: strict `{scope='site',siteId,payload}` routing.
- `src/campaign_history.lua`: global command log, checkpoint/archive behavior,
  tagged bundle, and bounded replay verification.
- `src/campaign_codec.lua`: 32 MiB campaign codec boundary.
- `src/storage.lua`: isolated `campaign.run.tmp`/`campaign.run.dat` save adapter.
- `tests/campaign.lua`, `tests/all.lua`, `tests/syntax.lua`: P01-A through P01-J.
- `tools/campaign_benchmark.lua`: isolated headless CPU/persistence diagnostic.
- `docs/FRONTIER_CONTRACT.md` and `README.md`: campaign contract/current boundary.

The campaign envelope is `cosmonauts-campaign` v1; its state is
`cosmonauts-campaign-state` v1 with ruleset `frontier-campaign-v1` and
`features={core=1}`. Legacy local save/history format, `deepward_02`,
`deepward-map`, and map schemas 1/2 are unchanged.

## Verification

Baseline before changes, LuaJIT 2.1.1767980792:

```text
luajit tests/syntax.lua             PASS (64 files)
luajit tests/run.lua                PASS (66 groups; 113,663 assertions)
luajit tests/benchmark_smoke.lua    PASS
```

P01 focused run after changes:

```text
luajit tests/syntax.lua
luajit -e "require('tests.campaign').run()"
PASS: 70 files; P01-A through P01-J; 4,851 assertions
```

`luajit tools/campaign_benchmark.lua 2000` creates matching seeded 128×80 worlds,
warms both paths, alternates seven local/campaign samples, and reports 2,000
headless-tick plus persistence CPU timings. It excludes construction, renderer/GPU,
filesystem replacement, and RSS. The samples varied widely across repeated isolated
runs: one run reported a 91.7% campaign regression and triggered the 15% diagnostic
message; later runs reported −3.7% and −72.6% medians with bimodal raw samples. The
implementation pass investigated the wrapper (cached site ordering and no-command
fast path) and verified deterministic state equality, but **cannot verify a stable
performance delta on this host**. The benchmark prints all raw samples; repeat it on
a controlled target machine before accepting or rejecting a 15% regression. This is
CPU-only diagnostic evidence, not FPS, GPU, RSS, or cross-machine evidence.

Final post-change checks passed: `tests/run.lua` (76 groups; 118,514 assertions),
`tests/benchmark_smoke.lua`, all three mocked GUI adapters, the 2,000-tick headless
trace, six-case 600-tick expansion soak, and `tests/maximum_size.lua`. Mock save
tests use `/tmp/cosmonauts-p01-campaign-storage` and leave legacy `run.dat` as a
checked sentinel; no real LÖVE save path was used.

## Limits and follow-up boundary

Ordinary campaign load performs structural validation only. It deliberately does
not replay arbitrary-length histories; callers use the budgeted verification API to
prove replay equality. The current UI continues to load/save only legacy local runs.
Campaign persistence is callable through `Store.saveCampaign`/`loadCampaign` but is
not exposed as a new menu path. Stop at P01; later multi-site/travel work must update
the campaign feature/ruleset contract explicitly.
