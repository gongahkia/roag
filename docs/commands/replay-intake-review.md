# `replay-intake-review`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Validate a permission-aware replay intake manifest before local analysis.

## Inputs

A JSON manifest with replay provenance and permission fields.

## Outputs

Optional full JSON report and accepted-item JSONL output.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku replay-intake-review path/to/replay-manifest.json --report runs/replay-intake-review.json --accepted-output runs/replay-intake-accepted.jsonl
```

## Gotchas

- This does not fetch live services or prove legal rights beyond the manifest fields.
- Keep accepted output under ignored local paths if it contains sensitive provenance.

## Help

```text
usage: kenjaku replay-intake-review [-h] [--report REPORT]
                                    [--accepted-output ACCEPTED_OUTPUT]
                                    [--json]
                                    manifest

positional arguments:
  manifest              JSON replay manifest to review

options:
  -h, --help            show this help message and exit
  --report REPORT       optional path for the full JSON intake review report
  --accepted-output ACCEPTED_OUTPUT
                        optional JSONL output for accepted replay intake items
  --json                emit the review as JSON instead of text
```
