import { describe, expect, it } from 'vitest'
import { CAMPAIGN_AUTOPLAY_PROFILES, assertCampaignAutoplaySuite, campaignAutoplayDelta, campaignAutoplaySuite, summarizeCampaignAutoplay, type CampaignAutoplayRun, type CampaignAutoplaySuite } from './autoplay-campaign'
import { isCompleteCampaign, runAutoplay } from './autoplay-runner'
import { newRun } from './engine'

const run = (profile: CampaignAutoplayRun['profile'], campaignComplete: boolean): CampaignAutoplayRun => ({
  seed: 7,
  profile,
  mode: profile === 'omniscient-clear' ? 'omniscient' : 'visible',
  policy: profile === 'omniscient-clear' ? 'clear' : 'explore',
  campaignComplete,
  outcome: campaignComplete ? 'complete' : 'stalled',
  turns: 240,
  finalBiome: campaignComplete ? 'floodedRuins' : 'mine',
  floor: campaignComplete ? 24 : 1,
  completedAreas: campaignComplete ? ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'] : []
})

const suite = (runs: CampaignAutoplayRun[]): CampaignAutoplaySuite => ({
  version: 1,
  seeds: [7],
  turnLimit: 19_200,
  profiles: CAMPAIGN_AUTOPLAY_PROFILES.map(profile => ({ ...profile })),
  runs,
  summary: summarizeCampaignAutoplay(runs)
})

describe('campaign autoplay baseline', () => {
  it('requires all six ordered areas for campaign completion', () => {
    expect(isCompleteCampaign('complete', ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'])).toBe(true)
    expect(isCompleteCampaign('complete', ['floodedRuins'])).toBe(false)
    const state = newRun(7, 'floodedRuins', 3)
    state.floor.actors = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    const report = runAutoplay(state, { mode: 'omniscient', policy: 'clear', turnLimit: 1 })
    expect(report).toMatchObject({ outcome: 'complete', campaignComplete: false, completedAreas: ['floodedRuins'] })
  })

  it('summarizes failure rates and reports improvements as negative deltas', () => {
    const baseline = suite([run('omniscient-clear', false), run('visible-explore', false)])
    const current = suite([run('omniscient-clear', true), run('visible-explore', false)])
    expect(current.summary).toMatchObject({ total: 2, completed: 1, failed: 1, failureRate: .5 })
    expect(campaignAutoplayDelta(current, baseline)).toEqual({ overall: -.5, byProfile: { 'omniscient-clear': -1, 'visible-explore': 0 } })
  })

  it('rejects a seed/profile matrix with missing runs', () => {
    expect(() => campaignAutoplaySuite([])).toThrow('campaign autoplay suite has missing or duplicate seed/profile runs')
    expect(() => assertCampaignAutoplaySuite(suite([run('omniscient-clear', true), run('visible-explore', false)]))).toThrow('campaign autoplay suite has missing or duplicate seed/profile runs')
  })
})
