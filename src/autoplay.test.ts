import { describe, expect, it } from 'vitest'
import { autoplayCommand } from './autoplay'
import { runAutoplay } from './autoplay-runner'
import { newRun } from './engine'

describe('autoplay', () => {
  it('does not mutate planning state and resolves level-up choices', () => {
    const state = newRun(71)
    const before = structuredClone(state)
    expect(autoplayCommand(state, 'visible')).toBeDefined()
    expect(state).toEqual(before)
    state.modal = { kind: 'skills', source: 'level' }
    expect(autoplayCommand(state, 'omniscient')).toBe('1')
  })

  it('keeps visible-only planning within explored terrain', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.items = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.floor.tiles.forEach(tile => { tile.explored = false; if (tile.kind === 'crate' || tile.kind === 'chest') tile.kind = 'floor' })
    state.floor.tiles[state.hero.y * 48 + state.hero.x].explored = true
    expect(autoplayCommand(state, 'visible')).toBeUndefined()
    expect(autoplayCommand(state, 'omniscient')).toBeDefined()
  })

  it('replays deterministically without mutating its input', () => {
    const state = newRun(913, 'mine')
    const before = structuredClone(state)
    const first = runAutoplay(state, { mode: 'omniscient', turnLimit: 120 })
    const second = runAutoplay(state, { mode: 'omniscient', turnLimit: 120 })
    expect(state).toEqual(before)
    expect(first.outcome).not.toBe('error')
    expect(first.commands.length).toBeGreaterThan(0)
    expect(first).toEqual(second)
  })
})
