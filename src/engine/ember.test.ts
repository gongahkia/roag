import { describe, expect, it } from 'vitest'
import { castSpell } from './inventory'
import { createEnemy, createHero, createRun } from '../test/factories'
import { getTile } from '../world'

describe('Ember', () => {
  it('burns direct targets', () => {
    const target = createEnemy({ x: 2, y: 1, health: 30, maxHealth: 30, speed: 0 })
    const state = createRun({ hero: createHero({ inventory: ['ember'] }) })
    state.floor.actors = [target]

    castSpell(state, 'ember', 'e')

    expect(target.health).toBeLessThan(20)
    expect(target.conditions).toContainEqual({ kind: 'burning', duration: 1, potency: 2 })
  })

  it('combusts clear floor into a fire vent', () => {
    const state = createRun({ hero: createHero({ inventory: ['ember'] }) })

    castSpell(state, 'ember', 'e')

    expect(getTile(state.floor, 2, 1)?.kind).toBe('fireVent')
  })

  it('ignites gas and volatile terrain into damaging blasts', () => {
    for (const kind of ['gas', 'crate'] as const) {
      const target = createEnemy({ x: 3, y: 1, health: 30, maxHealth: 30, speed: 0 })
      const state = createRun({ hero: createHero({ inventory: ['ember'] }) })
      state.floor.tiles[1 * 48 + 2].kind = kind
      state.floor.actors = [target]

      castSpell(state, 'ember', 'e')

      expect(getTile(state.floor, 2, 1)?.kind).toBe('floor')
      expect(target.health).toBeLessThan(30)
    }
  })
})
