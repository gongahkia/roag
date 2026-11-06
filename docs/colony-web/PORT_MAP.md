# Mechanic preservation and port map

Baseline: `archive/pre-web-colony-2026-11-06`, commit
`4d89585c13cfcb6874e48a0d8ef64c3a2a1001df`. Paths below are repository-relative.
After runtime retirement, read them from that tag or the complete source archive.

This is selective behavioral reuse, not a promise of parity with every old feature.
Keep new TypeScript independent of MonoGame, generated rules, Lua, and the legacy
save format. Read the source and corresponding tests before implementing a row.
If a method's assumptions differ from the colony design, record the intentional
difference and test the new behavior instead of inheriting it accidentally.

## Core, world, and logistics

| System / phase | Source references | Retain | Adapt / avoid |
| --- | --- | --- | --- |
| Randomness and tick ordering / P02 | `src/Roag.Core/Tactics/RoagRandom.cs`, `src/Roag.Core/Tactics/ActionTimeline.cs`, `src/Roag.Core/World/WorldModel.cs`, `tests/Roag.Core.Tests/Tactics/ActionTimelineTests.cs` | Seeded streams, deterministic ordering, save pending events, stable ties. | Replace waiting for a selected party member with a fixed real-time tick. Use explicit unsigned arithmetic; do not assume JS numbers implement C# integer overflow. |
| Tile world / P02 | `src/Roag.Core/World/WorldModel.cs`, `ChunkGenerator.cs`, `StructuralLayouts.cs`, `OverlappingWfc.cs` in the same directory; `content/generation/world.json`, `assets/world/samples.json` | Stable cells, semantic terrain, reproducible generation, bounded attempts, connectivity. | Begin with one modest surface map. Huge world streaming, multi-floor dungeon integration and generalized WFC are not prerequisites. |
| Navigation and indexing / P02 | `src/Roag.Core/World/WorldNavigation.cs`, `WorldCollision.cs`, `ActorSpatialIndex.cs`; `tests/Roag.Core.Tests/World/LocalRoutingTests.cs`, `SpatialIndexTests.cs` | Solid terrain, stable IDs, local spatial queries, blocked-route handling, invalidation on edits. | Many autonomous people need claims/reservations and bounded routing. Avoid the four-person formation assumptions. |
| Resources / P03 | `src/Roag.Core/World/WorldResources.cs`, `WorldWorkOutputs.cs`; `content/resource/`, `content/item/`; `tests/Roag.Core.Tests/World/WorkOutputTests.cs` | Finite node stock, extract once, carry actual yield, deliver only what fits. | Convert old aliases/data to selected colony definitions. No passive inventory increase disconnected from real work. |
| Work and construction / P03 | `src/Roag.Core/World/WorldWork.cs`, `WorldDesignations.cs`, `WorldNativeWork.cs`; `content/recipe/`; `tests/Roag.Core.Tests/Tactics/NativeWorkTests.cs`, `tests/Roag.Core.Tests/World/WorldDesignationTests.cs` | Orders, work cost, target validation, assignment, suspension, repair, cancellation and exactly-once completion. | Colony job board and priority UI are new. Replace party readiness/paid input with autonomous tick-driven actions. |
| Reservations and hauling / P03 | `src/Roag.Core/World/WorldWorkSupplies.cs`, `WorldWorkOutputs.cs`; `tests/Roag.Core.Tests/World/WorkSupplyTests.cs`, `WorkOutputTests.cs`, `CaravanCargoTests.cs` | Explicit stock/cargo ownership, handoff, delivered state, bounded storage, recoverable cancellation/death. | Remove global shared-pack assumptions. Name each transfer's origin and destination; prevent double reservation and lost carried material. |
| Occupations / P04 | `src/Roag.Core/World/WorldOccupations.cs`, `WorldOccupationValidation.cs`, `WorldNativeResidents.cs`; `tests/Roag.Core.Tests/World/OccupationTests.cs`, `tests/Roag.Core.Tests/Tactics/NativeResidentWorkTests.cs` | Physical farmer/woodcutter/quarrier/healer work, hauling, workplace costs, interrupted job retention. | All colonists are physical workers; do not also grant aggregate town production. Old grain nodes do not implement planting/tending. |
| Needs/routines / P04 | `src/Roag.Core/World/WorldSimulation.cs`, `WorldRoutines.cs`, `WorldResidentJourneys.cs`, `NeedTimeline.cs`; `content/simulation/shared.json`; `tests/Roag.Core.Tests/World/ResidentRoutineTests.cs`, `ResidentJourneyTests.cs` | Danger/food/rest/work ordering, actual arrival, work penalties, blocked reasons, resumed jobs. | Replace town/party-specific locations with colony beds/food/shelter. Calibrate needs to real-time pacing. |
| Medicine/downing/death / P04-P05 | `src/Roag.Core/Tactics/TacticalRevival.cs`, `TacticalResidentRescue.cs`; `src/Roag.Core/World/WorldNativeLife.cs`; `tests/Roag.Core.Tests/Tactics/EnemyRevivalTests.cs`, `NativeNpcRescueTests.cs` | Real medicine/time, persistent corpses/cargo, one-time death, limited rescue windows. | Use simulation durations, not five personal turns. All-downed is not all-dead; determine a coherent rescue/bleed-out outcome. |
| Persistence / P02 onward | `src/Roag.Core/World/WorldPersistence.cs`; `tests/Roag.Core.Tests/World/PersistenceTests.cs`, `SaveSnapshotTests.cs`, `tests/Roag.Core.Tests/Tactics/NativeWorldClockTests.cs` | Authoritative state, exact continuation, validated references, pending work, safe rejection. | IndexedDB/export replaces disk manifests/chunks. No old save conversion or filesystem dependency. |

## Combat, encounters, and world identity

| System / phase | Source references | Retain | Adapt / avoid |
| --- | --- | --- | --- |
| Combat / P05 | `src/Roag.Core/Tactics/TacticalCombat.cs`, `AttackGeometry.cs`, `TacticalEnemyTurns.cs`, `EnemyTargeting.cs`; `tests/Roag.Core.Tests/Tactics/TacticalCombatTests.cs`, `EnemyTargetingTests.cs` | Shared damage path, range/LOS, locked target cells, visible windups, cooldowns, deterministic hit/death ordering. | Many defenders act autonomously. Define friendly-fire policy per attack data; no required direct party control. |
| Alarms / P05 | `src/Roag.Core/Tactics/TacticalAlarm.cs`, `EnemyAwareness.cs`; `tests/Roag.Core.Tests/Tactics/EnemyAlarmTests.cs`; `docs/ROAG_IDEA_ENEMIES.md` | Spatial awareness, sound attenuation, explicit alarm sources, limited propagation. | Colony warning/rally/shelter behavior; avoid revealing hidden enemy positions through UI. |
| Squads / P06 | `src/Roag.Core/Tactics/EnemySquadPlanning.cs`, `EnemySquads.cs`, `TacticalSquads.cs`; `tests/Roag.Core.Tests/Tactics/EnemySquadPlanningTests.cs`, `EnemySquadWorldTests.cs`; `docs/ROAG_ENEMY_SQUADS.md` | Healer priority, shield facing, ranged lanes, support radius/LOS, luring/separation counterplay. | Attacks target colony objectives as well as defenders. Spawn new raid identities, not old dungeon rosters. |
| Pressure / P06 | `src/Roag.Core/Tactics/TacticalSpecialDungeons.cs`, `src/Roag.Core/Dungeons/`; `docs/MIGRATION_SPECIAL_DUNGEONS.md` | Saved scheduling, separate RNG, legal spawn placement, finite reinforcement identities. | Endless raid director is NEW. Discard extraction timers, invulnerable pursuers and dungeon-collapse loss conditions. |
| Four spatial bosses / P06-P07 | `src/Roag.Core/Tactics/TacticalCampaignBosses.cs`, `TacticalCampaignBossObjects.cs`, `CampaignBossState.cs`; `src/Roag.Core/World/WorldCampaignBosses.cs`; `tests/Roag.Core.Tests/Tactics/CampaignBossTests.cs`; `docs/MIGRATION_BOSS_PROPOSALS.md` | Bellkeeper sound/rings, Sluice-Maw channels/valves, Threadmother webs/pulls/fire, Captain facing/charge/barricades. | Geometry must fit a colony without destroying existing player work. Boss death never ends endless play. Source tests do not prove new balance or natural placement. |
| Terrain/environment / P07 | `src/Roag.Core/Tactics/TacticalTerrain.cs`, `TacticalOptionalTerrain.cs`, `TacticalWebs.cs`, `TacticalEmbers.cs`; `docs/ROAG_TERRAIN_EXPANSION.md`, `ROAG_IDEA_TERRAINS.md`, `ROAG_BIOME_EXPANSION.md` | Blocking/sight/projectile distinctions, destructible obstacles, scheduled hazards, bounded effect chains. | New terrain must work for workers, raiders and defense placement; avoid hazards that only function in old player sight. |
| Traits/consequences / P08 | `src/Roag.Core/World/WorldConsequences.cs`, `WorldSimulation.cs`, `WorldRoutines.cs`; `content/trait/`, `content/incident/`; `tests/Roag.Core.Tests/World/ConsequenceTests.cs` | Bounded memories, actual aid/harm, stable attribution, mood/work effects, recoverable relations. | Start with simple colony consequences. Avoid personal-leader recruitment politics or complex social graphs in the first pass. |
| Climate / P08 | `src/Roag.Core/World/WorldClimate.cs`, `WorldDaylight.cs`, `WorldCalendarRest.cs`; `tests/Roag.Core.Tests/World/ClimateTests.cs`, `NativeDaylightTests.cs` | Seeded independent weather, seasons, exposure, real shelter arrival, saved recovery deadlines. | Calibrate for a small colony map; a global 4096x4096 weather grid is unnecessary. |
| Ecology / P08 | `src/Roag.Core/World/WorldHabitats.cs`, `WorldWildlifeMigration.cs`, `WorldWildlifePopulations.cs`; `tests/Roag.Core.Tests/World/HabitatTests.cs`, `WildlifeRecoveryTests.cs` | Finite forage, land pressure, identity-preserving recovery, no duplicate loot, bounded retained history. | Limited local wildlife first. Large migration networks and full ecosystem balance remain expansion candidates. |
| Trade / P08 | `src/Roag.Core/World/WorldEconomy.cs`, `WorldTrade.cs`; `tests/Roag.Core.Tests/Tactics/NativeTradeTests.cs`, `tests/Roag.Core.Tests/World/CaravanCargoTests.cs` | Finite cargo/stocks, prices, conservation, transaction validation. | Local visiting trader; regional town production and continent routes are deferred. |
| Performance / P09 | `src/Roag.Core/World/ActorSpatialIndex.cs`, `WorldBodySchedule.cs`; `docs/STRESS_REVIEW.md`, `PERFORMANCE_REVIEW.md` | Bounded searches/history, cadence separation, representative active workloads, honest measurement scope. | No automatic adoption of C# threading or claims that synthetic 10,000-actor queries prove active AI capacity. |

Paths listed without their full directory in a row share the directory of that row's
preceding explicit source/test path. Inspect any ambiguous reference before copying.

## Preserve as authored data, then select

- `content/` contains the 48 legacy boss definitions, item/resource/recipe/ability
  definitions, traits, dialogue, incidents, encounter/reward tables and names. Keep
  the original data unchanged; copy/adapt selected definitions to `web/content/` with
  an origin ID and a note about changed meaning, timing or costs.
- `assets/tactics/enemies.json` (50 definitions), `boons.json` (103 definitions), and
  `dungeons.json` (147 room definitions, seven biome themes) remain source libraries.
  They are not automatically runtime-loaded content or first-release promises.
- `docs/sprite-audit/`, `docs/room-review/`, `src/rooms.lua`, `src/room_expansion.lua`,
  and `tools/room_prototypes.py` contain human-reviewed layouts/choices. The last three
  are safe in the complete legacy archive even after retiring old runtime files.
- Art identity is independent of role: a sprite used as a boss now may represent a
  colonist/prop later without changing its original neutral catalog description.

## New systems with no complete legacy implementation

Do not label these as completed ports just because related C# exists:

1. Colony heart objective, its destruction and combined defeat semantics.
2. A colony-wide job priority UI for more than four independently managed inhabitants.
3. Deliberate farming/planting/tending and colony-specific production chains.
4. Player-buildable towers with ammunition production/hauling, traps and guard posts.
5. Raid objective selection, obstacle breaching, and defense against constructed mazes.
6. Endless wave composition, escalation, recovery pacing and survival statistics.
7. Browser storage/error recovery, tab visibility behavior, static hosting and web input.

## Keep out of the production TypeScript graph

`src/Roag.Reference/`, generated translated rules, Lua runtimes, the C# client,
MonoGame/OpenGL adapters, desktop automation, disk save APIs, campaign import adapters,
and the old four-person FFX-like control loop belong to the historical implementation.
Do not emulate these just to make old tests pass. Reference tests constrain retained
behavior, not the new game's architecture.

The implementer should add a short per-system record to its phase report:
source -> invariant retained -> behavior changed -> new test/scenario -> playable evidence.
