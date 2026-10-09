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
| DANGER-03 | Complete | Legible pressure forecasting and cadence-bound reuse of dormant ordinary threats when reinforcement placement is blocked. |
| ENGINE-01 | Complete | Transient simulation facts and bounded component effects. |
| ENGINE-02 | Complete | A crafted, placed threat sensor converts nearby courier defeats into bounded charge in a physically connected rack. |
| ENGINE-03 | Complete | A connected mass sensor spends rack charge for useful terrain power, producing additional noise and danger pressure. |
| ENGINE-04 | Complete | Successful physical gathering emits a resource fact; a nearby connected mass sensor converts it into bounded rack charge while the item remains an ordinary recipe input. |
| ENGINE-05 | Complete | The bounded effect resolver can consume an exact quantity of real pack inventory through an explicit actor target, without touching equipped items or adding another resource model. |
| ENGINE-06 | Complete | An opt-in supply sensor atomically converts a newly fabricated physical galvanic cell into its connected rack's full 24-pulse yield. |
| WORLD-04 | Complete | Destroyed ordinary reeds and mud create persistent physical inventory yields, with deterministic ground fallback and acquisition reactions only after successful packing. |
| WORLD-05 | Complete | Atomic zero-time ground pickup is a typed command; first-time pack acquisition consistently publishes one resource fact without drop/pick reaction loops. |
| WORLD-06 | Complete | Standing timber is a sustained noisy terrain harvest, while authoritative material collapse exposes exact transient geometry and its resulting terrain mutation. |
| WORLD-07 | Complete | Destroying ordinary standing timber explicitly destabilizes its sparse material support, scheduling the existing delayed, braceable collapse consequence. |
| PRESENTATION-01 | Complete | Existing runtime-event batches drive ordered transient movement, combat, terrain, and visible threat-arrival glyph effects without affecting simulation. |
| PRESENTATION-02 | Complete | Enemy movement, positional attack warnings, and committed impacts now produce ordered world-step effects without changing AI resolution. |
| PRESENTATION-03 | Complete | Ranged releases carry exact projectile paths, while bounded authored area attacks carry exact affected cells for staged, visibility-safe ASCII motion. |
| PRESENTATION-04 | Complete | Visible water, fire, smoke, and precipitation derive restrained idle motion from presentation time without entering simulation state. |
| PRESENTATION-05 | Complete | Visible structural collapses stage bounded debris, a local ASCII shockwave, and visibility-safe one-cell camera tremble from semantic collapse geometry. |
| GAMEPLAY-01 | Complete | A deterministic headless field-loop contract crosses direct entry, physical terrain harvest, engine reaction, pressure escalation, reinforcement, collapse, and save/load continuation. |
| GAMEPLAY-02 | Complete | A deterministic generated-opening audit measures direct-start terrain, threat, production, role-loadout, and fresh-field engine access without changing balance. |
| GAMEPLAY-03 | Complete | Every selected new-run courier has physical pack-carried access to ordinary terrain work without replacing role equipment or changing load band. |

The published foundation roadmap through ENGINE-02 is complete. ENGINE-03
through ENGINE-06, WORLD-04 through WORLD-07, DANGER-03, and PRESENTATION-01
through PRESENTATION-05 and GAMEPLAY-01 through GAMEPLAY-03 are post-roadmap
tranches scoped from the integrated systems already in the repository. Further
tranches should continue to be chosen from playtesting and the still-open
product decisions rather than assumed here.

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
- `roag.danger.danger_forecast` exposes only the current pressure inputs,
  thresholds, and shared response cadence. The regional status rail now makes
  exposure, depth, noise, carried value, current score, next threshold, and an
  active response window legible without exposing hidden actors or positions.
- A due response that cannot place a new reinforcement may deterministically
  reuse the nearest ordinary non-elite dormant threat, provided the active
  actor budget has room. Reactivation shares the existing regional cadence,
  preserves the 24-active/48-regional caps, and receives no AI decision on its
  activation step.
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
- `ResourceGainedFact` carries the existing physical item ID, item kind, and
  quantity rather than introducing a second abstract resource inventory.
  Successful shore-site gathering emits it only after the item has entered the
  courier's pack; rejected or exhausted gathers emit nothing.
- A connected mass sensor within one cell can react to that acquisition and
  add one bounded charge to its rack. The acquired ingredient remains normal
  inventory and can be consumed by the existing recipe system.
- Engine rule discovery can filter registrations by the fact triggers being
  resolved. A mass sensor may therefore expose both acquisition and terrain
  reactions without irrelevant rules consuming the resolver's finite budget.
- `roag.inventory.consume_pack_items` is the authoritative all-or-nothing seam
  for engine resource spending. It consumes physical stacks in stable item-ID
  order and deliberately excludes readied, secondary, and other equipped
  locations.
- `ActorRef` lets a bounded effect target the active courier without treating
  the courier as a component source. `ConsumeResource` delegates exact pack
  mutation to inventory and records the physical item kind in the immutable
  `EffectApplication` audit result.
- Resource consumption requires the source circuit, fact, and active courier
  to share the active simulation space. Missing stock, a different actor, an
  inactive region, or an insufficient quantity is a deterministic no-op.
- `roag.circuits.load_rack_from_pack` owns the atomic physical conversion used
  by both manual rack operation and engine effects. It consumes a galvanic cell
  only when the target rack can accept the cell's entire 24-pulse yield.
- The finite `LoadRack` effect binds that conversion to one actor and one
  connected rack and records both delivered charge and physical resource spend
  in its transient application audit. It cannot partially spend a cell.
- Field sensors now have an explicit `supply` mode. A supply sensor connected
  to a rack reacts only to a nearby `resource.gained` fact for a physical
  `circuit:cell`; configuring this mode is the player's opt-in to automatic
  loading rather than a global inventory side effect.
- Successful recipe outputs placed in the active courier's pack now emit the
  same authoritative physical-acquisition fact as gathered resources. Outputs
  diverted to vessel storage, rejected crafts, and flask-content recipes do
  not emit that fact.
- Semantic terrain definitions may declare a physical item kind and quantity
  produced only when destruction completes. Ordinary reeds yield one bundle,
  dense reeds yield two, and mud yields one existing clay ingredient; partial
  terrain damage never creates inventory.
- `roag.terrain_yields.materialize_terrain_yield` is the narrow bridge from a
  completed terrain resolution to existing physical `Item` persistence. It
  packs the item when automatic placement succeeds and otherwise leaves it on
  the destroyed cell in the active Region, without rolling terrain mutation
  back for inventory capacity.
- A packed terrain yield enters the established `resource.gained` reaction
  path, so nearby mass machinery can respond. A ground yield remains inert
  until a later, explicit acquisition producer moves it into a pack.
- `AcquireGroundItemsCommand` gives `GameSession` a headless, stable-ID command
  for one or more items at the courier's feet. `roag.acquisition` validates the
  active spatial cell, transfers all requested items in stable ID order, and
  rolls the complete state back if any item cannot fit.
- `publish_packed_acquisition` is the shared explicit producer used by ground
  pickup, terrain harvesting, site gathering, and physical recipe output. It
  records the existing persistent acquisition marker before publishing
  `ResourceGainedFact`; re-picking a previously acquired item moves it but does
  not repeat the mechanical reaction.
- Ground pickup remains zero-time to preserve existing inventory behavior. It
  emits no fabricated world-step or presentation event. The terminal ground
  transfer path now submits the typed command, while container/locker repacking
  retains its existing modal implementation.
- Inventory-modal cancellation restores the item, acquisition marker, engine
  reaction, and messages together through the existing full-state inventory
  transaction.
- Standing timber (`T`) now declares timber material, cut-only interaction,
  hardness three, sound four, ground replacement, environmental fuel, and one
  existing physical `commodity:timber` yield through the semantic terrain
  catalog. A felling axe applies two power, so harvesting takes two exposed
  world actions and produces eight total noise before any machinery bonus.
- Terrain definitions may declare a bounded `support_loss_on_destroy`
  consequence independently of their portable yield. Standing timber is the
  only current terrain opting in: its completed cut removes three support from
  the existing sparse `MaterialCell`, while partial cuts, reeds, and mud do not
  alter support.
- The ordinary action step passes that zero-support cell through the existing
  material scheduler, which warns immediately and sets collapse two further
  world actions away. Existing brace work can cancel the deadline; otherwise
  the normal collapse mutation and runtime-event path commits it.
- `roag.materials.advance_materials` accepts the open world-step collector and
  emits one immutable `CollapseResolved` fact when a scheduled collapse
  commits. Its exact origin, affected origin/impact cells, severity, and stable
  result identity are transient; an actual regional glyph replacement also
  emits the established environmental `TerrainChanged` event in causal order.
- Authored landmarks, vertical links, and container cells retain their prior
  collapse protection and are identified as protected results rather than
  fabricating a terrain-change event. Collector-free material advancement
  remains mechanically identical.
- `tests.test_gameplay_redesign_loop` fixes one small deterministic Hearthford
  field and exercises the redesign as one causal sequence rather than isolated
  unit seams. A packed reed harvest charges a mass engine; that charge makes a
  timber cut faster and louder; continued timber work crosses strained
  pressure, produces a fair persistent reinforcement, and resolves delayed
  collapses through ordered runtime-event steps.
- The integrated loop forks from one equivalent state and again through a
  format-15 save checkpoint. Identical remaining commands must produce equal
  outcomes and complete authoritative state, while a newly arrived threat may
  not damage the courier on its creation step. The contract is included in
  `roag.checks fast`.
- `roag.opening_audit` constructs actual new worlds, enters Hearthford through
  `begin_region`, and measures movement distance to semantic destructible
  terrain, active threats, and the shore production site. It profiles every
  selectable courier through their real physically readied weapon and
  secondary item rather than a parallel role/loadout table.
- The opening audit computes an explicitly optimistic recipe closure from
  starting physical equipment, reachable ordinary terrain yields, regional
  gathered ingredients, and the actual portable/shore stations. This is a
  diagnostic content-access proof, not an inventory simulation or a source of
  authoritative state. Its deterministic CLI and focused test are included in
  `roag.checks fast`.
- `roag.inventory.ensure_initial_field_tool` is the new-run loadout seam between
  character creation and `begin_region`. It issues one existing physical reed
  sickle to the selected courier only when their role equipment cannot already
  cut or dig. The tool occupies ordinary pack space, retains the role's readied
  weapon and secondary item, and is marked already acquired without publishing
  an engine fact.
- `terrain_action_power` now recognizes work tools physically present in the
  active courier's pack as well as the existing synchronized weapon/gear
  mirrors. Locker, ground, lost, and other actors' tools provide no power. The
  same query drives resolution and generated-opening audit profiles.
- `roag.presentation.MapEffect` is an immutable renderer-neutral description
  of one transient glyph sequence, emphasis role, world position, start time,
  and deterministic priority. `EffectState` schedules these from
  `RuntimeEventBatch` without retaining a `GameState` reference.
- Command events are scheduled in causal order at the existing 100 ms
  presentation cadence. World-step events follow command events and retain
  their authoritative step grouping, including delayed effects after empty
  steps.
- Existing events now produce movement/retreat trails, attack motion, damage
  flashes, status and guard cues, defeat debris, item/relic pulses, terrain
  damage/change debris, and threat-arrival warnings. Overlapping effects use
  deterministic priority and production order, and the queue is bounded.
- `_draw_map` composes transient glyph and emphasis over the authoritative map
  after visibility has been resolved. All semantic event effects are
  visibility-gated, so an off-screen reinforcement event never reveals its
  spawn location through remembered terrain.
- The terminal passes outcomes from its existing typed movement, interaction,
  combat, guard, gear, retreat, terrain, and relic command paths into the same
  UI-owned `EffectState`. Effects never delay input or submit another command.
- Enemy AI resolution now passes the open command collector through its
  existing reducer without changing decisions. Committed enemy and patrol
  movement emits positional `ActorMoved` facts; exact single-cell warnings
  emit `AttackTelegraphed`; warning resolution reuses `AttackResolved` with
  optional authoritative origin and target positions.
- The presenter renders visible warning cells with a short `!` pulse, then
  distinguishes hit/neutral resolution from a quieter blocked or missed `x`.
  Attacker recoil and target impact can be composed from one semantic event.
  Existing map danger marks and chronicle text remain the durable static cues.
- Ordinary enemy ranged releases now emit one step-scoped
  `ProjectileResolved` containing the authoritative origin, marked target,
  projectile identity, outcome, and exact world path computed before the
  reducer can move a skirmisher. The presenter advances an ASCII trace along
  that embedded path at the existing 100 ms cadence and delays its impact cue
  until the trace arrives.
- `AreaTelegraphed` and `AreaResolved` carry exact cells for the existing
  floodgate sluice, wreck-cover pull, and rising-resin attacks. Warnings expose
  the whole bounded footprint immediately; resolution expands from the
  semantic origin with restrained one-frame staggering. All cells remain
  FOV-gated, and durable text plus authoritative terrain/material changes
  remain available when effects are disabled.
- `roag.presentation.AmbientCell` is a one-frame derived value rather than
  stored effect state. `EffectState.ambient_cell(...)` uses only its UI-owned
  clock, stable seed offset, world position, and immutable render context to
  cycle visible deep/shallow water, fire, and smoke glyphs.
- Hard rain, forest rain, and coast squalls add sparse, deterministic marks
  only over a conservative set of ordinary exposed terrain glyphs. Current
  visibility is required, primary entity/hazard/structure glyphs are never
  replaced, and transient semantic effects retain draw priority. Weather
  motion supplements the existing weather label/mechanics rather than being
  its only cue.
- `CollapseResolved` now schedules a four-frame high-priority debris sequence
  over its exact semantic cells and a delayed eight-cell ASCII shockwave around
  the origin. The established terrain mutation and chronicle message remain
  the durable static cues when the transient sequence ends or presentation is
  disabled.
- `roag.presentation.CameraEffect` is an immutable, bounded UI-owned viewport
  offset sequence. Collapse severity selects a finite presentation policy;
  simulation does not specify timing or camera instructions. Active camera
  effects are capped at 16 and never displace more than two cells by contract,
  while the current collapse treatment uses only one-cell offsets.
- `_draw_map` applies camera displacement only when the semantic event origin
  is in its already-computed current visibility snapshot, then clamps the
  shifted origin to map bounds. Hidden collapses cannot announce themselves
  through screen motion, and no extra FOV computation is introduced.

## Compatibility and migrations

- Current save format: 15.
- Redesign migrations introduced: none.
- Runtime event batches and presentation state are not serialized.
- Opening audit profiles are returned diagnostics only; they add no state,
  save migration, format bump, or mechanical content-fingerprint input.
- GAMEPLAY-03 uses existing format-15 `Item`, `owned_weapons`, and
  `vessel_changes` persistence. It adds no state field, migration, save-format
  bump, content row, or fingerprint change; existing saves and ordinary vessel
  departures do not receive a new item implicitly.
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
- DANGER-03 adds no persistent field or migration. Reactivation deliberately
  reuses the existing `danger:last_reinforcement_turn` cadence key and ordinary
  persisted threat status/last-known-position fields. `DangerForecast` is a
  read-only transient projection and format 15 remains current.
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
- ENGINE-04 adds no persistent field or migration. Resource facts and reaction
  results remain transient; only the already-persistent gathered item, site
  stock, rack charge, and world time survive save/load. The added circuit text
  is presentation content, and format 15 remains current.
- ENGINE-05 adds no persistent field, catalog row, or migration. Actor/effect
  references and application audits are transient. Successful consumption
  persists solely through the existing physical `Item` quantity/location
  fields and their established format-15 serialization.
- ENGINE-06 adds no persistent field or migration. The `supply` sensor mode is
  a newly accepted value in the existing serialized `CircuitCell.mode` field;
  old mode values and old saves remain valid. Only existing rack charge and
  physical item state persist, while the resource fact and effect audit remain
  transient. New circuit wording is presentation content only.
- WORLD-04 adds no persistent field, save migration, or format bump. Terrain
  yields use the existing `Item` model and deterministic `next_item_id`; the
  finite `material:reeds` item specification derives from the existing
  materials catalog, while mud reuses `ingredient:clay`. Four new material
  presentation rows affect text only, and the mechanical fingerprint remains
  unchanged.
- WORLD-05 adds no persistent field, migration, catalog row, or save-format
  change. It reuses the existing `acquired:<item-id>` entries in
  `GameState.vessel_changes` as first-acquisition identity and existing `Item`
  locations for transfer. Commands, outcomes, and resource facts remain
  transient; only ordinary pack placement, acquisition history, and bounded
  component effects persist.
- WORLD-06 adds no persistent field, migration, save-format bump, or content
  row. Historical and current `T` glyphs resolve to the new standing-timber
  behavior in place; sparse terrain damage, replacement, local material
  residue, physical timber cargo, and collapse outcomes all use existing
  format-15 state. `CollapseResolved` and its event-batch grouping are unsaved.
- WORLD-07 adds no persistent field, migration, content row, event type, or
  save-format bump. Terrain policy is catalog metadata; only the existing
  format-15 `MaterialCell.support` and `collapse_due` values persist, and old
  saved glyph rows acquire the same standing-timber rule on load.
- PRESENTATION-01 adds no persistent state, migration, catalog content, or
  runtime-event schema. `MapEffect`, the effect queue, presentation sequence,
  and elapsed clock remain UI-owned and absent from saves and deterministic
  state hashes. `ROAG_PRESENTATION=off` continues to disable both timed frames
  and event effects.
- PRESENTATION-02 adds only transient runtime-event metadata. The optional
  positions on `AttackResolved`, `AttackTelegraphed`, enemy movement events,
  and all derived effects remain unsaved. Collector-enabled and collector-free
  world advancement are tested to produce identical authoritative state;
  format 15 and content fingerprints are unchanged.
- PRESENTATION-03 likewise adds only immutable transient events and UI-owned
  effects. Embedded projectile paths and area cells are derived from existing
  authoritative geometry without RNG draws or mutation, are absent from save
  output, and require no save migration, format bump, or fingerprint change.
- PRESENTATION-04 adds no event, catalog row, persistent field, migration, or
  fingerprint change. `AmbientCell` values exist for one draw only; repeated
  precipitation/material frames do not mutate `GameState`, consume RNG, or
  rebuild authoritative world views. `ROAG_PRESENTATION=off` preserves the
  static glyph path.
- PRESENTATION-05 adds no authoritative or persistent state, migration,
  runtime-event schema, catalog row, or fingerprint change. `CameraEffect`,
  shockwave glyphs, and their elapsed frame state live solely in the existing
  UI-owned `EffectState`; disabled presentation schedules none of them.
- GAMEPLAY-01 adds no production state, mechanic, content row, migration, or
  save-format change. Its checkpoint and measurements are test-owned; the
  saved continuation contains only the existing format-15 terrain, material,
  circuit, inventory, pressure, threat, and regional state.

## Known deviations and baseline issues

- FOUNDATION-01 preserves legacy reducer event ownership; world-step groups are
  frequently empty until later mechanics emit step-scoped runtime events.
- Before WORLD-01 changes, focused validation already had two unrelated
  failures: character save round-trip courier selection and elite-machinery
  damage expectation. They are not part of WORLD-01.
- Nearby content-pack validation also exposes existing stale UI-contract count
  and terminal legacy-overlay failures; the redesign tranches do not currently
  depend on either path.
- WORLD-03 and WORLD-04 deliberately began with ordinary reeds, dense reeds,
  and mud. WORLD-06 and WORLD-07 add only ordinary standing timber plus its
  existing support/collapse consequence; walls, worked timber, fragile floors,
  circuit placement, authored features, and broader tool balance remain
  protected or delegated rather than being silently generalized.
- DANGER-01 deliberately added no actors, RNG draws, population caps, spawn
  placement, or balance changes; those arrived through the DANGER-02 boundary.
- DANGER-02 initially admits one actor per reinforcement directive and no new
  elites. Exact cadence, group composition, and longer-term population policy
  remain balance work rather than being generalized prematurely.
- DANGER-03 does not activate elites, animals, or machinery as a generic
  fallback, does not exceed the active actor budget, and does not change spawn
  intervals, pressure thresholds, damage, or pursuit speed. Patrol-sized
  arrivals, archetype weighting, and cadence tuning remain playtest decisions.
- ENGINE-03 closes one bounded engine loop rather than adding a full component
  roster: defeat -> charge -> terrain power -> additional noise/pressure. It
  does not add charge-consuming combat effects, equipment-hosted components,
  production that runs over time, or arbitrary content scripting.
- ENGINE-04 initially limited `resource.gained` to the existing shore-site
  gather producer. ENGINE-06 deliberately opted successful pack-placed recipe
  outputs into the same fact, WORLD-04 added terrain harvest, and WORLD-05 added
  first-time ground pickup. Container loot and other reward transfers still
  require explicit producer semantics; item construction itself is not a
  global event bus.
- WORLD-04 intentionally retains the environmental `MaterialCell` residue
  (`reeds` or `soil`) alongside its physical yield because fire, fuel, water,
  and collapse consume that local environmental state. The physical item is
  the portable inventory resource; the cell is not a second pack resource.
- Harvested clay already feeds existing production recipes. Harvested reeds
  are physical and saveable but have no new recipe consumer in WORLD-04; adding
  one belongs to content/balance work rather than the terrain bridge.
- PRESENTATION-03 intentionally covers ordinary ranged shots and three core
  bounded authored area attacks only. Machinery row sweeps and the broader
  frontier-elite catalogue do not yet expose stable exact area geometry, so
  this tranche does not infer it from prose, duplicate their reducers, or add
  camera shake and screen-wide effects prematurely.
- PRESENTATION-04 intentionally limits weather overlays to actual
  precipitation and does not invent fog density, wind particles, lightning,
  camera movement, or screen-wide storm flashes. Those require their own
  legibility and terminal-performance decisions.
- PRESENTATION-05 applies camera motion only to the infrequent semantic
  collapse event. It does not shake routine attacks, terrain hits, projectiles,
  weather, or every `TerrainChanged`, and it never blocks the next command.
  Map-edge clamping may deliberately damp one axis of tremble rather than draw
  outside the terminal viewport. Large structure-emergence choreography still
  needs a future semantic event with honest authoritative geometry.
- With the current mass-sensor rules, spending one rack charge to finish a
  harvest and then packing its yield restores one charge. The loop is bounded
  by finite terrain and emits the machinery's extra noise, but its net-zero
  charge economy is an explicit later balance decision rather than hidden by
  WORLD-04.
- WORLD-05 deliberately preserves zero-time inventory pickup and does not add a
  renderer-facing item-acquired event. Whether field pickup should eventually
  spend exposure is a balance decision; presentation vocabulary should be
  added only with a concrete effect consumer.
- WORLD-06 does not make walls, bridges, doors, fragile floors, vertical links,
  or landmark structures generically destructible. The new collapse event is
  renderer-neutral and currently relies on the accompanying `TerrainChanged`
  effect for ordinary presentation; a bespoke multi-cell collapse treatment
  belongs to a presentation tranche rather than this world-mechanics slice.
- WORLD-07 does not model load propagation, neighbouring supports, structural
  graphs, or arbitrary building demolition. One explicit terrain property
  connects a completed ordinary harvest to the existing bounded support model;
  authored positions retain the terrain action and collapse protection already
  enforced by their domain boundaries.
- GAMEPLAY-01 deliberately freezes architecture contracts rather than balance:
  its local arena and fitted mass circuit are deterministic fixtures, not a
  claim that normal generation should place those exact cells or that current
  noise/reinforcement cadence is final. Player-facing pacing still requires
  playtesting outside the headless contract.
- GAMEPLAY-02 deliberately measures rather than rebalances the generated
  opening. Across its stable twelve-seed diagnostic sample, destructible
  terrain begins zero to eight movement actions away, the nearest initially
  active threat is eighteen to forty-six terrain-path steps away, and the shore
  production site is sixty-three to seventy-nine steps away. Exact balance
  targets remain a playtest decision.
- Current physical start kits let only the bargemaster and carpenter act on
  generated Hearthford's ordinary destructible terrain before GAMEPLAY-03.
  The selected new-run courier now receives a lightweight physical tool only
  when needed, closing that access gap without redistributing role equipment.
  Even an optimistic closure over starting equipment, terrain yields, regional
  gathering, and shore stations still cannot make both a circuit rack and
  sensor for any starting role because required commodity inputs have no
  fresh-field source in that opening.
- GAMEPLAY-03 does not grant a tool to every household member, alter existing
  role loadouts, make locker tools remotely usable, change terrain hardness or
  yield, or add a circuit recipe. Death and item loss remain physical: the
  issued tool can be dropped, recovered, transferred, or lost like any other
  inventory item.
- PRESENTATION-01 consumes only runtime events already emitted by typed command
  paths. Legacy spell, manoeuvre, thrown-device, enemy-action, and other direct
  reducer paths do not receive inferred animation from messages. Camera shake,
  projectile traces, actor displacement, large world events, and new semantic
  event vocabulary remain later vertical slices.
- Presentation frames do not gate authoritative input. Rapid commands can
  overlap or supersede transient effects; this preserves the action-clock
  contract rather than turning animation duration into a gameplay cooldown.
- PRESENTATION-02 intentionally covers exact single-cell warnings and ordinary
  committed enemy attacks. Multi-cell machinery sweeps, environmental elite
  releases, projectile paths, camera displacement, and synthesized
  `DamageApplied` events for enemy damage need dedicated semantic events rather
  than message parsing or guessed geometry.
- ENGINE-05 intentionally registers no live component rule. The repository has
  no existing component whose honest benefit warrants consuming a particular
  carried resource, so the tranche establishes the safe mutation boundary
  without adding a wasteful proof mechanic. A later vertical slice must pair
  consumption and benefit atomically rather than using two independent rules.
- ENGINE-06 closes that atomic vertical slice specifically for the already
  established galvanic-cell/rack conversion. It does not introduce arbitrary
  resource-to-resource recipes, background production, equipment-hosted
  triggers, or automatic reactions to container loot and rewards.
- The DANGER-01 starting commit also reproduces the ecology water-interruption
  assertion failure (`fire` remains 1 instead of 0); it is unrelated to the
  director extraction.
