# `produce-decision-predictions`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Write stub prediction rows for decision snapshot protocol tests.

## Inputs

Decision snapshot JSONL.

## Outputs

Prediction JSONL at `--output`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku produce-decision-predictions runs/decision-snapshots.jsonl --strategy first-legal --output runs/decision-predictions.jsonl
```

## Gotchas

- Stub strategies are protocol smoke tests only.
- Do not report them as real engine strength.

## Help

```text
usage: kenjaku produce-decision-predictions [-h] --output OUTPUT
                                            [--strategy {pass,first-legal,echo-actual}]
                                            snapshots

positional arguments:
  snapshots             decision snapshot JSONL file

options:
  -h, --help            show this help message and exit
  --output OUTPUT       prediction JSONL output path
  --strategy {pass,first-legal,echo-actual}
                        stub prediction strategy; protocol tests only
```
