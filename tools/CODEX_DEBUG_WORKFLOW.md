# ROAG Codex Debug Workflow

All developer diagnostics run in Docker. Start with `make build`; do not run
host Lua/LÖVE commands for project validation.

## Fast triage

```console
make doctor
make debug-bundle LABEL=issue-name
```

`make doctor` validates the production registry and serialized Expedition
modifier pool in memory. `make debug-bundle` writes an ignored,
machine-readable `.roag-debug/issue-name.json` containing the content report,
a real self-owned-bomb simulation, an Expedition plan, determinism checks, and
the repository revision/status. It never opens or changes active saves, meta
progression, or Campaign data.

## Narrow the failure

```console
make debug-test TEST="bomb"
make debug-scenario SCENARIO=bomb-self SEED=44001
make debug-modifier ID=expedition.passive.arc_relay STACKS=5
make debug-expedition SEED=1337 CHARACTER=expedition.conductor
make debug-determinism SEED=1337
```

- `debug-test` runs only existing test names containing the supplied text.
- `debug-scenario` runs a named authoritative simulation fixture and records
  emitted events plus presentation-safe feedback state.
- `debug-modifier` runs the real serialized modifier resolver and produces its
  nested trace, resolved stack values, and effect payloads.
- `debug-expedition` shows the deterministic encounter plan and initial reward
  offer for a seed. It unlocks characters only in its isolated in-memory
  profile.
- `debug-determinism` repeats core diagnostics and compares canonical output.

Each focused command writes a canonical JSON report under `.roag-debug/`:
`content.json`, `scenario.json`, `modifier.json`, `expedition.json`, or
`determinism.json`. This folder is ignored by Git and excluded from packages.

## Full confidence checks

```console
make test
make package
```

`make test` is the complete LuaJIT suite. `make package` creates and
integrity-checks `roag.love` only after source changes are ready.

## Graphical confirmation

```console
make play
make studio
make sprite-editor
make generation-inspector
make room-editor
```

These require WSLg/X11 forwarding. A graphical observation is evidence for
feel/readability; it does not replace the headless report or regression test.

## Adding a repro

Add a named scenario to `src/tools/debug_cockpit.lua`, using isolated session
or Expedition state and a compact serializable report. Add an automated case
to `tests/test_debug_cockpit.lua`. Do not make a repro open real saves or
persist debug state. If a bug has a concise test predicate, add it to the
normal suite and use `make debug-test` for its fast iteration loop.
