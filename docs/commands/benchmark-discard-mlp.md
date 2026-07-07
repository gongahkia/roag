# `benchmark-discard-mlp`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Compare a PyTorch discard MLP against discard baseline anchors.

## Inputs

Tenhou XML files or directories.

## Outputs

Optional JSON report and MLP checkpoint.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-discard-mlp data/fixtures/tenhou --epochs 1 --batch-size 4 --device cpu --linear-epochs 1 --report runs/discard-mlp-benchmark.json
```

## Gotchas

- Use CPU for portable verification.
- Anchor settings affect comparison cost and runtime.

## Help

```text
usage: kenjaku benchmark-discard-mlp [-h] [--epochs EPOCHS]
                                     [--batch-size BATCH_SIZE]
                                     [--learning-rate LEARNING_RATE]
                                     [--hidden-dim HIDDEN_DIM]
                                     [--linear-epochs LINEAR_EPOCHS]
                                     [--linear-learning-rate LINEAR_LEARNING_RATE]
                                     [--linear-l2 LINEAR_L2]
                                     [--eval-fraction EVAL_FRACTION]
                                     [--split-seed SPLIT_SEED] [--seed SEED]
                                     [--device {auto,cpu,mps,cuda}]
                                     [--checkpoint CHECKPOINT]
                                     [--report REPORT] [--skip-errors]
                                     [--source-label SOURCE_LABEL]
                                     [--source-command SOURCE_COMMAND]
                                     [--source-date SOURCE_DATE]
                                     paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --epochs EPOCHS       MLP training epochs
  --batch-size BATCH_SIZE
                        MLP mini-batch size
  --learning-rate LEARNING_RATE
                        MLP AdamW learning rate
  --hidden-dim HIDDEN_DIM
                        MLP hidden layer width
  --linear-epochs LINEAR_EPOCHS
                        linear anchor training epochs
  --linear-learning-rate LINEAR_LEARNING_RATE
                        linear anchor SGD learning rate
  --linear-l2 LINEAR_L2
                        linear anchor L2 regularization strength
  --eval-fraction EVAL_FRACTION
                        fraction of examples reserved for deterministic
                        evaluation
  --split-seed SPLIT_SEED
                        stable seed for deterministic train/eval split
  --seed SEED           torch and dataloader random seed
  --device {auto,cpu,mps,cuda}
                        training device
  --checkpoint CHECKPOINT
                        optional path for the best PyTorch checkpoint artifact
  --report REPORT       optional path for a JSON MLP benchmark report artifact
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
