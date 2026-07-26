import { runAutoplay } from '../src/autoplay-runner'
import { newRun } from '../src/engine'

const seed = Number(process.env.BALANCE_SEED)
const turnLimit = Number(process.env.BALANCE_TURN_LIMIT)
if (!Number.isInteger(seed) || seed < 0) throw new Error(`invalid BALANCE_SEED: ${process.env.BALANCE_SEED}`)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid BALANCE_TURN_LIMIT: ${process.env.BALANCE_TURN_LIMIT}`)
const report = runAutoplay(newRun(seed), { mode: 'omniscient', policy: 'clear', turnLimit })
process.stdout.write(JSON.stringify({ seed, outcome: report.outcome, complete: report.campaignComplete, biome: report.finalBiome, floor: report.floor, turns: report.turns, metrics: report.metrics }))
