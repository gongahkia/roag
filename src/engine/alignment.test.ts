import { describe, expect, it } from 'vitest'
import { createHero, createRun } from '../test/factories'
import { perform } from './input'
import { deliveryEndingFor } from './alignment'

describe('Voyager doctrine alignment', () => {
  it('records a pragmatism event and emits its exact log line', () => {
    const state = createRun({ hero: createHero({ gold: 40 }) })
    state.floor.encounters = [{ id: 'mine-pact', kind: 'minePact', x: 1, y: 1, state: 'dormant' }]
    perform(state, 'c'); perform(state, '1')
    expect(state.alignment).toEqual({ kami: 0, villagePact: 1 })
    expect(state.messages[0]).toBe('Your report favors pragmatism.')
    expect(state.hero.inventory).toContain('bombPack')
  })

  it('records an idealism event and resolves all New Edo endings at threshold four', () => {
    const state = createRun()
    state.floor.encounters = [{ id: 'mine-kami', kind: 'mineKami', x: 1, y: 1, state: 'dormant' }]
    perform(state, 'c'); perform(state, '2')
    expect(state.alignment).toEqual({ kami: 1, villagePact: 0 })
    expect(state.messages[0]).toBe('Your report favors idealism.')
    expect(deliveryEndingFor({ kami: 3, villagePact: 3 })).toBe('plain')
    expect(deliveryEndingFor({ kami: 4, villagePact: 3 })).toBe('kami')
    expect(deliveryEndingFor({ kami: 3, villagePact: 4 })).toBe('villagePact')
    expect(deliveryEndingFor({ kami: 4, villagePact: 4 })).toBe('both')
  })
})
