import { describe, expect, it } from 'vitest'
import { CAMPAIGN_AUTOPLAY_PROFILES, CAMPAIGN_AUTOPLAY_SEEDS, assertCampaignAutoplaySuite, campaignAutoplayDelta, campaignAutoplaySuite, compactCampaignAutoplayRun, summarizeCampaignAutoplay, type CampaignAutoplayRun, type CampaignAutoplaySuite } from './autoplay-campaign'
import { isCompleteCampaign, runAutoplay } from './autoplay-runner'
import { newRun, newSeededCampaignRun } from './engine'
import { BIOME_POOL, campaignOrderForSeed } from './engine/campaign'

const run = (profile: CampaignAutoplayRun['profile'], campaignComplete: boolean): CampaignAutoplayRun => ({
  seed: 7,
  profile,
  mode: profile === 'omniscient-clear' ? 'omniscient' : 'visible',
  policy: profile === 'omniscient-clear' ? 'clear' : 'explore',
  areaOrder: ['mine', 'wilds', 'caverns', 'ruins'],
  campaignComplete,
  outcome: campaignComplete ? 'complete' : 'stalled',
  turns: 240,
  finalBiome: campaignComplete ? 'ruins' : 'mine',
  floor: campaignComplete ? 16 : 1,
  completedAreas: campaignComplete ? ['mine', 'wilds', 'caverns', 'ruins'] : []
})

const suite = (runs: CampaignAutoplayRun[]): CampaignAutoplaySuite => ({
  version: 2,
  seeds: [7],
  turnLimit: 19_200,
  profiles: CAMPAIGN_AUTOPLAY_PROFILES.map(profile => ({ ...profile })),
  runs,
  summary: summarizeCampaignAutoplay(runs)
})

describe('campaign autoplay baseline', () => {
  it('covers every randomized biome in the multi-seed suite', () => {
    const covered = new Set(CAMPAIGN_AUTOPLAY_SEEDS.flatMap(seed => campaignOrderForSeed(seed)))
    expect(BIOME_POOL.every(biome => covered.has(biome))).toBe(true)
  })

  it('does not error during the Salt Flats seed that exercises an environmental kill', () => {
    const report = runAutoplay(newSeededCampaignRun(42), { mode: 'visible', policy: 'explore', turnLimit: 32, chainAreas: false })
    expect(report.outcome).not.toBe('error')
  }, 30_000)

  it('requires all four seeded areas for campaign completion', () => {
    expect(isCompleteCampaign('complete', ['mine', 'wilds', 'caverns', 'ruins'], ['mine', 'wilds', 'caverns', 'ruins'])).toBe(true)
    expect(isCompleteCampaign('complete', ['floodedRuins'])).toBe(false)
    const state = newRun(7, 'floodedRuins', 3, undefined, [], [], ['floodedRuins'])
    state.floor.actors = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    const report = runAutoplay(state, { mode: 'omniscient', policy: 'clear', turnLimit: 1 })
    expect(report).toMatchObject({ outcome: 'complete', campaignComplete: true, completedAreas: ['floodedRuins'] })
    expect(isCompleteCampaign('complete', ['furnace', 'mine'], ['furnace', 'mine'])).toBe(true)
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

  it('keeps a compact replay trace and floor metadata on failures', () => {
    const report = runAutoplay(newRun(7), { mode: 'omniscient', policy: 'clear', turnLimit: 1, captureTrace: true })
    const compact = compactCampaignAutoplayRun(7, CAMPAIGN_AUTOPLAY_PROFILES[0], report)
    expect(compact.failure).toMatchObject({ replay: { seed: 7, biome: 'mine', areaFloor: 0, floorIndex: 0, layoutId: expect.any(String), macroRecipeId: expect.any(String), routeContractId: expect.any(String), objectiveId: expect.any(String), escalation: expect.any(String) } })
    expect(compact.failure?.trace).toHaveLength(1)
    expect(compact.failure?.trace[0]?.replay).toEqual(compact.failure?.replay)
  })
})
