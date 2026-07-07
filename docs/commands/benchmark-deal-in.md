# `benchmark-deal-in`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Train and evaluate a small direct ron-discard probability estimator.

## Inputs

Tenhou XML files or directories with reconstructed discard outcomes.

## Outputs

Text or JSON benchmark report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-deal-in data/fixtures/tenhou --epochs 5 --report runs/deal-in-benchmark.json --json
```

## Gotchas

- Small fixture runs validate command shape only.
- Use `--active-riichi-only` only when that narrower evaluation is intended.

## Help

```text
usage: kenjaku benchmark-deal-in [-h] [--epochs EPOCHS]
                                 [--learning-rate LEARNING_RATE] [--l2 L2]
                                 [--positive-class-weight POSITIVE_CLASS_WEIGHT]
                                 [--eval-fraction EVAL_FRACTION]
                                 [--split-seed SPLIT_SEED]
                                 [--threshold THRESHOLD]
                                 [--active-riichi-only] [--report REPORT]
                                 [--json] [--skip-errors]
                                 [--source-label SOURCE_LABEL]
                                 [--source-command SOURCE_COMMAND]
                                 [--source-date SOURCE_DATE]
                                 paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --epochs EPOCHS       training epochs for the logistic estimator
  --learning-rate LEARNING_RATE
                        logistic estimator learning rate
  --l2 L2               L2 regularization strength
  --positive-class-weight POSITIVE_CLASS_WEIGHT
                        weight applied to direct deal-in examples during
                        training
  --eval-fraction EVAL_FRACTION
                        fraction of examples assigned to eval split
  --split-seed SPLIT_SEED
                        stable seed for train/eval split
  --threshold THRESHOLD
                        probability threshold for binary metrics
  --active-riichi-only  train/evaluate only discard examples with active
                        riichi opponents
  --report REPORT       optional path for a JSON deal-in benchmark report
  --json                emit the benchmark report as JSON instead of text
  --skip-errors         record parse failures and continue with successfully
                        parsed files
  --source-label SOURCE_LABEL
                        human-readable source label recorded in JSON reports
  --source-command SOURCE_COMMAND
                        local data command or manifest reference recorded in
                        JSON reports
  --source-date SOURCE_DATE
                        source date or date range recorded in JSON reports
```
