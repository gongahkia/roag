import { describe, expect, it } from 'vitest'
import { newHero, newRun } from './engine'
import { migrateCampaignRoute, migrateRunRecord } from './storage'

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

  it('rejects v1 and v2 runs after the prop schema change', () => {
    expect(migrateRunRecord({ ...newRun(456), version: 1 })).toBeUndefined()
    expect(migrateRunRecord({ ...newRun(456), version: 2 })).toBeUndefined()
  })

  it('rejects incomplete current-schema heroes', () => {
    const legacy = newRun(457)
    delete (legacy.hero as Partial<typeof legacy.hero>).name
    delete (legacy.hero as Partial<typeof legacy.hero>).origin
    delete (legacy.hero as Partial<typeof legacy.hero>).calling
    delete (legacy.hero as Partial<typeof legacy.hero>).deathMode
    expect(migrateRunRecord(legacy)).toBeUndefined()
  })

  it('builds mechanical origins and starter callings', () => {
    const hero = newHero({ name: 'Ari', origin: 'cavernSeeker', calling: 'spiritbearer', deathMode: 'ironTrail' })
    expect(hero).toMatchObject({ name: 'Ari', stats: { intellect: 3 }, deathMode: 'ironTrail', inventory: expect.arrayContaining(['focusTonic', 'sight']) })
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
    expect(route).toEqual({ version: 1, completedAreas: ['mine'], unlockedAreas: ['mine', 'wilds'], selectedBiome: 'wilds', rescuedNpcs: [], lineageEvents: [], legacyRecords: [], legacyEncounterAreas: [] })
    expect(migrateCampaignRoute({ version: 1, completedAreas: ['mine'], unlockedAreas: [], selectedBiome: 'wilds' })).toEqual({ version: 1, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], lineageEvents: [], legacyRecords: [], legacyEncounterAreas: [] })
  })

  it('retains canonical legacy records in campaign storage', () => {
    const legacy = { id: 'legacy-1', heirName: 'Ari', cause: 'defeated' as const, biome: 'mine' as const, floor: 2, seed: 9, lineage: ['Ari'], location: { x: 4, y: 6 }, cache: { gold: 30, items: ['tonic'] }, encounter: { kind: 'cache' as const, resolved: false } }
    expect(migrateCampaignRoute({ version: 1, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', legacyRecords: [legacy] }).legacyRecords).toEqual([legacy])
  })
})
