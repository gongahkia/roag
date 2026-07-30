import { describe, expect, it } from 'vitest'
import { CAMPAIGN_AUTOPLAY_PROFILES, campaignAutoplayEntries, campaignAutoplaySuite, type CampaignAutoplayRun } from './autoplay-campaign'
import { autoplayHeuristicProfile, autoplayHeuristicProfileRef } from './autoplay-heuristics'
import { compareFrozenAutoplayBaseline, createFrozenAutoplayBaseline, parseFrozenAutoplayBaseline } from './autoplay-frozen-baseline'
import { createPolicyProfile, createPolicyRunMetadata, type PolicyScore } from './autoplay-policy'

const heuristic = autoplayHeuristicProfileRef(autoplayHeuristicProfile('compatibility'))
const score = (explorationValue: number): PolicyScore => ({ campaignClears: 1, deaths: 0, stalls: 0, explorationValue, resourceEfficiency: 4 })
const suite = (explorationValue = 12) => campaignAutoplaySuite(campaignAutoplayEntries('development').flatMap(entry => CAMPAIGN_AUTOPLAY_PROFILES.map(profile => {
  const episodeScore = score(explorationValue)
  return {
    seed: entry.seed,
    profile: profile.id,
    mode: profile.mode,
    policy: profile.policy,
    heuristicProfile: heuristic,
    areaOrder: [...entry.routeConfiguration.areaOrder],
    campaignComplete: true,
    outcome: 'complete',
    turns: 240,
    finalBiome: entry.routeConfiguration.areaOrder.at(-1)!,
    floor: 20,
    completedAreas: [...entry.routeConfiguration.areaOrder],
    evaluation: {
      policyMetadata: createPolicyRunMetadata(createPolicyProfile({ informationMode: profile.mode, policy: profile.policy }), entry.seed, entry.turnBudget, episodeScore),
      explorationValue: episodeScore.explorationValue,
      resourcesSpent: 0,
      resourcesRetained: 4,
      resourceOutcomes: { selected: 0, deferred: 0, rejected: 0, projectedRouteGains: 0, criticalRouteSelections: 0 },
      optionalOutcomes: { pursued: 0, deferred: 0, declined: 0, secrets: 0, shortcuts: 0 }
    }
  } satisfies CampaignAutoplayRun
})), 'development')

describe('frozen autoplay baseline', () => {
  it('passes an equal full compatibility suite and reports every seed/profile tuple', () => {
    const current = suite()
    const baseline = createFrozenAutoplayBaseline(current, heuristic, { reviewedBy: 'qa', review: 'deterministic compatibility review' })
    const comparison = compareFrozenAutoplayBaseline(current, baseline, heuristic)
    expect(comparison).toMatchObject({ passed: true, runtimeTolerance: 'wall-time is diagnostic-only and excluded from objective comparison' })
    expect(comparison.evidence).toHaveLength(current.runs.length)
    expect(comparison.evidence[0]).toMatchObject({ baseline: [1, 0, 0, 12, 4], candidate: [1, 0, 0, 12, 4], comparison: 'equal' })
  })

  it('fails a lexicographically worse per-seed objective tuple', () => {
    const baseline = createFrozenAutoplayBaseline(suite(), heuristic, { reviewedBy: 'qa', review: 'deterministic compatibility review' })
    const comparison = compareFrozenAutoplayBaseline(suite(11), baseline, heuristic)
    expect(comparison.passed).toBe(false)
    expect(comparison.regressions).toHaveLength(baseline.suite.runs.length)
    expect(comparison.regressions[0]).toMatchObject({ baseline: [1, 0, 0, 12, 4], candidate: [1, 0, 0, 11, 4], comparison: 'regression' })
  })

  it('creates a reviewed approval artifact without an ordinary-suite side effect', () => {
    const current = suite()
    const baseline = createFrozenAutoplayBaseline(current, heuristic, { reviewedBy: 'release-engineer', review: 'approved after tuple diff inspection' })
    expect(baseline.review).toEqual({ reviewedBy: 'release-engineer', review: 'approved after tuple diff inspection' })
    expect(baseline.suite).toEqual(current)
  })

  it('rejects stale or legacy baseline versions', () => {
    const baseline = createFrozenAutoplayBaseline(suite(), heuristic, { reviewedBy: 'qa', review: 'deterministic compatibility review' })
    expect(() => parseFrozenAutoplayBaseline({ ...baseline, engineVersion: 0 })).toThrow('stale engine version')
    expect(() => parseFrozenAutoplayBaseline({ version: 2 })).toThrow('unsupported version')
  })
})
