import { describe, expect, it } from 'vitest'
import type { CampaignRouteState, LegacyRecord } from './types'

const legacy: LegacyRecord = { id: 'heir-1', heirName: 'Ari', biome: 'mine', floor: 3, seed: 7 }
const route: CampaignRouteState = { version: 3, areaOrder: ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], lineageEvents: [], legacyRecords: [legacy] }

describe('campaign route types', () => {
  it('contains only persisted progression and journal state', () => {
    expect(route).toEqual({ version: 3, areaOrder: ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], lineageEvents: [], legacyRecords: [legacy] })
  })
})
