# Supervised Baseline Comparison

TODO-104 is not complete until TODO-103 has a real Mortal-compatible baseline report on a permitted
shared slice. This file freezes the metric contract before the three-seed Kenjaku runs.

## Required Inputs

- One ignored BC manifest for the supervised slice, for example
  `runs/todo-102/bc-examples-v0/manifest.json`.
- One ignored shared decision snapshot file from the same permitted slice, for example
  `runs/todo-103/shared-decision-snapshots.jsonl`.
- One ignored TODO-103 external baseline report with a real `mortal-compatible:*` row, not a stub
  `pass` or `first-legal` producer.
- Three Kenjaku seed reports per selected supervised model.
- Prediction JSONL rows for the selected Kenjaku checkpoint when the model can run through
  `predict`; report-only benchmark metrics are acceptable only for model families without a saved
  checkpoint path.

## Target Metrics

| Target | Primary metric | Source field | Direction | Guards |
| --- | --- | --- | --- | --- |
| Discard | Exact discard accuracy | `by_decision_type.discard.accuracy` in `external-baseline-report`, or `eval_accuracy` in the matching Kenjaku supervised report | Higher is better | Comparable discard examples, zero missing prediction rows, zero illegal prediction rows |
| Call | Binary balanced accuracy | `binary.call.balanced_accuracy` in `external-baseline-report`, or `eval_balanced_accuracy` for `call_linear_v1_calibrated` with `--call-threshold-source train-best` | Higher is better | Report call recall and pass recall; reject pass-only wins |
| Riichi | Binary balanced accuracy | `binary.riichi.balanced_accuracy` in `external-baseline-report`, or `eval_balanced_accuracy` for `riichi_linear_calibrated` with `--riichi-threshold-source train-best` | Higher is better | Report riichi recall and pass recall; reject riichi-spam wins |
| Deal-in | Calibration loss | `eval_brier_score` and `eval_log_loss` from `benchmark-deal-in` summary | Lower is better | Also report balanced accuracy, recall, specificity, and heuristic-risk deltas |

If a report lacks the primary field above, the run is invalid for TODO-104.

## Seed Rule

Use three stable split seeds before looking at eval results:

- `todo-104-s0`
- `todo-104-s1`
- `todo-104-s2`

For PyTorch models, pair those with numeric model seeds `1040`, `1041`, and `1042`. Report both the
mean primary metric across all three seeds and the best seed. Keep all raw reports, checkpoints,
prediction rows, and generated tables under ignored paths.

## Match Labels

Use `delta = kenjaku - baseline` for higher-is-better metrics and `delta = baseline - kenjaku` for
lower-is-better metrics.

- `exceeded`: mean delta is positive and the best seed is also positive.
- `matched`: mean delta is not positive, but the mean Kenjaku value is inside the baseline Wilson
  95% interval when that interval exists; otherwise absolute delta is no worse than `0.005` for
  accuracy or balanced accuracy, and no worse than `0.001` for Brier/log loss.
- `missed`: neither condition above is true.

For call and riichi, balanced accuracy is `(target_recall + pass_recall) / 2`. A model cannot be
called matched or exceeded if either recall side is `n/a`.

## Report Template

The checked-in TODO-104 summary must state:

- Frozen input paths and source commands.
- The three seed IDs and report paths.
- The TODO-103 baseline report path and baseline row name.
- Mean and best seed for each primary metric.
- `matched`, `exceeded`, or `missed` for each target, plus the rule that produced the label.
- Explicit statement that raw data, checkpoints, and generated reports remain ignored.

## Evidence Gate

Write the generated comparison evidence under an ignored path such as
`runs/todo-104/supervised-baseline-comparison.json`, then validate it before updating the checked-in
summary:

```bash
python3 scripts/validate_supervised_baseline_comparison.py \
  runs/todo-104/supervised-baseline-comparison.json
```

The validator requires:

- `checked_in_summary_path` to be an existing tracked repo file.
- Ignored paths for the comparison bundle, BC manifest, shared snapshots, TODO-103 baseline report,
  and every non-empty Kenjaku seed report artifact.
- The TODO-103 report to contain the selected baseline family/name row.
- Exactly the seed IDs `todo-104-s0`, `todo-104-s1`, and `todo-104-s2` for every target.
- `discard`, `call`, `riichi`, and `deal_in` target rows with the expected primary metric and
  direction, baseline value, Kenjaku mean, best seed value, label, and guard metrics.
- Label consistency with the match/exceeded/missed rules above.
