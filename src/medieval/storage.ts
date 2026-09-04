import { isValidFoundationWorld, resolveCourierContinuityLoss as resolveCourierContinuityTransition, upgradeFoundationWorldV13, upgradeFoundationWorldV14, type CourierContinuityResolution } from './world'
import type { CourierContinuityConfirmation } from './courier-continuity'
import { emptyCreationSettingsRecord, isCreationSettingsRecord, saveCreationSettingsProfile as saveNamedCreationSettingsProfile, withLastUsedCreationSettings, type CreationSettings, type CreationSettingsRecord } from './settings'
import { defaultTerminalControlPreferences, isTerminalControlPreferences, type TerminalControlPreferences } from './terminal-controls'
import { createActiveWorldBackupBundle, createChronicleBackupBundle, createWorldRecordIndex, MedievalPersistenceLayoutError, MEDIEVAL_PERSISTENCE_DATABASE_VERSION, parsePersistenceBackupBundle, persistenceWorldSourceFor, serializePersistenceBackupBundle, snapshotRingAfterReplacement, validateFoundationWorldSnapshotRing, validateLegacyWorldRecordIndex, validateWorldRecordIndex, type FoundationWorldSnapshotRing, type WorldRecordIndex } from './persistence-layout'
import { MEDIEVAL_DATABASE_NAME, type ChronicleReason, type FoundationWorld, type WorldChronicle, type WorldIndex } from './types'

const CATALOG_STORE = 'catalog'
const WORLD_STORE = 'worlds'
const CHRONICLE_STORE = 'chronicles'
const CREATION_SETTINGS_STORE = 'creation-settings'
const TERMINAL_CONTROLS_STORE = 'terminal-controls'
const RECORD_INDEX_STORE = 'world-record-indexes'
const SNAPSHOT_STORE = 'world-snapshots'
const INDEX_KEY = 'world-index'
const CREATION_SETTINGS_KEY = 'last-used-and-profiles'
const TERMINAL_CONTROLS_KEY = 'world-controls'
const databaseName = MEDIEVAL_DATABASE_NAME

export const emptyWorldIndex = (): WorldIndex => ({ version: 1, activeWorlds: [], chronicles: [] })
export const addWorldToIndex = (index: WorldIndex, world: FoundationWorld): WorldIndex => ({ version: 1, activeWorlds: [...index.activeWorlds.filter(entry => entry.id !== world.id), { id: world.id, label: world.manifest.creation.label, ...(world.state.courier.initialCourierId === undefined ? {} : { initialCourierId: world.state.courier.initialCourierId }) }].sort((left, right) => left.id.localeCompare(right.id)), chronicles: [...index.chronicles] })
export const removeWorldFromIndex = (index: WorldIndex, worldId: string): WorldIndex => ({ version: 1, activeWorlds: index.activeWorlds.filter(entry => entry.id !== worldId), chronicles: [...index.chronicles] })
export const addChronicleToIndex = (index: WorldIndex, chronicle: WorldChronicle): WorldIndex => ({ version: 1, activeWorlds: [...index.activeWorlds], chronicles: [...index.chronicles.filter(entry => entry.id !== chronicle.id), { id: chronicle.id, label: chronicle.world.manifest.creation.label, reason: chronicle.reason }].sort((left, right) => left.id.localeCompare(right.id)) })

const clone = <T>(value: T): T => structuredClone(value)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const string = (value: unknown): value is string => typeof value === 'string' && value.length > 0
const chronicleReason = (value: unknown): value is ChronicleReason => value === 'jomon-loss' || value === 'crew-extinction'
const isFoundationWorld = (value: unknown): value is FoundationWorld => isValidFoundationWorld(value)
/** Read-only conversion; callers explicitly save a returned current envelope to persist it. */
const loadedFoundationWorld = (value: unknown): FoundationWorld | undefined => {
  if (isFoundationWorld(value)) return clone(value)
  try { return upgradeFoundationWorldV14(value) } catch { }
  try { return upgradeFoundationWorldV13(value) } catch { return undefined }
}
const isChronicle = (value: unknown): value is WorldChronicle => record(value) && value.version === 12 && string(value.id) && value.id === `chronicle:${(value.world as { id?: unknown })?.id ?? ''}` && value.status === 'finalized' && chronicleReason(value.reason) && isFoundationWorld(value.world)
const loadedChronicle = (value: unknown): WorldChronicle | undefined => {
  if (isChronicle(value)) return clone(value)
  if (!record(value) || value.version !== 12 || !string(value.id) || value.status !== 'finalized' || !chronicleReason(value.reason)) return undefined
  const world = loadedFoundationWorld(value.world)
  if (!world || value.id !== `chronicle:${world.id}`) return undefined
  return { version: 12, id: value.id, status: 'finalized', reason: value.reason, world }
}
const isActiveWorldIndexEntry = (value: unknown): boolean => record(value) && string(value.id) && string(value.label) && (value.initialCourierId === undefined || string(value.initialCourierId))
const isChronicleIndexEntry = (value: unknown): boolean => record(value) && string(value.id) && string(value.label) && chronicleReason(value.reason)
const isWorldIndex = (value: unknown): value is WorldIndex => record(value) && value.version === 1 && Array.isArray(value.activeWorlds) && value.activeWorlds.every(isActiveWorldIndexEntry) && Array.isArray(value.chronicles) && value.chronicles.every(isChronicleIndexEntry)
const requestResult = <T>(request: IDBRequest<T>): Promise<T> => new Promise((resolve, reject) => { request.onsuccess = () => resolve(request.result); request.onerror = () => reject(request.error ?? new Error('IndexedDB request failed')) })
const transactionDone = (transaction: IDBTransaction): Promise<void> => new Promise((resolve, reject) => { transaction.oncomplete = () => resolve(); transaction.onerror = () => reject(transaction.error ?? new Error('IndexedDB transaction failed')); transaction.onabort = () => reject(transaction.error ?? new Error('IndexedDB transaction aborted')) })
const persistenceError = (error: unknown): MedievalPersistenceLayoutError => {
  if (error instanceof MedievalPersistenceLayoutError) return error
  const name = record(error) && typeof error.name === 'string' ? error.name : ''
  return name === 'QuotaExceededError' ? new MedievalPersistenceLayoutError('storage-quota-exceeded') : name === 'AbortError' ? new MedievalPersistenceLayoutError('storage-transaction-aborted') : new MedievalPersistenceLayoutError('storage-request-failed', error instanceof Error ? error.message : undefined)
}

export type SnapshotInspection = { status: 'absent' } | { status: 'available'; ring: FoundationWorldSnapshotRing } | { status: 'corrupt' }

/** v4 adds metadata stores only; valid v3 FoundationWorld envelopes load unchanged. */
export class MedievalWorldRepository {
  private connection: Promise<IDBDatabase> | undefined
  private open(): Promise<IDBDatabase> {
    if (this.connection) return this.connection
    if (typeof indexedDB === 'undefined') return Promise.reject(new MedievalPersistenceLayoutError('storage-unavailable'))
    this.connection = new Promise((resolve, reject) => {
      const request = indexedDB.open(databaseName, MEDIEVAL_PERSISTENCE_DATABASE_VERSION)
      request.onupgradeneeded = () => { const database = request.result; for (const name of [CATALOG_STORE, WORLD_STORE, CHRONICLE_STORE, CREATION_SETTINGS_STORE, TERMINAL_CONTROLS_STORE, RECORD_INDEX_STORE, SNAPSHOT_STORE]) if (!database.objectStoreNames.contains(name)) database.createObjectStore(name) }
      request.onsuccess = () => resolve(request.result)
      request.onerror = () => reject(new MedievalPersistenceLayoutError('storage-request-failed', request.error?.message))
      request.onblocked = () => reject(new MedievalPersistenceLayoutError('storage-upgrade-blocked'))
    })
    return this.connection
  }
  async loadIndex(): Promise<WorldIndex> { const database = await this.open(); const transaction = database.transaction(CATALOG_STORE, 'readonly'); const value = await requestResult(transaction.objectStore(CATALOG_STORE).get(INDEX_KEY)); await transactionDone(transaction); return isWorldIndex(value) ? clone(value) : emptyWorldIndex() }
  async loadWorld(id: string): Promise<FoundationWorld | undefined> { const database = await this.open(); const transaction = database.transaction(WORLD_STORE, 'readonly'); const value = await requestResult(transaction.objectStore(WORLD_STORE).get(id)); await transactionDone(transaction); return loadedFoundationWorld(value) }
  async loadChronicle(id: string): Promise<WorldChronicle | undefined> { const database = await this.open(); const transaction = database.transaction(CHRONICLE_STORE, 'readonly'); const value = await requestResult(transaction.objectStore(CHRONICLE_STORE).get(id)); await transactionDone(transaction); return loadedChronicle(value) }
  async loadWorldRecordIndex(worldId: string): Promise<WorldRecordIndex | undefined> {
    const database = await this.open(); const transaction = database.transaction([WORLD_STORE, RECORD_INDEX_STORE], 'readonly'); const [world, index] = await Promise.all([requestResult(transaction.objectStore(WORLD_STORE).get(worldId)), requestResult(transaction.objectStore(RECORD_INDEX_STORE).get(worldId))]); await transactionDone(transaction)
    return isFoundationWorld(world) && validateWorldRecordIndex(index, world) ? clone(index as WorldRecordIndex) : undefined
  }
  async inspectWorldSnapshots(worldId: string): Promise<SnapshotInspection> { const database = await this.open(); const transaction = database.transaction(SNAPSHOT_STORE, 'readonly'); const value = await requestResult(transaction.objectStore(SNAPSHOT_STORE).get(worldId)); await transactionDone(transaction); return value === undefined ? { status: 'absent' } : validateFoundationWorldSnapshotRing(value) ? { status: 'available', ring: clone(value) } : { status: 'corrupt' } }
  async inspectSnapshot(worldId: string, sequence: number): Promise<FoundationWorld | undefined> { const inspection = await this.inspectWorldSnapshots(worldId); return inspection.status === 'available' ? clone(inspection.ring.snapshots.find(item => item.sequence === sequence)?.world) : undefined }
  /** Invalid settings remain untouched and fall back to defaults; prototype data is never read. */
  async loadCreationSettings(): Promise<CreationSettingsRecord> { const database = await this.open(); const transaction = database.transaction(CREATION_SETTINGS_STORE, 'readonly'); const value = await requestResult(transaction.objectStore(CREATION_SETTINGS_STORE).get(CREATION_SETTINGS_KEY)); await transactionDone(transaction); return isCreationSettingsRecord(value) ? clone(value) : emptyCreationSettingsRecord() }
  private async writeCreationSettings(value: CreationSettingsRecord): Promise<CreationSettingsRecord> { if (!isCreationSettingsRecord(value)) throw new Error('refusing to save invalid medieval creation settings'); const database = await this.open(); const transaction = database.transaction(CREATION_SETTINGS_STORE, 'readwrite'); transaction.objectStore(CREATION_SETTINGS_STORE).put(clone(value), CREATION_SETTINGS_KEY); await transactionDone(transaction); return clone(value) }
  async saveLastUsedCreationSettings(settings: CreationSettings): Promise<CreationSettingsRecord> { return this.writeCreationSettings(withLastUsedCreationSettings(await this.loadCreationSettings(), settings)) }
  async saveCreationSettingsProfile(name: string, settings: CreationSettings): Promise<CreationSettingsRecord> { return this.writeCreationSettings(saveNamedCreationSettingsProfile(await this.loadCreationSettings(), name, settings)) }
  async loadTerminalControls(): Promise<TerminalControlPreferences> { const database = await this.open(); const transaction = database.transaction(TERMINAL_CONTROLS_STORE, 'readonly'); const value = await requestResult(transaction.objectStore(TERMINAL_CONTROLS_STORE).get(TERMINAL_CONTROLS_KEY)); await transactionDone(transaction); return isTerminalControlPreferences(value) ? clone(value) : defaultTerminalControlPreferences() }
  async saveTerminalControls(preferences: TerminalControlPreferences): Promise<TerminalControlPreferences> { if (!isTerminalControlPreferences(preferences)) throw new Error('refusing to save invalid medieval terminal controls'); const database = await this.open(); const transaction = database.transaction(TERMINAL_CONTROLS_STORE, 'readwrite'); transaction.objectStore(TERMINAL_CONTROLS_STORE).put(clone(preferences), TERMINAL_CONTROLS_KEY); await transactionDone(transaction); return clone(preferences) }

  private async writeActiveWorld(world: FoundationWorld, collision: 'allow' | 'reject'): Promise<void> {
    if (!isFoundationWorld(world)) throw new MedievalPersistenceLayoutError('invalid-submitted-world', 'refusing to save an invalid medieval world')
    const database = await this.open(); const transaction = database.transaction([CATALOG_STORE, WORLD_STORE, RECORD_INDEX_STORE, SNAPSHOT_STORE], 'readwrite'); const completed = transactionDone(transaction)
    try {
      const [catalogValue, previousValue, indexValue, snapshotsValue] = await Promise.all([requestResult(transaction.objectStore(CATALOG_STORE).get(INDEX_KEY)), requestResult(transaction.objectStore(WORLD_STORE).get(world.id)), requestResult(transaction.objectStore(RECORD_INDEX_STORE).get(world.id)), requestResult(transaction.objectStore(SNAPSHOT_STORE).get(world.id))])
      // Catalog metadata is derived and non-authoritative. A caller's explicit
      // save may rebuild it; corrupt full envelopes/indexes are never overwritten.
      const previous = previousValue === undefined ? undefined : loadedFoundationWorld(previousValue)
      if (previousValue !== undefined && previous === undefined) throw new MedievalPersistenceLayoutError('corrupt-existing-world')
      const previousIsCurrent = previousValue !== undefined && isFoundationWorld(previousValue)
      if (indexValue !== undefined && (previousValue === undefined || !(previousIsCurrent ? validateWorldRecordIndex(indexValue, previousValue as FoundationWorld) : validateLegacyWorldRecordIndex(indexValue, previousValue)))) throw new MedievalPersistenceLayoutError('corrupt-existing-index')
      if (snapshotsValue !== undefined && !validateFoundationWorldSnapshotRing(snapshotsValue)) throw new MedievalPersistenceLayoutError('corrupt-existing-snapshots')
      if (previousValue !== undefined && collision === 'reject') throw new MedievalPersistenceLayoutError('backup-collision')
      const nextSnapshots = previous !== undefined && persistenceWorldSourceFor(previous).digest !== persistenceWorldSourceFor(world).digest ? snapshotRingAfterReplacement(snapshotsValue as FoundationWorldSnapshotRing | undefined, previous) : snapshotsValue as FoundationWorldSnapshotRing | undefined
      transaction.objectStore(WORLD_STORE).put(clone(world), world.id)
      transaction.objectStore(CATALOG_STORE).put(addWorldToIndex(isWorldIndex(catalogValue) ? catalogValue : emptyWorldIndex(), world), INDEX_KEY)
      transaction.objectStore(RECORD_INDEX_STORE).put(createWorldRecordIndex(world), world.id)
      if (nextSnapshots !== undefined) transaction.objectStore(SNAPSHOT_STORE).put(nextSnapshots, world.id)
      await completed
    } catch (error) { void completed.catch(() => undefined); try { transaction.abort() } catch { } throw persistenceError(error) }
  }
  /** Catalog, full envelope, exact derived index, and snapshot rotation share one transaction. */
  async saveWorld(world: FoundationWorld): Promise<void> { return this.writeActiveWorld(world, 'allow') }
  /**
   * The loss reducer remains pure; this is its only repository bridge. A
   * continuation atomically replaces the exact submitted active source, and
   * crew-extinction reuses the chronicle finalization transaction below.
   */
  async resolveCourierContinuityLoss(world: FoundationWorld, confirmation: CourierContinuityConfirmation): Promise<CourierContinuityResolution> {
    if (!isFoundationWorld(world)) throw new MedievalPersistenceLayoutError('invalid-submitted-world', 'refusing to resolve courier continuity for an invalid world')
    const resolution = resolveCourierContinuityTransition(world, confirmation)
    if (resolution.status === 'crew-extinction') {
      await this.finalize(world, resolution.chronicle)
      return resolution
    }
    const next = resolution.world
    const database = await this.open(); const transaction = database.transaction([CATALOG_STORE, WORLD_STORE, RECORD_INDEX_STORE, SNAPSHOT_STORE], 'readwrite'); const completed = transactionDone(transaction)
    try {
      const [active, catalogValue, index, snapshots] = await Promise.all([requestResult(transaction.objectStore(WORLD_STORE).get(world.id)), requestResult(transaction.objectStore(CATALOG_STORE).get(INDEX_KEY)), requestResult(transaction.objectStore(RECORD_INDEX_STORE).get(world.id)), requestResult(transaction.objectStore(SNAPSHOT_STORE).get(world.id))])
      if (!isFoundationWorld(active) || persistenceWorldSourceFor(active).digest !== persistenceWorldSourceFor(world).digest) throw new MedievalPersistenceLayoutError('corrupt-existing-world', 'stale source')
      if (index !== undefined && !validateWorldRecordIndex(index, active)) throw new MedievalPersistenceLayoutError('corrupt-existing-index')
      if (snapshots !== undefined && !validateFoundationWorldSnapshotRing(snapshots)) throw new MedievalPersistenceLayoutError('corrupt-existing-snapshots')
      const nextSnapshots = persistenceWorldSourceFor(active).digest === persistenceWorldSourceFor(next).digest ? snapshots as FoundationWorldSnapshotRing | undefined : snapshotRingAfterReplacement(snapshots as FoundationWorldSnapshotRing | undefined, active)
      transaction.objectStore(WORLD_STORE).put(clone(next), next.id)
      transaction.objectStore(CATALOG_STORE).put(addWorldToIndex(isWorldIndex(catalogValue) ? catalogValue : emptyWorldIndex(), next), INDEX_KEY)
      transaction.objectStore(RECORD_INDEX_STORE).put(createWorldRecordIndex(next), next.id)
      if (nextSnapshots !== undefined) transaction.objectStore(SNAPSHOT_STORE).put(nextSnapshots, next.id)
      await completed
      return resolution
    } catch (error) { void completed.catch(() => undefined); try { transaction.abort() } catch { } throw persistenceError(error) }
  }
  async restoreSnapshot(worldId: string, sequence: number): Promise<FoundationWorld> { const snapshot = await this.inspectSnapshot(worldId, sequence); if (snapshot === undefined) throw new MedievalPersistenceLayoutError('snapshot-not-found'); await this.writeActiveWorld(snapshot, 'allow'); return clone(snapshot) }
  async exportActiveWorldBackup(worldId: string): Promise<string> { const world = await this.loadWorld(worldId); if (world === undefined) throw new MedievalPersistenceLayoutError('backup-malformed', 'active world unavailable'); return serializePersistenceBackupBundle(createActiveWorldBackupBundle(world)) }
  async exportChronicleBackup(chronicleId: string): Promise<string> { const chronicle = await this.loadChronicle(chronicleId); if (chronicle === undefined) throw new MedievalPersistenceLayoutError('backup-malformed', 'chronicle unavailable'); return serializePersistenceBackupBundle(createChronicleBackupBundle(chronicle)) }
  async importActiveWorldBackup(serialized: string, options: { collision?: 'reject' | 'replace' } = {}): Promise<FoundationWorld> { const bundle = parsePersistenceBackupBundle(serialized); if (bundle.kind !== 'active-world') throw new MedievalPersistenceLayoutError('backup-incompatible', 'expected active world'); await this.writeActiveWorld(bundle.world, options.collision === 'replace' ? 'allow' : 'reject'); return clone(bundle.world) }
  async importChronicleBackup(serialized: string): Promise<WorldChronicle> {
    const bundle = parsePersistenceBackupBundle(serialized); if (bundle.kind !== 'read-only-chronicle') throw new MedievalPersistenceLayoutError('backup-incompatible', 'expected read-only chronicle')
    const database = await this.open(); const transaction = database.transaction([CATALOG_STORE, CHRONICLE_STORE], 'readwrite'); const completed = transactionDone(transaction)
    try { const [catalogValue, existing] = await Promise.all([requestResult(transaction.objectStore(CATALOG_STORE).get(INDEX_KEY)), requestResult(transaction.objectStore(CHRONICLE_STORE).get(bundle.chronicle.id))]); if (catalogValue !== undefined && !isWorldIndex(catalogValue)) throw new MedievalPersistenceLayoutError('corrupt-existing-index'); if (existing !== undefined) throw new MedievalPersistenceLayoutError('backup-collision'); transaction.objectStore(CHRONICLE_STORE).put(clone(bundle.chronicle), bundle.chronicle.id); transaction.objectStore(CATALOG_STORE).put(addChronicleToIndex(catalogValue === undefined ? emptyWorldIndex() : catalogValue as WorldIndex, bundle.chronicle), INDEX_KEY); await completed; return clone(bundle.chronicle) } catch (error) { void completed.catch(() => undefined); try { transaction.abort() } catch { } throw persistenceError(error) }
  }
  async finalize(world: FoundationWorld, chronicle: WorldChronicle): Promise<void> {
    if (!isFoundationWorld(world) || !isChronicle(chronicle) || chronicle.world.id !== world.id) throw new MedievalPersistenceLayoutError('invalid-submitted-world', 'refusing to save an invalid medieval world')
    const database = await this.open(); const transaction = database.transaction([CATALOG_STORE, WORLD_STORE, CHRONICLE_STORE, RECORD_INDEX_STORE, SNAPSHOT_STORE], 'readwrite'); const completed = transactionDone(transaction)
    try {
      const [catalogValue, active, existingChronicle, index, snapshots] = await Promise.all([requestResult(transaction.objectStore(CATALOG_STORE).get(INDEX_KEY)), requestResult(transaction.objectStore(WORLD_STORE).get(world.id)), requestResult(transaction.objectStore(CHRONICLE_STORE).get(chronicle.id)), requestResult(transaction.objectStore(RECORD_INDEX_STORE).get(world.id)), requestResult(transaction.objectStore(SNAPSHOT_STORE).get(world.id))])
      if (active !== undefined && !isFoundationWorld(active)) throw new MedievalPersistenceLayoutError('corrupt-existing-world')
      if (active !== undefined && persistenceWorldSourceFor(active).digest !== persistenceWorldSourceFor(world).digest) throw new MedievalPersistenceLayoutError('corrupt-existing-world', 'stale source')
      if (index !== undefined && active !== undefined && !validateWorldRecordIndex(index, active)) throw new MedievalPersistenceLayoutError('corrupt-existing-index')
      if (snapshots !== undefined && !validateFoundationWorldSnapshotRing(snapshots)) throw new MedievalPersistenceLayoutError('corrupt-existing-snapshots')
      if (existingChronicle !== undefined) throw new MedievalPersistenceLayoutError('backup-collision')
      const base = isWorldIndex(catalogValue) ? catalogValue : emptyWorldIndex()
      transaction.objectStore(WORLD_STORE).delete(world.id); transaction.objectStore(CHRONICLE_STORE).put(clone(chronicle), chronicle.id); transaction.objectStore(CATALOG_STORE).put(addChronicleToIndex(removeWorldFromIndex(base, world.id), chronicle), INDEX_KEY)
      await completed
    } catch (error) { void completed.catch(() => undefined); try { transaction.abort() } catch { } throw persistenceError(error) }
  }
}
