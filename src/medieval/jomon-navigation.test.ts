import { describe, expect, it } from 'vitest'
import { causalDigestFor, causalReplayProjectionDigest } from './causal-history'
import { assessJomonDeckStep, canonicalJomonDeckSpawn, isWalkableJomonDeckCoordinate } from './jomon-navigation'
import { causalReplayProjectionForWorldState } from './world-state'
import { chooseInitialCourier, createFoundationWorld, moveFoundationWorldCourier, replayFoundationWorldCausalHistory, upgradeFoundationWorldV13, validateFoundationWorld } from './world'

const removeSettlementTradingFromLegacy = (legacy: Record<string, any>): void => {
  delete legacy.state.settlementTrading
  legacy.state.contentSafetyAudit.reviewed = legacy.state.contentSafetyAudit.reviewed
    .filter((item: { id: string }) => !item.id.startsWith('settlement-trading:'))
}

const selectedWorld = (seed: string) => {
  const source = createFoundationWorld({ seed, configuration: { preset: 'watershed' } })
  return chooseInitialCourier(source, source.crew[0]!.id)
}

/** Produces an authentic pre-navigation envelope only for conversion coverage. */
const v13Envelope = (world: ReturnType<typeof selectedWorld>) => {
  const legacy = structuredClone(world) as unknown as Record<string, any>
  legacy.version = 13
  legacy.jomon.props = ['prop:chart-table', 'prop:task-ledger', 'prop:gangplank'].map(id => structuredClone(world.jomon.props.find(prop => prop.id === id)!))
  legacy.state.version = 11
  delete legacy.state.navigation
  legacy.state.courier = legacy.state.courier.initialCourierId === undefined
    ? { version: 1 }
    : { version: 1, initialCourierId: legacy.state.courier.initialCourierId }
  const checkpointProjection = structuredClone(legacy.state.causalHistory.checkpoint.projection)
  delete checkpointProjection.jomon
  delete checkpointProjection.settlementTrading
  checkpointProjection.version = 4
  checkpointProjection.courier = checkpointProjection.courier.initialCourierId === undefined
    ? { version: 1 }
    : { version: 1, initialCourierId: checkpointProjection.courier.initialCourierId }
  delete checkpointProjection.navigation
  const stateDigest = causalReplayProjectionDigest(checkpointProjection)
  const checkpoint = {
    version: 4,
    id: `causal-checkpoint:0:${causalDigestFor('causal-checkpoint-id', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: 0, stateDigest })}`,
    token: causalDigestFor('causal-checkpoint-token', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: 0, atWorldTime: 0, stateDigest }),
    sequence: 0,
    atWorldTime: 0,
    provenanceDigest: legacy.manifest.creation.digest,
    stateDigest,
    projection: checkpointProjection
  }
  legacy.state.causalHistory = {
    ...legacy.state.causalHistory,
    version: 4,
    tail: legacy.state.causalHistory.tail.map((command: Record<string, unknown>) => ({ ...command, version: 4 })),
    checkpoint
  }
  legacy.state.jomon.version = 1
  delete legacy.state.jomon.propActions
  delete legacy.state.jomon.cargo
  removeSettlementTradingFromLegacy(legacy)
  return legacy
}

describe('Jomon deck navigation', () => {
  it('assigns the deterministic tavern spawn at zero-time selection without changing immutable provenance', () => {
    const source = createFoundationWorld({ seed: 'navigation-spawn', configuration: { preset: 'watershed' } })
    const before = structuredClone(source)
    const selected = chooseInitialCourier(source, source.crew[0]!.id)

    expect(canonicalJomonDeckSpawn(selected)).toEqual({ column: 4, row: 4 })
    expect(selected.state.navigation).toEqual({ version: 1, courierId: source.crew[0]!.id, coordinate: { column: 4, row: 4 } })
    expect(selected.state.temporal.worldTime).toBe(0)
    expect(selected.state.temporal.actionSequence).toBe(0)
    expect(source).toEqual(before)
  })

  it('moves one cardinal deck cell as a one-minute replayed action and compacts it canonically', () => {
    let world = selectedWorld('navigation-step')
    const original = structuredClone(world)
    const first = moveFoundationWorldCourier(world, 'north')

    expect(first).toMatchObject({ status: 'moved', direction: 'north', from: { column: 4, row: 4 }, to: { column: 4, row: 3 } })
    if (first.status !== 'moved') throw new Error('north from the canonical tavern spawn must be walkable')
    expect(first.world.state.temporal.worldTime).toBe(1)
    expect(first.world.state.temporal.actionSequence).toBe(1)
    expect(first.world.state.temporal.causalRecords.at(-1)).toMatchObject({
      kind: 'action-completed',
      actionKind: 'movement',
      durationMinutes: 1,
      startedAtWorldTime: 0,
      atWorldTime: 1,
      scheduledEventIds: []
    })
    expect(first.world.state.causalHistory.tail.at(-1)).toMatchObject({ kind: 'deck-moved', payload: { direction: 'north', from: { column: 4, row: 4 }, to: { column: 4, row: 3 } } })
    expect(replayFoundationWorldCausalHistory(first.world)).toEqual(causalReplayProjectionForWorldState(first.world.state))
    expect(world).toEqual(original)

    world = first.world
    for (let index = 0; index < 8; index++) {
      const result = moveFoundationWorldCourier(world, index % 2 === 0 ? 'south' : 'north')
      if (result.status !== 'moved') throw new Error('canonical stores/tavern path must remain walkable')
      world = result.world
    }
    expect(world.state.causalHistory.tail).toHaveLength(1)
    expect(world.state.causalHistory.tail[0]?.kind).toBe('deck-moved')
    expect(world.state.causalHistory.compactedSegments.at(-1)?.commandKinds.deckMoved).toBe(8)
    expect(replayFoundationWorldCausalHistory(world)).toEqual(causalReplayProjectionForWorldState(world.state))
  }, 15_000)

  it('blocks hull and diagonal corner cuts without advancing time, history, or coordinate', () => {
    const world = selectedWorld('navigation-collision')
    const before = structuredClone(world)

    expect(assessJomonDeckStep(world, world.state.navigation.coordinate!, 'west')).toMatchObject({ status: 'blocked', collision: 'hull-boundary' })
    expect(moveFoundationWorldCourier(world, 'west')).toMatchObject({ status: 'blocked', collision: 'hull-boundary' })
    expect(moveFoundationWorldCourier(world, 'north-west')).toMatchObject({ status: 'blocked', collision: 'diagonal-corner' })
    expect(world).toEqual(before)
  })

  it('rejects tampered positions and converts valid v13 envelopes by rebasing only navigation replay evidence', () => {
    const selected = selectedWorld('navigation-upgrade')
    const tampered = structuredClone(selected)
    tampered.state.navigation.coordinate = { column: 3, row: 4 }
    expect(isWalkableJomonDeckCoordinate(selected, tampered.state.navigation.coordinate)).toBe(false)
    expect(validateFoundationWorld(tampered).map(item => item.code)).toContain('foundation-world.invalid-navigation')

    const legacy = v13Envelope(selected)
    const upgraded = upgradeFoundationWorldV13(legacy)
    expect(upgraded).toMatchObject({ version: 15, state: { version: 17, jomon: { version: 3, cargo: { version: 1, lots: [] } }, settlementTrading: { version: 1, contracts: [{ status: 'offered' }] }, courier: { version: 3, initialCourierId: selected.state.courier.initialCourierId, activeCourierId: selected.state.courier.initialCourierId, departedCourierIds: [] }, navigation: { courierId: selected.state.courier.initialCourierId, coordinate: { column: 4, row: 4 } } } })
    expect(upgraded.id).toBe(selected.id)
    expect(upgraded.manifest).toEqual(selected.manifest)
    expect(validateFoundationWorld(upgraded)).toEqual([])
    expect(() => upgradeFoundationWorldV13({ ...legacy, id: 'world:forged' })).toThrow('invalid')
  })

  it('keeps an unselected v13 courier coordinate absent through the strict conversion', () => {
    const source = createFoundationWorld({ seed: 'navigation-upgrade-unselected' })
    const legacy = v13Envelope(source as ReturnType<typeof selectedWorld>)

    const upgraded = upgradeFoundationWorldV13(legacy)

    expect(upgraded.state.courier).toEqual({ version: 3 })
    expect(upgraded.state.navigation).toEqual({ version: 1 })
    expect(upgraded.state.temporal).toMatchObject({ worldTime: 0, actionSequence: 0 })
    expect(validateFoundationWorld(upgraded)).toEqual([])
  })
})
