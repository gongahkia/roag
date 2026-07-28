import { describe, expect, it } from 'vitest'
import { chooseEncounter } from './encounters'
import { createRun } from '../test/factories'

const social = { id: 'social:0:trader', faction: 'trailfolk' as const, role: 'trader' as const, goal: 'move surplus along a safe route', visibility: 'visible' as const, offer: 'supplyCache' as const, consequence: 'alliance' as const, disposition: 'neutral' as const }

describe('social encounters', () => {
  it('turns cooperation into a persistent route reward', () => {
    const state = createRun({ reputation: { trailfolk: 0, kami: 0 }, floor: createRun().floor })
    state.floor.encounters = [{ id: 'social-1', kind: 'wayfarer', x: 2, y: 1, state: 'dormant', social }]
    chooseEncounter(state, 'social-1', '1')
    expect(state.reputation).toEqual({ trailfolk: 1, kami: 0 })
    expect(state.hero.inventory).toContain('ropeBundle')
    expect(state.floor.encounters[0]?.social?.disposition).toBe('allied')
  })

  it('makes a social conflict explicit and hostile', () => {
    const state = createRun({ reputation: { trailfolk: 0, kami: 0 }, floor: createRun().floor })
    state.floor.encounters = [{ id: 'social-2', kind: 'wayfarer', x: 2, y: 1, state: 'dormant', social }]
    chooseEncounter(state, 'social-2', '2')
    expect(state.reputation).toEqual({ trailfolk: -1, kami: 0 })
    expect(state.floor.actors).toContainEqual(expect.objectContaining({ id: 'social-hostile:social-2', hostile: true }))
    expect(state.floor.encounters[0]?.social?.disposition).toBe('hostile')
  })
})
