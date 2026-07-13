import { describe, expect, it } from 'vitest'
import { resolveDisplacement } from './displacement'
import { createEnemy, createRun } from '../test/factories'

describe('displacement resolution', () => {
  it('pushes, pulls, and swaps without overlapping actors', () => {
    const state = createRun()
    const enemy = createEnemy({ x: 3, y: 1 })
    state.floor.actors = [enemy]
    expect(resolveDisplacement(state, state.hero, enemy, 'push')).toMatchObject({ moved: true, to: { x: 4, y: 1 } })
    expect(resolveDisplacement(state, state.hero, enemy, 'pull')).toMatchObject({ moved: true, to: { x: 3, y: 1 } })
    expect(resolveDisplacement(state, state.hero, enemy, 'swap')).toMatchObject({ moved: true, to: { x: 1, y: 1 } })
    expect(state.hero).toMatchObject({ x: 3, y: 1 })
    expect(state.messages[0]).toContain('swap')
  })

  it('blocks walls and occupants, while hazards damage displaced targets', () => {
    const state = createRun()
    const enemy = createEnemy({ x: 3, y: 1 })
    state.floor.actors = [enemy, createEnemy({ id: 'other', x: 4, y: 1 })]
    expect(resolveDisplacement(state, state.hero, enemy, 'knockback')).toMatchObject({ moved: false, blocked: 'occupant' })
    state.floor.actors = [enemy]
    state.floor.tiles[1 * 48 + 4].kind = 'wall'
    expect(resolveDisplacement(state, state.hero, enemy, 'push')).toMatchObject({ moved: false, blocked: 'wall' })
    state.floor.tiles[1 * 48 + 4].kind = 'lava'
    expect(resolveDisplacement(state, state.hero, enemy, 'push')).toMatchObject({ moved: true, hazard: 'lava' })
    expect(enemy).toMatchObject({ x: 4, y: 1, health: 0 })
  })
})
