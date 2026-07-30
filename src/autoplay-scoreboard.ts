import { comparePolicyScores, policyScoreTuple, type PolicyScore, type PolicyScoreTuple } from './autoplay-policy'
import { assertAutoplayTraceDocument } from './autoplay-trace'
import type { AutoplayMode, AutoplayOptionalOutcomes, AutoplayPolicy } from './types'
import type { CampaignAutoplayProfile, CampaignAutoplayProfileId, CampaignAutoplayRun } from './autoplay-campaign'
import type { AutoplaySeedCorpusPartition } from './autoplay-seed-corpus'

export const AUTOPLAY_SCOREBOARD_VERSION = 1 as const
export type AutoplayScoreboardEpisodeStatus = 'complete' | 'failed' | 'incomplete' | 'missing'
export interface AutoplayScoreboardEpisode { seed: number; profile: CampaignAutoplayProfileId; policy: AutoplayPolicy; informationMode: Exclude<AutoplayMode, 'off'>; status: AutoplayScoreboardEpisodeStatus; outcome?: CampaignAutoplayRun['outcome']; turns?: number; score?: PolicyScore; scoreTuple?: PolicyScoreTuple; explorationValue?: number; resourcesSpent?: number; resourcesRetained?: number; optionalOutcomes?: AutoplayOptionalOutcomes; traceHash?: string }
export interface AutoplayScoreboardAggregate { expected: number; observed: number; missing: number; incomplete: number; failed: number; clearCount: number; clearRate: number; deaths: number; stalls: number; turns: number; explorationValue: number; resourcesSpent: number; resourcesRetained: number; optionalOutcomes: AutoplayOptionalOutcomes; score: PolicyScore; scoreTuple: PolicyScoreTuple }
export interface AutoplayScoreboardProfileAggregate extends AutoplayScoreboardAggregate { profile: CampaignAutoplayProfileId; policy: AutoplayPolicy; informationMode: Exclude<AutoplayMode, 'off'> }
export interface AutoplayScoreboardPolicyAggregate extends AutoplayScoreboardAggregate { policy: AutoplayPolicy }
export interface AutoplayScoreboardModeAggregate extends AutoplayScoreboardAggregate { informationMode: Exclude<AutoplayMode, 'off'> }
export interface AutoplayScoreboardSeedAggregate extends AutoplayScoreboardAggregate { seed: number }
export interface AutoplayScoreboardWinner { profile: CampaignAutoplayProfileId; policy: AutoplayPolicy; informationMode: Exclude<AutoplayMode, 'off'>; score: PolicyScore; scoreTuple: PolicyScoreTuple }
export interface AutoplayScoreboard { version: typeof AUTOPLAY_SCOREBOARD_VERSION; partition: AutoplaySeedCorpusPartition; episodes: AutoplayScoreboardEpisode[]; overall: AutoplayScoreboardAggregate; byProfile: AutoplayScoreboardProfileAggregate[]; byPolicy: AutoplayScoreboardPolicyAggregate[]; byInformationMode: AutoplayScoreboardModeAggregate[]; bySeed: AutoplayScoreboardSeedAggregate[]; winner?: AutoplayScoreboardWinner }
export interface AutoplayScoreboardInput { partition: AutoplaySeedCorpusPartition; seeds: readonly number[]; profiles: readonly CampaignAutoplayProfile[]; runs: readonly CampaignAutoplayRun[] }

const scoreZero = (): PolicyScore => ({ campaignClears: 0, deaths: 0, stalls: 0, explorationValue: 0, resourceEfficiency: 0 })
const addScore = (total: PolicyScore, score: PolicyScore): PolicyScore => ({ campaignClears: total.campaignClears + score.campaignClears, deaths: total.deaths + score.deaths, stalls: total.stalls + score.stalls, explorationValue: total.explorationValue + score.explorationValue, resourceEfficiency: total.resourceEfficiency + score.resourceEfficiency })
const optionalZero = (): AutoplayOptionalOutcomes => ({ pursued: 0, deferred: 0, declined: 0, secrets: 0, shortcuts: 0 })
const addOptional = (total: AutoplayOptionalOutcomes, outcomes: AutoplayOptionalOutcomes): AutoplayOptionalOutcomes => ({ pursued: total.pursued + outcomes.pursued, deferred: total.deferred + outcomes.deferred, declined: total.declined + outcomes.declined, secrets: total.secrets + outcomes.secrets, shortcuts: total.shortcuts + outcomes.shortcuts })
const compareText = (left: string, right: string): number => left.localeCompare(right)

const assertTrace = (run: CampaignAutoplayRun): void => {
  const traceDocument = run.traceDocument ?? run.failure?.traceDocument
  if (!traceDocument) return
  assertAutoplayTraceDocument(traceDocument)
  if (traceDocument.episode.seed !== run.seed || traceDocument.episode.policy !== run.policy || traceDocument.episode.informationMode !== run.mode || traceDocument.terminal.outcome !== run.outcome || traceDocument.terminal.turns !== run.turns) throw new Error('autoplay scoreboard trace does not match its episode')
  if (run.evaluation?.traceHash !== traceDocument.hash) throw new Error('autoplay scoreboard trace hash does not match its evaluation')
}

const episode = (seed: number, profile: CampaignAutoplayProfile, run?: CampaignAutoplayRun): AutoplayScoreboardEpisode => {
  if (!run) return { seed, profile: profile.id, policy: profile.policy, informationMode: profile.mode, status: 'missing' }
  if (run.seed !== seed || run.profile !== profile.id || run.policy !== profile.policy || run.mode !== profile.mode) throw new Error('autoplay scoreboard run does not match its expected episode')
  assertTrace(run)
  const evaluation = run.evaluation
  if (!evaluation) return { seed, profile: profile.id, policy: profile.policy, informationMode: profile.mode, status: 'incomplete', outcome: run.outcome, turns: run.turns }
  const score = structuredClone(evaluation.policyMetadata.score)
  const scoreTuple = policyScoreTuple(score)
  if (evaluation.explorationValue !== score.explorationValue || evaluation.policyMetadata.scoreTuple.some((value, index) => value !== scoreTuple[index])) throw new Error('autoplay scoreboard evaluation score tuple is inconsistent')
  if (score.campaignClears !== (run.campaignComplete ? 1 : 0) || score.deaths !== (run.outcome === 'dead' ? 1 : 0) || score.stalls !== (run.outcome === 'stalled' || run.outcome === 'turn-limit' ? 1 : 0)) throw new Error('autoplay scoreboard evaluation score does not match its run')
  return { seed, profile: profile.id, policy: profile.policy, informationMode: profile.mode, status: run.campaignComplete ? 'complete' : 'failed', outcome: run.outcome, turns: run.turns, score, scoreTuple, explorationValue: evaluation.explorationValue, resourcesSpent: evaluation.resourcesSpent, resourcesRetained: evaluation.resourcesRetained, optionalOutcomes: { ...evaluation.optionalOutcomes }, ...(evaluation.traceHash ? { traceHash: evaluation.traceHash } : {}) }
}

const aggregate = (episodes: readonly AutoplayScoreboardEpisode[]): AutoplayScoreboardAggregate => {
  const observed = episodes.filter(entry => entry.score)
  const score = observed.reduce<PolicyScore>((total, entry) => addScore(total, entry.score!), scoreZero())
  const optionalOutcomes = observed.reduce<AutoplayOptionalOutcomes>((total, entry) => addOptional(total, entry.optionalOutcomes ?? optionalZero()), optionalZero())
  const clearCount = observed.reduce((total, entry) => total + entry.score!.campaignClears, 0)
  return {
    expected: episodes.length,
    observed: observed.length,
    missing: episodes.filter(entry => entry.status === 'missing').length,
    incomplete: episodes.filter(entry => entry.status === 'incomplete').length,
    failed: episodes.filter(entry => entry.status === 'failed').length,
    clearCount,
    clearRate: episodes.length ? clearCount / episodes.length : 0,
    deaths: score.deaths,
    stalls: score.stalls,
    turns: observed.reduce((total, entry) => total + entry.turns!, 0),
    explorationValue: score.explorationValue,
    resourcesSpent: observed.reduce((total, entry) => total + entry.resourcesSpent!, 0),
    resourcesRetained: observed.reduce((total, entry) => total + entry.resourcesRetained!, 0),
    optionalOutcomes,
    score,
    scoreTuple: policyScoreTuple(score)
  }
}

const unique = <T>(values: readonly T[]): T[] => [...new Set(values)]

export const createAutoplayScoreboard = ({ partition, seeds, profiles, runs }: AutoplayScoreboardInput): AutoplayScoreboard => {
  const uniqueSeeds = unique(seeds).sort((left, right) => left - right)
  if (uniqueSeeds.length !== seeds.length || uniqueSeeds.some(seed => !Number.isSafeInteger(seed) || seed < 0)) throw new Error('autoplay scoreboard seeds must be unique non-negative integers')
  const sortedProfiles = [...profiles].sort((left, right) => compareText(left.id, right.id))
  if (unique(sortedProfiles.map(profile => profile.id)).length !== sortedProfiles.length) throw new Error('autoplay scoreboard profiles must be unique')
  const expected = new Map(uniqueSeeds.flatMap(seed => sortedProfiles.map(profile => [`${seed}:${profile.id}`, { seed, profile }])))
  const actual = new Map<string, CampaignAutoplayRun>()
  for (const run of runs) {
    const key = `${run.seed}:${run.profile}`
    if (!expected.has(key) || actual.has(key)) throw new Error('autoplay scoreboard has an unexpected or duplicate run')
    actual.set(key, run)
  }
  const episodes = [...expected.values()].map(({ seed, profile }) => episode(seed, profile, actual.get(`${seed}:${profile.id}`))).sort((left, right) => left.seed - right.seed || compareText(left.profile, right.profile))
  const byProfile = sortedProfiles.map(profile => ({ profile: profile.id, policy: profile.policy, informationMode: profile.mode, ...aggregate(episodes.filter(entry => entry.profile === profile.id)) }))
  const byPolicy = unique(sortedProfiles.map(profile => profile.policy)).sort(compareText).map(policy => ({ policy, ...aggregate(episodes.filter(entry => entry.policy === policy)) }))
  const byInformationMode = unique(sortedProfiles.map(profile => profile.mode)).sort(compareText).map(informationMode => ({ informationMode, ...aggregate(episodes.filter(entry => entry.informationMode === informationMode)) }))
  const bySeed = uniqueSeeds.map(seed => ({ seed, ...aggregate(episodes.filter(entry => entry.seed === seed)) }))
  const eligible = byProfile.every(entry => entry.missing === 0 && entry.incomplete === 0) ? [...byProfile].sort((left, right) => comparePolicyScores(left.score, right.score) || compareText(left.profile, right.profile)) : []
  const leader = eligible[0]
  const runnerUp = eligible[1]
  return {
    version: AUTOPLAY_SCOREBOARD_VERSION,
    partition,
    episodes,
    overall: aggregate(episodes),
    byProfile,
    byPolicy,
    byInformationMode,
    bySeed,
    ...(leader && (!runnerUp || comparePolicyScores(leader.score, runnerUp.score) !== 0) ? { winner: { profile: leader.profile, policy: leader.policy, informationMode: leader.informationMode, score: leader.score, scoreTuple: leader.scoreTuple } } : {})
  }
}

export const assertAutoplayScoreboard = (scoreboard: AutoplayScoreboard, input: AutoplayScoreboardInput): void => {
  if (scoreboard.version !== AUTOPLAY_SCOREBOARD_VERSION) throw new Error('autoplay scoreboard has an unsupported version')
  const expected = createAutoplayScoreboard(input)
  if (JSON.stringify(scoreboard) !== JSON.stringify(expected)) throw new Error('autoplay scoreboard does not match campaign runs')
}

export const formatAutoplayScoreboard = (scoreboard: AutoplayScoreboard): string => [
  `partition=${scoreboard.partition} clear=${scoreboard.overall.clearCount}/${scoreboard.overall.expected} missing=${scoreboard.overall.missing} incomplete=${scoreboard.overall.incomplete} failed=${scoreboard.overall.failed}`,
  ...scoreboard.byProfile.map(entry => `profile=${entry.profile} clear=${entry.clearCount}/${entry.expected} deaths=${entry.deaths} stalls=${entry.stalls} turns=${entry.turns} exploration=${entry.explorationValue} optional=${entry.optionalOutcomes.pursued}/${entry.optionalOutcomes.deferred}/${entry.optionalOutcomes.declined} spent=${entry.resourcesSpent} retained=${entry.resourcesRetained} score=${entry.scoreTuple.join(',')}`),
  `winner=${scoreboard.winner?.profile ?? 'none'}`
].join('\n')
