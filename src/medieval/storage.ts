import type { FoundationWorld, WorldChronicle, WorldIndex } from './types'

const CATALOG_STORE = 'catalog'
const WORLD_STORE = 'worlds'
const CHRONICLE_STORE = 'chronicles'
const INDEX_KEY = 'world-index'
const DATABASE_VERSION = 1
const databaseName = 'jomon-medieval-worlds-v1'

export const emptyWorldIndex = (): WorldIndex => ({ version: 1, activeWorlds: [], chronicles: [] })

export const addWorldToIndex = (index: WorldIndex, world: FoundationWorld): WorldIndex => ({
  version: 1,
  activeWorlds: [...index.activeWorlds.filter(entry => entry.id !== world.id), { id: world.id, label: world.manifest.label, ...(world.manifest.initialCourierId === undefined ? {} : { initialCourierId: world.manifest.initialCourierId }) }].sort((left, right) => left.id.localeCompare(right.id)),
  chronicles: [...index.chronicles]
})

export const removeWorldFromIndex = (index: WorldIndex, worldId: string): WorldIndex => ({ version: 1, activeWorlds: index.activeWorlds.filter(entry => entry.id !== worldId), chronicles: [...index.chronicles] })

export const addChronicleToIndex = (index: WorldIndex, chronicle: WorldChronicle): WorldIndex => ({
  version: 1,
  activeWorlds: [...index.activeWorlds],
  chronicles: [...index.chronicles.filter(entry => entry.id !== chronicle.id), { id: chronicle.id, label: chronicle.world.manifest.label, reason: chronicle.reason }].sort((left, right) => left.id.localeCompare(right.id))
})

const clone = <T>(value: T): T => structuredClone(value)

const isWorldIndex = (value: unknown): value is WorldIndex => {
  if (!value || typeof value !== 'object') return false
  const candidate = value as Partial<WorldIndex>
  return candidate.version === 1 && Array.isArray(candidate.activeWorlds) && Array.isArray(candidate.chronicles)
}

const requestResult = <T>(request: IDBRequest<T>): Promise<T> => new Promise((resolve, reject) => {
  request.onsuccess = () => resolve(request.result)
  request.onerror = () => reject(request.error ?? new Error('IndexedDB request failed'))
})

const transactionDone = (transaction: IDBTransaction): Promise<void> => new Promise((resolve, reject) => {
  transaction.oncomplete = () => resolve()
  transaction.onerror = () => reject(transaction.error ?? new Error('IndexedDB transaction failed'))
  transaction.onabort = () => reject(transaction.error ?? new Error('IndexedDB transaction aborted'))
})

export class MedievalWorldRepository {
  private connection: Promise<IDBDatabase> | undefined

  private open(): Promise<IDBDatabase> {
    if (this.connection) return this.connection
    if (typeof indexedDB === 'undefined') return Promise.reject(new Error('IndexedDB is unavailable'))
    this.connection = new Promise((resolve, reject) => {
      const request = indexedDB.open(databaseName, DATABASE_VERSION)
      request.onupgradeneeded = () => {
        const database = request.result
        if (!database.objectStoreNames.contains(CATALOG_STORE)) database.createObjectStore(CATALOG_STORE)
        if (!database.objectStoreNames.contains(WORLD_STORE)) database.createObjectStore(WORLD_STORE)
        if (!database.objectStoreNames.contains(CHRONICLE_STORE)) database.createObjectStore(CHRONICLE_STORE)
      }
      request.onsuccess = () => resolve(request.result)
      request.onerror = () => reject(request.error ?? new Error('could not open medieval world storage'))
      request.onblocked = () => reject(new Error('medieval world storage upgrade is blocked'))
    })
    return this.connection
  }

  async loadIndex(): Promise<WorldIndex> {
    const database = await this.open()
    const transaction = database.transaction(CATALOG_STORE, 'readonly')
    const value = await requestResult(transaction.objectStore(CATALOG_STORE).get(INDEX_KEY))
    await transactionDone(transaction)
    return isWorldIndex(value) ? clone(value) : emptyWorldIndex()
  }

  async loadWorld(id: string): Promise<FoundationWorld | undefined> {
    const database = await this.open()
    const transaction = database.transaction(WORLD_STORE, 'readonly')
    const value = await requestResult(transaction.objectStore(WORLD_STORE).get(id))
    await transactionDone(transaction)
    return value === undefined ? undefined : clone(value as FoundationWorld)
  }

  async loadChronicle(id: string): Promise<WorldChronicle | undefined> {
    const database = await this.open()
    const transaction = database.transaction(CHRONICLE_STORE, 'readonly')
    const value = await requestResult(transaction.objectStore(CHRONICLE_STORE).get(id))
    await transactionDone(transaction)
    return value === undefined ? undefined : clone(value as WorldChronicle)
  }

  async saveWorld(world: FoundationWorld): Promise<void> {
    const index = addWorldToIndex(await this.loadIndex(), world)
    const database = await this.open()
    const transaction = database.transaction([CATALOG_STORE, WORLD_STORE], 'readwrite')
    transaction.objectStore(WORLD_STORE).put(clone(world), world.id)
    transaction.objectStore(CATALOG_STORE).put(index, INDEX_KEY)
    await transactionDone(transaction)
  }

  async finalize(world: FoundationWorld, chronicle: WorldChronicle): Promise<void> {
    const index = addChronicleToIndex(removeWorldFromIndex(await this.loadIndex(), world.id), chronicle)
    const database = await this.open()
    const transaction = database.transaction([CATALOG_STORE, WORLD_STORE, CHRONICLE_STORE], 'readwrite')
    transaction.objectStore(WORLD_STORE).delete(world.id)
    transaction.objectStore(CHRONICLE_STORE).put(clone(chronicle), chronicle.id)
    transaction.objectStore(CATALOG_STORE).put(index, INDEX_KEY)
    await transactionDone(transaction)
  }
}
