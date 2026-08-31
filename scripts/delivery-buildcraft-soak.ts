import assert from 'node:assert/strict'
import { createHash } from 'node:crypto'
import { announceDeliveryHazard, applyDeliveryPersistentInjury, beginDeliveryExpedition, createGalaxy, deliveryModifiers, installCourierModification, loseGalaxyCourier, loseSealedPackagesForCourier, materializeNeridaIntakeExpedition, migrateGalaxy, newHero, newRun, recordGalaxyNeridaTacticalConsequence, resolveDeliveryHazard, resolveRouteBoardTransit, resolveTelegraphs, selectDeliveryOffer, selectRouteBoardConnection, commitRouteBoardTransit, stabilizeNeridaIntake, synchronizeDeliveryExpedition, synchronizeDeliveryRunEquipment, useDeliveryActiveEquipment, acceptSealedPackageContract, advanceGalaxyRouteReckoning } from '../src/engine'
import { migrateRunRecord } from '../src/storage'
import type { GalaxyState, RunState } from '../src/types'

const SEED_COUNT = 24
const marksForSeed = (seed: number): number => [0, 120, 720, 1_200][seed % 4]!
const safeNeighbors = (state: RunState) => [{ x: state.hero.x, y: state.hero.y - 1 }, { x: state.hero.x + 1, y: state.hero.y }, { x: state.hero.x, y: state.hero.y + 1 }, { x: state.hero.x - 1, y: state.hero.y }]
const advance = (source: GalaxyState, marks: number, chunked: boolean): GalaxyState => chunked
  ? Array.from({ length: Math.ceil(marks / 60) }, (_, index) => Math.min(60, marks - index * 60)).reduce((galaxy, step) => advanceGalaxyRouteReckoning(galaxy, step), source)
  : advanceGalaxyRouteReckoning(source, marks)
const travel = (source: GalaxyState, connectionId: string): GalaxyState => resolveRouteBoardTransit(commitRouteBoardTransit(selectRouteBoardConnection(source, connectionId).galaxy).galaxy).galaxy
const activeCourier = (galaxy: GalaxyState) => galaxy.couriers.find(courier => courier.id === galaxy.activeCourierId)!

interface ScenarioMetrics {
  encounter: boolean
  elite: boolean
  tier: string
  survived: boolean
  died: boolean
  maxManifest: number
  maxIntents: number
  maxRewards: number
  maxInjuries: number
}

const fingerprint = (galaxy: GalaxyState): string => JSON.stringify({
  routeReckoning: galaxy.routeReckoning,
  routeBoard: galaxy.routeBoard,
  destinationWorld: galaxy.destinationWorld,
  institutionWorld: galaxy.institutionWorld,
  deliveryRun: galaxy.deliveryRun ? {
    ...galaxy.deliveryRun,
    equipment: [...galaxy.deliveryRun.equipment].sort((left, right) => left.itemId.localeCompare(right.itemId)),
    offers: [...galaxy.deliveryRun.offers].sort((left, right) => left.id.localeCompare(right.id)),
    eliteHistory: [...galaxy.deliveryRun.eliteHistory].sort((left, right) => left.id.localeCompare(right.id))
  } : undefined,
  generalManifest: galaxy.generalManifest,
  sealedPackageContracts: galaxy.sealedPackageContracts,
  sealedPackages: galaxy.sealedPackages,
  routeCaches: galaxy.routeCaches,
  couriers: galaxy.couriers.map(courier => ({ id: courier.id, status: courier.status, inventory: [...courier.hero.inventory].sort(), modifications: courier.hero.deliveryModifications, injuries: courier.hero.deliveryInjuries, techniques: courier.hero.learnedDeliveryTechniques }))
})

const assertNoRunEquipmentDuplication = (galaxy: GalaxyState): void => {
  const run = galaxy.deliveryRun
  if (!run) return
  const assigned = galaxy.couriers.find(courier => courier.id === run.courierId)
  for (const stack of run.equipment) {
    const held = assigned?.hero.inventory.filter(item => item === stack.itemId).length ?? 0
    if (run.resolution) assert.equal(held, 0, `closed ${run.id} retained active ${stack.itemId}`)
    else assert.equal(held, stack.count, `active ${run.id} duplicated or lost ${stack.itemId}`)
  }
}

const scenario = (seed: number, chunked: boolean, reverseOrder: boolean, deathPath: boolean): { galaxy: GalaxyState; metrics: ScenarioMetrics } => {
  let galaxy = createGalaxy(seed, newHero({ name: `M6 Soak ${seed}` }), 0)
  const contract = galaxy.sealedPackageContracts[0]!
  galaxy = acceptSealedPackageContract(galaxy, contract.id, galaxy.activeCourierId).galaxy
  galaxy = advance(galaxy, marksForSeed(seed), chunked)
  const requisition = galaxy.deliveryRun!.offers.find(offer => offer.state === 'pending')!
  assert.equal(selectDeliveryOffer(galaxy, requisition.id, requisition.choices[seed % requisition.choices.length]).changed, true)
  galaxy = travel(galaxy, 'route:kestrel-orison')
  const firstTransit = galaxy.deliveryRun!.offers.find(offer => offer.state === 'pending')!
  const firstChoice = deathPath ? 'routeCurrentCapacitor' : 'groundingSpindle'
  assert.ok(firstTransit.choices.includes(firstChoice), `missing deterministic transit choice ${firstChoice}`)
  assert.equal(selectDeliveryOffer(galaxy, firstTransit.id, firstChoice).changed, true)
  galaxy = travel(galaxy, 'route:orison-nerida')
  const secondTransit = galaxy.deliveryRun!.offers.find(offer => offer.state === 'pending')!
  const secondChoice = deathPath ? 'routeCurrentCapacitor' : 'routeCurrentCapacitor'
  assert.ok(secondTransit.choices.includes(secondChoice), `missing repeatable transit choice ${secondChoice}`)
  assert.equal(selectDeliveryOffer(galaxy, secondTransit.id, secondChoice).changed, true)
  if (reverseOrder) galaxy.deliveryRun!.equipment.reverse()
  galaxy = migrateGalaxy(JSON.parse(JSON.stringify(galaxy)))!

  const run = beginDeliveryExpedition(galaxy, 'destination:nerida')!
  const courier = activeCourier(galaxy)
  courier.hero.inventory.push('tonic')
  assert.equal(installCourierModification(galaxy, seed % 2 ? 'pressure-baffles' : 'relay-marrow-conduit').changed, true)
  let state = newRun(seed, galaxy.sites[galaxy.activeSiteId]!.biome, 0, structuredClone(courier.hero))
  galaxy.destinationWorld.partitions['destination:nerida']!.condition = 'cavitation-restriction'
  assert.equal(materializeNeridaIntakeExpedition(state, galaxy, run), true)
  assert.ok(state.floor.actors.some(actor => actor.deliveryElite), 'expedition did not create an elite')

  state.turn = 2
  let intent = announceDeliveryHazard(state)
  assert.ok(intent, 'first delivery hazard was not declared')
  let safe = safeNeighbors(state).find(point => state.floor.tiles[point.y * state.floor.width + point.x]?.kind === 'floor' && !intent!.cells.some(cell => cell.x === point.x && cell.y === point.y))
  assert.ok(safe, 'first delivery hazard had no safe response')
  state = migrateRunRecord(JSON.parse(JSON.stringify(state)))!
  state.hero.x = safe!.x
  state.hero.y = safe!.y
  assert.equal(resolveDeliveryHazard(state, intent!).damage, 0)
  state.turn = intent!.resolveTurn
  assert.equal(resolveTelegraphs(state).length, 1)

  state.turn = 4
  intent = announceDeliveryHazard(state)
  assert.ok(intent, 'second delivery hazard was not declared')
  if (deathPath) {
    safe = safeNeighbors(state).find(point => state.floor.tiles[point.y * state.floor.width + point.x]?.kind === 'floor' && !intent!.cells.some(cell => cell.x === point.x && cell.y === point.y))
    assert.ok(safe, 'death-path hazard had no safe response')
    state.hero.x = safe!.x
    state.hero.y = safe!.y
    assert.equal(resolveDeliveryHazard(state, intent!).damage, 0)
  } else {
    assert.equal(intent!.category, 'relayDischarge')
    assert.equal(useDeliveryActiveEquipment(state), true)
  }
  state.turn = intent!.resolveTurn
  resolveTelegraphs(state)
  applyDeliveryPersistentInjury(state)
  courier.hero = structuredClone(state.hero)
  synchronizeDeliveryRunEquipment(galaxy)

  if (deathPath) {
    galaxy = loseSealedPackagesForCourier(galaxy, galaxy.activeCourierId, `m6-soak:${seed}`, 0)
    galaxy = loseGalaxyCourier(galaxy, galaxy.activeCourierId, 'M6 soak courier-loss branch')
  } else {
    const elite = state.floor.actors.find(actor => actor.deliveryElite)!
    elite.health = 0
    state.hero.x = state.floor.deliveryExpedition!.console.x
    state.hero.y = state.floor.deliveryExpedition!.console.y
    assert.equal(stabilizeNeridaIntake(state), true)
    courier.hero = structuredClone(state.hero)
    synchronizeDeliveryRunEquipment(galaxy)
    assert.equal(synchronizeDeliveryExpedition(galaxy, state), true)
    assert.equal(recordGalaxyNeridaTacticalConsequence(galaxy, run.id).changed, true)
  }
  galaxy = migrateGalaxy(JSON.parse(JSON.stringify(galaxy)))!
  assertNoRunEquipmentDuplication(galaxy)
  const modifiers = deliveryModifiers(activeCourier(galaxy).hero, galaxy.deliveryRun)
  assert.ok(modifiers.applied.length > 0, 'delivery modifier pipeline lost all provenance')
  const metrics = {
    encounter: true,
    elite: true,
    tier: galaxy.deliveryRun!.pressureTier,
    survived: !deathPath,
    died: deathPath,
    maxManifest: galaxy.generalManifest.entries.length,
    maxIntents: state.floor.tacticalIntentHistory?.length ?? 0,
    maxRewards: galaxy.deliveryRun!.offers.length,
    maxInjuries: activeCourier(galaxy).hero.deliveryInjuries?.length ?? 0
  }
  return { galaxy, metrics }
}

const metrics: ScenarioMetrics[] = []
const fingerprints: string[] = []
for (let index = 0; index < SEED_COUNT; index++) {
  const seed = 81_000 + index
  const deathPath = index % 3 === 0
  const whole = scenario(seed, false, false, deathPath)
  const chunked = scenario(seed, true, true, deathPath)
  assert.equal(fingerprint(chunked.galaxy), fingerprint(whole.galaxy), `chunked/order variant diverged for seed ${seed}`)
  metrics.push(whole.metrics)
  fingerprints.push(fingerprint(whole.galaxy))
}

const distribution = Object.fromEntries(['working-load', 'compression', 'cavitation', 'cascade'].map(tier => [tier, metrics.filter(metric => metric.tier === tier).length]))
const result = {
  seedCount: SEED_COUNT,
  encounterCount: metrics.filter(metric => metric.encounter).length,
  eliteCount: metrics.filter(metric => metric.elite).length,
  pressureTierDistribution: distribution,
  survivalCount: metrics.filter(metric => metric.survived).length,
  deathCount: metrics.filter(metric => metric.died).length,
  fingerprintSha256: createHash('sha256').update(fingerprints.join('\n')).digest('hex'),
  maximumRetainedState: {
    manifestEntries: Math.max(...metrics.map(metric => metric.maxManifest)),
    intentHistory: Math.max(...metrics.map(metric => metric.maxIntents)),
    offers: Math.max(...metrics.map(metric => metric.maxRewards)),
    injuries: Math.max(...metrics.map(metric => metric.maxInjuries))
  }
}
process.stdout.write(`${JSON.stringify(result, null, 2)}\n`)
