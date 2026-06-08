# Session Handoff

Last updated: 2026-06-08.

## Stop State

- Last work slice: refreshed README/docs, narrowed Python support to `>=3.11,<3.14`, added
  PyTorch as a core dependency, added GitHub Actions CI, fixed balanced call limiting to
  deterministic call/pass interleaving, added stub decision prediction production, added opt-in
  outcome labels, added a minimal PyTorch discard MLP, and reran local aggregate call/riichi checks
  on the ignored 500-log Tenhou slice.
- Current work slice: added `benchmark-call --example-cache` for local-only reconstructed
  `CallExample` JSON caching before feature preparation. Cache keys include command input paths,
  resolved XML files, each file's size/mtime, and `--skip-errors`; source metadata intentionally
  does not invalidate the cache. Call reports now include an additive `example_cache` block.
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
- `benchmark-discard --models` accepts `all`, `fast`, or comma-separated model names. `all` is the
  default; `fast` runs frequency, shanten-aware, risk-context, and defense-context only.
- `benchmark-discard --l2` applies L2 regularization to every linear model in the benchmark and
  records it in each training block.
- Benchmark reports now include `weight_summary` and `feature_summary` for every linear model.
- `benchmark-discard --disagreements PATH` writes local-only capped examples where risk-context and
  defense-context predictions disagree, including defense buckets and legal-candidate logits.
- `benchmark-report-summary` reads ignored discard, call, and riichi benchmark reports. Discard
  summaries include ablation deltas and selected defense buckets; call/riichi summaries include
  eval accuracy, balanced accuracy, pass/target recall, policy threshold/source, train/eval best
  threshold diagnostics, and positive class weight. `--json` emits a machine-readable summary.
- `disagreement-report-summary` reads ignored disagreement exports and summarizes category counts,
  defense bucket rates, common actual/predicted tile pairs, and logit margins. Use `--examples N`
  to append representative stored examples with defense flags and top logits. Use `--tags` to add
  deterministic stored-example tags for defense signals, efficiency-like cases, close logits,
  active riichi, and safe-tile candidates. Use `--tag TAG` with `--examples` to render only stored
  examples matching one deterministic tag.
- `benchmark-call` scores four supervised call/pass baselines from existing `CallExample`
  reconstruction data: `call-frequency-v0`, `call-legal-frequency-v0`, `call-linear-v0`, and
  additive `call-linear-v1`.
  Reports include overall accuracy, balanced accuracy, macro recall, pass/call recall, and
  per-action recall. Reports now also include `call_linear_v1_calibrated`, a fixed-threshold
  policy variant over `call-linear-v1` at non-pass threshold 0.40. Linear call payloads include
  report-only call/pass threshold calibration sweeps, and `--include-weighted` adds
  `call_linear_v1_weighted` trained with `--call-positive-weight`. `benchmark-call --models`
  accepts `all`, `fast`, or comma-separated names; `fast` runs frequency, legal-frequency,
  `call_linear_v1`, and `call_linear_v1_calibrated`. For weighted sweeps, use `all` with
  `--include-weighted` or explicitly include `call_linear_v1_weighted`; `fast` intentionally omits
  it. `--example-limit N` keeps bounded large-slice iteration practical.
  `--example-limit-strategy prefix|balanced` selects either a deterministic
  prefix or a roughly even call/pass cap. `--profile-stages` records stage timings,
  `--example-cache PATH` reuses reconstructed call examples before feature preparation,
  `--feature-cache PATH` reuses prepared call features across repeated runs, `--epochs 0` supports
  zero-update report smokes, and `--call-threshold-source train-best` uses the train split threshold
  sweep winner for the calibrated policy variant.
- `benchmark-riichi` scores the first conservative riichi/pass dataset from explicit Tenhou reach
  events plus closed tenpai no-riichi discard decisions. It now reports `riichi-frequency-v0` and
  `riichi-linear-v0`. Reports now also include `riichi_linear_calibrated`, a fixed-threshold policy
  variant over `riichi-linear-v0` at riichi threshold 0.95. Linear riichi payloads include
  report-only riichi/pass threshold calibration sweeps, `--include-weighted` adds
  `riichi_linear_weighted` trained with `--riichi-positive-weight`, and
  `--riichi-threshold-source train-best` uses the train split threshold sweep winner for the
  calibrated policy variant.
- Local Mortal checkout/build reconnaissance is recorded in `docs/external-baselines.md`.
- `export-decision-snapshots` writes local-only JSONL decision rows for discard/call/riichi
  examples. Rows include stable `row_id` values, Kenjaku reconstruction fields, legal actions,
  observed action, and a minimal `mjai_events` prefix for future offline comparison through a
  neutral boundary.
- `decision-snapshot-summary` reads local snapshot JSONL files and reports valid/malformed row
  counts, decision type counts, action counts, source labels, and `mjai_events` presence.
- `decision-snapshot-compare` compares snapshot JSONL against prediction JSONL rows containing
  `row_id` and `predicted_action`, reporting exact-action accuracy by decision type plus binary
  call/riichi metrics and missing/malformed/duplicate prediction counts.
- `produce-decision-predictions` writes protocol-test prediction JSONL with `pass`, `first-legal`,
  or `echo-actual` stub strategies. It is not real Mortal inference.
- `export-decision-snapshots --include-outcome` adds terminal score-delta/win/deal-in/draw labels
  parsed from Tenhou `sc` fields. Default snapshots intentionally omit outcome labels.
- `train-discard-mlp` trains a small PyTorch masked-logit discard MLP over normalized hand and
  visible-count tensors. It supports deterministic seeds, CPU/MPS/CUDA/auto device selection, and
  JSON reports.

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
- PyTorch is now a core dependency for the supervised-learning path. Keep non-ML command imports
  lazy where practical so source checkouts remain usable before installation.

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
- `call-linear-v0` uses a concealed-remainder shanten proxy for calls. It does not model full
  open-hand shanten or exact chi shape selection beyond choosing the best simple consumed-tile
  proxy.
- `call-linear-v1` is additive and keeps the v0 120-feature prefix stable. It appends exact
  chi-position flags, consumed-tile/open-call proxies, shanten-improvement flags, cached ukeire
  proxies, discarded-tile visibility, and terminal/honor flags.
- Call feature-cache payloads are local-only prepared-example caches. The cache key includes
  selected/train/eval example signatures; do not remove those signatures because equivalent counts
  are not enough to prove feature compatibility.
- Call example-cache payloads are local-only reconstructed-example caches. They intentionally sit
  before `--example-limit` and deterministic splitting, so one cache can support multiple caps and
  split settings as long as the XML file identity/size/mtime and `--skip-errors` match.
- `--example-limit-strategy balanced` is a deterministic roughly even call/pass cap for diagnostics,
  not a natural distribution sample. Metrics from balanced caps should not be compared directly to
  prefix or uncapped reports.
- Balanced call limiting now interleaves selected call and pass examples. Cache signatures include
  selected order, so the v1 cache path must change after any selection-order change.
- Riichi examples are conservative supervised decision points, not complete riichi legality. Negative
  examples require closed tenpai by the existing closed-hand shanten proxy and sufficient score.
- `riichi-linear-v0` fixes riichi recall but needs threshold calibration. On the current 500-log
  checks, train-best calibrated thresholds beat fixed `0.25` on balanced accuracy; fixed `0.25`
  is useful as a high-riichi-recall comparison, not as the baseline.
- Threshold sweeps use thresholds 0.00 through 1.00 in 0.05 steps and choose the best threshold by
  balanced accuracy, then target recall, then lower threshold. Calibrated report variants default
  to fixed policy thresholds from the 100-log `tenhou-100-v0` sweep, but
  `--call-threshold-source train-best` and `--riichi-threshold-source train-best` can use the train
  split winner instead. Eval best thresholds remain diagnostics only. None of these options changes
  default model `predict()` behavior.
- Mortal is AGPL-3.0-or-later. Keep any future comparison behind a neutral data/subprocess boundary
  unless the project intentionally accepts that license boundary.

## Local Artifacts

Expected ignored local paths when local Tenhou data and external baselines are available:

```bash
data/raw/tenhou/db/current-year.db
data/raw/tenhou/xml/4p-hanchan-25
data/raw/tenhou/xml/4p-hanchan-100
data/raw/tenhou/xml/4p-hanchan-500
data/raw/external/mortal
runs/discard-benchmark-tenhou-25-report-v1.json
runs/discard-benchmark-tenhou-100-lr0.1-l2-0-report.json
runs/discard-benchmark-tenhou-100-lr0.1-l2-0.0001-report.json
runs/discard-benchmark-tenhou-100-lr0.1-l2-0.001-report.json
runs/discard-benchmark-tenhou-100-lr0.05-l2-0-report.json
runs/discard-benchmark-tenhou-100-fast-lr0.05-l2-0-report.json
runs/discard-benchmark-tenhou-100-lr0.05-l2-0.0001-report.json
runs/discard-disagreements-tenhou-100-lr0.1-l2-0.json
runs/discard-disagreements-tenhou-100-lr0.1-l2-0-summary.json
runs/discard-disagreements-tenhou-100-lr0.05-l2-0.json
runs/discard-disagreements-tenhou-100-lr0.05-l2-0-summary.json
runs/call-benchmark-tenhou-100-report.json
runs/call-benchmark-tenhou-100-v1-report.json
runs/call-benchmark-tenhou-100-v2-report.json
runs/call-benchmark-tenhou-100-v3-report.json
runs/call-benchmark-tenhou-100-v4-report.json
runs/call-benchmark-tenhou-100-v5-report.json
runs/call-benchmark-tenhou-100-v6-report.json
runs/call-benchmark-tenhou-500-fast-limit5000-epochs5-train-best-v0-report.json
runs/call-features-tenhou-500-fast-balanced-limit10000-v0.json
runs/call-benchmark-tenhou-500-fast-balanced-limit10000-epochs5-train-best-v0-report.json
runs/call-benchmark-tenhou-500-fast-balanced-limit10000-epochs5-train-best-v0-cachehit-report.json
runs/call-features-tenhou-500-fast-balanced-limit10000-v1.json
runs/call-benchmark-tenhou-500-balanced-limit10000-e30-lr0.05-w1.0-weighted-v1-report.json
runs/call-features-tenhou-500-balanced-limit20000-v1.json
runs/call-benchmark-tenhou-500-balanced-limit20000-e30-lr0.05-w1.0-weighted-v1-report.json
runs/call-benchmark-tenhou-500-balanced-limit20000-e30-lr0.05-w1.0-weighted-v1-cachehit-report.json
runs/riichi-benchmark-tenhou-100-v0-report.json
runs/riichi-benchmark-tenhou-100-v1-report.json
runs/riichi-benchmark-tenhou-100-v2-report.json
runs/riichi-benchmark-tenhou-100-v3-report.json
runs/riichi-benchmark-tenhou-500-v0-report.json
runs/riichi-benchmark-tenhou-500-train-best-v0-report.json
runs/riichi-benchmark-tenhou-500-train-best-v1-report.json
runs/riichi-benchmark-tenhou-500-train-best-tenhou-500-v*.json
runs/riichi-benchmark-tenhou-500-fixed025-tenhou-500-v*.json
runs/fixture-discard-mlp.json
runs/fixture-decision-snapshots.jsonl
runs/fixture-decision-predictions.jsonl
runs/decision-snapshots-local.jsonl
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
python3 -m ruff check .
PYTHONPATH=src python3 -m kenjaku benchmark-discard data/fixtures/tenhou \
  --epochs 1 --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --l2 0.0001 --report runs/fixture-benchmark-diagnostics.json \
  --disagreements runs/fixture-disagreements.json
PYTHONPATH=src python3 -m kenjaku benchmark-report-summary runs/fixture-benchmark-diagnostics.json
PYTHONPATH=src python3 -m kenjaku disagreement-report-summary \
  runs/fixture-disagreements.json --examples 1 --tags
PYTHONPATH=src python3 -m kenjaku disagreement-report-summary \
  runs/fixture-disagreements.json --examples 1 --tags --tag close_logit
PYTHONPATH=src python3 -m kenjaku export-decision-snapshots data/fixtures/tenhou \
  --output runs/fixture-decision-snapshots.jsonl --limit 5
PYTHONPATH=src python3 -m kenjaku decision-snapshot-summary \
  runs/fixture-decision-snapshots.jsonl
PYTHONPATH=src python3 -m kenjaku produce-decision-predictions \
  runs/fixture-decision-snapshots.jsonl \
  --strategy echo-actual \
  --output runs/fixture-decision-predictions.jsonl
PYTHONPATH=src python3 -m kenjaku decision-snapshot-compare \
  runs/fixture-decision-snapshots.jsonl \
  runs/fixture-decision-predictions.jsonl
PYTHONPATH=src python3 -m kenjaku train-discard-mlp data/fixtures/tenhou \
  --epochs 1 --batch-size 2 --hidden-dim 8 --device cpu \
  --eval-fraction 0.25 --split-seed fixed --seed 123 \
  --report runs/fixture-discard-mlp.json
PYTHONPATH=src python3 -m kenjaku benchmark-call data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --include-weighted \
  --report runs/fixture-call-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-call data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --models fast \
  --example-limit 1 \
  --example-limit-strategy balanced \
  --profile-stages \
  --example-cache runs/fixture-call-example-cache.json \
  --feature-cache runs/fixture-call-feature-cache.json \
  --call-threshold-source train-best \
  --report runs/fixture-call-benchmark-fast.json
PYTHONPATH=src python3 -m kenjaku benchmark-riichi data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --include-weighted \
  --report runs/fixture-riichi-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-riichi data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --riichi-threshold-source train-best \
  --report runs/fixture-riichi-benchmark-train-best.json
PYTHONPATH=src python3 -m kenjaku benchmark-report-summary \
  runs/fixture-call-benchmark.json runs/fixture-riichi-benchmark.json
git diff --check
```

Useful local benchmark command:

```bash
PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --epochs 3 \
  --learning-rate 0.05 \
  --l2 0.0 \
  --models fast \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --skip-errors \
  --report runs/discard-benchmark-tenhou-100-fast-lr0.05-l2-0-report.json \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-call \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --skip-errors \
  --include-weighted \
  --call-positive-weight 2.0 \
  --report runs/call-benchmark-tenhou-100-v6-report.json \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-call \
  data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 \
  --split-seed tenhou-500-v0 \
  --epochs 30 \
  --learning-rate 0.05 \
  --skip-errors \
  --models call_linear_v1,call_linear_v1_calibrated,call_linear_v1_weighted \
  --include-weighted \
  --call-positive-weight 1.0 \
  --example-limit 20000 \
  --example-limit-strategy balanced \
  --call-threshold-source train-best \
  --profile-stages \
  --example-cache runs/call-examples-tenhou-500-v1.json \
  --feature-cache runs/call-features-tenhou-500-balanced-limit20000-v1.json \
  --report runs/call-benchmark-tenhou-500-balanced-limit20000-v1-report.json \
  --source-label tenhou-4p-hanchan-500 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-500 --players 4 --length h --limit 500" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-riichi \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --skip-errors \
  --include-weighted \
  --riichi-positive-weight 2.0 \
  --report runs/riichi-benchmark-tenhou-100-v2-report.json \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-riichi \
  data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 \
  --split-seed tenhou-500-v1 \
  --skip-errors \
  --include-weighted \
  --riichi-positive-weight 2.0 \
  --riichi-threshold-source train-best \
  --report runs/riichi-benchmark-tenhou-500-train-best-v1-report.json \
  --source-label tenhou-4p-hanchan-500 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-500 --players 4 --length h --limit 500" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --epochs 3 \
  --learning-rate 0.05 \
  --l2 0.0 \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --models risk_context_linear,defense_context_linear,defense_context_v1_linear \
  --skip-errors \
  --disagreements runs/discard-disagreements-tenhou-100-lr0.05-l2-0.json \
  --max-disagreements 100 \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku disagreement-report-summary \
  runs/discard-disagreements-tenhou-100-lr0.05-l2-0.json \
  --examples 2 \
  --tags \
  --tag close_logit

PYTHONPATH=src python3 -m kenjaku export-decision-snapshots \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --decision-types discard,call,riichi \
  --limit 1000 \
  --output runs/decision-snapshots-tenhou-100-v0.jsonl \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku decision-snapshot-summary \
  runs/decision-snapshots-tenhou-100-v0.jsonl
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

100-log fast discard rerun, split `tenhou-100-v0`, `lr=0.05`, `l2=0.0`, `--models fast`:

- Eval accuracy: frequency 0.2985, shanten-aware linear 0.5012, risk-context linear 0.5124,
  defense-context linear 0.5124.
- This matches the earlier best risk/defense aggregate result while skipping raw-count and v1.

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
- `disagreement-report-summary` on the capped `lr=0.1`, `l2=0.0` artifact shows stored examples
  are usually not obvious safe-tile cases: the four 100-item category samples have active-riichi
  rates around 24-26%, genbutsu 10-13%, suji 6-12%, and seen-after-riichi 2-8%.
- Mean stored logit margins are modest but nontrivial: risk-correct/defense-wrong has risk actual
  margin 0.3002 and defense wrong-over-actual margin 0.3169; defense-correct/risk-wrong has defense
  actual margin 0.2538 and risk wrong-over-actual margin 0.3062.
- Current-best disagreement counts at `lr=0.05`, `l2=0.0`: risk-correct/defense-wrong 203,
  risk-correct/v1-wrong 268, defense-correct/risk-wrong 203, v1-correct/risk-wrong 246.
- The capped `lr=0.05`, `l2=0.0` stored examples have active-riichi rates around 28-36%,
  genbutsu 10-16%, suji 4-14%, and seen-after-riichi 1-10%. Defense-correct samples have more
  active-riichi and safety-signal mass than risk-correct samples, but neither side is dominated by
  obvious safe-tile examples.
- `disagreement-report-summary --examples 2` confirms the stored examples are mixed: some
  defense-correct cases are active-riichi/suji or genbutsu-adjacent, while several risk-correct
  cases are no-riichi close-logit choices rather than obvious defense misses.
- `disagreement-report-summary --examples 2 --tags` on the current-best capped artifact tags 400
  stored examples as 373 efficiency-like, 336 close-logit, 130 active-riichi, 130 safe-tile
  candidates, and 82 defense-signal cases. No stored item falls into `no_obvious_signal` under the
  current deterministic rules.
- Mean stored logit margins at `lr=0.05`, `l2=0.0`: risk-correct/defense-wrong has risk actual
  margin 0.2660 and defense wrong-over-actual margin 0.2449; defense-correct/risk-wrong has defense
  actual margin 0.2312 and risk wrong-over-actual margin 0.2142.
- At `lr=0.05`, `l2=0.0`, selected v1-only feature activation rates on eval candidates:
  sotogawa 0.0462, dora 0.0033, active ippatsu fraction 0.0369, active tsumogiri fraction 0.1244,
  opponent meld tile fraction 0.4234.
- Feature summaries are identical across LR/L2 runs for a fixed profile and split; weight summaries
  change with training settings.

Call benchmark, 100-log local slice, split `tenhou-100-v0`:

- Dataset: 13,435 call/pass examples; 10,748 train / 2,687 eval.
- `call-frequency-v0` scored 0.8524 train / 0.8463 eval accuracy, but only 0.5000 balanced eval
  accuracy and 0.0000 call recall. It predicts pass too often.
- `call-legal-frequency-v0` scored 0.1515 eval accuracy, 0.4927 balanced eval accuracy, 0.0000
  pass recall, and 0.9855 call recall. It predicts legal calls too often.
- `call-linear-v0` scored 0.8306 train / 0.8288 eval accuracy, 0.7294 balanced eval accuracy,
  0.8729 pass recall, and 0.5860 call recall. Per-action eval recall: chi 0.4045, minkan 1.0000,
  pass 0.8729, pon 0.7210.
- `call-linear-v1` scored 0.8410 train / 0.8377 eval accuracy, 0.7496 balanced eval accuracy,
  0.8769 pass recall, and 0.6223 call recall. Per-action eval recall: chi 0.4663, minkan 0.5000,
  pass 0.8769, pon 0.7425.
- Report-only threshold calibration for `call-linear-v1` picked threshold 0.40 on eval, with binary
  balanced accuracy 0.7762, call precision 0.4393, call recall 0.7191, and pass recall 0.8333.
  For comparison, `call-linear-v0` picked threshold 0.35 with binary balanced accuracy 0.7531.
- The explicit `call_linear_v1_calibrated` policy variant at threshold 0.40 scored 0.8154 eval
  accuracy, 0.7750 exact-action balanced eval accuracy, 0.8333 pass recall, and 0.7167 exact-call
  recall.
- The opt-in `call_linear_v1_weighted` comparison at positive class weight 2.0 scored 0.6729 eval
  accuracy, 0.7522 balanced eval accuracy, 0.6376 pass recall, and 0.8668 call recall. Its own
  report-only threshold sweep picked 0.80 with binary balanced accuracy 0.7669.
- `call-linear-v1` is now the strongest call/pass floor on this split. It improves aggregate,
  balanced accuracy, pass recall, and call recall versus v0. The calibrated v1 policy beats the
  simple weighted training comparison on balanced accuracy, while weighted training mostly trades
  pass recall for call recall.

Riichi benchmark, 100-log local slice, split `tenhou-100-v0`:

- Dataset: 2,039 conservative riichi/pass examples; 1,631 train / 408 eval.
- `riichi-frequency-v0` scored 0.6272 train / 0.6618 eval accuracy, 0.5000 balanced eval accuracy,
  1.0000 pass recall, and 0.0000 riichi recall. It is a pass-dominant floor, not a useful riichi
  policy.
- `riichi-linear-v0` scored 0.5254 train / 0.4510 eval accuracy, 0.5639 balanced eval accuracy,
  0.2148 pass recall, and 0.9130 riichi recall.
- Report-only threshold calibration for `riichi-linear-v0` picked threshold 0.95 on eval, with
  binary balanced accuracy 0.6444, riichi precision 0.5476, riichi recall 0.5000, and pass recall
  0.7889.
- The explicit `riichi_linear_calibrated` policy variant at threshold 0.95 scored 0.6912 eval
  accuracy, 0.6444 balanced eval accuracy, 0.7889 pass recall, and 0.5000 riichi recall.
- The opt-in `riichi_linear_weighted` comparison at positive class weight 2.0 scored 0.3603 eval
  accuracy, 0.5114 balanced eval accuracy, 0.0444 pass recall, and 0.9783 riichi recall. Its own
  report-only threshold sweep picked 0.95 with binary balanced accuracy 0.5713.
- `riichi-linear-v0` proves the feature stream can identify riichi opportunities, and thresholding
  gives a much healthier tradeoff than raw argmax prediction or simple positive weighting. Do not
  add riichi features before confirming this calibrated policy on a larger local slice.

Riichi benchmark, 500-log local slice, split-seed checks:

- Dataset shape: 10,249 conservative riichi/pass examples per split; 8,199 train / 2,050 eval.
- Train-best `riichi_linear_calibrated` thresholds across seeds `tenhou-500-v0` through
  `tenhou-500-v3`: 0.35, 0.45, 0.35, 0.40.
- Train-best balanced eval accuracy by seed: 0.6737, 0.6608, 0.6738, 0.6533.
- Train-best riichi/pass recall by seed: 0.7548/0.5927, 0.6601/0.6615, 0.6970/0.6507,
  0.7015/0.6051.
- Fixed threshold 0.25 across the same seeds raised riichi recall but weakened pass recall:
  balanced eval accuracy 0.6732, 0.6401, 0.6690, 0.6406; riichi recall 0.8624, 0.8379, 0.7997,
  0.8426; pass recall 0.4840, 0.4423, 0.5383, 0.4387.
- Conclusion: keep train-best `riichi_linear_calibrated` as the current baseline. Fixed 0.25 is a
  high-riichi-recall comparison, not the balanced baseline.

Call benchmark, 500-log local slice:

- Source examples: 69,013 call/pass examples.
- Balanced 10k cap after deterministic call/pass interleaving: 8,000 train / 2,000 eval. Initial
  v1 cache build spent 377.2313s in `feature_prepare_v1`; weighted-grid cache-hit loads were
  0.3100-0.3914s.
- Full 10k grid over epochs `{5,15,30}`, learning rates `{0.1,0.05,0.02}`, and positive weights
  `{1.0,1.5,2.0}` produced 27 explicit weighted reports.
- Best train-selected calibrated policy on 10k: 30 epochs, LR 0.05, train-selected threshold 0.40,
  eval accuracy 0.7560, balanced eval accuracy 0.7558, pass recall 0.7404, call recall 0.7713.
- Best weighted comparison on 10k: 30 epochs, LR 0.1, positive weight 2.0, eval accuracy 0.7600,
  balanced eval accuracy 0.7601, pass recall 0.7687, call recall 0.7515.
- Since 10k no longer collapsed, the next 20k cap was run with 30 epochs / LR 0.05 / weight 1.0.
  Initial 20k cache build spent 267.5774s in `feature_prepare_v1`; the cache-hit rerun loaded v1
  features in 0.7491s.
- 20k calibrated result: 16,000 train / 4,000 eval, train-selected threshold 0.50, eval accuracy
  0.7290, balanced eval accuracy 0.7289, pass recall 0.6979, call recall 0.7598.
- Conclusion: balanced call training is no longer pass-only after the interleaving fix and longer
  training. The next blocker is scaling beyond 20k or uncapped without repeated parse/reconstruct
  cost, not basic call recall collapse.

Mortal local baseline reconnaissance:

- Ignored checkout: `data/raw/external/mortal`.
- Commit inspected: `0cff2b52982be5b1163aa9a62fb01f03ce91e0d2`.
- `cargo build -p libriichi --lib --release` passed locally in 37.70s.
- `cargo test --workspace --no-default-features --features flate2/zlib -- --nocapture` passed:
  28 Rust unit tests plus doctests reported OK.
- `cp target/release/libriichi.dylib mortal/libriichi.so` followed by
  `PYTHONPATH=mortal python3 -c "import libriichi"` succeeded.

## Next Tasks

1. Use `benchmark-call --example-cache` on the 500-log slice, then compare the 10k best weighted
   policy against the 20k calibrated policy with the same cap and
   seed before selecting a default call report policy.
2. Keep riichi on train-best calibration for now. Do not add riichi features until a concrete
   failure mode appears beyond the fixed-0.25 recall/pass-recall tradeoff.
3. Use the stub prediction producer only for protocol tests. Real Mortal inference still requires
   legally usable weights and a subprocess/data boundary.
4. Use disagreement tag filters to guide discard work. The current sample is mostly efficiency-like
   and close-logit, so avoid a new defense profile until tag-specific examples reveal a concrete gap.
5. Extend the PyTorch path carefully: add validation metrics and checkpointing before attempting a
   larger discard neural model.
