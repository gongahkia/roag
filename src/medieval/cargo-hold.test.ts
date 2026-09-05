import { beforeAll, describe, expect, it } from 'vitest'
import { cargoLotIdFor, cargoUnitsPerCommodityUnit, initialVesselCargoState, loadVesselCargo, recoverVesselCargo, resolveVesselCargoFailure, unloadVesselCargo, validateVesselCargoState, vesselCargoUsedUnits } from './cargo-hold'
import { causalDigestFor } from './causal-history'
import { chooseInitialCourier, createFoundationWorld, loadCargoHold, moveFoundationWorldCourier, recordCargoHoldFailure, recoverCargoHold, replayFoundationWorldCausalHistory, unloadCargoHold, upgradeFoundationWorldStateV16, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'
import { createTerminalPresentationModel, validateTerminalPresentationModel } from './terminal-presentation'

/** Six eastward canonical steps cross tavern → chart table → cargo hold. */
const atCargoHold = (seed: string) => {
  let world = chooseInitialCourier(createFoundationWorld({ seed }), 'crew:0')
  for (const direction of ['east', 'east', 'east', 'east', 'east', 'east'] as const) {
    const moved = moveFoundationWorldCourier(world, direction)
    if (moved.status !== 'moved') throw new Error('planned cargo-hold step was blocked')
    world = moved.world
  }
  return world
}

let cargoHoldWorld: ReturnType<typeof atCargoHold>
let loadedCargoHoldWorld: ReturnType<typeof loadCargoHold>
let lostCargoHoldWorld: ReturnType<typeof recordCargoHoldFailure>
let recoveredCargoHoldWorld: ReturnType<typeof recoverCargoHold>
beforeAll(() => {
  cargoHoldWorld = atCargoHold('cargo-world-replay')
  loadedCargoHoldWorld = loadCargoHold(cargoHoldWorld, 'commodity:grain', 1)
  const cargoId = loadedCargoHoldWorld.state.jomon.cargo.lots[0]!.id
  lostCargoHoldWorld = recordCargoHoldFailure(loadedCargoHoldWorld, cargoId, 'loss')
  recoveredCargoHoldWorld = recoverCargoHold(lostCargoHoldWorld, cargoId)
})

/** A token-valid v15 source is accepted only by the explicit read-only cargo upgrade. */
const stateV15Envelope = () => {
  const legacy = structuredClone(chooseInitialCourier(createFoundationWorld({ seed: 'cargo-v15-upgrade' }), 'crew:0')) as unknown as Record<string, any>
  const checkpoint = legacy.state.causalHistory.checkpoint
  const projection = structuredClone(checkpoint.projection)
  projection.version = 7
  projection.jomon.version = 2
  delete projection.jomon.cargo
  const stateDigest = causalDigestFor('causal-replay-projection', projection)
  checkpoint.version = 5
  checkpoint.stateDigest = stateDigest
  checkpoint.id = `causal-checkpoint:${checkpoint.sequence}:${causalDigestFor('causal-checkpoint-id', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: checkpoint.sequence, stateDigest })}`
  checkpoint.token = causalDigestFor('causal-checkpoint-token', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: checkpoint.sequence, atWorldTime: checkpoint.atWorldTime, stateDigest })
  checkpoint.projection = projection
  legacy.state.causalHistory.version = 5
  legacy.state.causalHistory.tail = legacy.state.causalHistory.tail.map((command: Record<string, unknown>) => ({ ...command, version: 5 }))
  legacy.state.jomon.version = 2
  delete legacy.state.jomon.cargo
  legacy.state.version = 15
  return legacy
}

describe('bounded vessel cargo hold', () => {
  it('starts empty and derives capacity use only from the closed commodity weight/bulk catalogue', () => {
    const state = initialVesselCargoState()

    expect(state).toEqual({ version: 1, lots: [] })
    expect(cargoUnitsPerCommodityUnit('commodity:paper')).toBe(1)
    expect(cargoUnitsPerCommodityUnit('commodity:grain')).toBe(4)
    expect(validateVesselCargoState(state, 12)).toEqual([])
    expect(JSON.stringify(state)).not.toContain('market')
    expect(JSON.stringify(state)).not.toContain('coordinate')
    expect(JSON.stringify(state)).not.toContain('person')
  })

  it('loads, unloads, fails, loses, and recovers bounded canonical lots without inventing a commodity', () => {
    const loaded = loadVesselCargo(initialVesselCargoState(), 12, 2, 'commodity:timber', 2)
    const damaged = resolveVesselCargoFailure(loaded, 12, cargoLotIdFor(2, 'commodity:timber'), 'damage')
    const lost = resolveVesselCargoFailure(damaged, 12, cargoLotIdFor(2, 'commodity:timber'), 'loss')
    const recovered = recoverVesselCargo(lost, 12, cargoLotIdFor(2, 'commodity:timber'))
    const unloaded = unloadVesselCargo(recovered, 12, cargoLotIdFor(2, 'commodity:timber'))

    expect(loaded.lots).toEqual([{ id: 'cargo:2:commodity:timber', commodityId: 'commodity:timber', quantity: 2, condition: 'sound', status: 'in-hold' }])
    expect(vesselCargoUsedUnits(loaded)).toBe(8)
    expect(damaged.lots[0]).toMatchObject({ condition: 'damaged', status: 'in-hold' })
    expect(lost.lots[0]).toMatchObject({ condition: 'damaged', status: 'lost' })
    expect(vesselCargoUsedUnits(lost)).toBe(0)
    expect(recovered.lots[0]).toMatchObject({ condition: 'damaged', status: 'in-hold' })
    expect(unloaded).toEqual(initialVesselCargoState())
  })

  it('fails closed for capacity overflow, incompatible failure, forged content, and invalid recovery without mutation', () => {
    const loaded = loadVesselCargo(initialVesselCargoState(), 12, 2, 'commodity:timber', 2)
    const before = structuredClone(loaded)
    const overflow = () => loadVesselCargo(loaded, 12, 3, 'commodity:grain', 2)
    const invalidSpoilage = () => resolveVesselCargoFailure(loaded, 12, loaded.lots[0]!.id, 'spoilage')
    const invalidRecovery = () => recoverVesselCargo(loaded, 12, loaded.lots[0]!.id)
    const forged = structuredClone(loaded) as unknown as { lots: Array<Record<string, unknown>> }
    forged.lots[0]!.commodityId = 'commodity:not-present'
    const extended = structuredClone(loaded) as unknown as { lots: Array<Record<string, unknown>> }
    extended.lots[0]!.coordinate = { column: 10, row: 4 }

    expect(overflow).toThrow('capacity-exceeded')
    expect(invalidSpoilage).toThrow('invalid-condition')
    expect(invalidRecovery).toThrow('invalid-lot')
    expect(validateVesselCargoState(forged, 12)).toContain('vessel-cargo.unknown-commodity')
    expect(validateVesselCargoState(extended, 12)).toContain('vessel-cargo.invalid-lot')
    expect(loaded).toEqual(before)
  })

  it('projects an exact-anchor cargo load through terminal presentation without advancing world time', () => {
    const loaded = structuredClone(loadedCargoHoldWorld)
    const cargoId = loaded.state.jomon.cargo.lots[0]!.id

    expect(loaded.state.temporal).toEqual(cargoHoldWorld.state.temporal)
    expect(loaded.state.jomon.cargo.lots).toEqual([{ id: cargoId, commodityId: 'commodity:grain', quantity: 1, condition: 'sound', status: 'in-hold' }])
    const terminal = createTerminalPresentationModel(loaded)
    const cargoPrompt = terminal.prompts.find(prompt => prompt.kind === 'vessel-station-readout')
    expect(cargoPrompt).toMatchObject({
      source: { propId: 'prop:cargo-hold-rack' },
      readout: { value: { kind: 'cargo-capacity', cargoUnits: 12, usedUnits: 4, lots: [{ commodityId: 'commodity:grain', quantity: 1, condition: 'sound', status: 'in-hold' }] } }
    })
    expect(JSON.stringify(cargoPrompt)).not.toContain(cargoId)
    expect(terminal.messages).toEqual(expect.arrayContaining([expect.objectContaining({ kind: 'vessel-prop-action', value: expect.objectContaining({ propId: 'prop:cargo-hold-rack', action: 'vessel-cargo-loaded', recordedAtWorldTime: loaded.state.temporal.worldTime }) })]))
    expect(validateTerminalPresentationModel(loaded, terminal)).toEqual([])
    expect(cargoHoldWorld.state.jomon.cargo).toEqual(initialVesselCargoState())
  })

  it('replays loss and recovery through compaction without changing the source world', () => {
    const recovered = structuredClone(recoveredCargoHoldWorld)

    expect(lostCargoHoldWorld.state.jomon.cargo.lots[0]).toMatchObject({ status: 'lost' })
    expect(recovered.state.jomon.cargo.lots[0]).toMatchObject({ condition: 'damaged', status: 'in-hold' })
    expect(recovered.state.temporal).toEqual(cargoHoldWorld.state.temporal)
    expect(recovered.state.causalHistory.tail.map(command => command.kind)).toEqual(['vessel-cargo-recovered'])
    expect(recovered.state.causalHistory.compactedSegments.some(segment => (segment.commandKinds.vesselCargoLoaded ?? 0) === 1 && (segment.commandKinds.vesselCargoFailureResolved ?? 0) === 1)).toBe(true)
    expect(replayFoundationWorldCausalHistory(recovered)).toEqual(causalReplayProjectionForWorldState(recovered.state))
    expect(validateFoundationWorld(recovered)).toEqual([])
    expect(cargoHoldWorld.state.jomon.cargo).toEqual(initialVesselCargoState())
  })

  it('unloads an exact current hold lot through its own typed causal transition', () => {
    const loaded = structuredClone(loadedCargoHoldWorld)
    const unloaded = unloadCargoHold(loaded, loaded.state.jomon.cargo.lots[0]!.id)

    expect(unloaded.state.jomon.cargo).toEqual(initialVesselCargoState())
    expect(unloaded.state.temporal).toEqual(loaded.state.temporal)
    expect(unloaded.state.causalHistory.compactedSegments.some(segment => (segment.commandKinds.vesselCargoUnloaded ?? 0) === 1)).toBe(true)
    expect(replayFoundationWorldCausalHistory(unloaded)).toEqual(causalReplayProjectionForWorldState(unloaded.state))
  })

  it('rejects off-anchor and forged cargo transitions without mutation, and upgrades only an exact v15 source', () => {
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'cargo-off-anchor' }), 'crew:0')
    const before = structuredClone(selected)
    expect(() => loadCargoHold(selected, 'commodity:paper', 1)).toThrow('exact cargo-hold anchor')
    expect(selected).toEqual(before)

    const legacy = stateV15Envelope()
    const legacyBefore = structuredClone(legacy)
    const upgraded = upgradeFoundationWorldStateV16(legacy)
    expect(upgraded.state).toMatchObject({ version: 16, jomon: { version: 3, cargo: initialVesselCargoState() } })
    expect(validateFoundationWorld(upgraded)).toEqual([])
    expect(legacy).toEqual(legacyBefore)

    const forged = stateV15Envelope()
    forged.state.jomon.propActions.records.reverse()
    const forgedBefore = structuredClone(forged)
    expect(() => upgradeFoundationWorldStateV16(forged)).toThrow('v15/state-v15 envelope is invalid')
    expect(forged).toEqual(forgedBefore)
  })
})
