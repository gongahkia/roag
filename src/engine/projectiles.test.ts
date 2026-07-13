import { describe, expect, it } from 'vitest'
import { advance } from './combat'
import { projectBolt } from './projectiles'
import { createEnemy, createRun } from '../test/factories'

describe('telegraphed projectiles', () => {
  it('exposes bolt path, collision, and cover', () => {
    const state = createRun()
    const clear = projectBolt(state.floor, { x: 5, y: 1 }, state.hero)
    expect(clear.cells).toEqual([{ x: 4, y: 1 }, { x: 3, y: 1 }, { x: 2, y: 1 }, { x: 1, y: 1 }])
    expect(clear).toMatchObject({ collision: { point: { x: 1, y: 1 }, by: 'target' }, cover: false })
    state.floor.tiles[1 * 48 + 3].kind = 'wall'
    expect(projectBolt(state.floor, { x: 5, y: 1 }, state.hero)).toMatchObject({ collision: { point: { x: 3, y: 1 }, by: 'wall' }, cover: true })
  })

  it('announces a bolt before its deterministic impact turn', () => {
    const state = createRun()
    state.floor.actors = [createEnemy({ id: 'sapper-1', ai: 'ranged', x: 5, y: 1, energy: 0 })]
    const health = state.hero.health
    advance(state, [])
    expect(state.hero.health).toBe(health)
    expect(state.floor.telegraphs).toMatchObject([{ sourceId: 'sapper-1', actionId: 'enemy-shot', resolveTurn: 2, collision: { point: { x: 1, y: 1 }, by: 'target' }, cover: false }])
    advance(state, [])
    expect(state.hero.health).toBeLessThan(health)
    expect(state.floor.telegraphs?.[0]?.resolveTurn).toBe(3)
  })
})
