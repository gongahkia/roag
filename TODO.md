# Jomon — Operational Tracker

## Current State

- The active product is a standard-library Python 3.11 `curses` roguelike; the browser version is archived in Git.
- One deterministic Hearthford expedition is complete: six-adult household, physical preparation and gangplanks, three loadouts, three supports, generated material pressure, accept/refuse/alter choices, combat, negotiation, positional evasion, environmental repair, cargo, contextual defeat, succession, return, and atomic save/load.
- Automated checks cover the complete state loop. PTY play verified the keyboard UI at 100x32 and 80x24 plus the minimum-size message at 70x20.

## Now

- The project owner plays and evaluates the bounded loop for clarity, pace, and enjoyment.
- Record only demonstrated friction, unclear choices, or missing consequences.

## Next

- Improve the smallest problem demonstrated by owner play; do not expand the architecture.

## Later

- Add another compact expedition only after the current loop's decisions and return consequences are proven enjoyable.
- Add capture or rescue only if play exposes a specific contextual need.

## Frozen

- expanding-frontier, distant-person, fidelity, catch-up, delegation, and universal NPC simulation;
- eras, NG+, NG++, alternate renderers, management UI, broad content frameworks, save migrations, replay infrastructure, and speculative optimization;
- server, network, telemetry, database, plugin, ECS, generic dialogue, generic encounter, and global economy systems.

## Done

- The web v19 tree is preserved at branch `archive/web-v19` and annotated tag `jomon-web-v19-final`, both targeting `de1c1e8`.
- Browser runtime, dependencies, tests, generated assets, and active contracts were removed after the terminal loop passed tests and PTY play.
- The implemented causal chain and loop are recorded in [`docs/causal-generation-and-loop.md`](docs/causal-generation-and-loop.md).
