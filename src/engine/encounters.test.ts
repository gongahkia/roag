import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createHero, createRun } from '../test/factories'
import { perform } from './input'

const encounter = (kind: 'wayfarer' | 'bloodBargain' | 'shiftingChamber') => ({ id: `${kind}-1`, kind, x: 1, y: 1, state: 'dormant' as const })

describe('optional floor encounters', () => {
  it('trades with a wayfarer only after the player selects a priced option', () => {
    const state = createRun({ hero: createHero({ gold: 35 }) })
    state.floor.encounters = [encounter('wayfarer')]
    perform(state, 'c')
    expect(state.modal).toMatchObject({ kind: 'encounter' })
    perform(state, '1')
    expect(state.hero.gold).toBe(0)
    expect(state.hero.inventory).toHaveLength(1)
    expect(state.floor.encounters[0].state).toBe('resolved')
    expect(state.telemetry?.eventOutcomes['wayfarer:trade']).toBe(1)
  })

  it('leaves a risky bargain untouched without forcing a cost', () => {
    const state = createRun()
    state.floor.encounters = [encounter('bloodBargain')]
    perform(state, 'c'); perform(state, '3')
    expect(state.hero).toMatchObject({ maxHealth: 22, focus: 8, gold: 0 })
    expect(state.floor.encounters[0].state).toBe('resolved')
    expect(state.telemetry?.eventOutcomes['bloodBargain:decline']).toBe(1)
  })

  it('opens a mutable chamber by converting nearby blockers to floor', () => {
    const state = createRun({ hero: createHero({ focus: 3 }) })
    state.floor.encounters = [encounter('shiftingChamber')]
    state.floor.tiles[indexOf(2, 1)].kind = 'rubble'
    perform(state, 'c'); perform(state, '1')
    expect(state.hero.focus).toBe(1)
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('floor')
    expect(state.telemetry?.eventOutcomes['shiftingChamber:open']).toBe(1)
  })
})
