# `analyze-hand`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Rank discard candidates for one hand position with a fixture-scale discard model and heuristic
interpretability overlay.

## Inputs

Compact hand notation, drawn tile, seat, round wind, optional dora indicators, and optional discard
linear checkpoint.

## Output

Text, JSON, or self-contained HTML printed to stdout.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku analyze-hand --hand "234m 567p 22s 6z 6z 6z" --drawn 1m --seat 0 --round E --dora 5p --output text
```

## Gotchas

- `1z`..`7z` map to `E S W N P F C`.
- Without `--model`, the command uses `data/fixtures/models/discard-linear-tiny.json`.
- The bundled checkpoint is a fixture smoke model, not a trained production policy.
- `--output html` prints HTML to stdout; it is not an output path.

## Help

```text
usage: kenjaku analyze-hand [-h] --hand HAND --drawn DRAWN --seat SEAT
                            --round ROUND_WIND [--dora DORA] [--model MODEL]
                            [--output {text,json,html}]

options:
  -h, --help            show this help message and exit
  --hand HAND           hand tiles, e.g. '234m 567p 22s'
  --drawn DRAWN         drawn tile
  --seat SEAT           seat 0..3
  --round ROUND_WIND    round wind: E, S, W, N, East, South, West, or North
  --dora DORA           dora indicator tile; repeat for multiple indicators
  --model MODEL         optional discard linear checkpoint JSON
  --output {text,json,html}
                        output format
```
