import type { AutoplayMode, AutoplayPolicy, RunTelemetry } from './types'

export const POLICY_PROFILE_VERSION = 1 as const
export const POLICY_OBJECTIVE_VERSION = 1 as const
export const POLICY_OBJECTIVE_ID = 'campaign-clears-deaths-stalls-exploration-resources' as const

export type PolicyInformationMode = Exclude<AutoplayMode, 'off'>
export type PolicyInputCapability = 'observation' | 'history' | 'hidden-map'
export type PolicyScoreTuple = readonly [campaignClears: number, deaths: number, stalls: number, explorationValue: number, resourceEfficiency: number]
export interface PolicyProfile { version: typeof POLICY_PROFILE_VERSION; id: string; policy: AutoplayPolicy; informationMode: PolicyInformationMode; requestedInputs: readonly PolicyInputCapability[]; objectiveVersion: typeof POLICY_OBJECTIVE_VERSION; objectiveId: typeof POLICY_OBJECTIVE_ID }
export interface PolicyProfileInput { policy: AutoplayPolicy; informationMode: PolicyInformationMode; requestedInputs?: readonly PolicyInputCapability[] }
export interface PolicyScore { campaignClears: number; deaths: number; stalls: number; explorationValue: number; resourceEfficiency: number }
export interface PolicyRunMetadata { profile: PolicyProfile; seed: number; turnBudget: number; score: PolicyScore; scoreTuple: PolicyScoreTuple }
export interface PolicyScoreInput { campaignComplete: boolean; outcome: 'complete' | 'dead' | 'stalled' | 'turn-limit' | 'unsupported' | 'error'; exploredTiles: number; metrics: RunTelemetry; retainedResources: number }
export interface RankedPolicyEvaluation { score: PolicyScore; seed: number; profile: Pick<PolicyProfile, 'id' | 'informationMode' | 'policy'> }

const inputsForMode: Record<PolicyInformationMode, readonly PolicyInputCapability[]> = {
  visible: ['observation', 'history'],
  omniscient: ['observation', 'history', 'hidden-map']
}

const compareNumber = (left: number, right: number): number => left === right ? 0 : left < right ? -1 : 1

export const assertPolicySeed = (seed: number): void => {
  if (!Number.isSafeInteger(seed) || seed < 0) throw new Error(`invalid autoplay policy seed: ${seed}`)
}

export const assertPolicyTurnBudget = (turnBudget: number): void => {
  if (!Number.isSafeInteger(turnBudget) || turnBudget < 1) throw new Error(`invalid autoplay policy turn budget: ${turnBudget}`)
}

export const createPolicyProfile = ({ policy, informationMode, requestedInputs = inputsForMode[informationMode] }: PolicyProfileInput): PolicyProfile => {
  const allowed = inputsForMode[informationMode]
  const inputs = [...new Set(requestedInputs)].sort()
  const unsupported = inputs.filter(input => !allowed.includes(input))
  if (unsupported.length) throw new Error(`policy profile ${informationMode}/${policy} requests unavailable inputs: ${unsupported.join(',')}`)
  return { version: POLICY_PROFILE_VERSION, id: `${informationMode}-${policy}`, policy, informationMode, requestedInputs: inputs, objectiveVersion: POLICY_OBJECTIVE_VERSION, objectiveId: POLICY_OBJECTIVE_ID }
}

export const policyScoreTuple = (score: PolicyScore): PolicyScoreTuple => [score.campaignClears, score.deaths, score.stalls, score.explorationValue, score.resourceEfficiency]

export const comparePolicyScores = (left: PolicyScore, right: PolicyScore): number => {
  const comparisons: Array<[number, number, 1 | -1]> = [
    [left.campaignClears, right.campaignClears, -1],
    [left.deaths, right.deaths, 1],
    [left.stalls, right.stalls, 1],
    [left.explorationValue, right.explorationValue, -1],
    [left.resourceEfficiency, right.resourceEfficiency, -1]
  ]
  for (const [leftValue, rightValue, direction] of comparisons) {
    const comparison = compareNumber(leftValue, rightValue)
    if (comparison) return comparison * direction
  }
  return 0
}

export const comparePolicyEvaluations = (left: RankedPolicyEvaluation, right: RankedPolicyEvaluation): number => {
  const scoreComparison = comparePolicyScores(left.score, right.score)
  if (scoreComparison) return scoreComparison
  const seedComparison = compareNumber(left.seed, right.seed)
  if (seedComparison) return seedComparison
  const profileComparison = left.profile.id.localeCompare(right.profile.id)
  if (profileComparison) return profileComparison
  const modeComparison = left.profile.informationMode.localeCompare(right.profile.informationMode)
  if (modeComparison) return modeComparison
  return left.profile.policy.localeCompare(right.profile.policy)
}

export const scorePolicyEpisode = ({ campaignComplete, outcome, exploredTiles, metrics, retainedResources }: PolicyScoreInput): PolicyScore => {
  if (!Number.isSafeInteger(exploredTiles) || exploredTiles < 0) throw new Error(`invalid autoplay exploration value: ${exploredTiles}`)
  if (!Number.isSafeInteger(retainedResources) || retainedResources < 0) throw new Error(`invalid autoplay retained resources: ${retainedResources}`)
  return {
    campaignClears: campaignComplete ? 1 : 0,
    deaths: outcome === 'dead' ? 1 : 0,
    stalls: outcome === 'stalled' || outcome === 'turn-limit' ? 1 : 0,
    explorationValue: exploredTiles + metrics.pickups + (metrics.secretValue ?? 0),
    resourceEfficiency: retainedResources - metrics.bombsUsed - metrics.ropesUsed
  }
}

export const createPolicyRunMetadata = (profile: PolicyProfile, seed: number, turnBudget: number, score: PolicyScore): PolicyRunMetadata => {
  assertPolicySeed(seed)
  assertPolicyTurnBudget(turnBudget)
  return { profile, seed, turnBudget, score, scoreTuple: policyScoreTuple(score) }
}
