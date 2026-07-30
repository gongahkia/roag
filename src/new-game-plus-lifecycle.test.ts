import { describe, expect, it } from 'vitest'
import { completeCampaignTier, initialCampaignCycle } from './engine/campaign'
import { newGamePlusLifecycleFixture, NEW_GAME_PLUS_LIFECYCLE_VERSION, validateNewGamePlusLifecycleCorpus } from './new-game-plus-lifecycle'
import { migrateCampaignRoute } from './storage'

const smoke = validateNewGamePlusLifecycleCorpus('smoke')

describe('New Game+ lifecycle regression', () => {
  it('reaches the terminal cap through deferred continuations without carryover loss', () => {
    expect(smoke).toMatchObject({ version: NEW_GAME_PLUS_LIFECYCLE_VERSION, mode: 'smoke', seeds: [7], accepted: true, failures: [] })
    expect(smoke.runs).toHaveLength(1)
    expect(smoke.runs[0]).toMatchObject({ seed: 7, accepted: true, terminalCap: true, stages: [
      { tier: 'base', replayValid: true, reloadValid: true, optionalContentValid: true, victoryRecorded: true, continuationPending: true, carryoverPreserved: true, carryoverDiagnostics: 1, difficultyPackage: { id: 'base-v1', tier: 'base' } },
      { tier: 'ngPlus', replayValid: true, reloadValid: true, optionalContentValid: true, victoryRecorded: true, continuationPending: true, carryoverPreserved: true, carryoverDiagnostics: 2, difficultyPackage: { id: 'ng-plus-v1', tier: 'ngPlus' } },
      { tier: 'ngPlusPlus', replayValid: true, reloadValid: true, optionalContentValid: true, victoryRecorded: true, continuationPending: false, carryoverPreserved: true, carryoverDiagnostics: 2, difficultyPackage: { id: 'ng-plus-plus-v1', tier: 'ngPlusPlus' } }
    ] })
  }, 90_000)

  it('migrates legacy routes to Base and preserves a pending tier victory', () => {
    const fixture = newGamePlusLifecycleFixture(7)
    const legacy = structuredClone(fixture.campaign) as { version: number; cycle?: unknown }
    legacy.version = 4
    delete legacy.cycle
    expect(migrateCampaignRoute(legacy).cycle).toEqual(initialCampaignCycle())
    const pending = { ...fixture.campaign, cycle: completeCampaignTier(fixture.campaign.cycle) }
    expect(migrateCampaignRoute(JSON.parse(JSON.stringify(pending))).cycle).toEqual(pending.cycle)
  })
})
