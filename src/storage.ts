import { isItemId, isMonsterId, isSkillId } from './content'
import { objectiveForFloor } from './objectives'
import { FLOOR_COUNT, MAP_HEIGHT, MAP_WIDTH, inBounds } from './types'
import type { Actor, Biome, CampaignRouteState, ConditionState, CourierCalling, CourierMenuEntry, CourierOrigin, CourierSave, DeathMode, EncyclopediaState, Floor, GroundItem, Hero, LegacyRecord, LineageEvent, Modal, Point, Prop, Records, RescuedNpc, RunAnalysis, RunFloorMetrics, RunMetricSample, RunState, RunTelemetry, Telegraph, Tile } from './types'
import { createRunTelemetry } from './telemetry'
import { PROP_IDS } from './props'
import { initialCampaignRoute } from './engine/campaign'

const DB = 'jomon-expedition-v2'
const STORE = 'state'
const RUN = 'active-run'
const RECORDS = 'records'
const CAMPAIGN_ROUTE = 'campaign-route'
const COURIER_INDEX = 'courier-index'
const COURIER_PREFIX = 'courier:'

type RunRecord = Omit<RunState, 'version'> & { version: number }
type UnknownRecord = Record<string, unknown>
interface CampaignRouteRecord {
  version: 1 | 2
  completedAreas: Biome[]
  unlockedAreas: Biome[]
  selectedBiome: Biome
  rescuedNpcs?: RescuedNpc[]
  lineageEvents?: LineageEvent[]
  legacyRecords?: LegacyRecord[]
}

export class SerialWriteQueue {
  private tail: Promise<void> = Promise.resolve()

  enqueue<T>(operation: () => Promise<T>): Promise<T> {
    const result = this.tail.then(operation)
    this.tail = result.then(() => undefined, () => undefined)
    return result
  }

  flush(): Promise<void> { return this.tail }
}

const isRecord = (value: unknown): value is UnknownRecord => typeof value === 'object' && value !== null && !Array.isArray(value)
const isNumber = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value)
const isInteger = (value: unknown): value is number => isNumber(value) && Number.isInteger(value)
const isNonNegativeInteger = (value: unknown): value is number => isInteger(value) && value >= 0
const isString = (value: unknown): value is string => typeof value === 'string'
const isCourierOrigin = (value: unknown): value is CourierOrigin => value === 'mineborn' || value === 'mosswalker' || value === 'cavernSeeker' || value === 'tidebound'
const isCourierCalling = (value: unknown): value is CourierCalling => value === 'trailguard' || value === 'pathmaker' || value === 'spiritbearer'
const isDeathMode = (value: unknown): value is DeathMode => value === 'checkpoint' || value === 'ironTrail'
const oneOf = <T extends string>(value: unknown, values: readonly T[]): value is T => typeof value === 'string' && values.some(current => current === value)
const isPoint = (value: unknown): value is Point => isRecord(value) && isNumber(value.x) && isNumber(value.y)
const isTile = (value: unknown): value is Tile => isRecord(value) && oneOf(value.kind, ['wall', 'floor', 'exit', 'door', 'lockedDoor', 'water', 'lava', 'pit', 'rope', 'spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'web', 'gas', 'support', 'rail', 'rubble', 'bramble', 'darkness', 'crate', 'chest', 'altar', 'shop', 'rescue']) && typeof value.explored === 'boolean' && typeof value.visible === 'boolean'
const isCondition = (value: unknown): value is ConditionState => isRecord(value) && oneOf(value.kind, ['burning', 'rooted', 'staggered', 'shielded', 'marked', 'slowed']) && isNumber(value.duration) && isNumber(value.potency)
const isActor = (value: unknown): value is Actor => isRecord(value) && isString(value.id) && oneOf(value.role, ['hero', 'monster', 'merchant', 'ally', 'guardian']) && isString(value.kind) && isString(value.name) && isNumber(value.x) && isNumber(value.y) && isNumber(value.health) && isNumber(value.maxHealth) && isNumber(value.attack) && isNumber(value.defense) && isNumber(value.speed) && isNumber(value.energy) && isString(value.glyph) && isString(value.color) && typeof value.hostile === 'boolean' && (value.role === 'monster' || value.role === 'guardian' ? isMonsterId(value.kind) : value.role === 'merchant' ? value.kind === 'merchant' : value.role === 'ally' ? value.kind === 'ally' : true) && (value.ai === undefined || oneOf(value.ai, ['chase', 'ranged', 'wander', 'guardian'])) && (value.status === undefined || (Array.isArray(value.status) && value.status.every(isString))) && (value.conditions === undefined || (Array.isArray(value.conditions) && value.conditions.every(isCondition))) && (value.guardianPhase === undefined || oneOf(value.guardianPhase, ['opening', 'pressure', 'cataclysm']))
const isGroundItem = (value: unknown): value is GroundItem => isRecord(value) && (value.id === 'gold' || isItemId(value.id)) && isInteger(value.x) && isInteger(value.y) && isNonNegativeInteger(value.count) && value.count > 0
const isProp = (value: unknown): value is Prop => isRecord(value) && isString(value.id) && oneOf(value.kind, PROP_IDS) && isNumber(value.x) && isNumber(value.y) && oneOf(value.biome, ['mine', 'wilds', 'caverns', 'ruins']) && oneOf(value.state, ['dormant', 'inspected', 'activated', 'destroyed']) && Array.isArray(value.tags) && value.tags.every(isString) && (value.hooks === undefined || (Array.isArray(value.hooks) && value.hooks.every(hook => hook === 'operate' || ['bomb', 'fire', 'water', 'root', 'force', 'throw', 'hazard', 'ward', 'gate'].includes(hook)))) && (value.effectCells === undefined || (Array.isArray(value.effectCells) && value.effectCells.every(isPoint))) && (value.expiresAt === undefined || isNumber(value.expiresAt))
const isObjective = (value: unknown): boolean => isRecord(value) && isString(value.id) && oneOf(value.kind, ['recoverSupplies', 'rescueScout', 'invokeAltar', 'defeatGuardian']) && oneOf(value.status, ['active', 'complete']) && isString(value.label)
const isRescuedNpc = (value: unknown): value is RescuedNpc => isRecord(value) && isString(value.id) && isString(value.name) && oneOf(value.biome, ['mine', 'wilds', 'caverns', 'ruins']) && isNumber(value.floor)
const isLineageEvent = (value: unknown): value is LineageEvent => isRecord(value) && isString(value.id) && value.kind === 'npcSacrifice' && isString(value.npcId) && isString(value.npcName) && oneOf(value.biome, ['mine', 'wilds', 'caverns', 'ruins']) && isNumber(value.floor) && isString(value.gateId) && isNumber(value.seed)
const isLegacyRecord = (value: unknown): value is LegacyRecord => isRecord(value) && isString(value.id) && isString(value.heirName) && oneOf(value.biome, ['mine', 'wilds', 'caverns', 'ruins']) && isNumber(value.floor) && isNumber(value.seed)
const copyLegacyRecords = (records: readonly LegacyRecord[]): LegacyRecord[] => records.slice(-12).map(record => ({ id: record.id, heirName: record.heirName, biome: record.biome, floor: record.floor, seed: record.seed }))
const isEncyclopedia = (value: unknown): value is EncyclopediaState => isRecord(value) && Array.isArray(value.enemies) && value.enemies.every(isString) && Array.isArray(value.telegraphs) && value.telegraphs.every(isString) && Array.isArray(value.tags) && value.tags.every(isString) && Array.isArray(value.gates) && value.gates.every(isString) && Array.isArray(value.legacyRecords) && value.legacyRecords.every(isLegacyRecord)
const isRunMetricSample = (value: unknown): value is RunMetricSample => isRecord(value) && isNumber(value.turn) && isNumber(value.floor) && isNumber(value.health) && isNumber(value.focus) && isNumber(value.gold) && isNumber(value.bombs) && isNumber(value.ropes) && isNumber(value.kills) && isNumber(value.damageDealt) && isNumber(value.damageTaken)
const isRunFloorMetrics = (value: unknown): value is RunFloorMetrics => isRecord(value) && isNumber(value.floor) && isNumber(value.turns) && isNumber(value.kills) && isNumber(value.damageDealt) && isNumber(value.damageTaken) && isNumber(value.goldGained) && isNumber(value.xpGained) && isNumber(value.pickups) && isNumber(value.bombsUsed) && isNumber(value.ropesUsed)
const isRunActions = (value: unknown): boolean => isRecord(value) && ['moves', 'attacks', 'casts', 'pickups', 'bombs', 'ropes', 'rests'].every(key => isNumber(value[key]))
const isRunTelemetry = (value: unknown): value is RunTelemetry => isRecord(value) && isNumber(value.turns) && isRunActions(value.actions) && isNumber(value.kills) && isNumber(value.damageDealt) && isNumber(value.damageTaken) && isNumber(value.goldGained) && isNumber(value.xpGained) && isNumber(value.pickups) && isNumber(value.bombsUsed) && isNumber(value.ropesUsed) && Array.isArray(value.samples) && value.samples.every(isRunMetricSample) && Array.isArray(value.floors) && value.floors.every(isRunFloorMetrics)
const isRunAnalysis = (value: unknown): value is RunAnalysis => isRecord(value) && isNumber(value.seed) && oneOf(value.biome, ['mine', 'wilds', 'caverns', 'ruins']) && isNumber(value.floor) && oneOf(value.outcome, ['lost', 'complete', 'suspended']) && isString(value.date) && isRunTelemetry(value.metrics)
const isTelegraph = (value: unknown): value is Telegraph => isRecord(value) && isString(value.id) && isString(value.sourceId) && isString(value.actionId) && Array.isArray(value.cells) && value.cells.every(isPoint) && oneOf(value.danger, ['minor', 'major']) && isNumber(value.resolveTurn) && (value.collision === undefined || (isRecord(value.collision) && isPoint(value.collision.point) && isString(value.collision.by))) && (value.cover === undefined || typeof value.cover === 'boolean')
const isFloor = (value: unknown): value is Floor => isRecord(value) && isInteger(value.index) && oneOf(value.biome, ['mine', 'wilds', 'caverns', 'ruins']) && isNumber(value.seed) && Array.isArray(value.tiles) && value.tiles.length === MAP_WIDTH * MAP_HEIGHT && value.tiles.every(isTile) && Array.isArray(value.actors) && value.actors.every(isActor) && Array.isArray(value.items) && value.items.every(isGroundItem) && Array.isArray(value.props) && value.props.every(isProp) && isPoint(value.start) && isPoint(value.exit) && typeof value.guardianDefeated === 'boolean' && isObjective(value.objective) && (value.telegraphs === undefined || (Array.isArray(value.telegraphs) && value.telegraphs.every(isTelegraph))) && (value.puzzleIds === undefined || (Array.isArray(value.puzzleIds) && value.puzzleIds.every(isString)))
const isHero = (value: unknown): value is Hero => isRecord(value) && isInteger(value.x) && isInteger(value.y) && isNonNegativeInteger(value.health) && isInteger(value.maxHealth) && value.maxHealth > 0 && value.health <= value.maxHealth && isNonNegativeInteger(value.focus) && isInteger(value.maxFocus) && value.maxFocus > 0 && value.focus <= value.maxFocus && isNonNegativeInteger(value.gold) && isNonNegativeInteger(value.bombs) && isNonNegativeInteger(value.ropes) && isNonNegativeInteger(value.keys) && isNonNegativeInteger(value.xp) && isInteger(value.level) && value.level > 0 && isRecord(value.stats) && isInteger(value.stats.strength) && isInteger(value.stats.agility) && isInteger(value.stats.vitality) && isInteger(value.stats.intellect) && Array.isArray(value.skills) && value.skills.every(isSkillId) && Array.isArray(value.inventory) && value.inventory.every(isItemId) && isRecord(value.equipment) && Object.values(value.equipment).every(item => item === undefined || isItemId(item)) && isString(value.name) && value.name.trim().length > 0 && isCourierOrigin(value.origin) && isCourierCalling(value.calling) && isDeathMode(value.deathMode) && (value.lastUnequipped === undefined || isItemId(value.lastUnequipped)) && (value.conditions === undefined || (Array.isArray(value.conditions) && value.conditions.every(isCondition))) && (value.cooldowns === undefined || (isRecord(value.cooldowns) && Object.values(value.cooldowns).every(isNonNegativeInteger)))

const isModal = (value: unknown): value is Modal | undefined => {
  if (value === undefined) return true
  if (!isRecord(value)) return false
  if (value.kind === 'help' || value.kind === 'skills' || value.kind === 'pause') return true
  if (value.kind === 'encyclopedia') return oneOf(value.section, ['enemies', 'telegraphs', 'tags', 'gates', 'legacy']) && (value.page === undefined || isNumber(value.page))
  if (value.kind === 'settings') return (value.page === undefined || isNumber(value.page)) && (value.awaiting === undefined || oneOf(value.awaiting, ['northwest', 'north', 'northeast', 'west', 'east', 'southwest', 'south', 'southeast', 'wait', 'help', 'encyclopedia', 'settings', 'use', 'drop', 'throw', 'equip', 'skills', 'bomb', 'rope', 'get', 'operate', 'descend', 'swap', 'script']))
  if (value.kind === 'inventory') return oneOf(value.mode, ['use', 'drop', 'throw', 'equip'])
  if (value.kind === 'shop') return isString(value.merchantId)
  if (value.kind === 'gate') return isString(value.gateId) && (value.choice === undefined || isNumber(value.choice)) && (value.confirming === undefined || typeof value.confirming === 'boolean')
  return value.kind === 'target' && oneOf(value.action, ['throw', 'spell', 'bomb']) && (value.item === undefined || isItemId(value.item))
}

const isRunRecord = (value: unknown): value is RunRecord => isRecord(value) && isNumber(value.version) && isNumber(value.seed) && isFloor(value.floor) && isHero(value.hero) && Array.isArray(value.messages) && value.messages.every(isString) && oneOf(value.status, ['title', 'playing', 'dead', 'victory']) && isModal(value.modal) && isNumber(value.turn) && (value.area === undefined || oneOf(value.area, ['mine', 'wilds', 'caverns', 'ruins'])) && (value.areaFloor === undefined || isNumber(value.areaFloor)) && (value.gateDestination === undefined || oneOf(value.gateDestination, ['mine', 'wilds', 'caverns', 'ruins'])) && (value.rescuedNpcs === undefined || (Array.isArray(value.rescuedNpcs) && value.rescuedNpcs.every(isRescuedNpc))) && (value.lineageEvents === undefined || (Array.isArray(value.lineageEvents) && value.lineageEvents.every(isLineageEvent))) && (value.encyclopedia === undefined || isEncyclopedia(value.encyclopedia)) && (value.telemetry === undefined || isRunTelemetry(value.telemetry))
const isRunState = (value: unknown): value is RunState => isRunRecord(value) && value.version === 3
const isCampaignRoute = (value: unknown): value is CampaignRouteRecord => isRecord(value) && (value.version === 1 || value.version === 2) && Array.isArray(value.completedAreas) && value.completedAreas.every(area => oneOf(area, ['mine', 'wilds', 'caverns', 'ruins'])) && Array.isArray(value.unlockedAreas) && value.unlockedAreas.every(area => oneOf(area, ['mine', 'wilds', 'caverns', 'ruins'])) && oneOf(value.selectedBiome, ['mine', 'wilds', 'caverns', 'ruins']) && value.unlockedAreas.includes(value.selectedBiome) && (value.rescuedNpcs === undefined || (Array.isArray(value.rescuedNpcs) && value.rescuedNpcs.every(isRescuedNpc))) && (value.lineageEvents === undefined || (Array.isArray(value.lineageEvents) && value.lineageEvents.every(isLineageEvent))) && (value.legacyRecords === undefined || (Array.isArray(value.legacyRecords) && value.legacyRecords.every(isLegacyRecord)))

type LegacyHero = Omit<Hero, 'name' | 'origin' | 'calling' | 'deathMode'>
type LegacyFloor = Omit<Floor, 'props' | 'objective'> & { props?: Floor['props']; objective?: Floor['objective'] }
type LegacyRunState = Omit<RunState, 'version' | 'floor' | 'hero'> & { version: 1 | 2; floor: LegacyFloor; hero: LegacyHero }

const isLegacyHero = (value: unknown): value is LegacyHero => isRecord(value) && isInteger(value.x) && isInteger(value.y) && isNonNegativeInteger(value.health) && isInteger(value.maxHealth) && value.maxHealth > 0 && value.health <= value.maxHealth && isNonNegativeInteger(value.focus) && isInteger(value.maxFocus) && value.maxFocus > 0 && value.focus <= value.maxFocus && isNonNegativeInteger(value.gold) && isNonNegativeInteger(value.bombs) && isNonNegativeInteger(value.ropes) && isNonNegativeInteger(value.keys) && isNonNegativeInteger(value.xp) && isInteger(value.level) && value.level > 0 && isRecord(value.stats) && isInteger(value.stats.strength) && isInteger(value.stats.agility) && isInteger(value.stats.vitality) && isInteger(value.stats.intellect) && Array.isArray(value.skills) && value.skills.every(isSkillId) && Array.isArray(value.inventory) && value.inventory.every(isItemId) && isRecord(value.equipment) && Object.values(value.equipment).every(item => item === undefined || isItemId(item)) && (value.lastUnequipped === undefined || isItemId(value.lastUnequipped)) && (value.conditions === undefined || (Array.isArray(value.conditions) && value.conditions.every(isCondition))) && (value.cooldowns === undefined || (isRecord(value.cooldowns) && Object.values(value.cooldowns).every(isNonNegativeInteger)))
const isLegacyFloor = (value: unknown): value is LegacyFloor => isRecord(value) && isInteger(value.index) && oneOf(value.biome, ['mine', 'wilds', 'caverns', 'ruins']) && isNumber(value.seed) && Array.isArray(value.tiles) && value.tiles.length === MAP_WIDTH * MAP_HEIGHT && value.tiles.every(isTile) && Array.isArray(value.actors) && value.actors.every(isActor) && Array.isArray(value.items) && value.items.every(isGroundItem) && (value.props === undefined || (Array.isArray(value.props) && value.props.every(isProp))) && isPoint(value.start) && isPoint(value.exit) && typeof value.guardianDefeated === 'boolean' && (value.objective === undefined || isObjective(value.objective)) && (value.telegraphs === undefined || (Array.isArray(value.telegraphs) && value.telegraphs.every(isTelegraph))) && (value.puzzleIds === undefined || (Array.isArray(value.puzzleIds) && value.puzzleIds.every(isString)))
const isLegacyRunState = (value: unknown): value is LegacyRunState => isRecord(value) && (value.version === 1 || value.version === 2) && isNumber(value.seed) && isLegacyFloor(value.floor) && isLegacyHero(value.hero) && Array.isArray(value.messages) && value.messages.every(isString) && oneOf(value.status, ['title', 'playing', 'dead', 'victory']) && isModal(value.modal) && isNonNegativeInteger(value.turn) && (value.area === undefined || oneOf(value.area, ['mine', 'wilds', 'caverns', 'ruins'])) && (value.areaFloor === undefined || isInteger(value.areaFloor))
const migrateLegacyRun = (legacy: LegacyRunState): RunState => ({
  version: 3,
  seed: legacy.seed,
  floor: { ...legacy.floor, props: legacy.floor.props ?? [], objective: legacy.floor.objective ?? objectiveForFloor(legacy.floor.index) },
  hero: { ...legacy.hero, name: 'Existing Courier', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint', conditions: legacy.hero.conditions ?? [], cooldowns: legacy.hero.cooldowns ?? {} },
  messages: [...legacy.messages],
  status: legacy.status,
  turn: legacy.turn,
  area: legacy.area ?? legacy.floor.biome,
  areaFloor: legacy.areaFloor ?? legacy.floor.index % 4
})

const pointOnMap = (point: Point): boolean => isInteger(point.x) && isInteger(point.y) && inBounds(point.x, point.y)
const validPersistedRun = (run: RunState): boolean => {
  const floor = run.floor
  if (!isNonNegativeInteger(run.turn) || floor.index < 0 || floor.index >= FLOOR_COUNT) return false
  if (!pointOnMap(run.hero) || !pointOnMap(floor.start) || !pointOnMap(floor.exit) || floor.tiles[floor.exit.y * MAP_WIDTH + floor.exit.x]?.kind !== 'exit') return false
  if (run.area !== undefined && run.area !== floor.biome) return false
  if (run.areaFloor !== undefined && (!isInteger(run.areaFloor) || run.areaFloor < 0 || run.areaFloor > 3)) return false
  if (floor.actors.some(actor => !pointOnMap(actor)) || floor.items.some(item => !pointOnMap(item)) || floor.props.some(prop => !pointOnMap(prop) || (prop.effectCells?.some(point => !pointOnMap(point)) ?? false))) return false
  return !(floor.telegraphs?.some(telegraph => telegraph.cells.some(point => !pointOnMap(point)) || (telegraph.collision !== undefined && !pointOnMap(telegraph.collision.point))) ?? false)
}

export const migrateRunRecord = (value: unknown): RunState | undefined => {
  const run = isRunState(value) ? { ...value } : isLegacyRunState(value) ? migrateLegacyRun(value) : undefined
  if (!run || !validPersistedRun(run)) return undefined
  if (run.encyclopedia) run.encyclopedia = { ...run.encyclopedia, legacyRecords: copyLegacyRecords(run.encyclopedia.legacyRecords) }
  run.telemetry ??= createRunTelemetry(run)
  return run
}

export const migrateCampaignRoute = (value: unknown): CampaignRouteState => isCampaignRoute(value) ? { version: 2, completedAreas: [...value.completedAreas], unlockedAreas: [...value.unlockedAreas], selectedBiome: value.selectedBiome, rescuedNpcs: (value.rescuedNpcs ?? []).map(npc => ({ ...npc })), lineageEvents: (value.lineageEvents ?? []).map(event => ({ ...event })), legacyRecords: copyLegacyRecords(value.legacyRecords ?? []) } : initialCampaignRoute()

const database = (): Promise<IDBDatabase> => new Promise((resolve, reject) => {
  const request = indexedDB.open(DB, 1)
  request.onupgradeneeded = () => request.result.createObjectStore(STORE)
  request.onsuccess = () => resolve(request.result)
  request.onerror = () => reject(request.error)
})

const requestError = (error: DOMException | null, fallback: string): Error => error ?? new Error(fallback)
const get = async <T>(key: string): Promise<T | undefined> => {
  const db = await database()
  return new Promise((resolve, reject) => {
    const request = db.transaction(STORE).objectStore(STORE).get(key)
    request.onsuccess = () => resolve(request.result as T | undefined)
    request.onerror = () => reject(requestError(request.error, `failed to read ${key}`))
  })
}

const put = async <T>(key: string, value: T): Promise<void> => {
  const db = await database()
  await new Promise<void>((resolve, reject) => {
    const request = db.transaction(STORE, 'readwrite').objectStore(STORE).put(value, key)
    request.onsuccess = () => resolve()
    request.onerror = () => reject(requestError(request.error, `failed to write ${key}`))
  })
}

const remove = async (key: string): Promise<void> => {
  const db = await database()
  await new Promise<void>((resolve, reject) => {
    const request = db.transaction(STORE, 'readwrite').objectStore(STORE).delete(key)
    request.onsuccess = () => resolve()
    request.onerror = () => reject(requestError(request.error, `failed to delete ${key}`))
  })
}

export async function loadRun(): Promise<RunState | undefined> {
  const record = await get<unknown>(RUN)
  return migrateRunRecord(record)
}
export const saveRun = (state: RunState) => put(RUN, state)
export const deleteRun = () => remove(RUN)

const emptyRecords = (): Records => ({ bestDepth: 0, wins: 0, deaths: 0, runs: [], analyses: [] })
const migrateRecords = (value: unknown): Records => {
  if (!isRecord(value)) return emptyRecords()
  const runs = Array.isArray(value.runs) ? value.runs.filter(run => isRecord(run) && isNumber(run.seed) && isNumber(run.floor) && isNumber(run.score) && typeof run.won === 'boolean' && isString(run.date)).map(run => ({ seed: run.seed, floor: run.floor, score: run.score, won: run.won, date: run.date })) : []
  const analyses = Array.isArray(value.analyses) ? value.analyses.filter(isRunAnalysis).slice(0, 20) : []
  return { bestDepth: isNumber(value.bestDepth) ? value.bestDepth : 0, wins: isNumber(value.wins) ? value.wins : 0, deaths: isNumber(value.deaths) ? value.deaths : 0, runs, analyses }
}
export const loadRecords = async (): Promise<Records> => migrateRecords(await get<unknown>(RECORDS))
export const saveRecords = (records: Records) => put(RECORDS, records)
export const loadCampaignRoute = async (): Promise<CampaignRouteState> => migrateCampaignRoute(await get<unknown>(CAMPAIGN_ROUTE))
export const saveCampaignRoute = (route: CampaignRouteState) => put(CAMPAIGN_ROUTE, migrateCampaignRoute(route))

interface CourierIndex { version: 1; ids: string[]; selectedId?: string }
const emptyCourierIndex = (): CourierIndex => ({ version: 1, ids: [] })
const isCourierIndex = (value: unknown): value is CourierIndex => isRecord(value) && value.version === 1 && Array.isArray(value.ids) && value.ids.every(isString) && (value.selectedId === undefined || isString(value.selectedId))
const courierKey = (id: string): string => `${COURIER_PREFIX}${id}`
const courierWrites = new SerialWriteQueue()

const writeTransaction = async (operation: (store: IDBObjectStore, fail: (error: Error) => void) => void): Promise<void> => {
  const db = await database()
  await new Promise<void>((resolve, reject) => {
    const transaction = db.transaction(STORE, 'readwrite')
    let failure: Error | undefined
    const fail = (error: Error): void => {
      failure ??= error
      try { transaction.abort() } catch { }
    }
    transaction.oncomplete = () => resolve()
    transaction.onerror = () => reject(failure ?? requestError(transaction.error, 'courier persistence transaction failed'))
    transaction.onabort = () => reject(failure ?? requestError(transaction.error, 'courier persistence transaction aborted'))
    try { operation(transaction.objectStore(STORE), fail) } catch (caught) { fail(caught instanceof Error ? caught : new Error(String(caught))) }
  })
}

const putInTransaction = (store: IDBObjectStore, key: string, value: unknown, fail: (error: Error) => void): void => {
  const request = store.put(value, key)
  request.onerror = () => fail(requestError(request.error, `failed to write ${key}`))
}

const mutateCourierIndex = (operation: (store: IDBObjectStore, index: CourierIndex, fail: (error: Error) => void) => void): Promise<void> => writeTransaction((store, fail) => {
  const request = store.get(COURIER_INDEX)
  request.onerror = () => fail(requestError(request.error, 'failed to read courier index'))
  request.onsuccess = () => {
    try { operation(store, isCourierIndex(request.result) ? request.result : emptyCourierIndex(), fail) }
    catch (caught) { fail(caught instanceof Error ? caught : new Error(String(caught))) }
  }
})
const courierIdentity = (value: unknown): CourierSave['identity'] | undefined => {
  if (!isRecord(value) || !isString(value.id) || !isString(value.name) || !isCourierOrigin(value.origin) || !isCourierCalling(value.calling) || !isDeathMode(value.deathMode) || !isString(value.createdAt)) return undefined
  return { id: value.id, name: value.name, origin: value.origin, calling: value.calling, deathMode: value.deathMode, createdAt: value.createdAt, ...(isString(value.parentId) ? { parentId: value.parentId } : {}) }
}
const migrateCourier = (value: unknown): CourierSave | undefined => {
  if (!isRecord(value) || value.version !== 1) return undefined
  const identity = courierIdentity(value.identity)
  if (!identity) return undefined
  const run = migrateRunRecord(value.run)
  const checkpoint = migrateRunRecord(value.checkpoint)
  const heir = isHero(value.heir) ? { ...value.heir, name: value.heir.name ?? identity.name, origin: value.heir.origin ?? identity.origin, calling: value.heir.calling ?? identity.calling, deathMode: value.heir.deathMode ?? identity.deathMode } : undefined
  const campaign = migrateCampaignRoute(value.campaign)
  const records = migrateRecords(value.records)
  return { version: 1, identity, ...(run ? { run } : {}), ...(checkpoint ? { checkpoint } : {}), ...(heir ? { heir } : {}), campaign, records, ...(value.archived === true ? { archived: true } : {}) }
}
const entryFor = (courier: CourierSave): CourierMenuEntry => ({
  id: courier.identity.id, name: courier.identity.name, origin: courier.identity.origin, calling: courier.identity.calling, deathMode: courier.identity.deathMode,
  ...(courier.run ? { area: courier.run.area ?? courier.run.floor.biome, floor: courier.run.floor.index + 1, turn: courier.run.turn } : {}), ...(courier.archived ? { archived: true } : {})
})

export async function loadCouriers(): Promise<{ couriers: CourierSave[]; selectedId?: string }> {
  await courierWrites.flush()
  const indexed = await get<unknown>(COURIER_INDEX)
  const index = isCourierIndex(indexed) ? indexed : undefined
  if (index) {
    const couriers = (await Promise.all(index.ids.map(async id => migrateCourier(await get<unknown>(courierKey(id)))))).filter((courier): courier is CourierSave => Boolean(courier))
    return { couriers, ...(couriers.some(courier => courier.identity.id === index.selectedId) ? { selectedId: index.selectedId } : { selectedId: couriers.find(courier => !courier.archived)?.identity.id }) }
  }
  const [run, records, campaign] = await Promise.all([loadRun(), loadRecords(), loadCampaignRoute()])
  if (!run && records.runs.length === 0 && campaign.completedAreas.length === 0 && campaign.rescuedNpcs.length === 0) return { couriers: [] }
  const id = crypto.randomUUID()
  const legacy: CourierSave = { version: 1, identity: { id, name: run?.hero.name ?? 'Existing Courier', origin: run?.hero.origin ?? 'mineborn', calling: run?.hero.calling ?? 'trailguard', deathMode: run?.hero.deathMode ?? 'checkpoint', createdAt: new Date().toISOString() }, ...(run ? { run, checkpoint: structuredClone(run), heir: structuredClone(run.hero) } : {}), campaign, records }
  await saveCourier(legacy, id)
  return { couriers: [legacy], selectedId: id }
}

export async function saveCourier(courier: CourierSave, selectedId?: string): Promise<void> {
  const snapshot = structuredClone(courier)
  await courierWrites.enqueue(() => mutateCourierIndex((store, index, fail) => {
    const ids = index.ids.includes(snapshot.identity.id) ? index.ids : [...index.ids, snapshot.identity.id]
    putInTransaction(store, courierKey(snapshot.identity.id), snapshot, fail)
    putInTransaction(store, COURIER_INDEX, { version: 1, ids, selectedId: selectedId ?? index.selectedId ?? snapshot.identity.id } satisfies CourierIndex, fail)
  }))
}

export async function selectCourier(id: string): Promise<void> {
  await courierWrites.enqueue(() => mutateCourierIndex((store, index, fail) => {
    if (index.ids.includes(id)) putInTransaction(store, COURIER_INDEX, { ...index, selectedId: id }, fail)
  }))
}

export async function deleteCourier(id: string): Promise<void> {
  await courierWrites.enqueue(() => mutateCourierIndex((store, index, fail) => {
    const ids = index.ids.filter(currentId => currentId !== id)
    const selectedId = index.selectedId === id ? ids[0] : index.selectedId
    const deletion = store.delete(courierKey(id))
    deletion.onerror = () => fail(requestError(deletion.error, `failed to delete courier ${id}`))
    putInTransaction(store, COURIER_INDEX, { version: 1, ids, ...(selectedId ? { selectedId } : {}) } satisfies CourierIndex, fail)
  }))
}

export const flushCourierWrites = (): Promise<void> => courierWrites.flush()

export const courierMenuEntries = (couriers: readonly CourierSave[]): CourierMenuEntry[] => couriers.filter(courier => !courier.archived).map(entryFor)
