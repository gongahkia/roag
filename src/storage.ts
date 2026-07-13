import type { Actor, ConditionState, Floor, GroundItem, Hero, Modal, Point, Records, RunState, RunStateV1, Telegraph, Tile } from './types'

const DB = 'blockscape-expedition-v2'
const STORE = 'state'
const RUN = 'active-run'
const RECORDS = 'records'

type RunRecord = Omit<RunState, 'version'> & { version: number }
type UnknownRecord = Record<string, unknown>

const isRecord = (value: unknown): value is UnknownRecord => typeof value === 'object' && value !== null && !Array.isArray(value)
const isNumber = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value)
const isString = (value: unknown): value is string => typeof value === 'string'
const oneOf = <T extends string>(value: unknown, values: readonly T[]): value is T => typeof value === 'string' && values.some(current => current === value)
const isPoint = (value: unknown): value is Point => isRecord(value) && isNumber(value.x) && isNumber(value.y)
const isTile = (value: unknown): value is Tile => isRecord(value) && oneOf(value.kind, ['wall', 'floor', 'exit', 'door', 'lockedDoor', 'water', 'lava', 'pit', 'rope', 'spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'web', 'gas', 'crate', 'chest', 'altar', 'shop', 'rescue']) && typeof value.explored === 'boolean' && typeof value.visible === 'boolean'
const isCondition = (value: unknown): value is ConditionState => isRecord(value) && oneOf(value.kind, ['burning', 'rooted', 'staggered', 'shielded', 'marked', 'slowed']) && isNumber(value.duration) && isNumber(value.potency)
const isActor = (value: unknown): value is Actor => isRecord(value) && isString(value.id) && oneOf(value.role, ['hero', 'monster', 'merchant', 'ally', 'guardian']) && isString(value.kind) && isString(value.name) && isNumber(value.x) && isNumber(value.y) && isNumber(value.health) && isNumber(value.maxHealth) && isNumber(value.attack) && isNumber(value.defense) && isNumber(value.speed) && isNumber(value.energy) && isString(value.glyph) && isString(value.color) && typeof value.hostile === 'boolean' && (value.ai === undefined || oneOf(value.ai, ['chase', 'ranged', 'wander', 'guardian'])) && (value.status === undefined || (Array.isArray(value.status) && value.status.every(isString))) && (value.conditions === undefined || (Array.isArray(value.conditions) && value.conditions.every(isCondition)))
const isGroundItem = (value: unknown): value is GroundItem => isRecord(value) && isString(value.id) && isNumber(value.x) && isNumber(value.y) && isNumber(value.count)
const isTelegraph = (value: unknown): value is Telegraph => isRecord(value) && isString(value.id) && isString(value.sourceId) && isString(value.actionId) && Array.isArray(value.cells) && value.cells.every(isPoint) && oneOf(value.danger, ['minor', 'major']) && isNumber(value.resolveTurn) && (value.collision === undefined || (isRecord(value.collision) && isPoint(value.collision.point) && isString(value.collision.by))) && (value.cover === undefined || typeof value.cover === 'boolean')
const isFloor = (value: unknown): value is Floor => isRecord(value) && isNumber(value.index) && oneOf(value.biome, ['mine', 'wilds', 'caverns', 'ruins']) && isNumber(value.seed) && Array.isArray(value.tiles) && value.tiles.every(isTile) && Array.isArray(value.actors) && value.actors.every(isActor) && Array.isArray(value.items) && value.items.every(isGroundItem) && isPoint(value.start) && isPoint(value.exit) && typeof value.guardianDefeated === 'boolean' && (value.telegraphs === undefined || (Array.isArray(value.telegraphs) && value.telegraphs.every(isTelegraph)))
const isHero = (value: unknown): value is Hero => isRecord(value) && isNumber(value.x) && isNumber(value.y) && isNumber(value.health) && isNumber(value.maxHealth) && isNumber(value.focus) && isNumber(value.maxFocus) && isNumber(value.gold) && isNumber(value.bombs) && isNumber(value.ropes) && isNumber(value.keys) && isNumber(value.xp) && isNumber(value.level) && isRecord(value.stats) && isNumber(value.stats.strength) && isNumber(value.stats.agility) && isNumber(value.stats.vitality) && isNumber(value.stats.intellect) && Array.isArray(value.skills) && value.skills.every(isString) && Array.isArray(value.inventory) && value.inventory.every(isString) && isRecord(value.equipment) && Object.values(value.equipment).every(item => item === undefined || isString(item)) && (value.lastUnequipped === undefined || isString(value.lastUnequipped)) && (value.conditions === undefined || (Array.isArray(value.conditions) && value.conditions.every(isCondition))) && (value.cooldowns === undefined || (isRecord(value.cooldowns) && Object.values(value.cooldowns).every(isNumber)))

const isModal = (value: unknown): value is Modal | undefined => {
  if (value === undefined) return true
  if (!isRecord(value)) return false
  if (value.kind === 'help' || value.kind === 'skills') return true
  if (value.kind === 'inventory') return oneOf(value.mode, ['use', 'drop', 'throw', 'equip'])
  return value.kind === 'shop' ? isString(value.merchantId) : value.kind === 'target' && oneOf(value.action, ['throw', 'spell', 'bomb']) && (value.item === undefined || isString(value.item))
}

const isRunRecord = (value: unknown): value is RunRecord => isRecord(value) && isNumber(value.version) && isNumber(value.seed) && isFloor(value.floor) && isHero(value.hero) && Array.isArray(value.messages) && value.messages.every(isString) && oneOf(value.status, ['title', 'playing', 'dead', 'victory']) && isModal(value.modal) && isNumber(value.turn)
const isRunState = (value: unknown): value is RunState => isRunRecord(value) && value.version === 2
const isRunStateV1 = (value: unknown): value is RunStateV1 => isRunRecord(value) && value.version === 1

export const migrateRunRecord = (value: unknown): RunState | undefined => {
  if (isRunState(value)) return value
  if (isRunStateV1(value)) return { ...value, version: 2 }
  return undefined
}

const database = (): Promise<IDBDatabase> => new Promise((resolve, reject) => {
  const request = indexedDB.open(DB, 1)
  request.onupgradeneeded = () => request.result.createObjectStore(STORE)
  request.onsuccess = () => resolve(request.result)
  request.onerror = () => reject(request.error)
})

const get = async <T>(key: string): Promise<T | undefined> => {
  try {
    const db = await database()
    return await new Promise((resolve, reject) => {
      const request = db.transaction(STORE).objectStore(STORE).get(key)
      request.onsuccess = () => resolve(request.result as T | undefined)
      request.onerror = () => reject(request.error)
    })
  } catch { return undefined }
}

const put = async <T>(key: string, value: T): Promise<void> => {
  try {
    const db = await database()
    await new Promise<void>((resolve, reject) => {
      const request = db.transaction(STORE, 'readwrite').objectStore(STORE).put(value, key)
      request.onsuccess = () => resolve()
      request.onerror = () => reject(request.error)
    })
  } catch { }
}

export async function loadRun(): Promise<RunState | undefined> {
  const record = await get<unknown>(RUN)
  const run = migrateRunRecord(record)
  if (run && isRecord(record) && record.version === 1) await put(RUN, run)
  return run
}
export const saveRun = (state: RunState) => put(RUN, state)
export async function deleteRun(): Promise<void> {
  try {
    const db = await database()
    await new Promise<void>((resolve, reject) => {
      const request = db.transaction(STORE, 'readwrite').objectStore(STORE).delete(RUN)
      request.onsuccess = () => resolve()
      request.onerror = () => reject(request.error)
    })
  } catch { }
}

const emptyRecords = (): Records => ({ bestDepth: 0, wins: 0, deaths: 0, runs: [] })
export const loadRecords = async (): Promise<Records> => (await get<Records>(RECORDS)) ?? emptyRecords()
export const saveRecords = (records: Records) => put(RECORDS, records)
