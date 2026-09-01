import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { MedievalWorldRepository } from './storage'
import { MEDIEVAL_DATABASE_NAME } from './types'
import { chooseInitialCourier, createFoundationWorld, finalizeWorldAsChronicle } from './world'

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
    expect(await repository.loadWorld(second.id)).toEqual(second)
    expect(await repository.loadWorld('world:not-present')).toBeUndefined()
  })

  it('rejects malformed local records instead of treating them as a medieval world', async () => {
    const repository = new MedievalWorldRepository()
    await repository.loadIndex()
    fakeIndexedDB.store(MEDIEVAL_DATABASE_NAME, 'worlds').set('world:bad', { version: 6, hero: { name: 'prototype' } })

    await expect(repository.loadWorld('world:bad')).resolves.toBeUndefined()
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
