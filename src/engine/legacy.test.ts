import { describe, expect, it } from 'vitest'
import { initialCampaignRoute } from './campaign'
import { legacyRecordForDeath, recordDeath, selectLegacyEncounter } from './legacy'
import { createRun } from '../test/factories'

describe('legacy death records', () => {
  it('captures lineage, location, cache, and encounter state before persisting', () => {
    const state = createRun({ seed: 77, area: 'wilds', areaFloor: 2, turn: 9 })
    state.hero.x = 6
    state.hero.y = 8
    state.hero.gold = 40
    state.hero.inventory = ['tonic', 'ropeBundle']
    expect(legacyRecordForDeath(state, 'Ari Vale', ['Bea Morrow'])).toEqual({ id: 'legacy:77:0:9', heirName: 'Ari Vale', cause: 'defeated', biome: 'wilds', floor: 2, seed: 77, lineage: ['Bea Morrow', 'Ari Vale'], location: { x: 6, y: 8 }, cache: { gold: 40, items: ['tonic', 'ropeBundle'] }, encounter: { kind: 'cache', resolved: false } })
  })

  it('appends one bounded record to the campaign route', () => {
    const route = recordDeath(initialCampaignRoute(), createRun({ seed: 8 }), 'Ari Vale')
    expect(route.legacyRecords).toHaveLength(1)
    expect(route.legacyRecords[0]).toMatchObject({ cause: 'defeated', heirName: 'Ari Vale' })
  })

  it('selects one eligible legacy encounter per area deterministically', () => {
    const campaign = recordDeath(recordDeath(initialCampaignRoute(), createRun({ seed: 8 }), 'Ari Vale'), createRun({ seed: 9 }), 'Bea Morrow')
    const first = selectLegacyEncounter(campaign, 'mine', 44)
    expect(first.record?.biome).toBe('mine')
    expect(selectLegacyEncounter(first.campaign, 'mine', 44).record).toBeUndefined()
    expect(selectLegacyEncounter(campaign, 'wilds', 44).record).toBeUndefined()
  })
})
