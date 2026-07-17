import { describe, expect, it } from 'vitest'
import { advance } from './combat'
import { advanceGuardianPhase, arenaPhaseFor } from './guardians'
import { createEnemy, createRun } from '../test/factories'

describe('guardian phase machine', () => {
  it('transitions through health and arena phases with deterministic hazards', () => {
    const state = createRun()
    const guardian = createEnemy({ role: 'guardian', ai: 'guardian', maxHealth: 90, health: 90, guardianPhase: 'opening' })
    state.floor.actors = [guardian]
    guardian.health = 60
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ from: 'opening', to: 'pressure', arena: 'hazard', tile: 'gas' })
    guardian.health = 30
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ from: 'pressure', to: 'cataclysm', arena: 'collapse', tile: 'fireVent' })
    expect(state.floor.tiles.some(tile => tile.kind === 'gas')).toBe(true)
    expect(state.floor.tiles.some(tile => tile.kind === 'fireVent')).toBe(true)
    expect(arenaPhaseFor('cataclysm')).toBe('collapse')
  })

  it('telegraphs final-phase guardian slams before impact', () => {
    const state = createRun()
    const guardian = createEnemy({ id: 'foreman-1', role: 'guardian', ai: 'guardian', x: 2, y: 1, maxHealth: 90, health: 20, guardianPhase: 'cataclysm', energy: 0 })
    state.floor.actors = [guardian]
    advance(state, [])
    expect(state.floor.telegraphs).toMatchObject([{ actionId: 'guardian-slam', sourceId: 'foreman-1', resolveTurn: 2 }])
  })

  it('does not seal the only hero-to-exit route with guardian terrain', () => {
    const state = createRun()
    state.hero.x = 1
    state.hero.y = 1
    state.floor.exit = { x: 3, y: 1 }
    for (let y = 0; y < 35; y++) for (let x = 0; x < 48; x++) state.floor.tiles[y * 48 + x].kind = 'wall'
    for (let x = 1; x <= 3; x++) state.floor.tiles[48 + x].kind = x === 3 ? 'exit' : 'floor'
    const guardian = createEnemy({ id: 'heartwood-1', kind: 'heartwood', role: 'guardian', ai: 'guardian', x: 2, y: 1, maxHealth: 90, health: 60, guardianPhase: 'opening' })
    state.floor.actors = [guardian]
    advanceGuardianPhase(state, guardian)
    expect(state.floor.tiles[48 + 2].kind).toBe('floor')
  })
})
