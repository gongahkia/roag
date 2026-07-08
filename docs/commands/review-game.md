# `review-game`

Generated from `argparse` help and reviewed on 2026-07-08.

## Purpose

Render a per-player offline review HTML report from one Tenhou XML.

## Inputs

One Tenhou XML file, a player seat, and either `frequency` or a discard linear checkpoint path.

## Outputs

One standalone HTML report.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku review-game data/fixtures/tenhou/events_4p.xml --player 0 --model frequency --output runs/review.html
```

## Gotchas

- Expected point impact and deal-in risk are heuristic review signals, not calibrated engine values.
- `--model frequency` needs no checkpoint; any other value is treated as a discard linear checkpoint path.

## Help

```text
usage: kenjaku review-game [-h] --player PLAYER --model MODEL --output OUTPUT
                           input_xml

positional arguments:
  input_xml        Tenhou XML file

options:
  -h, --help       show this help message and exit
  --player PLAYER  seat id to review
  --model MODEL    frequency or discard linear checkpoint path
  --output OUTPUT  HTML report path
```
