export const MAP_WIDTH = 48
export const MAP_HEIGHT = 35
export const TERMINAL_WIDTH = 80
export const TERMINAL_HEIGHT = 45
export const FLOOR_COUNT = 16

export type Biome = 'mine' | 'wilds' | 'caverns' | 'ruins'
export type Direction = 'nw' | 'n' | 'ne' | 'w' | 'wait' | 'e' | 'sw' | 's' | 'se'
export type StatName = 'strength' | 'agility' | 'vitality' | 'intellect'
export type TileKind = 'wall' | 'floor' | 'exit' | 'door' | 'lockedDoor' | 'water' | 'lava' | 'pit' | 'rope' | 'spikes' | 'dart' | 'fireVent' | 'crumble' | 'boulder' | 'web' | 'gas' | 'crate' | 'chest' | 'altar' | 'shop' | 'rescue'
export type ActorRole = 'hero' | 'monster' | 'merchant' | 'ally' | 'guardian'
export type EquipmentSlot = 'mainHand' | 'offHand' | 'head' | 'body' | 'boots' | 'charm'
export type ItemId = string

export interface Point { x: number; y: number }
export interface Tile { kind: TileKind; explored: boolean; visible: boolean }
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
}

export interface GroundItem { id: ItemId; x: number; y: number; count: number }
export type TelegraphDanger = 'minor' | 'major'
export interface Telegraph { id: string; sourceId: string; actionId: string; cells: Point[]; danger: TelegraphDanger; resolveTurn: number }
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
  telegraphs?: Telegraph[]
}

export interface Hero {
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
}

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
  rescued: string[]
  unlockedAreas: Biome[]
}

export interface LegacyRecord {
  id: string
  heirName: string
  cause: LegacyCause
  biome: Biome
  floor: number
  seed: number
}

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
}

export type RunStateV1 = Omit<RunState, 'version'> & { version: 1 }

export type Modal =
  | { kind: 'help' }
  | { kind: 'inventory'; mode: 'use' | 'drop' | 'throw' | 'equip' }
  | { kind: 'skills' }
  | { kind: 'shop'; merchantId: string }
  | { kind: 'target'; action: 'throw' | 'spell' | 'bomb'; item?: ItemId; direction?: Exclude<Direction, 'wait'> }

export interface RunRecord { seed: number; floor: number; score: number; won: boolean; date: string }
export interface Records { bestDepth: number; wins: number; deaths: number; runs: RunRecord[] }

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
