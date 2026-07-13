import { describe, expect, it } from 'vitest'
import { planEnemyIntent } from './intents'
import { createEnemy, createRun } from '../test/factories'

describe('enemy intent planner', () => {
  it('selects readable actions from range, terrain, and phase', () => {
    const state = createRun()
    const ranged = createEnemy({ ai: 'ranged', x: 5, y: 1 })
    expect(planEnemyIntent(state, ranged)).toMatchObject({ action: { id: 'enemy-shot' }, phase: 'opening', reason: 'clear line at range 4' })
    const guardian = createEnemy({ role: 'guardian', ai: 'guardian', x: 2, y: 1, health: 1, maxHealth: 9 })
    expect(planEnemyIntent(state, guardian)).toMatchObject({ action: { id: 'guardian-slam' }, phase: 'desperate' })
    state.floor.tiles[1 * 48 + 5].kind = 'gas'
    expect(planEnemyIntent(state, ranged)).toMatchObject({ action: { id: 'enemy-reposition' }, reason: 'escaping gas' })
  })

  it('produces an action for every hostile enemy on a generated floor', () => {
    const state = createRun()
    state.floor.actors = [createEnemy({ x: 2, y: 1 }), createEnemy({ id: 'ranged', ai: 'ranged', x: 6, y: 1 }), createEnemy({ id: 'wanderer', ai: 'wander', x: 12, y: 1 })]
    for (const actor of state.floor.actors) expect(planEnemyIntent(state, actor).action.owner).toBe('enemy')
  })
})
