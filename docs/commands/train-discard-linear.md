# `train-discard-linear`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Train a dependency-free linear discard model on reconstructed examples.

## Inputs

Tenhou XML files or directories.

## Outputs

Optional JSON model and report artifacts.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku train-discard-linear data/fixtures/tenhou --epochs 5 --output runs/discard-linear-model.json --report runs/discard-linear-report.json
```

## Gotchas

- Use fixed `--split-seed` for comparable runs.
- Reports and models belong in ignored local paths.

## Help

```text
usage: kenjaku train-discard-linear [-h] [--epochs EPOCHS]
                                    [--learning-rate LEARNING_RATE] [--l2 L2]
                                    [--eval-fraction EVAL_FRACTION]
                                    [--split-seed SPLIT_SEED]
                                    [--output OUTPUT] [--report REPORT]
                                    [--skip-errors]
                                    [--source-label SOURCE_LABEL]
                                    [--source-command SOURCE_COMMAND]
                                    [--source-date SOURCE_DATE]
                                    paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --epochs EPOCHS       training epochs
  --learning-rate LEARNING_RATE
                        SGD learning rate
  --l2 L2               L2 regularization strength
  --eval-fraction EVAL_FRACTION
                        fraction of examples reserved for deterministic
                        evaluation
  --split-seed SPLIT_SEED
                        stable seed for deterministic train/eval split
  --output OUTPUT       optional path for the trained JSON model artifact
  --report REPORT       optional path for a JSON training report artifact
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
