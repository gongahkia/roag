# `browser-demo`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Generate the static browser-playable PPO policy demo and optionally serve it locally.

## Inputs

No external input; it uses fixture-safe demo assets.

## Outputs

HTML/CSS/JS assets plus `policy.json` and `manifest.json` under `--output-dir`; optionally
binds a local HTTP server.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku browser-demo --output-dir runs/browser-demo --no-serve
```

## Gotchas

- Use `--no-serve` for CI or artifact generation.
- The page embeds the same static PPO policy payload that is also written to `policy.json`, so
  opening `index.html` from disk works without `fetch`.
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
