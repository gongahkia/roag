import { balanceFloorSamples, type BalanceRun } from '../src/balance-dashboard'
import { AUTOPLAY_MAX_TURNS } from '../src/autoplay'
import { runAutoplay } from '../src/autoplay-runner'
import { newRun } from '../src/engine'
import { BIOME_POOL } from '../src/engine/campaign'

const seed = Number(process.env.BALANCE_SEED)
const biome = process.env.BALANCE_BIOME
const turnLimit = Number(process.env.BALANCE_TURN_LIMIT ?? AUTOPLAY_MAX_TURNS)
if (!Number.isInteger(seed) || seed < 0) throw new Error(`invalid BALANCE_SEED: ${process.env.BALANCE_SEED}`)
if (!BIOME_POOL.includes(biome as typeof BIOME_POOL[number])) throw new Error(`invalid BALANCE_BIOME: ${process.env.BALANCE_BIOME}`)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid BALANCE_TURN_LIMIT: ${process.env.BALANCE_TURN_LIMIT}`)
const routePosition = BIOME_POOL.indexOf(biome as typeof BIOME_POOL[number])
const report = runAutoplay(newRun(seed, biome as typeof BIOME_POOL[number], 0, undefined, [], [], BIOME_POOL), { mode: 'omniscient', policy: 'clear', turnLimit, chainFloors: false, chainAreas: false, captureTrace: false })
const run: BalanceRun = { seed, biome: biome as typeof BIOME_POOL[number], outcome: report.outcome, complete: report.outcome === 'complete', floor: report.floor, turns: report.turns, metrics: report.metrics, floors: balanceFloorSamples(seed, biome as typeof BIOME_POOL[number], routePosition) }
process.stdout.write(JSON.stringify(run))
