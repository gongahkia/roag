# `replay-viewer`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Render a self-play match trajectory JSONL file as a turn-by-turn HTML viewer.

## Inputs

Trajectory JSONL from `self-play-match-sandbox --trajectory-jsonl`.

## Outputs

Self-contained HTML at `--output` with a timeline scrubber, action annotation, board state, discards, and per-player hand reveal toggles.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku replay-viewer runs/self-play-match-trajectory.jsonl --output runs/self-play-viewer.html
```

## Gotchas

- The viewer is for generated sandbox trajectories, not raw Tenhou or Mahjong Soul replay files.
- Hand tiles are present in the generated trajectory JSONL; use the reveal toggles before sharing screenshots.

## Help

```text
usage: kenjaku replay-viewer [-h] --output OUTPUT [--title TITLE]
                             trajectory_jsonl

positional arguments:
  trajectory_jsonl  self-play trajectory JSONL from self-play-match-sandbox

options:
  -h, --help        show this help message and exit
  --output OUTPUT   HTML viewer output path
  --title TITLE     HTML document title
```
