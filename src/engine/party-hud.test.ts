import { describe, expect, it } from 'vitest'
import { companionLeadForRescue, injureCompanion } from './companions'
import { partyHud } from './party-hud'
import { synchronizePartyActors } from './party'
import { createRun } from '../test/factories'

const companion = (id: string, role: 'guard' | 'scout' | 'pathmaker' | 'ritualist') => {
  const value = companionLeadForRescue({ id: `rescue:${id}`, name: id, biome: 'mine', floor: 1 })
  value.role = role
  value.rosterStatus = 'active'
  return value
}

describe('party HUD', () => {
  it('reports roster state, visible actor condition/cooldown, turn order, and direct focus', () => {
    const guard = companion('guard', 'guard')
    const scout = companion('scout', 'scout')
    const pathmaker = companion('path', 'pathmaker')
    const ritualist = companion('rite', 'ritualist')
    scout.controlMode = 'direct'
    guard.controlMode = 'direct'
    pathmaker.rosterStatus = 'benched'
    injureCompanion(ritualist)
    guard.abilityState.cooldowns.intercept = 2
    const state = createRun({ companions: [scout, ritualist, pathmaker, guard] })
    synchronizePartyActors(state, 'spawn')
    const actor = state.floor.actors.find(candidate => candidate.id === 'party:companion:rescue:guard')!
    actor.health = 7
    actor.conditions = [{ kind: 'slowed', duration: 2, potency: 1 }]
    actor.status = [...(actor.status ?? []), 'intercept:0']
    state.modal = { kind: 'companionCommand', companionIds: [guard.id, scout.id], index: 0 }
    expect(partyHud(state)).toMatchObject({
      controlMode: 'direct',
      order: ['courier', guard.id, scout.id],
      entries: [
        { id: guard.id, glyph: 'G', status: 'active', health: { current: 7, maximum: 12 }, conditions: ['slowed 2T', 'intercept'], cooldowns: [{ action: 'intercept', turns: 2 }], turnOrder: 1, focused: true },
        { id: pathmaker.id, glyph: 'P', status: 'benched', turnOrder: undefined, focused: false },
        { id: ritualist.id, glyph: 'R', status: 'injured', turnOrder: undefined, focused: false },
        { id: scout.id, glyph: 'S', status: 'active', turnOrder: 2, focused: false }
      ]
    })
  })

  it('keeps lost and recovering companions in the roster without exposing a map actor', () => {
    const lost = companion('lost', 'guard')
    lost.permanentlyLost = true
    lost.rosterStatus = 'lost'
    const recovering = companion('recovering', 'scout')
    recovering.injury = 'recovering'
    recovering.rosterStatus = 'benched'
    recovering.recoveryFloors = 2
    const state = createRun({ companions: [lost, recovering] })
    synchronizePartyActors(state, 'spawn')
    expect(partyHud(state)).toMatchObject({
      order: ['courier'],
      entries: [
        { id: lost.id, status: 'lost', health: undefined },
        { id: recovering.id, status: 'recovering', health: undefined }
      ]
    })
  })
})
