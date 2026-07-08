# `tenhou-to-mjai`

Generated from `argparse` help and reviewed on 2026-07-08.

## Purpose

Convert Tenhou XML files into MJAI JSONL replay streams.

## Inputs

Tenhou XML files or directories.

## Outputs

One `.mjson` JSONL file per input XML in `--output`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku tenhou-to-mjai data/fixtures/tenhou --output runs/mjai
```

## Gotchas

- Fixture player names are synthetic because bundled XML fixtures do not include Tenhou `UN` names.
- Mahjong Soul conversion is out of scope.

## Help

```text
usage: kenjaku tenhou-to-mjai [-h] --output OUTPUT paths [paths ...]

positional arguments:
  paths            Tenhou XML files or directories

options:
  -h, --help       show this help message and exit
  --output OUTPUT  directory for .mjson output files
```
