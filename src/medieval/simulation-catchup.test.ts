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
const simulationContext = (world: ReturnType<typeof selectedWorld>) => ({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime,
  personIds: world.state.people.records.map(person => person.id),
  marketIds: world.state.markets.markets.map(market => market.id),
  institutionIds: world.state.institutions.registry.map(institution => institution.id),
  actionEvidence: world.state.temporal.causalRecords.filter(record => record.kind === 'action-completed').map(record => ({ id: record.actionId, startedAtWorldTime: record.startedAtWorldTime, atWorldTime: record.atWorldTime }))
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
    expect(advanced.state.simulation.records.every(record => record.cause.evidence.targetId === record.targetId && record.cause.evidence.dueIntervals === record.dueIntervals)).toBe(true)
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
    expect(first.state.simulation.records).toEqual(expect.arrayContaining([
      expect.objectContaining({
        outcomeDetail: 'detailed',
        cause: expect.objectContaining({
          version: 1,
          kind: 'fidelity-cadence',
          evidence: expect.objectContaining({ source: 'known-world-record' })
        })
      })
    ]))
    expect(first.state.simulation.records.every(record => record.cause.evidence.actionId === record.actionId && record.cause.evidence.targetId === record.targetId && record.cause.evidence.processedAtWorldTime === record.processedAtWorldTime)).toBe(true)
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

    expect(validateSimulationCatchUpState(simulationContext(advanced), forged).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.invalid-record')
  })

  it('rejects missing or prohibited outcome/cause classifications instead of trusting a stored audit', () => {
    const advanced = advanceFoundationWorldTime(selectedWorld('catch-up outcome safety'), action('movement:safety'))
    const missingOutcomeClassification = structuredClone(advanced.state.simulation)
    delete (missingOutcomeClassification.records[0] as { contentSafety?: unknown }).contentSafety
    const prohibitedCause = structuredClone(advanced.state.simulation)
    ;(prohibitedCause.records[0]!.cause.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'

    expect(validateSimulationCatchUpState(simulationContext(advanced), missingOutcomeClassification).map(diagnostic => diagnostic.code)).toContain('content-safety.missing-classification')
    expect(validateSimulationCatchUpState(simulationContext(advanced), prohibitedCause).map(diagnostic => diagnostic.code)).toContain('content-safety.prohibited.torture')
  })

  it('rejects missing, untraceable, or token-forged causal evidence', () => {
    const advanced = advanceFoundationWorldTime(selectedWorld('catch-up evidence safety'), action('movement:evidence'))
    const missingCause = structuredClone(advanced.state.simulation)
    delete (missingCause.records[0] as { cause?: unknown }).cause
    const missingEvidence = structuredClone(advanced.state.simulation)
    delete (missingEvidence.records[0]!.cause as { evidence?: unknown }).evidence
    const untraceableCause = structuredClone(advanced.state.simulation)
    untraceableCause.records[0]!.cause.evidence.actionId = 'movement:not-accepted'
    const forgedToken = structuredClone(advanced.state.simulation)
    forgedToken.records[0]!.outcomeToken++

    expect(validateSimulationCatchUpState(simulationContext(advanced), missingCause).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.invalid-cause')
    expect(validateSimulationCatchUpState(simulationContext(advanced), missingEvidence).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.invalid-evidence')
    expect(validateSimulationCatchUpState(simulationContext(advanced), untraceableCause).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.untraceable-cause')
    expect(validateSimulationCatchUpState(simulationContext(advanced), forgedToken).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.invalid-outcome-token')
  })

  it('does not mutate simulation state for a pure zero-time command', () => {
    const world = selectedWorld('catch-up pure command')
    const before = structuredClone(world)

    expect(() => advanceFoundationWorldTime(world, { kind: 'inspect' })).toThrow('temporal contract rejected')
    expect(world).toEqual(before)
    expect(world.state.simulation).toEqual(before.state.simulation)
  })
})
