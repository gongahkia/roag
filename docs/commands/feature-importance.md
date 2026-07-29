# `feature-importance`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Rank linear-model features from a benchmark report.

## Inputs

Benchmark JSON containing weight and feature summary blocks.

## Outputs

Feature-importance JSON at `--output`.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku feature-importance runs/fixture-discard-benchmark.json --profile DEFENSE_CONTEXT --top-k 20 --output runs/feature-importance.json
```

## Gotchas

- Choose a profile that exists in the input report.
- This reads report weights; it does not retrain models.

## Help

```text
usage: kenjaku feature-importance [-h] --profile PROFILE [--top-k TOP_K]
                                  --output OUTPUT
                                  model

positional arguments:
  model              benchmark report JSON with weight_summary and
                     feature_summary blocks

options:
  -h, --help         show this help message and exit
  --profile PROFILE  linear profile/model: RISK_CONTEXT, DEFENSE_CONTEXT,
                     raw_count, deal_in
  --top-k TOP_K      maximum ranked features to emit
  --output OUTPUT    feature-importance JSON output path
```
