import { describe, expect, it } from 'vitest'
import { autoplayCommand, autoplayDecision, createAutoplayContext, recordAutoplayTransition } from './autoplay'
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

  it('does not route to or attempt non-currency loot with a full pack', () => {
    const state = newRun(71)
    state.hero.inventory = Array.from({ length: 12 }, () => 'rock')
    state.floor.items = [{ id: 'tonic', x: state.hero.x, y: state.hero.y, count: 1 }, { id: 'fireJar', x: state.hero.x + 2, y: state.hero.y, count: 1 }]
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(decision?.command).not.toBe('g')
    expect(decision?.candidates.some(candidate => candidate.reason === 'pickup:tonic')).toBe(false)
    expect(decision?.candidates.some(candidate => candidate.reason === 'reach loot')).toBe(false)
  })

  it('still collects gold with a full pack', () => {
    const state = newRun(71)
    state.hero.inventory = Array.from({ length: 12 }, () => 'rock')
    state.floor.items = [{ id: 'gold', x: state.hero.x, y: state.hero.y, count: 7 }]
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe('g')
  })

  it('halts strategic loops even when volatile actor state masks exact repeats', () => {
    const context = createAutoplayContext()
    let state = newRun(71)
    let decision = autoplayDecision(state, 'omniscient', 'clear', context)
    for (let turn = 0; turn < 24 && decision; turn++) {
      const next = structuredClone(state)
      next.turn++
      next.floor.actors[0].energy = turn + 1
      recordAutoplayTransition(context, state, 'l', next)
      state = next
      decision = autoplayDecision(state, 'omniscient', 'clear', context)
    }
    expect(decision).toBeUndefined()
    expect(context.loopRecoveries).toBe(8)
  })

  it('uses the final bomb when critically threatened', () => {
    const state = newRun(71)
    const hostile = state.floor.actors.find(actor => actor.hostile)!
    state.floor.actors = [hostile]
    hostile.x = state.hero.x + 1
    hostile.y = state.hero.y
    state.hero.health = 1
    state.hero.bombs = 1
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe('b')
  })

  it('completes the Mine reference run using tactical actions', () => {
    const report = runAutoplay(newRun(7, 'mine'), { mode: 'omniscient', policy: 'clear', turnLimit: 800 })
    expect(report.outcome).toBe('complete')
    expect(report.trace.some(entry => entry.reason.includes('bomb tactical cluster'))).toBe(true)
    expect(report.trace.some(entry => entry.reason.startsWith('throw:') || entry.reason.startsWith('cast:'))).toBe(true)
  }, 30_000)

  it('replays deterministically without mutating its input', () => {
    const state = newRun(913, 'mine')
    const before = structuredClone(state)
    const first = runAutoplay(state, { mode: 'omniscient', turnLimit: 120 })
    const second = runAutoplay(state, { mode: 'omniscient', turnLimit: 120 })
    expect(state).toEqual(before)
    expect(first.outcome).not.toBe('error')
    expect(first.commands.length).toBeGreaterThan(0)
    expect(first).toEqual(second)
  }, 20_000)
})
