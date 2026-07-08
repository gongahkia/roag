# `export-bc-examples`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Stream Tenhou XML into behavior-cloning JSONL shards.

## Inputs

Tenhou XML files or directories.

## Outputs

Shard JSONL files and `manifest.json` under `--output-dir`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku export-bc-examples data/fixtures/tenhou --output-dir runs/bc-examples --shard-size 1000 --overwrite
```

## Gotchas

- Use `--overwrite` only for disposable output directories.
- BC shards may encode source replay content; keep them ignored unless policy allows publication.

## Help

```text
usage: kenjaku export-bc-examples [-h] --output-dir OUTPUT_DIR
                                  [--actions ACTIONS]
                                  [--shard-size SHARD_SIZE]
                                  [--limit-per-type LIMIT_PER_TYPE]
                                  [--overwrite] [--skip-errors]
                                  [--parse-cache PARSE_CACHE] [--jobs JOBS]
                                  [--source-label SOURCE_LABEL]
                                  [--source-command SOURCE_COMMAND]
                                  [--source-date SOURCE_DATE]
                                  paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --output-dir OUTPUT_DIR
                        directory for ignored BC JSONL shards and
                        manifest.json
  --actions ACTIONS     comma-separated decision types to export:
                        discard,call,riichi
  --shard-size SHARD_SIZE
                        maximum examples per JSONL shard
  --limit-per-type LIMIT_PER_TYPE
                        optional maximum examples to export per decision type
  --overwrite           replace existing BC shard files in --output-dir
  --skip-errors         record parse failures and continue with successfully
                        parsed files
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
