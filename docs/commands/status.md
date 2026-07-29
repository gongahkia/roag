# `status`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Print the implemented feature matrix and local environment status.

## Inputs

No positional input.

## Outputs

Stdout only.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku status --json
```

## Gotchas

- Use this before reporting support status; it is the narrowest capability source.

## Help

```text
usage: kenjaku status [-h] [--json]

options:
  -h, --help  show this help message and exit
  --json      emit status as JSON instead of text
```
