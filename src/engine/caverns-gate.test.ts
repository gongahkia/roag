import { describe, expect, it } from 'vitest'
import { gateForArea, resolveAreaGate } from './gates'
import { createRun } from '../test/factories'

describe('Caverns gate solutions', () => {
  it('unlocks Glass Caverns through NPC, light-plus-rope, or mobility', () => {
    const gate = gateForArea('wilds')
    const npc = createRun({ area: 'wilds', rescuedNpcs: [{ id: 'scout-1', name: 'Lost Scout', biome: 'wilds', floor: 1 }] })
    expect(resolveAreaGate(npc, gate, 0)).toMatchObject({ resolved: true, destination: 'caverns' })
    const lightRope = createRun({ area: 'wilds' })
    lightRope.hero.inventory = ['lantern', 'ropeBundle']
    expect(resolveAreaGate(lightRope, gate, 1)).toMatchObject({ resolved: true, destination: 'caverns' })
    const mobility = createRun({ area: 'wilds' })
    mobility.hero.inventory = ['blinkRune']
    expect(resolveAreaGate(mobility, gate, 2)).toMatchObject({ resolved: true, destination: 'caverns' })
  })

  it('requires both light and rope for the combined alternative', () => {
    const state = createRun({ area: 'wilds' })
    state.hero.inventory = ['lantern']
    expect(resolveAreaGate(state, gateForArea('wilds'), 1).resolved).toBe(false)
  })
})
