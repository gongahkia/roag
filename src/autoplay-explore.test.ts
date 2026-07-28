import { describe, expect, it } from 'vitest'
import { autoplayDecision, createAutoplayContext } from './autoplay'
import { perform } from './engine'
import { createEnemy, createRun } from './test/factories'

describe('Explore autoplay policy', () => {
  it('pursues a discovered milestone before the cleared exit', () => {
    const state = createRun()
    state.floor.objective.status = 'complete'
    state.floor.milestones = [{ id: 'boon', kind: 'boon', x: 4, y: 1, discovered: true, claimed: false }]
    const decision = autoplayDecision(state, 'visible', 'explore', createAutoplayContext())
    expect(decision?.reason).toBe('explore milestone')
    expect([';', 'p', '/']).toContain(decision?.command)
  })

  it('does not target an undiscovered milestone in visible mode', () => {
    const state = createRun()
    state.floor.objective.status = 'complete'
    state.floor.milestones = [{ id: 'boon', kind: 'boon', x: 4, y: 1, discovered: false, claimed: false }]
    const decision = autoplayDecision(state, 'visible', 'explore', createAutoplayContext())
    expect(decision?.reason).not.toBe('explore milestone')
  })

  it('prioritizes an optional reward over the cleared exit', () => {
    const state = createRun()
    state.floor.objective.status = 'complete'
    state.floor.milestones = [{ id: 'payoff', kind: 'boon', rewardKey: 'boon-payoff', x: 4, y: 1, discovered: true, claimed: false }]
    expect(autoplayDecision(state, 'visible', 'explore', createAutoplayContext())?.reason).toBe('explore optional reward')
  })

  it('opens and resolves nearby encounter responses while exploring', () => {
    const state = createRun()
    state.floor.encounters = [{ id: 'event', kind: 'wayfarer', x: 2, y: 1, state: 'dormant' }]
    const context = createAutoplayContext()
    expect(autoplayDecision(state, 'visible', 'explore', context)?.reason).toBe('inspect encounter')
    perform(state, 'c')
    expect(autoplayDecision(state, 'visible', 'explore', context)?.reason).toMatch(/^encounter:wayfarer:/)
  })

  it('labels combat against a native-terrain group', () => {
    const state = createRun()
    state.floor.actors = [createEnemy({ id: 'native', x: 2, y: 1, encounter: { id: 'group', archetype: 'nativeTerrainPack', leader: true, answer: 'leave the terrain' } })]
    expect(autoplayDecision(state, 'omniscient', 'explore', createAutoplayContext())?.reason).toBe('dislodge native pack:native')
  })
})
