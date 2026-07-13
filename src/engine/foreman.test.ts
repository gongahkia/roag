import { describe, expect, it } from 'vitest'
import { advance, moveHero } from './combat'
import { createEnemy, createRun } from '../test/factories'
import { getTile } from '../world'

describe('Foreman encounter', () => {
  it('transitions onto rail and telegraphs cave-ins that crumble ground', () => {
    const state = createRun()
    const foreman = createEnemy({ id: 'foreman-1', kind: 'foreman', name: 'The Foreman', role: 'guardian', ai: 'guardian', x: 4, y: 1, health: 28, maxHealth: 42, speed: 100, energy: 0, guardianPhase: 'opening' })
    state.floor.actors = [foreman]
    advance(state, [])
    expect(foreman.guardianPhase).toBe('pressure')
    expect(getTile(state.floor, 4, 1)?.kind).toBe('rail')
    expect(state.floor.telegraphs).toMatchObject([{ actionId: 'foreman-cavein', sourceId: 'foreman-1' }])

    moveHero(state, 'e')
    expect(getTile(state.floor, 1, 1)?.kind).toBe('crumble')

    foreman.health = 14
    advance(state, [])
    expect(foreman.guardianPhase).toBe('cataclysm')
    expect(state.floor.tiles.some(tile => tile.kind === 'crumble')).toBe(true)
  })
})
