# `run-external-prediction-producer`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Run a separate process that converts snapshots into prediction JSONL.

## Inputs

Decision snapshot JSONL plus an external command after `--command`.

## Outputs

Prediction JSONL and optional comparison report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku run-external-prediction-producer runs/decision-snapshots.jsonl --output runs/external-predictions.jsonl --compare-report runs/external-compare.json --command /path/to/producer
```

## Gotchas

- Put `--command` last; remaining tokens become the subprocess argv.
- The subprocess receives `KENJAKU_SNAPSHOTS` and `KENJAKU_PREDICTIONS`.

## Help

```text
usage: kenjaku run-external-prediction-producer [-h] --output OUTPUT
                                                [--timeout-seconds TIMEOUT_SECONDS]
                                                [--compare-report COMPARE_REPORT]
                                                --command ...
                                                snapshots

positional arguments:
  snapshots             decision snapshot JSONL file

options:
  -h, --help            show this help message and exit
  --output OUTPUT       prediction JSONL path the external command must write
  --timeout-seconds TIMEOUT_SECONDS
                        optional subprocess timeout
  --compare-report COMPARE_REPORT
                        optional path for a decision-snapshot comparison JSON
                        report
  --command ...         external command to run; put this option last. The
                        command receives KENJAKU_SNAPSHOTS and
                        KENJAKU_PREDICTIONS in its environment
```
