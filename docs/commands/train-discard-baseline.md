# `train-discard-baseline`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Fit and print the deterministic discard frequency baseline.

## Inputs

Tenhou XML files or directories.

## Outputs

Stdout summary only.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku train-discard-baseline data/fixtures/tenhou
```

## Gotchas

- Prefer `benchmark-discard` for train/eval metrics.
- Use `--skip-errors` on mixed local corpora.

## Help

```text
usage: kenjaku train-discard-baseline [-h] [--skip-errors]
                                      [--parse-cache PARSE_CACHE]
                                      [--jobs JOBS]
                                      paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --skip-errors         skip files that fail Tenhou XML parsing
  --parse-cache PARSE_CACHE
                        directory for opt-in content-addressed Tenhou XML
                        parse cache
  --jobs JOBS           parallel Tenhou XML parse worker processes
```
