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
| FOUNDATION-02 | Complete | Independent 100 ms presentation clock, UI-owned effects, and ambient water glyph cycling. |
| WORLD-02 | Complete | Read-only semantic regional terrain catalog with strict legacy glyph parity. |
| WORLD-03 | Complete | Generic physical terrain actions, sparse damage, and destructible reeds/mud. |
| DANGER-01 | Complete | Existing pressure policy behind deterministic evaluated danger actions. |
| DANGER-02 | Complete | Fair deterministic persistent regional reinforcements. |
| ENGINE-01 | Next | Transient simulation facts and bounded effects. |
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
- `roag.presentation.EffectState` owns transient presentation time and ambient
  glyph composition without retaining or mutating `GameState`.
- `roag.terminal._play_loop` uses 100 ms input timeouts only for the unobscured
  main view. Modal views remain blocking, and `ROAG_PRESENTATION=off` restores
  static blocking input for the whole play loop.
- `roag.terrain.TerrainDefinition` and `TerrainCatalog` resolve persisted
  regional glyphs to stable semantic identities and foundational walkability,
  sight-blocking, cover, and tag properties.
- `roag.terrain.terrain_at` resolves sparse `tile_changes` before base rows;
  regional movement, reachability, FOV, cover, and `WorldView` terrain identity
  now consume that seam while retaining legacy mechanics.
- `roag.terrain_actions.resolve_terrain_action` owns deterministic ordinary
  terrain legality, tool power, sparse damage, replacement, sound, and yields;
  authored doors, floors, controls, landmarks, and transitions remain delegated.
- `TerrainActionCommand` routes physical material-menu work through
  `GameSession`, producing `TerrainDamaged` and `TerrainChanged` runtime events
  before exactly one existing authoritative world step.
- `roag.terrain.replace_terrain` is the shared runtime mutation seam and clears
  damage belonging to the displaced terrain identity.
- `roag.danger` owns the unchanged pressure formula and immutable
  `DangerAction`/`DangerOutcome` boundary. Per-step critical escalation,
  post-command band transitions, pursuit/alert values, and authored situation
  activation now enter regional simulation through this policy seam.
- Danger evaluation is pure and deterministic; application remains synchronous
  inside the action clock. Region entry retains its historical steady situation
  regardless of initially carried valuables.
- Strained and critical pressure can now evaluate one deterministic regional
  reinforcement directive. Standard hostile archetypes arrive on reachable,
  dynamically walkable cells outside FOV and line of sight, 12--24 steps away.
- Reinforcements use stable sequence-derived IDs, ordinary `Threat` and item
  persistence, the existing 24-active/48-regional actor limits, and a 36-turn
  strained or 18-turn critical cadence. Arrivals retain the courier's last
  known position but receive no AI decision on their creation step.
- `ThreatSpawned` is a renderer-neutral, step-scoped runtime event; it is not
  persisted or used as simulation input.

## Compatibility and migrations

- Current save format: 15.
- Redesign migrations introduced: none.
- Runtime event batches and presentation state are not serialized.
- Presentation timeout frames submit no command and consume no world time or
  deterministic RNG.
- Regional maps remain glyph-backed through `Region.levels` and
  `Region.tile_changes`.
- WORLD-02 introduced no save field, migration, format bump, or mechanical
  content-fingerprint change. Unknown one-character historical glyphs retain
  the prior permissive movement/sight/cover behavior through deterministic
  fallback definitions.
- WORLD-03 added default-empty `Region.terrain_damage`; format-15 saves without
  it load unchanged, partial damage round-trips sparsely, and no save-format
  bump was required. Runtime terrain events remain transient.
- DANGER-01 added no persistent state or migration. `roag.world.pressure`
  remains a compatibility export of the new danger-domain implementation.
- DANGER-02 adds only sparse keys to existing `Region.changes` for each
  region's sequence and last-arrival turn. Existing format-15 saves default to
  no prior arrival, and spawned threats/equipment use existing serialization.
  No save-format or content-fingerprint change was required.

## Known deviations and baseline issues

- FOUNDATION-01 preserves legacy reducer event ownership; world-step groups are
  frequently empty until later mechanics emit step-scoped runtime events.
- Before WORLD-01 changes, focused validation already had two unrelated
  failures: character save round-trip courier selection and elite-machinery
  damage expectation. They are not part of WORLD-01.
- Nearby content-pack validation also exposes existing stale UI-contract count
  and terminal legacy-overlay failures; the redesign tranches do not currently
  depend on either path.
- WORLD-03 deliberately limits generic destruction to ordinary reeds, dense
  reeds, and mud. Walls, timber, fragile floors, circuit placement, collapse,
  authored features, inventory harvesting, and broader tool balance remain
  later work rather than being silently generalized.
- DANGER-01 deliberately added no actors, RNG draws, population caps, spawn
  placement, or balance changes; those arrived through the DANGER-02 boundary.
- DANGER-02 initially admits one actor per reinforcement directive and no new
  elites. Exact cadence, group composition, and longer-term population policy
  remain balance work rather than being generalized prematurely.
- The DANGER-01 starting commit also reproduces the ecology water-interruption
  assertion failure (`fire` remains 1 instead of 0); it is unrelated to the
  director extraction.
