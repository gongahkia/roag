import { describe, expect, it } from 'vitest'
import { autoplayDecision, createAutoplayContext } from './autoplay'
import { createRun } from './test/factories'

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
})
