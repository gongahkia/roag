import { describe, expect, it } from 'vitest'
import { AREA_GATES, gateForArea } from './gates'

describe('area gate data model', () => {
  it('defines NPC offering, tag alternatives, cost, and destination for every area', () => {
    for (const [biome, gate] of Object.entries(AREA_GATES)) {
      expect(gate).toMatchObject({ biome, npcOffering: expect.any(String), cost: { gold: expect.any(Number), items: expect.any(Array) }, unlockedDestination: { biome, floor: 3, point: { x: 45, y: 32 } } })
      expect(gate.tagAlternatives.length).toBeGreaterThan(1)
      expect(gate.tagAlternatives.every(option => option.tags.length > 0)).toBe(true)
    }
  })

  it('looks up gates by their owning area', () => {
    expect(gateForArea('mine').id).toBe('mine-shaft')
    expect(gateForArea('ruins').unlockedDestination.biome).toBe('ruins')
  })
})
