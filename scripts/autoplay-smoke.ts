import assert from 'node:assert/strict'
import { runAutoplay } from '../src/autoplay-runner'
import { newRun } from '../src/engine'
import type { Biome } from '../src/types'
import { generateAreaFloor, validateGeneration } from '../src/world'

const allBiomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']
const parseNumbers = (value: string | undefined, fallback: readonly number[], label: string): number[] => {
  const parsed = value?.split(',').filter(Boolean).map(Number) ?? [...fallback]
  if (!parsed.length || parsed.some(entry => !Number.isInteger(entry))) throw new Error(`invalid ${label}: ${value}`)
  return [...new Set(parsed)]
}
const parseBiomes = (value: string | undefined): Biome[] => {
  const parsed = value?.split(',').filter(Boolean) ?? [...allBiomes]
  if (!parsed.length || parsed.some(entry => !allBiomes.includes(entry as Biome))) throw new Error(`invalid BIOMES: ${value}`)
  return [...new Set(parsed as Biome[])]
}
const biomes = parseBiomes(process.env.BIOMES)
const seeds = parseNumbers(process.env.SEEDS, [7, 42, 999], 'SEEDS')
const floors = parseNumbers(process.env.FLOORS, [0, 1, 2, 3], 'FLOORS')
const modes = ['omniscient'] as const
const requireCompletion = process.env.AUTOPLAY_REQUIRE_COMPLETION === '1'
const validationRun = (seed: number, biome: Biome) => {
  const state = newRun(seed, biome)
  state.hero.health = 120
  state.hero.maxHealth = 120
  state.hero.bombs = 8
  state.hero.ropes = 8
  state.hero.stats = { strength: 12, agility: 2, vitality: 12, intellect: 4 }
  state.hero.inventory.push('auger', 'reedGlider', 'grappleLine', 'bridgeKit', 'steamJetpack', 'portableWinch')
  return state
}
if (floors.some(floor => floor < 0 || floor > 3)) throw new Error(`invalid FLOORS: ${process.env.FLOORS}`)
const outcomes = new Map<string, number>()
const incomplete: string[] = []

for (const seed of seeds) for (const biome of biomes) for (const floor of floors) assert.deepEqual(validateGeneration(generateAreaFloor(seed, biome, floor)), { valid: true, errors: [] })
for (const seed of seeds) for (const biome of biomes) for (const mode of modes) {
  const initial = validationRun(seed, biome)
  const options = { mode, policy: 'survival' as const, turnLimit: 15_000, chainAreas: false, captureTrace: false } as const
  const first = runAutoplay(initial, options)
  const second = runAutoplay(initial, options)
  const label = `${mode}/${biome}/route/${seed}`
  assert.deepEqual(first, second, `${label} was nondeterministic`)
  assert.ok(first.commands.length > 0, `${label} issued no commands`)
  if (first.outcome !== 'complete') incomplete.push(`${label}: ${first.outcome}`)
  outcomes.set(`${mode}:${first.outcome}`, (outcomes.get(`${mode}:${first.outcome}`) ?? 0) + 1)
  console.log(`${label}: ${first.outcome} at ${first.turns}`)
}
console.log(`outcomes: ${[...outcomes].map(([outcome, count]) => `${outcome}=${count}`).join(' ')}`)
if (incomplete.length) console.log(`incomplete: ${incomplete.join(', ')}`)
if (requireCompletion) assert.deepEqual(incomplete, [], 'autoplay completion failures')
