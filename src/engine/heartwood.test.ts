import { describe, expect, it } from 'vitest'
import { advance } from './combat'
import { createEnemy, createRun } from '../test/factories'
import { getTile } from '../world'

describe('Heartwood Stag encounter', () => {
  it('transitions through Wilds terrain and pressures movement with bramble charges', () => {
    const state = createRun()
    const stag = createEnemy({ id: 'heartwood-1', kind: 'heartwood', name: 'Heartwood Stag', role: 'guardian', ai: 'guardian', x: 4, y: 1, health: 34, maxHealth: 52, speed: 100, energy: 0, guardianPhase: 'opening' })
    state.floor.actors = [stag]
    advance(state, [])
    expect(stag.guardianPhase).toBe('pressure')
    expect(state.floor.telegraphs).toMatchObject([{ actionId: 'heartwood-charge', sourceId: 'heartwood-1' }])

    advance(state, [])
    expect(state.hero).toMatchObject({ x: 0, y: 1 })
    expect(getTile(state.floor, 1, 1)?.kind).toBe('bramble')

    const cataclysm = createRun()
    const wounded = createEnemy({ id: 'heartwood-2', kind: 'heartwood', name: 'Heartwood Stag', role: 'guardian', ai: 'guardian', x: 4, y: 1, health: 17, maxHealth: 52, speed: 100, energy: 0, guardianPhase: 'pressure' })
    cataclysm.floor.actors = [wounded]
    advance(cataclysm, [])
    expect(wounded.guardianPhase).toBe('cataclysm')
    expect(cataclysm.floor.tiles.some(tile => tile.kind === 'water')).toBe(true)
  })
})
