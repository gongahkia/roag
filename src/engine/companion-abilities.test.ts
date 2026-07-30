import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createEnemy, createRun } from '../test/factories'
import { executeCompanionRoleAction } from './companion-autonomy'
import { assessCompanionAbility, companionAbilityTarget } from './companion-abilities'
import { companionLeadForRescue } from './companions'
import { perform } from './input'
import { isCompanionActor, synchronizePartyActors } from './party'
import { fieldReadout } from './readout'

const companion = (id: string, biome: 'mine' | 'wilds' | 'caverns' | 'ruins', controlMode: 'autonomous' | 'direct' = 'autonomous') => {
  const value = companionLeadForRescue({ id: `rescue:${id}`, name: id, biome, floor: 1 }, controlMode)
  value.rosterStatus = 'active'
  return value
}
const setup = (value: ReturnType<typeof companion>) => {
  const state = createRun({ companions: [value] })
  synchronizePartyActors(state, 'spawn')
  return { state, companion: value, actor: state.floor.actors.find(isCompanionActor)! }
}

describe('companion abilities', () => {
  it('enforces role legality, visible targets, party targeting, and companion cooldowns', () => {
    const guard = setup(companion('guard', 'mine'))
    guard.state.floor.actors.push(createEnemy({ id: 'threat', name: 'Threat', x: 2, y: 1 }))
    expect(assessCompanionAbility(guard.state, guard.companion, guard.actor, 'intercept')).toMatchObject({ ready: true, targetLabel: 'Threat', range: 2, cost: 'companion cooldown' })
    expect(assessCompanionAbility(guard.state, guard.companion, guard.actor, 'intercept', { x: guard.state.hero.x, y: guard.state.hero.y })).toMatchObject({ ready: false, reason: 'friendly-fire' })
    expect(executeCompanionRoleAction(guard.state, guard.companion, guard.actor, 'intercept', companionAbilityTarget(guard.state, guard.companion, guard.actor, 'intercept'), 'test')).toMatchObject({ executed: true, targetLabel: 'Threat', result: 'intercept stance armed' })
    expect(guard.companion.abilityState.cooldowns).toMatchObject({ intercept: 2 })
    expect(assessCompanionAbility(guard.state, guard.companion, guard.actor, 'intercept')).toMatchObject({ ready: false, reason: 'cooldown', cooldown: 2 })
  })

  it('executes one bounded effect for scout, pathmaker, and ritualist', () => {
    const scout = setup(companion('scout', 'wilds'))
    scout.state.floor.milestones.push({ id: 'trail-sign', kind: 'waycache', x: 2, y: 1, discovered: false, claimed: false })
    expect(executeCompanionRoleAction(scout.state, scout.companion, scout.actor, 'observe', companionAbilityTarget(scout.state, scout.companion, scout.actor, 'observe'), 'test')).toMatchObject({ executed: true, targetLabel: 'trail-sign', result: 'trail sign assessed' })
    expect(scout.state.floor.milestones[0]!.discovered).toBe(true)
    const pathmaker = setup(companion('path', 'caverns'))
    pathmaker.state.floor.tiles[indexOf(pathmaker.state.hero.x, pathmaker.state.hero.y)]!.kind = 'fireVent'
    expect(executeCompanionRoleAction(pathmaker.state, pathmaker.companion, pathmaker.actor, 'traverse', companionAbilityTarget(pathmaker.state, pathmaker.companion, pathmaker.actor, 'traverse'), 'test')).toMatchObject({ executed: true, result: 'crossing shielded' })
    expect(pathmaker.state.hero.conditions).toMatchObject([{ kind: 'shielded' }])
    const ritualist = setup(companion('rite', 'ruins'))
    ritualist.state.floor.tiles[indexOf(2, 1)]!.kind = 'altar'
    expect(executeCompanionRoleAction(ritualist.state, ritualist.companion, ritualist.actor, 'ward', companionAbilityTarget(ritualist.state, ritualist.companion, ritualist.actor, 'ward'), 'test')).toMatchObject({ executed: true, result: 'ward shielded courier' })
    expect(ritualist.state.hero.conditions).toMatchObject([{ kind: 'shielded' }])
  })

  it('records direct source, action, target, and result while readout exposes a specific disabled reason', () => {
    const value = companion('guard', 'mine', 'direct')
    const state = createRun({ companions: [value] })
    synchronizePartyActors(state, 'spawn')
    state.floor.actors.push(createEnemy({ id: 'threat', name: 'Threat', x: 2, y: 1 }))
    perform(state, 'l')
    expect(perform(state, '1')).toEqual(expect.arrayContaining([expect.objectContaining({ type: 'companion', id: value.id, reason: 'intercept:2,1:intercept stance armed' })]))
    expect(state.messages.join('\n')).toContain('guard intercept Threat: intercept stance armed')
    const readout = fieldReadout(state)
    expect(readout.lines.join('\n')).toContain('COMPANION: guard — intercept cooldown')
  })
})
