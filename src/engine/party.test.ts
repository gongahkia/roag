import { describe, expect, it } from 'vitest'
import { migrateRunRecord } from '../storage'
import { descend } from './inventory'
import { companionLeadForRescue } from './companions'
import { activeCompanionRoster, isCompanionActor, synchronizePartyActors } from './party'
import { newRun } from './run'
import { createEnemy, createRun } from '../test/factories'

const companion = (id: string, biome: 'mine' | 'wilds' = 'mine') => {
  const value = companionLeadForRescue({ id: `rescue:${id}`, name: id, biome, floor: 1 })
  value.rosterStatus = 'active'
  return value
}
const partyActors = (state: ReturnType<typeof newRun>) => state.floor.actors.filter(isCompanionActor)

describe('deterministic party placement', () => {
  it('uses one canonical active-roster order and deterministic legal spawn positions', () => {
    const companions = [companion('zeta'), companion('alpha', 'wilds')]
    expect(activeCompanionRoster(companions).map(value => value.id)).toEqual(['companion:rescue:alpha', 'companion:rescue:zeta'])
    const first = newRun(771, 'mine', 0, undefined, [], [], undefined, undefined, companions)
    const second = newRun(771, 'mine', 0, undefined, [], [], undefined, undefined, [...companions].reverse())
    expect(partyActors(first).map(actor => ({ id: actor.id, x: actor.x, y: actor.y }))).toEqual(partyActors(second).map(actor => ({ id: actor.id, x: actor.x, y: actor.y })))
    const occupied = [{ x: first.hero.x, y: first.hero.y }, ...first.floor.actors.filter(actor => actor.health > 0)].map(point => `${point.x},${point.y}`)
    expect(new Set(occupied).size).toBe(occupied.length)
    expect(partyActors(first).some(actor => actor.x === first.floor.exit.x && actor.y === first.floor.exit.y)).toBe(false)
  })

  it('repositions the full active party on floor transitions and preserves it through save/load', () => {
    const companions = [companion('mika'), companion('bo', 'wilds')]
    const state = newRun(772, 'mine', 0, undefined, [], [], undefined, undefined, companions)
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    expect(descend(state)).toMatchObject([{ type: 'floor' }])
    expect(partyActors(state)).toHaveLength(2)
    expect(migrateRunRecord(JSON.parse(JSON.stringify(state)))?.floor.actors.filter(isCompanionActor)).toEqual(partyActors(state))
  })

  it('fails safely and visibly when the formation has no legal tile', () => {
    const state = createRun({ companions: [companion('mika')] })
    state.floor.actors = state.floor.tiles.flatMap((tile, index) => tile.kind === 'floor' && index !== state.hero.y * state.floor.width + state.hero.x ? [createEnemy({ id: `block:${index}`, x: index % state.floor.width, y: Math.floor(index / state.floor.width), speed: 0 })] : [])
    const result = synchronizePartyActors(state, 'floorTransition')
    expect(result).toMatchObject({ reason: 'floorTransition', placed: false, companionIds: ['companion:rescue:mika'], positions: [], message: 'Companion formation failed: no legal tile for mika.' })
    expect(state.messages[0]).toBe(result.message)
    expect(state.floor.actors.some(isCompanionActor)).toBe(false)
  })

  it('updates occupancy deterministically for Lodge activation and removal', () => {
    const state = newRun(773)
    state.companions = [companion('mika')]
    expect(synchronizePartyActors(state, 'activation')).toMatchObject({ placed: true, companionIds: ['companion:rescue:mika'] })
    expect(partyActors(state)).toHaveLength(1)
    state.companions = []
    expect(synchronizePartyActors(state, 'removal')).toMatchObject({ placed: true, companionIds: [] })
    expect(partyActors(state)).toHaveLength(0)
  })

  it('logs formation joins and uses distinct role map markers', () => {
    const guard = companion('guard')
    guard.role = 'guard'
    const scout = companion('scout', 'wilds')
    scout.role = 'scout'
    const state = createRun({ companions: [guard, scout] })
    synchronizePartyActors(state, 'activation')
    expect(partyActors(state).map(actor => [actor.glyph, actor.color])).toEqual([['G', '#e9c965'], ['S', '#8fd39b']])
    expect(state.messages[0]).toBe('guard, scout join the party formation.')
  })
})
