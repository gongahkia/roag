import assert from 'node:assert/strict'
import { runAutoplay } from '../src/autoplay-runner'
import { newRun } from '../src/engine'
import type { Biome } from '../src/types'
import { generateAreaFloor, validateGeneration } from '../src/world'

const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins']
const outcomes = new Map<string, number>()

for (const seed of [7, 42, 999]) for (const biome of biomes) for (let floor = 0; floor < 4; floor++) assert.deepEqual(validateGeneration(generateAreaFloor(seed, biome, floor)), { valid: true, errors: [] })
for (const seed of [7, 42]) for (const biome of biomes) for (const mode of ['visible', 'omniscient'] as const) {
  const initial = newRun(seed, biome)
  const first = runAutoplay(initial, { mode, turnLimit: mode === 'visible' ? 350 : 600 })
  const second = runAutoplay(initial, { mode, turnLimit: mode === 'visible' ? 350 : 600 })
  assert.notEqual(first.outcome, 'error', `${mode}/${biome}/${seed} returned an engine error`)
  assert.deepEqual(first, second, `${mode}/${biome}/${seed} was nondeterministic`)
  assert.ok(first.commands.length > 0, `${mode}/${biome}/${seed} issued no commands`)
  outcomes.set(`${mode}:${first.outcome}`, (outcomes.get(`${mode}:${first.outcome}`) ?? 0) + 1)
  console.log(`${mode}/${biome}/${seed}: ${first.outcome} at ${first.turns}`)
}
console.log(`outcomes: ${[...outcomes].map(([outcome, count]) => `${outcome}=${count}`).join(' ')}`)
