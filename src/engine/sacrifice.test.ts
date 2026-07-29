import { describe, expect, it } from 'vitest'
import { createRun } from '../test/factories'
import { gateForArea, resolveAreaGate } from './gates'

describe('NPC sacrifice gates', () => {
  it('consumes one rescued NPC for this run and leaves another route available', () => {
    const state = createRun({ area: 'mine', areaFloor: 1, rescuedNpcs: [{ id: 'scout-1', name: 'Lost Scout', biome: 'mine', floor: 1 }] })
    const gate = gateForArea('mine')

    const resolution = resolveAreaGate(state, gate, 0)
    expect(resolution).toMatchObject({ resolved: true, sacrificedNpc: { id: 'scout-1' } })
    expect(resolution.lineageEvent).toBeUndefined()
    expect(state.rescuedNpcs).toEqual([])
    expect(state.lineageEvents ?? []).toEqual([])

    const fallback = createRun({ area: 'mine' })
    fallback.hero.inventory = ['ember']
    fallback.hero.gold = 20
    expect(resolveAreaGate(fallback, gate, 0)).toMatchObject({ resolved: false })
    expect(resolveAreaGate(fallback, gate, 1)).toMatchObject({ resolved: true, destination: 'wilds' })
  })
})
