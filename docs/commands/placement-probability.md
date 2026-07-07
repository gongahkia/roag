# `placement-probability`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Estimate one seat's first-through-fourth final-placement probabilities from current scores and
round state.

## Inputs

Current four-player scores, kyoku label, optional honba/kyotaku/dealer/seat, and optional placement
checkpoint from `train-placement`.

## Output

Text summary or JSON payload with a four-value probability vector.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku placement-probability --scores 25000,25000,25000,25000 --kyoku E1 --output json
```

## Gotchas

- This is a sandbox estimate, not published placement stats.
- Without `--model`, the command uses an untrained uniform default model.
- `--output json` selects JSON formatting; it is not an output file path.

## Help

```text
usage: kenjaku placement-probability [-h] [--model MODEL] --scores SCORES
                                     --kyoku KYOKU [--honba HONBA]
                                     [--kyotaku KYOTAKU] [--dealer DEALER]
                                     [--seat SEAT] [--output {text,json}]

options:
  -h, --help            show this help message and exit
  --model MODEL         optional placement checkpoint JSON from train-
                        placement
  --scores SCORES       comma-separated current scores for seats 0..3
  --kyoku KYOKU         round label such as E1, E4, S1, or South-2
  --honba HONBA         honba count
  --kyotaku KYOTAKU     riichi stick count
  --dealer DEALER       dealer seat 0..3
  --seat SEAT           seat to estimate 0..3
  --output {text,json}  output format
```
