import { describe, expect, it } from 'vitest'
import { newRun } from './engine'
import { migrateCampaignRoute, migrateRunRecord } from './storage'
import type { RunStateV1 } from './types'

describe('run persistence migration', () => {
  it('loads valid v2 records without changing them', () => {
    const run = newRun(123)
    expect(migrateRunRecord(run)).toEqual(run)
  })

  it('migrates valid v1 records to v2', () => {
    const legacy: RunStateV1 = { ...newRun(456), version: 1 }
    const migrated = migrateRunRecord(legacy)
    expect(migrated).toMatchObject({ version: 2, seed: 456 })
  })

  it('rejects malformed records so the caller stays at title', () => {
    expect(migrateRunRecord({ version: 2, seed: 1 })).toBeUndefined()
  })

  it('keeps only route progression when loading campaign state', () => {
    const route = migrateCampaignRoute({ version: 1, completedAreas: ['mine'], unlockedAreas: ['mine', 'wilds'], selectedBiome: 'wilds', hero: { gold: 999 } })
    expect(route).toEqual({ version: 1, completedAreas: ['mine'], unlockedAreas: ['mine', 'wilds'], selectedBiome: 'wilds' })
    expect(migrateCampaignRoute({ version: 1, completedAreas: ['mine'], unlockedAreas: [], selectedBiome: 'wilds' })).toEqual({ version: 1, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine' })
  })
})
