# `demo`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Run the fixture quickstart and write a linked local artifact landing page.

## Inputs

Bundled fixture data under `data/fixtures/tenhou`.

## Outputs

A run directory with reports, snapshots, predictions, and a landing page.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku demo --output-dir runs/demo
```

## Gotchas

- Outputs belong under ignored paths such as `runs/`.
- The command proves fixture workflow shape, not model strength.

## Help

```text
usage: kenjaku demo [-h] [--output-dir OUTPUT_DIR]

options:
  -h, --help            show this help message and exit
  --output-dir OUTPUT_DIR
                        directory for generated demo artifacts
```
