import { describe, expect, it } from 'vitest'
import { AREA_GATES, gateForArea } from './gates'

describe('area gate data model', () => {
  it('defines NPC offering, tag alternatives, cost, and destination for every area', () => {
    const destinations = { mine: 'wilds', wilds: 'caverns', caverns: 'ruins', ruins: 'ruins' }
    for (const [biome, gate] of Object.entries(AREA_GATES)) {
      const destination = destinations[biome as keyof typeof destinations]
      expect(gate).toMatchObject({ biome, npcOffering: expect.any(String), cost: { gold: expect.any(Number), items: expect.any(Array) }, unlockedDestination: { biome: destination, floor: destination === biome ? 3 : 0, point: destination === biome ? { x: 45, y: 32 } : { x: 2, y: 2 } } })
      expect(gate.tagAlternatives.length).toBeGreaterThan(1)
      expect(gate.tagAlternatives.every(option => option.tags.length > 0)).toBe(true)
    }
  })

  it('looks up gates by their owning area', () => {
    expect(gateForArea('mine').id).toBe('mine-wilds-pass')
    expect(gateForArea('ruins').unlockedDestination.biome).toBe('ruins')
  })
})
