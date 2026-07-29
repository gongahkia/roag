# `repro-report`

## Purpose

Verify that a JSON report contains a provenance block matching the current checkout.

## Example

```bash
PYTHONPATH=src python3 -m kenjaku repro-report runs/discard-benchmark-local-report.json
PYTHONPATH=src python3 -m kenjaku repro-report runs/discard-benchmark-local-report.json --strict
```

## Gotchas

- `--strict` exits non-zero when provenance is missing, dirty, or mismatched.
- Reports written before provenance support will warn instead of validating.
- Generated reports should stay under ignored paths such as `runs/`.

## Help

```text
usage: kenjaku repro-report [-h] [--strict] [--json] path
```
