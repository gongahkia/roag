# `replay-share-plan`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Plan whether accepted replay intake rows can be used for a demo or redistribution.

## Inputs

Accepted JSONL rows from `replay-intake-review --accepted-output`.

## Outputs

Optional JSON shareability plan.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku replay-share-plan runs/replay-intake-accepted.jsonl --intent demo --report runs/replay-share-plan.json
```

## Gotchas

- The `--intent` must match the intended public use.
- It is a planning gate, not a downloader.

## Help

```text
usage: kenjaku replay-share-plan [-h] [--intent {demo,redistribution}]
                                 [--report REPORT] [--json]
                                 accepted_items

positional arguments:
  accepted_items        JSONL rows from replay-intake-review --accepted-output

options:
  -h, --help            show this help message and exit
  --intent {demo,redistribution}
                        share intent to validate against permission scope
  --report REPORT       optional path for the JSON shareability plan
  --json                emit the share plan as JSON instead of text
```
