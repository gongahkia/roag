# `benchmark-riichi`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Compare deterministic riichi/pass baselines on one train/eval split.

## Inputs

Tenhou XML files or directories.

## Outputs

Optional JSON riichi benchmark report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-riichi data/fixtures/tenhou --epochs 3 --report runs/riichi-benchmark.json
```

## Gotchas

- Riichi positives are sparse in tiny fixtures.
- Use `--riichi-threshold-source` consistently across comparisons.

## Help

```text
usage: kenjaku benchmark-riichi [-h] [--eval-fraction EVAL_FRACTION]
                                [--split-seed SPLIT_SEED]
                                [--example-limit EXAMPLE_LIMIT]
                                [--stream-examples] [--epochs EPOCHS]
                                [--learning-rate LEARNING_RATE] [--l2 L2]
                                [--riichi-threshold RIICHI_THRESHOLD]
                                [--riichi-threshold-source {fixed,train-best}]
                                [--include-weighted]
                                [--riichi-positive-weight RIICHI_POSITIVE_WEIGHT]
                                [--report REPORT] [--skip-errors]
                                [--parse-cache PARSE_CACHE] [--jobs JOBS]
                                [--source-label SOURCE_LABEL]
                                [--source-command SOURCE_COMMAND]
                                [--source-date SOURCE_DATE]
                                paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --eval-fraction EVAL_FRACTION
                        fraction of examples reserved for deterministic
                        evaluation
  --split-seed SPLIT_SEED
                        stable seed for deterministic train/eval split
  --example-limit EXAMPLE_LIMIT
                        maximum riichi/pass examples to use after
                        deterministic reconstruction
  --stream-examples     collect examples file-by-file and stop at --example-
                        limit instead of retaining the full parsed dataset
  --epochs EPOCHS       riichi linear model epochs
  --learning-rate LEARNING_RATE
                        riichi linear model SGD learning rate
  --l2 L2               riichi linear model L2 regularization strength
  --riichi-threshold RIICHI_THRESHOLD
                        riichi probability threshold for
                        riichi_linear_calibrated
  --riichi-threshold-source {fixed,train-best}
                        threshold source for riichi_linear_calibrated
  --include-weighted    include positive class-weighted riichi linear
                        comparison variants
  --riichi-positive-weight RIICHI_POSITIVE_WEIGHT
                        positive example weight for --include-weighted riichi
                        linear training
  --report REPORT       optional path for a JSON riichi benchmark report
                        artifact
  --skip-errors         record parse failures and continue with successfully
                        parsed files
  --parse-cache PARSE_CACHE
                        directory for opt-in content-addressed Tenhou XML
                        parse cache
  --jobs JOBS           parallel Tenhou XML parse worker processes
  --source-label SOURCE_LABEL
                        human-readable source label recorded in JSON reports
  --source-command SOURCE_COMMAND
                        local data command or manifest reference recorded in
                        JSON reports
  --source-date SOURCE_DATE
                        source date or date range recorded in JSON reports
```
