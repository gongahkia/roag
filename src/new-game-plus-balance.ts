import { AUTOPLAY_MAX_TURNS } from './autoplay'
import { runAutoplay, type AutoplayOutcome } from './autoplay-runner'
import { newSeededCampaignRun } from './engine/run'
import { newGamePlusLifecycleFixture, NEW_GAME_PLUS_LIFECYCLE_CORPUS, type NewGamePlusLifecycleMode } from './new-game-plus-lifecycle'
import { campaignCycleForValidationTier } from './new-game-plus-validation'
import type { CampaignTier } from './types'

export const NEW_GAME_PLUS_BALANCE_VERSION = 1 as const
const tiers: readonly CampaignTier[] = ['base', 'ngPlus', 'ngPlusPlus']

export interface NewGamePlusBalanceSample { seed: number; tier: CampaignTier; outcome: AutoplayOutcome; clear: boolean; death: boolean; stall: boolean; turns: number; resources: { bombs: number; ropes: number; goldSpent: number; items: number; traversalTools: number } }
export interface NewGamePlusBalanceTotals { samples: number; clears: number; deaths: number; stalls: number; turns: number; resources: { bombs: number; ropes: number; goldSpent: number; items: number; traversalTools: number } }
export interface NewGamePlusBalanceReport { version: typeof NEW_GAME_PLUS_BALANCE_VERSION; mode: NewGamePlusLifecycleMode; seeds: number[]; turnLimit: number; policy: 'omniscient-clear'; reviewOnly: true; byTier: Record<CampaignTier, NewGamePlusBalanceTotals>; samples: NewGamePlusBalanceSample[] }

const totals = (samples: readonly NewGamePlusBalanceSample[]): NewGamePlusBalanceTotals => ({
  samples: samples.length,
  clears: samples.filter(sample => sample.clear).length,
  deaths: samples.filter(sample => sample.death).length,
  stalls: samples.filter(sample => sample.stall).length,
  turns: samples.reduce((total, sample) => total + sample.turns, 0),
  resources: {
    bombs: samples.reduce((total, sample) => total + sample.resources.bombs, 0),
    ropes: samples.reduce((total, sample) => total + sample.resources.ropes, 0),
    goldSpent: samples.reduce((total, sample) => total + sample.resources.goldSpent, 0),
    items: samples.reduce((total, sample) => total + sample.resources.items, 0),
    traversalTools: samples.reduce((total, sample) => total + sample.resources.traversalTools, 0)
  }
})

export const newGamePlusBalanceReport = (mode: NewGamePlusLifecycleMode = 'full', turnLimit = AUTOPLAY_MAX_TURNS): NewGamePlusBalanceReport => {
  if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid New Game+ balance turn limit: ${turnLimit}`)
  const seeds = [...NEW_GAME_PLUS_LIFECYCLE_CORPUS[mode]]
  const samples = seeds.flatMap(seed => tiers.map(tier => {
    const fixture = newGamePlusLifecycleFixture(seed)
    const report = runAutoplay(newSeededCampaignRun(seed, fixture.hero, fixture.campaign.rescuedNpcs, fixture.campaign.legacyRecords, campaignCycleForValidationTier(tier), fixture.campaign.companions, 'permadeath'), { mode: 'omniscient', policy: 'clear', turnLimit, captureTrace: false })
    return { seed, tier, outcome: report.outcome, clear: report.campaignComplete, death: report.outcome === 'dead', stall: report.outcome === 'stalled' || report.outcome === 'turn-limit', turns: report.turns, resources: { bombs: report.metrics.bombsUsed, ropes: report.metrics.ropesUsed, goldSpent: report.metrics.goldSpent, items: Object.values(report.metrics.itemsUsed).reduce((total, count) => total + count, 0), traversalTools: report.toolOutcomes.uses } }
  }))
  return { version: NEW_GAME_PLUS_BALANCE_VERSION, mode, seeds, turnLimit, policy: 'omniscient-clear', reviewOnly: true, byTier: Object.fromEntries(tiers.map(tier => [tier, totals(samples.filter(sample => sample.tier === tier))])) as Record<CampaignTier, NewGamePlusBalanceTotals>, samples }
}
