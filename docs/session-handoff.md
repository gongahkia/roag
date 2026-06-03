# Session Handoff

Last updated: 2026-06-03.

## Current State

- Branch: `main`.
- Raw Tenhou data, generated reports, and model artifacts are local-only and ignored by git.
- The core Tenhou path parses XML directories, reconstructs through decoded calls, extracts discard
  and call examples, and records parse failures with `--skip-errors`.
- `benchmark-discard` trains and scores three baselines on one deterministic split:
  frequency, raw-count linear, and shanten-aware linear.
- Benchmark JSON reports include source metadata, split settings, model metadata, shanten summaries,
  ablation lift over raw counts, and held-out `eval_analysis` by shanten impact, tile family, and
  rough round event phase.

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
2. Add features for safe tile choice and risk context: visible honor/terminal pressure, suji-style
   basics, riichi state, and opponent rivers.
3. Add a richer turn-position signal. Current `by_round_event_phase` is based on Tenhou event index;
   a seat-relative turn counter would be cleaner.
4. Scale beyond the 25-log local slice only after the error-analysis report remains stable and
   useful on this slice.
