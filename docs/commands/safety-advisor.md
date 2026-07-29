# `safety-advisor`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Rank candidate hand tiles by heuristic riichi-defense safety signals.

## Inputs

Hand tiles, active opponent river tiles, active riichi seats, optional self seat, and output format.

## Output

Text table or JSON report.

## JSON Schema

Top-level object:

- `kind`: `kenjaku-safety-advisor-v0`.
- `hand_tiles`: normalized hand tile strings.
- `river_tiles`: normalized river tile strings.
- `seat`: self seat.
- `active_riichi_seats`: active riichi opponent seats.
- `candidates`: ranked candidate objects.

Candidate object:

- `tile`, `copies`, `safety_score`, `estimated_deal_in_risk`.
- `genbutsu`, `suji`, `kabe`, `one_chance`, `sotogawa`.
- `seen_after_riichi`, `seen_before_riichi`.
- `safety_reasons`, `danger_reasons`, `reasons`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku safety-advisor --hand "1m 7m 5p 5s E" --river "1m 4m 4p 4p 4p 4p 4s 4s 4s" --active-riichi 1 --output text
```

## Gotchas

- One `--river` string is applied to each active riichi opponent.
- `--seat` defaults to `0`; self riichi in `--active-riichi` is ignored.
- Scores are heuristic ranking signals, not calibrated deal-in probabilities.

## Help

```text
usage: kenjaku safety-advisor [-h] --hand HAND --river RIVER
                              --active-riichi ACTIVE_RIICHI [--seat SEAT]
                              [--output {text,json}]

options:
  -h, --help            show this help message and exit
  --hand HAND           candidate hand tiles
  --river RIVER         active opponent river tiles
  --active-riichi ACTIVE_RIICHI
                        comma-separated active riichi seats, e.g. 1,3
  --seat SEAT           self seat 0..3
  --output {text,json}  output format
```
