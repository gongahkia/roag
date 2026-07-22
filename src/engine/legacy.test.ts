import { describe, expect, it } from 'vitest'
import { initialCampaignRoute } from './campaign'
import { legacyRecordForDeath, recordDeath } from './legacy'
import { createRun } from '../test/factories'

describe('death history', () => {
  it('records only durable journal details', () => {
    const state = createRun({ seed: 77, area: 'wilds', areaFloor: 2, turn: 9 })
    expect(legacyRecordForDeath(state, 'Ari Vale')).toEqual({ id: 'legacy:77:0:9', heirName: 'Ari Vale', biome: 'wilds', floor: 2, seed: 77 })
  })

  it('appends one bounded death record to the campaign route', () => {
    const route = recordDeath(initialCampaignRoute(), createRun({ seed: 8 }), 'Ari Vale')
    expect(route.legacyRecords).toEqual([{ id: 'legacy:8:0:0', heirName: 'Ari Vale', biome: 'mine', floor: 0, seed: 8 }])
  })
})
