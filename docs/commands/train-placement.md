# `train-placement`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Train a fixture-scale final-placement probability checkpoint from Tenhou XML round starts and final
rank labels.

## Inputs

Tenhou XML files or directories via `--data`.

## Output

Placement checkpoint JSON at `--output`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku train-placement --data data/fixtures/tenhou --output runs/placement.json
```

## Gotchas

- This is a sandbox estimate, not published placement stats.
- The estimator currently supports four-player score vectors.
- Tiny fixture-trained checkpoints are smoke artifacts only.

## Help

```text
usage: kenjaku train-placement [-h] --data DATA [DATA ...] --output OUTPUT
                               [--epochs EPOCHS]
                               [--learning-rate LEARNING_RATE] [--l2 L2]
                               [--json]

options:
  -h, --help            show this help message and exit
  --data DATA [DATA ...]
                        Tenhou XML files or directories
  --output OUTPUT       path for the placement checkpoint JSON
  --epochs EPOCHS       training epochs for the placement estimator
  --learning-rate LEARNING_RATE
                        softmax estimator learning rate
  --l2 L2               L2 regularization strength
  --json                emit the training checkpoint metadata as JSON instead
                        of text
```
