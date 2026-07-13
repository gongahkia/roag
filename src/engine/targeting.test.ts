import { describe, expect, it } from 'vitest'
import { perform } from './input'
import { targetPreview } from './targeting'
import { createRun } from '../test/factories'

describe('player targeting preview', () => {
  it('previews bounded throw paths and bomb cells before commitment', () => {
    const state = createRun()
    const thrown = targetPreview(state, { kind: 'target', action: 'throw', item: 'rock', direction: 'e' })
    expect(thrown.path).toEqual([{ x: 2, y: 1 }, { x: 3, y: 1 }, { x: 4, y: 1 }, { x: 5, y: 1 }, { x: 6, y: 1 }])
    expect(thrown.cells).toEqual([{ x: 6, y: 1 }])
    expect(targetPreview(state, { kind: 'target', action: 'bomb', direction: 'e' }).cells).toContainEqual({ x: 2, y: 1 })
  })

  it('requires confirmation after choosing a target direction', () => {
    const state = createRun()
    state.modal = { kind: 'target', action: 'bomb' }
    expect(perform(state, 'ArrowRight')).toEqual([{ type: 'menu' }])
    expect(state.modal).toMatchObject({ kind: 'target', direction: 'e' })
    expect(state.hero.bombs).toBe(0)
    state.hero.bombs = 1
    expect(perform(state, 'Enter')).toEqual(expect.arrayContaining([{ type: 'boom' }]))
    expect(state.modal).toBeUndefined()
  })
})
