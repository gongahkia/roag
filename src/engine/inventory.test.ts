import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createHero, createRun } from '../test/factories'
import { inventoryChoice, operate, throwItem } from './inventory'

describe('player-left items', () => {
  it('remain visible through fog after dropping or throwing', () => {
    const dropped = createRun({ hero: createHero({ inventory: ['rock'] }) })
    inventoryChoice(dropped, { kind: 'inventory', mode: 'drop' }, '1')
    expect(dropped.floor.items[0]).toMatchObject({ id: 'rock', visibleInFog: true })

    const thrown = createRun({ hero: createHero({ inventory: ['rock'] }) })
    throwItem(thrown, 'rock', 'e')
    expect(thrown.floor.items[0]).toMatchObject({ id: 'rock', visibleInFog: true })
  })

  it('operates an occupied altar from an adjacent tile', () => {
    const state = createRun({ hero: createHero({ gold: 75 }) })
    state.floor.objective = { id: 'altar', kind: 'invokeAltar', label: 'Make a shrine offering', status: 'active' }
    state.floor.tiles[indexOf(2, 1)].kind = 'altar'
    state.floor.actors.push({ id: 'keeper', role: 'ally', kind: 'keeper', name: 'shrine keeper', x: 2, y: 1, health: 1, maxHealth: 1, attack: 0, defense: 0, speed: 0, energy: 0, glyph: '_', color: '#fff', hostile: false })
    expect(operate(state).map(event => event.type)).toEqual(['spell'])
    expect(state.floor.objective.status).toBe('complete')
  })

  it('uses a carried key for an ordinary locked door before opening an area gate', () => {
    const state = createRun({ hero: createHero({ keys: 1 }) })
    state.floor.tiles[indexOf(2, 1)].kind = 'lockedDoor'
    expect(operate(state).map(event => event.type)).toEqual(['gateResolved'])
    expect(state.hero.keys).toBe(0)
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('floor')
    expect(state.modal).toBeUndefined()
  })
})
