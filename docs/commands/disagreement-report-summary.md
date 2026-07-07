# `disagreement-report-summary`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Summarize discard model-disagreement JSON reports.

## Inputs

Discard disagreement report JSON files.

## Outputs

Text or JSON summary with optional example snippets.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku disagreement-report-summary runs/discard-disagreements.json --examples 3 --tags
```

## Gotchas

- Examples come from stored report payloads; verify they are safe before sharing.
- Use `--tag` only with known deterministic disagreement tags.

## Help

```text
usage: kenjaku disagreement-report-summary [-h] [--json] [--examples EXAMPLES]
                                           [--tags]
                                           [--tag {defense_signal,efficiency_like,close_logit,active_riichi,safe_tile_candidate,no_obvious_signal}]
                                           reports [reports ...]

positional arguments:
  reports               discard disagreement report JSON files

options:
  -h, --help            show this help message and exit
  --json                emit the summary as JSON instead of text
  --examples EXAMPLES   append this many stored representative examples per
                        category in text mode
  --tags                append deterministic disagreement tag counts from
                        stored examples
  --tag {defense_signal,efficiency_like,close_logit,active_riichi,safe_tile_candidate,no_obvious_signal}
                        when rendering examples, include only stored items
                        with this deterministic tag
```
