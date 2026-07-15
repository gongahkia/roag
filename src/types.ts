export const MAP_WIDTH = 48
export const MAP_HEIGHT = 35
export const TERMINAL_WIDTH = 96
export const TERMINAL_HEIGHT = 60
export const FLOOR_COUNT = 16

export type Biome = 'mine' | 'wilds' | 'caverns' | 'ruins'
export type Direction = 'nw' | 'n' | 'ne' | 'w' | 'wait' | 'e' | 'sw' | 's' | 'se'
export type AutoplayMode = 'off' | 'visible' | 'omniscient'
export type AutoplayPolicy = 'survival' | 'clear' | 'legacy'
export type StatName = 'strength' | 'agility' | 'vitality' | 'intellect'
export type CourierOrigin = 'mineborn' | 'mosswalker' | 'cavernSeeker'
export type CourierCalling = 'trailguard' | 'pathmaker' | 'spiritbearer'
export type DeathMode = 'checkpoint' | 'ironTrail'
export type TileKind = 'wall' | 'floor' | 'exit' | 'door' | 'lockedDoor' | 'water' | 'lava' | 'pit' | 'rope' | 'spikes' | 'dart' | 'fireVent' | 'crumble' | 'boulder' | 'web' | 'gas' | 'support' | 'rail' | 'rubble' | 'bramble' | 'darkness' | 'crate' | 'chest' | 'altar' | 'shop' | 'rescue'
export type ActorRole = 'hero' | 'monster' | 'merchant' | 'ally' | 'guardian'
export type EquipmentSlot = 'mainHand' | 'offHand' | 'head' | 'body' | 'boots' | 'charm'
export type ItemId = string
export type ConditionKind = 'burning' | 'rooted' | 'staggered' | 'shielded' | 'marked' | 'slowed'
export type GuardianPhase = 'opening' | 'pressure' | 'cataclysm'
export type ObjectiveKind = 'recoverSupplies' | 'rescueScout' | 'invokeAltar' | 'defeatGuardian'
export type ObjectiveStatus = 'active' | 'complete'

export interface Point { x: number; y: number }
export interface Tile { kind: TileKind; explored: boolean; visible: boolean }
export interface ConditionState { kind: ConditionKind; duration: number; potency: number }
export interface Actor {
  id: string
  role: ActorRole
  kind: string
  name: string
  x: number
  y: number
  health: number
  maxHealth: number
  attack: number
  defense: number
  speed: number
  energy: number
  glyph: string
  color: string
  hostile: boolean
  ai?: 'chase' | 'ranged' | 'wander' | 'guardian'
  status?: string[]
  conditions?: ConditionState[]
  guardianPhase?: GuardianPhase
}

export interface GroundItem { id: ItemId; x: number; y: number; count: number; visibleInFog?: boolean }
export interface FloorObjective { id: string; kind: ObjectiveKind; status: ObjectiveStatus; label: string }
export type TelegraphDanger = 'minor' | 'major'
export interface Telegraph { id: string; sourceId: string; actionId: string; cells: Point[]; danger: TelegraphDanger; resolveTurn: number; collision?: { point: Point; by: string }; cover?: boolean }
export interface Floor {
  index: number
  biome: Biome
  seed: number
  tiles: Tile[]
  actors: Actor[]
  items: GroundItem[]
  start: Point
  exit: Point
  guardianDefeated: boolean
  objective: FloorObjective
  telegraphs?: Telegraph[]
  puzzleIds?: string[]
}

export interface Hero {
  name: string
  origin: CourierOrigin
  calling: CourierCalling
  deathMode: DeathMode
  x: number
  y: number
  health: number
  maxHealth: number
  focus: number
  maxFocus: number
  gold: number
  bombs: number
  ropes: number
  keys: number
  xp: number
  level: number
  stats: Record<StatName, number>
  skills: string[]
  inventory: ItemId[]
  equipment: Partial<Record<EquipmentSlot, ItemId>>
  lastUnequipped?: ItemId
  conditions?: ConditionState[]
  cooldowns?: Record<string, number>
}

export interface CourierIdentity { id: string; name: string; origin: CourierOrigin; calling: CourierCalling; deathMode: DeathMode; createdAt: string; parentId?: string }
export interface CourierSave { version: 1; identity: CourierIdentity; run?: RunState; checkpoint?: RunState; heir?: Hero; campaign: CampaignRouteState; records: Records; archived?: boolean }
export interface CourierMenuEntry { id: string; name: string; origin: CourierOrigin; calling: CourierCalling; deathMode: DeathMode; area?: Biome; floor?: number; turn?: number; archived?: boolean }
export interface CourierMenuView { entries: CourierMenuEntry[]; selectedId?: string; confirmingDelete?: boolean }
export interface CourierDraft { name: string; origin: CourierOrigin; calling: CourierCalling; deathMode: DeathMode; focus: 0 | 1 | 2 | 3 }

export type CampaignPhase = 'title' | 'hub' | 'area' | 'dead' | 'victory'
export type AreaStatus = 'locked' | 'available' | 'active' | 'completed'
export type LegacyCause = 'defeated' | 'sacrificed' | 'retired'

export interface AreaState {
  biome: Biome
  status: AreaStatus
  floor: number
  completed: boolean
}

export interface HubState {
  season: number
  supplies: ItemId[]
  rescued: RescuedNpc[]
  unlockedAreas: Biome[]
  completedAreas: Biome[]
}

export interface RescuedNpc { id: string; name: string; biome: Biome; floor: number }
export interface LineageEvent { id: string; kind: 'npcSacrifice'; npcId: string; npcName: string; biome: Biome; floor: number; gateId: string; seed: number }
export interface CampaignRouteState { version: 1; completedAreas: Biome[]; unlockedAreas: Biome[]; selectedBiome: Biome; rescuedNpcs: RescuedNpc[]; lineageEvents: LineageEvent[]; legacyRecords: LegacyRecord[]; legacyEncounterAreas: Biome[] }

export interface LegacyCache { gold: number; items: ItemId[] }
export interface LegacyEncounterState { kind: 'cache' | 'revenant' | 'anchor'; resolved: boolean }
export interface LegacyRecord {
  id: string
  heirName: string
  cause: LegacyCause
  biome: Biome
  floor: number
  seed: number
  lineage: string[]
  location: Point
  cache: LegacyCache
  encounter: LegacyEncounterState
}

export type EncyclopediaSection = 'enemies' | 'telegraphs' | 'tags' | 'gates' | 'legacy'
export interface EncyclopediaState { enemies: string[]; telegraphs: string[]; tags: string[]; gates: string[]; legacyRecords: LegacyRecord[] }
export type KeyBindingId = 'northwest' | 'north' | 'northeast' | 'west' | 'east' | 'southwest' | 'south' | 'southeast' | 'wait' | 'help' | 'encyclopedia' | 'settings' | 'use' | 'drop' | 'throw' | 'equip' | 'skills' | 'bomb' | 'rope' | 'get' | 'operate' | 'descend' | 'swap' | 'script'

export interface RunActions { moves: number; attacks: number; casts: number; pickups: number; bombs: number; ropes: number; rests: number }
export interface RunMetricSample { turn: number; floor: number; health: number; focus: number; gold: number; bombs: number; ropes: number; kills: number; damageDealt: number; damageTaken: number }
export interface RunFloorMetrics { floor: number; turns: number; kills: number; damageDealt: number; damageTaken: number; goldGained: number; xpGained: number; pickups: number; bombsUsed: number; ropesUsed: number }
export interface RunTelemetry { turns: number; actions: RunActions; kills: number; damageDealt: number; damageTaken: number; goldGained: number; xpGained: number; pickups: number; bombsUsed: number; ropesUsed: number; samples: RunMetricSample[]; floors: RunFloorMetrics[] }
export interface AutoplayCandidate { command: string; reason: string; score: number }
export interface AutoplayTraceEntry { turn: number; fingerprint: string; command: string; reason: string; candidates: AutoplayCandidate[]; events: string[]; nextFingerprint: string }
export type AutoplayTerminal = 'complete' | 'dead' | 'stalled' | 'turn-limit' | 'manual'
export interface AutoplayDiagnostic { id: string; date: string; seed: number; biome: Biome; floor: number; mode: Exclude<AutoplayMode, 'off'>; policy: AutoplayPolicy; outcome: AutoplayTerminal; turns: number; reason: string; trace: AutoplayTraceEntry[] }
export type RunOutcome = 'lost' | 'complete' | 'suspended'
export interface RunAnalysis { seed: number; biome: Biome; floor: number; outcome: RunOutcome; date: string; metrics: RunTelemetry }

interface CampaignBase {
  version: 2
  seed: number
  phase: CampaignPhase
  areas: AreaState[]
  hub: HubState
  legacy: LegacyRecord[]
}

export interface TitleCampaign extends CampaignBase { phase: 'title' }
export interface HubCampaign extends CampaignBase { phase: 'hub'; hero: Hero }
export interface AreaCampaign extends CampaignBase { phase: 'area'; hero: Hero; activeBiome: Biome }
export interface DeadCampaign extends CampaignBase { phase: 'dead'; legacyRecord: LegacyRecord }
export interface VictoryCampaign extends CampaignBase { phase: 'victory'; hero: Hero }
export type Campaign = TitleCampaign | HubCampaign | AreaCampaign | DeadCampaign | VictoryCampaign

export interface RunState {
  version: 2
  seed: number
  floor: Floor
  hero: Hero
  messages: string[]
  status: 'title' | 'playing' | 'dead' | 'victory'
  modal?: Modal
  turn: number
  area?: Biome
  areaFloor?: number
  gateDestination?: Biome
  rescuedNpcs?: RescuedNpc[]
  lineageEvents?: LineageEvent[]
  encyclopedia?: EncyclopediaState
  telemetry?: RunTelemetry
}

export type RunStateV1 = Omit<RunState, 'version'> & { version: 1 }

export type Modal =
  | { kind: 'help' }
  | { kind: 'encyclopedia'; section: EncyclopediaSection; page?: number }
  | { kind: 'settings'; page?: number; awaiting?: KeyBindingId }
  | { kind: 'inventory'; mode: 'use' | 'drop' | 'throw' | 'equip' }
  | { kind: 'skills'; source?: 'level' }
  | { kind: 'pause' }
  | { kind: 'shop'; merchantId: string }
  | { kind: 'gate'; gateId: string; choice?: number; confirming?: boolean }
  | { kind: 'target'; action: 'throw' | 'spell' | 'bomb'; item?: ItemId; direction?: Exclude<Direction, 'wait'> }

export interface RunRecord { seed: number; floor: number; score: number; won: boolean; date: string }
export interface Records { bestDepth: number; wins: number; deaths: number; runs: RunRecord[]; analyses: RunAnalysis[] }

export const DIRECTIONS: Record<Direction, Point> = {
  nw: { x: -1, y: -1 }, n: { x: 0, y: -1 }, ne: { x: 1, y: -1 },
  w: { x: -1, y: 0 }, wait: { x: 0, y: 0 }, e: { x: 1, y: 0 },
  sw: { x: -1, y: 1 }, s: { x: 0, y: 1 }, se: { x: 1, y: 1 }
}

export const SLOT_NAMES: Record<EquipmentSlot, string> = {
  mainHand: 'Main hand', offHand: 'Off hand', head: 'Head', body: 'Body', boots: 'Boots', charm: 'Charm'
}

export const indexOf = (x: number, y: number) => y * MAP_WIDTH + x
export const inBounds = (x: number, y: number) => x >= 0 && x < MAP_WIDTH && y >= 0 && y < MAP_HEIGHT
