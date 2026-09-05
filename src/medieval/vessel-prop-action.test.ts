import { beforeAll, describe, expect, it } from 'vitest'
import { causalReplayProjectionForWorldState } from './world-state'
import { initialVesselPropActionState, recordVesselPropAction, validateVesselPropActionState, vesselPropActionFeedbacks } from './vessel-prop-action'
import { causalDigestFor } from './causal-history'
import { chooseInitialCourier, createFoundationWorld, moveFoundationWorldCourier, recordVesselStationReadout, replayFoundationWorldCausalHistory, switchTavernCourier, upgradeFoundationWorldStateV15, validateFoundationWorld } from './world'
import { initialHouseholdActiveCrew } from './initial-household'

let selected = chooseInitialCourier(createFoundationWorld({ seed: 'vessel-prop-action-fixture' }), 'crew:0')
let gangplankWorld = selected

beforeAll(() => {
  for (const direction of ['south', 'west'] as const) {
    const result = moveFoundationWorldCourier(gangplankWorld, direction)
    if (result.status !== 'moved') throw new Error('expected canonical route to gangplank')
    gangplankWorld = result.world
  }
})

/** A token-valid state-v14 source used only to exercise the strict read-only bridge. */
const stateV14Envelope = () => {
  const legacy = structuredClone(selected) as unknown as Record<string, any>
  const oldProjection = structuredClone(legacy.state.causalHistory.checkpoint.projection)
  delete oldProjection.jomon
  oldProjection.version = 6
  const checkpoint = legacy.state.causalHistory.checkpoint
  const stateDigest = causalDigestFor('causal-replay-projection', oldProjection)
  const sequence = checkpoint.sequence
  checkpoint.version = 4
  checkpoint.stateDigest = stateDigest
  checkpoint.id = `causal-checkpoint:${sequence}:${causalDigestFor('causal-checkpoint-id', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence, stateDigest })}`
  checkpoint.token = causalDigestFor('causal-checkpoint-token', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence, atWorldTime: checkpoint.atWorldTime, stateDigest })
  checkpoint.projection = oldProjection
  legacy.state.causalHistory.version = 4
  legacy.state.causalHistory.tail = legacy.state.causalHistory.tail.map((command: Record<string, unknown>) => ({ ...command, version: 4 }))
  legacy.state.jomon.version = 1
  delete legacy.state.jomon.propActions
  legacy.state.version = 14
  return legacy
}

describe('bounded vessel prop actions', () => {
  it('initializes exactly the canonical eight immutable prop references without hidden data', () => {
    const state = initialVesselPropActionState(selected.jomon)

    expect(state).toEqual({
      version: 1,
      records: [
        { propId: 'prop:berth' },
        { propId: 'prop:cargo-hold-rack' },
        { propId: 'prop:chart-table' },
        { propId: 'prop:galley-hearth' },
        { propId: 'prop:gangplank' },
        { propId: 'prop:repair-space-rack' },
        { propId: 'prop:stores-rack' },
        { propId: 'prop:task-ledger' }
      ]
    })
    expect(JSON.stringify(state)).not.toContain('coordinate')
    expect(JSON.stringify(state)).not.toContain('person')
    expect(validateVesselPropActionState(selected.jomon, state)).toEqual([])
  })

  it('records an exact non-ledger anchor acknowledgement through replay without advancing world time or mutating its source', () => {
    const before = structuredClone(gangplankWorld)
    const recorded = recordVesselStationReadout(gangplankWorld, 'prop:gangplank')
    const action = recorded.state.jomon.propActions.records.find(item => item.propId === 'prop:gangplank')?.latestAction

    expect(recorded.state.temporal).toEqual(gangplankWorld.state.temporal)
    expect(action).toEqual({ kind: 'station-readout-recorded', recordedAtWorldTime: gangplankWorld.state.temporal.worldTime, causalSequence: recorded.state.causalHistory.tail.at(-1)!.sequence })
    expect(recorded.state.causalHistory.tail.at(-1)).toMatchObject({ kind: 'vessel-station-readout-recorded', payload: { propId: 'prop:gangplank' } })
    expect(replayFoundationWorldCausalHistory(recorded)).toEqual(causalReplayProjectionForWorldState(recorded.state))
    expect(validateFoundationWorld(recorded)).toEqual([])
    expect(gangplankWorld).toEqual(before)
  })

  it('replaces a prop latest action deterministically and rejects unavailable direct station records without mutation', () => {
    const first = recordVesselStationReadout(gangplankWorld, 'prop:gangplank')
    const second = recordVesselStationReadout(first, 'prop:gangplank')
    const latest = second.state.jomon.propActions.records.find(item => item.propId === 'prop:gangplank')!.latestAction!
    const before = structuredClone(selected)

    expect(latest.causalSequence).toBeGreaterThan(first.state.causalHistory.tail.at(-1)!.sequence)
    expect(second.state.temporal).toEqual(first.state.temporal)
    expect(() => recordVesselStationReadout(selected, 'prop:gangplank')).toThrow('source-anchor')
    expect(selected).toEqual(before)
  })

  it('uses the existing tavern command to record the task ledger rather than adding a second prop command', () => {
    const target = initialHouseholdActiveCrew(selected.crew).find(member => member.id !== selected.state.courier.activeCourierId)!
    const switched = switchTavernCourier(selected, target.id)
    const action = switched.state.jomon.propActions.records.find(item => item.propId === 'prop:task-ledger')!.latestAction

    expect(switched.state.causalHistory.tail.map(command => command.kind)).toEqual(['initial-courier-selected', 'tavern-courier-switched'])
    expect(action).toEqual({ kind: 'tavern-courier-switched', recordedAtWorldTime: 0, causalSequence: 2 })
    expect(replayFoundationWorldCausalHistory(switched)).toEqual(causalReplayProjectionForWorldState(switched.state))
  })

  it('fails closed for reordered, unknown, unsafe-shape, stale-sequence, and impossible prop/action records', () => {
    const initial = initialVesselPropActionState(selected.jomon)
    const valid = recordVesselPropAction(selected.jomon, initial, 'prop:chart-table', { kind: 'station-readout-recorded', recordedAtWorldTime: 0, causalSequence: 1 })
    const reordered = structuredClone(valid) as { version: number; records: Array<{ propId: string; latestAction?: { kind: string; recordedAtWorldTime: number; causalSequence: number } }> }
    ;[reordered.records[0], reordered.records[1]] = [reordered.records[1]!, reordered.records[0]!]
    const impossible = structuredClone(valid)
    ;(impossible.records.find(item => item.propId === 'prop:chart-table')!.latestAction as { kind: string }).kind = 'tavern-courier-switched'
    const unexpected = structuredClone(valid) as unknown as { records: Array<Record<string, unknown>> }
    unexpected.records[0]!.coordinate = { column: 1, row: 1 }

    expect(validateVesselPropActionState(selected.jomon, reordered)).not.toEqual([])
    expect(validateVesselPropActionState(selected.jomon, impossible)).not.toEqual([])
    expect(validateVesselPropActionState(selected.jomon, unexpected)).not.toEqual([])
    expect(() => recordVesselPropAction(selected.jomon, valid, 'prop:chart-table', { kind: 'station-readout-recorded', recordedAtWorldTime: 0, causalSequence: 1 })).toThrow('invalid-sequence')
    expect(vesselPropActionFeedbacks(valid)).toMatchObject([{ propId: 'prop:chart-table', text: 'Chart table: bounded station readout recorded.' }])
  })

  it('read-only upgrades an exact v15/state-v14 envelope and leaves forged legacy data untouched', () => {
    const legacy = stateV14Envelope()
    const before = structuredClone(legacy)

    const upgraded = upgradeFoundationWorldStateV15(legacy)

    expect(upgraded.state.version).toBe(15)
    expect(upgraded.state.jomon).toMatchObject({ version: 2, propActions: initialVesselPropActionState(upgraded.jomon) })
    expect(validateFoundationWorld(upgraded)).toEqual([])
    expect(legacy).toEqual(before)

    const forged = stateV14Envelope()
    forged.state.jomon.capacity.cargoUnits = 101
    const forgedBefore = structuredClone(forged)
    expect(() => upgradeFoundationWorldStateV15(forged)).toThrow('v15/state-v14 envelope is invalid')
    expect(forged).toEqual(forgedBefore)
  })
})
