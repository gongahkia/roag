# `transformer-attention-overlay`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Render transformer attention heatmaps from a discard transformer checkpoint.

## Inputs

A transformer checkpoint and decision snapshot JSONL.

## Outputs

HTML file at `--output`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku transformer-attention-overlay runs/fixture-discard-transformer.pt runs/decision-snapshots.jsonl --output runs/transformer-attention.html --limit 20 --device cpu
```

## Gotchas

- The checkpoint must match Kenjaku transformer checkpoint format.
- Use `--device cpu` for portable verification.

## Help

```text
usage: kenjaku transformer-attention-overlay [-h] --output OUTPUT
                                             [--limit LIMIT]
                                             [--max-heads MAX_HEADS]
                                             [--device {auto,cpu,mps,cuda}]
                                             [--title TITLE]
                                             checkpoint snapshots

positional arguments:
  checkpoint            discard transformer checkpoint path
  snapshots             decision snapshot JSONL file

options:
  -h, --help            show this help message and exit
  --output OUTPUT       HTML output path
  --limit LIMIT         maximum discard decisions to render
  --max-heads MAX_HEADS
                        maximum attention heads per layer to render
  --device {auto,cpu,mps,cuda}
                        checkpoint inference device
  --title TITLE         HTML document title
```
