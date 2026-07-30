import { describe, expect, it } from 'vitest'
import { migrateRunRecord } from '../storage'
import { createRun } from '../test/factories'
import { beginCompanionRecovery, companionLeadForRescue, companionLodgeAction, completeCompanionRecovery, COMPANION_RECOVERY_COST, injureCompanion, progressCompanionRecovery } from './companions'
import { descend } from './inventory'
import { isCompanionActor, synchronizePartyActors } from './party'
import { newRun } from './run'

const rescue = { id: 'rescue:mine:1:mika', name: 'Mika', biome: 'mine' as const, floor: 1 }
const injured = () => {
  const companion = companionLeadForRescue(rescue)
  companion.rosterStatus = 'active'
  expect(injureCompanion(companion)).toBe(true)
  return companion
}

describe('companion injury recovery', () => {
  it('transitions a qualifying loss once into a benched recoverable injury', () => {
    const companion = injured()
    expect(companion).toMatchObject({ injury: 'injured', rosterStatus: 'benched', permanentlyLost: false })
    expect(injureCompanion(companion)).toBe(false)
    expect(companionLodgeAction(companion)).toBe('beginRecovery')
  })

  it('requires 10 cash and one cleared floor before Lodge completion', () => {
    const companion = injured()
    expect(beginCompanionRecovery([companion], [rescue], companion.id, COMPANION_RECOVERY_COST - 1)).toMatchObject({ changed: false, cashSpent: 0, message: 'Mika\'s treatment needs 1 more cash.' })
    const started = beginCompanionRecovery([companion], [rescue], companion.id, COMPANION_RECOVERY_COST)
    expect(started).toMatchObject({ changed: true, cashSpent: COMPANION_RECOVERY_COST, companions: [{ injury: 'recovering', rosterStatus: 'benched', recoveryFloors: 1 }] })
    expect(completeCompanionRecovery(started.companions, [rescue], companion.id)).toMatchObject({ changed: false, message: 'Mika needs 1 more cleared floor before returning.' })
    const progressed = progressCompanionRecovery(started.companions, [rescue])
    expect(progressed).toMatchObject([{ injury: 'recovering', recoveryFloors: 0 }])
    const recovered = completeCompanionRecovery(progressed, [rescue], companion.id)
    expect(recovered).toMatchObject({ changed: true, cashSpent: 0, companions: [{ injury: 'healthy', rosterStatus: 'benched' }] })
    expect(companionLodgeAction(recovered.companions[0]!)).toBe('activate')
  })

  it('counts only cleared floors, removes injured actors, and preserves recovery in saves', () => {
    const companion = injured()
    companion.injury = 'recovering'
    companion.recoveryFloors = 1
    const state = newRun(1234, 'mine', 0, undefined, [rescue], [], undefined, undefined, [companion])
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    expect(descend(state)).toEqual(expect.arrayContaining([expect.objectContaining({ type: 'floor' })]))
    expect(state.companions).toMatchObject([{ injury: 'recovering', recoveryFloors: 0, rosterStatus: 'benched' }])
    expect(state.floor.actors.some(isCompanionActor)).toBe(false)
    const raw = createRun({ companions: [companion] })
    raw.companions![0]!.injury = 'recovering'
    delete raw.companions![0]!.recoveryFloors
    raw.companions![0]!.rosterStatus = 'benched'
    expect(migrateRunRecord(JSON.parse(JSON.stringify(raw)))?.companions).toMatchObject([{ injury: 'recovering', recoveryFloors: 1 }])
    const recovered = completeCompanionRecovery([{ ...companion, recoveryFloors: 0 }], [rescue], companion.id).companions
    recovered[0]!.rosterStatus = 'active'
    const party = createRun({ companions: recovered })
    synchronizePartyActors(party, 'activation')
    expect(party.floor.actors.some(isCompanionActor)).toBe(true)
  })
})
