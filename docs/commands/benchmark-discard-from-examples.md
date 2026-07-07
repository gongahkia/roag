# `benchmark-discard-from-examples`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Benchmark discard baselines from exported BC JSONL shards or manifests.

## Inputs

BC manifest, JSONL files, or shard directories.

## Outputs

Optional JSON benchmark report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-discard-from-examples runs/bc-examples --models fast --epochs 3 --report runs/discard-from-examples.json
```

## Gotchas

- Run `export-bc-examples` first.
- Use `--example-limit` for quick local smoke runs.

## Help

```text
usage: kenjaku benchmark-discard-from-examples [-h] [--epochs EPOCHS]
                                               [--learning-rate LEARNING_RATE]
                                               [--l2 L2] [--models MODELS]
                                               [--eval-fraction EVAL_FRACTION]
                                               [--split-seed SPLIT_SEED]
                                               [--example-limit EXAMPLE_LIMIT]
                                               [--report REPORT]
                                               [--source-label SOURCE_LABEL]
                                               [--source-command SOURCE_COMMAND]
                                               [--source-date SOURCE_DATE]
                                               paths [paths ...]

positional arguments:
  paths                 BC manifest, JSONL files, or shard directories

options:
  -h, --help            show this help message and exit
  --epochs EPOCHS
  --learning-rate LEARNING_RATE
  --l2 L2
  --models MODELS       discard benchmark models: all, fast, or comma-
                        separated model names
  --eval-fraction EVAL_FRACTION
  --split-seed SPLIT_SEED
  --example-limit EXAMPLE_LIMIT
  --report REPORT
  --source-label SOURCE_LABEL
                        human-readable source label recorded in JSON reports
  --source-command SOURCE_COMMAND
                        local data command or manifest reference recorded in
                        JSON reports
  --source-date SOURCE_DATE
                        source date or date range recorded in JSON reports
```
