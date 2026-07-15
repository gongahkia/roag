import { runAutoplay } from '../src/autoplay-runner'
import { newRun } from '../src/engine'
import type { AutoplayMode, AutoplayPolicy, Biome } from '../src/types'

const allBiomes = ['mine', 'wilds', 'caverns', 'ruins'] as const
const allModes = ['visible', 'omniscient'] as const
const allPolicies = ['survival', 'clear', 'legacy'] as const
const list = <T extends string>(value: string | undefined, valid: readonly T[], fallback: readonly T[], name: string): T[] => {
  const entries = (value?.split(',').filter(Boolean) ?? [...fallback]) as T[]
  if (!entries.length || entries.some(entry => !valid.includes(entry))) throw new Error(`invalid ${name}: ${value}`)
  return [...new Set(entries)]
}
const seeds = (process.env.SEEDS ?? '7,42,999').split(',').filter(Boolean).map(value => Number(value))
if (!seeds.length || seeds.some(seed => !Number.isInteger(seed) || seed < 0)) throw new Error(`invalid SEEDS: ${process.env.SEEDS}`)
const biomes = list(process.env.BIOMES, allBiomes, allBiomes, 'BIOMES') as Biome[]
const modes = list(process.env.MODES, allModes, ['omniscient'], 'MODES') as Exclude<AutoplayMode, 'off'>[]
const policyValue = process.env.POLICY ?? 'clear'
if (!allPolicies.includes(policyValue as AutoplayPolicy)) throw new Error(`invalid POLICY: ${policyValue}`)
const policy = policyValue as AutoplayPolicy
const turnLimit = Number(process.env.TURNS ?? 800)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid TURNS: ${process.env.TURNS}`)
const fullTrace = process.env.FULL_TRACE === '1'
const reports = seeds.flatMap(seed => biomes.flatMap(biome => modes.map(mode => runAutoplay(newRun(seed, biome), { mode, policy, turnLimit }))))
const outcomes = Object.fromEntries(['complete', 'dead', 'stalled', 'turn-limit', 'error'].map(outcome => [outcome, reports.filter(report => report.outcome === outcome).length]))
const clearRate = reports.length ? outcomes.complete / reports.length : 0
const detail = reports.filter(report => fullTrace || report.outcome !== 'complete').map(report => ({ ...report, trace: fullTrace ? report.trace : report.trace.slice(-40) }))
console.log(JSON.stringify({ config: { seeds, biomes, modes, policy, turnLimit, fullTrace }, total: reports.length, clearRate, outcomes, detail }, null, 2))
