# `benchmark-discard-transformer`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Compare a PyTorch discard transformer against discard baseline anchors.

## Inputs

Tenhou XML files or directories.

## Outputs

Optional JSON report and transformer checkpoint.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-discard-transformer data/fixtures/tenhou --epochs 1 --batch-size 2 --device cpu --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --linear-epochs 1 --report runs/discard-transformer-benchmark.json
```

## Gotchas

- Tiny fixture settings are for command verification.
- Keep checkpoint artifacts out of git unless explicitly approved.

## Help

```text
usage: kenjaku benchmark-discard-transformer [-h] [--epochs EPOCHS]
                                             [--batch-size BATCH_SIZE]
                                             [--learning-rate LEARNING_RATE]
                                             [--model-dim MODEL_DIM]
                                             [--num-heads NUM_HEADS]
                                             [--num-layers NUM_LAYERS]
                                             [--feedforward-dim FEEDFORWARD_DIM]
                                             [--dropout DROPOUT]
                                             [--value-head]
                                             [--linear-epochs LINEAR_EPOCHS]
                                             [--linear-learning-rate LINEAR_LEARNING_RATE]
                                             [--linear-l2 LINEAR_L2]
                                             [--eval-fraction EVAL_FRACTION]
                                             [--split-seed SPLIT_SEED]
                                             [--seed SEED]
                                             [--device {auto,cpu,mps,cuda}]
                                             [--checkpoint CHECKPOINT]
                                             [--report REPORT] [--skip-errors]
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
  --epochs EPOCHS       transformer training epochs
  --batch-size BATCH_SIZE
                        transformer mini-batch size
  --learning-rate LEARNING_RATE
                        transformer AdamW learning rate
  --model-dim MODEL_DIM
                        transformer hidden width
  --num-heads NUM_HEADS
                        transformer attention heads
  --num-layers NUM_LAYERS
                        transformer encoder layers
  --feedforward-dim FEEDFORWARD_DIM
                        transformer feedforward width
  --dropout DROPOUT     transformer dropout
  --value-head          enable an auxiliary scalar value head
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
  --report REPORT       optional path for a JSON transformer benchmark report
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
