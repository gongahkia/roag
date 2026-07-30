import { describe, expect, it } from 'vitest'
import { newRun } from './engine'
import { applyTerrainInteraction } from './terrain-interaction'

const fixture = () => { const state = newRun(7); state.hero.x = 1; state.hero.y = 1; state.floor.start = { x: 1, y: 1 }; state.floor.exit = { x: 3, y: 1 }; state.floor.tiles.forEach(tile => { tile.kind = 'floor' }); state.floor.tiles[1 * state.floor.width + 3]!.kind = 'exit'; return state }
describe('terrain interaction contract', () => {
  it('mutates only explicit targets and preserves serialization', () => {
    const state = fixture(); state.floor.tiles[1 * state.floor.width + 2]!.kind = 'breakwall'
    const result = applyTerrainInteraction(state.floor, state.hero, { x: 2, y: 1 }, 'stoneAdze')
    expect(result).toMatchObject({ applied: true, event: 'terrain-breach' })
    expect(result.floor.tiles[1 * result.floor.width + 2]?.kind).toBe('floor')
    expect(JSON.parse(JSON.stringify(result.floor)).tiles[1 * result.floor.width + 2].kind).toBe('floor')
  })
  it('rejects protected, invalid, and non-adjacent targets without mutating input', () => {
    const state = fixture(); state.floor.exit = { x: 2, y: 1 }; state.floor.tiles[1 * state.floor.width + 2]!.kind = 'exit'; state.floor.tiles[1 * state.floor.width + 3]!.kind = 'boulder'
    expect(applyTerrainInteraction(state.floor, state.hero, state.floor.exit, 'stoneAdze')).toMatchObject({ applied: false, reason: 'target is protected' })
    expect(applyTerrainInteraction(state.floor, state.hero, { x: 1, y: 2 }, 'stoneAdze')).toMatchObject({ applied: false, reason: 'unsupported terrain interaction' })
    expect(applyTerrainInteraction(state.floor, state.hero, { x: 5, y: 1 }, 'stoneAdze')).toMatchObject({ applied: false, reason: 'target is not adjacent' })
    expect(state.floor.tiles[1 * state.floor.width + 3]?.kind).toBe('boulder')
  })
})
