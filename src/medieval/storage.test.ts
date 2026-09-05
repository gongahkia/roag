import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { MedievalWorldRepository } from './storage'
import { CREATION_SETTINGS_PROFILE_LIMIT, defaultCreationSettings } from './settings'
import { MEDIEVAL_DATABASE_NAME } from './types'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, finalizeWorldAsChronicle, loadCargoHold, moveFoundationWorldCourier, offerFoundationWorldDelegatedTask, recordDurableJomonGrowth, replayFoundationWorldCausalHistory, switchTavernCourier, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'
import { classifyMedievalContent } from './content-safety'
import { DELEGATION_CONTRACT_VERSION, DELEGATION_TASK_DEFINITIONS, delegationTaskIdForOffer, type DelegationOfferInput } from './delegation'
import { SeededRng } from './rng'
import { assessCourierConversation } from './conversation'
import { socialMemoryRecallForPair } from './social-memory'
import { CAUSAL_HISTORY_LIMITS, causalDigestFor, causalReplayProjectionDigest } from './causal-history'
import { captureTerminalControlBinding, defaultTerminalControlPreferences } from './terminal-controls'
import { createActiveWorldBackupBundle, createChronicleBackupBundle, serializePersistenceBackupBundle } from './persistence-layout'
import { courierContinuityConfirmationIdFor, courierContinuityContentSafety } from './courier-continuity'

/** A fully token-valid state-v15 source for the read-only cargo bridge. */
const stateV15Envelope = () => {
  const legacy = structuredClone(chooseInitialCourier(createFoundationWorld({ seed: 'storage-cargo-v15' }), 'crew:0')) as unknown as Record<string, any>
  const checkpoint = legacy.state.causalHistory.checkpoint
  const projection = structuredClone(checkpoint.projection)
  projection.version = 7
  projection.jomon.version = 2
  delete projection.jomon.cargo
  const stateDigest = causalDigestFor('causal-replay-projection', projection)
  checkpoint.version = 5
  checkpoint.stateDigest = stateDigest
  checkpoint.id = `causal-checkpoint:${checkpoint.sequence}:${causalDigestFor('causal-checkpoint-id', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: checkpoint.sequence, stateDigest })}`
  checkpoint.token = causalDigestFor('causal-checkpoint-token', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: checkpoint.sequence, atWorldTime: checkpoint.atWorldTime, stateDigest })
  checkpoint.projection = projection
  legacy.state.causalHistory.version = 5
  legacy.state.causalHistory.tail = legacy.state.causalHistory.tail.map((command: Record<string, unknown>) => ({ ...command, version: 5 }))
  legacy.state.jomon.version = 2
  delete legacy.state.jomon.cargo
  legacy.state.version = 15
  return legacy
}

type Handler = (() => void) | null

class FakeRequest<T> {
  result!: T
  error: Error | null = null
  onsuccess: Handler = null
  onerror: Handler = null
}

class FakeTransaction {
  oncomplete: Handler = null
  onerror: Handler = null
  onabort: Handler = null
  error: Error | null = null
  private pending = 0
  private finished = false
  private readonly stores: Map<string, Map<unknown, unknown>>

  constructor(private readonly database: FakeDatabase, private failure: 'abort' | 'quota' | undefined) {
    this.stores = new Map([...database.stores].map(([name, values]) => [name, new Map([...values].map(([key, value]) => [key, structuredClone(value)]))]))
  }

  objectStore(name: string): FakeObjectStore {
    if (!this.stores.has(name)) throw new Error(`unknown store ${name}`)
    return new FakeObjectStore(this, name)
  }

  request<T>(operation: () => T, isWrite = false): FakeRequest<T> {
    const request = new FakeRequest<T>()
    this.pending++
    queueMicrotask(() => {
      if (this.finished) return
      try {
        if (isWrite && this.failure !== undefined) {
          const kind = this.failure
          this.failure = undefined
          throw new DOMException(kind === 'quota' ? 'quota' : 'aborted', kind === 'quota' ? 'QuotaExceededError' : 'AbortError')
        }
        request.result = operation()
        request.onsuccess?.()
      } catch (error) {
        request.error = error instanceof Error ? error : new Error('fake IndexedDB write failed')
        this.error = request.error
        request.onerror?.()
        this.abort()
      }
      this.pending--
      this.completeWhenIdle()
    })
    return request
  }

  get(store: string, key: unknown): unknown { return this.stores.get(store)?.get(key) }
  put(store: string, key: unknown, value: unknown): void { this.stores.get(store)?.set(key, structuredClone(value)) }
  delete(store: string, key: unknown): void { this.stores.get(store)?.delete(key) }

  abort(): void {
    if (this.finished) return
    this.finished = true
    this.onabort?.()
  }

  private completeWhenIdle(): void {
    if (this.finished || this.pending > 0) return
    // IndexedDB stays active through the current task. A later macrotask lets
    // the repository read the current records and enqueue its atomic writes.
    setTimeout(() => {
      if (this.finished || this.pending > 0) return
      this.finished = true
      this.database.stores = this.stores
      this.oncomplete?.()
    }, 0)
  }
}

class FakeObjectStore {
  constructor(private readonly transaction: FakeTransaction, private readonly name: string) { }
  get(key: unknown): IDBRequest<unknown> { return this.transaction.request(() => this.transaction.get(this.name, key)) as unknown as IDBRequest<unknown> }
  put(value: unknown, key: unknown): IDBRequest<unknown> { return this.transaction.request(() => { this.transaction.put(this.name, key, value); return key }, true) as unknown as IDBRequest<unknown> }
  delete(key: unknown): IDBRequest<undefined> { return this.transaction.request(() => { this.transaction.delete(this.name, key); return undefined }, true) as unknown as IDBRequest<undefined> }
}

class FakeDatabase {
  stores = new Map<string, Map<unknown, unknown>>()
  version = 0
  nextWriteFailure: 'abort' | 'quota' | undefined
  readonly objectStoreNames = { contains: (name: string): boolean => this.stores.has(name) }

  transaction(): IDBTransaction { const failure = this.nextWriteFailure; this.nextWriteFailure = undefined; return new FakeTransaction(this, failure) as unknown as IDBTransaction }
  createObjectStore(name: string): IDBObjectStore {
    this.stores.set(name, new Map())
    return {} as IDBObjectStore
  }
}

class FakeIndexedDB {
  readonly openedNames: string[] = []
  private readonly databases = new Map<string, FakeDatabase>()

  open(name: string, version?: number): IDBOpenDBRequest {
    this.openedNames.push(name)
    const request = new FakeRequest<FakeDatabase>() as FakeRequest<FakeDatabase> & { onupgradeneeded: Handler; onblocked: Handler }
    request.onupgradeneeded = null
    request.onblocked = null
    const existing = this.databases.get(name)
    const database = existing ?? new FakeDatabase()
    const upgrading = !existing || (version !== undefined && version > database.version)
    if (version !== undefined && version > database.version) database.version = version
    this.databases.set(name, database)
    request.result = database
    queueMicrotask(() => {
      if (upgrading) request.onupgradeneeded?.()
      request.onsuccess?.()
    })
    return request as unknown as IDBOpenDBRequest
  }

  store(name: string, store: string): Map<unknown, unknown> {
    const database = this.databases.get(name)
    if (!database) throw new Error(`database ${name} was not opened`)
    const records = database.stores.get(store)
    if (!records) throw new Error(`store ${store} was not created`)
    return records
  }

  failNextWrite(name: string, kind: 'abort' | 'quota'): void {
    const database = this.databases.get(name)
    if (!database) throw new Error(`database ${name} was not opened`)
    database.nextWriteFailure = kind
  }

  seedV3(name: string): FakeDatabase {
    const database = new FakeDatabase()
    database.version = 3
    for (const store of ['catalog', 'worlds', 'chronicles', 'creation-settings', 'terminal-controls']) database.createObjectStore(store)
    this.databases.set(name, database)
    return database
  }
}

const originalIndexedDB = globalThis.indexedDB
let fakeIndexedDB: FakeIndexedDB

// Immutable fixtures are constructed outside individual timeout windows; each
// test still clones its submitted envelope before a repository transition.
const continuityStorageSource = chooseInitialCourier(createFoundationWorld({ seed: 'storage-courier-continuity' }), 'crew:0')
const continuityStorageAlternate = switchTavernCourier(continuityStorageSource, 'crew:1')
/** Built outside test windows; the repository still receives only a cloned full envelope. */
const cargoStorageSource = (() => {
  let world = chooseInitialCourier(createFoundationWorld({ seed: 'storage-cargo-round-trip' }), 'crew:0')
  for (const direction of ['east', 'east', 'east', 'east', 'east', 'east'] as const) {
    const moved = moveFoundationWorldCourier(world, direction)
    if (moved.status !== 'moved') throw new Error('cargo storage fixture could not reach the hold')
    world = moved.world
  }
  return loadCargoHold(world, 'commodity:paper', 1)
})()
const continuityStorageConfirmation = {
  version: 1 as const,
  id: courierContinuityConfirmationIdFor('departure', 'crew:0', 0),
  kind: 'confirmed-courier-continuity-loss' as const,
  outcome: 'departure' as const,
  courierId: 'crew:0',
  atWorldTime: 0,
  evidenceIds: ['loss-evidence:storage-continuity'] as const,
  contentSafety: courierContinuityContentSafety()
}

beforeEach(() => {
  fakeIndexedDB = new FakeIndexedDB()
  Object.defineProperty(globalThis, 'indexedDB', { configurable: true, value: fakeIndexedDB })
})

afterEach(() => {
  Object.defineProperty(globalThis, 'indexedDB', { configurable: true, value: originalIndexedDB })
})

const delegatedWorld = (seed: string) => {
  const world = chooseInitialCourier(createFoundationWorld({ seed }), 'crew:0')
  const courierId = world.state.courier.initialCourierId!
  for (const recipient of world.state.people.records.filter(person => person.id !== courierId)) {
    for (const definition of DELEGATION_TASK_DEFINITIONS) {
      const interest = definition.relevantMaterialInterests.find(item => recipient.materialInterests.includes(item))
      const skill = definition.relevantSkills.find(item => recipient.work.skills.some(candidate => candidate.kind === item && candidate.level >= 1))
      if (!interest || !skill) continue
      for (let attempt = 0; attempt < 8; attempt++) {
        const id = `storage-delegation:${recipient.id}:${definition.family}:${attempt}`
        const roll = new SeededRng(`jomon-delegation:${DELEGATION_CONTRACT_VERSION}:${world.id}:${world.manifest.creation.digest}:agreement:${delegationTaskIdForOffer(id)}:1`).integer(6)
        if (roll !== 0) continue
        const proposal = {
          version: DELEGATION_CONTRACT_VERSION,
          kind: 'request' as const,
          urgency: 'routine' as const,
          complexity: 'routine' as const,
          materialInterest: interest,
          contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
        }
        const offer: DelegationOfferInput = { version: DELEGATION_CONTRACT_VERSION, id, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal }
        const delegated = offerFoundationWorldDelegatedTask(world, offer)
        if (delegated.state.delegation.tasks[0]!.status === 'in-progress') return delegated
      }
    }
  }
  throw new Error('fixture did not create an accepted delegated task')
}

/** Uses the public assessment/offer boundary; an interest mismatch always remains a real refusal. */
const recurringRefusalOffer = (world: ReturnType<typeof createFoundationWorld>, id: string, recipientId?: string): DelegationOfferInput => {
  const courierId = world.state.courier.initialCourierId
  if (courierId === undefined) throw new Error('fixture requires a selected courier')
  const recipients = [...world.state.people.records]
    .filter(person => person.id !== courierId && (recipientId === undefined || person.id === recipientId))
    .sort((left, right) => left.id.localeCompare(right.id))
  for (const recipient of recipients) {
    const definition = DELEGATION_TASK_DEFINITIONS.find(candidate => candidate.relevantMaterialInterests.every(interest => !recipient.materialInterests.includes(interest)))
    if (!definition) continue
    const proposal = {
      version: DELEGATION_CONTRACT_VERSION,
      kind: 'request' as const,
      urgency: 'routine' as const,
      complexity: 'routine' as const,
      materialInterest: definition.relevantMaterialInterests[0]!,
      contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
    }
    const assessment = assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId: recipient.id, proposal })
    if (assessment.eligibility === 'eligible' && assessment.unlockedApproaches.includes('direct-request')) {
      return { version: DELEGATION_CONTRACT_VERSION, id, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal }
    }
  }
  throw new Error('fixture did not find a public refusal offer')
}

describe('medieval local persistence', () => {
  it('persists bounded cargo only through the complete authoritative envelope', async () => {
    const repository = new MedievalWorldRepository()
    const source = structuredClone(cargoStorageSource)

    await repository.saveWorld(source)
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(source.id)).toEqual(source)
  })

  it('reloads bounded cargo through the full authoritative envelope', async () => {
    const repository = new MedievalWorldRepository()
    const source = structuredClone(cargoStorageSource)
    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(source.id, structuredClone(source))

    const loaded = await repository.loadWorld(source.id)
    expect(loaded).toEqual(source)
    expect(loaded?.state.jomon.cargo).toEqual({ version: 1, lots: [{ id: 'cargo:8:commodity:paper', commodityId: 'commodity:paper', quantity: 1, condition: 'sound', status: 'in-hold' }] })
    expect(loaded && replayFoundationWorldCausalHistory(loaded)).toEqual(causalReplayProjectionForWorldState(source.state))
  })

  it('rejects forged cargo lots without rewriting the submitted record', async () => {
    const repository = new MedievalWorldRepository()
    const source = structuredClone(cargoStorageSource)
    await repository.loadIndex()
    const forged = structuredClone(source)
    forged.state.jomon.cargo.lots[0]!.quantity = 0
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(source.id, forged)
    expect(await repository.loadWorld(source.id)).toBeUndefined()
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(source.id)).toEqual(forged)
  })

  it('round-trips the selected zero-time courier and canonical deck spawn through the full authoritative envelope', async () => {
    const repository = new MedievalWorldRepository()
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'storage-initial-courier-selection' }), 'crew:1')

    await repository.saveWorld(selected)
    const loaded = await repository.loadWorld(selected.id)

    expect(loaded).toEqual(selected)
    expect(loaded?.state.courier.initialCourierId).toBe('crew:1')
    expect(loaded?.state.navigation).toEqual({ version: 1, courierId: 'crew:1', coordinate: { column: 4, row: 4 } })
    expect(loaded && replayFoundationWorldCausalHistory(loaded)).toEqual(causalReplayProjectionForWorldState(selected.state))
  })

  it('round-trips a zero-time tavern courier switch through the full authoritative envelope', async () => {
    const repository = new MedievalWorldRepository()
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'storage-tavern-courier-switch' }), 'crew:0')
    const switched = switchTavernCourier(selected, 'crew:1')

    await repository.saveWorld(switched)
    const loaded = await repository.loadWorld(switched.id)

    expect(loaded).toEqual(switched)
    expect(loaded?.state.courier).toEqual({ version: 3, initialCourierId: 'crew:0', activeCourierId: 'crew:1', departedCourierIds: [] })
    expect(loaded?.state.navigation).toEqual({ version: 1, courierId: 'crew:1', coordinate: { column: 4, row: 4 } })
    expect(loaded?.state.jomon.propActions.records.find(record => record.propId === 'prop:task-ledger')).toMatchObject({ latestAction: { kind: 'tavern-courier-switched', recordedAtWorldTime: 0, causalSequence: 2 } })
    expect(loaded && replayFoundationWorldCausalHistory(loaded)).toEqual(causalReplayProjectionForWorldState(switched.state))
  })

  it('atomically persists a validated courier-continuity successor', async () => {
    const repository = new MedievalWorldRepository()
    const source = structuredClone(continuityStorageSource)
    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(source.id, structuredClone(source))
    const result = await repository.resolveCourierContinuityLoss(source, continuityStorageConfirmation)

    expect(result).toMatchObject({ status: 'continued', world: { state: { courier: { initialCourierId: 'crew:0', activeCourierId: 'crew:1', departedCourierIds: ['crew:0'] }, navigation: { courierId: 'crew:1', coordinate: { column: 4, row: 4 } } } } })
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(source.id)).toEqual(result.status === 'continued' ? result.world : undefined)
  })

  it('rejects a stale courier-continuity source without overwriting the active envelope', async () => {
    const repository = new MedievalWorldRepository()
    const source = structuredClone(continuityStorageSource)
    const alternate = structuredClone(continuityStorageAlternate)
    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(source.id, structuredClone(alternate))

    await expect(repository.resolveCourierContinuityLoss(source, continuityStorageConfirmation)).rejects.toMatchObject({ code: 'corrupt-existing-world' })
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(source.id)).toEqual(alternate)
  })

  it('reads a valid v13 full envelope through the strict v14 navigation conversion without overwriting corrupt input', async () => {
    const repository = new MedievalWorldRepository()
    await repository.loadIndex()
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'storage-navigation-upgrade' }), 'crew:0')
    const legacy = structuredClone(selected) as unknown as Record<string, any>
    legacy.version = 13
    legacy.jomon.props = ['prop:chart-table', 'prop:task-ledger', 'prop:gangplank'].map(id => structuredClone(selected.jomon.props.find(prop => prop.id === id)!))
    legacy.state.version = 11
    delete legacy.state.navigation
    legacy.state.courier = { version: 1, initialCourierId: selected.state.courier.initialCourierId }
    const checkpointProjection = structuredClone(legacy.state.causalHistory.checkpoint.projection)
    delete checkpointProjection.jomon
    checkpointProjection.version = 4
    checkpointProjection.courier = checkpointProjection.courier.initialCourierId === undefined
      ? { version: 1 }
      : { version: 1, initialCourierId: checkpointProjection.courier.initialCourierId }
    delete checkpointProjection.navigation
    const stateDigest = causalReplayProjectionDigest(checkpointProjection)
    legacy.state.causalHistory = {
      ...legacy.state.causalHistory,
      version: 4,
      tail: legacy.state.causalHistory.tail.map((command: Record<string, unknown>) => ({ ...command, version: 4 })),
      checkpoint: {
        version: 4,
        id: `causal-checkpoint:0:${causalDigestFor('causal-checkpoint-id', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: 0, stateDigest })}`,
        token: causalDigestFor('causal-checkpoint-token', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: 0, atWorldTime: 0, stateDigest }),
        sequence: 0,
        atWorldTime: 0,
        provenanceDigest: legacy.manifest.creation.digest,
        stateDigest,
        projection: checkpointProjection
      }
    }
    legacy.state.jomon.version = 1
    delete legacy.state.jomon.propActions
    delete legacy.state.jomon.cargo
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(selected.id, structuredClone(legacy))

    const loaded = await repository.loadWorld(selected.id)
    expect(loaded).toMatchObject({ version: 15, state: { version: 16, jomon: { version: 3, cargo: { version: 1, lots: [] } }, courier: { version: 3, initialCourierId: 'crew:0', activeCourierId: 'crew:0', departedCourierIds: [] }, navigation: { courierId: 'crew:0', coordinate: { column: 4, row: 4 } } } })
    expect((fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(selected.id) as { version: number }).version).toBe(13)

    const corrupt = structuredClone(legacy)
    corrupt.state.courier.initialCourierId = 'crew:unknown'
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(selected.id, corrupt)
    expect(await repository.loadWorld(selected.id)).toBeUndefined()
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(selected.id)).toEqual(corrupt)
  }, 15_000)

  it('reads only an exact state-v15 cargo source into state-v16 without rewriting storage', async () => {
    const repository = new MedievalWorldRepository()
    await repository.loadIndex()
    const legacy = stateV15Envelope()
    const before = structuredClone(legacy)
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(legacy.id, structuredClone(legacy))

    const loaded = await repository.loadWorld(legacy.id)
    expect(loaded).toMatchObject({ version: 15, state: { version: 16, jomon: { version: 3, cargo: { version: 1, lots: [] } } } })
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(legacy.id)).toEqual(before)

    const forged = stateV15Envelope()
    forged.state.jomon.propActions.records.reverse()
    const forgedBefore = structuredClone(forged)
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(forged.id, forged)
    expect(await repository.loadWorld(forged.id)).toBeUndefined()
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(forged.id)).toEqual(forgedBefore)
  })

  it('reads only exact v14 three-prop active worlds and chronicles through the v15 static-prop conversion without rewriting either record', async () => {
    const repository = new MedievalWorldRepository()
    await repository.loadIndex()
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'storage-v15-static-prop-upgrade' }), 'crew:0')
    const legacy = structuredClone(selected) as unknown as Record<string, any>
    legacy.version = 14
    legacy.jomon.props = ['prop:chart-table', 'prop:task-ledger', 'prop:gangplank'].map(id => structuredClone(selected.jomon.props.find(prop => prop.id === id)!))
    const checkpointProjection = structuredClone(legacy.state.causalHistory.checkpoint.projection)
    delete checkpointProjection.jomon
    checkpointProjection.version = 6
    const checkpoint = legacy.state.causalHistory.checkpoint
    const stateDigest = causalDigestFor('causal-replay-projection', checkpointProjection)
    const sequence = checkpoint.sequence
    checkpoint.version = 4
    checkpoint.stateDigest = stateDigest
    checkpoint.id = `causal-checkpoint:${sequence}:${causalDigestFor('causal-checkpoint-id', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence, stateDigest })}`
    checkpoint.token = causalDigestFor('causal-checkpoint-token', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence, atWorldTime: checkpoint.atWorldTime, stateDigest })
    checkpoint.projection = checkpointProjection
    legacy.state.causalHistory.version = 4
    legacy.state.causalHistory.tail = legacy.state.causalHistory.tail.map((command: Record<string, unknown>) => ({ ...command, version: 4 }))
    legacy.state.jomon.version = 1
    delete legacy.state.jomon.propActions
    delete legacy.state.jomon.cargo
    legacy.state.version = 14
    const legacyChronicle = { version: 12, id: `chronicle:${selected.id}`, status: 'finalized' as const, reason: 'jomon-loss' as const, world: structuredClone(legacy) }
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(selected.id, structuredClone(legacy))
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'chronicles').set(legacyChronicle.id, structuredClone(legacyChronicle))

    const loadedWorld = await repository.loadWorld(selected.id)
    const loadedChronicle = await repository.loadChronicle(legacyChronicle.id)
    expect(loadedWorld?.version).toBe(15)
    expect(loadedWorld?.jomon.props.map(prop => prop.id)).toEqual(expect.arrayContaining(['prop:galley-hearth', 'prop:repair-space-rack']))
    expect(loadedChronicle?.version).toBe(12)
    expect(loadedChronicle?.world.version).toBe(15)
    expect(loadedChronicle?.world.jomon.props.map(prop => prop.id)).toEqual(expect.arrayContaining(['prop:stores-rack', 'prop:berth']))
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(selected.id)).toEqual(legacy)
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'chronicles').get(legacyChronicle.id)).toEqual(legacyChronicle)

    const corrupt = structuredClone(legacy)
    corrupt.jomon.props[0].kind = 'ledger'
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(selected.id, corrupt)
    expect(await repository.loadWorld(selected.id)).toBeUndefined()
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(selected.id)).toEqual(corrupt)
  })

  it('uses only the medieval database and never reads or migrates the prototype database', async () => {
    const repository = new MedievalWorldRepository()

    await repository.loadIndex()

    expect(fakeIndexedDB.openedNames).toEqual([MEDIEVAL_DATABASE_NAME])
    expect(fakeIndexedDB.openedNames).not.toContain('jomon-expedition-v2')
  })

  it('persists validated last-used settings and bounded named profiles only in the medieval settings store', async () => {
    const repository = new MedievalWorldRepository()
    const selected = { ...defaultCreationSettings(), seed: 'lower quay', configuration: { preset: 'far-coast' as const, advanced: { historyYears: 350 } }, advancedMode: true }

    await expect(repository.loadCreationSettings()).resolves.toMatchObject({ version: 1, lastUsed: defaultCreationSettings(), profiles: [] })
    await repository.saveLastUsedCreationSettings(selected)
    for (let index = 0; index < CREATION_SETTINGS_PROFILE_LIMIT; index++) await repository.saveCreationSettingsProfile(`profile ${index}`, { ...selected, seed: `profile seed ${index}` })

    const loaded = await repository.loadCreationSettings()
    expect(loaded.lastUsed).toEqual({ ...selected, seed: 'profile seed 5' })
    expect(loaded.profiles).toHaveLength(CREATION_SETTINGS_PROFILE_LIMIT)
    await repository.saveCreationSettingsProfile('profile 2', { ...selected, seed: 'replacement' })
    expect((await repository.loadCreationSettings()).profiles[2]).toMatchObject({ name: 'profile 2', settings: { seed: 'replacement' } })
    await expect(repository.saveCreationSettingsProfile('seventh', selected)).rejects.toThrow('at most 6')
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'creation-settings').has('last-used-and-profiles')).toBe(true)
  })

  it('rejects malformed local creation settings rather than interpreting them as a usable profile', async () => {
    const repository = new MedievalWorldRepository()
    await repository.loadCreationSettings()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'creation-settings').set('last-used-and-profiles', { version: 1, lastUsed: { seed: 'prototype' }, profiles: [] })

    await expect(repository.loadCreationSettings()).resolves.toEqual({ version: 1, lastUsed: defaultCreationSettings(), profiles: [] })
  })

  it('persists only validated terminal-control preferences and safely falls back from corrupt UI records without touching worlds or settings', async () => {
    const repository = new MedievalWorldRepository()
    const settings = { ...defaultCreationSettings(), seed: 'controls-independent' }
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'controls-independent' }), 'crew:0')
    const captured = captureTerminalControlBinding(defaultTerminalControlPreferences(), 'move-west', { key: 'Q' })
    if (captured.status !== 'accepted') throw new Error('controls fixture requires an accepted key capture')

    await repository.saveLastUsedCreationSettings(settings)
    await repository.saveWorld(world)
    await repository.saveTerminalControls(captured.preferences)
    expect(await repository.loadTerminalControls()).toEqual(captured.preferences)
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'terminal-controls').has('world-controls')).toBe(true)

    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'terminal-controls').set('world-controls', { version: 999, bindings: [] })
    await expect(repository.loadTerminalControls()).resolves.toEqual(defaultTerminalControlPreferences())
    expect(await repository.loadWorld(world.id)).toEqual(world)
    expect((await repository.loadCreationSettings()).lastUsed).toEqual(settings)
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'terminal-controls').get('world-controls')).toEqual({ version: 999, bindings: [] })
  })

  it('persists separate active worlds without loading or mutating one through the other', async () => {
    const repository = new MedievalWorldRepository()
    const first = chooseInitialCourier(createFoundationWorld({ seed: 'first-barge' }), 'crew:0')
    const second = chooseInitialCourier(createFoundationWorld({ seed: 'second-barge' }), 'crew:1')

    await repository.saveWorld(first)
    await repository.saveWorld(second)
    const index = await repository.loadIndex()
    const loadedFirst = await repository.loadWorld(first.id)

    expect(index.activeWorlds.map(entry => entry.id)).toEqual([first.id, second.id].sort())
    expect(loadedFirst).toEqual(first)
    if (!loadedFirst) throw new Error('saved medieval world should load')
    loadedFirst.state.jomon.integrity.current = 1
    expect(await repository.loadWorld(first.id)).toEqual(first)
    expect(await repository.loadWorld(second.id)).toEqual(second)
    expect(await repository.loadWorld('world:not-present')).toBeUndefined()
  }, 30_000)

  it('keeps valid local creation settings intact while saving and loading the current full-world record', async () => {
    const repository = new MedievalWorldRepository()
    const settings = { ...defaultCreationSettings(), seed: 'settings-survive-state', configuration: { preset: 'far-coast' as const, advanced: {} } }
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'settings-survive-state', configuration: settings.configuration }), 'crew:0')

    await repository.saveLastUsedCreationSettings(settings)
    await repository.saveWorld(world)

    expect((await repository.loadCreationSettings()).lastUsed).toEqual(settings)
    expect(await repository.loadWorld(world.id)).toEqual(world)
  })

  it('round-trips delegated person/task state and replays a loaded active task through its canonical completion', async () => {
    const repository = new MedievalWorldRepository()
    const delegated = delegatedWorld('storage-delegation-round-trip')
    const task = delegated.state.delegation.tasks[0]!

    await repository.saveWorld(delegated)
    const loaded = await repository.loadWorld(delegated.id)
    expect(loaded).toEqual(delegated)
    if (!loaded) throw new Error('saved delegated world should load')
    expect(replayFoundationWorldCausalHistory(loaded)).toEqual(causalReplayProjectionForWorldState(loaded.state))

    const completed = advanceFoundationWorldTime(loaded, {
      id: 'wait:storage-delegation-completion',
      kind: 'wait',
      durationMinutes: task.plannedCompletionAtWorldTime! - loaded.state.temporal.worldTime,
      contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
    })
    await repository.saveWorld(completed)
    const reloadedCompleted = await repository.loadWorld(completed.id)
    expect(reloadedCompleted?.state.delegation.tasks[0]).toMatchObject({ id: task.id, status: 'completed' })
    expect(reloadedCompleted?.state.socialMemory).toEqual(completed.state.socialMemory)
    expect(reloadedCompleted?.state.autonomy).toEqual(completed.state.autonomy)
    expect(reloadedCompleted && replayFoundationWorldCausalHistory(reloadedCompleted)).toEqual(causalReplayProjectionForWorldState(completed.state))
  }, 10_000)

  it('reloads recurring social refusals after public journal compaction and continues their real source-linked history', async () => {
    const repository = new MedievalWorldRepository()
    let world = chooseInitialCourier(createFoundationWorld({ seed: 'storage-recurring-social-refusals', configuration: { preset: 'far-coast' } }), 'crew:0')
    const immutableFoundationMemories = world.state.people.records.map(person => ({ id: person.id, memories: person.memories.filter(memory => memory.kind === 'foundation-history') }))
    const personCount = world.state.people.records.length
    let recipientId: string | undefined

    for (let index = 0; index < CAUSAL_HISTORY_LIMITS.retainedCommands; index++) {
      const offer = recurringRefusalOffer(world, `storage-recurring-refusal:${String(index).padStart(2, '0')}`, recipientId)
      recipientId ??= offer.recipientId
      world = offerFoundationWorldDelegatedTask(world, offer)
      expect(world.state.delegation.tasks.find(task => task.offerId === offer.id)?.status).toBe('refused')
    }
    if (recipientId === undefined) throw new Error('fixture requires a repeated recipient')
    const courierId = world.state.courier.initialCourierId!
    const recurringRecipient = world.state.people.records.find(person => person.id === recipientId)!

    expect(world.state.socialMemory.records).toHaveLength(CAUSAL_HISTORY_LIMITS.retainedCommands)
    expect(world.state.socialMemory.records.map(record => record.id)).toEqual([...world.state.socialMemory.records.map(record => record.id)].sort())
    expect(world.state.socialMemory.records.every(record => record.retention === 'participant-retained')).toBe(true)
    expect(recurringRecipient.memories.filter(memory => memory.kind === 'social-memory')).toHaveLength(CAUSAL_HISTORY_LIMITS.retainedCommands)
    expect(world.state.people.records.map(person => ({ id: person.id, memories: person.memories.filter(memory => memory.kind === 'foundation-history') }))).toEqual(immutableFoundationMemories)
    expect(world.state.people.records).toHaveLength(personCount)
    expect(world.state.causalHistory.checkpoint.sequence).toBeGreaterThan(0)
    expect(socialMemoryRecallForPair(world.state.socialMemory, courierId, recipientId)).toMatchObject({ band: 'unsettled' })
    expect(replayFoundationWorldCausalHistory(world)).toEqual(causalReplayProjectionForWorldState(world.state))

    await repository.saveWorld(world)
    const loaded = await repository.loadWorld(world.id)
    if (!loaded) throw new Error('valid recurring social history should reload')
    expect(loaded).toEqual(world)

    const continuedOffer = recurringRefusalOffer(loaded, 'storage-recurring-refusal:continued', recipientId)
    const continued = offerFoundationWorldDelegatedTask(loaded, continuedOffer)
    expect(continued.state.delegation.tasks.find(task => task.offerId === continuedOffer.id)?.status).toBe('refused')
    expect(continued.state.people.records).toHaveLength(personCount)
    expect(socialMemoryRecallForPair(continued.state.socialMemory, courierId, recipientId)).toMatchObject({ band: 'unsettled' })
    expect(replayFoundationWorldCausalHistory(continued)).toEqual(causalReplayProjectionForWorldState(continued.state))
  }, 20_000)

  it('contains forged autonomy, task, and social-memory state at both full-world and local-storage boundaries', async () => {
    const repository = new MedievalWorldRepository()
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'storage-forged-social-domains', configuration: { preset: 'far-coast' } }), 'crew:0')
    const refused = offerFoundationWorldDelegatedTask(selected, recurringRefusalOffer(selected, 'storage-forged-social-domains:refusal'))
    const valid = advanceFoundationWorldTime(refused, {
      id: 'storage-forged-social-domains:observe', kind: 'wait', durationMinutes: 120,
      contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
    })
    const forgedAutonomy = structuredClone(valid)
    ;(forgedAutonomy.state.autonomy.observations[0] as { token: number }).token++
    const forgedTask = structuredClone(valid)
    ;(forgedTask.state.delegation.tasks[0] as { outcome: { token: number } }).outcome.token++
    const forgedSocial = structuredClone(valid)
    forgedSocial.state.socialMemory.records[0]!.token = 'forged'

    await repository.saveWorld(valid)
    for (const [kind, forged] of [['autonomy', forgedAutonomy], ['task', forgedTask], ['social', forgedSocial]] as const) {
      expect(validateFoundationWorld(forged)).not.toEqual([])
      await expect(repository.saveWorld(forged)).rejects.toThrow('invalid medieval world')
      fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(`world:forged-${kind}`, forged)
      await expect(repository.loadWorld(`world:forged-${kind}`)).resolves.toBeUndefined()
    }
    expect(await repository.loadWorld(valid.id)).toEqual(valid)
  }, 15_000)

  it('rejects malformed local records instead of treating them as a medieval world', async () => {
    const repository = new MedievalWorldRepository()
    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set('world:bad', { version: 6, hero: { name: 'prototype' } })

    await expect(repository.loadWorld('world:bad')).resolves.toBeUndefined()
  })

  it('rejects the earlier medieval manifest schema instead of inferring or migrating its missing frontier provenance', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'clean-break-manifest' })
    const legacy = structuredClone(world) as unknown as { manifest: unknown }
    legacy.manifest = { version: 4, seed: 'clean-break-manifest' }

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, legacy)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects the older medieval world envelope rather than inferring its missing scheduler state', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'clock-envelope-clean-break' })
    const legacy = structuredClone(world) as unknown as { version: number; state?: unknown }
    legacy.version = 2
    delete legacy.state

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, legacy)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects the v3 mutable-world envelope rather than inventing persistent person records', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'people-envelope-clean-break' })
    const legacy = structuredClone(world) as unknown as { version: number; state: { version: number; people: { version: number; registry?: unknown[]; records?: unknown[] } } }
    legacy.version = 3
    legacy.state.version = 1
    legacy.state.people.version = 1
    legacy.state.people.registry = legacy.state.people.records
    delete legacy.state.people.records

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, legacy)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects the immediately prior world/person envelopes rather than migrating incomplete v1 people', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'persistent-person-v2-clean-break' })
    const legacy = structuredClone(world) as unknown as {
      version: number
      state: { version: number; people: { version: number; records: Array<Record<string, unknown>> } }
    }
    legacy.version = 9
    legacy.state.version = 7
    legacy.state.people.version = 2
    legacy.state.people.records[0]!.version = 1
    delete legacy.state.people.records[0]!.household
    delete legacy.state.people.records[0]!.home
    delete legacy.state.people.records[0]!.materialInterests

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, legacy)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects the pre-delegation mutable/world envelopes rather than inventing task and replay state', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'delegation-envelope-clean-break' })
    const legacy = structuredClone(world) as unknown as { version: number; state: { version: number; delegation?: unknown; people: { version: number; records: Array<Record<string, unknown>> }; causalHistory: { version: number } } }
    legacy.version = 10
    legacy.state.version = 8
    legacy.state.people.version = 3
    legacy.state.causalHistory.version = 1
    delete legacy.state.delegation

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, legacy)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects the v6 mutable-world envelope rather than inventing canonical catch-up windows', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'catch-up-envelope-clean-break' })
    const legacy = structuredClone(world) as unknown as { version: number; state: { version: number; simulation?: unknown } }
    legacy.version = 6
    legacy.state.version = 4
    delete legacy.state.simulation

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, legacy)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects the v7 mutable-world envelope rather than inventing durable era evidence', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'era-envelope-clean-break' })
    const legacy = structuredClone(world) as unknown as { version: number; state: { version: number; era?: unknown } }
    legacy.version = 7
    legacy.state.version = 5
    delete legacy.state.era

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, legacy)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects the immediate pre-social-memory world envelope instead of inventing linked recall', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'social-memory-envelope-clean-break' })
    const legacy = structuredClone(world) as unknown as { version: number; state: { version: number; people: { version: number }; socialMemory?: unknown; causalHistory: { version: number } } }
    legacy.version = 12
    legacy.state.version = 10
    legacy.state.people.version = 4
    legacy.state.causalHistory.version = 3
    delete legacy.state.socialMemory

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, legacy)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('persists and reloads a validated pending scheduler event without changing immutable manifest provenance', async () => {
    const repository = new MedievalWorldRepository()
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'clock-persistence' }), 'crew:0')
    const advanced = advanceFoundationWorldTime(world, {
      id: 'movement:moor',
      kind: 'movement',
      durationMinutes: 1,
      contentSafety: classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text']),
      events: [{
        id: 'event:moor',
        dueAtWorldTime: 2,
        priority: 'ordinary',
        payload: { kind: 'action-resolution', sourceActionId: 'movement:moor', creationDigest: world.manifest.creation.digest },
        contentSafety: classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])
      }]
    })

    await repository.saveWorld(advanced)

    expect(await repository.loadWorld(advanced.id)).toEqual(advanced)
    expect(advanced.manifest).toEqual(world.manifest)
    expect(advanced.state.temporal.pendingEvents.map(event => event.id)).toEqual(['event:moor'])
  })

  it('persists deterministic catch-up cursors and outcomes across a local reload', async () => {
    const repository = new MedievalWorldRepository()
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'catch-up-persistence' }), 'crew:0')
    const advanced = advanceFoundationWorldTime(world, {
      id: 'wait:catch-up-persistence',
      kind: 'wait',
      durationMinutes: 240,
      contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
    })

    await repository.saveWorld(advanced)

    expect(await repository.loadWorld(advanced.id)).toEqual(advanced)
    expect(advanced.state.simulation.cursors.length).toBeGreaterThan(0)
    expect(advanced.state.simulation.records.length).toBeGreaterThan(0)
  })

  it('persists canonical era growth and transition evidence across a local reload', async () => {
    const repository = new MedievalWorldRepository()
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'era-reload' }), 'crew:0')
    const advanced = advanceFoundationWorldTime(selected, {
      id: 'travel:era-reload',
      kind: 'travel',
      durationMinutes: 4_800,
      contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
    })
    const grown = recordDurableJomonGrowth(advanced, {
      id: 'growth:era-reload:small-craft',
      kind: 'small-craft',
      source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
      atWorldTime: 4_800
    })

    await repository.saveWorld(grown)

    const loaded = await repository.loadWorld(grown.id)
    expect(loaded).toEqual(grown)
    if (!loaded) throw new Error('journalled world should reload locally')
    expect(replayFoundationWorldCausalHistory(loaded)).toEqual(causalReplayProjectionForWorldState(loaded.state))
    expect(grown.state.era).toMatchObject({ era: 'ng-plus', activePlayMinutes: 4_800, durableGrowthUnits: 3_600 })
    expect(grown.state.causalHistory.tail.map(command => command.kind)).toEqual(['initial-courier-selected', 'time-bearing-action', 'durable-jomon-growth'])
  })

  it('persists enriched v3 people unchanged through journal compaction, replay, and local reload', async () => {
    const repository = new MedievalWorldRepository()
    let world = chooseInitialCourier(createFoundationWorld({ seed: 'persistent-person-v2-storage' }), 'crew:0')
    const originalPeople = structuredClone(world.state.people.records)
    for (const suffix of ['a', 'b'] as const) {
      world = recordDurableJomonGrowth(world, {
        id: `growth:persistent-person-v2-storage:small-craft:${suffix}`,
        kind: 'small-craft',
        source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
        atWorldTime: world.state.temporal.worldTime
      })
    }
    for (let index = 0; index < 6; index++) {
      world = advanceFoundationWorldTime(world, {
        id: `wait:persistent-person-v2-storage:${index}`,
        kind: 'wait',
        durationMinutes: 1,
        contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
      })
    }
    await repository.saveWorld(world)
    const loaded = await repository.loadWorld(world.id)
    if (!loaded) throw new Error('valid enriched people should reload locally')

    expect(loaded.state.people).toEqual({ version: 5, records: originalPeople })
    expect(loaded.state.causalHistory).toMatchObject({ tail: [], checkpoint: { sequence: 9 } })
    expect(replayFoundationWorldCausalHistory(loaded)).toEqual(causalReplayProjectionForWorldState(loaded.state))
    expect(loaded.state.era.era).toBe('ng-plus')
    expect(loaded.state.people.records).toHaveLength(loaded.crew.length)
    expect(loaded.state.people.records.some(person => loaded.initialWorld.people.some(seed => seed.id === person.id))).toBe(false)
  })

  it('reloads and continues compacted replay checkpoints after realistic temporal, due-event, and era commands for two configurations', async () => {
    const repository = new MedievalWorldRepository()
    const cases = [
      { seed: 'reload-watershed', preset: 'watershed' as const },
      { seed: 'reload-far-coast', preset: 'far-coast' as const }
    ]

    for (const specimen of cases) {
      let world = chooseInitialCourier(createFoundationWorld({ seed: specimen.seed, configuration: { preset: specimen.preset } }), 'crew:0')
      world = advanceFoundationWorldTime(world, {
        id: `movement:${specimen.preset}`,
        kind: 'movement',
        durationMinutes: 1,
        contentSafety: classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text']),
        events: [{
          id: `event:${specimen.preset}`,
          dueAtWorldTime: 2,
          priority: 'ordinary',
          payload: { kind: 'action-resolution', sourceActionId: `movement:${specimen.preset}`, creationDigest: world.manifest.creation.digest },
          contentSafety: classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])
        }]
      })
      world = advanceFoundationWorldTime(world, {
        id: `wait:${specimen.preset}:resolve`,
        kind: 'wait',
        durationMinutes: 1,
        contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
      })
      world = recordDurableJomonGrowth(world, {
        id: `growth:${specimen.preset}:small-craft:a`,
        kind: 'small-craft',
        source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
        atWorldTime: 2
      })
      world = recordDurableJomonGrowth(world, {
        id: `growth:${specimen.preset}:small-craft:b`,
        kind: 'small-craft',
        source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
        atWorldTime: 2
      })
      for (let index = 0; index < 4; index++) {
        world = advanceFoundationWorldTime(world, {
          id: `wait:${specimen.preset}:compact:${index}`,
          kind: 'wait',
          durationMinutes: 1,
          contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
        })
      }

      expect(world.state.era.era).not.toBe('base')
      expect(world.state.temporal.causalRecords.some(record => record.kind === 'event-resolved' && record.event.id === `event:${specimen.preset}`)).toBe(true)
      expect(world.state.causalHistory).toMatchObject({ tail: [], checkpoint: { sequence: 9 } })
      await repository.saveWorld(world)
      const loaded = await repository.loadWorld(world.id)
      if (!loaded) throw new Error('valid compacted world should reload')

      expect(loaded).toEqual(world)
      expect(replayFoundationWorldCausalHistory(loaded)).toEqual(causalReplayProjectionForWorldState(loaded.state))
      expect(loaded.state.temporal).toEqual(world.state.temporal)
      expect(loaded.state.simulation).toEqual(world.state.simulation)
      expect(loaded.state.era).toEqual(world.state.era)
      expect(loaded.state.courier).toEqual(world.state.courier)
      expect(loaded.state.causalHistory).toEqual(world.state.causalHistory)

      const continued = advanceFoundationWorldTime(loaded, {
        id: `wait:${specimen.preset}:after-reload`, kind: 'wait', durationMinutes: 1,
        contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
      })
      expect(continued.state.causalHistory.tail).toHaveLength(1)
      expect(replayFoundationWorldCausalHistory(continued)).toEqual(causalReplayProjectionForWorldState(continued.state))
    }
  }, 20_000)

  it('contains corrupt worlds, chronicles, indices, and journal data without repair or deletion while independent valid records remain usable', async () => {
    const repository = new MedievalWorldRepository()
    const valid = chooseInitialCourier(createFoundationWorld({ seed: 'storage-containment-valid' }), 'crew:0')
    const settings = { ...defaultCreationSettings(), seed: 'storage-containment-settings' }
    await repository.saveLastUsedCreationSettings(settings)
    await repository.saveWorld(valid)
    const inMemory = structuredClone(valid)
    const forgedCheckpoint = structuredClone(valid)
    forgedCheckpoint.state.causalHistory.checkpoint.token = 'forged-checkpoint'
    const forgedTail = structuredClone(valid)
    forgedTail.state.causalHistory.tail[0]!.token = 'forged-tail'
    const malformedWorld = { version: 10, id: 'world:malformed' }
    const malformedChronicle = { version: 9, id: 'chronicle:malformed', status: 'finalized' }
    const malformedIndex = { version: 1, activeWorlds: [{ id: 1 }], chronicles: [] }

    await repository.loadIndex()
    const worlds = fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds')
    worlds.set('world:forged-checkpoint', forgedCheckpoint)
    worlds.set('world:forged-tail', forgedTail)
    worlds.set('world:malformed', malformedWorld)
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'chronicles').set('chronicle:malformed', malformedChronicle)
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'catalog').set('world-index', malformedIndex)

    await expect(repository.loadWorld('world:forged-checkpoint')).resolves.toBeUndefined()
    await expect(repository.loadWorld('world:forged-tail')).resolves.toBeUndefined()
    await expect(repository.loadWorld('world:malformed')).resolves.toBeUndefined()
    await expect(repository.loadChronicle('chronicle:malformed')).resolves.toBeUndefined()
    await expect(repository.loadIndex()).resolves.toEqual({ version: 1, activeWorlds: [], chronicles: [] })
    expect(worlds.get('world:forged-checkpoint')).toEqual(forgedCheckpoint)
    expect(worlds.get('world:forged-tail')).toEqual(forgedTail)
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'catalog').get('world-index')).toEqual(malformedIndex)
    expect(valid).toEqual(inMemory)
    expect(await repository.loadCreationSettings()).toMatchObject({ lastUsed: settings })
    expect(await repository.loadWorld(valid.id)).toEqual(valid)

    const replacement = advanceFoundationWorldTime(valid, {
      id: 'wait:storage-containment-replacement', kind: 'wait', durationMinutes: 1,
      contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
    })
    await repository.saveWorld(replacement)
    expect(await repository.loadWorld(valid.id)).toEqual(replacement)
    expect(worlds.get('world:forged-checkpoint')).toEqual(forgedCheckpoint)
  })

  it('rejects unsafe and unclassified detailed and summary scheduler records at both save and load boundaries', async () => {
    const repository = new MedievalWorldRepository()
    const source = advanceFoundationWorldTime(
      chooseInitialCourier(createFoundationWorld({ seed: 'storage-scheduler-safety', configuration: { preset: 'sheltered-reach' } }), 'crew:0'),
      { id: 'travel:storage-scheduler-safety', kind: 'travel', durationMinutes: 240, contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary']) }
    )
    const detailed = source.state.simulation.records.find(record => record.outcomeDetail === 'detailed')
    const summary = source.state.simulation.records.find(record => record.outcomeDetail === 'summary')
    if (!detailed || !summary) throw new Error('expected detailed and summary scheduler records')
    const cases: readonly ((world: typeof source) => void)[] = [
      world => { (world.state.simulation.records.find(record => record.id === detailed.id)!.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present' },
      world => { delete (world.state.simulation.records.find(record => record.id === detailed.id)!.cause as { contentSafety?: unknown }).contentSafety },
      world => { delete (world.state.simulation.records.find(record => record.id === summary.id)! as { contentSafety?: unknown }).contentSafety },
      world => { (world.state.simulation.records.find(record => record.id === summary.id)!.cause.contentSafety.exclusions as unknown as Record<string, string>).slavery = 'present' }
    ]

    await repository.loadIndex()
    for (const [index, tamper] of cases.entries()) {
      const forged = structuredClone(source)
      tamper(forged)
      await expect(repository.saveWorld(forged)).rejects.toThrow('invalid medieval world')
      fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(`world:unsafe-scheduler:${index}`, forged)
      await expect(repository.loadWorld(`world:unsafe-scheduler:${index}`)).resolves.toBeUndefined()
    }
  })

  it('rejects the v8 mutable-world envelope rather than inventing a replay checkpoint or command journal', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'causal-history-envelope-clean-break' })
    const legacy = structuredClone(world) as unknown as { version: number; state: { version: number; causalHistory?: unknown } }
    legacy.version = 8
    legacy.state.version = 6
    delete legacy.state.causalHistory

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, legacy)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects a stored or submitted world with forged simulation cause evidence', async () => {
    const repository = new MedievalWorldRepository()
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'forged-catch-up-cause' }), 'crew:0')
    const advanced = advanceFoundationWorldTime(world, {
      id: 'movement:forged-catch-up-cause',
      kind: 'movement',
      durationMinutes: 1,
      contentSafety: classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])
    })
    const forged = structuredClone(advanced)
    forged.state.simulation.records[0]!.cause.evidence.targetId = 'person:not-known'

    await expect(repository.saveWorld(forged)).rejects.toThrow('invalid medieval world')
    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(forged.id, forged)
    await expect(repository.loadWorld(forged.id)).resolves.toBeUndefined()
  })

  it('rejects a stored world whose manifest provenance does not reproduce its resolved configuration', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'provenance-check', configuration: { preset: 'far-coast' } })
    const forged = structuredClone(world)
    forged.manifest.creation.resolvedConfiguration.terrainRuggedness = 1

    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, forged)

    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects foundation worlds whose generated content no longer matches the accepted safety audit', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'safety-check' })
    const forged = structuredClone(world)
    ;(forged.crew[0]!.historyContentSafety.exclusions as unknown as Record<string, string>).torture = 'present'

    await expect(repository.saveWorld(forged)).rejects.toThrow('invalid medieval world')
    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(world.id, forged)
    await expect(repository.loadWorld(world.id)).resolves.toBeUndefined()
  })

  it('rejects a structurally valid but non-reproducible household at save and load without changing the supplied record', async () => {
    const repository = new MedievalWorldRepository()
    const source = createFoundationWorld({ seed: 'storage-forged-household' })
    const forged = structuredClone(source)
    forged.crew[0]!.relationships[0]!.basis = forged.crew[0]!.relationships[0]!.basis === 'debt' ? 'work' : 'debt'
    const before = structuredClone(forged)

    await expect(repository.saveWorld(forged)).rejects.toThrow('invalid medieval world')
    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(forged.id, forged)
    await expect(repository.loadWorld(forged.id)).resolves.toBeUndefined()
    expect(forged).toEqual(before)
  })

  it('rejects a generated initial-world policy bypass and a safe-looking altered region at the local-save boundary', async () => {
    const repository = new MedievalWorldRepository()
    const world = createFoundationWorld({ seed: 'initial-world-storage-check' })
    const unsafe = structuredClone(world)
    ;(unsafe.initialWorld.history[0]!.contentSafety.exclusions as unknown as Record<string, string>).slavery = 'present'

    await expect(repository.saveWorld(unsafe)).rejects.toThrow('invalid medieval world')

    const altered = structuredClone(world)
    altered.initialWorld.settlements[0]!.name = 'Altered Landing'
    await expect(repository.saveWorld(altered)).rejects.toThrow('invalid medieval world')
  })

  it('atomically replaces an active world with its read-only finalized chronicle', async () => {
    const repository = new MedievalWorldRepository()
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'last-mooring' }), 'crew:0')
    const chronicle = finalizeWorldAsChronicle(world, 'jomon-loss')

    await repository.saveWorld(world)
    await repository.finalize(world, chronicle)

    expect(await repository.loadWorld(world.id)).toBeUndefined()
    expect(await repository.loadChronicle(chronicle.id)).toEqual(chronicle)
    await expect(repository.loadIndex()).resolves.toEqual({ version: 1, activeWorlds: [], chronicles: [{ id: chronicle.id, label: world.manifest.creation.label, reason: 'jomon-loss' }] })
  })

  it('requires explicit validated snapshot recovery and contains stale finalization or corrupt recovery records without overwriting the active envelope', async () => {
    const repository = new MedievalWorldRepository()
    const original = chooseInitialCourier(createFoundationWorld({ seed: 'loss-recovery-containment' }), 'crew:0')
    const current = advanceFoundationWorldTime(original, {
      id: 'wait:loss-recovery-containment', kind: 'wait', durationMinutes: 1,
      contentSafety: classifyMedievalContent('event', ['adult-labour'], 'adults-only', ['data'])
    })
    await repository.saveWorld(original)
    await repository.saveWorld(current)

    const staleChronicle = finalizeWorldAsChronicle(original, 'jomon-loss')
    await expect(repository.finalize(original, staleChronicle)).rejects.toThrow('corrupt-existing-world')
    expect(await repository.loadWorld(current.id)).toEqual(current)
    expect(await repository.loadChronicle(staleChronicle.id)).toBeUndefined()
    expect(await repository.loadIndex()).toEqual({
      version: 1,
      activeWorlds: [{ id: current.id, label: current.manifest.creation.label, initialCourierId: 'crew:0' }],
      chronicles: []
    })

    const snapshots = await repository.inspectWorldSnapshots(current.id)
    expect(snapshots.status).toBe('available')
    if (snapshots.status !== 'available') throw new Error('fixture requires a valid snapshot ring')
    const corruptRing = structuredClone(snapshots.ring)
    corruptRing.snapshots[0]!.source.digest = 'layout:forged-recovery-source'
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'world-snapshots').set(current.id, corruptRing)

    await expect(repository.inspectWorldSnapshots(current.id)).resolves.toEqual({ status: 'corrupt' })
    await expect(repository.restoreSnapshot(current.id, 1)).rejects.toThrow('snapshot-not-found')
    expect(await repository.loadWorld(current.id)).toEqual(current)
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'world-snapshots').get(current.id)).toEqual(corruptRing)
  }, 30_000)

  it('writes a derived source-bound index and bounded deterministic snapshot ring with the active envelope', async () => {
    const repository = new MedievalWorldRepository()
    let world = chooseInitialCourier(createFoundationWorld({ seed: 'layout-atomic' }), 'crew:0')
    await repository.saveWorld(world)
    expect(await repository.loadWorldRecordIndex(world.id)).toBeDefined()
    expect(await repository.inspectWorldSnapshots(world.id)).toEqual({ status: 'absent' })

    for (let index = 0; index < 4; index++) {
      world = advanceFoundationWorldTime(world, { id: `layout-save-${index}`, kind: 'wait', durationMinutes: 1, contentSafety: classifyMedievalContent('event', ['adult-labour'], 'adults-only', ['data']) })
      await repository.saveWorld(world)
    }
    const snapshots = await repository.inspectWorldSnapshots(world.id)
    expect(snapshots.status).toBe('available')
    if (snapshots.status !== 'available') throw new Error('snapshot ring should be inspectable')
    expect(snapshots.ring.snapshots).toHaveLength(3)
    expect(snapshots.ring.snapshots.map(snapshot => snapshot.sequence)).toEqual([2, 3, 4])
    expect(await repository.inspectSnapshot(world.id, 4)).toEqual(snapshots.ring.snapshots[2]!.world)
    await repository.restoreSnapshot(world.id, 2)
    expect(await repository.loadWorld(world.id)).toEqual(snapshots.ring.snapshots[0]!.world)
  }, 60_000)

  it('derives task, event, person, and history locators from a validated delegated world without indexing generation seeds', async () => {
    const repository = new MedievalWorldRepository()
    const world = delegatedWorld('layout-derived-records')
    await repository.saveWorld(world)
    const index = await repository.loadWorldRecordIndex(world.id)
    expect(index?.locators.some(locator => locator.kind === 'task')).toBe(true)
    expect(index?.locators.some(locator => locator.kind === 'event')).toBe(true)
    expect(index?.locators.some(locator => locator.kind === 'history')).toBe(true)
    expect(index?.locators.some(locator => locator.container === 'social-memory')).toBe(true)
    expect(index?.locators.filter(locator => locator.kind === 'person').map(locator => locator.targetId)).toEqual(world.state.people.records.map(person => person.id))
    expect(index?.locators.some(locator => locator.targetId.startsWith('initial:person:'))).toBe(false)
  }, 20_000)

  it('contains quota/abort failures and corrupt active records without overwriting a valid prior envelope or metadata', async () => {
    const repository = new MedievalWorldRepository()
    const before = chooseInitialCourier(createFoundationWorld({ seed: 'layout-failure' }), 'crew:0')
    const next = advanceFoundationWorldTime(before, { id: 'layout-failure-wait', kind: 'wait', durationMinutes: 1, contentSafety: classifyMedievalContent('event', ['adult-labour'], 'adults-only', ['data']) })
    await repository.saveWorld(before)
    const index = await repository.loadWorldRecordIndex(before.id)
    fakeIndexedDB.failNextWrite(MEDIEVAL_DATABASE_NAME, 'quota')
    await expect(repository.saveWorld(next)).rejects.toThrow('storage-quota-exceeded')
    expect(await repository.loadWorld(before.id)).toEqual(before)
    expect(await repository.loadWorldRecordIndex(before.id)).toEqual(index)

    fakeIndexedDB.failNextWrite(MEDIEVAL_DATABASE_NAME, 'abort')
    await expect(repository.saveWorld(next)).rejects.toThrow('storage-transaction-aborted')
    expect(await repository.loadWorld(before.id)).toEqual(before)

    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set(before.id, { version: 999 })
    await expect(repository.saveWorld(next)).rejects.toThrow('corrupt-existing-world')
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').get(before.id)).toEqual({ version: 999 })
  }, 20_000)

  it('upgrades valid v3 envelopes without forced rewrites and adds metadata only on an explicit save', async () => {
    const legacy = fakeIndexedDB.seedV3(MEDIEVAL_DATABASE_NAME)
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'layout-v3' }), 'crew:0')
    legacy.stores.get('worlds')!.set(world.id, structuredClone(world))
    legacy.stores.get('catalog')!.set('world-index', { version: 1, activeWorlds: [{ id: world.id, label: world.manifest.creation.label, initialCourierId: 'crew:0' }], chronicles: [] })
    const repository = new MedievalWorldRepository()
    expect(await repository.loadWorld(world.id)).toEqual(world)
    expect(fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'world-record-indexes').has(world.id)).toBe(false)
    expect(await repository.inspectWorldSnapshots(world.id)).toEqual({ status: 'absent' })
    await repository.saveWorld(world)
    expect(await repository.loadWorldRecordIndex(world.id)).toBeDefined()
  })

  it('uses explicit canonical backup collision/replacement rules and preserves the replaced active record as a snapshot', async () => {
    const repository = new MedievalWorldRepository()
    const base = chooseInitialCourier(createFoundationWorld({ seed: 'layout-import' }), 'crew:0')
    const replacement = advanceFoundationWorldTime(base, { id: 'layout-import-wait', kind: 'wait', durationMinutes: 1, contentSafety: classifyMedievalContent('event', ['adult-labour'], 'adults-only', ['data']) })
    await repository.saveWorld(base)
    const bundle = serializePersistenceBackupBundle(createActiveWorldBackupBundle(replacement))
    await expect(repository.importActiveWorldBackup(bundle)).rejects.toThrow('backup-collision')
    await repository.importActiveWorldBackup(bundle, { collision: 'replace' })
    expect(await repository.loadWorld(base.id)).toEqual(replacement)
    const snapshots = await repository.inspectWorldSnapshots(base.id)
    expect(snapshots.status).toBe('available')
    await expect(repository.importActiveWorldBackup('{bad JSON')).rejects.toMatchObject({ code: 'backup-malformed' })
    expect(await repository.loadWorld(base.id)).toEqual(replacement)
  }, 10_000)

  it('imports a canonical chronicle only as a read-only chronicle and rejects its collision without touching active worlds', async () => {
    const repository = new MedievalWorldRepository()
    const active = chooseInitialCourier(createFoundationWorld({ seed: 'layout-chronicle-active' }), 'crew:0')
    const historic = chooseInitialCourier(createFoundationWorld({ seed: 'layout-chronicle' }), 'crew:0')
    const chronicle = finalizeWorldAsChronicle(historic, 'crew-extinction')
    const bundle = serializePersistenceBackupBundle(createChronicleBackupBundle(chronicle))
    await repository.saveWorld(active)
    await repository.importChronicleBackup(bundle)
    expect(await repository.loadChronicle(chronicle.id)).toEqual(chronicle)
    expect(await repository.loadWorld(active.id)).toEqual(active)
    await expect(repository.importChronicleBackup(bundle)).rejects.toThrow('backup-collision')
    expect(await repository.loadWorld(historic.id)).toBeUndefined()
  })
})
