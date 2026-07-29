# ADR 0001: CLI Framework

Status: Accepted

Date: 2026-07-08

## Context

Kenjaku's CLI is currently implemented with `argparse` in `src/kenjaku/cli.py`.

Measured on 2026-07-08:

- `kenjaku --help` exposes 50 subcommands.
- `src/kenjaku/cli.py` is 9,021 lines.
- The parser has 398 `add_argument(...)` calls.
- The project has no required runtime dependencies in `pyproject.toml`.
- The command docs under `docs/commands/` are generated from the current `argparse` help surface.

Issue #16 asks whether to migrate from `argparse` to Typer. This ADR covers evaluation only; no
migration is approved in this issue.

Current upstream facts checked on 2026-07-08:

- Python documents `argparse` as a standard-library parser that reads `sys.argv`, generates help,
  and emits errors for invalid arguments.
- Click 8.4.x documents composable CLIs with arbitrary nested commands, automatic help generation,
  and lazy-loaded subcommands.
- Typer 0.26.x is type-hint driven, has command groups, shell completion, and automatic help.
- Typer 0.26.8 was verified with `uv run --with typer`; a multi-command Typer app supports direct
  test invocation through `app(args=[...], standalone_mode=False)` and `typer.testing.CliRunner`.
- Typer's current published metadata requires Python >=3.10, compatible with Kenjaku's
  `>=3.11,<3.14` range.
- Typer's dependency surface is not zero: current docs/PyPI list `rich`, `shellingham`,
  `annotated-doc`, and Windows-only `colorama`; Typer also vendors Click since 0.26.0.

## Decision

Keep `argparse` for the next CLI architecture step.

Do not migrate Kenjaku to Click or Typer now. First complete the command-package extraction
described by issue #15 while preserving the exact current CLI surface. Revisit Typer only after
`src/kenjaku/cli.py` is split into small command modules and help-output snapshots can isolate
semantic changes from framework churn.

If the revisit happens, prefer a small Typer pilot over direct Click:

- Port `status`, `safety-advisor`, and one report-writing command.
- Keep the public `main(argv) -> int` entrypoint shape during the pilot.
- Compare `--help`, exit codes, stdout/stderr, shell completion, and command docs churn.
- Accept migration only if the pilot proves materially lower maintenance cost without broad user
  surface drift.

## Options

### Option A: Keep `argparse`, Extract Command Modules First

Retain `argparse` but move command registration and handlers out of `cli.py`.

Pros:

- Zero new runtime dependency.
- Preserves current help text, exit behavior, and tests.
- Lowest risk for a 50-command CLI with generated command docs.
- Lets issue #15 reduce the real onboarding problem without changing the parser framework.
- Keeps `main(argv) -> int` simple for existing unit and subprocess tests.

Cons:

- Still verbose for heavily typed commands.
- Manual `add_argument(...)` blocks remain.
- Shell completion and rich help require custom work or third-party tooling.

### Option B: Migrate Directly to Click

Use Click decorators and groups directly.

Pros:

- Mature CLI framework with nested commands and automatic help.
- Good fit for lazy-loading command groups after command-package extraction.
- Smaller dependency footprint than full Typer.

Cons:

- Introduces a runtime dependency.
- More decorator-specific CLI syntax for contributors to learn.
- Less direct type-hint leverage than Typer.
- Would still require large help-output and test churn.

### Option C: Migrate Directly to Typer

Use Typer command functions with type annotations.

Pros:

- Best fit if Kenjaku wants type-hint-driven command declarations.
- Good editor support for command signatures.
- Built-in richer help and shell completion.
- Verified direct test invocation is possible for multi-command apps.

Cons:

- Adds runtime dependencies to a currently dependency-free base install.
- Help output formatting differs materially from `argparse`, so command docs and exact help tests
  would churn.
- Boolean flags, repeated values, `argparse.REMAINDER`, hidden defaults, and existing validation
  need careful one-by-one mapping.
- Typer 0.26.x vendors Click; Click-specific extension points are no longer the same as depending
  on Click directly.
- Migrating before issue #15 would combine module extraction, behavior preservation, and framework
  migration in one high-risk diff.

## Test Compatibility

Current tests call `kenjaku.cli.main([...])`, redirect stdout/stderr, and also exercise subprocess
entrypoints. Typer can support the direct-call pattern with `app(args=[...], standalone_mode=False)`
for multi-command apps, and `CliRunner` is available. That is compatible enough for a future pilot.

It is not drop-in compatible with the current exact help text. A migration would require help
snapshot updates and explicit tests for exit codes, stderr formatting, and command docs generation.

## Recommendation

Use Option A now.

Estimated work:

- Issue #15 `argparse` command-package extraction: 2-4 engineering days, mostly mechanical but
  high-regression because every command must preserve help and defaults.
- Typer pilot after issue #15: 0.5-1 day.
- Full Typer migration after a successful pilot: 3-5 engineering days for 50 commands and 398
  parser arguments, plus 1-2 days for docs/help snapshot review.

[Inference] Migrating to Typer before command-package extraction would increase regression risk
because it changes both ownership boundaries and parser semantics in the same diff.

## Consequences

- New CLI work should keep using `argparse` until issue #15 or a later framework ADR supersedes
  this decision.
- Command handlers should continue to keep business logic in library modules where practical.
- Any future Typer issue must include a pilot and help-output diff before approving a full port.
- `pyproject.toml` remains dependency-free for base installs.

## References

- [Python argparse documentation](https://docs.python.org/3/library/argparse.html)
- [Click documentation](https://click.palletsprojects.com/en/stable/)
- [Typer documentation](https://typer.tiangolo.com/)
- [Typer release notes](https://typer.tiangolo.com/release-notes/)
- [Typer PyPI metadata](https://pypi.org/project/typer/)
