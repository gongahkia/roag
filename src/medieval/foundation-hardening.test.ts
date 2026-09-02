import { describe, expect, it } from 'vitest'
import { CAUSAL_HISTORY_LIMITS } from './causal-history'
import { classifyMedievalContent } from './content-safety'
import { createFidelityPlan } from './fidelity'
import { advanceSimulationCatchUpState, createSimulationCatchUpState, simulationCatchUpProjection, withDelegatedWorkPlaceholder } from './simulation-catchup'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, foundationWorldCausalHistoryMatches, replayFoundationWorldCausalHistory, recordDurableJomonGrowth, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'
import { worldEraProjection } from './world-era'

const safety = () => classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
const temporalAction = (id: string, durationMinutes: number) => ({ id, kind: 'wait' as const, durationMinutes, contentSafety: safety() })
const simulationProjection = (world: ReturnType<typeof createFoundationWorld>) => simulationCatchUpProjection({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime
}, world.state.simulation)

describe('foundation cross-contract hardening', () => {
  it('keeps every pure UI/browser command at zero time without changing a complete selected world', () => {
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'zero-time-complete-world' }), 'crew:0')
    const before = structuredClone(selected)

    for (const kind of ['inspect', 'settings-opened', 'settings-closed', 'route-changed', 'browser-idle', 'browser-paused', 'browser-reloaded', 'ui-event'] as const) {
      expect(() => advanceFoundationWorldTime(selected, { kind })).toThrow('temporal contract rejected')
      expect(selected).toEqual(before)
      expect(selected.state.temporal.worldTime).toBe(0)
      expect(selected.state.causalHistory.tail).toEqual(before.state.causalHistory.tail)
      expect(selected.state.era).toEqual(before.state.era)
      expect(selected.state.simulation).toEqual(before.state.simulation)
      expect(selected.state.autonomy).toEqual(before.state.autonomy)
    }
  })

  it('reproduces deterministic due-event ordering, all scheduled tiers, and equivalent time partitions', () => {
    const createSequence = () => {
      const selected = chooseInitialCourier(createFoundationWorld({ seed: 'deterministic-due-order', configuration: { preset: 'sheltered-reach' } }), 'crew:0')
      const scheduled = advanceFoundationWorldTime(selected, {
        id: 'work:deterministic-order',
        kind: 'work',
        durationMinutes: 1,
        contentSafety: safety(),
        events: [
          { id: 'event:zeta', dueAtWorldTime: 2, priority: 'ordinary' as const, payload: { kind: 'action-resolution' as const, sourceActionId: 'work:deterministic-order', creationDigest: selected.manifest.creation.digest }, contentSafety: safety() },
          { id: 'event:alpha', dueAtWorldTime: 2, priority: 'ordinary' as const, payload: { kind: 'action-resolution' as const, sourceActionId: 'work:deterministic-order', creationDigest: selected.manifest.creation.digest }, contentSafety: safety() },
          { id: 'event:urgent', dueAtWorldTime: 2, priority: 'urgent' as const, payload: { kind: 'action-resolution' as const, sourceActionId: 'work:deterministic-order', creationDigest: selected.manifest.creation.digest }, contentSafety: safety() }
        ]
      })
      return advanceFoundationWorldTime(scheduled, temporalAction('travel:deterministic-order', 240))
    }
    const first = createSequence()
    const second = createSequence()
    const resolvedEventIds = first.state.temporal.causalRecords
      .filter((record): record is Extract<typeof record, { kind: 'event-resolved' }> => record.kind === 'event-resolved')
      .map(record => record.event.id)

    expect(second).toEqual(first)
    expect(resolvedEventIds).toEqual(['event:urgent', 'event:alpha', 'event:zeta'])
    expect([...new Set(first.state.simulation.cursors.map(cursor => cursor.tier))]).toEqual(expect.arrayContaining([
      'loaded', 'nearby', 'recurring', 'distant-individual-summary', 'loaded-place', 'distant-settlement-summary', 'loaded-institution', 'distant-institution-summary'
    ]))

    const source = chooseInitialCourier(createFoundationWorld({ seed: 'partition-cross-contract', configuration: { preset: 'watershed' } }), 'crew:0')
    const once = advanceFoundationWorldTime(source, temporalAction('wait:five', 5))
    const split = advanceFoundationWorldTime(advanceFoundationWorldTime(source, temporalAction('wait:two', 2)), temporalAction('wait:three', 3))

    expect(simulationProjection(split)).toEqual(simulationProjection(once))
    expect(worldEraProjection(split.state.era)).toEqual(worldEraProjection(once.state.era))
    expect(split.state.temporal.causalRecords).not.toEqual(once.state.temporal.causalRecords)
    expect(split.state.causalHistory.tail).not.toEqual(once.state.causalHistory.tail)
  })

  it('retains bounded, canonical scheduler observations and delegated-work placeholders under repeated valid operations', () => {
    const world = chooseInitialCourier(createFoundationWorld({ seed: 'bounded-cross-contract', configuration: { preset: 'sheltered-reach' } }), 'crew:0')
    const plan = createFidelityPlan({ world, activeCourierId: 'crew:0', loadedLocations: [] })
    const context = {
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: 0,
      personIds: world.state.people.records.map(person => person.id),
      marketIds: world.state.markets.markets.map(market => market.id),
      institutionIds: world.state.institutions.registry.map(institution => institution.id)
    }
    const run = () => {
      let state = createSimulationCatchUpState()
      for (let minute = 1; minute <= 32; minute++) state = advanceSimulationCatchUpState(state, plan, { actionId: `wait:bounded:${minute}`, startedAtWorldTime: minute - 1, atWorldTime: minute }).state
      return state
    }
    const first = run()
    const second = run()

    expect(first).toEqual(second)
    expect(first.records).toHaveLength(96)
    expect(first.records).toEqual([...first.records].sort((left, right) => left.windowEndWorldTime - right.windowEndWorldTime || left.id.localeCompare(right.id)))
    expect(first.records.some(record => record.windowEndWorldTime === 1)).toBe(false)

    let delegated = createSimulationCatchUpState()
    for (let index = 0; index < 24; index++) {
      delegated = withDelegatedWorkPlaceholder(delegated, context, {
        id: `delegated-work:bounded:${index}`,
        assigneePersonId: world.state.people.records[index % world.state.people.records.length]!.id,
        status: 'active',
        committedAtWorldTime: 0,
        progressIntervals: 0
      })
    }
    expect(() => withDelegatedWorkPlaceholder(delegated, context, {
      id: 'delegated-work:bounded:overflow', assigneePersonId: 'crew:0', status: 'active', committedAtWorldTime: 0, progressIntervals: 0
    })).toThrow('simulation catch-up rejected')
    expect(delegated.delegatedWork).toHaveLength(24)
    expect(CAUSAL_HISTORY_LIMITS.retainedCommands).toBe(8)
  })

  it('rejects unsafe or unclassified detailed and summary scheduler records at validation and public-transition boundaries', () => {
    const source = advanceFoundationWorldTime(
      chooseInitialCourier(createFoundationWorld({ seed: 'scheduler-content-boundaries', configuration: { preset: 'sheltered-reach' } }), 'crew:0'),
      temporalAction('travel:content-boundaries', 240)
    )
    const detailed = source.state.simulation.records.find(record => record.outcomeDetail === 'detailed')
    const summary = source.state.simulation.records.find(record => record.outcomeDetail === 'summary')
    if (!detailed || !summary) throw new Error('expected both scheduler output forms')
    const cases: readonly [string, (world: typeof source) => void][] = [
      ['detailed record', world => { (world.state.simulation.records.find(record => record.id === detailed.id)!.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present' }],
      ['detailed cause', world => { delete (world.state.simulation.records.find(record => record.id === detailed.id)!.cause as { contentSafety?: unknown }).contentSafety }],
      ['summary record', world => { delete (world.state.simulation.records.find(record => record.id === summary.id)! as { contentSafety?: unknown }).contentSafety }],
      ['summary cause', world => { (world.state.simulation.records.find(record => record.id === summary.id)!.cause.contentSafety.exclusions as unknown as Record<string, string>).slavery = 'present' }]
    ]

    for (const [, tamper] of cases) {
      const forged = structuredClone(source)
      tamper(forged)
      const before = structuredClone(forged)
      expect(validateFoundationWorld(forged)).not.toEqual([])
      expect(() => advanceFoundationWorldTime(forged, temporalAction('wait:rejected-content', 1))).toThrow('complete medieval foundation contract')
      expect(forged).toEqual(before)
    }
  })

  it('preserves replay and immutable evidence through an era threshold before a compacted journal continues', () => {
    let world = chooseInitialCourier(createFoundationWorld({ seed: 'replay-threshold-compaction', configuration: { preset: 'watershed' } }), 'crew:0')
    world = advanceFoundationWorldTime(world, {
      id: 'movement:replay-threshold',
      kind: 'movement',
      durationMinutes: 1,
      contentSafety: safety(),
      events: [{
        id: 'event:replay-threshold', dueAtWorldTime: 2, priority: 'ordinary',
        payload: { kind: 'action-resolution', sourceActionId: 'movement:replay-threshold', creationDigest: world.manifest.creation.digest }, contentSafety: safety()
      }]
    })
    world = advanceFoundationWorldTime(world, { id: 'travel:replay-threshold', kind: 'travel', durationMinutes: 7_199, contentSafety: safety() })
    world = recordDurableJomonGrowth(world, { id: 'growth:replay-threshold:refit', kind: 'workspace-refit', source: { kind: 'jomon-vessel', id: 'vessel:jomon' }, atWorldTime: 7_200 })
    const immutable = { manifest: structuredClone(world.manifest), jomon: structuredClone(world.jomon), crew: structuredClone(world.crew) }

    for (let index = 0; index < 5; index++) world = advanceFoundationWorldTime(world, temporalAction(`wait:replay-compaction:${index}`, 1))

    expect(world.state.era.era).toBe('ng-plus')
    expect(world.state.temporal.causalRecords.filter(record => record.kind === 'event-resolved').map(record => record.event.id)).toContain('event:replay-threshold')
    expect(world.state.causalHistory.tail).toEqual([])
    expect(world.state.causalHistory.checkpoint.sequence).toBe(9)
    expect(foundationWorldCausalHistoryMatches(world)).toBe(true)
    expect(replayFoundationWorldCausalHistory(world)).toEqual(causalReplayProjectionForWorldState(world.state))
    expect(world.manifest).toEqual(immutable.manifest)
    expect(world.jomon).toEqual(immutable.jomon)
    expect(world.crew).toEqual(immutable.crew)

    const continued = advanceFoundationWorldTime(world, temporalAction('wait:replay-after-compaction', 1))
    expect(continued.state.causalHistory.tail).toHaveLength(1)
    expect(replayFoundationWorldCausalHistory(continued)).toEqual(causalReplayProjectionForWorldState(continued.state))
  })
})
