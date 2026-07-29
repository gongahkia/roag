# `benchmark-call-from-examples`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Benchmark call/pass baselines from exported BC JSONL shards.

## Inputs

BC manifest, JSONL files, or shard directories.

## Outputs

Optional JSON call benchmark report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-call-from-examples runs/bc-examples --models fast --epochs 3 --report runs/call-from-examples.json
```

## Gotchas

- Run `export-bc-examples --actions call` or include call examples first.
- Use `--include-weighted` only when comparing weighted variants deliberately.

## Help

```text
usage: kenjaku benchmark-call-from-examples [-h]
                                            [--eval-fraction EVAL_FRACTION]
                                            [--split-seed SPLIT_SEED]
                                            [--epochs EPOCHS]
                                            [--learning-rate LEARNING_RATE]
                                            [--l2 L2]
                                            [--call-threshold CALL_THRESHOLD]
                                            [--call-threshold-source {fixed,train-best}]
                                            [--models MODELS]
                                            [--example-limit EXAMPLE_LIMIT]
                                            [--example-limit-strategy {prefix,balanced}]
                                            [--include-weighted]
                                            [--call-positive-weight CALL_POSITIVE_WEIGHT]
                                            [--report REPORT]
                                            [--source-label SOURCE_LABEL]
                                            [--source-command SOURCE_COMMAND]
                                            [--source-date SOURCE_DATE]
                                            paths [paths ...]

positional arguments:
  paths                 BC manifest, JSONL files, or shard directories

options:
  -h, --help            show this help message and exit
  --eval-fraction EVAL_FRACTION
  --split-seed SPLIT_SEED
  --epochs EPOCHS
  --learning-rate LEARNING_RATE
  --l2 L2
  --call-threshold CALL_THRESHOLD
  --call-threshold-source {fixed,train-best}
  --models MODELS
  --example-limit EXAMPLE_LIMIT
  --example-limit-strategy {prefix,balanced}
  --include-weighted
  --call-positive-weight CALL_POSITIVE_WEIGHT
  --report REPORT
  --source-label SOURCE_LABEL
                        human-readable source label recorded in JSON reports
  --source-command SOURCE_COMMAND
                        local data command or manifest reference recorded in
                        JSON reports
  --source-date SOURCE_DATE
                        source date or date range recorded in JSON reports
```
