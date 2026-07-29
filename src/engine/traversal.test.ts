import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createHero, createRun } from '../test/factories'
import { perform } from './input'

describe('traversal supplies', () => {
  it('breaches adjacent blocked ground only after confirming a target', () => {
    const state = createRun({ hero: createHero({ inventory: ['auger'] }) })
    state.floor.tiles[indexOf(2, 1)].kind = 'rubble'
    perform(state, 'u')
    perform(state, '1')
    expect(state.modal).toMatchObject({ kind: 'target', action: 'drill' })
    perform(state, ';')
    perform(state, 'Enter')
    expect(state.floor.tiles[indexOf(2, 1)].kind).toBe('floor')
    expect(state.hero.inventory).toEqual([])
    expect(state.turn).toBe(1)
  })

  it('crosses one hazardous tile without consuming an invalid glider target', () => {
    const invalid = createRun({ hero: createHero({ inventory: ['reedGlider'] }) })
    invalid.floor.tiles[indexOf(2, 1)].kind = 'pit'
    invalid.floor.tiles[indexOf(3, 1)].kind = 'wall'
    perform(invalid, 'u')
    perform(invalid, '1')
    perform(invalid, ';')
    perform(invalid, 'Enter')
    expect(invalid.hero.inventory).toEqual(['reedGlider'])
    expect(invalid.turn).toBe(0)

    const state = createRun({ hero: createHero({ inventory: ['reedGlider'] }) })
    state.floor.tiles[indexOf(2, 1)].kind = 'pit'
    perform(state, 'u')
    perform(state, '1')
    perform(state, ';')
    perform(state, 'Enter')
    expect(state.hero).toMatchObject({ x: 3, y: 1 })
    expect(state.hero.inventory).toEqual([])
    expect(state.turn).toBe(1)
  })
})
