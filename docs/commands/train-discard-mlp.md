# `train-discard-mlp`

Generated from `argparse` help and reviewed on 2026-07-08.

## Purpose

Train a small PyTorch masked-logit discard MLP.

## Inputs

Tenhou XML files or directories.

## Outputs

Optional JSON report, metrics JSONL, and best checkpoint.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku train-discard-mlp data/fixtures/tenhou --epochs 1 --batch-size 4 --device cpu --report runs/discard-mlp.json --metrics-jsonl runs/discard-mlp.metrics.jsonl --checkpoint runs/discard-mlp.pt
```

## Gotchas

- Use `--device cpu` for portable smoke runs.
- No trained checkpoint is bundled by default.

## Help

```text
usage: kenjaku train-discard-mlp [-h] [--epochs EPOCHS]
                                 [--batch-size BATCH_SIZE]
                                 [--learning-rate LEARNING_RATE]
                                 [--hidden-dim HIDDEN_DIM]
                                 [--eval-fraction EVAL_FRACTION]
                                 [--split-seed SPLIT_SEED] [--seed SEED]
                                 [--device {auto,cpu,mps,cuda}]
                                 [--report REPORT]
                                 [--metrics-jsonl METRICS_JSONL]
                                 [--checkpoint CHECKPOINT] [--skip-errors]
                                 [--parse-cache PARSE_CACHE] [--jobs JOBS]
                                 [--source-label SOURCE_LABEL]
                                 [--source-command SOURCE_COMMAND]
                                 [--source-date SOURCE_DATE]
                                 paths [paths ...]

positional arguments:
  paths                 Tenhou XML files or directories

options:
  -h, --help            show this help message and exit
  --epochs EPOCHS       training epochs
  --batch-size BATCH_SIZE
                        mini-batch size
  --learning-rate LEARNING_RATE
                        AdamW learning rate
  --hidden-dim HIDDEN_DIM
                        hidden layer width
  --eval-fraction EVAL_FRACTION
                        fraction of examples reserved for deterministic
                        evaluation
  --split-seed SPLIT_SEED
                        stable seed for deterministic train/eval split
  --seed SEED           torch and dataloader random seed
  --device {auto,cpu,mps,cuda}
                        training device
  --report REPORT       optional path for a JSON training report artifact
  --metrics-jsonl METRICS_JSONL
                        optional path for per-epoch JSONL training metrics
  --checkpoint CHECKPOINT
                        optional path for the best PyTorch checkpoint artifact
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
