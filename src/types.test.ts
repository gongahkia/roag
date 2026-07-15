import { describe, expect, it } from 'vitest'
import type { Campaign, Hero, LegacyRecord } from './types'

const hero: Hero = {
  name: 'Ari', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint',
  x: 0, y: 0, health: 1, maxHealth: 1, focus: 0, maxFocus: 0, gold: 0, bombs: 0, ropes: 0, keys: 0, xp: 0, level: 1,
  stats: { strength: 1, agility: 1, vitality: 1, intellect: 1 }, skills: [], inventory: [], equipment: {}
}

const legacy: LegacyRecord = { id: 'heir-1', heirName: 'Ari', cause: 'defeated', biome: 'mine', floor: 3, seed: 7, lineage: [], location: { x: 1, y: 1 }, cache: { gold: 0, items: [] }, encounter: { kind: 'cache', resolved: false } }
const campaign = (phase: Campaign['phase']): Campaign => {
  const base = { version: 2 as const, seed: 7, areas: [{ biome: 'mine' as const, status: 'active' as const, floor: 0, completed: false }], hub: { season: 1, supplies: [], rescued: [], unlockedAreas: ['mine' as const], completedAreas: [] }, legacy: [] }
  if (phase === 'title') return { ...base, phase }
  if (phase === 'hub') return { ...base, phase, hero }
  if (phase === 'area') return { ...base, phase, hero, activeBiome: 'mine' }
  if (phase === 'dead') return { ...base, phase, legacyRecord: legacy }
  return { ...base, phase, hero }
}

describe('campaign domain types', () => {
  it('represents every versioned campaign lifecycle phase', () => {
    const phases: Campaign['phase'][] = ['title', 'hub', 'area', 'dead', 'victory']
    expect(phases.map(campaign).map(state => state.phase)).toEqual(phases)
    expect(campaign('dead').version).toBe(2)
  })
})
