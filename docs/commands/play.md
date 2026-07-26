# `play`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Generate the static local PPO gameplay surface and optionally serve it locally.

## Inputs

No external input; it uses fixture-safe gameplay assets.

## Outputs

HTML/CSS/JS assets plus `policy.json` and `manifest.json` under `--output-dir`; optionally
binds a local HTTP server.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku play --output-dir runs/play --no-serve
```

## Gotchas

- Use `--no-serve` for CI or artifact generation.
- The page embeds the same static PPO policy payload that is also written to `policy.json`, so
  opening `index.html` from disk works without `fetch`.
- Serving binds only the requested local host/port.

## Help

```text
usage: kenjaku play [-h] [--output-dir OUTPUT_DIR] [--host HOST]
                    [--port PORT] [--no-serve]

options:
  -h, --help            show this help message and exit
  --output-dir OUTPUT_DIR
                        directory for generated gameplay assets
  --host HOST           host to bind when serving the gameplay surface
  --port PORT           port to bind when serving the gameplay surface
  --no-serve            write the gameplay assets without starting an HTTP server
```
