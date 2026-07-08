# `benchmark-call`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Compare deterministic call/pass baselines on one train/eval split.

## Inputs

Tenhou XML files or directories.

## Outputs

Optional JSON call benchmark report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-call data/fixtures/tenhou --models fast --epochs 3 --report runs/call-benchmark.json
```

## Gotchas

- Call/pass labels are sparse; small fixtures mostly validate plumbing.
- Use cache paths only under ignored local directories.

## Help

```text
usage: kenjaku benchmark-call [-h] [--eval-fraction EVAL_FRACTION]
                              [--split-seed SPLIT_SEED] [--epochs EPOCHS]
                              [--learning-rate LEARNING_RATE] [--l2 L2]
                              [--call-threshold CALL_THRESHOLD]
                              [--call-threshold-source {fixed,train-best}]
                              [--models MODELS]
                              [--example-limit EXAMPLE_LIMIT]
                              [--stream-examples]
                              [--example-limit-strategy {prefix,balanced}]
                              [--profile-stages]
                              [--example-cache EXAMPLE_CACHE]
                              [--feature-cache FEATURE_CACHE]
                              [--include-weighted]
                              [--call-positive-weight CALL_POSITIVE_WEIGHT]
                              [--report REPORT] [--skip-errors]
                              [--parse-cache PARSE_CACHE]
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
  --epochs EPOCHS       call linear model epochs
  --learning-rate LEARNING_RATE
                        call linear model SGD learning rate
  --l2 L2               call linear model L2 regularization strength
  --call-threshold CALL_THRESHOLD
                        non-pass probability threshold for
                        call_linear_v1_calibrated
  --call-threshold-source {fixed,train-best}
                        threshold source for call_linear_v1_calibrated
  --models MODELS       call benchmark models: all, fast, or comma-separated
                        model names (call_frequency, call_legal_frequency,
                        call_linear, call_linear_v1,
                        call_linear_v1_calibrated, call_linear_v1_weighted)
  --example-limit EXAMPLE_LIMIT
                        maximum call examples to keep after deterministic
                        reconstruction order
  --stream-examples     collect examples file-by-file and stop at --example-
                        limit instead of retaining the full parsed dataset
  --example-limit-strategy {prefix,balanced}
                        call example limiting strategy
  --profile-stages      print and record call benchmark stage timings
  --example-cache EXAMPLE_CACHE
                        optional ignored JSON cache for reconstructed call
                        examples
  --feature-cache FEATURE_CACHE
                        optional ignored JSON feature cache for prepared call
                        examples
  --include-weighted    include positive class-weighted call linear v1
                        comparison variants
  --call-positive-weight CALL_POSITIVE_WEIGHT
                        positive example weight for --include-weighted call
                        linear training
  --report REPORT       optional path for a JSON call benchmark report
                        artifact
  --skip-errors         record parse failures and continue with successfully
                        parsed files
  --parse-cache PARSE_CACHE
                        directory for opt-in content-addressed Tenhou XML
                        parse cache
  --source-label SOURCE_LABEL
                        human-readable source label recorded in JSON reports
  --source-command SOURCE_COMMAND
                        local data command or manifest reference recorded in
                        JSON reports
  --source-date SOURCE_DATE
                        source date or date range recorded in JSON reports
```
