import { describe, expect, it } from 'vitest'
import { completeObjective, objectiveForFloor } from './objectives'
import { createFloor, createRun } from './test/factories'

describe('floor objectives', () => {
  it('uses one stable objective type for each local floor role', () => {
    expect([0, 1, 2, 3].map(objectiveForFloor)).toMatchObject([
      { kind: 'recoverSupplies', status: 'active' }, { kind: 'rescueScout', status: 'active' }, { kind: 'invokeAltar', status: 'active' }, { kind: 'defeatGuardian', status: 'active' }
    ])
  })

  it('only completes the matching active objective', () => {
    const state = createRun({ floor: createFloor({ objective: objectiveForFloor(1) }) })
    expect(completeObjective(state, 'invokeAltar')).toBe(false)
    expect(completeObjective(state, 'rescueScout')).toBe(true)
    expect(completeObjective(state, 'rescueScout')).toBe(false)
    expect(state.floor.objective.status).toBe('complete')
  })
})
