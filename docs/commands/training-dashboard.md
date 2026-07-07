# `training-dashboard`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Build a static training dashboard from one or more epoch metrics JSONL files.

## Inputs

Training metrics JSONL files with one JSON object per epoch/update/step.

## Output

Static HTML dashboard at `--output` with SVG line charts and a sortable run table.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku training-dashboard runs/*.metrics.jsonl --output runs/dashboards/training/index.html
```

## Gotchas

- Rows must include a numeric `epoch`, `update`, `step`, or `iteration`.
- Metrics can be top-level numeric fields or nested under `metrics`.
- Hyperparameters can be nested under `hyperparameters`, `hparams`, `params`, or `config`.
- The dashboard is self-contained and does not require browser network access.

## Help

```text
usage: kenjaku training-dashboard [-h] --output OUTPUT [--title TITLE]
                                  metrics_jsonl [metrics_jsonl ...]

positional arguments:
  metrics_jsonl    training epoch metrics JSONL files

options:
  -h, --help       show this help message and exit
  --output OUTPUT  path to the static HTML dashboard to write
  --title TITLE    dashboard page title
```
