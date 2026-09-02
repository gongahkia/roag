import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { createFidelityPlan } from './fidelity'
import { advanceSimulationCatchUpState, createSimulationCatchUpState, simulationCatchUpProjection, validateSimulationCatchUpPlanState, validateSimulationCatchUpState, withDelegatedWorkPlaceholder } from './simulation-catchup'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld } from './world'

const selectedWorld = (seed: string, preset: 'sheltered-reach' | 'watershed' = 'watershed') => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset } }), 'crew:0')
const action = (id: string, durationMinutes = 1) => ({
  id,
  kind: 'wait' as const,
  durationMinutes,
  contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
})
const simulationContext = (world: ReturnType<typeof selectedWorld>) => ({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime,
  personIds: world.state.people.records.map(person => person.id),
  marketIds: world.state.markets.markets.map(market => market.id),
  institutionIds: world.state.institutions.registry.map(institution => institution.id)
})
const projectionOf = (world: ReturnType<typeof selectedWorld>) => simulationCatchUpProjection(simulationContext(world), world.state.simulation)
const runPartition = (seed: string, durations: readonly number[], preset: 'sheltered-reach' | 'watershed' = 'watershed') => durations.reduce((world, duration, index) => advanceFoundationWorldTime(world, action(`wait:partition:${index}`, duration)), selectedWorld(seed, preset))

describe('deterministic medieval fidelity catch-up', () => {
  it('uses canonical windows so one long wait, unit waits, and a 2+3 split have the same simulation projection', () => {
    const long = runPartition('catch-up partition five', [5])
    const units = runPartition('catch-up partition five', [1, 1, 1, 1, 1])
    const mixed = runPartition('catch-up partition five', [2, 3])

    expect(projectionOf(units)).toEqual(projectionOf(long))
    expect(projectionOf(mixed)).toEqual(projectionOf(long))
    expect(units.state.temporal.causalRecords).not.toEqual(long.state.temporal.causalRecords)
    expect(long.state.simulation.records.some(record => record.tier === 'nearby' && record.windowEndWorldTime === 5 && record.dueIntervals === 1)).toBe(true)
    expect(long.state.simulation.cursors.every(cursor => cursor.processedThroughWorldTime <= long.state.temporal.worldTime)).toBe(true)
  })

  it('preserves the projection across all scheduled fidelity tiers without treating player action boundaries as simulation boundaries', () => {
    const long = runPartition('catch-up partition tiers', [240], 'sheltered-reach')
    const partitioned = runPartition('catch-up partition tiers', [120, 120], 'sheltered-reach')
    const longProjection = projectionOf(long)

    expect(projectionOf(partitioned)).toEqual(longProjection)
    expect([...new Set(longProjection.cursors.map(cursor => cursor.tier))]).toEqual(expect.arrayContaining([
      'loaded', 'nearby', 'recurring', 'distant-individual-summary', 'loaded-place', 'distant-settlement-summary', 'loaded-institution', 'distant-institution-summary'
    ]))
    const longLoaded = long.state.simulation.records.find(record => record.targetKind === 'person' && record.tier === 'loaded' && record.windowEndWorldTime === 240)
    const shortLoaded = partitioned.state.simulation.records.find(record => record.targetKind === 'person' && record.tier === 'loaded' && record.windowEndWorldTime === 240)
    expect(shortLoaded).toMatchObject({ id: longLoaded?.id, outcomeToken: longLoaded?.outcomeToken, cadenceMinutes: 1 })
  })

  it('folds a bounded long action by canonical cadence windows rather than minutes times population', () => {
    const advanced = runPartition('catch-up long action', [1_440], 'sheltered-reach')

    expect(advanced.state.simulation.records.length).toBeLessThanOrEqual(96)
    expect(advanced.state.simulation.records.every(record => record.cause.evidence.targetId === record.targetId && record.cause.evidence.windowEndWorldTime === record.windowEndWorldTime)).toBe(true)
    expect(advanced.state.simulation.cursors.some(cursor => cursor.tier === 'nearby' && cursor.processedIntervals === 288)).toBe(true)
    expect(advanced.state.simulation.cursors.some(cursor => cursor.tier === 'distant-individual-summary' && cursor.processedIntervals === 12)).toBe(true)
    expect(advanced.state.simulation.cursors.some(cursor => cursor.tier === 'distant-settlement-summary' && cursor.processedIntervals === 6)).toBe(true)
  })

  it('gives delegated-work placeholders the same partition-invariant cadence projection', () => {
    const world = selectedWorld('catch-up delegated partition')
    const context = simulationContext(world)
    const plan = createFidelityPlan({ world, activeCourierId: 'crew:0', loadedLocations: [] })
    const assigneePersonId = plan.individuals.find(item => item.tier === 'loaded' && item.personId !== 'crew:0')?.personId ?? 'crew:0'
    const initial = withDelegatedWorkPlaceholder(createSimulationCatchUpState(), context, {
      id: 'delegated-work:repair-lines',
      assigneePersonId,
      status: 'active',
      committedAtWorldTime: 0,
      progressIntervals: 0
    })
    const run = (durations: readonly number[]) => {
      let state = initial
      let elapsed = 0
      for (const [index, duration] of durations.entries()) {
        state = advanceSimulationCatchUpState(state, plan, { actionId: `delegated:${index}`, startedAtWorldTime: elapsed, atWorldTime: elapsed + duration }).state
        elapsed += duration
      }
      return { state, elapsed }
    }
    const long = run([5])
    const mixed = run([2, 3])

    expect(simulationCatchUpProjection({ ...context, worldTime: mixed.elapsed }, mixed.state)).toEqual(simulationCatchUpProjection({ ...context, worldTime: long.elapsed }, long.state))
    expect(long.state.delegatedWork[0]).toMatchObject({ progressIntervals: 5 })
    expect(validateSimulationCatchUpPlanState(long.state, plan, 5)).toEqual([])
  })

  it('keeps action-derived observation records bounded and keeps the full mutable world valid after reloadable scheduling', () => {
    const first = runPartition('catch-up replay', [1, 239])
    const second = runPartition('catch-up replay', [1, 239])

    expect(second).toEqual(first)
    expect(first.state.simulation.records.some(record => record.tier === 'distant-institution-summary')).toBe(true)
    expect(first.state.simulation.records).toEqual(expect.arrayContaining([
      expect.objectContaining({
        outcomeDetail: 'detailed',
        cause: expect.objectContaining({ version: 2, kind: 'fidelity-cadence', evidence: expect.objectContaining({ source: 'known-world-record' }) })
      })
    ]))
  })

  it('fails closed for malformed scheduling, token, cause, provenance, and safety evidence', () => {
    const advanced = runPartition('catch-up validation', [5])
    const malformed = structuredClone(advanced.state.simulation)
    malformed.records[0]!.targetId = 'market:not-present'
    const forgedToken = structuredClone(advanced.state.simulation)
    forgedToken.records[0]!.outcomeToken++
    const forgedCause = structuredClone(advanced.state.simulation)
    forgedCause.records[0]!.cause.evidence.cadenceMinutes = 5
    const missingCause = structuredClone(advanced.state.simulation)
    delete (missingCause.records[0] as { cause?: unknown }).cause
    const missingOutcomeClassification = structuredClone(advanced.state.simulation)
    delete (missingOutcomeClassification.records[0] as { contentSafety?: unknown }).contentSafety
    const prohibitedCause = structuredClone(advanced.state.simulation)
    ;(prohibitedCause.records[0]!.cause.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const skippedCursor = structuredClone(advanced.state.simulation)
    skippedCursor.cursors = skippedCursor.cursors.filter(cursor => cursor.tier !== 'nearby')
    const wrongProvenance = { ...simulationContext(advanced), creationDigest: 'forged-provenance' }
    const plan = createFidelityPlan({ world: advanced, activeCourierId: 'crew:0', loadedLocations: [] })

    expect(validateSimulationCatchUpState(simulationContext(advanced), malformed).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.invalid-record')
    expect(validateSimulationCatchUpState(simulationContext(advanced), forgedToken).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.invalid-outcome-token')
    expect(validateSimulationCatchUpState(simulationContext(advanced), forgedCause).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.untraceable-cause')
    expect(validateSimulationCatchUpState(simulationContext(advanced), missingCause).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.invalid-cause')
    expect(validateSimulationCatchUpState(simulationContext(advanced), missingOutcomeClassification).map(diagnostic => diagnostic.code)).toContain('content-safety.missing-classification')
    expect(validateSimulationCatchUpState(simulationContext(advanced), prohibitedCause).map(diagnostic => diagnostic.code)).toContain('content-safety.prohibited.torture')
    expect(validateSimulationCatchUpState(wrongProvenance, advanced.state.simulation).map(diagnostic => diagnostic.code)).toContain('simulation-catchup.invalid-outcome-token')
    expect(validateSimulationCatchUpPlanState(skippedCursor, plan)).toContainEqual({ recordId: expect.stringContaining('catch-up:'), code: 'simulation-catchup.invalid-cursor' })
  })

  it('does not mutate scheduling state for pure zero-time commands', () => {
    const world = selectedWorld('catch-up pure command')
    const before = structuredClone(world)

    expect(() => advanceFoundationWorldTime(world, { kind: 'inspect' })).toThrow('temporal contract rejected')
    expect(world).toEqual(before)
    expect(world.state.simulation).toEqual(before.state.simulation)
  })
})
