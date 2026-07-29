import assert from 'node:assert/strict'
import { runAutoplay } from '../src/autoplay-runner'
import { newRun } from '../src/engine'
import type { Biome } from '../src/types'

const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']
const seeds = (process.env.SEEDS ?? '7,42,999').split(',').filter(Boolean).map(Number)
const minimum = Number(process.env.MIN_COVERAGE ?? .7)
if (!seeds.length || seeds.some(seed => !Number.isInteger(seed) || seed < 0)) throw new Error(`invalid SEEDS: ${process.env.SEEDS}`)
if (!Number.isFinite(minimum) || minimum <= 0 || minimum > 1) throw new Error(`invalid MIN_COVERAGE: ${process.env.MIN_COVERAGE}`)

const configured = (seed: number, biome: Biome, floor: number) => {
  const state = newRun(seed, biome, floor)
  state.hero.health = 120
  state.hero.maxHealth = 120
  state.hero.bombs = 8
  state.hero.ropes = 8
  state.hero.stats = { strength: 12, agility: 2, vitality: 12, intellect: 4 }
  state.hero.inventory.push('auger', 'reedGlider', 'grappleLine', 'bridgeKit', 'steamJetpack', 'portableWinch')
  return state
}

const profiles = [{ id: 'visible', mode: 'visible' as const, policy: 'explore' as const }, { id: 'full-map', mode: 'omniscient' as const, policy: 'clear' as const }]
const failures: string[] = []
for (const profile of profiles) for (const biome of biomes) {
  const reports = seeds.flatMap(seed => [0, 1, 2, 3].map(floor => runAutoplay(configured(seed, biome, floor), { mode: profile.mode, policy: profile.policy, turnLimit: 15_000, chainAreas: false, chainFloors: false, captureTrace: false })))
  const completed = reports.filter(report => report.outcome === 'complete').length
  const coverage = completed / reports.length
  console.log(`${profile.id}/${biome}: ${completed}/${reports.length} ${(coverage * 100).toFixed(1)}%`)
  if (coverage < minimum) failures.push(`${profile.id}/${biome}=${(coverage * 100).toFixed(1)}%`)
}
assert.deepEqual(failures, [], `autoplay coverage below ${(minimum * 100).toFixed(0)}%: ${failures.join(', ')}`)
