# Session Handoff

Last updated: 2026-06-05.

## Stop State

- Last work slice: added benchmark-report summaries, L2 benchmark wiring, linear weight/feature
  diagnostics, model-disagreement exports, and 100-log learning-rate/L2 sweeps.
- Expected tracked worktree after this implementation is committed and pushed: clean.
- Do not promote `discard-linear-defense-context-v1` as the default path yet. Lowering learning
  rate fixed the largest aggregate regression, but v1 still trails risk/defense v0 on the 100-log
  slice.

## Current State

- Branch: `main`.
- Raw Tenhou data, generated reports, disagreement exports, model artifacts, external checkouts,
  and Mortal build outputs are local-only and ignored by git.
- `benchmark-discard` trains and scores six baselines on one deterministic split: frequency,
  raw-count linear, shanten-aware linear, risk-context linear, defense-context linear, and
  defense-context-v1 linear.
- `benchmark-discard --l2` applies L2 regularization to every linear model in the benchmark and
  records it in each training block.
- Benchmark reports now include `weight_summary` and `feature_summary` for every linear model.
- `benchmark-discard --disagreements PATH` writes local-only capped examples where risk-context and
  defense-context predictions disagree, including defense buckets and legal-candidate logits.
- `benchmark-report-summary` reads ignored benchmark reports and prints comparable model metrics,
  ablation deltas, and selected defense buckets; `--json` emits a machine-readable summary.
- Local Mortal checkout/build reconnaissance is recorded in `docs/external-baselines.md`.

## Working Rules

- Start every session with `git status --short` and inspect current files before relying on this
  document.
- Make small commits for each coherent change. Update `IDEAS.md` with progress, benchmark results,
  and next targets as work lands.
- Do not revert or overwrite user changes. If the worktree is dirty, distinguish current-task files
  from unrelated edits before patching.
- Keep raw Tenhou XML, SQLite databases, generated reports, disagreement exports, model artifacts,
  external checkouts, build outputs, and bytecode out of git. Use ignored paths under `data/raw/`,
  `runs/`, and `models/`.
- Live ladder automation remains out of scope unless a platform gives explicit permission. The
  project is a replay-analysis and research toolkit.
- Prefer simple, tested, dependency-free code until there is a clear reason to add heavier ML
  dependencies.

## Implicit Assumptions

- Local benchmark metrics are aggregate-only notes from ignored local Tenhou data. They are not
  reproducibility proof unless the same local sample exists or is regenerated with the runbook.
- `benchmark-discard` comparisons are only comparable when the split seed, eval fraction, epochs,
  learning rate, L2, and data slice are held fixed.
- `eval_analysis.by_shanten_delta` is based on the actual supervised discard action, not on the
  model's predicted discard.
- `by_round_event_phase` uses `DiscardExample.event_index`. `by_seat_turn_phase` uses the
  discarding player's own discard index: 0-5 early, 6-11 middle, and 12+ late.
- `discard-linear-risk-context-v0` remains a strong aggregate anchor on the 100-log slice.
- `discard-linear-defense-context-v0` ties risk-context aggregate accuracy at `lr=0.05, l2=0.0`
  and has stronger targeted defense buckets, but it is not clearly better enough to replace the
  risk anchor.
- `discard-linear-defense-context-v1` is still exploratory. It adds finer active-opponent fractions,
  sotogawa-style outside tiles, terminal/honor live pressure, dora/indicator flags, ippatsu timing,
  last-tsumogiri-after-riichi context, and opponent meld-tile pressure, but still trails v0 in
  aggregate on the 100-log slice.
- `DiscardLinearModel` uses explicit feature profiles. Keep `discard-linear-raw-count-v0`,
  `discard-linear-v1`, `discard-linear-risk-context-v0`, `discard-linear-defense-context-v0`, and
  `discard-linear-defense-context-v1` stable; add new model kinds rather than silently changing an
  existing profile.
- Mortal is AGPL-3.0-or-later. Keep any future comparison behind a neutral data/subprocess boundary
  unless the project intentionally accepts that license boundary.

## Local Artifacts

Expected ignored local paths when local Tenhou data and external baselines are available:

```bash
data/raw/tenhou/db/current-year.db
data/raw/tenhou/xml/4p-hanchan-25
data/raw/tenhou/xml/4p-hanchan-100
data/raw/external/mortal
runs/discard-benchmark-tenhou-25-report-v1.json
runs/discard-benchmark-tenhou-100-lr0.1-l2-0-report.json
runs/discard-benchmark-tenhou-100-lr0.1-l2-0.0001-report.json
runs/discard-benchmark-tenhou-100-lr0.1-l2-0.001-report.json
runs/discard-benchmark-tenhou-100-lr0.05-l2-0-report.json
runs/discard-benchmark-tenhou-100-lr0.05-l2-0.0001-report.json
runs/discard-disagreements-tenhou-100-lr0.1-l2-0.json
runs/inspect-tenhou-25-report.json
runs/inspect-tenhou-100-report.json
runs/
models/
```

Do not commit those paths. Record only aggregate counts and metrics in docs.

## Verification

```bash
PYTHONPATH=src python3 -m unittest discover -s tests
PYTHONPATH=src python3 -m compileall -q src tests
PYTHONPATH=src python3 -m kenjaku benchmark-discard data/fixtures/tenhou \
  --epochs 1 --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --l2 0.0001 --report runs/fixture-benchmark-diagnostics.json \
  --disagreements runs/fixture-disagreements.json
PYTHONPATH=src python3 -m kenjaku benchmark-report-summary runs/fixture-benchmark-diagnostics.json
git diff --check
```

Useful local benchmark command:

```bash
PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --epochs 3 \
  --learning-rate 0.05 \
  --l2 0.0 \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --skip-errors \
  --report runs/discard-benchmark-tenhou-100-lr0.05-l2-0-report.json \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year
```

## Latest Benchmarks

25-log local result, split `tenhou-25-v0`, `lr=0.1`, `l2=0.0`:

- Dataset: 25 XML files, 255 rounds, 11,855 discard examples, 3,185 call examples, zero parse
  failures; 9,484 train / 2,371 eval.
- Eval accuracy: frequency 0.3037, raw-count linear 0.3830, shanten-aware linear 0.4757,
  risk-context linear 0.4829, defense-context linear 0.4825, defense-context-v1 linear 0.4884.
- Defense-context-v1 lift: +0.0059 over defense-context and about +0.0055 over risk-context.

100-log local sweep, split `tenhou-100-v0`, 3 epochs, 40,221 train / 10,055 eval:

| Learning rate | L2 | Risk eval | Defense eval | V1 eval | Takeaway |
|---:|---:|---:|---:|---:|---|
| 0.10 | 0.0 | 0.4919 | 0.4881 | 0.4840 | Original regression. |
| 0.10 | 0.0001 | 0.4892 | 0.4853 | 0.4812 | Small L2 hurt. |
| 0.10 | 0.001 | 0.4707 | 0.4669 | 0.4630 | Strong L2 badly hurt. |
| 0.05 | 0.0 | 0.5124 | 0.5124 | 0.5102 | Best aggregate result. |
| 0.05 | 0.0001 | 0.5113 | 0.5093 | 0.5086 | L2 still hurt. |

Best 100-log selected buckets at `lr=0.05`, `l2=0.0`, risk -> defense -> v1:

- Active-riichi: 0.5252 -> 0.5181 -> 0.5228 on 1,687 examples.
- Actual genbutsu: 0.6129 -> 0.6154 -> 0.6179 on 793 examples.
- Actual suji: 0.3686 -> 0.4278 -> 0.4227 on 388 examples.
- Actual kabe: 0.4962 -> 0.5166 -> 0.5192 on 391 examples.
- One-chance: 0.4730 -> 0.4950 -> 0.4950 on 1,091 examples.
- Seen after riichi: 0.5401 -> 0.5875 -> 0.5935 on 337 examples.
- Shanten-worsening: 0.0977 -> 0.1015 -> 0.1157 on 778 examples.

Diagnostics from ignored artifacts:

- Baseline disagreement counts at `lr=0.1`, `l2=0.0`: risk-correct/defense-wrong 268,
  risk-correct/v1-wrong 358, defense-correct/risk-wrong 230, v1-correct/risk-wrong 279.
- At `lr=0.05`, `l2=0.0`, selected v1-only feature activation rates on eval candidates:
  sotogawa 0.0462, dora 0.0033, active ippatsu fraction 0.0369, active tsumogiri fraction 0.1244,
  opponent meld tile fraction 0.4234.
- Feature summaries are identical across LR/L2 runs for a fixed profile and split; weight summaries
  change with training settings.

Mortal local baseline reconnaissance:

- Ignored checkout: `data/raw/external/mortal`.
- Commit inspected: `0cff2b52982be5b1163aa9a62fb01f03ce91e0d2`.
- `cargo build -p libriichi --lib --release` passed locally in 37.70s.
- `cargo test --workspace --no-default-features --features flate2/zlib -- --nocapture` passed:
  28 Rust unit tests plus doctests reported OK.
- `cp target/release/libriichi.dylib mortal/libriichi.so` followed by
  `PYTHONPATH=mortal python3 -c "import libriichi"` succeeded.

## Next Tasks

1. Make `lr=0.05` the documented default for local discard benchmark experiments, but do not change
   the CLI default yet unless a broader compatibility decision is made.
2. Inspect the captured disagreement examples to decide whether defense v1 should be simplified
   before adding any new deterministic features.
3. Add feature normalization behind a new model kind if the next goal is improving linear-model
   stability; do not mutate existing feature profiles.
4. Add a compact `--profiles` or `--models` benchmark option if full six-model diagnostics remain
   too slow for repeated sweeps.
5. Define an offline Mortal comparison boundary: start with a Tenhou XML to `mjai` decision-snapshot
   exporter, then compare Kenjaku decisions to a Mortal-compatible inference path only when weights
   are available and legally usable.
6. Add first supervised call/riichi decision baselines from the existing call examples once discard
   risk/defense diagnostics stop moving.
