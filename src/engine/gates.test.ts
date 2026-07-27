import { describe, expect, it } from 'vitest'
import { AREA_GATES, gateForArea, gateForRun, resolveAreaGate, validateAreaGate } from './gates'
import { newRun } from './run'

describe('area gate data model', () => {
  it('defines NPC offering, tag alternatives, cost, and destination for every area', () => {
    for (const [biome, gate] of Object.entries(AREA_GATES)) {
      expect(gate).toMatchObject({ biome, npcOffering: expect.any(String), cost: { gold: expect.any(Number), items: expect.any(Array) }, unlockedDestination: { biome: expect.any(String), floor: expect.any(Number), point: { x: expect.any(Number), y: expect.any(Number) } } })
      expect(gate.tagAlternatives.length).toBeGreaterThan(1)
      expect(gate.tagAlternatives.every(option => option.tags.length > 0)).toBe(true)
    }
  })

  it('looks up gates by their owning area', () => {
    expect(gateForArea('mine').id).toBe('mine-wilds-pass')
    expect(gateForArea('ruins').unlockedDestination.biome).toBe('furnace')
    expect(gateForArea('furnace').unlockedDestination.biome).toBe('floodedRuins')
  })

  it('routes a gate to the persisted campaign successor', () => {
    const state = newRun(7, 'mine', 0, undefined, [], [], ['mine', 'furnace', 'cliffs', 'burial'])
    expect(gateForRun(state)?.unlockedDestination.biome).toBe('furnace')
    state.area = 'burial'
    expect(gateForRun(state)).toBeUndefined()
  })

  it('rejects impossible gate definitions', () => {
    expect(validateAreaGate({ ...gateForArea('mine'), tagAlternatives: [{ label: 'unknown', kind: 'tag', tags: ['unknown'] }] })).toContain('no possible gate alternative')
  })

  it('aligns practical gate work with the village pact and rites with the kami', () => {
    const mine = newRun(4, 'mine')
    mine.hero.bombs = 1
    mine.hero.gold = 20
    expect(resolveAreaGate(mine, gateForRun(mine)!, 2)).toMatchObject({ resolved: true, alignment: 'villagePact' })
    const cliffs = newRun(4, 'cliffs', 0, undefined, [], [], ['cliffs', 'burial', 'mine', 'wilds'])
    expect(resolveAreaGate(cliffs, gateForRun(cliffs)!, 1)).toMatchObject({ resolved: true, alignment: 'kami' })
  })
})
