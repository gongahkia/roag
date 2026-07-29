import { spawn } from 'node:child_process'
import { readFile } from 'node:fs/promises'
import { availableParallelism } from 'node:os'
import { resolve } from 'node:path'
import { summarizeBalanceRuns, type BalanceRun } from '../src/balance-dashboard'
import { AUTOPLAY_MAX_TURNS } from '../src/autoplay'
import { CAMPAIGN_AUTOPLAY_SEEDS } from '../src/autoplay-campaign'
import { BIOME_POOL } from '../src/engine/campaign'
import { summarizePlaytestRecords } from '../src/playtest-records'

const requestedSeeds = process.env.BALANCE_SEEDS?.split(',').map(Number)
const seeds = requestedSeeds?.length ? requestedSeeds : CAMPAIGN_AUTOPLAY_SEEDS.slice(0, 4)
if (seeds.some(seed => !Number.isInteger(seed) || seed < 0)) throw new Error(`invalid BALANCE_SEEDS: ${process.env.BALANCE_SEEDS}`)
const turnLimit = Number(process.env.BALANCE_TURN_LIMIT ?? AUTOPLAY_MAX_TURNS)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid BALANCE_TURN_LIMIT: ${process.env.BALANCE_TURN_LIMIT}`)
const requestedWorkers = Number(process.env.MAX_WORKERS ?? Math.min(4, availableParallelism()))
if (!Number.isInteger(requestedWorkers) || requestedWorkers < 1) throw new Error(`invalid MAX_WORKERS: ${process.env.MAX_WORKERS}`)
const requireAcceptance = process.env.BALANCE_REQUIRE_ACCEPTANCE === '1'
const playtestDocument: unknown = JSON.parse(await readFile(resolve(process.env.PLAYTEST_RECORDS ?? 'docs/playtests/records.json'), 'utf8'))
const playtests = summarizePlaytestRecords(playtestDocument)
if (!playtests.valid) throw new Error(`invalid playtest records: ${playtests.errors.join('; ')}`)

const jobs = seeds.flatMap(seed => BIOME_POOL.map(biome => ({ seed, biome })))
const run = ({ seed, biome }: typeof jobs[number]): Promise<BalanceRun> => new Promise((resolveRun, reject) => {
  const child = spawn(resolve('node_modules/.bin/tsx'), [resolve('scripts/balance-report-worker.ts')], { env: { ...process.env, BALANCE_SEED: String(seed), BALANCE_BIOME: biome, BALANCE_TURN_LIMIT: String(turnLimit) }, stdio: ['ignore', 'pipe', 'pipe'] })
  let output = ''
  let errors = ''
  child.stdout.on('data', chunk => { output += chunk })
  child.stderr.on('data', chunk => { errors += chunk })
  child.once('error', reject)
  child.once('close', code => {
    if (code !== 0) { reject(new Error(`${seed}/${biome} exited ${code}: ${errors.trim()}`)); return }
    try { resolveRun(JSON.parse(output) as BalanceRun) }
    catch (error) { reject(error instanceof Error ? error : new Error(String(error))) }
  })
})

const runs = new Array<BalanceRun>(jobs.length)
let next = 0
let completed = 0
await Promise.all(Array.from({ length: Math.min(requestedWorkers, jobs.length) }, async () => {
  while (next < jobs.length) {
    const index = next++
    runs[index] = await run(jobs[index])
    completed++
    console.error(`balance ${completed}/${jobs.length}: ${jobs[index].seed}/${jobs[index].biome} ${runs[index].outcome}`)
  }
}))
const dashboard = summarizeBalanceRuns(runs, seeds)
console.log(JSON.stringify({ ...dashboard, turnLimit, workers: Math.min(requestedWorkers, jobs.length), playtests }, null, 2))
if (requireAcceptance && !dashboard.acceptance.valid) process.exitCode = 1
