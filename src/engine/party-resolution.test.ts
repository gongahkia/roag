import { describe, expect, it } from 'vitest'
import { createEnemy, createRun } from '../test/factories'
import { companionLeadForRescue } from './companions'
import { companionActorId, synchronizePartyActors } from './party'
import { partyActionOrder, removeDefeatedPartyActors, resolvePartyMove, resolvePartyTargets } from './party-resolution'

const companion = (id: string) => {
  const value = companionLeadForRescue({ id: `rescue:${id}`, name: id, biome: 'mine', floor: 1 })
  value.rosterStatus = 'active'
  return value
}

describe('party action resolution', () => {
  it('uses courier, canonical companion, then enemy action order', () => {
    const state = createRun({ companions: [companion('zeta'), companion('alpha')] })
    synchronizePartyActors(state, 'spawn')
    state.floor.actors.push(createEnemy({ id: 'z' }), createEnemy({ id: 'a' }))
    expect(partyActionOrder(state)).toEqual(['courier', companionActorId('companion:rescue:alpha'), companionActorId('companion:rescue:zeta'), 'a', 'z'])
  })

  it('returns typed collision reasons and deterministic wait fallbacks', () => {
    const state = createRun()
    state.floor.actors.push(createEnemy({ id: 'occupant', x: 2, y: 1 }))
    expect(resolvePartyMove(state, 'courier', { x: 2, y: 1 })).toMatchObject({ resolved: false, reason: 'blocked-occupant', targets: ['occupant'], fallback: 'wait' })
    expect(resolvePartyMove(state, 'missing', { x: 2, y: 1 })).toMatchObject({ resolved: false, reason: 'source-missing', fallback: 'wait' })
  })

  it('deduplicates line and area targets while rejecting friendly fire', () => {
    const ally = companion('mika')
    const state = createRun({ companions: [ally] })
    synchronizePartyActors(state, 'spawn')
    const actor = state.floor.actors.find(candidate => candidate.id === companionActorId(ally.id))!
    state.floor.actors.push(createEnemy({ id: 'enemy', x: 3, y: 1 }))
    expect(resolvePartyTargets(state, 'courier', 'line', [{ x: actor.x, y: actor.y }, { x: actor.x, y: actor.y }])).toMatchObject({ resolved: false, reason: 'friendly-fire', targets: [] })
    expect(resolvePartyTargets(state, 'courier', 'area', [{ x: 3, y: 1 }, { x: 3, y: 1 }])).toEqual({ sourceId: 'courier', kind: 'area', resolved: true, reason: 'resolved', targets: ['enemy'] })
  })

  it('removes defeated actors only after sequential resolution', () => {
    const state = createRun()
    state.floor.actors.push(createEnemy({ id: 'later', health: 0 }))
    expect(removeDefeatedPartyActors(state)).toEqual(['later'])
    expect(state.floor.actors).toEqual([])
  })
})
