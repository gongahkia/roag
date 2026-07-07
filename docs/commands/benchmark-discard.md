# `benchmark-discard`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Compare deterministic discard baselines on one train/eval split.

## Inputs

Tenhou XML files or directories.

## Outputs

Optional JSON benchmark and disagreement reports.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-discard data/fixtures/tenhou --models fast --epochs 3 --report runs/discard-benchmark.json
```

## Gotchas

- Use `--stream-examples` with large corpora and an example limit.
- Fixture metrics are smoke evidence, not population-level claims.

## Help

```text
usage: kenjaku benchmark-discard [-h] [--epochs EPOCHS]
                                 [--learning-rate LEARNING_RATE] [--l2 L2]
                                 [--models MODELS]
                                 [--eval-fraction EVAL_FRACTION]
                                 [--split-seed SPLIT_SEED]
                                 [--example-limit EXAMPLE_LIMIT]
                                 [--stream-examples] [--report REPORT]
                                 [--disagreements DISAGREEMENTS]
                                 [--max-disagreements MAX_DISAGREEMENTS]
                                 [--skip-errors] [--source-label SOURCE_LABEL]
                                 [--source-command SOURCE_COMMAND]
                                 [--source-date SOURCE_DATE]
                                 paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --epochs EPOCHS       linear model epochs
  --learning-rate LEARNING_RATE
                        linear model SGD learning rate
  --l2 L2               linear model L2 regularization strength
  --models MODELS       discard benchmark models: all, fast, or comma-
                        separated model names (frequency, raw_count_linear,
                        linear, risk_context_linear, defense_context_linear,
                        defense_context_v1_linear)
  --eval-fraction EVAL_FRACTION
                        fraction of examples reserved for deterministic
                        evaluation
  --split-seed SPLIT_SEED
                        stable seed for deterministic train/eval split
  --example-limit EXAMPLE_LIMIT
                        maximum discard examples to use after deterministic
                        reconstruction
  --stream-examples     collect examples file-by-file and stop at --example-
                        limit instead of retaining the full parsed dataset
  --report REPORT       optional path for a JSON benchmark report artifact
  --disagreements DISAGREEMENTS
                        optional path for a JSON model-disagreement diagnostic
                        artifact
  --max-disagreements MAX_DISAGREEMENTS
                        maximum stored examples per disagreement category
  --skip-errors         record parse failures and continue with successfully
                        parsed files
  --source-label SOURCE_LABEL
                        human-readable source label recorded in JSON reports
  --source-command SOURCE_COMMAND
                        local data command or manifest reference recorded in
                        JSON reports
  --source-date SOURCE_DATE
                        source date or date range recorded in JSON reports
```
