# `export-decision-snapshots`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Export neutral decision snapshots for shared offline prediction/evaluation protocols.

## Inputs

Tenhou XML files or directories.

## Outputs

JSONL decision snapshots at `--output`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku export-decision-snapshots data/fixtures/tenhou --output runs/decision-snapshots.jsonl --limit 20
PYTHONPATH=src python3.13 -m kenjaku export-decision-snapshots data/fixtures/tenhou --format mjai --output runs/decision-snapshots.mjson --limit 20
```

## Gotchas

- Terminal outcome labels are excluded unless `--include-outcome` is set.
- Use stable source metadata when comparing across producers.
- `--format mjai` emits MJAI `request_action` events with `kenjaku_meta` sidecars.

## Help

```text
usage: kenjaku export-decision-snapshots [-h] --output OUTPUT
                                         [--decision-types DECISION_TYPES]
                                         [--limit LIMIT] [--skip-errors]
                                         [--include-outcome]
                                         [--format {kenjaku,mjai}]
                                         [--parse-cache PARSE_CACHE]
                                         [--jobs JOBS]
                                         [--source-label SOURCE_LABEL]
                                         [--source-command SOURCE_COMMAND]
                                         [--source-date SOURCE_DATE]
                                         paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --output OUTPUT       JSONL path for exported decision snapshots
  --decision-types DECISION_TYPES
                        comma-separated snapshot types:
                        discard,call,riichi,kita
                        comma-separated snapshot types: discard,call,riichi
  --limit LIMIT         maximum snapshots to write after deterministic
                        ordering
  --skip-errors         record parse failures and continue with successfully
                        parsed files
  --include-outcome     include terminal score-delta labels; opt-in to avoid
                        future outcome leakage
  --format {kenjaku,mjai}
                        snapshot JSONL format
  --parse-cache PARSE_CACHE
                        directory for opt-in content-addressed Tenhou XML
                        parse cache
  --jobs JOBS           parallel Tenhou XML parse worker processes
  --source-label SOURCE_LABEL
                        human-readable source label recorded in JSON reports
  --source-command SOURCE_COMMAND
                        local data command or manifest reference recorded in
                        JSON reports
  --source-date SOURCE_DATE
                        source date or date range recorded in JSON reports
```
