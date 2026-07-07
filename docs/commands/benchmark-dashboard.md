# `benchmark-dashboard`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Build a static public dashboard from benchmark JSON reports.

## Inputs

Benchmark report JSON files.

## Outputs

Static HTML dashboard at `--output`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-dashboard runs/discard-benchmark.json --output runs/benchmark-dashboard/index.html --title "Kenjaku Fixture Benchmark"
```

## Gotchas

- The dashboard reflects only supplied reports.
- Use public-safe reports for any committed or shared dashboard.

## Help

```text
usage: kenjaku benchmark-dashboard [-h] --output OUTPUT [--title TITLE]
                                   reports [reports ...]

positional arguments:
  reports          benchmark report JSON files

options:
  -h, --help       show this help message and exit
  --output OUTPUT  path to the static HTML dashboard to write
  --title TITLE    dashboard page title
```
