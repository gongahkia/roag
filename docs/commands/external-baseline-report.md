# `external-baseline-report`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Build a shared-log report across named external baseline prediction files.

## Inputs

One decision snapshot JSONL plus one or more `FAMILY:NAME=PATH` baselines.

## Outputs

Text or JSON external-baseline report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku external-baseline-report runs/decision-snapshots.jsonl --baseline kenjaku:first-legal=runs/decision-predictions.jsonl --report runs/external-baseline-report.json --json
```

## Gotchas

- Real Mortal/akochan outputs must come from external legally usable producers.
- Default `--min-decisions` is intentionally high for real comparisons.
- Binary call and riichi sections include balanced accuracy as `(target_recall + pass_recall) / 2`.

## Help

```text
usage: kenjaku external-baseline-report [-h] --baseline BASELINE
                                        [--min-decisions MIN_DECISIONS]
                                        [--report REPORT] [--json]
                                        snapshots

positional arguments:
  snapshots             decision snapshot JSONL file shared by every baseline

options:
  -h, --help            show this help message and exit
  --baseline BASELINE   baseline prediction JSONL as FAMILY:NAME=PATH; repeat
                        for Kenjaku, Mortal-compatible, and akochan-compatible
                        outputs
  --min-decisions MIN_DECISIONS
                        minimum comparable decisions required per baseline
                        (default: 1000)
  --report REPORT       optional path for a JSON external-baseline report
  --json                emit the report as JSON instead of text
```
