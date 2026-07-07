# `replay-public-summary`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Build a public-safe replay summary from accepted replay intake rows.

## Inputs

Accepted JSONL rows from replay intake review.

## Outputs

Optional JSON public summary report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku replay-public-summary runs/replay-intake-accepted.jsonl --intent demo --report runs/replay-public-summary.json
```

## Gotchas

- Only summarize rows that passed the matching permission scope.
- Do not treat summaries as permission to publish raw replay URLs or XML.

## Help

```text
usage: kenjaku replay-public-summary [-h] [--intent {demo,redistribution}]
                                     [--report REPORT] [--json]
                                     accepted_items

positional arguments:
  accepted_items        JSONL rows from replay-intake-review --accepted-output

options:
  -h, --help            show this help message and exit
  --intent {demo,redistribution}
                        share intent to validate before summary generation
  --report REPORT       optional path for the public-safe JSON summary
  --json                emit the public-safe summary as JSON instead of text
```
