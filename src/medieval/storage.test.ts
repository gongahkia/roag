import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { MedievalWorldRepository } from './storage'
import { CREATION_SETTINGS_PROFILE_LIMIT, defaultCreationSettings } from './settings'
import { MEDIEVAL_DATABASE_NAME } from './types'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, finalizeWorldAsChronicle } from './world'
import { classifyMedievalContent } from './content-safety'

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

  constructor(private readonly database: FakeDatabase) {
    this.stores = new Map([...database.stores].map(([name, values]) => [name, new Map([...values].map(([key, value]) => [key, structuredClone(value)]))]))
  }

  objectStore(name: string): FakeObjectStore {
    if (!this.stores.has(name)) throw new Error(`unknown store ${name}`)
    return new FakeObjectStore(this, name)
  }

  request<T>(operation: () => T): FakeRequest<T> {
    const request = new FakeRequest<T>()
    this.pending++
    queueMicrotask(() => {
      if (this.finished) return
      request.result = operation()
      request.onsuccess?.()
      this.pending--
      this.completeWhenIdle()
    })
    return request
  }

  get(store: string, key: unknown): unknown { return this.stores.get(store)?.get(key) }
  put(store: string, key: unknown, value: unknown): void { this.stores.get(store)?.set(key, structuredClone(value)) }
  delete(store: string, key: unknown): void { this.stores.get(store)?.delete(key) }

  private completeWhenIdle(): void {
    if (this.finished || this.pending > 0) return
    queueMicrotask(() => {
      if (this.finished || this.pending > 0) return
      this.finished = true
      this.database.stores = this.stores
      this.oncomplete?.()
    })
  }
}

class FakeObjectStore {
  constructor(private readonly transaction: FakeTransaction, private readonly name: string) { }
  get(key: unknown): IDBRequest<unknown> { return this.transaction.request(() => this.transaction.get(this.name, key)) as unknown as IDBRequest<unknown> }
  put(value: unknown, key: unknown): IDBRequest<unknown> { return this.transaction.request(() => { this.transaction.put(this.name, key, value); return key }) as unknown as IDBRequest<unknown> }
  delete(key: unknown): IDBRequest<undefined> { return this.transaction.request(() => { this.transaction.delete(this.name, key); return undefined }) as unknown as IDBRequest<undefined> }
}

class FakeDatabase {
  stores = new Map<string, Map<unknown, unknown>>()
  readonly objectStoreNames = { contains: (name: string): boolean => this.stores.has(name) }

  transaction(): IDBTransaction { return new FakeTransaction(this) as unknown as IDBTransaction }
  createObjectStore(name: string): IDBObjectStore {
    this.stores.set(name, new Map())
    return {} as IDBObjectStore
  }
}

class FakeIndexedDB {
  readonly openedNames: string[] = []
  private readonly databases = new Map<string, FakeDatabase>()

  open(name: string): IDBOpenDBRequest {
    this.openedNames.push(name)
    const request = new FakeRequest<FakeDatabase>() as FakeRequest<FakeDatabase> & { onupgradeneeded: Handler; onblocked: Handler }
    request.onupgradeneeded = null
    request.onblocked = null
    const existing = this.databases.get(name)
    const database = existing ?? new FakeDatabase()
    this.databases.set(name, database)
    request.result = database
    queueMicrotask(() => {
      if (!existing) request.onupgradeneeded?.()
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
}

const originalIndexedDB = globalThis.indexedDB
let fakeIndexedDB: FakeIndexedDB

beforeEach(() => {
  fakeIndexedDB = new FakeIndexedDB()
  Object.defineProperty(globalThis, 'indexedDB', { configurable: true, value: fakeIndexedDB })
})

afterEach(() => {
  Object.defineProperty(globalThis, 'indexedDB', { configurable: true, value: originalIndexedDB })
})

describe('medieval local persistence', () => {
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
  })

  it('keeps valid local creation settings intact while saving and loading the v4 full-world record', async () => {
    const repository = new MedievalWorldRepository()
    const settings = { ...defaultCreationSettings(), seed: 'settings-survive-state', configuration: { preset: 'far-coast' as const, advanced: {} } }
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'settings-survive-state', configuration: settings.configuration }), 'crew:0')

    await repository.saveLastUsedCreationSettings(settings)
    await repository.saveWorld(world)

    expect((await repository.loadCreationSettings()).lastUsed).toEqual(settings)
    expect(await repository.loadWorld(world.id)).toEqual(world)
  })

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
})
