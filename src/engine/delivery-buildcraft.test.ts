import { describe, expect, it } from 'vitest'
import { ITEM } from '../content'
import { advanceGalaxyRouteReckoning, createGalaxy, recordGalaxyNeridaTacticalConsequence } from './galaxy'
import { archiveDeliveryRun, beginDeliveryExpedition, courierModificationChoices, createDeliveryOffer, deliveryItemStackCount, deliveryModifiers, deliveryNextThreshold, deliveryRunContext, installCourierModification, selectDeliveryOffer, synchronizeDeliveryExpedition } from './delivery-buildcraft'
import { deliveryEliteTraitsCompatible, deliveryEliteTraitsFor, materializeNeridaIntakeExpedition } from './delivery-tactics'
import { migrateGalaxy } from './galaxy'
import { newHero, newRun } from './run'
import { acceptSealedPackageContract, deliverSealedPackage, loseSealedPackagesForCourier, markSealedPackageDestinationReached } from './sealed-packages'

const acceptedDelivery = (seed = 61_001) => {
  const initial = createGalaxy(seed, newHero({ name: 'M6 Courier' }), 0)
  const contract = initial.sealedPackageContracts[0]!
  return acceptSealedPackageContract(initial, contract.id, initial.activeCourierId).galaxy
}

const courier = (galaxy: ReturnType<typeof acceptedDelivery>) => galaxy.couriers.find(candidate => candidate.id === galaxy.activeCourierId)!

const neridaState = (seed = 61_101) => {
  const galaxy = acceptedDelivery(seed)
  const run = beginDeliveryExpedition(galaxy, 'destination:nerida')!
  const state = newRun(seed, galaxy.sites[galaxy.activeSiteId]!.biome, 0, structuredClone(courier(galaxy).hero))
  state.deliveryContext = deliveryRunContext(run)
  expect(materializeNeridaIntakeExpedition(state, galaxy, run)).toBe(true)
  return { galaxy, run, state }
}

describe('M6 delivery buildcraft', () => {
  it('derives pressure only from Route Reckoning, including standing-still canonical marks and explicit transit-sized batches', () => {
    const accepted = acceptedDelivery()
    const noAdvance = advanceGalaxyRouteReckoning(accepted, 0)
    const standing = advanceGalaxyRouteReckoning(accepted, 360)
    const travel = advanceGalaxyRouteReckoning(accepted, 780)
    expect(noAdvance.deliveryRun).toMatchObject({ elapsedMarks: 0, pressureTier: 'working-load' })
    expect(standing.deliveryRun).toMatchObject({ elapsedMarks: 360, pressureTier: 'compression' })
    expect(travel.deliveryRun).toMatchObject({ elapsedMarks: 780, pressureTier: 'cavitation' })
  })

  it('is chunking invariant, bounded, and records each escalation exactly once', () => {
    const initial = acceptedDelivery(61_002)
    const chunked = [360, 360, 480, 1_920].reduce((galaxy, marks) => advanceGalaxyRouteReckoning(galaxy, marks), initial)
    const batched = advanceGalaxyRouteReckoning(initial, 3_120)
    const repeated = advanceGalaxyRouteReckoning(batched, 120)
    expect(chunked.deliveryRun).toEqual(batched.deliveryRun)
    expect(batched.deliveryRun).toMatchObject({ elapsedMarks: 1920, pressureTier: 'cascade' })
    expect(batched.deliveryRun!.crossedThresholdIds).toEqual(['pressure:compression', 'pressure:cavitation', 'pressure:cascade'])
    expect(repeated.generalManifest.entries.filter(entry => entry.kind === 'deliveryPressureEscalated')).toHaveLength(3)
    expect(deliveryNextThreshold(batched.deliveryRun)).toBeUndefined()
  })

  it('keeps closed sessions inert and stops pressure at contract resolution', () => {
    const accepted = acceptedDelivery(61_003)
    const serialized = JSON.parse(JSON.stringify(accepted))
    const reloaded = migrateGalaxy(serialized)!
    const closed = archiveDeliveryRun(reloaded, 'abandoned')
    const later = advanceGalaxyRouteReckoning(reloaded, 1_200)
    expect(closed).toBe(true)
    expect(reloaded.deliveryRun!.elapsedMarks).toBe(0)
    expect(later.deliveryRun!.elapsedMarks).toBe(0)
  })

  it('persists deterministic three-choice offers before selection and does not reroll after reload', () => {
    const first = acceptedDelivery(61_004)
    const reloaded = migrateGalaxy(JSON.parse(JSON.stringify(first)))!
    const offer = first.deliveryRun!.offers[0]!
    expect(offer.choices).toHaveLength(3)
    expect(reloaded.deliveryRun!.offers[0]).toEqual(offer)
    const selected = selectDeliveryOffer(reloaded, offer.id, offer.choices[0])
    const selectedAgain = selectDeliveryOffer(reloaded, offer.id, offer.choices[1])
    expect(selected.changed).toBe(true)
    expect(selectedAgain.changed).toBe(false)
    expect(courier(reloaded).hero.inventory.filter(item => item === offer.choices[0])).toHaveLength(1)
    expect(reloaded.deliveryRun!.offers[0]).toMatchObject({ state: 'selected', selectedItemId: offer.choices[0] })
  })

  it('makes bounded stacks reachable through successive resolved transit offers', () => {
    const galaxy = acceptedDelivery(61_041)
    const requisition = galaxy.deliveryRun!.offers[0]!
    expect(selectDeliveryOffer(galaxy, requisition.id, undefined).changed).toBe(true)
    const first = createDeliveryOffer(galaxy, 'transit-salvage')!
    expect(first.choices).toContain('routeCurrentCapacitor')
    expect(selectDeliveryOffer(galaxy, first.id, 'routeCurrentCapacitor').changed).toBe(true)
    const second = createDeliveryOffer(galaxy, 'transit-salvage')!
    expect(second.choices).toContain('routeCurrentCapacitor')
    expect(selectDeliveryOffer(galaxy, second.id, 'routeCurrentCapacitor').changed).toBe(true)
    expect(deliveryItemStackCount(galaxy.deliveryRun, 'routeCurrentCapacitor')).toBe(2)
    expect(deliveryModifiers(courier(galaxy).hero, galaxy.deliveryRun).values.hazardChargeCap).toBe(2)
  })

  it('enforces stack limits, insertion-order-independent modifiers, and authored charge/custody synergies', () => {
    const galaxy = acceptedDelivery(61_005)
    const run = galaxy.deliveryRun!
    run.equipment = [{ itemId: 'routeCurrentCapacitor', count: 2 }, { itemId: 'pressureWeaveLiner', count: 3 }, { itemId: 'custodySealMesh', count: 1 }]
    const hero = courier(galaxy).hero
    hero.inventory.push('routeCurrentCapacitor', 'routeCurrentCapacitor', 'pressureWeaveLiner', 'pressureWeaveLiner', 'pressureWeaveLiner', 'custodySealMesh')
    const first = deliveryModifiers(hero, run)
    const second = deliveryModifiers(hero, { ...run, equipment: [...run.equipment].reverse() })
    expect(deliveryItemStackCount(run, 'pressureWeaveLiner')).toBe(3)
    expect(first).toEqual(second)
    expect(first.values).toMatchObject({ hazardReduction: 4, hazardChargeCap: 2, custodyBuffer: 1 })
    expect(first.applied).toEqual([...first.applied].sort())
  })

  it('archives only run-bound equipment for success, loss, and terminal recovery paths', () => {
    const galaxy = acceptedDelivery(61_006)
    const run = galaxy.deliveryRun!
    const offer = run.offers[0]!
    selectDeliveryOffer(galaxy, offer.id, offer.choices[0])
    const selectedId = offer.choices[0]!
    const hero = courier(galaxy).hero
    hero.inventory.push('tonic')
    const reached = markSealedPackageDestinationReached(galaxy, galaxy.sealedPackageContracts[0]!.terms.destinationSiteId)
    const settled = deliverSealedPackage(reached, reached.sealedPackageContracts[0]!.id, hero).galaxy
    expect(settled.deliveryRun).toMatchObject({ resolution: 'completed' })
    expect(courier(settled).hero.inventory).not.toContain(selectedId)
    expect(hero.inventory).not.toContain(selectedId)
    expect(courier(settled).hero.inventory).toContain('tonic')

    const lost = acceptedDelivery(61_007)
    const lossOffer = lost.deliveryRun!.offers[0]!
    selectDeliveryOffer(lost, lossOffer.id, lossOffer.choices[0])
    const cached = loseSealedPackagesForCourier(lost, lost.activeCourierId, 'm6-test-link', 0)
    expect(cached.deliveryRun).toMatchObject({ resolution: 'courier-loss' })
    expect(courier(cached).hero.inventory).not.toContain(lossOffer.choices[0])
  })

  it('requires one real treatment and one explicit confirmation-ready modification choice, then keeps it courier-bound', () => {
    const { galaxy, run } = neridaState(61_008)
    const hero = courier(galaxy).hero
    const treatments = hero.inventory.filter(item => item === 'tonic').length
    hero.inventory.push('tonic')
    expect(courierModificationChoices(hero)).toEqual(['pressure-baffles', 'relay-marrow-conduit'])
    const installed = installCourierModification(galaxy, 'pressure-baffles')
    const duplicate = installCourierModification(galaxy, 'pressure-baffles')
    expect(installed.changed).toBe(true)
    expect(duplicate.changed).toBe(false)
    expect(hero.inventory.filter(item => item === 'tonic')).toHaveLength(treatments)
    expect(hero.deliveryModifications).toMatchObject([{ id: 'pressure-baffles', runId: run.id }])
    expect(deliveryModifiers(hero, run).values).toMatchObject({ hazardReduction: 1, healingPenalty: 1 })
  })

  it('materializes a stable elite composition, reward identity, and M4/M5 outcome once', () => {
    const { galaxy, run, state } = neridaState(61_009)
    const elite = state.floor.actors.find(actor => actor.deliveryElite)!
    const again = deliveryEliteTraitsFor(run, galaxy)
    expect(elite.deliveryElite!.traitIds).toEqual(again)
    expect(deliveryEliteTraitsCompatible(elite.deliveryElite!.traitIds)).toBe(true)
    elite.health = 0
    state.floor.deliveryExpedition!.status = 'completed'
    state.floor.deliveryExpedition!.techniqueGranted = true
    state.hero.learnedDeliveryTechniques = ['intake-routing']
    expect(synchronizeDeliveryExpedition(galaxy, state)).toBe(true)
    const outcome = recordGalaxyNeridaTacticalConsequence(galaxy, run.id)
    const repeated = recordGalaxyNeridaTacticalConsequence(outcome.galaxy, run.id)
    expect(outcome.changed).toBe(true)
    expect(repeated.changed).toBe(false)
    expect(outcome.galaxy.institutionWorld.actors.find(actor => actor.id === 'actor:iren-vos')?.memories.at(-1)).toMatchObject({ kind: 'tactical-intervention' })
    expect(outcome.galaxy.generalManifest.entries.some(entry => entry.kind === 'institutionalTacticalConsequence')).toBe(true)
  })

  it('keeps all authored M6 player-facing item content free of retired carrier and settlement aliases', () => {
    const labels = Object.values(ITEM).filter(item => item.delivery).flatMap(item => [item.name, item.delivery!.description, ...item.delivery!.tags]).join('\n')
    expect(labels).not.toMatch(/Jomon Voyager|New Edo/i)
  })
})
