import { describe, expect, it } from 'vitest'
import { appendCausalCommand, causalDigestFor, causalReplayProjectionDigest, createCausalCommand, validateCausalHistoryState } from './causal-history'
import { createFidelityPlanForVerifiedWorld } from './fidelity'
import { initialHouseholdActiveCrew } from './initial-household'
import { moveFoundationWorldCourier } from './world'
import { assessTavernCourierSwitch, chooseInitialCourier, createFoundationWorld, replayFoundationWorldCausalHistory, switchTavernCourier, upgradeFoundationWorldV14, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'

const selectedWorld = (seed: string) => {
  const world = createFoundationWorld({ seed, configuration: { preset: 'watershed' } })
  return chooseInitialCourier(world, initialHouseholdActiveCrew(world.crew)[0]!.id)
}

/** Produces a token-valid v14/v12 envelope without writing it to storage. */
const v14Envelope = (world: ReturnType<typeof selectedWorld>) => {
  const legacy = structuredClone(world) as unknown as Record<string, any>
  legacy.state.version = 12
  legacy.state.courier = { version: 1, initialCourierId: world.state.courier.initialCourierId }
  const checkpointProjection = structuredClone(legacy.state.causalHistory.checkpoint.projection)
  checkpointProjection.version = 4
  checkpointProjection.courier = checkpointProjection.courier.initialCourierId === undefined
    ? { version: 1 }
    : { version: 1, initialCourierId: checkpointProjection.courier.initialCourierId }
  const stateDigest = causalReplayProjectionDigest(checkpointProjection)
  legacy.state.causalHistory = {
    ...legacy.state.causalHistory,
    checkpoint: {
      version: 4,
      id: `causal-checkpoint:0:${causalDigestFor('causal-checkpoint-id', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: 0, stateDigest })}`,
      token: causalDigestFor('causal-checkpoint-token', { worldId: legacy.id, creationDigest: legacy.manifest.creation.digest, sequence: 0, atWorldTime: 0, stateDigest }),
      sequence: 0,
      atWorldTime: 0,
      provenanceDigest: legacy.manifest.creation.digest,
      stateDigest,
      projection: checkpointProjection
    }
  }
  return legacy
}

describe('tavern courier switch contract', () => {
  it('derives only canonical eligible household candidates from the source-backed ledger', () => {
    const first = selectedWorld('tavern-candidates')
    const second = selectedWorld('tavern-candidates')
    const before = structuredClone(first)
    const assessment = assessTavernCourierSwitch(first)

    expect(assessment).toEqual(assessTavernCourierSwitch(second))
    expect(assessment).toMatchObject({
      version: 1,
      status: 'available',
      source: { propBindingId: 'deck-prop-binding:prop:task-ledger', propId: 'prop:task-ledger', areaId: 'tavern', coordinate: { column: 4, row: 4 } },
      current: initialHouseholdActiveCrew(first.crew)[0]
    })
    expect(assessment.candidates).toEqual(initialHouseholdActiveCrew(first.crew).slice(1))
    expect(JSON.stringify(assessment)).not.toContain(first.initialWorld.id)
    expect(JSON.stringify(assessment)).not.toContain(first.state.geography.frontier.regions[0]!.commitment.id)
    expect(first).toEqual(before)
  })

  it('requires the exact ledger anchor and leaves rejected submissions unchanged', () => {
    const source = selectedWorld('tavern-off-anchor')
    const moved = moveFoundationWorldCourier(source, 'north')
    if (moved.status !== 'moved') throw new Error('canonical tavern spawn must permit a north step')
    const before = structuredClone(moved.world)

    expect(assessTavernCourierSwitch(moved.world)).toMatchObject({ status: 'unavailable', reason: 'not-at-tavern-ledger' })
    expect(() => switchTavernCourier(moved.world, initialHouseholdActiveCrew(moved.world.crew)[1]!.id)).toThrow('not-at-tavern-ledger')
    expect(moved.world).toEqual(before)
  })

  it('switches exactly one active perspective at zero time and replays it canonically', () => {
    const source = selectedWorld('tavern-zero-time')
    const before = structuredClone(source)
    const target = initialHouseholdActiveCrew(source.crew)[1]!
    const switched = switchTavernCourier(source, target.id)

    expect(switched.state.courier).toEqual({ version: 2, initialCourierId: source.state.courier.initialCourierId, activeCourierId: target.id })
    expect(switched.state.navigation).toEqual({ version: 1, courierId: target.id, coordinate: { column: 4, row: 4 } })
    expect(switched.state.temporal).toEqual(source.state.temporal)
    expect(switched.manifest).toEqual(source.manifest)
    expect(switched.initialWorld).toEqual(source.initialWorld)
    expect(switched.crew).toEqual(source.crew)
    expect(switched.state.causalHistory.tail.at(-1)).toMatchObject({
      kind: 'tavern-courier-switched',
      payload: { fromCourierId: source.state.courier.activeCourierId, toCourierId: target.id, propId: 'prop:task-ledger', coordinate: { column: 4, row: 4 } }
    })
    expect(createFidelityPlanForVerifiedWorld(switched, target.id).activeCourier).toEqual({ personId: target.id, tier: 'loaded' })
    expect(() => createFidelityPlanForVerifiedWorld(switched, source.state.courier.activeCourierId!)).toThrow('fidelity.invalid-world')
    expect(replayFoundationWorldCausalHistory(switched)).toEqual(causalReplayProjectionForWorldState(switched.state))
    expect(validateFoundationWorld(switched)).toEqual([])
    expect(source).toEqual(before)
  })

  it('compacts typed zero-time switch evidence with exact causal accounting', () => {
    const world = selectedWorld('tavern-compaction')
    const context = { worldId: world.id, creationDigest: world.manifest.creation.digest }
    let history = world.state.causalHistory
    let projection = causalReplayProjectionForWorldState(world.state)
    for (let index = 0; index < 9; index++) {
      const fromCourierId = projection.courier.activeCourierId!
      const toCourierId = fromCourierId === 'crew:0' ? 'crew:1' : 'crew:0'
      projection = {
        ...projection,
        courier: { version: 2, initialCourierId: projection.courier.initialCourierId, activeCourierId: toCourierId },
        navigation: { version: 1, courierId: toCourierId, coordinate: { column: 4, row: 4 } }
      }
      const command = createCausalCommand(context, history, 'tavern-courier-switched', { fromCourierId, toCourierId, propId: 'prop:task-ledger', coordinate: { column: 4, row: 4 } })
      history = appendCausalCommand(context, history, command, projection)
    }

    expect(projection.temporal.worldTime).toBe(0)
    expect(projection.courier).toEqual({ version: 2, initialCourierId: 'crew:0', activeCourierId: 'crew:1' })
    expect(history.compactedSegments.at(-1)?.commandKinds).toMatchObject({ initialCourierSelected: 1, tavernCourierSwitched: 8 })
    expect(history.tail).toEqual([expect.objectContaining({ kind: 'tavern-courier-switched' })])
    expect(validateCausalHistoryState(context, history)).toEqual([])
  })

  it('fails closed for same, unknown, unavailable, and forged targets without mutating the submitted input', () => {
    const source = selectedWorld('tavern-rejection')
    const before = structuredClone(source)
    const active = source.state.courier.activeCourierId!

    expect(() => switchTavernCourier(source, active)).toThrow('validated ledger operation')
    expect(() => switchTavernCourier(source, 'crew:unknown')).toThrow('validated ledger operation')
    expect(source).toEqual(before)

    const forged = structuredClone(source)
    forged.state.people.records.find(person => person.id === initialHouseholdActiveCrew(source.crew)[1]!.id)!.work.availability = 'unavailable'
    const forgedBefore = structuredClone(forged)
    expect(() => switchTavernCourier(forged, initialHouseholdActiveCrew(source.crew)[1]!.id)).toThrow('complete medieval foundation contract')
    expect(forged).toEqual(forgedBefore)
  })

  it('upgrades only valid v14/v12 envelopes by retaining selected courier as the initial and active courier', () => {
    const source = selectedWorld('tavern-v14-upgrade')
    const legacy = v14Envelope(source)
    const before = structuredClone(legacy)
    const upgraded = upgradeFoundationWorldV14(legacy)

    expect(upgraded).toMatchObject({
      version: 14,
      state: {
        version: 13,
        courier: { version: 2, initialCourierId: source.state.courier.initialCourierId, activeCourierId: source.state.courier.initialCourierId },
        navigation: source.state.navigation
      }
    })
    expect(validateFoundationWorld(upgraded)).toEqual([])
    expect(legacy).toEqual(before)
    expect(() => upgradeFoundationWorldV14({ ...legacy, id: 'world:forged' })).toThrow('invalid')
  })
})
