import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { advanceCampaignTier, changeCampaignCompanionControlMode, completeCampaignTier, createGalaxy, hubCampaignStatus, initialCampaignCycle, initialCampaignRoute, newHero, newRun } from './engine'
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
  return { version: 1, identity: { id, name: id, origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint', companionControlMode: 'autonomous', companionDeathMode: 'injury', createdAt: '2026-01-01T00:00:00.000Z' }, run, checkpoint: structuredClone(run), heir: structuredClone(run.hero), campaign: initialCampaignRoute(), records: records() }
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

  it('adds discovery and reward rules to TR-10 secret metadata', () => {
    const run = structuredClone(newRun(123))
    for (const room of run.floor.secretRooms ?? []) {
      delete (room as Partial<typeof room>).clueChannel
      delete (room as Partial<typeof room>).rewardProfile
      delete (room as Partial<typeof room>).riskProfile
    }
    expect(migrateRunRecord(run)?.floor.secretRooms).toEqual(newRun(123).floor.secretRooms)
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

  it('preserves valid ecology event contracts', () => {
    const run = newRun(126)
    run.floor.ecology = [{ id: 'ecology:test', kind: 'collapse', source: 'brace', target: { x: 3, y: 4 }, warning: 'move clear', startsAt: 2, duration: 2, responses: ['move'], cleanup: 'clear', state: 'waiting', original: 'floor', effect: 'crumble' }]
    expect(migrateRunRecord(run)?.floor.ecology).toMatchObject([{ id: 'ecology:test', kind: 'collapse', target: { x: 3, y: 4 }, effect: 'crumble' }])
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
    expect(migrated).toMatchObject({ version: 5, area: 'mine', areaFloor: 0, hero: { name: 'Existing Courier', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint' }, floor: { props: [], milestones: expect.any(Array), objective: { status: 'active' } } })
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
    expect(migrateRunRecord(run)).toMatchObject({ telemetry: { turns: 0, optionalContent: { generated: expect.any(Object), discovered: {}, used: {}, failed: {} }, interactions: { terrainToolUses: {}, rejectedInteractions: {}, routeFailures: {} }, samples: [{ turn: 0 }], floors: [{ floor: 1 }] } })
  })

  it('preserves social reputation in run and campaign migrations', () => {
    const run = newRun(791)
    run.reputation = { trailfolk: 3, kami: -2 }
    expect(migrateRunRecord(run)?.reputation).toEqual({ trailfolk: 3, kami: -2 })
    expect(migrateCampaignRoute({ version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', reputation: { trailfolk: 3, kami: -2 } })).toMatchObject({ reputation: { trailfolk: 3, kami: -2 } })
  })

  it('defaults absent companion death settings to recoverable injury', () => {
    const run = newRun(792)
    delete (run as { companionDeathMode?: unknown }).companionDeathMode
    expect(migrateRunRecord(run)?.companionDeathMode).toBe('injury')
  })

  it('rejects malformed records so the caller stays at title', () => {
    expect(migrateRunRecord({ version: 3, seed: 1 })).toBeUndefined()
  })

  it('keeps only route progression when loading campaign state', () => {
    const route = migrateCampaignRoute({ version: 1, completedAreas: ['mine'], unlockedAreas: ['mine', 'wilds'], selectedBiome: 'wilds', hero: { gold: 999 } })
    expect(route).toEqual({ version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'], completedAreas: ['mine'], unlockedAreas: ['mine', 'wilds'], selectedBiome: 'wilds', rescuedNpcs: [], companions: [], companionControlMode: 'autonomous', companionControlHistory: [{ sequence: 0, mode: 'autonomous', source: 'migration' }], carryoverDiagnostics: [], lineageEvents: [], legacyRecords: [], alignment: { kami: 0, villagePact: 0 }, reputation: { trailfolk: 0, kami: 0 }, cycle: initialCampaignCycle() })
    expect(migrateCampaignRoute({ version: 1, completedAreas: ['mine'], unlockedAreas: [], selectedBiome: 'wilds' })).toEqual({ version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], companions: [], companionControlMode: 'autonomous', companionControlHistory: [{ sequence: 0, mode: 'autonomous', source: 'creation' }], carryoverDiagnostics: [], lineageEvents: [], legacyRecords: [], alignment: { kami: 0, villagePact: 0 }, reputation: { trailfolk: 0, kami: 0 }, cycle: initialCampaignCycle() })
    const order = ['furnace', 'mine', 'wilds', 'caverns', 'ruins', 'floodedRuins'] as const
    expect(migrateCampaignRoute({ version: 3, areaOrder: order, completedAreas: [], unlockedAreas: ['furnace'], selectedBiome: 'furnace' })).toMatchObject({ version: 5, areaOrder: order, selectedBiome: 'furnace' })
  })

  it('migrates legacy galaxy clock data without applying closed-session elapsed time', () => {
    const legacyGalaxy = structuredClone(createGalaxy(501, newHero({ name: 'Ari' }), 99)) as unknown as Record<string, unknown>
    legacyGalaxy.version = 1
    legacyGalaxy.sectorDay = 3.25
    delete legacyGalaxy.routeReckoning
    delete legacyGalaxy.lastWorldTick
    for (const contract of legacyGalaxy.sealedPackageContracts as Array<Record<string, unknown>>) delete (contract.terms as Record<string, unknown>).deadlineReckoning
    const source = { version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', galaxy: legacyGalaxy }
    const first = migrateCampaignRoute(source)
    const reopened = migrateCampaignRoute(JSON.parse(JSON.stringify(first)))

    expect(first.galaxy).toMatchObject({ version: 3, routeReckoning: 4_680, lastWorldTick: 13, sealedPackageContracts: [{ status: 'offered', terms: { deadlineReckoning: 5_760 } }], routeBoard: { version: 1, currentDestinationId: 'destination:kestrel', networkId: 'helios-intake-v1' } })
    expect(reopened.galaxy).toEqual(first.galaxy)
  })

  it('migrates v1 death records to the journal schema', () => {
    const legacy = { id: 'legacy-1', heirName: 'Ari', cause: 'defeated' as const, biome: 'mine' as const, floor: 2, seed: 9, lineage: ['Ari'], location: { x: 4, y: 6 }, cache: { gold: 30, items: ['tonic'] }, encounter: { kind: 'cache' as const, resolved: false } }
    expect(migrateCampaignRoute({ version: 1, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', legacyRecords: [legacy], legacyEncounterAreas: ['mine'] })).toEqual({ version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], companions: [], companionControlMode: 'autonomous', companionControlHistory: [{ sequence: 0, mode: 'autonomous', source: 'migration' }], carryoverDiagnostics: [], lineageEvents: [], legacyRecords: [{ id: 'legacy-1', heirName: 'Ari', biome: 'mine', floor: 2, seed: 9 }], alignment: { kami: 0, villagePact: 0 }, reputation: { trailfolk: 0, kami: 0 }, cycle: initialCampaignCycle() })
  })

  it('migrates historical rescues into inert companion leads and rejects missing source references', () => {
    const rescue = { id: 'rescue:mine:1:mika', name: 'Mika', biome: 'mine' as const, floor: 1 }
    const legacy = { version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [rescue] }
    const migrated = migrateCampaignRoute(legacy)
    expect(migrated).toMatchObject({ rescuedNpcs: [rescue], companions: [{ id: 'companion:rescue:mine:1:mika', name: 'Mika', rosterStatus: 'lead', permanentlyLost: false, recruitment: { rescueId: rescue.id } }] })
    expect(migrateCampaignRoute(JSON.parse(JSON.stringify(migrated)))).toEqual(migrated)
    expect(() => migrateCampaignRoute({ ...migrated, rescuedNpcs: [] })).toThrow(`recruitment rescue ${rescue.id} is missing`)
  })

  it('migrates existing couriers to documented autonomous companion control', () => {
    const rescue = { id: 'rescue:mine:1:mika', name: 'Mika', biome: 'mine' as const, floor: 1 }
    const legacy = { version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [rescue], companions: [{ version: 1, id: 'companion:rescue:mine:1:mika', templateId: 'rescue:mine', name: 'Mika', role: 'guard', recruitment: { kind: 'rescue' as const, rescueId: rescue.id, biome: 'mine' as const, floor: 1 }, rosterStatus: 'active' as const, controlMode: 'direct' as const, injury: 'healthy' as const, abilityState: { cooldowns: {}, retired: [] }, toolState: { cooldown: 0, retired: false }, permanentlyLost: false }] }
    expect(migrateCampaignRoute(legacy)).toMatchObject({ companionControlMode: 'autonomous', companionControlHistory: [{ sequence: 0, mode: 'autonomous', source: 'migration' }], companions: [{ controlMode: 'autonomous' }] })
  })
})

describe('courier persistence', () => {
  it('migrates legacy courier saves to recoverable companion loss', async () => {
    await saveCourier(courier('legacy', 3), 'legacy')
    const stored = fakeIndexedDB.database.records.get('courier:legacy') as { identity: { companionDeathMode?: unknown }; run?: { companionDeathMode?: unknown }; checkpoint?: { companionDeathMode?: unknown } }
    delete stored.identity.companionDeathMode
    delete stored.run?.companionDeathMode
    delete stored.checkpoint?.companionDeathMode
    fakeIndexedDB.database.records.set('courier:legacy', stored)
    const loaded = (await loadCouriers()).couriers[0]!
    expect(loaded.identity.companionDeathMode).toBe('injury')
    expect(loaded.run?.companionDeathMode).toBe('injury')
    expect(loaded.checkpoint?.companionDeathMode).toBe('injury')
  })

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

  it('round-trips versioned companion leads through courier storage', async () => {
    const saved = courier('mika', 3)
    saved.campaign = migrateCampaignRoute({ version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [{ id: 'rescue:mine:1:mika', name: 'Mika', biome: 'mine', floor: 1 }] })
    saved.campaign = changeCampaignCompanionControlMode(saved.campaign, 'direct', 'lodge').state
    await saveCourier(saved, 'mika')
    const loaded = (await loadCouriers()).couriers[0]!
    expect(loaded.campaign).toMatchObject({ companionControlMode: 'direct', companionControlHistory: [{ sequence: 0, mode: 'autonomous', source: 'migration' }, { sequence: 1, mode: 'direct', source: 'lodge' }], companions: [{ version: 1, rosterStatus: 'lead', controlMode: 'direct', recruitment: { rescueId: 'rescue:mine:1:mika' } }] })
  })

  it('reloads campaign tier status from the persisted cycle', async () => {
    const saved = courier('tiered', 3)
    const cycle = advanceCampaignTier(completeCampaignTier(initialCampaignCycle()))
    saved.campaign = { ...saved.campaign, cycle }
    saved.run!.campaignCycle = structuredClone(cycle)
    saved.checkpoint!.campaignCycle = structuredClone(cycle)
    await saveCourier(saved, 'tiered')
    const loaded = (await loadCouriers()).couriers[0]!
    expect(loaded.campaign.cycle).toEqual(cycle)
    expect(hubCampaignStatus(loaded.campaign.cycle)).toMatchObject({ tier: 'ngPlus', completedTiers: ['base'], continuationPending: false, terminal: false })
  })
})
