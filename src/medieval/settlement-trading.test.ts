import { describe, expect, it } from 'vitest'
import { appendCausalCommand, createCausalCommand, type DeckMovedCommand, type InitialCourierSelectedCommand } from './causal-history'
import { assessJomonDeckStep, jomonDeckCoordinateId, type JomonDeckMovementDirection } from './jomon-navigation'
import { causalReplayProjectionForWorldState, createMedievalWorldState } from './world-state'
import { createJomonDeckContextualPrompt, validateTerminalPrompt } from './terminal-presentation'
import { acceptSettlementTradeContract as acceptState, deliverSettlementTradeContract as deliverState, initialSettlementTradingState, refuseSettlementTradeContract as refuseState, settlementTradeFeedback, validateSettlementTradingState } from './settlement-trading'
import { acceptSettlementTradeContract, chooseInitialCourier, createFoundationWorld, deliverSettlementTradeContract, moveFoundationWorldCourier, replayFoundationWorldCausalHistory, refuseSettlementTradeContract, validateFoundationWorld } from './world'

const selectedWorld = (seed: string) => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset: 'watershed' } }), 'crew:0')

/** Builds a retained-tail-valid physical tally fixture without profiling five public movement reducers here. */
const publicTallyWorld = (seed: string) => {
  const foundation = createFoundationWorld({ seed, configuration: { preset: 'watershed' } })
  const context = { worldId: foundation.id, creationDigest: foundation.manifest.creation.digest }
  let history = foundation.state.causalHistory
  let coordinate = { column: 4, row: 4 }
  const append = (
    kind: 'initial-courier-selected' | 'deck-moved',
    payload: InitialCourierSelectedCommand['payload'] | DeckMovedCommand['payload']
  ) => {
    const command = createCausalCommand(context, history, kind, payload)
    history = appendCausalCommand(context, history, command, causalReplayProjectionForWorldState(foundation.state))
  }
  append('initial-courier-selected', { courierId: 'crew:0' })
  for (const direction of ['south', 'west', 'west', 'west', 'north-west'] as const satisfies readonly JomonDeckMovementDirection[]) {
    const assessment = assessJomonDeckStep(foundation, coordinate, direction)
    if (assessment.status !== 'moved') throw new Error(`expected ${direction} deck step`)
    const sequence = history.checkpoint.sequence + history.tail.length + 1
    append('deck-moved', {
      actionId: `deck-move:${sequence}:crew:0:${jomonDeckCoordinateId(assessment.from)}:${jomonDeckCoordinateId(assessment.to)}`,
      courierId: 'crew:0', direction, from: assessment.from, to: assessment.to
    })
    coordinate = assessment.to
  }
  const staged = { ...foundation, state: { ...foundation.state, causalHistory: history } }
  const projection = replayFoundationWorldCausalHistory(staged)
  return {
    ...foundation,
    state: createMedievalWorldState({
      seed: foundation.manifest.creation.seed,
      configuration: foundation.manifest.creation.resolvedConfiguration,
      initialWorld: foundation.initialWorld,
      jomon: foundation.jomon,
      crew: foundation.crew,
      frontier: foundation.state.geography.frontier,
      temporal: projection.temporal,
      initialCourierId: projection.courier.initialCourierId!,
      activeCourierId: projection.courier.activeCourierId!,
      departedCourierIds: projection.courier.departedCourierIds ?? [],
      navigationState: projection.navigation,
      jomonState: foundation.state.jomon,
      peopleState: projection.people,
      simulationState: projection.simulation,
      eraState: projection.era,
      delegationState: projection.delegation,
      autonomyState: projection.autonomy,
      socialMemoryState: projection.socialMemory,
      settlementTradingState: projection.settlementTrading,
      causalHistoryState: history
    })
  }
}

/** Replays a compact retained movement tail before a public reducer owns its next transition. */
const worldFromReplayedHistory = (world: ReturnType<typeof publicTallyWorld>, history: ReturnType<typeof publicTallyWorld>['state']['causalHistory']) => {
  const staged = { ...world, state: { ...world.state, causalHistory: history } }
  const projection = replayFoundationWorldCausalHistory(staged)
  return {
    ...world,
    state: createMedievalWorldState({
      seed: world.manifest.creation.seed,
      configuration: world.manifest.creation.resolvedConfiguration,
      initialWorld: world.initialWorld,
      jomon: world.jomon,
      crew: world.crew,
      frontier: world.state.geography.frontier,
      temporal: projection.temporal,
      initialCourierId: projection.courier.initialCourierId!,
      activeCourierId: projection.courier.activeCourierId!,
      departedCourierIds: projection.courier.departedCourierIds ?? [],
      navigationState: projection.navigation,
      jomonState: projection.jomon,
      peopleState: projection.people,
      simulationState: projection.simulation,
      eraState: projection.era,
      delegationState: projection.delegation,
      autonomyState: projection.autonomy,
      socialMemoryState: projection.socialMemory,
      settlementTradingState: projection.settlementTrading,
      causalHistoryState: history
    })
  }
}

/**
 * Reaches the existing hold through replay-valid public deck evidence while
 * keeping the test focused on the single public delivery reducer.
 */
const acceptedWorldAtCargoHold = () => {
  const accepted = acceptSettlementTradeContract(publicTallyWorld('settlement-trade-delivery-public'))
  const first = moveFoundationWorldCourier(accepted, 'south-east')
  if (first.status !== 'moved') throw new Error('tally-to-gangplank step must be walkable')
  const second = moveFoundationWorldCourier(first.world, 'east')
  if (second.status !== 'moved') throw new Error('quay-to-gangplank step must compact normally')

  let history = second.world.state.causalHistory
  let coordinate = second.world.state.navigation.coordinate!
  for (let index = 0; index < 8; index++) {
    const sequence = history.checkpoint.sequence + history.tail.length + 1
    const to = { column: coordinate.column + 1, row: coordinate.row }
    const command = createCausalCommand(
      { worldId: second.world.id, creationDigest: second.world.manifest.creation.digest },
      history,
      'deck-moved',
      {
        actionId: `deck-move:${sequence}:crew:0:${jomonDeckCoordinateId(coordinate)}:${jomonDeckCoordinateId(to)}`,
        courierId: 'crew:0', direction: 'east', from: coordinate, to
      }
    )
    history = appendCausalCommand(
      { worldId: second.world.id, creationDigest: second.world.manifest.creation.digest },
      history,
      command,
      causalReplayProjectionForWorldState(second.world.state)
    )
    coordinate = to
  }
  const beforeAnchor = worldFromReplayedHistory(second.world, history)
  const north = moveFoundationWorldCourier(beforeAnchor, 'north')
  if (north.status !== 'moved') throw new Error('cargo-hold anchor step must be walkable')
  return north.world
}

const acceptedCargoHoldWorld = acceptedWorldAtCargoHold()

describe('local settlement freight handoff', () => {
  it('keeps its initial local source state exact and rejects hidden or malformed additions', () => {
    const state = initialSettlementTradingState()
    const before = structuredClone(state)

    expect(validateSettlementTradingState(state)).toEqual([])
    expect(state).toEqual({
      version: 1,
      locations: [{ id: 'settlement-location:hearthford-mill-quay', profileId: 'settlement-profile:hearthford-mill-quay', sourceAreaId: 'quay-approach', serviceId: 'public-tally-table' }],
      contracts: [{ id: 'settlement-contract:hearthford-mill-ironwork', locationId: 'settlement-location:hearthford-mill-quay', status: 'offered' }]
    })
    const forged = { ...structuredClone(state), worldTime: 0 }
    expect(validateSettlementTradingState(forged)).toContain('settlement-trading.malformed-state')
    expect(state).toEqual(before)
  })

  it('routes only exact public-tally occupancy to the bounded offer without exposing hidden state', () => {
    const source = selectedWorld('settlement-trade-delivery')
    const sourceBefore = structuredClone(source)
    expect(() => acceptSettlementTradeContract(source)).toThrow(/settlement trade/i)
    expect(source).toEqual(sourceBefore)

    const tally = publicTallyWorld('settlement-trade-prompt')
    const prompt = createJomonDeckContextualPrompt(tally)
    if (prompt.kind !== 'settlement-trade') throw new Error('expected the physical public tally prompt')
    expect(validateTerminalPrompt(prompt)).toEqual([])
    expect(prompt).toMatchObject({
      surface: 'public-tally',
      source: { locationId: 'settlement-location:hearthford-mill-quay', sourceAreaId: 'quay-approach', serviceId: 'public-tally-table' },
      contract: { status: 'offered', commodityId: 'commodity:ironwork', quantity: 1 },
      choices: [{ id: 'accept' }, { id: 'refuse' }]
    })
    expect(JSON.stringify(prompt)).not.toMatch(/"(?:column|row|frontier|initialWorld|manifest|people|cargoId)"\s*:/i)
    const withCoordinate = { ...structuredClone(prompt), coordinate: { column: 0, row: 4 } }
    const reorderedChoices = structuredClone(prompt)
    if (reorderedChoices.kind === 'settlement-trade' && reorderedChoices.choices) reorderedChoices.choices = [...reorderedChoices.choices].reverse()
    const forgedSource = structuredClone(prompt)
    if (forgedSource.kind === 'settlement-trade' && forgedSource.surface === 'public-tally') (forgedSource.source as { serviceId: string }).serviceId = 'witness-ledger'
    for (const forged of [withCoordinate, reorderedChoices, forgedSource]) {
      expect(validateTerminalPrompt(forged).map(item => item.code)).toContain('terminal-presentation.invalid-prompt')
    }

  })

  it('accepts the physical tally burden at zero world time and blocks a second decision', () => {
    const tally = publicTallyWorld('settlement-trade-acceptance')
    const accepted = acceptSettlementTradeContract(tally)
    expect(accepted.state.temporal.worldTime).toBe(tally.state.temporal.worldTime)
    expect(accepted.state.settlementTrading.contracts[0]).toMatchObject({ status: 'accepted', burden: { commodityId: 'commodity:ironwork', quantity: 1, deliveryPropId: 'prop:cargo-hold-rack' } })
    expect(accepted.state.jomon.cargo.lots).toEqual([])
    expect(accepted.state.markets.markets.find(market => market.id === 'market:settlement-location:hearthford-mill-quay')?.commodityStates[0]).toEqual({ commodityId: 'commodity:ironwork', stock: 'limited', demand: 'high', price: 'high' })
    expect(() => refuseSettlementTradeContract(accepted)).toThrow()
  })

  it('delivers the accepted material burden at the cargo hold through one replayed zero-time receipt', () => {
    const source = structuredClone(acceptedCargoHoldWorld)
    const before = structuredClone(source)
    const delivered = deliverSettlementTradeContract(source)

    expect(source).toEqual(before)
    expect(delivered.state.temporal).toEqual(source.state.temporal)
    expect(delivered.state.settlementTrading.contracts[0]).toMatchObject({
      status: 'delivered',
      cargoId: 'cargo:19:commodity:ironwork',
      recordedAtWorldTime: source.state.temporal.worldTime,
      causalSequence: 19
    })
    expect(delivered.state.jomon.cargo.lots).toEqual([
      { id: 'cargo:19:commodity:ironwork', commodityId: 'commodity:ironwork', quantity: 1, condition: 'sound', status: 'in-hold' }
    ])
    expect(delivered.state.markets.markets.find(market => market.id === 'market:settlement-location:hearthford-mill-quay')?.commodityStates[0]).toEqual({ commodityId: 'commodity:ironwork', stock: 'none', demand: 'steady', price: 'fair' })
    expect(delivered.state.causalHistory.tail.at(-1)).toMatchObject({ kind: 'settlement-trade-delivered' })
    expect(replayFoundationWorldCausalHistory(delivered)).toEqual(causalReplayProjectionForWorldState(delivered.state))
    expect(validateFoundationWorld(delivered)).toEqual([])
    expect(settlementTradeFeedback(delivered.state.settlementTrading)?.text).toMatch(/Jomon received the ironwork case/i)
    expect(() => deliverSettlementTradeContract(delivered)).toThrow()
  })

  it('keeps acceptance, refusal, and delivery material outcomes canonical, bounded, and source-safe', () => {
    const accepted = acceptState(initialSettlementTradingState(), 7, 3)
    const refused = refuseState(initialSettlementTradingState(), 7, 3)
    const delivered = deliverState(accepted, 9, 4)
    expect(accepted.contracts[0]).toMatchObject({ status: 'accepted', burden: { commodityId: 'commodity:ironwork', quantity: 1, deliveryPropId: 'prop:cargo-hold-rack' }, recordedAtWorldTime: 7, causalSequence: 3 })
    expect(refused.contracts[0]).toMatchObject({ status: 'refused', recordedAtWorldTime: 7, causalSequence: 3 })
    expect(delivered.contracts[0]).toMatchObject({ status: 'delivered', cargoId: 'cargo:4:commodity:ironwork', recordedAtWorldTime: 9, causalSequence: 4 })
    expect(settlementTradeFeedback(delivered)?.text).toMatch(/mill-race work can proceed/i)
    expect(() => deliverState(refused, 8, 4)).toThrow()
  })
})
