# `browser-demo`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Generate the static browser-playable demo and optionally serve it locally.

## Inputs

No external input; it uses fixture-safe demo assets.

## Outputs

HTML/CSS/JS assets under `--output-dir`; optionally binds a local HTTP server.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku browser-demo --output-dir runs/browser-demo --no-serve
```

## Gotchas

- Use `--no-serve` for CI or artifact generation.
- Serving binds only the requested local host/port.

## Help

```text
usage: kenjaku browser-demo [-h] [--output-dir OUTPUT_DIR] [--host HOST]
                            [--port PORT] [--no-serve]

options:
  -h, --help            show this help message and exit
  --output-dir OUTPUT_DIR
                        directory for generated demo assets
  --host HOST           host to bind when serving the demo
  --port PORT           port to bind when serving the demo
  --no-serve            write the demo assets without starting an HTTP server
```
