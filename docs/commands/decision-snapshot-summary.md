# `decision-snapshot-summary`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Summarize one or more decision snapshot JSONL exports.

## Inputs

Decision snapshot JSONL files.

## Outputs

Text or JSON summary.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku decision-snapshot-summary runs/decision-snapshots.jsonl --json
```

## Gotchas

- This validates export shape and counts; it does not run a model.
- Malformed rows should be fixed at export time.

## Help

```text
usage: kenjaku decision-snapshot-summary [-h] [--json]
                                         snapshots [snapshots ...]

positional arguments:
  snapshots   decision snapshot JSONL files

options:
  -h, --help  show this help message and exit
  --json      emit the summary as JSON instead of text
```
