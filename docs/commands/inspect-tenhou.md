# `inspect-tenhou`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Parse Tenhou XML inputs and print dataset-level counts.

## Inputs

Tenhou XML files or directories.

## Outputs

Text summary and optional JSON report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku inspect-tenhou data/fixtures/tenhou --report runs/inspect-tenhou.json
```

## Gotchas

- Use `--skip-errors` for mixed local datasets.
- Do not commit raw downloaded Tenhou XML.

## Help

```text
usage: kenjaku inspect-tenhou [-h] [--report REPORT] [--skip-errors]
                              [--parse-cache PARSE_CACHE]
                              [--source-label SOURCE_LABEL]
                              [--source-command SOURCE_COMMAND]
                              [--source-date SOURCE_DATE]
                              paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --report REPORT       optional path for a JSON inspection report artifact
  --skip-errors         record parse failures and continue with successfully
                        parsed files
  --parse-cache PARSE_CACHE
                        directory for opt-in content-addressed Tenhou XML
                        parse cache
  --source-label SOURCE_LABEL
                        human-readable source label recorded in JSON reports
  --source-command SOURCE_COMMAND
                        local data command or manifest reference recorded in
                        JSON reports
  --source-date SOURCE_DATE
                        source date or date range recorded in JSON reports
```
