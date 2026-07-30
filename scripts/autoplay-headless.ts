import { runAutoplay } from '../src/autoplay-runner'
import { autoplayHeuristicProfile } from '../src/autoplay-heuristics'
import { newRun } from '../src/engine'
import type { AutoplayMode, AutoplayPolicy, Biome } from '../src/types'

const biomes = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'] as const
const modes = ['visible', 'omniscient'] as const
const policies = ['survival', 'clear', 'explore', 'legacy'] as const
const biomeValue = process.env.BIOME ?? 'mine'
const modeValue = process.env.MODE ?? 'omniscient'
const policyValue = process.env.POLICY ?? 'clear'
const seed = Number(process.env.SEED ?? 7)
const areaFloor = Number(process.env.FLOOR ?? 0)
const turnLimit = Number(process.env.TURNS ?? 19200)
const captureTrace = process.env.TRACE === '1'
const includeState = process.env.DEBUG_STATE === '1'
const includeDebug = process.env.DEBUG_CONTEXT === '1'
const chainAreas = process.env.CHAIN_AREAS !== '0'
const chainFloors = process.env.CHAIN_FLOORS !== '0'
if (!biomes.includes(biomeValue as Biome)) throw new Error(`invalid BIOME: ${biomeValue}`)
if (!modes.includes(modeValue as Exclude<AutoplayMode, 'off'>)) throw new Error(`invalid MODE: ${modeValue}`)
if (!policies.includes(policyValue as AutoplayPolicy)) throw new Error(`invalid POLICY: ${policyValue}`)
if (!Number.isInteger(seed) || seed < 0) throw new Error(`invalid SEED: ${process.env.SEED}`)
if (!Number.isInteger(areaFloor) || areaFloor < 0 || areaFloor > 3) throw new Error(`invalid FLOOR: ${process.env.FLOOR}`)
if (!Number.isInteger(turnLimit) || turnLimit < 1) throw new Error(`invalid TURNS: ${process.env.TURNS}`)
const biome = biomeValue as Biome
const mode = modeValue as Exclude<AutoplayMode, 'off'>
const policy = policyValue as AutoplayPolicy
const heuristicProfile = autoplayHeuristicProfile(process.env.HEURISTIC_PROFILE)
const report = runAutoplay(newRun(seed, biome, areaFloor), { mode, policy, heuristicProfile, turnLimit, chainAreas, chainFloors, captureTrace, includeState, includeDebug })

console.log(JSON.stringify(report, null, 2))
