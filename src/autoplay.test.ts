import { describe, expect, it } from 'vitest'
import { AUTOPLAY_MAX_TURNS, autoplayCommand, autoplayDecision, autoplayRecoveryFingerprint, createAutoplayContext, recordAutoplayTransition } from './autoplay'
import { runAutoplay } from './autoplay-runner'
import { newRun, perform, skillChoices } from './engine'

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

  it('steps onto an altar before operating it', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.items = []
    state.floor.guardianDefeated = true
    state.floor.objective = { id: 'altar-objective', kind: 'invokeAltar', label: 'Make a shrine offering', status: 'active' }
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.hero.x = 5
    state.hero.y = 5
    state.hero.gold = 75
    state.floor.tiles[5 * 48 + 6].kind = 'altar'
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe(';')
    perform(state, ';')
    expect(state.hero).toMatchObject({ x: 6, y: 5 })
    expect(autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())?.command).toBe('c')
  })

  it('does not route through a direction consumed by a cooling weapon attack', () => {
    const state = newRun(71)
    const hostile = state.floor.actors.find(actor => actor.hostile)!
    state.floor.tiles.forEach(tile => { tile.kind = 'floor'; tile.explored = true })
    state.hero.x = 5
    state.hero.y = 5
    state.hero.skills = ['agi2']
    state.hero.equipment.mainHand = 'pickaxe'
    state.hero.cooldowns = { pickaxe: 1 }
    state.floor.actors = [
      { ...hostile, id: 'cross-target', x: 7, y: 4, health: 20 },
      { ...hostile, id: 'guardian', role: 'guardian', x: 15, y: 5, health: 30 }
    ]
    state.floor.objective = { id: 'guardian-objective', kind: 'defeatGuardian', label: 'Pass the trail guardian', status: 'active' }
    const decision = autoplayDecision(state, 'omniscient', 'clear', createAutoplayContext())
    expect(decision?.command).not.toBe('p')
    const beforeTurn = state.turn
    perform(state, decision!.command)
    expect(state.turn).toBe(beforeTurn + 1)
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

  it('stops a live autoplay session at the hard turn guard', () => {
    const state = newRun(71)
    const context = createAutoplayContext()
    context.startedTurn = state.turn
    state.turn += AUTOPLAY_MAX_TURNS
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toBeUndefined()
    expect(context.lastReason).toBe(`turn guard:${AUTOPLAY_MAX_TURNS}`)
  })

  it('stops after repeated commands that cannot advance a turn', () => {
    const state = newRun(71)
    const context = createAutoplayContext()
    context.noTurnCommands = 8
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toBeUndefined()
    expect(context.lastReason).toBe('non-turn guard:8')
  })

  it('stops after repeated recovery at the same strategic state', () => {
    const state = newRun(71)
    const context = createAutoplayContext()
    context.noProgressTurns = 32
    context.recoveryVisits.set(autoplayRecoveryFingerprint(state), 8)
    expect(autoplayDecision(state, 'omniscient', 'clear', context)).toBeUndefined()
    expect(context.lastReason).toBe('recovery cycle guard')
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

  it('falls back to a reachable objective target when the pinned target is sealed off', () => {
    const state = newRun(71)
    state.floor.actors = []
    state.floor.items = []
    state.hero.gold = 75
    state.floor.tiles.forEach(tile => { tile.kind = 'wall'; tile.explored = true })
    state.hero.x = 2
    state.hero.y = 2
    for (let x = 2; x <= 4; x++) state.floor.tiles[2 * 48 + x].kind = 'floor'
    state.floor.tiles[2 * 48 + 5].kind = 'altar'
    state.floor.tiles[10 * 48 + 10].kind = 'altar'
    state.floor.objective = { ...state.floor.objective, kind: 'invokeAltar', label: 'Make a shrine offering', status: 'active' }
    const context = createAutoplayContext()
    context.objectiveId = state.floor.objective.id
    context.objectiveTarget = '10,10'
    autoplayDecision(state, 'omniscient', 'clear', context)
    expect(context.objectiveTarget).toBe('5,2')
  })

  it('completes the Mine reference run using tactical actions', () => {
    const report = runAutoplay(newRun(7, 'mine'), { mode: 'omniscient', policy: 'clear', turnLimit: 800, chainAreas: false })
    expect(report.outcome).toBe('complete')
    expect(report.trace.some(entry => entry.reason.includes('bomb tactical cluster'))).toBe(true)
    expect(report.trace.some(entry => entry.reason.startsWith('throw:') || entry.reason.startsWith('cast:'))).toBe(true)
  }, 60_000)

  it('chains completed areas into the next biome by default', () => {
    const state = newRun(7, 'mine', 3)
    state.floor.actors = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    const chained = runAutoplay(state, { mode: 'omniscient', policy: 'clear', turnLimit: 1 })
    const singleArea = runAutoplay(state, { mode: 'omniscient', policy: 'clear', turnLimit: 1, chainAreas: false })
    expect(chained.completedAreas).toEqual(['mine'])
    expect(chained.finalBiome).toBe('wilds')
    expect(singleArea.outcome).toBe('complete')
    expect(singleArea.finalBiome).toBe('mine')
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
  }, 20_000)
})
