import { describe, expect, it } from 'vitest'
import { announceTelegraph, resolveTelegraphs } from './telegraphs'
import { createRun } from '../test/factories'

describe('telegraph scheduler', () => {
  it('announces, persists, resolves, and clears actions on deterministic turns', () => {
    const state = createRun({ turn: 4 })
    const telegraph = announceTelegraph(state, { id: 'shot-1', sourceId: 'sapper-1', actionId: 'enemy-shot', cells: [{ x: 2, y: 1 }], danger: 'major', windup: 2 })
    expect(telegraph.resolveTurn).toBe(6)
    expect(state.floor.telegraphs).toHaveLength(1)
    state.turn = 5
    expect(resolveTelegraphs(state)).toEqual([])
    expect(state.floor.telegraphs).toHaveLength(1)
    state.turn = 6
    expect(resolveTelegraphs(state).map(action => action.id)).toEqual(['shot-1'])
    expect(state.floor.telegraphs).toEqual([])
  })

  it('rejects invalid windups and duplicate telegraph ids', () => {
    const state = createRun()
    const plan = { id: 'slam-1', sourceId: 'foreman-1', actionId: 'guardian-slam', cells: [], danger: 'major' as const, windup: 1 }
    announceTelegraph(state, plan)
    expect(() => announceTelegraph(state, plan)).toThrow('duplicate telegraph')
    expect(() => announceTelegraph(state, { ...plan, id: 'invalid', windup: 0 })).toThrow('invalid telegraph windup')
  })
})
