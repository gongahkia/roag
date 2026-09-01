import { describe, expect, it } from 'vitest'
import { createFidelityPlan } from './fidelity'
import { advanceSimulationCatchUpState, createSimulationCatchUpState, validateSimulationCatchUpState, withDelegatedWorkPlaceholder } from './simulation-catchup'
import { chooseInitialCourier, createFoundationWorld, advanceFoundationWorldTime } from './world'

const selectedWorld = (seed: string, preset: 'sheltered-reach' | 'watershed' = 'watershed') => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset } }), 'crew:0')
const action = (id: string, kind: 'movement' | 'wait' = 'movement', durationMinutes = 1) => ({
  id,
  kind,
  durationMinutes,
  contentSafety: createFoundationWorld({ seed: 'catch-up classification' }).state.history.records[0]!.contentSafety
})

describe('deterministic medieval fidelity catch-up', () => {
  it('processes loaded tiers on every action and elapsed tiers only after their cadence boundary', () => {
    const world = selectedWorld('catch-up cadence')
    const first = advanceFoundationWorldTime(world, action('movement:one'))
    const fifth = advanceFoundationWorldTime(first, action('wait:to-five', 'wait', 4))

    expect(first.state.simulation.records.length).toBeGreaterThan(0)
    expect(first.state.simulation.records.every(record => record.tier === 'loaded' || record.tier === 'loaded-place' || record.tier === 'loaded-institution')).toBe(true)
    expect(fifth.state.simulation.records.some(record => record.tier === 'nearby' && record.dueIntervals === 1)).toBe(true)
    expect(fifth.state.simulation.cursors.every(cursor => cursor.processedAtWorldTime <= fifth.state.temporal.worldTime)).toBe(true)
  })

  it('batches long actions by crossed cadence boundaries without growing the outcome window unboundedly', () => {
    const world = selectedWorld('catch-up long action', 'sheltered-reach')
    const advanced = advanceFoundationWorldTime(world, action('travel:two-days', 'wait', 1_440))

    expect(advanced.state.simulation.records.length).toBeLessThanOrEqual(96)
    expect(advanced.state.simulation.records.some(record => record.tier === 'nearby' && record.dueIntervals === 288)).toBe(true)
    expect(advanced.state.simulation.records.some(record => record.tier === 'distant-individual-summary' && record.dueIntervals === 12)).toBe(true)
    expect(advanced.state.simulation.records.some(record => record.tier === 'distant-settlement-summary' && record.dueIntervals === 6)).toBe(true)
  })

  it('replays equivalent action sequences identically while preserving immutable creation evidence', () => {
    const run = () => {
      let world = selectedWorld('catch-up replay')
      world = advanceFoundationWorldTime(world, action('movement:a'))
      return advanceFoundationWorldTime(world, action('wait:b', 'wait', 239))
    }
    const first = run()
    const second = run()

    expect(second).toEqual(first)
    expect(first.manifest).toEqual(selectedWorld('catch-up replay').manifest)
    expect(first.state.simulation.records.some(record => record.tier === 'distant-institution-summary')).toBe(true)
  })

  it('advances a typed delegated-work placeholder at its assignee fidelity without adding delegation gameplay', () => {
    const world = selectedWorld('catch-up placeholder')
    const context = {
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: 0,
      personIds: world.state.people.records.map(person => person.id),
      marketIds: world.state.markets.markets.map(market => market.id),
      institutionIds: world.state.institutions.registry.map(institution => institution.id),
      actionEvidence: []
    }
    const state = withDelegatedWorkPlaceholder(createSimulationCatchUpState(), context, {
      id: 'delegated-work:repair-lines',
      assigneePersonId: 'crew:1',
      status: 'active',
      committedAtWorldTime: 0,
      progressIntervals: 0
    })
    const plan = createFidelityPlan({ world, activeCourierId: 'crew:0', loadedLocations: [] })
    const transition = advanceSimulationCatchUpState(state, plan, { actionId: 'movement:work', startedAtWorldTime: 0, atWorldTime: 1 })

    expect(transition.records).toContainEqual(expect.objectContaining({ targetKind: 'delegated-work', targetId: 'delegated-work:repair-lines', dueIntervals: 1 }))
    expect(transition.state.delegatedWork[0]).toMatchObject({ progressIntervals: 1 })
    expect(validateSimulationCatchUpState({ ...context, worldTime: 1, actionEvidence: [{ id: 'movement:work', startedAtWorldTime: 0, atWorldTime: 1 }] }, transition.state)).toEqual([])
  })

  it('fails closed for malformed catch-up records and keeps the bounded audit tied to outcomes', () => {
    const world = selectedWorld('catch-up validation')
    const advanced = advanceFoundationWorldTime(world, action('movement:validation'))
    const forged = structuredClone(advanced.state.simulation)
    forged.records[0]!.targetId = 'market:not-present'

    expect(validateSimulationCatchUpState({
      worldId: advanced.id,
      creationDigest: advanced.manifest.creation.digest,
      worldTime: advanced.state.temporal.worldTime,
      personIds: advanced.state.people.records.map(person => person.id),
      marketIds: advanced.state.markets.markets.map(market => market.id),
      institutionIds: advanced.state.institutions.registry.map(institution => institution.id),
      actionEvidence: advanced.state.temporal.causalRecords.filter(record => record.kind === 'action-completed').map(record => ({ id: record.actionId, startedAtWorldTime: record.startedAtWorldTime, atWorldTime: record.atWorldTime }))
    }, forged).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.invalid-record')
  })
})
