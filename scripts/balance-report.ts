import { spawn } from 'node:child_process'
import { availableParallelism } from 'node:os'
import { resolve } from 'node:path'
import { CAMPAIGN_AUTOPLAY_SEEDS, CAMPAIGN_AUTOPLAY_TURN_LIMIT } from '../src/autoplay-campaign'
import type { RunTelemetry } from '../src/types'

type Counts = Record<string, number>
interface BalanceRun { seed: number; outcome: string; complete: boolean; biome: string; floor: number; turns: number; metrics: RunTelemetry }

const requestedSeeds = process.env.BALANCE_SEEDS?.split(',').map(Number)
const seeds = requestedSeeds?.length ? requestedSeeds : [...CAMPAIGN_AUTOPLAY_SEEDS]
if (seeds.some(seed => !Number.isInteger(seed) || seed < 0)) throw new Error(`invalid BALANCE_SEEDS: ${process.env.BALANCE_SEEDS}`)
const turnLimit = Number(process.env.BALANCE_TURN_LIMIT ?? CAMPAIGN_AUTOPLAY_TURN_LIMIT)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid BALANCE_TURN_LIMIT: ${process.env.BALANCE_TURN_LIMIT}`)
const requestedWorkers = Number(process.env.MAX_WORKERS ?? Math.min(4, availableParallelism()))
if (!Number.isInteger(requestedWorkers) || requestedWorkers < 1) throw new Error(`invalid MAX_WORKERS: ${process.env.MAX_WORKERS}`)

const addCounts = (target: Counts, source: Counts): Counts => {
  for (const [key, value] of Object.entries(source)) target[key] = (target[key] ?? 0) + value
  return target
}

const total = (runs: readonly BalanceRun[], project: (metrics: RunTelemetry) => number): number => runs.reduce((sum, run) => sum + project(run.metrics), 0)
const average = (runs: readonly BalanceRun[], project: (metrics: RunTelemetry) => number): number => runs.length ? Number((total(runs, project) / runs.length).toFixed(2)) : 0
const rank = (counts: Counts) => Object.entries(counts).sort(([idA, countA], [idB, countB]) => countB - countA || idA.localeCompare(idB)).map(([id, count]) => ({ id, count }))

const run = (seed: number): Promise<BalanceRun> => new Promise((resolveRun, reject) => {
  const child = spawn(resolve('node_modules/.bin/tsx'), [resolve('scripts/balance-report-worker.ts')], { env: { ...process.env, BALANCE_SEED: String(seed), BALANCE_TURN_LIMIT: String(turnLimit) }, stdio: ['ignore', 'pipe', 'pipe'] })
  let output = ''
  let errors = ''
  child.stdout.on('data', chunk => { output += chunk })
  child.stderr.on('data', chunk => { errors += chunk })
  child.once('error', reject)
  child.once('close', code => {
    if (code !== 0) { reject(new Error(`seed ${seed} exited ${code}: ${errors.trim()}`)); return }
    try { resolveRun(JSON.parse(output) as BalanceRun) }
    catch (error) { reject(error instanceof Error ? error : new Error(String(error))) }
  })
})

const runs = new Array<BalanceRun>(seeds.length)
let nextSeed = 0
let completedRuns = 0
await Promise.all(Array.from({ length: Math.min(requestedWorkers, seeds.length) }, async () => {
  while (nextSeed < seeds.length) {
    const index = nextSeed++
    const result = await run(seeds[index])
    runs[index] = result
    completedRuns++
    console.error(`balance ${completedRuns}/${seeds.length}: ${result.seed} ${result.outcome} floor ${result.floor}`)
  }
}))

const completed = runs.filter(run => run.complete)
const aggregate = (project: (metrics: RunTelemetry) => Counts): Counts => runs.reduce((counts, run) => addCounts(counts, project(run.metrics)), {} as Counts)
const failures = runs.filter(run => !run.complete).map(run => ({ seed: run.seed, outcome: run.outcome, biome: run.biome, floor: run.floor, turns: run.turns }))

console.log(JSON.stringify({
  version: 1,
  mode: 'omniscient-clear',
  seeds,
  turnLimit,
  workers: Math.min(requestedWorkers, seeds.length),
  completion: { completed: completed.length, total: runs.length, rate: Number((completed.length / runs.length).toFixed(3)), failures },
  perRunAverage: {
    turns: average(runs, metrics => metrics.turns), kills: average(runs, metrics => metrics.kills), damageDealt: average(runs, metrics => metrics.damageDealt), damageTaken: average(runs, metrics => metrics.damageTaken), goldGained: average(runs, metrics => metrics.goldGained), goldSpent: average(runs, metrics => metrics.goldSpent), pickups: average(runs, metrics => metrics.pickups), bombsUsed: average(runs, metrics => metrics.bombsUsed), ropesUsed: average(runs, metrics => metrics.ropesUsed)
  },
  completionAverage: {
    turns: average(completed, metrics => metrics.turns), kills: average(completed, metrics => metrics.kills), damageDealt: average(completed, metrics => metrics.damageDealt), damageTaken: average(completed, metrics => metrics.damageTaken), goldGained: average(completed, metrics => metrics.goldGained), goldSpent: average(completed, metrics => metrics.goldSpent)
  },
  choices: {
    boons: rank(aggregate(metrics => metrics.boonPicks)),
    augments: rank(aggregate(metrics => metrics.boonAugments)),
    relics: rank(aggregate(metrics => metrics.relicPicks)),
    purchases: rank(aggregate(metrics => metrics.purchases)),
    itemsUsed: rank(aggregate(metrics => metrics.itemsUsed))
  },
  enemies: rank(aggregate(metrics => metrics.enemyKills)),
  deaths: rank(aggregate(metrics => metrics.deathCauses))
}, null, 2))
