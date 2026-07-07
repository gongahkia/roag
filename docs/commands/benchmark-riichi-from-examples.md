# `benchmark-riichi-from-examples`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Benchmark riichi/pass baselines from exported BC JSONL shards.

## Inputs

BC manifest, JSONL files, or shard directories.

## Outputs

Optional JSON riichi benchmark report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-riichi-from-examples runs/bc-examples --epochs 3 --report runs/riichi-from-examples.json
```

## Gotchas

- Run `export-bc-examples --actions riichi` or include riichi examples first.
- Use fixed split seeds for comparable reports.

## Help

```text
usage: kenjaku benchmark-riichi-from-examples [-h]
                                              [--eval-fraction EVAL_FRACTION]
                                              [--split-seed SPLIT_SEED]
                                              [--example-limit EXAMPLE_LIMIT]
                                              [--epochs EPOCHS]
                                              [--learning-rate LEARNING_RATE]
                                              [--l2 L2]
                                              [--riichi-threshold RIICHI_THRESHOLD]
                                              [--riichi-threshold-source {fixed,train-best}]
                                              [--include-weighted]
                                              [--riichi-positive-weight RIICHI_POSITIVE_WEIGHT]
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
  --example-limit EXAMPLE_LIMIT
  --epochs EPOCHS
  --learning-rate LEARNING_RATE
  --l2 L2
  --riichi-threshold RIICHI_THRESHOLD
  --riichi-threshold-source {fixed,train-best}
  --include-weighted
  --riichi-positive-weight RIICHI_POSITIVE_WEIGHT
  --report REPORT
  --source-label SOURCE_LABEL
                        human-readable source label recorded in JSON reports
  --source-command SOURCE_COMMAND
                        local data command or manifest reference recorded in
                        JSON reports
  --source-date SOURCE_DATE
                        source date or date range recorded in JSON reports
```
