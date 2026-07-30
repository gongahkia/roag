import { runAutoplay, type AutoplayOutcome, type AutoplayReport } from './autoplay-runner'
import { policyScoreTuple, type PolicyRunMetadata, type PolicyScore, type PolicyScoreTuple } from './autoplay-policy'
import { autoplayHeuristicProfile, type AutoplayHeuristicProfile, type AutoplayHeuristicProfileRef } from './autoplay-heuristics'
import { AUTOPLAY_SEED_CORPUS_VERSION, autoplaySeedCorpusPartition, type AutoplaySeedCorpusEntry, type AutoplaySeedCorpusPartition } from './autoplay-seed-corpus'
import { assertAutoplayTraceDocument, type AutoplayTraceDocument } from './autoplay-trace'
import { assertAutoplayScoreboard, createAutoplayScoreboard, type AutoplayScoreboard } from './autoplay-scoreboard'
import { newSeededCampaignRun } from './engine'
import { isCampaignAreaOrder } from './engine/campaign'
import type { AutoplayMode, AutoplayPolicy, AutoplayReplayMetadata, AutoplayTraceEntry, Biome } from './types'

export const CAMPAIGN_AUTOPLAY_SEEDS: readonly number[] = autoplaySeedCorpusPartition('development').map(entry => entry.seed)
export const CAMPAIGN_AUTOPLAY_TURN_LIMIT = autoplaySeedCorpusPartition('development')[0]!.turnBudget
export const CAMPAIGN_AUTOPLAY_PROFILES = [
  { id: 'omniscient-clear', mode: 'omniscient', policy: 'clear' },
  { id: 'visible-explore', mode: 'visible', policy: 'explore' }
] as const

export type CampaignAutoplayProfile = typeof CAMPAIGN_AUTOPLAY_PROFILES[number]
export type CampaignAutoplayProfileId = CampaignAutoplayProfile['id']
export interface CampaignAutoplayFailure { outcome: AutoplayOutcome; finalBiome: Biome; floor: number; completedAreas: Biome[]; replay: AutoplayReplayMetadata; reason?: string; trace: Array<Pick<AutoplayTraceEntry, 'turn' | 'replay' | 'command' | 'reason' | 'events'>>; traceDocument?: AutoplayTraceDocument }
export interface CampaignAutoplayEvaluation { policyMetadata: PolicyRunMetadata; explorationValue: number; resourcesSpent: number; resourcesRetained: number; traceHash?: string }
export interface CampaignAutoplayRun { seed: number; profile: CampaignAutoplayProfileId; mode: Exclude<AutoplayMode, 'off'>; policy: AutoplayPolicy; heuristicProfile?: AutoplayHeuristicProfileRef; areaOrder: Biome[]; campaignComplete: boolean; outcome: AutoplayOutcome; turns: number; finalBiome: Biome; floor: number; completedAreas: Biome[]; evaluation?: CampaignAutoplayEvaluation; traceDocument?: AutoplayTraceDocument; failure?: CampaignAutoplayFailure }
export interface CampaignAutoplayRate { total: number; completed: number; failed: number; failureRate: number }
export interface CampaignAutoplaySummary extends CampaignAutoplayRate { byProfile: Record<CampaignAutoplayProfileId, CampaignAutoplayRate> }
export interface CampaignAutoplaySuite { version: 4; corpusVersion: typeof AUTOPLAY_SEED_CORPUS_VERSION; partition: AutoplaySeedCorpusPartition; seeds: number[]; turnLimit: number; profiles: CampaignAutoplayProfile[]; runs: CampaignAutoplayRun[]; summary: CampaignAutoplaySummary; scoreboard: AutoplayScoreboard }
export interface CampaignAutoplayDelta { overall: number; byProfile: Record<CampaignAutoplayProfileId, number> }
export interface CampaignAutoplaySuiteOptions { partition?: AutoplaySeedCorpusPartition; captureTrace?: boolean; heuristicProfile?: AutoplayHeuristicProfile; onRun?: (run: CampaignAutoplayRun, completed: number, total: number) => void }

export const campaignAutoplayEntries = (partition: AutoplaySeedCorpusPartition = 'development'): AutoplaySeedCorpusEntry[] => autoplaySeedCorpusPartition(partition)

export const campaignAutoplayRunMatchesCorpus = (entry: AutoplaySeedCorpusEntry, run: CampaignAutoplayRun): boolean => {
  const profile = CAMPAIGN_AUTOPLAY_PROFILES.find(candidate => candidate.id === run.profile)
  const expectedModes = new Set(entry.policyModes)
  return Boolean(profile && run.seed === entry.seed && run.mode === profile.mode && run.policy === profile.policy && expectedModes.has(run.mode) && run.outcome !== 'error' && run.areaOrder.length === entry.routeConfiguration.areaOrder.length && run.areaOrder.every((biome, index) => biome === entry.routeConfiguration.areaOrder[index]))
}

const assertCampaignAutoplayEvaluation = (run: CampaignAutoplayRun): void => {
  const evaluation = run.evaluation
  if (!evaluation) return
  const { policyMetadata, explorationValue, resourcesSpent, resourcesRetained } = evaluation
  const score = policyMetadata.score
  const integer = (value: number) => Number.isSafeInteger(value) && value >= 0
  if (policyMetadata.seed !== run.seed || policyMetadata.profile.id !== `${run.mode}-${run.policy}` || policyMetadata.profile.informationMode !== run.mode || policyMetadata.profile.policy !== run.policy) throw new Error('campaign autoplay evaluation policy metadata does not match its run')
  if (!integer(explorationValue) || !integer(resourcesSpent) || !integer(resourcesRetained) || !integer(score.campaignClears) || !integer(score.deaths) || !integer(score.stalls) || !integer(score.explorationValue) || !Number.isSafeInteger(score.resourceEfficiency)) throw new Error('campaign autoplay evaluation has invalid metrics')
  if (explorationValue !== score.explorationValue || policyMetadata.scoreTuple.some((value, index) => value !== policyScoreTuple(score)[index])) throw new Error('campaign autoplay evaluation score tuple is inconsistent')
  if (score.campaignClears !== (run.campaignComplete ? 1 : 0) || score.deaths !== (run.outcome === 'dead' ? 1 : 0) || score.stalls !== (run.outcome === 'stalled' || run.outcome === 'turn-limit' ? 1 : 0)) throw new Error('campaign autoplay evaluation score does not match its run')
  const traceDocument = run.traceDocument ?? run.failure?.traceDocument
  if (!traceDocument) return
  assertAutoplayTraceDocument(traceDocument)
  if (evaluation.traceHash !== traceDocument.hash || traceDocument.episode.seed !== run.seed || traceDocument.episode.policy !== run.policy || traceDocument.episode.informationMode !== run.mode || traceDocument.terminal.outcome !== run.outcome || traceDocument.terminal.turns !== run.turns) throw new Error('campaign autoplay evaluation trace does not match its run')
}

export const assertCampaignAutoplaySuite = (suite: CampaignAutoplaySuite): void => {
  const entries = campaignAutoplayEntries(suite.partition)
  const expected = new Set(entries.flatMap(entry => CAMPAIGN_AUTOPLAY_PROFILES.map(profile => `${entry.seed}:${profile.id}`)))
  const actual = new Set(suite.runs.map(run => `${run.seed}:${run.profile}`))
  if (suite.runs.length !== expected.size || actual.size !== expected.size || [...actual].some(key => !expected.has(key))) throw new Error('campaign autoplay suite has missing or duplicate seed/profile runs')
  if (suite.version !== 4 || suite.corpusVersion !== AUTOPLAY_SEED_CORPUS_VERSION) throw new Error('campaign autoplay suite has an unsupported corpus version')
  if (suite.summary.total !== suite.runs.length || suite.summary.completed + suite.summary.failed !== suite.runs.length) throw new Error('campaign autoplay suite summary is inconsistent')
  if (suite.runs.some(run => !isCampaignAreaOrder(run.areaOrder))) throw new Error('campaign autoplay suite has an invalid area order')
  const bySeed = new Map(entries.map(entry => [entry.seed, entry]))
  if (suite.runs.some(run => !campaignAutoplayRunMatchesCorpus(bySeed.get(run.seed)!, run))) throw new Error('campaign autoplay suite has a corpus validation failure')
  suite.runs.forEach(assertCampaignAutoplayEvaluation)
  assertAutoplayScoreboard(suite.scoreboard, suite)
}

const rate = (runs: readonly CampaignAutoplayRun[]): CampaignAutoplayRate => {
  const completed = runs.filter(run => run.campaignComplete).length
  return { total: runs.length, completed, failed: runs.length - completed, failureRate: runs.length ? (runs.length - completed) / runs.length : 0 }
}

export const summarizeCampaignAutoplay = (runs: readonly CampaignAutoplayRun[]): CampaignAutoplaySummary => {
  const byProfile = Object.fromEntries(CAMPAIGN_AUTOPLAY_PROFILES.map(profile => [profile.id, rate(runs.filter(run => run.profile === profile.id))])) as Record<CampaignAutoplayProfileId, CampaignAutoplayRate>
  return { ...rate(runs), byProfile }
}

const failure = (report: AutoplayReport): CampaignAutoplayFailure => ({
  outcome: report.outcome,
  finalBiome: report.finalBiome,
  floor: report.floor,
  completedAreas: [...report.completedAreas],
  replay: { ...report.replay },
  ...(report.stall?.lastReason ? { reason: report.stall.lastReason } : report.error ? { reason: report.error } : {}),
  trace: report.trace.slice(-24).map(({ turn, replay, command, reason, events }) => ({ turn, replay: { ...replay }, command, reason, events: [...events] })),
  ...(report.traceDocument ? { traceDocument: structuredClone(report.traceDocument) } : {})
})

const evaluation = (report: AutoplayReport): CampaignAutoplayEvaluation => {
  const score: PolicyScore = structuredClone(report.policyMetadata.score)
  const scoreTuple: PolicyScoreTuple = policyScoreTuple(score)
  const policyMetadata: PolicyRunMetadata = { ...structuredClone(report.policyMetadata), score, scoreTuple }
  return {
    policyMetadata,
    explorationValue: score.explorationValue,
    resourcesSpent: report.metrics.bombsUsed + report.metrics.ropesUsed,
    resourcesRetained: report.final.hero.bombs + report.final.hero.ropes + report.final.hero.keys,
    ...(report.traceDocument ? { traceHash: report.traceDocument.hash } : {})
  }
}

export const compactCampaignAutoplayRun = (seed: number, profile: CampaignAutoplayProfile, report: AutoplayReport): CampaignAutoplayRun => ({
  seed,
  profile: profile.id,
  mode: profile.mode,
  policy: profile.policy,
  heuristicProfile: { ...report.heuristicProfile },
  areaOrder: [...report.areaOrder],
  campaignComplete: report.campaignComplete,
  outcome: report.outcome,
  turns: report.turns,
  finalBiome: report.finalBiome,
  floor: report.floor,
  completedAreas: [...report.completedAreas],
  evaluation: evaluation(report),
  ...(report.traceDocument ? { traceDocument: structuredClone(report.traceDocument) } : {}),
  ...(!report.campaignComplete ? { failure: failure(report) } : {})
})

export const campaignAutoplaySuite = (runs: CampaignAutoplayRun[], partition: AutoplaySeedCorpusPartition = 'development'): CampaignAutoplaySuite => {
  const entries = campaignAutoplayEntries(partition)
  const suite = {
    version: 4 as const,
    corpusVersion: AUTOPLAY_SEED_CORPUS_VERSION,
    partition,
    seeds: entries.map(entry => entry.seed),
    turnLimit: entries[0]!.turnBudget,
    profiles: CAMPAIGN_AUTOPLAY_PROFILES.map(profile => ({ ...profile })),
    runs,
    summary: summarizeCampaignAutoplay(runs),
    scoreboard: undefined as unknown as AutoplayScoreboard
  }
  suite.scoreboard = createAutoplayScoreboard(suite)
  assertCampaignAutoplaySuite(suite)
  return suite
}

export const runCampaignAutoplaySuite = (options: CampaignAutoplaySuiteOptions = {}): CampaignAutoplaySuite => {
  const partition = options.partition ?? 'development'
  const heuristicProfile = options.heuristicProfile ?? autoplayHeuristicProfile()
  const entries = campaignAutoplayEntries(partition)
  const runs: CampaignAutoplayRun[] = []
  const total = entries.length * CAMPAIGN_AUTOPLAY_PROFILES.length
  for (const entry of entries) for (const profile of CAMPAIGN_AUTOPLAY_PROFILES) {
    const report = runAutoplay(newSeededCampaignRun(entry.seed), { mode: profile.mode, policy: profile.policy, heuristicProfile, turnLimit: entry.turnBudget, captureTrace: true, traceLimit: options.captureTrace ? undefined : 24 })
    const current = compactCampaignAutoplayRun(entry.seed, profile, report)
    if (!campaignAutoplayRunMatchesCorpus(entry, current)) throw new Error(`campaign autoplay corpus validation failed for ${entry.seed}/${profile.id}`)
    runs.push(current)
    options.onRun?.(current, runs.length, total)
  }
  return campaignAutoplaySuite(runs, partition)
}

export const campaignAutoplayDelta = (current: CampaignAutoplaySuite, baseline: CampaignAutoplaySuite): CampaignAutoplayDelta => ({
  overall: current.summary.failureRate - baseline.summary.failureRate,
  byProfile: Object.fromEntries(CAMPAIGN_AUTOPLAY_PROFILES.map(profile => [profile.id, current.summary.byProfile[profile.id].failureRate - baseline.summary.byProfile[profile.id].failureRate])) as Record<CampaignAutoplayProfileId, number>
})
