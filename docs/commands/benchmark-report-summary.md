# `benchmark-report-summary`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Summarize one or more benchmark JSON reports.

## Inputs

Benchmark report JSON files.

## Outputs

Text or JSON summary.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-report-summary runs/discard-benchmark.json --json
```

## Gotchas

- Report schemas vary by benchmark family.
- Missing metrics usually mean the source benchmark did not produce that block.

## Help

```text
usage: kenjaku benchmark-report-summary [-h] [--json] reports [reports ...]

positional arguments:
  reports     benchmark report JSON files

options:
  -h, --help  show this help message and exit
  --json      emit the summary as JSON instead of text
```
