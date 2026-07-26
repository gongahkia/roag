import { spawn } from 'node:child_process'
import { availableParallelism } from 'node:os'
import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { CAMPAIGN_AUTOPLAY_PROFILES, CAMPAIGN_AUTOPLAY_SEEDS, campaignAutoplayDelta, campaignAutoplaySuite, type CampaignAutoplayRun, type CampaignAutoplaySuite } from '../src/autoplay-campaign'

const baselinePath = resolve('scripts/autoplay-campaign-baseline.json')
const updateBaseline = process.argv.includes('--update-baseline')
const captureTrace = process.env.TRACE === '1'
const allowedRegression = Number(process.env.CAMPAIGN_MAX_FAILURE_REGRESSION ?? 0)
const requestedWorkers = Number(process.env.MAX_WORKERS ?? Math.min(4, availableParallelism()))
if (!Number.isInteger(requestedWorkers) || requestedWorkers < 1) throw new Error(`invalid MAX_WORKERS: ${process.env.MAX_WORKERS}`)
if (!Number.isFinite(allowedRegression) || allowedRegression < 0 || allowedRegression > 1) throw new Error(`invalid CAMPAIGN_MAX_FAILURE_REGRESSION: ${process.env.CAMPAIGN_MAX_FAILURE_REGRESSION}`)
const baseline = existsSync(baselinePath) ? JSON.parse(readFileSync(baselinePath, 'utf8')) as CampaignAutoplaySuite : undefined
const jobs = CAMPAIGN_AUTOPLAY_SEEDS.flatMap(seed => CAMPAIGN_AUTOPLAY_PROFILES.map(profile => ({ seed, profile })))
const run = (seed: number, profile: typeof CAMPAIGN_AUTOPLAY_PROFILES[number]): Promise<CampaignAutoplayRun> => new Promise((resolveRun, reject) => {
  const child = spawn(resolve('node_modules/.bin/vite-node'), ['--script', resolve('scripts/autoplay-campaign-worker.ts')], { env: { ...process.env, CAMPAIGN_AUTOPLAY_SEED: String(seed), CAMPAIGN_AUTOPLAY_PROFILE: profile.id, CAMPAIGN_AUTOPLAY_TRACE: captureTrace ? '1' : '0' }, stdio: ['ignore', 'pipe', 'pipe'] })
  let output = ''
  let errors = ''
  child.stdout.on('data', chunk => { output += chunk })
  child.stderr.on('data', chunk => { errors += chunk })
  child.once('error', reject)
  child.once('close', code => {
    if (code !== 0) { reject(new Error(`${seed}/${profile.id} exited ${code}: ${errors.trim()}`)); return }
    try { resolveRun(JSON.parse(output) as CampaignAutoplayRun) }
    catch (error) { reject(error instanceof Error ? error : new Error(String(error))) }
  })
})
const runs = new Array<CampaignAutoplayRun>(jobs.length)
let nextJob = 0
let completed = 0
await Promise.all(Array.from({ length: Math.min(requestedWorkers, jobs.length) }, async () => {
  while (nextJob < jobs.length) {
    const index = nextJob++
    const job = jobs[index]
    const result = await run(job.seed, job.profile)
    runs[index] = result
    completed++
    console.error(`autoplay campaign ${completed}/${jobs.length}: ${result.seed}/${result.profile} ${result.outcome} at ${result.turns}`)
  }
}))
const current = campaignAutoplaySuite(runs)
if (updateBaseline) writeFileSync(baselinePath, `${JSON.stringify(current, null, 2)}\n`)
const comparison = baseline ? campaignAutoplayDelta(current, baseline) : undefined
const passed = updateBaseline || !comparison || comparison.overall <= allowedRegression
console.log(JSON.stringify({ current, traceCaptured: captureTrace, workers: Math.min(requestedWorkers, jobs.length), baseline: baseline ? { summary: baseline.summary, comparison } : undefined, baselineUpdated: updateBaseline, qualityGate: { allowedFailureRateRegression: allowedRegression, passed } }, null, 2))
if (!passed) process.exitCode = 1
