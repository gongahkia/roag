# `serve`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Generate `index.html` for a local artifact directory and optionally serve it over HTTP.

## Inputs

An artifact directory such as `runs/`; missing directories are created.

## Outputs

An auto-generated `index.html` linking discovered artifacts with file-type labels; optionally binds a local HTTP server rooted at the artifact directory.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku serve --dir runs
```

## Gotchas

- Use `--no-serve` in CI to write the landing page without blocking on the HTTP server.
- The generated root `index.html` is replaced each run and is not linked to itself.
- The server is local by default: `--host 127.0.0.1 --port 8766`.

## Help

```text
usage: kenjaku serve [-h] [--dir DIRECTORY] [--host HOST] [--port PORT]
                     [--title TITLE] [--no-serve]

options:
  -h, --help       show this help message and exit
  --dir DIRECTORY  artifact directory to index and serve
  --host HOST      host to bind when serving artifacts
  --port PORT      port to bind when serving artifacts
  --title TITLE    landing page title
  --no-serve       write index.html without starting an HTTP server
```
