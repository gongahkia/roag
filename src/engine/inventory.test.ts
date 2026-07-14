import { describe, expect, it } from 'vitest'
import { createHero, createRun } from '../test/factories'
import { inventoryChoice, throwItem } from './inventory'

describe('player-left items', () => {
  it('remain visible through fog after dropping or throwing', () => {
    const dropped = createRun({ hero: createHero({ inventory: ['rock'] }) })
    inventoryChoice(dropped, { kind: 'inventory', mode: 'drop' }, '1')
    expect(dropped.floor.items[0]).toMatchObject({ id: 'rock', visibleInFog: true })

    const thrown = createRun({ hero: createHero({ inventory: ['rock'] }) })
    throwItem(thrown, 'rock', 'e')
    expect(thrown.floor.items[0]).toMatchObject({ id: 'rock', visibleInFog: true })
  })
})
