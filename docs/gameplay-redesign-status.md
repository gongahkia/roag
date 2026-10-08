# Gameplay Redesign Status

This is a concise implementation ledger. Executable code and tests remain the
source of truth.

## Architectural decisions

- Authoritative simulation remains deterministic and action-clock driven.
- Presentation state and runtime event batches are transient and unsaved.
- `GameState`, named persistent `Region`s, glyph-backed levels, inventory,
  recipes, circuits, materials, FOV, pathfinding, and save validation remain.
- New runs enter regional play after character creation; the vessel remains
  optional content and an existing departure path.
- Final extraction, vessel role, permanent meta-progression, and long-term
  enemy-density limits remain open product decisions.

## Tranche roadmap

| Tranche | Status | Durable result |
|---|---|---|
| FOUNDATION-01 | Complete | One transient `RuntimeEventBatch` per submitted command, with ordered world-step groups. |
| WORLD-01 | Complete | Shared zero-time `begin_region(...)`; new-character flow enters Hearthford directly. |
| FOUNDATION-02 | Next | Independent presentation clock and UI-owned effects without simulation advancement. |
| WORLD-02 | Planned | Semantic regional terrain catalog with legacy glyph parity. |
| WORLD-03 | Planned | Generic terrain action and first ordinary destructible terrain. |
| DANGER-01 | Planned | Existing pressure policy behind a deterministic director boundary. |
| DANGER-02 | Planned | Fair deterministic persistent reinforcements. |
| ENGINE-01 | Planned | Transient simulation facts and bounded effects. |
| ENGINE-02 | Planned | First reusable cross-system component vertical slice. |

## Implemented seams

- `roag.runtime_events.RuntimeEventCollector`, `RuntimeEventBatch`, and
  `RuntimeEventStep` provide renderer-neutral transient command output.
- `roag.regions.begin_region` owns regional bootstrap without vessel policy or
  an action-clock cost.
- `roag.actions.depart` retains route/loadout/vessel gates and delegates shared
  regional initialization to `begin_region` before charging its existing tick.
- `roag.main._start_new_world` completes character creation, enters the active
  Region, and starts play.

## Compatibility and migrations

- Current save format: 15.
- Redesign migrations introduced: none.
- Runtime event batches and presentation state are not serialized.
- Regional maps remain glyph-backed through `Region.levels` and
  `Region.tile_changes`.

## Known deviations and baseline issues

- FOUNDATION-01 preserves legacy reducer event ownership; world-step groups are
  frequently empty until later mechanics emit step-scoped runtime events.
- Before WORLD-01 changes, focused validation already had two unrelated
  failures: character save round-trip courier selection and elite-machinery
  damage expectation. They are not part of WORLD-01.
