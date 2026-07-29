import { runAutoplay, type AutoplayOutcome, type AutoplayReport } from './autoplay-runner'
import { newSeededCampaignRun } from './engine'
import { isCampaignAreaOrder } from './engine/campaign'
import type { AutoplayMode, AutoplayPolicy, AutoplayReplayMetadata, AutoplayTraceEntry, Biome } from './types'

export const CAMPAIGN_AUTOPLAY_SEEDS: readonly number[] = [7, 42, 99, 123, 256, 512, 999, 1337, 4096, 77123, 11, 17, 23, 29, 31, 37, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211]
export const CAMPAIGN_AUTOPLAY_TURN_LIMIT = 38_400
export const CAMPAIGN_AUTOPLAY_PROFILES = [
  { id: 'omniscient-clear', mode: 'omniscient', policy: 'clear' },
  { id: 'visible-explore', mode: 'visible', policy: 'explore' }
] as const

export type CampaignAutoplayProfile = typeof CAMPAIGN_AUTOPLAY_PROFILES[number]
export type CampaignAutoplayProfileId = CampaignAutoplayProfile['id']
export interface CampaignAutoplayFailure { outcome: AutoplayOutcome; finalBiome: Biome; floor: number; completedAreas: Biome[]; replay: AutoplayReplayMetadata; reason?: string; trace: Array<Pick<AutoplayTraceEntry, 'turn' | 'replay' | 'command' | 'reason' | 'events'>> }
export interface CampaignAutoplayRun { seed: number; profile: CampaignAutoplayProfileId; mode: Exclude<AutoplayMode, 'off'>; policy: AutoplayPolicy; areaOrder: Biome[]; campaignComplete: boolean; outcome: AutoplayOutcome; turns: number; finalBiome: Biome; floor: number; completedAreas: Biome[]; failure?: CampaignAutoplayFailure }
export interface CampaignAutoplayRate { total: number; completed: number; failed: number; failureRate: number }
export interface CampaignAutoplaySummary extends CampaignAutoplayRate { byProfile: Record<CampaignAutoplayProfileId, CampaignAutoplayRate> }
export interface CampaignAutoplaySuite { version: 2; seeds: number[]; turnLimit: number; profiles: CampaignAutoplayProfile[]; runs: CampaignAutoplayRun[]; summary: CampaignAutoplaySummary }
export interface CampaignAutoplayDelta { overall: number; byProfile: Record<CampaignAutoplayProfileId, number> }
export interface CampaignAutoplaySuiteOptions { captureTrace?: boolean; onRun?: (run: CampaignAutoplayRun, completed: number, total: number) => void }

export const assertCampaignAutoplaySuite = (suite: CampaignAutoplaySuite): void => {
  const expected = new Set(CAMPAIGN_AUTOPLAY_SEEDS.flatMap(seed => CAMPAIGN_AUTOPLAY_PROFILES.map(profile => `${seed}:${profile.id}`)))
  const actual = new Set(suite.runs.map(run => `${run.seed}:${run.profile}`))
  if (suite.runs.length !== expected.size || actual.size !== expected.size || [...actual].some(key => !expected.has(key))) throw new Error('campaign autoplay suite has missing or duplicate seed/profile runs')
  if (suite.summary.total !== suite.runs.length || suite.summary.completed + suite.summary.failed !== suite.runs.length) throw new Error('campaign autoplay suite summary is inconsistent')
  if (suite.runs.some(run => !isCampaignAreaOrder(run.areaOrder))) throw new Error('campaign autoplay suite has an invalid area order')
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
  trace: report.trace.slice(-24).map(({ turn, replay, command, reason, events }) => ({ turn, replay: { ...replay }, command, reason, events: [...events] }))
})

export const compactCampaignAutoplayRun = (seed: number, profile: CampaignAutoplayProfile, report: AutoplayReport): CampaignAutoplayRun => ({
  seed,
  profile: profile.id,
  mode: profile.mode,
  policy: profile.policy,
  areaOrder: [...report.areaOrder],
  campaignComplete: report.campaignComplete,
  outcome: report.outcome,
  turns: report.turns,
  finalBiome: report.finalBiome,
  floor: report.floor,
  completedAreas: [...report.completedAreas],
  ...(!report.campaignComplete ? { failure: failure(report) } : {})
})

export const campaignAutoplaySuite = (runs: CampaignAutoplayRun[]): CampaignAutoplaySuite => {
  const suite = {
    version: 2 as const,
    seeds: [...CAMPAIGN_AUTOPLAY_SEEDS],
    turnLimit: CAMPAIGN_AUTOPLAY_TURN_LIMIT,
    profiles: CAMPAIGN_AUTOPLAY_PROFILES.map(profile => ({ ...profile })),
    runs,
    summary: summarizeCampaignAutoplay(runs)
  }
  assertCampaignAutoplaySuite(suite)
  return suite
}

export const runCampaignAutoplaySuite = (options: CampaignAutoplaySuiteOptions = {}): CampaignAutoplaySuite => {
  const runs: CampaignAutoplayRun[] = []
  const total = CAMPAIGN_AUTOPLAY_SEEDS.length * CAMPAIGN_AUTOPLAY_PROFILES.length
  for (const seed of CAMPAIGN_AUTOPLAY_SEEDS) for (const profile of CAMPAIGN_AUTOPLAY_PROFILES) {
    const report = runAutoplay(newSeededCampaignRun(seed), { mode: profile.mode, policy: profile.policy, turnLimit: CAMPAIGN_AUTOPLAY_TURN_LIMIT, captureTrace: true, traceLimit: options.captureTrace ? undefined : 24 })
    const current = compactCampaignAutoplayRun(seed, profile, report)
    runs.push(current)
    options.onRun?.(current, runs.length, total)
  }
  return campaignAutoplaySuite(runs)
}

export const campaignAutoplayDelta = (current: CampaignAutoplaySuite, baseline: CampaignAutoplaySuite): CampaignAutoplayDelta => ({
  overall: current.summary.failureRate - baseline.summary.failureRate,
  byProfile: Object.fromEntries(CAMPAIGN_AUTOPLAY_PROFILES.map(profile => [profile.id, current.summary.byProfile[profile.id].failureRate - baseline.summary.byProfile[profile.id].failureRate])) as Record<CampaignAutoplayProfileId, number>
})
