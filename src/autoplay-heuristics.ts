import type { AutoplayPolicy } from './types'

export const AUTOPLAY_HEURISTIC_PROFILE_VERSION = 1 as const
export type AutoplayHeuristicProfileId = 'compatibility' | 'conservative'
export interface AutoplayHeuristicProfileRef { id: AutoplayHeuristicProfileId; version: typeof AUTOPLAY_HEURISTIC_PROFILE_VERSION }
export interface AutoplayHeuristicProfile extends AutoplayHeuristicProfileRef {
  resourceReserve: Record<AutoplayPolicy, number>
  merchantGoldReserve: Record<AutoplayPolicy, number>
  gateNpcPenalty: Record<AutoplayPolicy, number>
  gateBombPenalty: Record<AutoplayPolicy, number>
  boonRankWeight: number
  boonExploreFamily: { scouting: number; traversal: number; recovery: number; other: number }
  boonStandardFamily: { recovery: number; combat: number; other: number }
  skillPriority: Record<'clear' | 'survival' | 'other', Record<'strength' | 'vitality' | 'intellect' | 'other', number>>
  failedCommandPenalty: number
  fallbackMinimumScore: number
}
export type AutoplayHeuristicTerm = 'resourceReserve' | 'merchantGoldReserve' | 'gateNpcPenalty' | 'gateBombPenalty' | 'boonRankWeight' | 'boonExploreFamily' | 'boonStandardFamily' | 'skillPriority' | 'failedCommandPenalty' | 'fallbackMinimumScore'
export interface AutoplayHeuristicTermSpec { purpose: string; minimum: number; maximum: number; default: number }

export const AUTOPLAY_HEURISTIC_TERM_SPECS: Readonly<Record<AutoplayHeuristicTerm, AutoplayHeuristicTermSpec>> = {
  resourceReserve: { purpose: 'minimum bombs or ropes retained before discretionary use', minimum: 0, maximum: 9, default: 1 },
  merchantGoldReserve: { purpose: 'gold retained before a merchant purchase', minimum: 0, maximum: 500, default: 45 },
  gateNpcPenalty: { purpose: 'penalty for irreversible NPC gate alternatives', minimum: 0, maximum: 2_000, default: 45 },
  gateBombPenalty: { purpose: 'penalty for irreversible bomb gate alternatives', minimum: 0, maximum: 500, default: 20 },
  boonRankWeight: { purpose: 'weight for an already-owned boon rank', minimum: 0, maximum: 20, default: 3 },
  boonExploreFamily: { purpose: 'family priority used by explore boon selection', minimum: 0, maximum: 100, default: 18 },
  boonStandardFamily: { purpose: 'family priority used by non-explore boon selection', minimum: 0, maximum: 100, default: 18 },
  skillPriority: { purpose: 'discipline priority by policy group and stat', minimum: 0, maximum: 100, default: 8 },
  failedCommandPenalty: { purpose: 'score deducted for each failed repeated command', minimum: 0, maximum: 500, default: 60 },
  fallbackMinimumScore: { purpose: 'score below which executable movement recovery is used', minimum: -10_000, maximum: 0, default: -500 }
}

const compatibility: AutoplayHeuristicProfile = {
  version: AUTOPLAY_HEURISTIC_PROFILE_VERSION,
  id: 'compatibility',
  resourceReserve: { survival: 1, clear: 1, explore: 2, legacy: 2 },
  merchantGoldReserve: { survival: 20, clear: 0, explore: 45, legacy: 45 },
  gateNpcPenalty: { survival: 300, clear: 45, explore: 45, legacy: 1_000 },
  gateBombPenalty: { survival: 80, clear: 20, explore: 20, legacy: 180 },
  boonRankWeight: 3,
  boonExploreFamily: { scouting: 34, traversal: 28, recovery: 24, other: 18 },
  boonStandardFamily: { recovery: 26, combat: 22, other: 18 },
  skillPriority: {
    clear: { strength: 30, vitality: 25, intellect: 18, other: 8 },
    survival: { strength: 30, vitality: 24, intellect: 18, other: 8 },
    other: { strength: 24, vitality: 18, intellect: 30, other: 8 }
  },
  failedCommandPenalty: 60,
  fallbackMinimumScore: -500
}

const conservative: AutoplayHeuristicProfile = {
  ...compatibility,
  id: 'conservative',
  resourceReserve: { survival: 2, clear: 2, explore: 3, legacy: 3 },
  merchantGoldReserve: { survival: 30, clear: 0, explore: 55, legacy: 55 },
  gateNpcPenalty: { survival: 360, clear: 60, explore: 60, legacy: 1_200 },
  gateBombPenalty: { survival: 100, clear: 30, explore: 30, legacy: 220 },
  failedCommandPenalty: 75
}

export const autoplayHeuristicProfiles: Readonly<Record<AutoplayHeuristicProfileId, Readonly<AutoplayHeuristicProfile>>> = { compatibility, conservative }

const policies: readonly AutoplayPolicy[] = ['survival', 'clear', 'explore', 'legacy']
const skillGroups = ['clear', 'survival', 'other'] as const
const stats = ['strength', 'vitality', 'intellect', 'other'] as const
const number = (value: unknown, term: AutoplayHeuristicTerm): number => {
  const spec = AUTOPLAY_HEURISTIC_TERM_SPECS[term]
  if (typeof value !== 'number' || !Number.isFinite(value) || value < spec.minimum || value > spec.maximum) throw new Error(`invalid autoplay heuristic ${term}: expected ${spec.minimum}..${spec.maximum}`)
  return value
}
const policyValues = (value: unknown, term: AutoplayHeuristicTerm): Record<AutoplayPolicy, number> => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error(`invalid autoplay heuristic ${term}: expected policy record`)
  const record = value as Record<string, unknown>
  if (Object.keys(record).length !== policies.length || policies.some(policy => !(policy in record))) throw new Error(`invalid autoplay heuristic ${term}: expected survival,clear,explore,legacy`)
  return Object.fromEntries(policies.map(policy => [policy, number(record[policy], term)])) as Record<AutoplayPolicy, number>
}
const namedValues = <T extends string>(value: unknown, names: readonly T[], term: AutoplayHeuristicTerm): Record<T, number> => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error(`invalid autoplay heuristic ${term}: expected named record`)
  const record = value as Record<string, unknown>
  if (Object.keys(record).length !== names.length || names.some(name => !(name in record))) throw new Error(`invalid autoplay heuristic ${term}: missing named value`)
  return Object.fromEntries(names.map(name => [name, number(record[name], term)])) as Record<T, number>
}

export const parseAutoplayHeuristicProfile = (value: unknown): AutoplayHeuristicProfile => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('invalid autoplay heuristic profile: expected object')
  const profile = value as Record<string, unknown>
  const keys = ['version', 'id', 'resourceReserve', 'merchantGoldReserve', 'gateNpcPenalty', 'gateBombPenalty', 'boonRankWeight', 'boonExploreFamily', 'boonStandardFamily', 'skillPriority', 'failedCommandPenalty', 'fallbackMinimumScore']
  if (Object.keys(profile).length !== keys.length || keys.some(key => !(key in profile))) throw new Error('invalid autoplay heuristic profile fields')
  if (profile.version !== AUTOPLAY_HEURISTIC_PROFILE_VERSION) throw new Error(`invalid autoplay heuristic profile version: ${String(profile.version)}`)
  if (typeof profile.id !== 'string' || !['compatibility', 'conservative'].includes(profile.id)) throw new Error(`invalid autoplay heuristic profile id: ${String(profile.id)}`)
  if (!profile.skillPriority || typeof profile.skillPriority !== 'object' || Array.isArray(profile.skillPriority)) throw new Error('invalid autoplay heuristic skillPriority: expected named record')
  const rawSkillPriority = profile.skillPriority as Record<string, unknown>
  if (Object.keys(rawSkillPriority).length !== skillGroups.length || skillGroups.some(group => !(group in rawSkillPriority))) throw new Error('invalid autoplay heuristic skillPriority: missing policy group')
  const parsed: AutoplayHeuristicProfile = {
    version: AUTOPLAY_HEURISTIC_PROFILE_VERSION,
    id: profile.id as AutoplayHeuristicProfileId,
    resourceReserve: policyValues(profile.resourceReserve, 'resourceReserve'),
    merchantGoldReserve: policyValues(profile.merchantGoldReserve, 'merchantGoldReserve'),
    gateNpcPenalty: policyValues(profile.gateNpcPenalty, 'gateNpcPenalty'),
    gateBombPenalty: policyValues(profile.gateBombPenalty, 'gateBombPenalty'),
    boonRankWeight: number(profile.boonRankWeight, 'boonRankWeight'),
    boonExploreFamily: namedValues(profile.boonExploreFamily, ['scouting', 'traversal', 'recovery', 'other'], 'boonExploreFamily'),
    boonStandardFamily: namedValues(profile.boonStandardFamily, ['recovery', 'combat', 'other'], 'boonStandardFamily'),
    skillPriority: Object.fromEntries(skillGroups.map(group => [group, namedValues(rawSkillPriority[group], stats, 'skillPriority')])) as AutoplayHeuristicProfile['skillPriority'],
    failedCommandPenalty: number(profile.failedCommandPenalty, 'failedCommandPenalty'),
    fallbackMinimumScore: number(profile.fallbackMinimumScore, 'fallbackMinimumScore')
  }
  if (JSON.stringify(parsed) !== JSON.stringify(autoplayHeuristicProfiles[parsed.id])) throw new Error(`invalid autoplay heuristic profile ${parsed.id}: named profiles cannot be modified`)
  return parsed
}

for (const profile of Object.values(autoplayHeuristicProfiles)) parseAutoplayHeuristicProfile(profile)

export const autoplayHeuristicProfile = (id: string | undefined = 'compatibility'): AutoplayHeuristicProfile => {
  const profile = autoplayHeuristicProfiles[id as AutoplayHeuristicProfileId]
  if (!profile) throw new Error(`unknown autoplay heuristic profile: ${id}; expected ${Object.keys(autoplayHeuristicProfiles).join(',')}`)
  return structuredClone(profile)
}

export const autoplayHeuristicProfileRef = (profile: AutoplayHeuristicProfile): AutoplayHeuristicProfileRef => ({ id: profile.id, version: profile.version })
