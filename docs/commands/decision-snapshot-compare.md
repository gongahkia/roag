# `decision-snapshot-compare`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Compare neutral snapshots against prediction JSONL keyed by `row_id`.

## Inputs

Decision snapshot JSONL and prediction JSONL.

## Outputs

Text or JSON comparison report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku decision-snapshot-compare runs/decision-snapshots.jsonl runs/decision-predictions.jsonl --json
```

## Gotchas

- Predictions must use legal actions from the corresponding snapshot.
- Shared `row_id` values are the join key.

## Help

```text
usage: kenjaku decision-snapshot-compare [-h] [--json] snapshots predictions

positional arguments:
  snapshots    decision snapshot JSONL file
  predictions  prediction JSONL file with row_id and predicted_action

options:
  -h, --help   show this help message and exit
  --json       emit the comparison as JSON instead of text
```
