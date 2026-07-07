# `interpretability-overlay`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Render a paginated discard interpretability HTML page from decision snapshots.

## Inputs

Decision snapshot JSONL containing discard decisions.

## Outputs

HTML file at `--output` with 100 decisions per page plus client-side filters and search.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku interpretability-overlay runs/decision-snapshots.jsonl --output runs/interpretability-overlay.html --limit 20
```

## Gotchas

- Use `--min-decisions` when a report must fail on empty snapshot input.
- The overlay uses heuristic alternatives, not guaranteed optimal actions.
- Search matches hand pattern and dora indicators; filters cover round, seat, discard tile, and shanten-delta bin.

## Help

```text
usage: kenjaku interpretability-overlay [-h] --output OUTPUT [--limit LIMIT]
                                        [--min-decisions MIN_DECISIONS]
                                        [--title TITLE]
                                        snapshots

positional arguments:
  snapshots             decision snapshot JSONL file

options:
  -h, --help            show this help message and exit
  --output OUTPUT       HTML output path
  --limit LIMIT         maximum discard decisions to render
  --min-decisions MIN_DECISIONS
                        fail unless at least this many discard decisions
                        render
  --title TITLE         HTML document title
```
