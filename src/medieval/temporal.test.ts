import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { advanceMedievalTemporalState, createMedievalTemporalState, isMedievalTemporalState, MEDIEVAL_TEMPORAL_CONTRACT_VERSION, MEDIEVAL_TIME_UNIT, TEMPORAL_ACTION_DURATION_LIMITS, TEMPORAL_LIMITS, TemporalContractError, temporalRandomInteger, temporalStreamSeedFor, temporalTimeAfterAction, validateMedievalTemporalState, type TemporalEventRequest, type TemporalProvenance, type TimeBearingTemporalAction } from './temporal'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, recreateFoundationWorld } from './world'

const safety = () => classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])

const provenance = (): TemporalProvenance => ({
  version: MEDIEVAL_TEMPORAL_CONTRACT_VERSION,
  worldId: 'world:temporal-specimen',
  creationDigest: 'temporal-digest-1',
  seed: 'temporal specimen'
})

const event = (id: string, sourceActionId: string, creationDigest: string, dueAtWorldTime = 1, priority: TemporalEventRequest['priority'] = 'ordinary'): TemporalEventRequest => ({
  id,
  dueAtWorldTime,
  priority,
  payload: { kind: 'action-resolution', sourceActionId, creationDigest },
  contentSafety: safety()
})

const action = (id: string, kind: TimeBearingTemporalAction['kind'] = 'movement', durationMinutes = 1, events: readonly TemporalEventRequest[] = []): TimeBearingTemporalAction => ({
  id,
  kind,
  durationMinutes,
  contentSafety: safety(),
  ...(events.length ? { events } : {})
})

const codes = (error: unknown): readonly string[] => error instanceof TemporalContractError ? error.diagnostics.map(item => item.code) : []

describe('medieval action clock and deterministic scheduler', () => {
  it('creates and reconstructs a zero-time scheduler without an RNG cursor', () => {
    const world = createFoundationWorld({ seed: 'clock zero' })

    expect(world.worldTime).toBe(0)
    expect(world.temporal).toMatchObject({
      version: MEDIEVAL_TEMPORAL_CONTRACT_VERSION,
      timeUnit: MEDIEVAL_TIME_UNIT,
      worldTime: 0,
      actionSequence: 0,
      nextEventSequence: 0,
      pendingEvents: [],
      causalRecords: []
    })
    expect(recreateFoundationWorld(world.manifest)).toEqual(world)
  })

  it('replays equivalent state-changing actions identically and preserves immutable creation provenance', () => {
    const first = chooseInitialCourier(createFoundationWorld({ seed: 'clock replay' }), 'crew:0')
    const second = chooseInitialCourier(createFoundationWorld({ seed: 'clock replay' }), 'crew:0')
    const scheduled = action('movement:quay', 'movement', 1, [event('event:quay', 'movement:quay', first.manifest.creation.digest)])

    const nextFirst = advanceFoundationWorldTime(first, scheduled)
    const nextSecond = advanceFoundationWorldTime(second, scheduled)

    expect(nextFirst).toEqual(nextSecond)
    expect(nextFirst.worldTime).toBe(1)
    expect(nextFirst.temporal.actionSequence).toBe(1)
    expect(nextFirst.temporal.pendingEvents).toEqual([])
    expect(nextFirst.temporal.causalRecords.map(record => record.kind)).toEqual(['action-completed', 'event-resolved'])
    expect(nextFirst.causalHistory.map(record => record.kind)).toEqual(['world-created', 'initial-courier-selected', 'temporal-action', 'scheduled-event-resolved'])
    expect(nextFirst.manifest).toEqual(first.manifest)
  })

  it('rejects inspection and browser/UI commands without mutating time, sequence, queue, or streams', () => {
    const initial = createMedievalTemporalState(provenance())
    const before = structuredClone(initial)

    for (const kind of ['inspect', 'settings-opened', 'settings-closed', 'route-changed', 'browser-idle', 'browser-paused', 'browser-reloaded', 'ui-event'] as const) {
      try { advanceMedievalTemporalState(initial, { kind }) } catch (error) { expect(codes(error)).toEqual(['temporal.pure-command']) }
      expect(initial).toEqual(before)
    }
  })

  it('recognizes every closed time-bearing action family and advances only its explicit duration', () => {
    let state = createMedievalTemporalState(provenance())

    for (const [kind, limits] of Object.entries(TEMPORAL_ACTION_DURATION_LIMITS) as [TimeBearingTemporalAction['kind'], { minimumMinutes: number }][]) {
      const transition = advanceMedievalTemporalState(state, action(`${kind}:contract`, kind, limits.minimumMinutes))
      expect(transition.state.worldTime).toBe(state.worldTime + limits.minimumMinutes)
      state = transition.state
    }
    expect(state.actionSequence).toBe(Object.keys(TEMPORAL_ACTION_DURATION_LIMITS).length)
  })

  it('uses canonical event ordering even when equivalent requests arrive in a different insertion order', () => {
    const first = createMedievalTemporalState(provenance())
    const second = createMedievalTemporalState(provenance())
    const firstEvents = [
      event('event:zeta', 'work:sorting', first.provenance.creationDigest, 1, 'ordinary'),
      event('event:alpha', 'work:sorting', first.provenance.creationDigest, 1, 'ordinary'),
      event('event:urgent', 'work:sorting', first.provenance.creationDigest, 1, 'urgent')
    ]
    const firstResult = advanceMedievalTemporalState(first, action('work:sorting', 'work', 1, firstEvents))
    const secondResult = advanceMedievalTemporalState(second, action('work:sorting', 'work', 1, [...firstEvents].reverse()))

    expect(secondResult.state).toEqual(firstResult.state)
    expect(firstResult.dueEvents.map(result => result.eventId)).toEqual(['event:urgent', 'event:alpha', 'event:zeta'])
    expect(firstResult.dueEvents.map(result => result.sequence)).toEqual([0, 1, 2])
  })

  it('derives named random values from immutable provenance and identity, never a mutable cursor', () => {
    const source = provenance()
    const first = temporalStreamSeedFor(source, 'event-resolution', 'event:reed')

    expect(first).toBe(temporalStreamSeedFor(structuredClone(source), 'event-resolution', 'event:reed'))
    expect(first).not.toBe(temporalStreamSeedFor(source, 'action', 'event:reed'))
    expect(first).not.toBe(temporalStreamSeedFor(source, 'event-resolution', 'event:other'))
    expect(temporalRandomInteger(source, 'event-resolution', 'event:reed', 97)).toBe(temporalRandomInteger(source, 'event-resolution', 'event:reed', 97))
  })

  it('keeps future events through serializable reload data and resolves them at the next qualifying action', () => {
    const initial = createMedievalTemporalState(provenance())
    const scheduled = advanceMedievalTemporalState(initial, action('movement:bank', 'movement', 1, [event('event:bank', 'movement:bank', initial.provenance.creationDigest, 2)]))
    const restored = structuredClone(scheduled.state)

    expect(isMedievalTemporalState(restored, initial.provenance)).toBe(true)
    expect(restored.pendingEvents.map(item => item.id)).toEqual(['event:bank'])
    const resolved = advanceMedievalTemporalState(restored, action('wait:bank', 'wait', 1))

    expect(resolved.state.worldTime).toBe(2)
    expect(resolved.dueEvents.map(result => result.eventId)).toEqual(['event:bank'])
    expect(resolved.state.pendingEvents).toEqual([])
  })

  it('fails closed on duplicate event IDs, past scheduling, and noncanonical persisted event ordering', () => {
    const initial = createMedievalTemporalState(provenance())
    const duplicate = action('movement:duplicate', 'movement', 1, [
      event('event:once', 'movement:duplicate', initial.provenance.creationDigest, 4),
      event('event:once', 'movement:duplicate', initial.provenance.creationDigest, 5)
    ])
    try { advanceMedievalTemporalState(initial, duplicate) } catch (error) { expect(codes(error)).toContain('temporal.duplicate-event-id') }

    const afterMove = advanceMedievalTemporalState(initial, action('movement:past', 'movement', 1))
    try { advanceMedievalTemporalState(afterMove.state, action('work:past', 'work', 1, [event('event:past', 'work:past', initial.provenance.creationDigest, 0)])) } catch (error) { expect(codes(error)).toContain('temporal.past-event') }
    try { advanceMedievalTemporalState(afterMove.state, action('movement:past', 'movement', 1)) } catch (error) { expect(codes(error)).toContain('temporal.duplicate-action-id') }

    const pending = advanceMedievalTemporalState(initial, action('work:ordered', 'work', 1, [
      event('event:last', 'work:ordered', initial.provenance.creationDigest, 5, 'ordinary'),
      event('event:first', 'work:ordered', initial.provenance.creationDigest, 4, 'deferred')
    ]))
    const reordered = structuredClone(pending.state) as unknown as { pendingEvents: unknown[] }
    reordered.pendingEvents.reverse()

    expect(validateMedievalTemporalState(reordered).diagnostics.map(item => item.code)).toContain('temporal.invalid-event-order')
    expect(isMedievalTemporalState(reordered)).toBe(false)
  })

  it('rejects invalid temporal actions, payloads, content, overflow-state, and bounded queue input with stable diagnostics', () => {
    const initial = createMedievalTemporalState(provenance())
    const unsafe = safety()
    ;(unsafe.exclusions as unknown as Record<string, string>).torture = 'present'
    const invalidCases: readonly { command: unknown; code: string }[] = [
      { command: action('movement:zero', 'movement', 0), code: 'temporal.invalid-action-duration' },
      { command: action('movement:unsafe', 'movement', 1, [event('event:unsafe', 'movement:unsafe', initial.provenance.creationDigest, 1, 'ordinary'), { ...event('event:unsafe-content', 'movement:unsafe', initial.provenance.creationDigest), contentSafety: unsafe }]), code: 'content-safety.prohibited.torture' },
      { command: action('movement:wrong-proof', 'movement', 1, [event('event:wrong-proof', 'movement:wrong-proof', 'wrong-digest')]), code: 'temporal.invalid-event-provenance' },
      { command: action('movement:queue', 'movement', 1, Array.from({ length: TEMPORAL_LIMITS.pendingEvents + 1 }, (_, index) => event(`event:queue:${index}`, 'movement:queue', initial.provenance.creationDigest, 100))), code: 'temporal.queue-limit' }
    ]

    for (const invalid of invalidCases) {
      try { advanceMedievalTemporalState(initial, invalid.command) } catch (error) { expect(codes(error)).toContain(invalid.code) }
      expect(initial.worldTime).toBe(0)
      expect(initial.pendingEvents).toEqual([])
    }
    const overflow = structuredClone(initial) as unknown as { worldTime: number }
    overflow.worldTime = Number.MAX_SAFE_INTEGER + 1
    expect(isMedievalTemporalState(overflow)).toBe(false)
    expect(() => temporalTimeAfterAction(Number.MAX_SAFE_INTEGER, 1)).toThrow(TemporalContractError)
    try { temporalTimeAfterAction(Number.MAX_SAFE_INTEGER, 1) } catch (error) { expect(codes(error)).toEqual(['temporal.action-time-overflow']) }
  })

  it('rejects an action that would exceed the bounded causal-record history before mutating state', () => {
    let state = createMedievalTemporalState(provenance())
    const eventsFor = (batch: number): readonly TemporalEventRequest[] => Array.from({ length: TEMPORAL_LIMITS.pendingEvents }, (_, index) => event(`event:batch:${batch}:${index}`, `work:batch:${batch}`, state.provenance.creationDigest, state.worldTime + 1))

    for (let batch = 0; batch < 7; batch++) state = advanceMedievalTemporalState(state, action(`work:batch:${batch}`, 'work', 1, eventsFor(batch))).state
    expect(state.causalRecords).toHaveLength(7 * (TEMPORAL_LIMITS.pendingEvents + 1))
    const before = structuredClone(state)

    try { advanceMedievalTemporalState(state, action('work:batch:7', 'work', 1, eventsFor(7))) } catch (error) { expect(codes(error)).toEqual(['temporal.causal-record-limit']) }
    expect(state).toEqual(before)
  })
})
