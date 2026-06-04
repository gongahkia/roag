# Session Handoff

Last updated: 2026-06-04.

## Stop State

- Last implementation slice: first risk-context discard features.
- Expected tracked worktree after this implementation is committed: clean.
- Do not continue into larger local benchmarks, defense-feature expansion, or external baseline
  work unless the user starts a new work session and asks for it.

## Current State

- Branch: `main`.
- Raw Tenhou data, generated reports, and model artifacts are local-only and ignored by git.
- The core Tenhou path parses XML directories, reconstructs through decoded calls, extracts discard
  and call examples, and records parse failures with `--skip-errors`.
- Discard examples now carry active riichi seats, per-seat river count snapshots, and a true
  seat-relative discard index in addition to flattened visible counts.
- `benchmark-discard` trains and scores four baselines on one deterministic split: frequency,
  raw-count linear, shanten-aware linear, and risk-context linear.
- Benchmark JSON reports include source metadata, split settings, model metadata, shanten summaries,
  ablation lift over raw counts and risk context, and held-out `eval_analysis` by shanten impact,
  tile family, rough round event phase, and seat-relative turn phase.

## Working Rules

- Start every session with `git status --short` and inspect current files before relying on this
  document.
- Make small commits for each coherent change. Update `IDEAS.md` with progress, benchmark results,
  and next targets as work lands.
- Do not revert or overwrite user changes. If the worktree is dirty, distinguish current-task files
  from unrelated edits before patching.
- Keep raw Tenhou XML, SQLite databases, generated reports, model artifacts, and bytecode out of
  git. Use ignored paths under `data/raw/`, `runs/`, and `models/`.
- Live ladder automation remains out of scope unless a platform gives explicit permission. The
  project is a replay-analysis and research toolkit.
- Prefer simple, tested, dependency-free code until there is a clear reason to add heavier ML
  dependencies.

## Implicit Assumptions

- The local benchmark metrics below are aggregate-only notes from ignored local Tenhou data. They
  are not reproducibility proof unless the same local sample exists or is regenerated with the
  runbook.
- `benchmark-discard` comparisons are only comparable when the split seed, eval fraction, epochs,
  learning rate, and data slice are held fixed.
- `eval_analysis.by_shanten_delta` is based on the actual supervised discard action, not on the
  model's predicted discard.
- The original rough phase buckets still use `DiscardExample.event_index`. The newer
  `by_seat_turn_phase` buckets use the discarding player's own discard index: 0-5 early, 6-11
  middle, and 12+ late.
- `ReconstructionState.visible_tiles()` currently flattens dora indicators, all visible discards,
  visible meld tiles, and the perspective player's hand into one count vector. Per-seat river count
  snapshots now exist separately, but they do not preserve river order or separate meld ownership.
- `ReconstructionState.apply_reach()` treats `TenhouReach step=1` as an active riichi declaration.
  `step=2` is treated as payment/score metadata and does not create a second declaration.
- `discard-linear-risk-context-v0` is a first coarse risk model. It uses active-riichi flags,
  candidate counts in active-riichi/opponent/self/all rivers, and an active-riichi unseen-count
  feature. It does not yet model suji, kabe, one-chance, discard order after riichi, ippatsu, or
  explicit opponent tenpai estimates.
- `DiscardLinearModel` uses explicit feature profiles. Keep `discard-linear-raw-count-v0`,
  `discard-linear-v1`, and `discard-linear-risk-context-v0` stable; add new model kinds rather than
  silently changing an existing profile.

## Local Artifacts

Ignored local paths currently used:

```bash
data/raw/tenhou/db/current-year.db
data/raw/tenhou/xml/4p-hanchan-smoke
data/raw/tenhou/xml/4p-hanchan-25
runs/
models/
```

Do not commit those paths. Record only aggregate counts and metrics in docs.

## Verification

```bash
PYTHONPATH=src python3 -m unittest discover -s tests
PYTHONPATH=src python3 -m compileall -q src tests
git diff --check
```

Useful local benchmark command:

```bash
PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/raw/tenhou/xml/4p-hanchan-25 \
  --epochs 3 \
  --eval-fraction 0.2 \
  --split-seed tenhou-25-v0 \
  --skip-errors \
  --report runs/discard-benchmark-tenhou-25-report.json \
  --source-label tenhou-4p-hanchan-25 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-25 --players 4 --length h --limit 25" \
  --source-date 2026-current-year
```

Latest documented 25-log result before the risk-context profile:

- Examples: 11,855 total, 9,484 train, 2,371 eval.
- Frequency eval accuracy: 0.3037.
- Raw-count linear eval accuracy: 0.3830.
- Shanten-aware linear eval accuracy: 0.4757.
- Shanten-aware eval lift over raw-count linear: +0.0928.
- Shanten-aware eval breakdown: 0.5091 on shanten-preserving discards, 0.0414 on
  shanten-worsening discards.
- The command above now also emits risk-context metrics, but the 25-log risk-context result has not
  been recorded yet.

## Next Tasks

1. Rerun the 25-log benchmark with the risk-context profile if the ignored local data is present,
   then record only aggregate metrics and risk-context lift in docs.
2. Inspect whether risk context improves shanten-worsening and active-riichi-adjacent decisions.
   If the current report is too coarse, add held-out buckets for active opponent riichi and
   candidate genbutsu status.
3. Preserve richer river/riichi chronology in reconstruction: discard order, riichi declaration
   turn, and whether a candidate was discarded before or after each opponent's riichi.
4. Add explicit defense features behind a new model kind: suji, kabe, one-chance/two-chance,
   sotogawa-style outside tiles, and live terminal/honor pressure.
5. Scale beyond the 25-log local slice only after the risk-context report remains stable and useful
   on the small slice.
6. Revisit external baselines after the local supervised benchmark is less fragile: build `mortal`
   locally and decide how to compare offline without live ladder automation.
