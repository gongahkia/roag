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
| ENGINE-01 | Complete | Transient simulation facts and bounded component effects. |
| ENGINE-02 | Complete | A crafted, placed threat sensor converts nearby courier defeats into bounded charge in a physically connected rack. |
| ENGINE-03 | Complete | A connected mass sensor spends rack charge for useful terrain power, producing additional noise and danger pressure. |

The published foundation roadmap through ENGINE-02 is complete. ENGINE-03 is
the first post-roadmap tranche, scoped from the integrated systems already in
the repository. Further tranches should continue to be chosen from playtesting
and the still-open product decisions rather than assumed here.

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
- `roag.simulation_effects` defines immutable authoritative facts, explicit
  registered reaction rules, a command-scoped fact collector, and a bounded
  deterministic resolver distinct from renderer-facing runtime events.
- The initial finite effect vocabulary contains only `gain_charge`, delegated
  through `roag.circuits.gain_charge` to an existing physical rack. Rules are
  explicit inputs, space-scoped, non-recursive, deterministically ordered, and
  capped per resolution.
- `roag.engine_components` discovers active-space reaction rules from physical
  circuits. An enabled threat-mode sensor connected to a rack reacts to an
  authoritative nearby courier defeat on the same level and grants one rack
  charge through the ENGINE-01 resolver.
- Reaction discovery is deterministic, ignores circuits in inactive spaces,
  and uses the sensor's existing threshold plus physical circuit topology.
  Multiple matching sensors may stack, while the rack's existing charge cap
  remains authoritative.
- Enemy defeat facts originate in `roag.enemy_equipment.harm_enemy`, where the
  authoritative transition to zero health occurs. Courier weapons, spells,
  manoeuvres, and their secondary strikes identify that cause; environmental
  damage does not synthesize a courier defeat.
- The vertical slice reuses existing rack/sensor recipes, physical inventory,
  placement, circuit persistence, combat, and selected-pack feedback instead
  of adding a parallel component representation.
- `TerrainActionFact` describes one validated physical terrain action without
  entering runtime presentation events or persistent state. A finite
  `spend_charge` effect delegates charge removal to `roag.circuits`, mirroring
  the existing bounded charge-gain seam.
- An active-space mass sensor connected to a charged rack can contribute one
  power to terrain work within one cell. The resolver requests only power the
  equipped tool is missing, so multiple sensors are deterministic but never
  consume charge that cannot improve the result.
- Every charge used for terrain assistance adds one unit to the action's
  existing semantic sound. The ordinary sound, alert, pressure, and danger
  paths therefore receive the machinery cost without an engine-specific
  danger branch.

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
- ENGINE-01 adds no persistent fields, migration, catalog rows, or content
  fingerprint changes. Simulation facts, collectors, rules, and resolution
  records are transient; an applied effect mutates only existing authoritative
  circuit charge through the circuit domain's bounded API.
- ENGINE-02 adds no persistent fields or migration. Sensor and rack identities,
  recipes, placement, and rack charge already serialize in format 15. The new
  circuit feedback row changes presentation content only; facts, rules, and
  resolution records remain transient and absent from saves.
- ENGINE-03 likewise adds no persistent field, migration, or mechanical
  content identity. It persists only existing authoritative outcomes: rack
  charge, terrain mutation/damage, material yield, noise consequences, and
  world time. Its new circuit feedback is presentation-only.

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
- ENGINE-03 closes one bounded engine loop rather than adding a full component
  roster: defeat -> charge -> terrain power -> additional noise/pressure. It
  does not yet add inventory-resource triggers, charge-consuming combat
  effects, equipment-hosted components, production that runs over time, or
  arbitrary content scripting.
- The DANGER-01 starting commit also reproduces the ecology water-interruption
  assertion failure (`fire` remains 1 instead of 0); it is unrelated to the
  director extraction.
