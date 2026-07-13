import { describe, expect, it } from 'vitest'
import { gateForArea, resolveAreaGate } from './gates'
import { createRun } from '../test/factories'

describe('Ruins gate solutions', () => {
  it('unlocks Ashen Ruins through NPC, ward-plus-astral, or relic alternatives', () => {
    const gate = gateForArea('caverns')
    const npc = createRun({ area: 'caverns', rescuedNpcs: [{ id: 'scout-1', name: 'Lost Scout', biome: 'caverns', floor: 1 }] })
    expect(resolveAreaGate(npc, gate, 0)).toMatchObject({ resolved: true, destination: 'ruins' })
    const wardAstral = createRun({ area: 'caverns' })
    wardAstral.hero.inventory = ['ward']
    wardAstral.hero.skills = ['astral1']
    expect(resolveAreaGate(wardAstral, gate, 1)).toMatchObject({ resolved: true, destination: 'ruins' })
    const relic = createRun({ area: 'caverns' })
    relic.hero.inventory = ['sunseal']
    expect(resolveAreaGate(relic, gate, 2)).toMatchObject({ resolved: true, destination: 'ruins' })
    expect(relic.hero.inventory).toEqual([])
  })

  it('rejects a ward route without an astral capability', () => {
    const state = createRun({ area: 'caverns' })
    state.hero.inventory = ['ward']
    expect(resolveAreaGate(state, gateForArea('caverns'), 1).resolved).toBe(false)
  })
})
