import { AUTOPLAY_HEURISTIC_PROFILE_VERSION, type AutoplayHeuristicProfileRef } from './autoplay-heuristics'
import { CAMPAIGN_AUTOPLAY_PROFILES, assertCampaignAutoplaySuite, campaignAutoplayEntries, type CampaignAutoplayProfile, type CampaignAutoplayProfileId, type CampaignAutoplaySuite } from './autoplay-campaign'
import { POLICY_OBJECTIVE_ID, POLICY_OBJECTIVE_VERSION, POLICY_PROFILE_VERSION, comparePolicyScores, policyScoreTuple, type PolicyScoreTuple } from './autoplay-policy'
import { AUTOPLAY_SEED_CORPUS_VERSION } from './autoplay-seed-corpus'

export const AUTOPLAY_FROZEN_BASELINE_VERSION = 1 as const
export const AUTOPLAY_ENGINE_VERSION = 1 as const
export const AUTOPLAY_BASELINE_RUNTIME_TOLERANCE = 'wall-time is diagnostic-only and excluded from objective comparison' as const

export type FrozenAutoplayBaselineProfile = CampaignAutoplayProfile & { policyProfileVersion: typeof POLICY_PROFILE_VERSION }
export interface FrozenAutoplayBaselineReview { reviewedBy: string; review: string }
export interface FrozenAutoplayBaseline {
  version: typeof AUTOPLAY_FROZEN_BASELINE_VERSION
  engineVersion: typeof AUTOPLAY_ENGINE_VERSION
  corpusVersion: typeof AUTOPLAY_SEED_CORPUS_VERSION
  objective: { id: typeof POLICY_OBJECTIVE_ID; version: typeof POLICY_OBJECTIVE_VERSION }
  heuristicProfile: AutoplayHeuristicProfileRef
  profiles: FrozenAutoplayBaselineProfile[]
  review: FrozenAutoplayBaselineReview
  suite: CampaignAutoplaySuite
}
export interface FrozenAutoplayBaselineEvidence { seed: number; profile: CampaignAutoplayProfileId; baseline: PolicyScoreTuple; candidate: PolicyScoreTuple; comparison: 'better' | 'equal' | 'regression' }
export interface FrozenAutoplayBaselineComparison { passed: boolean; runtimeTolerance: typeof AUTOPLAY_BASELINE_RUNTIME_TOLERANCE; evidence: FrozenAutoplayBaselineEvidence[]; regressions: FrozenAutoplayBaselineEvidence[] }

const profileKey = (profile: Pick<CampaignAutoplayProfile, 'id' | 'mode' | 'policy'>): string => `${profile.id}:${profile.mode}:${profile.policy}`
const object = (value: unknown): Record<string, unknown> | undefined => value !== null && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : undefined

const assertReview = (review: FrozenAutoplayBaselineReview): void => {
  if (!review || typeof review.reviewedBy !== 'string' || !review.reviewedBy.trim() || typeof review.review !== 'string' || !review.review.trim()) throw new Error('frozen autoplay baseline requires reviewedBy and review')
}

const assertProfiles = (profiles: readonly FrozenAutoplayBaselineProfile[]): void => {
  const expected = CAMPAIGN_AUTOPLAY_PROFILES.map(profile => profileKey(profile)).sort()
  const actual = profiles.map(profile => profileKey(profile)).sort()
  if (actual.length !== expected.length || actual.some((profile, index) => profile !== expected[index])) throw new Error('frozen autoplay baseline profiles do not match campaign profiles')
  if (profiles.some(profile => profile.policyProfileVersion !== POLICY_PROFILE_VERSION)) throw new Error('frozen autoplay baseline has an unsupported policy profile version')
}

const assertEvaluations = (suite: CampaignAutoplaySuite): void => {
  if (suite.runs.some(run => !run.evaluation)) throw new Error('frozen autoplay baseline requires objective evaluation for every run')
  for (const run of suite.runs) {
    const metadata = run.evaluation!.policyMetadata
    if (metadata.profile.version !== POLICY_PROFILE_VERSION || metadata.profile.objectiveVersion !== POLICY_OBJECTIVE_VERSION || metadata.profile.objectiveId !== POLICY_OBJECTIVE_ID) throw new Error('frozen autoplay baseline has an unsupported objective or policy profile version')
  }
}

export const assertFrozenAutoplayBaseline = (baseline: FrozenAutoplayBaseline): void => {
  if (!baseline || baseline.version !== AUTOPLAY_FROZEN_BASELINE_VERSION) throw new Error('frozen autoplay baseline has an unsupported version')
  if (baseline.engineVersion !== AUTOPLAY_ENGINE_VERSION) throw new Error('frozen autoplay baseline has a stale engine version')
  if (baseline.corpusVersion !== AUTOPLAY_SEED_CORPUS_VERSION || baseline.objective?.id !== POLICY_OBJECTIVE_ID || baseline.objective?.version !== POLICY_OBJECTIVE_VERSION) throw new Error('frozen autoplay baseline has a stale corpus or objective version')
  if (!baseline.heuristicProfile || baseline.heuristicProfile.version !== AUTOPLAY_HEURISTIC_PROFILE_VERSION) throw new Error('frozen autoplay baseline has an unsupported heuristic profile version')
  assertReview(baseline.review)
  assertProfiles(baseline.profiles)
  assertCampaignAutoplaySuite(baseline.suite)
  const developmentSeeds = campaignAutoplayEntries('development').map(entry => entry.seed)
  if (baseline.suite.partition !== 'development' || baseline.suite.seeds.length !== developmentSeeds.length || baseline.suite.seeds.some((seed, index) => seed !== developmentSeeds[index])) throw new Error('frozen autoplay baseline must cover the full development corpus')
  if (baseline.suite.corpusVersion !== baseline.corpusVersion || baseline.suite.profiles.some((profile, index) => profileKey(profile) !== profileKey(baseline.profiles[index]!))) throw new Error('frozen autoplay baseline suite metadata does not match its envelope')
  if (baseline.suite.runs.some(run => run.heuristicProfile?.id !== baseline.heuristicProfile.id || run.heuristicProfile?.version !== baseline.heuristicProfile.version)) throw new Error('frozen autoplay baseline run heuristic profile does not match its envelope')
  assertEvaluations(baseline.suite)
}

export const parseFrozenAutoplayBaseline = (raw: unknown): FrozenAutoplayBaseline => {
  const record = object(raw)
  if (!record) throw new Error('frozen autoplay baseline must be an object')
  const baseline = record as unknown as FrozenAutoplayBaseline
  assertFrozenAutoplayBaseline(baseline)
  return structuredClone(baseline)
}

export const createFrozenAutoplayBaseline = (suite: CampaignAutoplaySuite, heuristicProfile: AutoplayHeuristicProfileRef, review: FrozenAutoplayBaselineReview): FrozenAutoplayBaseline => {
  const baseline: FrozenAutoplayBaseline = {
    version: AUTOPLAY_FROZEN_BASELINE_VERSION,
    engineVersion: AUTOPLAY_ENGINE_VERSION,
    corpusVersion: AUTOPLAY_SEED_CORPUS_VERSION,
    objective: { id: POLICY_OBJECTIVE_ID, version: POLICY_OBJECTIVE_VERSION },
    heuristicProfile: { ...heuristicProfile },
    profiles: CAMPAIGN_AUTOPLAY_PROFILES.map(profile => ({ ...profile, policyProfileVersion: POLICY_PROFILE_VERSION })),
    review: { reviewedBy: review.reviewedBy.trim(), review: review.review.trim() },
    suite: structuredClone(suite)
  }
  assertFrozenAutoplayBaseline(baseline)
  return baseline
}

export const compareFrozenAutoplayBaseline = (candidate: CampaignAutoplaySuite, baseline: FrozenAutoplayBaseline, heuristicProfile: AutoplayHeuristicProfileRef): FrozenAutoplayBaselineComparison => {
  assertFrozenAutoplayBaseline(baseline)
  assertCampaignAutoplaySuite(candidate)
  if (candidate.partition !== 'development' || candidate.corpusVersion !== baseline.corpusVersion || JSON.stringify(candidate.seeds) !== JSON.stringify(baseline.suite.seeds)) throw new Error('candidate autoplay suite does not match frozen baseline corpus')
  if (heuristicProfile.id !== baseline.heuristicProfile.id || heuristicProfile.version !== baseline.heuristicProfile.version) throw new Error('candidate heuristic profile does not match frozen baseline')
  const baselineRuns = new Map(baseline.suite.runs.map(run => [`${run.seed}:${run.profile}`, run]))
  const evidence = candidate.runs.map(run => {
    const reference = baselineRuns.get(`${run.seed}:${run.profile}`)
    if (!reference?.evaluation || !run.evaluation) throw new Error(`frozen autoplay baseline is missing objective evidence for ${run.seed}/${run.profile}`)
    const comparison = comparePolicyScores(run.evaluation.policyMetadata.score, reference.evaluation.policyMetadata.score)
    return { seed: run.seed, profile: run.profile, baseline: policyScoreTuple(reference.evaluation.policyMetadata.score), candidate: policyScoreTuple(run.evaluation.policyMetadata.score), comparison: comparison < 0 ? 'better' : comparison > 0 ? 'regression' : 'equal' } satisfies FrozenAutoplayBaselineEvidence
  }).sort((left, right) => left.seed - right.seed || left.profile.localeCompare(right.profile))
  const regressions = evidence.filter(entry => entry.comparison === 'regression')
  return { passed: !regressions.length, runtimeTolerance: AUTOPLAY_BASELINE_RUNTIME_TOLERANCE, evidence, regressions }
}
