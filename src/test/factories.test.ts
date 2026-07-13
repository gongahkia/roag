import { describe, expect, it } from 'vitest'
import { createArea, createEnemy, createGate, createHero, createLegacy, createRun } from './factories'

describe('deterministic game-state factories', () => {
  it('creates stable area, hero, enemy, gate, and legacy fixtures', () => {
    expect(createArea()).toEqual(createArea())
    expect(createHero({ gold: 9 }).gold).toBe(9)
    expect(createEnemy({ kind: 'foreman' }).kind).toBe('foreman')
    expect(createGate({ state: 'open' }).state).toBe('open')
    expect(createLegacy({ heirName: 'Bea' }).heirName).toBe('Bea')
  })

  it('composes independent playable run fixtures', () => {
    const first = createRun()
    const second = createRun()
    first.hero.inventory.push('tonic')
    expect(second.hero.inventory).toEqual([])
    expect(first.floor.tiles[first.floor.exit.y * 48 + first.floor.exit.x].kind).toBe('exit')
  })
})
