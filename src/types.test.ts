import { describe, expect, it } from 'vitest'
import { initialCampaignCycle } from './engine/campaign'
import type { CampaignRouteState, LegacyRecord } from './types'

const legacy: LegacyRecord = { id: 'heir-1', heirName: 'Ari', biome: 'mine', floor: 3, seed: 7 }
const route: CampaignRouteState = { version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], companions: [], carryoverDiagnostics: [], lineageEvents: [], legacyRecords: [legacy], alignment: { kami: 0, villagePact: 0 }, cycle: initialCampaignCycle() }

describe('campaign route types', () => {
  it('contains only persisted progression and journal state', () => {
    expect(route).toEqual({ version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], companions: [], carryoverDiagnostics: [], lineageEvents: [], legacyRecords: [legacy], alignment: { kami: 0, villagePact: 0 }, cycle: initialCampaignCycle() })
  })
})
