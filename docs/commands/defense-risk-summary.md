# `defense-risk-summary`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Summarize heuristic discard danger scores from Tenhou XML decisions.

## Inputs

Tenhou XML files or directories.

## Outputs

Text or JSON summary, plus optional report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku defense-risk-summary data/fixtures/tenhou --report runs/defense-risk.json --json
```

## Gotchas

- The heuristic is not a calibrated deal-in probability estimator.
- Use `--skip-errors` on noisy local corpora.

## Help

```text
usage: kenjaku defense-risk-summary [-h] [--report REPORT] [--json]
                                    [--skip-errors]
                                    [--parse-cache PARSE_CACHE] [--jobs JOBS]
                                    [--source-label SOURCE_LABEL]
                                    [--source-command SOURCE_COMMAND]
                                    [--source-date SOURCE_DATE]
                                    paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --report REPORT       optional path for a JSON defense-risk summary report
  --json                emit the summary as JSON instead of text
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
