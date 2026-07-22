import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { initialCampaignRoute, newHero, newRun } from './engine'
import { deleteCourier, flushCourierWrites, loadCouriers, migrateCampaignRoute, migrateRunRecord, saveCourier, selectCourier } from './storage'
import type { CourierSave, Records } from './types'

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
  private done = false
  private readonly records: Map<string, unknown>

  constructor(readonly database: FakeDatabase) { this.records = new Map(database.records) }
  objectStore(): FakeStore { return new FakeStore(this) }
  request<T>(action: () => T, fail = false): FakeRequest<T> {
    const request = new FakeRequest<T>()
    this.pending++
    queueMicrotask(() => {
      if (this.done) return
      if (fail) {
        request.error = new Error('write failed')
        request.onerror?.()
      } else {
        request.result = action()
        request.onsuccess?.()
      }
      this.pending--
      this.finish()
    })
    return request
  }
  get(key: string): unknown { return this.records.get(key) }
  put(key: string, value: unknown): void { this.records.set(key, structuredClone(value)) }
  delete(key: string): void { this.records.delete(key) }
  abort(): void {
    if (this.done) return
    this.done = true
    this.error = new Error('transaction aborted')
    queueMicrotask(() => this.onabort?.())
  }
  private finish(): void {
    if (this.done || this.pending) return
    queueMicrotask(() => {
      if (this.done || this.pending) return
      this.done = true
      this.database.records = new Map(this.records)
      this.oncomplete?.()
    })
  }
}

class FakeStore {
  constructor(private readonly transaction: FakeTransaction) { }
  get(key: string): IDBRequest<unknown> { return this.transaction.request(() => this.transaction.get(key)) as unknown as IDBRequest<unknown> }
  put(value: unknown, key: string): IDBRequest<unknown> {
    const fail = this.transaction.database.failNextPut
    this.transaction.database.failNextPut = false
    return this.transaction.request(() => { this.transaction.put(key, value); return key }, fail) as unknown as IDBRequest<unknown>
  }
  delete(key: string): IDBRequest<undefined> { return this.transaction.request(() => { this.transaction.delete(key); return undefined }) as unknown as IDBRequest<undefined> }
}

class FakeDatabase {
  records = new Map<string, unknown>()
  failNextPut = false
  transaction(): IDBTransaction { return new FakeTransaction(this) as unknown as IDBTransaction }
  createObjectStore(): FakeStore { return new FakeTransaction(this).objectStore() }
}

class FakeIndexedDB {
  private created = false
  readonly database = new FakeDatabase()
  open(): IDBOpenDBRequest {
    const request = new FakeRequest<FakeDatabase>() as FakeRequest<FakeDatabase> & { onupgradeneeded: Handler }
    request.onupgradeneeded = null
    request.result = this.database
    queueMicrotask(() => {
      if (!this.created) { this.created = true; request.onupgradeneeded?.() }
      request.onsuccess?.()
    })
    return request as unknown as IDBOpenDBRequest
  }
}

const records = (): Records => ({ bestDepth: 0, wins: 0, deaths: 0, runs: [], analyses: [] })
const courier = (id: string, turn: number): CourierSave => {
  const run = newRun(901)
  run.turn = turn
  return { version: 1, identity: { id, name: id, origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint', createdAt: '2026-01-01T00:00:00.000Z' }, run, checkpoint: structuredClone(run), heir: structuredClone(run.hero), campaign: initialCampaignRoute(), records: records() }
}

const originalIndexedDB = globalThis.indexedDB
let fakeIndexedDB: FakeIndexedDB
beforeEach(async () => {
  await flushCourierWrites()
  fakeIndexedDB = new FakeIndexedDB()
  Object.defineProperty(globalThis, 'indexedDB', { configurable: true, value: fakeIndexedDB })
})
afterEach(async () => {
  await flushCourierWrites()
  Object.defineProperty(globalThis, 'indexedDB', { configurable: true, value: originalIndexedDB })
})

describe('run persistence migration', () => {
  it('loads valid v3 records without changing them', () => {
    const run = newRun(123)
    expect(migrateRunRecord(run)).toEqual(run)
  })

  it('preserves persisted prop state in valid v3 runs', () => {
    const run = newRun(124)
    run.floor.props[0].state = 'inspected'
    expect(migrateRunRecord(run)?.floor.props).toEqual(run.floor.props)
  })

  it('preserves temporary prop terrain state in valid v3 runs', () => {
    const run = newRun(125)
    run.floor.props[0].effectCells = [{ x: 3, y: 4 }]
    run.floor.props[0].expiresAt = 9
    expect(migrateRunRecord(run)?.floor.props[0]).toMatchObject({ effectCells: [{ x: 3, y: 4 }], expiresAt: 9 })
  })

  it('upgrades v1 and v2 runs after the prop schema change', () => {
    for (const version of [1, 2] as const) {
      const legacy = structuredClone(newRun(456)) as unknown as { version: number; hero: Record<string, unknown>; floor: Record<string, unknown> }
      legacy.version = version
      delete legacy.hero.name
      delete legacy.hero.origin
      delete legacy.hero.calling
      delete legacy.hero.deathMode
      delete legacy.floor.props
      delete legacy.floor.objective
      const migrated = migrateRunRecord(legacy)
      expect(migrated).toMatchObject({ version: 3, area: 'mine', areaFloor: 0, hero: { name: 'Existing Courier', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint' }, floor: { props: [], objective: { status: 'active' } } })
    }
  })

  it('preserves valid props in a compatible v2 save', () => {
    const legacy = structuredClone(newRun(457)) as unknown as { version: number; floor: { props: unknown } }
    legacy.version = 2
    expect(migrateRunRecord(legacy)?.floor.props).toEqual(legacy.floor.props)
  })

  it('preserves a v2 objective state', () => {
    const legacy = structuredClone(newRun(458)) as unknown as { version: number }
    legacy.version = 2
    expect(migrateRunRecord(legacy)?.floor.objective).toEqual(newRun(458).floor.objective)
  })

  it('rejects incomplete current-schema heroes', () => {
    const legacy = newRun(457)
    delete (legacy.hero as Partial<typeof legacy.hero>).name
    delete (legacy.hero as Partial<typeof legacy.hero>).origin
    delete (legacy.hero as Partial<typeof legacy.hero>).calling
    delete (legacy.hero as Partial<typeof legacy.hero>).deathMode
    expect(migrateRunRecord(legacy)).toBeUndefined()
  })

  it('rejects malformed current saves before content consumers access them', () => {
    const unknownItem = newRun(459)
    unknownItem.hero.inventory.push('missing-item')
    expect(migrateRunRecord(unknownItem)).toBeUndefined()
    const unknownSkill = newRun(460)
    unknownSkill.hero.skills.push('missing-skill')
    expect(migrateRunRecord(unknownSkill)).toBeUndefined()
    const outOfBounds = newRun(461)
    outOfBounds.hero.x = 48
    expect(migrateRunRecord(outOfBounds)).toBeUndefined()
    const wrongDimensions = newRun(462)
    wrongDimensions.floor.tiles.pop()
    expect(migrateRunRecord(wrongDimensions)).toBeUndefined()
  })

  it('builds mechanical origins and starter callings', () => {
    const hero = newHero({ name: 'Ari', origin: 'cavernSeeker', calling: 'spiritbearer', deathMode: 'ironTrail' })
    expect(hero).toMatchObject({ name: 'Ari', stats: { intellect: 3 }, deathMode: 'ironTrail', inventory: expect.arrayContaining(['focusTonic', 'sight']) })
  })

  it('persists the Tidebound origin', () => {
    const run = newRun(458)
    run.hero = newHero({ name: 'Neri', origin: 'tidebound', calling: 'trailguard', deathMode: 'checkpoint' })
    expect(migrateRunRecord(run)).toMatchObject({ hero: { origin: 'tidebound', stats: { strength: 2, agility: 3, vitality: 1, intellect: 2 }, equipment: { mainHand: 'tideSpear' }, inventory: expect.arrayContaining(['tideSpear']) } })
  })

  it('rejects runs missing required floor state', () => {
    const run = newRun(789)
    delete (run.floor as Partial<typeof run.floor>).objective
    expect(migrateRunRecord(run)).toBeUndefined()
  })

  it('adds telemetry when loading a pre-telemetry run', () => {
    const run = newRun(790)
    delete run.telemetry
    expect(migrateRunRecord(run)).toMatchObject({ telemetry: { turns: 0, samples: [{ turn: 0 }], floors: [{ floor: 1 }] } })
  })

  it('rejects malformed records so the caller stays at title', () => {
    expect(migrateRunRecord({ version: 3, seed: 1 })).toBeUndefined()
  })

  it('keeps only route progression when loading campaign state', () => {
    const route = migrateCampaignRoute({ version: 1, completedAreas: ['mine'], unlockedAreas: ['mine', 'wilds'], selectedBiome: 'wilds', hero: { gold: 999 } })
    expect(route).toEqual({ version: 2, completedAreas: ['mine'], unlockedAreas: ['mine', 'wilds'], selectedBiome: 'wilds', rescuedNpcs: [], lineageEvents: [], legacyRecords: [] })
    expect(migrateCampaignRoute({ version: 1, completedAreas: ['mine'], unlockedAreas: [], selectedBiome: 'wilds' })).toEqual({ version: 2, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], lineageEvents: [], legacyRecords: [] })
  })

  it('migrates v1 death records to the journal schema', () => {
    const legacy = { id: 'legacy-1', heirName: 'Ari', cause: 'defeated' as const, biome: 'mine' as const, floor: 2, seed: 9, lineage: ['Ari'], location: { x: 4, y: 6 }, cache: { gold: 30, items: ['tonic'] }, encounter: { kind: 'cache' as const, resolved: false } }
    expect(migrateCampaignRoute({ version: 1, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', legacyRecords: [legacy], legacyEncounterAreas: ['mine'] })).toEqual({ version: 2, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], lineageEvents: [], legacyRecords: [{ id: 'legacy-1', heirName: 'Ari', biome: 'mine', floor: 2, seed: 9 }] })
  })
})

describe('courier persistence', () => {
  it('snapshots and serializes overlapping saves so the newest request wins', async () => {
    const first = courier('ari', 3)
    const firstSave = saveCourier(first, 'ari')
    first.run!.turn = 999
    const secondSave = saveCourier(courier('ari', 4), 'ari')
    await Promise.all([firstSave, secondSave])
    const loaded = await loadCouriers()
    expect(loaded.selectedId).toBe('ari')
    expect(loaded.couriers).toHaveLength(1)
    expect(loaded.couriers[0].run?.turn).toBe(4)
  })

  it('rolls back a failed atomic save and keeps later writes usable', async () => {
    fakeIndexedDB.database.failNextPut = true
    await expect(saveCourier(courier('ari', 3), 'ari')).rejects.toThrow('write failed')
    await flushCourierWrites()
    expect((await loadCouriers()).couriers).toHaveLength(0)
    await saveCourier(courier('ari', 4), 'ari')
    expect((await loadCouriers()).couriers[0].run?.turn).toBe(4)
  })

  it('serializes save, delete, and selection mutations through one index', async () => {
    await Promise.all([saveCourier(courier('ari', 3), 'ari'), saveCourier(courier('bo', 4), 'bo'), deleteCourier('ari'), selectCourier('bo')])
    const loaded = await loadCouriers()
    expect(loaded.selectedId).toBe('bo')
    expect(loaded.couriers.map(current => current.identity.id)).toEqual(['bo'])
  })
})
