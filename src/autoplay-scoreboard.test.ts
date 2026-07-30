import { describe, expect, it } from 'vitest'
import { createPolicyProfile, createPolicyRunMetadata, policyScoreTuple, type PolicyScore } from './autoplay-policy'
import { CAMPAIGN_AUTOPLAY_PROFILES, type CampaignAutoplayRun } from './autoplay-campaign'
import { assertAutoplayScoreboard, createAutoplayScoreboard, formatAutoplayScoreboard, type AutoplayScoreboardInput } from './autoplay-scoreboard'

const score = (campaignClears: number, deaths: number, stalls: number, explorationValue: number, resourceEfficiency: number): PolicyScore => ({ campaignClears, deaths, stalls, explorationValue, resourceEfficiency })
const run = (seed: number, profile: typeof CAMPAIGN_AUTOPLAY_PROFILES[number], outcome: CampaignAutoplayRun['outcome'], currentScore: PolicyScore): CampaignAutoplayRun => ({
  seed,
  profile: profile.id,
  mode: profile.mode,
  policy: profile.policy,
  areaOrder: ['mine', 'wilds', 'caverns', 'ruins'],
  campaignComplete: outcome === 'complete',
  outcome,
  turns: 20,
  finalBiome: 'mine',
  floor: 1,
  completedAreas: [],
  evaluation: {
    policyMetadata: createPolicyRunMetadata(createPolicyProfile({ policy: profile.policy, informationMode: profile.mode }), seed, 100, currentScore),
    explorationValue: currentScore.explorationValue,
    resourcesSpent: 3,
    resourcesRetained: 5
  }
})

const input = (runs: CampaignAutoplayRun[], seeds: number[] = [1, 2]): AutoplayScoreboardInput => ({ partition: 'development', seeds, profiles: CAMPAIGN_AUTOPLAY_PROFILES, runs })

describe('autoplay evaluation scoreboard', () => {
  it('sorts rows and retains missing, failed, and stalled episodes', () => {
    const scoreboard = createAutoplayScoreboard(input([
      run(2, CAMPAIGN_AUTOPLAY_PROFILES[0], 'dead', score(0, 1, 0, 4, 0)),
      run(1, CAMPAIGN_AUTOPLAY_PROFILES[1], 'stalled', score(0, 0, 1, 8, 1)),
      run(1, CAMPAIGN_AUTOPLAY_PROFILES[0], 'complete', score(1, 0, 0, 12, 2))
    ]))
    expect(scoreboard.episodes.map(entry => `${entry.seed}/${entry.profile}/${entry.status}`)).toEqual([
      '1/omniscient-clear/complete',
      '1/visible-explore/failed',
      '2/omniscient-clear/failed',
      '2/visible-explore/missing'
    ])
    expect(scoreboard.overall).toMatchObject({ expected: 4, observed: 3, missing: 1, incomplete: 0, failed: 2, clearCount: 1, clearRate: .25, deaths: 1, stalls: 1, turns: 60, explorationValue: 24, resourcesSpent: 9, resourcesRetained: 15, scoreTuple: [1, 1, 1, 24, 3] })
    expect(scoreboard.winner).toBeUndefined()
    expect(formatAutoplayScoreboard(scoreboard)).toContain('winner=none')
  })

  it('withholds a winner on a comparator tie', () => {
    const equal = score(1, 0, 0, 10, 2)
    const scoreboard = createAutoplayScoreboard(input([
      run(1, CAMPAIGN_AUTOPLAY_PROFILES[0], 'complete', equal),
      run(1, CAMPAIGN_AUTOPLAY_PROFILES[1], 'complete', equal)
    ], [1]))
    expect(scoreboard.byProfile.map(entry => entry.scoreTuple)).toEqual([[1, 0, 0, 10, 2], [1, 0, 0, 10, 2]])
    expect(scoreboard.winner).toBeUndefined()
  })

  it('rejects a scoreboard that differs from its episode data', () => {
    const current = createAutoplayScoreboard(input([
      run(1, CAMPAIGN_AUTOPLAY_PROFILES[0], 'complete', score(1, 0, 0, 10, 2)),
      run(1, CAMPAIGN_AUTOPLAY_PROFILES[1], 'stalled', score(0, 0, 1, 8, 0))
    ], [1]))
    current.overall.scoreTuple = policyScoreTuple(score(99, 0, 0, 0, 0))
    expect(() => assertAutoplayScoreboard(current, input([
      run(1, CAMPAIGN_AUTOPLAY_PROFILES[0], 'complete', score(1, 0, 0, 10, 2)),
      run(1, CAMPAIGN_AUTOPLAY_PROFILES[1], 'stalled', score(0, 0, 1, 8, 0))
    ], [1]))).toThrow('autoplay scoreboard does not match campaign runs')
  })
})
