import { describe, expect, it } from 'vitest'
import { CAUSAL_HISTORY_LIMITS, appendCausalCommand, createCausalCommand, createCausalHistoryState, validateCausalHistoryState } from './causal-history'
import { classifyMedievalContent } from './content-safety'
import { simulationCatchUpProjection } from './simulation-catchup'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, foundationWorldCausalHistoryMatches, recordDurableJomonGrowth, replayFoundationWorldCausalHistory, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'
import { worldEraProjection } from './world-era'

const safety = () => classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['simulation-summary'])
const contextFor = (world: ReturnType<typeof createFoundationWorld>) => ({ worldId: world.id, creationDigest: world.manifest.creation.digest })
const action = (id: string, durationMinutes = 1) => ({ id, kind: 'wait' as const, durationMinutes, contentSafety: safety() })
const simulationProjectionFor = (world: ReturnType<typeof createFoundationWorld>) => simulationCatchUpProjection({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime
}, world.state.simulation)

describe('bounded canonical causal-history journal', () => {
  it('journals and replays courier selection, scheduled temporal actions, and durable Jomon growth through shared reducers', () => {
    const creation = createFoundationWorld({ seed: 'causal-history-replay' })
    const selected = chooseInitialCourier(creation, 'crew:0')
    const advanced = advanceFoundationWorldTime(selected, {
      ...action('movement:causal-scheduled'),
      kind: 'movement',
      events: [{
        id: 'event:causal-scheduled',
        dueAtWorldTime: 1,
        priority: 'ordinary',
        payload: { kind: 'action-resolution', sourceActionId: 'movement:causal-scheduled', creationDigest: selected.manifest.creation.digest },
        contentSafety: safety()
      }]
    })
    const grown = recordDurableJomonGrowth(advanced, {
      id: 'growth:causal-history:deck',
      kind: 'physical-expansion',
      source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
      atWorldTime: 1
    })

    expect(grown.state.causalHistory.tail.map(command => command.kind)).toEqual(['initial-courier-selected', 'time-bearing-action', 'durable-jomon-growth'])
    expect(grown.state.temporal.causalRecords.map(record => record.kind)).toEqual(['action-completed', 'event-resolved'])
    expect(replayFoundationWorldCausalHistory(grown)).toEqual(causalReplayProjectionForWorldState(grown.state))
    expect(foundationWorldCausalHistoryMatches(grown)).toBe(true)
    expect(validateFoundationWorld(grown)).toEqual([])
    expect(grown.manifest).toEqual(creation.manifest)
    expect(grown.jomon).toEqual(creation.jomon)
    expect(grown.crew).toEqual(creation.crew)
  })

  it('never appends a command for pure UI input or rejected temporal/growth input', () => {
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'causal-history-rejected' }), 'crew:0')
    const before = structuredClone(selected)

    expect(() => advanceFoundationWorldTime(selected, { kind: 'inspect' })).toThrow('temporal contract rejected')
    expect(() => recordDurableJomonGrowth(selected, {
      id: 'growth:invalid',
      kind: 'small-craft',
      source: { kind: 'jomon-prop', id: 'prop:not-jomon' },
      atWorldTime: 0
    })).toThrow('invalid-growth-source')
    expect(selected).toEqual(before)

    const unsafe = safety()
    ;(unsafe.exclusions as unknown as Record<string, string>).torture = 'present'
    expect(() => advanceFoundationWorldTime(selected, { ...action('wait:unsafe'), contentSafety: unsafe })).toThrow('temporal contract rejected')
    expect(selected.state.causalHistory).toEqual(before.state.causalHistory)
  })

  it('fails closed on forged, malformed, reordered, duplicated, unsafe, mismatched, and replay-mismatched command data', () => {
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'causal-history-tamper' }), 'crew:0')
    const advanced = advanceFoundationWorldTime(selected, action('wait:tamper'))
    const grown = recordDurableJomonGrowth(advanced, {
      id: 'growth:tamper',
      kind: 'workspace-refit',
      source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
      atWorldTime: 1
    })
    const context = contextFor(grown)
    const forgedToken = structuredClone(grown.state.causalHistory)
    forgedToken.tail[1]!.token = 'forged'
    const reordered = structuredClone(grown.state.causalHistory)
    reordered.tail = [...reordered.tail].reverse()
    const duplicate = structuredClone(grown.state.causalHistory)
    duplicate.tail = [...duplicate.tail, structuredClone(duplicate.tail[0]!)]
    const malformed = structuredClone(grown.state.causalHistory)
    delete (malformed.tail[1]!.payload as { action?: unknown }).action
    const unsafe = structuredClone(grown.state.causalHistory)
    ;(unsafe.tail[1]!.contentSafety.exclusions as unknown as Record<string, string>).slavery = 'present'
    const mismatchedPayload = structuredClone(grown.state.causalHistory)
    ;(mismatchedPayload.tail[0]!.payload as { courierId: string }).courierId = 'crew:1'
    const replayMismatch = structuredClone(grown)
    replayMismatch.state.courier.initialCourierId = 'crew:1'

    expect(validateCausalHistoryState(context, forgedToken).map(item => item.code)).toContain('causal-history.invalid-command-token')
    expect(validateCausalHistoryState(context, reordered).map(item => item.code)).toContain('causal-history.invalid-command-order')
    expect(validateCausalHistoryState(context, duplicate).map(item => item.code)).toEqual(expect.arrayContaining(['causal-history.duplicate-command-id', 'causal-history.invalid-command-order']))
    expect(validateCausalHistoryState(context, malformed).map(item => item.code)).toContain('causal-history.invalid-command-payload')
    expect(validateCausalHistoryState(context, unsafe).map(item => item.code)).toContain('causal-history.invalid-command-token')
    expect(validateCausalHistoryState(context, mismatchedPayload).map(item => item.code)).toContain('causal-history.invalid-command-token')
    expect(validateFoundationWorld(replayMismatch).map(item => item.code)).toContain('foundation-world.invalid-causal-history')
  })

  it('compacts an exact replay checkpoint and bounded canonical summaries without retaining an unbounded tail', () => {
    const world = createFoundationWorld({ seed: 'causal-history-compaction' })
    const context = contextFor(world)
    const projection = causalReplayProjectionForWorldState(world.state)
    let history = createCausalHistoryState(context, projection)

    for (let index = 0; index < (CAUSAL_HISTORY_LIMITS.retainedCommands + 1) * (CAUSAL_HISTORY_LIMITS.compactedSegments + 2); index++) {
      const command = createCausalCommand(context, history, 'time-bearing-action', { action: action(`wait:compact:${index}`) })
      history = appendCausalCommand(context, history, command, projection)
    }

    expect(history.tail).toEqual([])
    expect(history.checkpoint.sequence).toBe((CAUSAL_HISTORY_LIMITS.retainedCommands + 1) * (CAUSAL_HISTORY_LIMITS.compactedSegments + 2))
    expect(history.compactedSegments).toHaveLength(CAUSAL_HISTORY_LIMITS.compactedSegments)
    expect(history.compactedSegments[0]!.sequenceStart).toBe(1)
    expect(history.compactedSegments.at(-1)!.sequenceEnd).toBe(history.checkpoint.sequence)
    expect(history.compactedSegments.every(segment => segment.commandKinds.timeBearingAction > 0 && segment.worldTimeStart === 0 && segment.worldTimeEnd === 0)).toBe(true)
    expect(validateCausalHistoryState(context, history)).toEqual([])

    let reproduced = createCausalHistoryState(context, projection)
    for (let index = 0; index < (CAUSAL_HISTORY_LIMITS.retainedCommands + 1) * (CAUSAL_HISTORY_LIMITS.compactedSegments + 2); index++) {
      const command = createCausalCommand(context, reproduced, 'time-bearing-action', { action: action(`wait:compact:${index}`) })
      reproduced = appendCausalCommand(context, reproduced, command, projection)
    }
    expect(reproduced.compactedSegments).toEqual(history.compactedSegments)
  })

  it('replays an actual compacted world checkpoint without retaining the compacted commands', () => {
    let world = chooseInitialCourier(createFoundationWorld({ seed: 'causal-history-live-checkpoint' }), 'crew:0')
    for (let index = 0; index < CAUSAL_HISTORY_LIMITS.retainedCommands; index++) world = advanceFoundationWorldTime(world, action(`wait:live-checkpoint:${index}`))

    expect(world.state.causalHistory.checkpoint.sequence).toBe(CAUSAL_HISTORY_LIMITS.retainedCommands + 1)
    expect(world.state.causalHistory.tail).toEqual([])
    expect(world.state.causalHistory.compactedSegments).toHaveLength(1)
    expect(world.state.causalHistory.compactedSegments[0]).toMatchObject({ sequenceStart: 1, sequenceEnd: CAUSAL_HISTORY_LIMITS.retainedCommands + 1, commandKinds: { initialCourierSelected: 1, timeBearingAction: CAUSAL_HISTORY_LIMITS.retainedCommands, durableJomonGrowth: 0 } })
    expect(replayFoundationWorldCausalHistory(world)).toEqual(causalReplayProjectionForWorldState(world.state))
    expect(validateFoundationWorld(world)).toEqual([])

    const continued = advanceFoundationWorldTime(world, action('wait:after-live-checkpoint'))
    expect(continued.state.causalHistory.tail).toHaveLength(1)
    expect(replayFoundationWorldCausalHistory(continued)).toEqual(causalReplayProjectionForWorldState(continued.state))
  }, 30_000)

  it('keeps catch-up and era projections partition-invariant while preserving distinct command journals', () => {
    const start = chooseInitialCourier(createFoundationWorld({ seed: 'causal-history-partition' }), 'crew:0')
    const one = advanceFoundationWorldTime(start, action('wait:five', 5))
    const two = advanceFoundationWorldTime(advanceFoundationWorldTime(start, action('wait:two', 2)), action('wait:three', 3))

    expect(simulationProjectionFor(two)).toEqual(simulationProjectionFor(one))
    expect(worldEraProjection(two.state.era)).toEqual(worldEraProjection(one.state.era))
    expect(two.state.causalHistory.tail).not.toEqual(one.state.causalHistory.tail)
    expect(replayFoundationWorldCausalHistory(one)).toEqual(causalReplayProjectionForWorldState(one.state))
    expect(replayFoundationWorldCausalHistory(two)).toEqual(causalReplayProjectionForWorldState(two.state))
  })
})
