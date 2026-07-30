import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createHero, createRun } from '../test/factories'
import { autonomousCompanionDecision, executeCompanionRoleAction } from './companion-autonomy'
import { companionLeadForRescue } from './companions'
import { companionTerrainMutationAssessment, companionTerrainTargets, COMPANION_RESOURCE_OWNERSHIP } from './companion-traversal'
import { isCompanionActor, synchronizePartyActors } from './party'

const companion = (id: string, biome: 'caverns' | 'ruins') => {
  const value = companionLeadForRescue({ id: `rescue:${id}`, name: id, biome, floor: 1 })
  value.rosterStatus = 'active'
  return value
}
const setup = (value: ReturnType<typeof companion>) => {
  const state = createRun({ companions: [value], hero: createHero({ bombs: 2, ropes: 1, traversalTools: ['stoneAdze'] }) })
  synchronizePartyActors(state, 'spawn')
  return { state, companion: value, actor: state.floor.actors.find(isCompanionActor)! }
}

describe('companion traversal support', () => {
  it('uses only companion cooldowns for deterministic pathmaker and ritualist terrain support', () => {
    const pathmaker = setup(companion('path', 'caverns'))
    pathmaker.state.floor.tiles[indexOf(2, 1)]!.kind = 'rubble'
    const resources = { bombs: pathmaker.state.hero.bombs, ropes: pathmaker.state.hero.ropes, tools: [...(pathmaker.state.hero.traversalTools ?? [])] }
    const target = companionTerrainTargets(pathmaker.state, 'stabilizeTerrain')[0]!
    expect(executeCompanionRoleAction(pathmaker.state, pathmaker.companion, pathmaker.actor, 'stabilizeTerrain', target, 'test support')).toMatchObject({ executed: true, result: 'terrain stabilized' })
    expect(pathmaker.state.floor.tiles[indexOf(2, 1)]!.kind).toBe('floor')
    expect(pathmaker.companion.abilityState.cooldowns).toMatchObject({ stabilizeTerrain: 2 })
    expect(pathmaker.state.hero).toMatchObject({ bombs: resources.bombs, ropes: resources.ropes, traversalTools: resources.tools })
    const ritualist = setup(companion('rite', 'ruins'))
    ritualist.state.floor.tiles[indexOf(2, 1)]!.kind = 'fireVent'
    expect(autonomousCompanionDecision(ritualist.state, ritualist.companion, ritualist.actor)).toMatchObject({ action: 'stabilizeHazard', target: { x: 2, y: 1 } })
    expect(COMPANION_RESOURCE_OWNERSHIP).toEqual({ cooldown: 'companion', courierFiniteResources: 'explicit-command-required', courierTraversalTools: 'courier', terrainMutations: 'companion' })
  })

  it('rejects hidden, protected, and occupied terrain without consuming a cooldown', () => {
    const pathmaker = setup(companion('path', 'caverns'))
    const hidden = { x: 2, y: 1 }
    pathmaker.state.floor.tiles[indexOf(hidden.x, hidden.y)]!.kind = 'rubble'
    pathmaker.state.floor.tiles[indexOf(hidden.x, hidden.y)]!.visible = false
    expect(companionTerrainMutationAssessment(pathmaker.state, 'stabilizeTerrain', hidden)).toMatchObject({ ready: false, reason: 'target-unseen' })
    expect(executeCompanionRoleAction(pathmaker.state, pathmaker.companion, pathmaker.actor, 'stabilizeTerrain', hidden, 'test support')).toMatchObject({ executed: false, result: 'target-unseen' })
    expect(pathmaker.companion.abilityState.cooldowns).toEqual({})
    const protectedTarget = { ...pathmaker.state.floor.exit }
    pathmaker.state.floor.tiles[indexOf(protectedTarget.x, protectedTarget.y)]!.kind = 'rubble'
    expect(companionTerrainMutationAssessment(pathmaker.state, 'stabilizeTerrain', protectedTarget)).toMatchObject({ ready: false, reason: 'target-protected' })
  })
})
