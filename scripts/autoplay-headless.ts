import { runAutoplay } from '../src/autoplay-runner'
import { newRun } from '../src/engine'
import type { AutoplayMode, AutoplayPolicy, Biome } from '../src/types'

const biomes = ['mine', 'wilds', 'caverns', 'ruins'] as const
const modes = ['visible', 'omniscient'] as const
const policies = ['survival', 'clear', 'legacy'] as const
const biomeValue = process.env.BIOME ?? 'mine'
const modeValue = process.env.MODE ?? 'omniscient'
const policyValue = process.env.POLICY ?? 'clear'
const seed = Number(process.env.SEED ?? 7)
const turnLimit = Number(process.env.TURNS ?? 3200)
const captureTrace = process.env.TRACE === '1'
const includeState = process.env.DEBUG_STATE === '1'
const includeDebug = process.env.DEBUG_CONTEXT === '1'
if (!biomes.includes(biomeValue as Biome)) throw new Error(`invalid BIOME: ${biomeValue}`)
if (!modes.includes(modeValue as Exclude<AutoplayMode, 'off'>)) throw new Error(`invalid MODE: ${modeValue}`)
if (!policies.includes(policyValue as AutoplayPolicy)) throw new Error(`invalid POLICY: ${policyValue}`)
if (!Number.isInteger(seed) || seed < 0) throw new Error(`invalid SEED: ${process.env.SEED}`)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid TURNS: ${process.env.TURNS}`)
const biome = biomeValue as Biome
const mode = modeValue as Exclude<AutoplayMode, 'off'>
const policy = policyValue as AutoplayPolicy
const report = runAutoplay(newRun(seed, biome), { mode, policy, turnLimit, captureTrace, includeState, includeDebug })

console.log(JSON.stringify(report, null, 2))
