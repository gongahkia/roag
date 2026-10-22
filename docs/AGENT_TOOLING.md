# Agent development workflow

Implementation plan (2026-10-23). This extends `./dev` without changing gameplay, save compatibility or adding an LLM/API dependency. The tools must work for any coding agent and for a person at a terminal. Completed commands and verification evidence will replace this planning section as milestones land.

## Required outcomes

- [ ] Concise JSON discovery, environment diagnosis, repository context and conservative change-impact guidance.
- [ ] Durable verification runs with logs, source fingerprints, process identity, progress, bounded failure summaries, cancellation and reconnect after a terminal/session closes. Missing graphical prerequisites must never count as passing.
- [ ] Validated content patches with dry runs, precise diffs, conflict detection and unchanged files on validation failure.
- [ ] Declarative deterministic game reproductions, step traces, assertions, exact save/reload checks and readable save diagnostics. No player-save mutation.
- [ ] Persistent task requirements, handoff notes and completion checks tied to current successful verification evidence; stale/missing/failed evidence must prevent completion.
- [ ] Real workflow regression tests, CI checks, documentation and dogfooding on this milestone. Preserve the existing developer commands and use cohesive local commits.

## Research and decisions

- [Anthropic: effective harnesses for long-running agents](https://www.anthropic.com/research/effective-harnesses-for-long-running-agents): carry explicit feature criteria and progress between sessions, and verify actual end-to-end behavior before declaring completion. Applied here as a task record and executable verification, with Git history as context.
- [Anthropic: writing effective tools for agents](https://www.anthropic.com/research/writing-tools-for-agents): evaluate tools on real tasks and return relevant, bounded information. Applied as short JSON summaries, drill-down logs and regression workflows rather than dumping complete worlds.
- [Python subprocess documentation](https://docs.python.org/3/library/subprocess.html): launch argument arrays without a shell, retain exit codes and explicitly handle timeouts/process cleanup. Applied to managed local verification workers.
- [Microsoft VSTest CLI documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest): retain TRX results and test diagnostics in an explicit results directory. Applied to durable verification artifacts instead of ephemeral `/tmp` logs.

The repository already has typed content, schema/reference validation, gameplay previews, seed audits and native mouse checks. Extend those entry points and test actual handlers. Do not invent a second game simulator, automatically alter user goals, commit/push on behalf of future tasks, or equate a passing test subset with satisfying an arbitrary requirement. Human/agent review remains necessary for scope, visual quality and game balance.
