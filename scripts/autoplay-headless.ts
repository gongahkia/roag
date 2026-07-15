import { runAutoplay } from '../src/autoplay-runner'
import { newRun } from '../src/engine'
import type { AutoplayMode, AutoplayPolicy, Biome } from '../src/types'

const biome = (process.env.BIOME ?? 'mine') as Biome
const mode = (process.env.MODE ?? 'omniscient') as Exclude<AutoplayMode, 'off'>
const policy = (process.env.POLICY ?? 'clear') as AutoplayPolicy
const seed = Number(process.env.SEED ?? 7)
const turnLimit = Number(process.env.TURNS ?? 600)
const report = runAutoplay(newRun(seed, biome), { mode, policy, turnLimit })

console.log(JSON.stringify(report, null, 2))
