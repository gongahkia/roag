# `benchmark-kita`

Generated from `argparse` help and reviewed on 2026-07-08.

## Purpose

Compare deterministic Sanma kita/pass baselines on one train/eval split.

## Inputs

Tenhou XML files or directories.

## Outputs

Optional JSON Sanma kita benchmark report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-kita data/raw/sanma --report runs/kita-benchmark.json
```

## Gotchas

- Only three-player Tenhou rounds can yield Kita examples.
- Synthetic fixtures validate plumbing; real-log reports still need permitted Sanma logs.

## Help

```text
usage: kenjaku benchmark-kita [-h] [--eval-fraction EVAL_FRACTION]
                              [--split-seed SPLIT_SEED]
                              [--example-limit EXAMPLE_LIMIT]
                              [--report REPORT] [--skip-errors]
                              [--parse-cache PARSE_CACHE] [--jobs JOBS]
                              [--source-label SOURCE_LABEL]
                              [--source-command SOURCE_COMMAND]
                              [--source-date SOURCE_DATE]
                              paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --eval-fraction EVAL_FRACTION
                        fraction of examples reserved for deterministic
                        evaluation
  --split-seed SPLIT_SEED
                        stable seed for deterministic train/eval split
  --example-limit EXAMPLE_LIMIT
                        maximum Sanma kita/pass examples to use after
                        deterministic reconstruction
  --report REPORT       optional path for a JSON Sanma kita benchmark report
                        artifact
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
