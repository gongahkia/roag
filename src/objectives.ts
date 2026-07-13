import type { FloorObjective, ObjectiveKind, RunState } from './types'

const objectives: readonly Omit<FloorObjective, 'id' | 'status'>[] = [
  { kind: 'recoverSupplies', label: 'Secure a supply cache' },
  { kind: 'rescueScout', label: 'Rescue the lost scout' },
  { kind: 'invokeAltar', label: 'Invoke the shrine altar' },
  { kind: 'defeatGuardian', label: 'Defeat the area guardian' }
]

export const objectiveForFloor = (index: number): FloorObjective => {
  const objective = objectives[index % 4]
  return { id: `objective:${index}:${objective.kind}`, ...objective, status: 'active' }
}

export const completeObjective = (state: RunState, kind: ObjectiveKind): boolean => {
  const objective = state.floor.objective
  if (objective.kind !== kind || objective.status === 'complete') return false
  objective.status = 'complete'
  return true
}
