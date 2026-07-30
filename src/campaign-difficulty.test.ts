import { describe, expect, it } from 'vitest'
import { autoplayReplayMetadata } from './autoplay-runner'
import { CAMPAIGN_DIFFICULTY_PACKAGES, campaignDifficultyPackageForCycle, resolveCampaignDifficulty } from './campaign-difficulty'
import { advanceCampaignTier, completeCampaignTier, initialCampaignCycle } from './engine/campaign'
import { newRun } from './engine/run'
import { migrateRunRecord } from './storage'
import { difficultyFor } from './world'

const base = initialCampaignCycle()
const plus = advanceCampaignTier(completeCampaignTier(base))
const plusPlus = advanceCampaignTier(completeCampaignTier(plus))
const capped = completeCampaignTier(plusPlus)

describe('campaign difficulty packages', () => {
  it('defines one complete, versioned package for each auditable tier', () => {
    expect(CAMPAIGN_DIFFICULTY_PACKAGES).toEqual([
      { version: 1, id: 'base-v1', tier: 'base', name: 'Base Route', rationale: 'Preserves route-position difficulty without campaign-tier modifiers.', modifiers: { threat: 0, healthMultiplier: 1, attackBonus: 0, defenseBonus: 0, eliteChance: 0, guardianPattern: 0, hazardMultiplier: 1, rewardMultiplier: 1 } },
      { version: 1, id: 'ng-plus-v1', tier: 'ngPlus', name: 'New Game+', rationale: 'Adds fixed pressure after route difficulty while retaining a modest deterministic reward uplift.', modifiers: { threat: 3, healthMultiplier: 1.15, attackBonus: 1, defenseBonus: 1, eliteChance: 8, guardianPattern: 1, hazardMultiplier: 1.15, rewardMultiplier: 1.1 } },
      { version: 1, id: 'ng-plus-plus-v1', tier: 'ngPlusPlus', name: 'New Game++', rationale: 'Raises fixed late-cycle pressure and rewards without any inventory, elapsed-time, or run-count scaling.', modifiers: { threat: 6, healthMultiplier: 1.3, attackBonus: 2, defenseBonus: 2, eliteChance: 16, guardianPattern: 2, hazardMultiplier: 1.3, rewardMultiplier: 1.2 } }
    ])
  })

  it('resolves route-position difficulty before the fixed tier package', () => {
    const route = difficultyFor(2, 1)
    expect(route).toEqual({ routePosition: 2, threat: 9, healthMultiplier: 1.54, attackBonus: 4, defenseBonus: 1, eliteChance: 22, guardianPattern: 2 })
    expect(resolveCampaignDifficulty(route, base)).toMatchObject({ threat: 9, healthMultiplier: 1.54, attackBonus: 4, defenseBonus: 1, eliteChance: 22, guardianPattern: 2, hazardMultiplier: 1, rewardMultiplier: 1, campaignTier: 'base', difficultyPackage: { id: 'base-v1', version: 1 } })
    expect(resolveCampaignDifficulty(route, plus)).toMatchObject({ threat: 12, healthMultiplier: 1.771, attackBonus: 5, defenseBonus: 2, eliteChance: 30, guardianPattern: 2, hazardMultiplier: 1.15, rewardMultiplier: 1.1, campaignTier: 'ngPlus', difficultyPackage: { id: 'ng-plus-v1', version: 1 } })
    expect(resolveCampaignDifficulty(route, plusPlus)).toMatchObject({ threat: 15, healthMultiplier: 2.0020000000000002, attackBonus: 6, defenseBonus: 3, eliteChance: 35, guardianPattern: 2, hazardMultiplier: 1.3, rewardMultiplier: 1.2, campaignTier: 'ngPlusPlus', difficultyPackage: { id: 'ng-plus-plus-v1', version: 1 } })
  })

  it('serializes deterministic tier metadata and applies no package after the terminal cap', () => {
    const route = difficultyFor(1, 2)
    const resolved = resolveCampaignDifficulty(route, plus)
    const state = newRun(73, 'mine', 0, undefined, [], [], undefined, plus)
    state.floor.difficulty = resolved
    expect(JSON.parse(JSON.stringify(resolved))).toEqual(resolved)
    expect(migrateRunRecord(JSON.parse(JSON.stringify(state)))!.floor.difficulty).toEqual(resolved)
    expect(resolveCampaignDifficulty(route, capped)).toEqual(route)
    expect(campaignDifficultyPackageForCycle(capped)).toBeUndefined()
    expect(autoplayReplayMetadata(state)).toMatchObject({ campaignCycle: plus, difficultyPackage: { id: 'ng-plus-v1', tier: 'ngPlus', version: 1 } })
    expect(autoplayReplayMetadata(newRun(73, 'mine', 0, undefined, [], [], undefined, capped))).not.toHaveProperty('difficultyPackage')
  })

  it('resolves the same seed, tier, and route context identically', () => {
    const first = resolveCampaignDifficulty(difficultyFor(3, 2), plusPlus)
    const second = resolveCampaignDifficulty(difficultyFor(3, 2), plusPlus)
    expect(first).toEqual(second)
  })
})
