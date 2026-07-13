import { describe, expect, it } from 'vitest'
import { advance, moveHero } from './combat'
import { createEnemy, createRun } from '../test/factories'
import { getTile } from '../world'

describe('Geode Wyrm encounter', () => {
  it('transitions through Cavern hazards and seals a telegraphed fissure line', () => {
    const state = createRun()
    const wyrm = createEnemy({ id: 'geode-1', kind: 'geode', name: 'Geode Wyrm', role: 'guardian', ai: 'guardian', x: 5, y: 1, health: 41, maxHealth: 62, speed: 100, energy: 0, guardianPhase: 'opening' })
    state.floor.actors = [wyrm]
    advance(state, [])
    expect(wyrm.guardianPhase).toBe('pressure')
    expect(state.floor.tiles.some(tile => tile.kind === 'gas')).toBe(true)
    expect(state.floor.telegraphs).toMatchObject([{ actionId: 'geode-fissure', sourceId: 'geode-1' }])

    moveHero(state, 's')
    expect(getTile(state.floor, 1, 1)?.kind).toBe('fireVent')
    expect(getTile(state.floor, 3, 1)?.kind).toBe('fireVent')

    const cataclysm = createRun()
    const wounded = createEnemy({ id: 'geode-2', kind: 'geode', name: 'Geode Wyrm', role: 'guardian', ai: 'guardian', x: 5, y: 1, health: 20, maxHealth: 62, speed: 100, energy: 0, guardianPhase: 'pressure' })
    cataclysm.floor.actors = [wounded]
    advance(cataclysm, [])
    expect(wounded.guardianPhase).toBe('cataclysm')
    expect(cataclysm.floor.tiles.some(tile => tile.kind === 'lava')).toBe(true)
  })
})
