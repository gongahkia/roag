import { describe, expect, it } from 'vitest'
import { advance, moveHero } from './combat'
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

  it('does not seal the only route to the exit with a charge', () => {
    const state = createRun()
    state.hero.x = 1
    state.hero.y = 1
    state.floor.exit = { x: 3, y: 1 }
    for (let y = 0; y < 35; y++) for (let x = 0; x < 48; x++) state.floor.tiles[y * 48 + x].kind = 'wall'
    for (let x = 1; x <= 3; x++) state.floor.tiles[48 + x].kind = x === 3 ? 'exit' : 'floor'
    const stag = createEnemy({ id: 'heartwood-route', kind: 'heartwood', name: 'Heartwood Stag', role: 'guardian', ai: 'guardian', x: 2, y: 1, health: 34, maxHealth: 52, speed: 100, energy: 0, guardianPhase: 'pressure' })
    state.floor.actors = [stag]
    state.floor.telegraphs = [{ id: 'heartwood-route:charge', sourceId: stag.id, actionId: 'heartwood-charge', cells: [{ x: 2, y: 1 }], danger: 'major', resolveTurn: 1 }]
    advance(state, [])
    expect(getTile(state.floor, 2, 1)?.kind).toBe('floor')
  })

  it('does not collapse a bridge tile that would split access to the exit', () => {
    const state = createRun()
    state.floor.actors = []
    state.hero.x = 1
    state.hero.y = 1
    state.floor.exit = { x: 3, y: 1 }
    for (let y = 0; y < 35; y++) for (let x = 0; x < 48; x++) state.floor.tiles[y * 48 + x].kind = 'wall'
    state.floor.tiles[48 + 1].kind = 'floor'
    state.floor.tiles[48 + 2].kind = 'crumble'
    state.floor.tiles[48 + 3].kind = 'exit'
    moveHero(state, 'e')
    expect(state.hero).toMatchObject({ x: 2, y: 1 })
    expect(getTile(state.floor, 2, 1)?.kind).toBe('floor')
  })
})
