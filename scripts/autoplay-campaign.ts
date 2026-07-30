import { spawn } from 'node:child_process'
import { availableParallelism } from 'node:os'
import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { CAMPAIGN_AUTOPLAY_PROFILES, campaignAutoplayEntries, campaignAutoplayRunMatchesCorpus, campaignAutoplaySuite, type CampaignAutoplayRun } from '../src/autoplay-campaign'
import { autoplayHeuristicProfile, autoplayHeuristicProfileRef } from '../src/autoplay-heuristics'
import { autoplayReplayMetadata } from '../src/autoplay-runner'
import { diagnoseAutoplayFailure, summarizeAutoplayFailureCodes } from '../src/autoplay-failure-diagnosis'
import { compareFrozenAutoplayBaseline, createFrozenAutoplayBaseline, parseFrozenAutoplayBaseline, type FrozenAutoplayBaseline } from '../src/autoplay-frozen-baseline'
import { createPolicyProfile, createPolicyRunMetadata, scorePolicyEpisode } from '../src/autoplay-policy'
import { newSeededCampaignRun } from '../src/engine'
import { AUTOPLAY_SEED_CORPUS_VERSION, type AutoplaySeedCorpusPartition } from '../src/autoplay-seed-corpus'

const baselinePath = resolve('scripts/autoplay-campaign-baseline.json')
const approveBaseline = process.argv.includes('--approve-baseline')
const partitionFlag = process.argv.indexOf('--partition')
const seedFlag = process.argv.indexOf('--seed')
const reviewedByFlag = process.argv.indexOf('--reviewed-by')
const reviewFlag = process.argv.indexOf('--review')
if (partitionFlag >= 0 && (!process.argv[partitionFlag + 1] || process.argv[partitionFlag + 1].startsWith('--'))) throw new Error('missing --partition value')
if (seedFlag >= 0 && (!process.argv[seedFlag + 1] || process.argv[seedFlag + 1].startsWith('--'))) throw new Error('missing --seed value')
if (reviewedByFlag >= 0 && (!process.argv[reviewedByFlag + 1] || process.argv[reviewedByFlag + 1].startsWith('--'))) throw new Error('missing --reviewed-by value')
if (reviewFlag >= 0 && (!process.argv[reviewFlag + 1] || process.argv[reviewFlag + 1].startsWith('--'))) throw new Error('missing --review value')
if (process.argv.includes('--update-baseline')) throw new Error('use --approve-baseline with --reviewed-by and --review')
const partition = (partitionFlag >= 0 ? process.argv[partitionFlag + 1] : 'development') as AutoplaySeedCorpusPartition
const requestedSeed = seedFlag >= 0 ? Number(process.argv[seedFlag + 1]) : undefined
const review = { reviewedBy: reviewedByFlag >= 0 ? process.argv[reviewedByFlag + 1] : '', review: reviewFlag >= 0 ? process.argv[reviewFlag + 1] : '' }
if (requestedSeed !== undefined && (!Number.isSafeInteger(requestedSeed) || requestedSeed < 0)) throw new Error(`invalid --seed value: ${process.argv[seedFlag + 1]}`)
const entries = campaignAutoplayEntries(partition).filter(entry => requestedSeed === undefined || entry.seed === requestedSeed)
if (!entries.length) throw new Error(`seed ${requestedSeed} is not in the ${partition} autoplay corpus`)
const captureTrace = process.env.TRACE === '1'
const requestedWorkers = Number(process.env.MAX_WORKERS ?? Math.min(4, availableParallelism()))
const workerTimeout = Number(process.env.CAMPAIGN_AUTOPLAY_JOB_TIMEOUT_MS ?? 300_000)
const heuristicProfile = autoplayHeuristicProfile(process.env.HEURISTIC_PROFILE)
if (!Number.isInteger(requestedWorkers) || requestedWorkers < 1) throw new Error(`invalid MAX_WORKERS: ${process.env.MAX_WORKERS}`)
if (!Number.isInteger(workerTimeout) || workerTimeout < 1_000) throw new Error(`invalid CAMPAIGN_AUTOPLAY_JOB_TIMEOUT_MS: ${process.env.CAMPAIGN_AUTOPLAY_JOB_TIMEOUT_MS}`)
if (approveBaseline && partition !== 'development') throw new Error('held-out autoplay corpus cannot update a baseline')
if (approveBaseline && requestedSeed !== undefined) throw new Error('seed-filtered autoplay runs cannot update a baseline')
if (approveBaseline && heuristicProfile.id !== 'compatibility') throw new Error('only the compatibility heuristic profile can approve the frozen baseline')
if (approveBaseline && (!review.reviewedBy.trim() || !review.review.trim())) throw new Error('--approve-baseline requires --reviewed-by and --review')
let baseline: FrozenAutoplayBaseline | undefined
let baselineError: string | undefined
if (requestedSeed === undefined && partition === 'development' && existsSync(baselinePath)) {
  try { baseline = parseFrozenAutoplayBaseline(JSON.parse(readFileSync(baselinePath, 'utf8'))) }
  catch (error) {
    baselineError = error instanceof Error ? error.message : String(error)
    if (!approveBaseline) throw error
  }
}
if (!approveBaseline && requestedSeed === undefined && partition === 'development' && !baseline) throw new Error(baselineError ?? 'frozen autoplay baseline is missing')
const jobs = entries.flatMap(entry => CAMPAIGN_AUTOPLAY_PROFILES.map(profile => ({ entry, profile })))
const timedOutRun = (entry: typeof entries[number], profile: typeof CAMPAIGN_AUTOPLAY_PROFILES[number]): CampaignAutoplayRun => {
  const initial = newSeededCampaignRun(entry.seed)
  const areaOrder = [...entry.routeConfiguration.areaOrder]
  const finalBiome = areaOrder[0]!
  const replay = autoplayReplayMetadata(initial)
  const profileRef = autoplayHeuristicProfileRef(heuristicProfile)
  const reason = `worker wall-time limit (${workerTimeout}ms)`
  const retainedResources = initial.hero.bombs + initial.hero.ropes + initial.hero.keys
  const score = scorePolicyEpisode({ campaignComplete: false, outcome: 'turn-limit', exploredTiles: 0, metrics: { turns: 0, moves: 0, attacks: 0, damageTaken: 0, damageDealt: 0, pickups: 0, bombsUsed: 0, ropesUsed: 0, skillsUsed: 0, spellsUsed: 0, charmsUsed: 0, rests: 0, failedActions: 0, floorTransitions: 0 }, retainedResources })
  const policyMetadata = createPolicyRunMetadata(createPolicyProfile({ informationMode: profile.mode, policy: profile.policy }), entry.seed, entry.turnBudget, score)
  const diagnosis = diagnoseAutoplayFailure({ seed: entry.seed, partition, mode: profile.mode, policy: profile.policy, heuristicProfile: profileRef, turnLimit: entry.turnBudget, outcome: 'turn-limit', replay, exitPath: 'terrain-blocked', trace: [], resources: { bombsUsed: 0, ropesUsed: 0, selected: 0, deferred: 0, rejected: 0, projectedRouteGains: 0, criticalRouteSelections: 0 }, tools: { selected: 0, deferred: 0, rejected: 0, uses: 0, retirements: 0 }, optional: { pursued: 0, deferred: 0, declined: 0, secrets: 0, shortcuts: 0 }, reason })
  return { seed: entry.seed, profile: profile.id, mode: profile.mode, policy: profile.policy, heuristicProfile: profileRef, areaOrder, campaignComplete: false, outcome: 'turn-limit', turns: 0, finalBiome, floor: 1, completedAreas: [], evaluation: { policyMetadata, explorationValue: 0, resourcesSpent: 0, resourcesRetained: retainedResources, resourceOutcomes: { selected: 0, deferred: 0, rejected: 0, projectedRouteGains: 0, criticalRouteSelections: 0 }, optionalOutcomes: { pursued: 0, deferred: 0, declined: 0, secrets: 0, shortcuts: 0 } }, failure: { outcome: 'turn-limit', finalBiome, floor: 1, completedAreas: [], replay, partition, mode: profile.mode, policy: profile.policy, heuristicProfile: profileRef, turnLimit: entry.turnBudget, code: diagnosis.code, diagnosis, reason, trace: [] } }
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
const profileRef = autoplayHeuristicProfileRef(heuristicProfile)
const comparison = baseline ? compareFrozenAutoplayBaseline(current, baseline, profileRef) : undefined
const approvedBaseline = approveBaseline ? createFrozenAutoplayBaseline(current, profileRef, review) : undefined
if (approvedBaseline) writeFileSync(baselinePath, `${JSON.stringify(approvedBaseline, null, 2)}\n`)
const passed = Boolean(approvedBaseline || !comparison || comparison.passed)
const failureDiagnostics = summarizeAutoplayFailureCodes(current.runs.flatMap(run => run.failure ? [run.failure.diagnosis] : []))
process.stdout.write(`${JSON.stringify({ corpus: { version: AUTOPLAY_SEED_CORPUS_VERSION, partition, entries: entries.length, ...(requestedSeed === undefined ? {} : { seed: requestedSeed }) }, heuristicProfile: profileRef, current, failureDiagnostics, traceCaptured: captureTrace, workers: Math.min(requestedWorkers, jobs.length), baseline: baseline ? { review: baseline.review, comparison } : baselineError ? { error: baselineError } : undefined, baselineUpdated: Boolean(approvedBaseline), qualityGate: { runtimeTolerance: comparison?.runtimeTolerance, passed } }, null, 2)}\n`, () => process.exit(passed ? 0 : 1))
