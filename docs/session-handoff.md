# Session Handoff

Last updated: 2026-06-03.

## Stop State

- This is a docs-only handoff clarification requested after the last implementation task.
- Last implementation slice before this clarification: `6e6657e feat: report discard error
  analysis`.
- The user explicitly asked to stop after the documentation handoff. Do not continue implementing
  risk features or larger benchmarks unless the user starts a new work session and asks for it.
- The attempted resume after the push only produced a planning/status message. It did not run repo
  commands or edit files.

## Current State

- Branch: `main`.
- Expected tracked worktree at handoff: clean after this docs-only update is committed.
- Raw Tenhou data, generated reports, and model artifacts are local-only and ignored by git.
- The core Tenhou path parses XML directories, reconstructs through decoded calls, extracts discard
  and call examples, and records parse failures with `--skip-errors`.
- `benchmark-discard` trains and scores three baselines on one deterministic split:
  frequency, raw-count linear, and shanten-aware linear.
- Benchmark JSON reports include source metadata, split settings, model metadata, shanten summaries,
  ablation lift over raw counts, and held-out `eval_analysis` by shanten impact, tile family, and
  rough round event phase.

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
- The current rough phase buckets use `DiscardExample.event_index`, not a seat-relative turn count.
  This is useful for a first pass but should not be treated as a precise turn-position feature.
- `ReconstructionState.visible_tiles()` currently flattens dora indicators, all visible discards,
  visible meld tiles, and the perspective player's hand into one count vector. It does not preserve
  separate opponent rivers, separate meld ownership, or riichi state.
- `TenhouReach` events are parsed in `src/kenjaku/io/tenhou_xml.py`, but `DiscardExample` does not
  yet expose active riichi declarations. Confirm Tenhou `REACH step=1` and `step=2` semantics with
  tests before using them as model features.
- `DiscardLinearModel` uses explicit feature profiles. Adding risk/context features should use a
  new profile and model kind rather than silently changing `discard-linear-v1`.

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

Latest 25-log result:

- Examples: 11,855 total, 9,484 train, 2,371 eval.
- Frequency eval accuracy: 0.3037.
- Raw-count linear eval accuracy: 0.3830.
- Shanten-aware linear eval accuracy: 0.4757.
- Shanten-aware eval lift over raw-count linear: +0.0928.
- Shanten-aware eval breakdown: 0.5091 on shanten-preserving discards, 0.0414 on
  shanten-worsening discards.

## Next Tasks

1. Investigate shanten-worsening decisions. They are rare but currently poorly predicted by the
   shanten-aware model, so the model may be over-rewarding efficiency preservation.
2. Add a first risk-context data slice before changing the model. Start in
   `src/kenjaku/training/discard_examples.py` and `src/kenjaku/training/reconstruction.py` by
   carrying active riichi state and opponent river context into `DiscardExample`.
3. Add targeted synthetic Tenhou fixtures/tests for reach state and opponent-river feature
   extraction. Current parser tests already cover `TenhouReach`; the missing piece is reconstructed
   training context.
4. After context fields are tested, add a new linear feature profile for risk features and keep
   `raw-count` and `shanten` profiles as ablation anchors.
5. Add a richer turn-position signal. Current `by_round_event_phase` is based on Tenhou event index;
   a seat-relative turn counter would be cleaner.
6. Scale beyond the 25-log local slice only after the error-analysis report remains stable and
   useful on this slice.
