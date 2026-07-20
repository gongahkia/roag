import { describe, expect, it } from 'vitest'
import { advance } from './combat'
import { projectBolt } from './projectiles'
import { applyPropEffects } from './props'
import { propDefinition } from '../props'
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

  it('revalidates announced shots against new cover without changing other telegraphs', () => {
    const covered = createRun()
    covered.floor.actors = [createEnemy({ id: 'sapper-1', ai: 'ranged', x: 5, y: 1, energy: 0 })]
    advance(covered, [])
    const health = covered.hero.health
    const statue = propDefinition('ruins.brokenStatue')
    covered.floor.props = [{ id: 'cover', kind: statue.id, biome: statue.biome, x: 3, y: 1, state: 'activated', tags: [...statue.tags], hooks: [...statue.hooks] }]
    advance(covered, [])
    expect(covered.hero.health).toBe(health)
    expect(covered.messages).toContain('The shot is stopped by cover.')

    const rooted = createRun()
    rooted.floor.actors = [createEnemy({ id: 'sapper-2', ai: 'ranged', x: 5, y: 1, energy: 0 })]
    advance(rooted, [])
    const shrine = propDefinition('wilds.rootShrine')
    rooted.floor.tiles[0 * 48 + 2].kind = 'wall'
    rooted.floor.tiles[0 * 48 + 3].kind = 'wall'
    rooted.floor.tiles[0 * 48 + 4].kind = 'wall'
    rooted.floor.props = [{ id: 'roots', kind: shrine.id, biome: shrine.biome, x: 3, y: 1, state: 'dormant', tags: [...shrine.tags], hooks: [...shrine.hooks] }]
    applyPropEffects(rooted, [{ x: 3, y: 1 }], ['root'])
    const rootHealth = rooted.hero.health
    advance(rooted, [])
    expect(rooted.hero.health).toBe(rootHealth)

    const fire = createRun()
    fire.hero.stats.agility = 0
    fire.floor.actors = [createEnemy({ id: 'fire-source', x: 5, y: 1, ai: 'chase', energy: 0 })]
    fire.floor.props = [{ id: 'cover', kind: statue.id, biome: statue.biome, x: 3, y: 1, state: 'activated', tags: [...statue.tags], hooks: [...statue.hooks] }]
    fire.floor.telegraphs = [{ id: 'fire', sourceId: 'fire-source', actionId: 'enemy-fire', cells: [{ x: 1, y: 1 }], danger: 'minor', resolveTurn: 1, collision: { point: { x: 1, y: 1 }, by: 'target' } }]
    const fireHealth = fire.hero.health
    advance(fire, [])
    expect(fire.hero.health).toBe(fireHealth)
    expect(fire.floor.tiles[1 * 48 + 1].kind).toBe('fireVent')

    const missing = createRun()
    missing.floor.telegraphs = [{ id: 'missing-shot', sourceId: 'missing', actionId: 'enemy-shot', cells: [{ x: 1, y: 1 }], danger: 'major', resolveTurn: 1, collision: { point: { x: 1, y: 1 }, by: 'target' } }]
    const missingHealth = missing.hero.health
    advance(missing, [])
    expect(missing.hero.health).toBe(missingHealth)
    expect(missing.messages).toContain('The abandoned shot fizzles.')
  })
})
