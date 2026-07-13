import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createEnemy, createFloor, createRun } from '../test/factories'
import { operate } from './inventory'
import { recordRescue } from './rescue'

describe('rescued NPC roster', () => {
  it('records each scout once and removes its map actor', () => {
    const scout = createEnemy({ id: 'scout-1', role: 'ally', kind: 'ally', name: 'lost scout', hostile: false, x: 1, y: 1 })
    const state = createRun({ area: 'wilds', areaFloor: 1, floor: createFloor({ index: 5, biome: 'wilds', actors: [scout] }) })
    state.floor.tiles[indexOf(1, 1)].kind = 'rescue'

    expect(operate(state)).toContainEqual({ type: 'rescue' })
    expect(state.rescuedNpcs).toEqual([{ id: 'rescue:wilds:5:scout-1', name: 'lost scout', biome: 'wilds', floor: 1 }])
    expect(state.floor.actors).toEqual([])
    recordRescue(state, scout)
    expect(state.rescuedNpcs).toHaveLength(1)
  })
})
