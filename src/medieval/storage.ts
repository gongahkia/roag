import { foundationWorldContentSatisfiesSafetyPolicy, foundationWorldIdForManifest, foundationWorldInitialWorldMatchesManifest, foundationWorldTemporalStateMatches, isReproducibleWorldManifest } from './world'
import { isInitialWorld } from './initial-world'
import { emptyCreationSettingsRecord, isCreationSettingsRecord, saveCreationSettingsProfile as saveNamedCreationSettingsProfile, withLastUsedCreationSettings, type CreationSettings, type CreationSettingsRecord } from './settings'
import { MEDIEVAL_DATABASE_NAME, type ChronicleReason, type FoundationCrewMember, type FoundationJomon, type FoundationWorld, type JomonDeckPartition, type JomonVesselPropKind, type WorldChronicle, type WorldIndex, type WorldManifest } from './types'

const CATALOG_STORE = 'catalog'
const WORLD_STORE = 'worlds'
const CHRONICLE_STORE = 'chronicles'
const CREATION_SETTINGS_STORE = 'creation-settings'
const INDEX_KEY = 'world-index'
const CREATION_SETTINGS_KEY = 'last-used-and-profiles'
const DATABASE_VERSION = 2
const databaseName = MEDIEVAL_DATABASE_NAME

export const emptyWorldIndex = (): WorldIndex => ({ version: 1, activeWorlds: [], chronicles: [] })

export const addWorldToIndex = (index: WorldIndex, world: FoundationWorld): WorldIndex => ({
  version: 1,
  activeWorlds: [...index.activeWorlds.filter(entry => entry.id !== world.id), { id: world.id, label: world.manifest.creation.label, ...(world.manifest.initialCourierId === undefined ? {} : { initialCourierId: world.manifest.initialCourierId }) }].sort((left, right) => left.id.localeCompare(right.id)),
  chronicles: [...index.chronicles]
})

export const removeWorldFromIndex = (index: WorldIndex, worldId: string): WorldIndex => ({ version: 1, activeWorlds: index.activeWorlds.filter(entry => entry.id !== worldId), chronicles: [...index.chronicles] })

export const addChronicleToIndex = (index: WorldIndex, chronicle: WorldChronicle): WorldIndex => ({
  version: 1,
  activeWorlds: [...index.activeWorlds],
  chronicles: [...index.chronicles.filter(entry => entry.id !== chronicle.id), { id: chronicle.id, label: chronicle.world.manifest.creation.label, reason: chronicle.reason }].sort((left, right) => left.id.localeCompare(right.id))
})

const clone = <T>(value: T): T => structuredClone(value)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const string = (value: unknown): value is string => typeof value === 'string' && value.length > 0
const nonNegativeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const chronicleReason = (value: unknown): value is ChronicleReason => value === 'jomon-loss' || value === 'crew-extinction'
const crewRole = (value: unknown): boolean => ['bargemaster', 'pilot', 'factor', 'carpenter', 'guard', 'cook', 'healer', 'scribe', 'carter', 'fisher', 'bard'].includes(String(value))
const deckPartition = (value: unknown): value is JomonDeckPartition => ['tavern', 'chart-table', 'cargo-hold', 'repair-space', 'stores', 'berths', 'galley', 'gangplank'].includes(String(value))
const vesselPropKind = (value: unknown): value is JomonVesselPropKind => ['table', 'ledger', 'rack', 'hearth', 'berth', 'gangplank'].includes(String(value))

const isManifest = (value: unknown): value is WorldManifest => isReproducibleWorldManifest(value)
const isCrewMember = (value: unknown): value is FoundationCrewMember => record(value) && string(value.id) && string(value.name) && crewRole(value.role) && nonNegativeInteger(value.conversation) && Array.isArray(value.equipment) && value.equipment.every(string) && string(value.history) && typeof value.eligible === 'boolean' && Array.isArray(value.relationships) && value.relationships.every(relationship => record(relationship) && string(relationship.personId) && [-2, -1, 0, 1, 2].includes(Number(relationship.standing)) && ['kinship', 'work', 'debt', 'friendship', 'rivalry'].includes(String(relationship.basis)))
const isFoundationJomon = (value: unknown): value is FoundationJomon => record(value) && value.id === 'vessel:jomon' && value.name === 'Jomon' && Array.isArray(value.deckPartitions) && value.deckPartitions.every(deckPartition) && Array.isArray(value.quays) && value.quays.every(quay => record(quay) && string(quay.id) && string(quay.name)) && Array.isArray(value.props) && value.props.every(prop => record(prop) && string(prop.id) && vesselPropKind(prop.kind) && deckPartition(prop.partition))
const isFoundationWorld = (value: unknown): value is FoundationWorld => {
  if (!record(value) || value.version !== 2 || !string(value.id) || value.status !== 'active' || !isManifest(value.manifest) || value.id !== foundationWorldIdForManifest(value.manifest) || !isFoundationJomon(value.jomon) || !Array.isArray(value.crew) || value.crew.length === 0 || !value.crew.every(isCrewMember) || !isInitialWorld(value.initialWorld) || !nonNegativeInteger(value.worldTime) || !Array.isArray(value.causalHistory) || !value.causalHistory.every(event => record(event) && nonNegativeInteger(event.sequence) && nonNegativeInteger(event.atWorldTime) && (event.kind === 'world-created' || event.kind === 'initial-courier-selected' || event.kind === 'temporal-action' || event.kind === 'scheduled-event-resolved') && string(event.detail))) return false

  const manifest = value.manifest
  const crew = value.crew as FoundationCrewMember[]
  return (manifest.initialCourierId === undefined || crew.some(member => member.id === manifest.initialCourierId && member.eligible)) && foundationWorldInitialWorldMatchesManifest(value as unknown as FoundationWorld) && foundationWorldContentSatisfiesSafetyPolicy(value as unknown as FoundationWorld) && foundationWorldTemporalStateMatches(value as unknown as FoundationWorld)
}
const isChronicle = (value: unknown): value is WorldChronicle => record(value) && value.version === 1 && string(value.id) && value.status === 'finalized' && chronicleReason(value.reason) && isFoundationWorld(value.world)
const isActiveWorldIndexEntry = (value: unknown): boolean => record(value) && string(value.id) && string(value.label) && (value.initialCourierId === undefined || string(value.initialCourierId))
const isChronicleIndexEntry = (value: unknown): boolean => record(value) && string(value.id) && string(value.label) && chronicleReason(value.reason)
const isWorldIndex = (value: unknown): value is WorldIndex => record(value) && value.version === 1 && Array.isArray(value.activeWorlds) && value.activeWorlds.every(isActiveWorldIndexEntry) && Array.isArray(value.chronicles) && value.chronicles.every(isChronicleIndexEntry)

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
        if (!database.objectStoreNames.contains(CREATION_SETTINGS_STORE)) database.createObjectStore(CREATION_SETTINGS_STORE)
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
    return isFoundationWorld(value) ? clone(value) : undefined
  }

  async loadChronicle(id: string): Promise<WorldChronicle | undefined> {
    const database = await this.open()
    const transaction = database.transaction(CHRONICLE_STORE, 'readonly')
    const value = await requestResult(transaction.objectStore(CHRONICLE_STORE).get(id))
    await transactionDone(transaction)
    return isChronicle(value) ? clone(value) : undefined
  }

  /** Invalid persisted settings are rejected to defaults; no prototype data is inspected. */
  async loadCreationSettings(): Promise<CreationSettingsRecord> {
    const database = await this.open()
    const transaction = database.transaction(CREATION_SETTINGS_STORE, 'readonly')
    const value = await requestResult(transaction.objectStore(CREATION_SETTINGS_STORE).get(CREATION_SETTINGS_KEY))
    await transactionDone(transaction)
    return isCreationSettingsRecord(value) ? clone(value) : emptyCreationSettingsRecord()
  }

  private async writeCreationSettings(recordValue: CreationSettingsRecord): Promise<CreationSettingsRecord> {
    if (!isCreationSettingsRecord(recordValue)) throw new Error('refusing to save invalid medieval creation settings')
    const database = await this.open()
    const transaction = database.transaction(CREATION_SETTINGS_STORE, 'readwrite')
    transaction.objectStore(CREATION_SETTINGS_STORE).put(clone(recordValue), CREATION_SETTINGS_KEY)
    await transactionDone(transaction)
    return clone(recordValue)
  }

  async saveLastUsedCreationSettings(settings: CreationSettings): Promise<CreationSettingsRecord> {
    return this.writeCreationSettings(withLastUsedCreationSettings(await this.loadCreationSettings(), settings))
  }

  /** Six normalized names are retained; an existing name is replaced in place. */
  async saveCreationSettingsProfile(name: string, settings: CreationSettings): Promise<CreationSettingsRecord> {
    return this.writeCreationSettings(saveNamedCreationSettingsProfile(await this.loadCreationSettings(), name, settings))
  }

  async saveWorld(world: FoundationWorld): Promise<void> {
    if (!isFoundationWorld(world)) throw new Error('refusing to save an invalid medieval world')
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
