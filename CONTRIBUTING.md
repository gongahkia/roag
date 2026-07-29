# Contributing

Kenjaku is a local-first riichi Mahjong research toolkit. Keep contributions scoped, reproducible,
and safe for public release: no raw private replay data, no downloaded model weights, and no live
service automation unless platform permission is explicit.

## Environment Setup

Use Python `>=3.11,<3.14` and [uv](https://docs.astral.sh/uv/) for environments and packages.
Local examples use Homebrew `python3.13` through uv.

```bash
uv sync --frozen --extra dev
uv run pre-commit install
uv run kenjaku --version
```

Run project commands with `uv run`; it manages and uses the project `.venv`. Do not install
project dependencies into the system Python with `pip`.

## Verification

Run the narrowest relevant tests first, then the standard checks before opening a PR:

```bash
uv run python -m unittest discover -s tests
uv run python -m compileall -q src tests scripts
uv run ruff check .
uv run pre-commit run --all-files
```

Run Pyright for the strict core/io/models scope:

```bash
uv run --extra dev-ml pyright src/kenjaku/core src/kenjaku/io src/kenjaku/models
```

## PR Expectations

There is no tracked PR template yet. PR descriptions should include:

- Linked issue or TODO ID.
- What changed and what stayed intentionally out of scope.
- Verification commands and exact results.
- Data-policy impact: whether the change touches replay input, raw logs, generated artifacts, model
  weights, live services, or redistribution boundaries.
- Public-claim impact: whether docs, reports, dashboards, or launch artifacts now make a stronger
  claim than the evidence supports.

Keep generated reports, raw Tenhou XML, SQLite databases, model checkpoints, external checkouts,
and feature caches under ignored paths such as `runs/`, `data/raw/`, and `models/`.

## Commit Conventions

For backlog/TODO work, use the project style:

```text
Complete TODO-XXX: short imperative summary
```

For GitHub issue-only work without a TODO number, use a short imperative subject such as:

```text
Add CLI command runbooks
```

Keep commits easy to revert: one issue or behavior change per commit.

## Adding a Subcommand

1. Add the parser in `build_parser()` in `src/kenjaku/cli.py`.
2. Implement the handler as a small `_command_name(args: argparse.Namespace) -> int` function.
3. Use `Path` arguments for filesystem I/O and fail fast with clear `SystemExit` messages.
4. Keep raw data and generated artifacts out of tracked paths by default.
5. Add focused tests in `tests/test_cli.py` or a new command-specific test module.
6. Add or regenerate the runbook in `docs/commands/<subcommand>.md`.
7. Run `tests/test_command_docs.py`; it checks that every argparse subcommand has a runbook.

Commands that touch external engines should use neutral files or subprocess boundaries. Do not
import AGPL/custom-licensed engine code or copy external model weights into this repository.

## Adding a Model

1. Put reusable model code under `src/kenjaku/models/`.
2. Give the model a stable versioned kind string, for example `discard-linear-defense-context-v1`.
3. Keep feature construction deterministic and colocate shared feature builders under
   `src/kenjaku/training/`.
4. Expose explicit seeds for splits, initialization, and sampling.
5. Store report/checkpoint outputs only when the caller passes an output path.
6. Add unit tests for fit/predict/evaluate behavior and serialization if supported.
7. Wire CLI training or benchmark commands only after the library path is tested.
8. Update benchmark/report docs when a new model changes public output schema.

Do not silently change the semantics of an existing model kind. Add a new kind/version instead.

## Data And Release Rules

Read `docs/data-policy.md` before changing replay ingestion, exported datasets, or public artifacts.
Read `docs/positioning.md` before making comparison claims against Mortal, kanachan,
mjai-reviewer, akochan, Mjx, MahJax, or other Mahjong AI projects.

External references checked on 2026-07-07:

- GitHub contributing guidelines: <https://docs.github.com/en/communities/setting-up-your-project-for-healthy-contributions/setting-guidelines-for-repository-contributors>
- Pyright: <https://microsoft.github.io/pyright/>
