# Session Handoff

Last updated: 2026-06-06.

## Stop State

- Last work slice: added call fast model selection, prepared-example reuse for call/riichi
  benchmark scoring and calibration sweeps, train-selected calibrated threshold policies,
  `decision-snapshot-summary`, and a fresh 500-log riichi train-best calibration run. The bounded
  500-log call fast run still did not finish quickly enough and remains the main runtime blocker.
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
  `call_linear_v1`, and `call_linear_v1_calibrated`. `--call-threshold-source train-best` uses the
  train split threshold sweep winner for the calibrated policy variant.
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
  examples. Rows include Kenjaku reconstruction fields, legal actions, observed action, and a
  minimal `mjai_events` prefix for future offline comparison through a neutral boundary.
- `decision-snapshot-summary` reads local snapshot JSONL files and reports valid/malformed row
  counts, decision type counts, action counts, source labels, and `mjai_events` presence.

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
- `call-linear-v0` uses a concealed-remainder shanten proxy for calls. It does not model full
  open-hand shanten or exact chi shape selection beyond choosing the best simple consumed-tile
  proxy.
- `call-linear-v1` is additive and keeps the v0 120-feature prefix stable. It appends exact
  chi-position flags, consumed-tile/open-call proxies, shanten-improvement flags, cached ukeire
  proxies, discarded-tile visibility, and terminal/honor flags.
- Riichi examples are conservative supervised decision points, not complete riichi legality. Negative
  examples require closed tenpai by the existing closed-hand shanten proxy and sufficient score.
- `riichi-linear-v0` fixes riichi recall but overcalls riichi on the current 100-log split. Treat
  calibration as the next riichi task before adding more features.
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
runs/riichi-benchmark-tenhou-100-v0-report.json
runs/riichi-benchmark-tenhou-100-v1-report.json
runs/riichi-benchmark-tenhou-100-v2-report.json
runs/riichi-benchmark-tenhou-100-v3-report.json
runs/riichi-benchmark-tenhou-500-v0-report.json
runs/riichi-benchmark-tenhou-500-train-best-v0-report.json
runs/fixture-decision-snapshots.jsonl
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
PYTHONPATH=src python3 -m kenjaku benchmark-call data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --include-weighted \
  --report runs/fixture-call-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-call data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --models fast \
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
  --skip-errors \
  --models fast \
  --call-threshold-source train-best \
  --report runs/call-benchmark-tenhou-500-fast-train-best-v0-report.json \
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
  --split-seed tenhou-500-v0 \
  --skip-errors \
  --include-weighted \
  --riichi-positive-weight 2.0 \
  --riichi-threshold-source train-best \
  --report runs/riichi-benchmark-tenhou-500-train-best-v0-report.json \
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

Riichi benchmark, 500-log local slice, split `tenhou-500-v0`:

- Dataset: 10,249 conservative riichi/pass examples; 8,199 train / 2,050 eval.
- `riichi-frequency-v0` scored 0.6420 eval accuracy, 0.5000 balanced eval accuracy, 1.0000 pass
  recall, and 0.0000 riichi recall.
- `riichi-linear-v0` scored 0.6712 eval accuracy, 0.6337 balanced eval accuracy, 0.7660 pass
  recall, and 0.5014 riichi recall.
- Fixed-threshold `riichi_linear_calibrated` at 0.95 did not transfer: 0.6502 eval accuracy,
  0.5143 balanced eval accuracy, 0.9932 pass recall, and only 0.0354 riichi recall.
- Train-selected `riichi_linear_calibrated` chose threshold 0.25 and scored 0.6293 eval accuracy,
  0.6534 balanced eval accuracy, 0.5684 pass recall, and 0.7384 riichi recall. This is the best
  current report-policy tradeoff on the 500-log slice.
- `riichi_linear_weighted` at positive class weight 2.0 scored 0.6434 eval accuracy, 0.6412
  balanced eval accuracy, 0.6489 pass recall, and 0.6335 riichi recall.
- The unweighted riichi threshold sweep picked 0.20 on eval with binary balanced accuracy 0.6606,
  riichi precision 0.4827, riichi recall 0.7984, and pass recall 0.5228.

Call benchmark, 500-log local slice:

- `data/raw/tenhou/xml/4p-hanchan-500` was exported successfully, but
  `benchmark-call --include-weighted` was still CPU-active after more than an hour and was stopped
  before writing `runs/call-benchmark-tenhou-500-v0-report.json`.
- The bounded fast run,
  `benchmark-call --models fast --call-threshold-source train-best`, was still CPU-active after
  about five minutes and was stopped before writing
  `runs/call-benchmark-tenhou-500-fast-train-best-v0-report.json`.
- Treat large-slice call calibration as a benchmark performance problem first. Do not use missing
  500-log call results to make a policy choice.

Mortal local baseline reconnaissance:

- Ignored checkout: `data/raw/external/mortal`.
- Commit inspected: `0cff2b52982be5b1163aa9a62fb01f03ce91e0d2`.
- `cargo build -p libriichi --lib --release` passed locally in 37.70s.
- `cargo test --workspace --no-default-features --features flate2/zlib -- --nocapture` passed:
  28 Rust unit tests plus doctests reported OK.
- `cp target/release/libriichi.dylib mortal/libriichi.so` followed by
  `PYTHONPATH=mortal python3 -c "import libriichi"` succeeded.

## Next Tasks

1. Profile and cache the call-v1 feature path. `--models fast` reduces report breadth, but the
   500-log run still did not finish within about five minutes, so the next call task is deeper
   shanten/ukeire/proxy caching or a lower-cost training/report mode.
2. Validate train-best riichi thresholds across another split or larger slice before adding riichi
   features. On `tenhou-500-v0`, train-best threshold 0.25 is now the best current policy report.
3. Build the next Mortal-boundary step on top of `export-decision-snapshots` and
   `decision-snapshot-summary`: add a neutral consumer or comparator through a subprocess/data-layer
   boundary only when weights are available and legally usable.
4. Use disagreement tag filters to guide discard work. The current sample is mostly efficiency-like
   and close-logit, so avoid a new defense profile until tag-specific examples reveal a concrete gap.
5. Add feature normalization only behind a new discard model kind if tagged examples or weight
   summaries point to scale instability; do not mutate existing feature profiles.
