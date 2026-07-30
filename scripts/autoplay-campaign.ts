import { spawn } from 'node:child_process'
import { availableParallelism } from 'node:os'
import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { CAMPAIGN_AUTOPLAY_PROFILES, campaignAutoplayDelta, campaignAutoplayEntries, campaignAutoplayRunMatchesCorpus, campaignAutoplaySuite, type CampaignAutoplayRun, type CampaignAutoplaySuite } from '../src/autoplay-campaign'
import { autoplayHeuristicProfile, autoplayHeuristicProfileRef } from '../src/autoplay-heuristics'
import { autoplayReplayMetadata } from '../src/autoplay-runner'
import { diagnoseAutoplayFailure, summarizeAutoplayFailureCodes } from '../src/autoplay-failure-diagnosis'
import { newSeededCampaignRun } from '../src/engine'
import { AUTOPLAY_SEED_CORPUS_VERSION, type AutoplaySeedCorpusPartition } from '../src/autoplay-seed-corpus'

const baselinePath = resolve('scripts/autoplay-campaign-baseline.json')
const updateBaseline = process.argv.includes('--update-baseline')
const partitionFlag = process.argv.indexOf('--partition')
const seedFlag = process.argv.indexOf('--seed')
if (partitionFlag >= 0 && (!process.argv[partitionFlag + 1] || process.argv[partitionFlag + 1].startsWith('--'))) throw new Error('missing --partition value')
if (seedFlag >= 0 && (!process.argv[seedFlag + 1] || process.argv[seedFlag + 1].startsWith('--'))) throw new Error('missing --seed value')
const partition = (partitionFlag >= 0 ? process.argv[partitionFlag + 1] : 'development') as AutoplaySeedCorpusPartition
const requestedSeed = seedFlag >= 0 ? Number(process.argv[seedFlag + 1]) : undefined
if (requestedSeed !== undefined && (!Number.isSafeInteger(requestedSeed) || requestedSeed < 0)) throw new Error(`invalid --seed value: ${process.argv[seedFlag + 1]}`)
const entries = campaignAutoplayEntries(partition).filter(entry => requestedSeed === undefined || entry.seed === requestedSeed)
if (!entries.length) throw new Error(`seed ${requestedSeed} is not in the ${partition} autoplay corpus`)
const captureTrace = process.env.TRACE === '1'
const allowedRegression = Number(process.env.CAMPAIGN_MAX_FAILURE_REGRESSION ?? 0)
const requestedWorkers = Number(process.env.MAX_WORKERS ?? Math.min(4, availableParallelism()))
const workerTimeout = Number(process.env.CAMPAIGN_AUTOPLAY_JOB_TIMEOUT_MS ?? 300_000)
const heuristicProfile = autoplayHeuristicProfile(process.env.HEURISTIC_PROFILE)
if (!Number.isInteger(requestedWorkers) || requestedWorkers < 1) throw new Error(`invalid MAX_WORKERS: ${process.env.MAX_WORKERS}`)
if (!Number.isFinite(allowedRegression) || allowedRegression < 0 || allowedRegression > 1) throw new Error(`invalid CAMPAIGN_MAX_FAILURE_REGRESSION: ${process.env.CAMPAIGN_MAX_FAILURE_REGRESSION}`)
if (!Number.isInteger(workerTimeout) || workerTimeout < 1_000) throw new Error(`invalid CAMPAIGN_AUTOPLAY_JOB_TIMEOUT_MS: ${process.env.CAMPAIGN_AUTOPLAY_JOB_TIMEOUT_MS}`)
if (updateBaseline && partition !== 'development') throw new Error('held-out autoplay corpus cannot update a baseline')
if (updateBaseline && requestedSeed !== undefined) throw new Error('seed-filtered autoplay runs cannot update a baseline')
const baseline = requestedSeed === undefined && partition === 'development' && existsSync(baselinePath) ? JSON.parse(readFileSync(baselinePath, 'utf8')) as CampaignAutoplaySuite : undefined
const jobs = entries.flatMap(entry => CAMPAIGN_AUTOPLAY_PROFILES.map(profile => ({ entry, profile })))
const timedOutRun = (entry: typeof entries[number], profile: typeof CAMPAIGN_AUTOPLAY_PROFILES[number]): CampaignAutoplayRun => {
  const initial = newSeededCampaignRun(entry.seed)
  const areaOrder = [...entry.routeConfiguration.areaOrder]
  const finalBiome = areaOrder[0]!
  const replay = autoplayReplayMetadata(initial)
  const profileRef = autoplayHeuristicProfileRef(heuristicProfile)
  const reason = `worker wall-time limit (${workerTimeout}ms)`
  const diagnosis = diagnoseAutoplayFailure({ seed: entry.seed, partition, mode: profile.mode, policy: profile.policy, heuristicProfile: profileRef, turnLimit: entry.turnBudget, outcome: 'turn-limit', replay, exitPath: 'terrain-blocked', trace: [], resources: { bombsUsed: 0, ropesUsed: 0, selected: 0, deferred: 0, rejected: 0, projectedRouteGains: 0, criticalRouteSelections: 0 }, tools: { selected: 0, deferred: 0, rejected: 0, uses: 0, retirements: 0 }, optional: { pursued: 0, deferred: 0, declined: 0, secrets: 0, shortcuts: 0 }, reason })
  return { seed: entry.seed, profile: profile.id, mode: profile.mode, policy: profile.policy, heuristicProfile: profileRef, areaOrder, campaignComplete: false, outcome: 'turn-limit', turns: 0, finalBiome, floor: 1, completedAreas: [], failure: { outcome: 'turn-limit', finalBiome, floor: 1, completedAreas: [], replay, partition, mode: profile.mode, policy: profile.policy, heuristicProfile: profileRef, turnLimit: entry.turnBudget, code: diagnosis.code, diagnosis, reason, trace: [] } }
}
const run = (entry: typeof entries[number], profile: typeof CAMPAIGN_AUTOPLAY_PROFILES[number]): Promise<CampaignAutoplayRun> => new Promise((resolveRun, reject) => {
  const child = spawn(resolve('node_modules/.bin/tsx'), [resolve('scripts/autoplay-campaign-worker.ts')], { env: { ...process.env, HEURISTIC_PROFILE: heuristicProfile.id, CAMPAIGN_AUTOPLAY_PARTITION: partition, CAMPAIGN_AUTOPLAY_SEED: String(entry.seed), CAMPAIGN_AUTOPLAY_PROFILE: profile.id, CAMPAIGN_AUTOPLAY_TURN_LIMIT: String(entry.turnBudget), CAMPAIGN_AUTOPLAY_TRACE: captureTrace ? '1' : '0' }, stdio: ['ignore', 'pipe', 'pipe'] })
  let output = ''
  let errors = ''
  let settled = false
  const settle = (callback: () => void) => { if (settled) return; settled = true; clearTimeout(timeout); callback() }
  const timeout = setTimeout(() => { child.kill('SIGKILL'); settle(() => resolveRun(timedOutRun(entry, profile))) }, workerTimeout)
  child.stdout.on('data', chunk => { output += chunk })
  child.stderr.on('data', chunk => { errors += chunk })
  child.once('error', error => settle(() => reject(error)))
  child.once('close', code => {
    if (code !== 0) { settle(() => reject(new Error(`${entry.seed}/${profile.id} exited ${code}: ${errors.trim()}`))); return }
    try {
      const result = JSON.parse(output) as CampaignAutoplayRun
      if (!campaignAutoplayRunMatchesCorpus(entry, result)) throw new Error(`campaign autoplay corpus validation failed for ${entry.seed}/${profile.id}`)
      settle(() => resolveRun(result))
    }
    catch (error) { settle(() => reject(error instanceof Error ? error : new Error(String(error)))) }
  })
})
const runs = new Array<CampaignAutoplayRun>(jobs.length)
let nextJob = 0
let completed = 0
await Promise.all(Array.from({ length: Math.min(requestedWorkers, jobs.length) }, async () => {
  while (nextJob < jobs.length) {
    const index = nextJob++
    const job = jobs[index]
    const result = await run(job.entry, job.profile)
    runs[index] = result
    completed++
    console.error(`autoplay campaign ${completed}/${jobs.length}: ${result.seed}/${result.profile} ${result.outcome} at ${result.turns}`)
  }
}))
const current = campaignAutoplaySuite(runs, partition, entries.map(entry => entry.seed))
if (updateBaseline) writeFileSync(baselinePath, `${JSON.stringify(current, null, 2)}\n`)
const comparison = baseline ? campaignAutoplayDelta(current, baseline) : undefined
const passed = updateBaseline || !comparison || comparison.overall <= allowedRegression
const failureDiagnostics = summarizeAutoplayFailureCodes(current.runs.flatMap(run => run.failure ? [run.failure.diagnosis] : []))
process.stdout.write(`${JSON.stringify({ corpus: { version: AUTOPLAY_SEED_CORPUS_VERSION, partition, entries: entries.length, ...(requestedSeed === undefined ? {} : { seed: requestedSeed }) }, heuristicProfile: autoplayHeuristicProfileRef(heuristicProfile), current, failureDiagnostics, traceCaptured: captureTrace, workers: Math.min(requestedWorkers, jobs.length), baseline: baseline ? { summary: baseline.summary, comparison } : undefined, baselineUpdated: updateBaseline, qualityGate: { allowedFailureRateRegression: allowedRegression, passed } }, null, 2)}\n`, () => process.exit(passed ? 0 : 1))
