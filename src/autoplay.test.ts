import { describe, expect, it } from 'vitest'
import { autoplayCommand, autoplayDecision, createAutoplayContext } from './autoplay'
import { runAutoplay } from './autoplay-runner'
import { newRun, skillChoices } from './engine'

describe('autoplay', () => {
  it('does not mutate planning state and resolves level-up choices', () => {
    const state = newRun(71)
    const before = structuredClone(state)
    expect(autoplayCommand(state, 'visible')).toBeDefined()
    expect(state).toEqual(before)
    state.modal = { kind: 'skills', source: 'level' }
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(skillChoices(state).map((_, index) => String(index + 1))).toContain(decision?.command)
  })

  it('keeps visible-only planning within explored terrain', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.items = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.floor.tiles.forEach(tile => { tile.explored = false; if (tile.kind === 'crate' || tile.kind === 'chest') tile.kind = 'floor' })
    state.floor.tiles[state.hero.y * 48 + state.hero.x].explored = true
    expect(autoplayCommand(state, 'visible')).toBe('l')
    expect(autoplayCommand(state, 'omniscient')).toBeDefined()
  })

  it('completes the Mine reference run using tactical actions', () => {
    const report = runAutoplay(newRun(7, 'mine'), { mode: 'omniscient', policy: 'clear', turnLimit: 800 })
    expect(report.outcome).toBe('complete')
    expect(report.trace.some(entry => entry.reason.includes('bomb tactical cluster'))).toBe(true)
    expect(report.trace.some(entry => entry.reason.startsWith('throw:') || entry.reason.startsWith('cast:'))).toBe(true)
  }, 12_000)

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
