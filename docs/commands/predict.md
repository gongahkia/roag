# `predict`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Run batch inference over decision snapshot JSONL and emit prediction rows keyed by `row_id`.

## Inputs

Decision snapshot JSONL, model type, optional checkpoint, and output path. `frequency` can run
without a checkpoint; learned models require `--checkpoint`.

## Outputs

Prediction JSONL at `--output`, using the same row shape as `produce-decision-predictions`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku predict --snapshots runs/decision-snapshots.jsonl --model frequency --output runs/decision-predictions.jsonl
```

## Gotchas

- Model-specific discard/call/riichi predictors skip incompatible snapshot decision types.
- `mlp-discard` and `transformer-discard` require PyTorch and their matching checkpoint formats.
- Discard predictions choose a tile, not whether the discard is tsumogiri.

## Help

```text
usage: kenjaku predict [-h] --snapshots SNAPSHOTS
                       --model {frequency,linear-discard,linear-call,linear-riichi,linear-deal-in,mlp-discard,transformer-discard}
                       [--checkpoint CHECKPOINT] --output OUTPUT
                       [--device DEVICE] [--batch-size BATCH_SIZE]

options:
  -h, --help            show this help message and exit
  --snapshots SNAPSHOTS
                        decision snapshot JSONL file
  --model {frequency,linear-discard,linear-call,linear-riichi,linear-deal-in,mlp-discard,transformer-discard}
                        model family to run
  --checkpoint CHECKPOINT
                        model checkpoint path; required except for frequency
  --output OUTPUT       prediction JSONL output path
  --device DEVICE       PyTorch inference device for mlp-discard or
                        transformer-discard
  --batch-size BATCH_SIZE
                        PyTorch inference batch size
```
