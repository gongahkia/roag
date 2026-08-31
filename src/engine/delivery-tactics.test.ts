import { describe, expect, it } from 'vitest'
import { createGalaxy, advanceGalaxyRouteReckoning } from './galaxy'
import { acceptSealedPackageContract } from './sealed-packages'
import { beginDeliveryExpedition, deliveryRunContext } from './delivery-buildcraft'
import { announceDeliveryHazard, applyDeliveryPersistentInjury, deliveryEliteDamageCap, materializeNeridaIntakeExpedition, resolveDeliveryHazard, useDeliveryActiveEquipment } from './delivery-tactics'
import { announceTelegraph, cancelTelegraphs, resolveTelegraphs } from './telegraphs'
import { newHero, newRun } from './run'
import { migrateRunRecord } from '../storage'
import { presentTelegraph } from '../telegraph-language'

const fixture = (seed = 62_001, marks = 0) => {
  const initial = createGalaxy(seed, newHero({ name: 'Hazard Courier' }), 0)
  const accepted = acceptSealedPackageContract(initial, initial.sealedPackageContracts[0]!.id, initial.activeCourierId).galaxy
  const galaxy = advanceGalaxyRouteReckoning(accepted, marks)
  const run = beginDeliveryExpedition(galaxy, 'destination:nerida')!
  const hero = structuredClone(galaxy.couriers.find(candidate => candidate.id === galaxy.activeCourierId)!.hero)
  const state = newRun(seed, galaxy.sites[galaxy.activeSiteId]!.biome, 0, hero)
  state.deliveryContext = deliveryRunContext(run)
  galaxy.destinationWorld.partitions['destination:nerida'].condition = 'cavitation-restriction'
  expect(materializeNeridaIntakeExpedition(state, galaxy, run)).toBe(true)
  return { galaxy, run, state }
}

describe('M6 tactical intent and hazards', () => {
  it('declares deterministic, renderer-independent hazards with a safe positional response', () => {
    const first = fixture(62_002)
    const second = fixture(62_002)
    first.state.turn = second.state.turn = 2
    const announced = announceDeliveryHazard(first.state)!
    const repeated = announceDeliveryHazard(second.state)!
    const safe = [{ x: first.state.hero.x, y: first.state.hero.y - 1 }, { x: first.state.hero.x + 1, y: first.state.hero.y }, { x: first.state.hero.x, y: first.state.hero.y + 1 }, { x: first.state.hero.x - 1, y: first.state.hero.y }]
      .some(point => first.state.floor.tiles[point.y * first.state.floor.width + point.x]?.kind === 'floor' && !announced.cells.some(cell => cell.x === point.x && cell.y === point.y))
    expect(announced).toMatchObject({ sourceKind: 'hazard', category: 'pressureVent', state: 'pending', declaredTurn: 2, resolveTurn: 3, responses: ['move', 'block'] })
    expect(repeated).toEqual(announced)
    expect(safe).toBe(true)
  })

  it('resolves and cancels intents exactly once, and save/reload retains pending intent without damage', () => {
    const { state } = fixture(62_003)
    state.turn = 2
    const intent = announceDeliveryHazard(state)!
    const reloaded = migrateRunRecord(JSON.parse(JSON.stringify(state)))!
    expect(reloaded.floor.telegraphs).toEqual([intent])
    const cancelled = cancelTelegraphs(reloaded, candidate => candidate.id === intent.id, 'test shutdown')
    expect(cancelled).toHaveLength(1)
    expect(cancelTelegraphs(reloaded, candidate => candidate.id === intent.id)).toHaveLength(0)
    expect(reloaded.floor.tacticalIntentHistory).toContainEqual(expect.objectContaining({ id: intent.id, state: 'cancelled' }))
    const resolvedState = fixture(62_004).state
    resolvedState.turn = 2
    announceDeliveryHazard(resolvedState)
    resolvedState.turn = 3
    expect(resolveTelegraphs(resolvedState)).toHaveLength(1)
    expect(resolveTelegraphs(resolvedState)).toHaveLength(0)
  })

  it('offers distinct counters for movement, pulse interruption, and grounding', () => {
    const { state } = fixture(62_005)
    const point = { x: state.hero.x, y: state.hero.y }
    const movement = announceTelegraph(state, { id: 'm6:move', sourceId: 'm6:hazard', actionId: 'pressure-vent-sweep', cells: [point], danger: 'major', windup: 1, sourceKind: 'hazard', category: 'pressureVent', responses: ['move', 'block'] })
    state.hero.x += 1
    expect(resolveDeliveryHazard(state, movement)).toMatchObject({ damage: 0 })

    const pulse = announceTelegraph(state, { id: 'm6:pulse', sourceId: 'm6:hazard', actionId: 'intake-shear', cells: [{ x: state.hero.x, y: state.hero.y }], danger: 'major', windup: 1, sourceKind: 'hazard', category: 'intakeShear', responses: ['move', 'interrupt'] })
    state.hero.inventory.push('pulseReverser')
    expect(useDeliveryActiveEquipment(state)).toBe(true)
    expect(state.floor.telegraphs?.some(intent => intent.id === pulse.id)).toBe(false)

    const relay = announceTelegraph(state, { id: 'm6:ground', sourceId: 'm6:hazard', actionId: 'relay-discharge', cells: [{ x: state.hero.x, y: state.hero.y }], danger: 'major', windup: 1, sourceKind: 'hazard', category: 'relayDischarge', responses: ['move', 'ground', 'interrupt'] })
    state.hero.inventory = state.hero.inventory.filter(item => item !== 'pulseReverser')
    state.hero.inventory.push('groundingSpindle', 'routeCurrentCapacitor')
    expect(useDeliveryActiveEquipment(state)).toBe(true)
    expect(state.floor.telegraphs?.some(intent => intent.id === relay.id)).toBe(false)
    expect(state.hero.cooldowns?.['delivery:relay-charge']).toBeGreaterThan(0)
  })

  it('causes one deterministic persistent pressure injury unless a patch consumes itself', () => {
    const first = fixture(62_006)
    first.state.hero.deliveryInjuries = []
    expect(applyDeliveryPersistentInjury(first.state)).toBe('applied')
    expect(applyDeliveryPersistentInjury(first.state)).toBe('existing')
    expect(first.state.hero.deliveryInjuries).toMatchObject([{ id: 'pressure-scarring', runId: first.run.id }])

    const protectedState = fixture(62_007).state
    protectedState.hero.inventory.push('tissueStitchPatch')
    expect(applyDeliveryPersistentInjury(protectedState)).toBe('prevented')
    expect(protectedState.hero.deliveryInjuries ?? []).toEqual([])
  })

  it('uses pressure and destination state for an elite with a relay counter and deterministic reward', () => {
    const low = fixture(62_008, 0)
    const high = fixture(62_008, 1_200)
    const lowElite = low.state.floor.actors.find(actor => actor.deliveryElite)!
    const highElite = high.state.floor.actors.find(actor => actor.deliveryElite)!
    expect(highElite.deliveryElite!.traitIds.length).toBeGreaterThanOrEqual(lowElite.deliveryElite!.traitIds.length)
    expect(highElite.deliveryElite!.rewardItemId).toBe('orphanPhaseSample')
    announceTelegraph(high.state, { id: 'm6:relay-shield', sourceId: 'm6:hazard', actionId: 'relay-discharge', cells: [{ x: high.state.hero.x, y: high.state.hero.y }], danger: 'major', windup: 1, sourceKind: 'hazard', category: 'relayDischarge', responses: ['ground'] })
    const relayBound = high.state.floor.actors.find(actor => actor.deliveryElite?.traitIds.includes('relay-bound'))
    if (relayBound) expect(deliveryEliteDamageCap(high.state, relayBound)).toBe(1)
    expect(high.state.floor.deliveryExpedition!.relayActivated).toBe(true)
  })

  it('presents identical textual warning semantics for ASCII and detailed renderers', () => {
    const { state } = fixture(62_009)
    const intent = announceTelegraph(state, { id: 'm6:present', sourceId: 'm6:hazard', actionId: 'relay-discharge', cells: [{ x: state.hero.x, y: state.hero.y }], danger: 'major', windup: 1, sourceKind: 'hazard', category: 'relayDischarge', responses: ['move', 'ground', 'interrupt'] })
    const presentation = presentTelegraph(intent, state.turn, 'intake relay')
    expect(presentation).toMatchObject({ glyph: '!', color: '#ee6f78' })
    expect(presentation.label).toContain('RELAY DISCHARGE [MOVE/GROUND/INTERRUPT]')
  })
})
