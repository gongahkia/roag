import { describe, expect, it } from 'vitest'
import { autoplayReplayMetadata } from '../autoplay-runner'
import { migrateCampaignRoute } from '../storage'
import type { CampaignCycle } from '../types'
import { advanceCampaignTier, assertCampaignCycle, campaignCycleErrors, cloneCampaignCycle, completeCampaignTier, initialCampaignCycle } from './campaign'
import { newRun } from './run'

describe('three-tier campaign cycle', () => {
  it('serializes, clones, and migrates legacy campaigns to base', () => {
    const cycle = initialCampaignCycle()
    const clone = cloneCampaignCycle(cycle)
    clone.events[0]!.sequence = 9
    expect(cycle.events[0]!.sequence).toBe(0)
    expect(JSON.parse(JSON.stringify(cycle))).toEqual(cycle)
    expect(migrateCampaignRoute({ version: 5, areaOrder: ['mine', 'wilds', 'caverns', 'ruins'], completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine' }).cycle).toEqual(cycle)
  })

  it('records deterministic entry and victory markers through the completed cap', () => {
    const base = initialCampaignCycle()
    const plus = advanceCampaignTier(completeCampaignTier(base))
    const plusPlus = advanceCampaignTier(completeCampaignTier(plus))
    const capped = completeCampaignTier(plusPlus)
    expect(capped).toMatchObject({ currentTier: 'ngPlusPlus', completedTiers: ['base', 'ngPlus', 'ngPlusPlus'], completedCap: true, events: [
      { sequence: 0, tier: 'base', kind: 'entered' },
      { sequence: 1, tier: 'base', kind: 'victory' },
      { sequence: 2, tier: 'ngPlus', kind: 'entered' },
      { sequence: 3, tier: 'ngPlus', kind: 'victory' },
      { sequence: 4, tier: 'ngPlusPlus', kind: 'entered' },
      { sequence: 5, tier: 'ngPlusPlus', kind: 'victory' }
    ] })
    expect(() => advanceCampaignTier(capped)).toThrow('NG++ completed cap reached')
  })

  it('fails fast for invalid states and transitions and exposes the cycle in replay metadata', () => {
    const invalid: CampaignCycle = { ...initialCampaignCycle(), completedTiers: ['ngPlus'] }
    expect(campaignCycleErrors(invalid)).toContain('completed tiers must be an ordered prefix')
    expect(() => assertCampaignCycle(invalid)).toThrow('invalid campaign cycle')
    expect(() => advanceCampaignTier(initialCampaignCycle())).toThrow('victory not recorded')
    const plus = advanceCampaignTier(completeCampaignTier(initialCampaignCycle()))
    const state = newRun(7, 'mine', 0, undefined, [], [], ['mine'], plus)
    expect(autoplayReplayMetadata(state).campaignCycle).toEqual(plus)
  })
})
