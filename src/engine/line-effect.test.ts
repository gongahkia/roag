import { describe, expect, it } from 'vitest'
import { resolveLineEffect } from './line-effect'
import { createEnemy, createFloor } from '../test/factories'

describe('line-of-effect resolution', () => {
  it('stops before walls and closed doors', () => {
    const floor = createFloor()
    floor.tiles[1 * 48 + 3].kind = 'wall'
    expect(resolveLineEffect(floor, { x: 1, y: 1 }, { x: 5, y: 1 })).toMatchObject({ cells: [{ x: 2, y: 1 }], blocked: { point: { x: 3, y: 1 }, by: 'wall' } })
    floor.tiles[1 * 48 + 3].kind = 'door'
    expect(resolveLineEffect(floor, { x: 1, y: 1 }, { x: 5, y: 1 }).blocked?.by).toBe('door')
  })

  it('includes the first actor and records terrain modifiers before it', () => {
    const floor = createFloor({ actors: [createEnemy({ x: 4, y: 1 })] })
    floor.tiles[1 * 48 + 2].kind = 'water'
    const effect = resolveLineEffect(floor, { x: 1, y: 1 }, { x: 6, y: 1 })
    expect(effect.cells).toEqual([{ x: 2, y: 1 }, { x: 3, y: 1 }, { x: 4, y: 1 }])
    expect(effect.modifiers).toEqual([{ point: { x: 2, y: 1 }, modifier: 'dampened' }])
    expect(effect.blocked).toEqual({ point: { x: 4, y: 1 }, by: 'actor' })
  })
})
