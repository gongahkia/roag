import { describe, expect, it } from 'vitest'
import { indexOf } from '../types'
import { createEnemy, createRun } from '../test/factories'
import { companionLeadForRescue } from './companions'
import { autonomousCompanionDecision, resolveAutonomousCompanions } from './companion-autonomy'
import { isLegalCompanionRoleAction } from './companion-roles'
import { isCompanionActor, synchronizePartyActors } from './party'

const companion = (id: string, biome: 'mine' | 'wilds' | 'caverns' | 'ruins') => {
  const value = companionLeadForRescue({ id: `rescue:${id}`, name: id, biome, floor: 1 })
  value.rosterStatus = 'active'
  return value
}
const setup = (value = companion('mika', 'mine')) => {
  const state = createRun({ companions: [value] })
  synchronizePartyActors(state, 'spawn')
  return { state, companion: value, actor: state.floor.actors.find(isCompanionActor)! }
}
const tile = (state: ReturnType<typeof createRun>, x: number, y: number) => state.floor.tiles[indexOf(x, y)]!

describe('autonomous companion behavior', () => {
  it('selects role-legal visible actions with deterministic priority ties', () => {
    const guard = setup()
    guard.state.floor.actors.push(createEnemy({ id: 'zeta', x: 2, y: 1 }), createEnemy({ id: 'alpha', x: 1, y: 2 }))
    expect(autonomousCompanionDecision(guard.state, guard.companion, guard.actor)).toMatchObject({ action: 'intercept', target: { x: 1, y: 2 } })
    const scout = setup(companion('scout', 'wilds'))
    scout.state.floor.milestones.push({ id: 'sign', kind: 'waycache', x: 3, y: 1, discovered: true, claimed: false })
    expect(autonomousCompanionDecision(scout.state, scout.companion, scout.actor).action).toBe('observe')
    const pathmaker = setup(companion('path', 'caverns'))
    tile(pathmaker.state, 2, 1).kind = 'rubble'
    expect(autonomousCompanionDecision(pathmaker.state, pathmaker.companion, pathmaker.actor).action).toBe('stabilizeTerrain')
    const ritualist = setup(companion('rite', 'ruins'))
    tile(ritualist.state, 2, 1).kind = 'fireVent'
    expect(autonomousCompanionDecision(ritualist.state, ritualist.companion, ritualist.actor).action).toBe('stabilizeHazard')
    for (const fixture of [guard, scout, pathmaker, ritualist]) {
      const decision = autonomousCompanionDecision(fixture.state, fixture.companion, fixture.actor)
      if (decision.action !== 'follow' && decision.action !== 'wait') expect(isLegalCompanionRoleAction(fixture.companion.role, decision.action)).toBe(true)
    }
  })

  it('honors cooldowns, direct control, and hidden-information boundaries', () => {
    const cooldown = setup()
    cooldown.state.floor.actors.push(createEnemy({ id: 'threat', x: 2, y: 1 }))
    cooldown.companion.abilityState.cooldowns.intercept = 2
    expect(autonomousCompanionDecision(cooldown.state, cooldown.companion, cooldown.actor).action).not.toBe('intercept')
    const hidden = setup()
    hidden.state.floor.actors.push(createEnemy({ id: 'hidden', x: 2, y: 1 }))
    tile(hidden.state, 2, 1).visible = false
    expect(autonomousCompanionDecision(hidden.state, hidden.companion, hidden.actor).action).not.toBe('intercept')
    hidden.companion.controlMode = 'direct'
    expect(resolveAutonomousCompanions(hidden.state)).toEqual([])
  })

  it('falls back safely when the path is blocked and emits ordered replay commands', () => {
    const first = companion('zeta', 'mine')
    const second = companion('alpha', 'wilds')
    const state = createRun({ companions: [first, second] })
    synchronizePartyActors(state, 'spawn')
    const commands = resolveAutonomousCompanions(state)
    expect(commands.map(command => command.companionId)).toEqual(['companion:rescue:alpha', 'companion:rescue:zeta'])
    const blocked = setup()
    blocked.actor.x = 8
    blocked.actor.y = 8
    for (let y = blocked.actor.y - 1; y <= blocked.actor.y + 1; y++) for (let x = blocked.actor.x - 1; x <= blocked.actor.x + 1; x++) if (x !== blocked.actor.x || y !== blocked.actor.y) blocked.state.floor.actors.push(createEnemy({ id: `block:${x}:${y}`, x, y, speed: 0 }))
    expect(autonomousCompanionDecision(blocked.state, blocked.companion, blocked.actor)).toMatchObject({ action: 'wait', rationale: 'the follow path is blocked' })
  })

  it('uses companion-owned cooldowns and logs nontrivial actions without courier resources', () => {
    const guard = setup()
    guard.state.floor.actors.push(createEnemy({ id: 'threat', x: 2, y: 1 }))
    const before = { bombs: guard.state.hero.bombs, ropes: guard.state.hero.ropes, inventory: [...guard.state.hero.inventory] }
    expect(resolveAutonomousCompanions(guard.state)).toMatchObject([{ action: 'intercept' }])
    expect(guard.companion.abilityState.cooldowns).toMatchObject({ intercept: 2 })
    expect(guard.state.hero).toMatchObject(before)
    expect(guard.state.messages[0]).toContain('visible hostile threatens the courier')
  })
})
