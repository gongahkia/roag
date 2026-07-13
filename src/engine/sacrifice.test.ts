import { describe, expect, it } from 'vitest'
import { createRun } from '../test/factories'
import { gateForArea, resolveAreaGate } from './gates'

describe('NPC sacrifice gates', () => {
  it('consumes one rescued NPC, records lineage, and leaves another route available', () => {
    const state = createRun({ area: 'mine', areaFloor: 1, rescuedNpcs: [{ id: 'scout-1', name: 'Lost Scout', biome: 'mine', floor: 1 }] })
    const gate = gateForArea('mine')

    expect(resolveAreaGate(state, gate, 0)).toMatchObject({ resolved: true, sacrificedNpc: { id: 'scout-1' }, lineageEvent: { kind: 'npcSacrifice', npcId: 'scout-1', gateId: 'mine-wilds-pass' } })
    expect(state.rescuedNpcs).toEqual([])
    expect(state.lineageEvents).toEqual([{ id: 'sacrifice:mine-wilds-pass:scout-1', kind: 'npcSacrifice', npcId: 'scout-1', npcName: 'Lost Scout', biome: 'mine', floor: 1, gateId: 'mine-wilds-pass', seed: 1 }])

    const fallback = createRun({ area: 'mine' })
    fallback.hero.inventory = ['ember']
    fallback.hero.gold = 20
    expect(resolveAreaGate(fallback, gate, 0)).toMatchObject({ resolved: false })
    expect(resolveAreaGate(fallback, gate, 1)).toMatchObject({ resolved: true, destination: 'wilds' })
  })
})
