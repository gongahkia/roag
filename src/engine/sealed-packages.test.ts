import { describe, expect, it } from 'vitest'
import { advanceGalaxyRouteReckoning, createGalaxy, loseGalaxyCourier, migrateGalaxy } from './galaxy'
import { ROUTE_RECKONING_NEAR_EXPIRY_UNITS } from './route-reckoning'
import { newHero } from './run'
import { acceptSealedPackageContract, declineSealedPackageContract, deliverSealedPackage, expireSealedPackageContracts, inspectSealedPackage, loseSealedPackagesForCourier, markSealedPackageDestinationReached, recoverSealedPackageRouteCaches, sealedPackageExteriorForContract, sealedPackageForContract, violateSealedPackageSeal } from './sealed-packages'

const offeredGalaxy = (seed = 241) => createGalaxy(seed, newHero({ name: 'Ari' }), 0)
const offer = (galaxy = offeredGalaxy()) => galaxy.sealedPackageContracts[0]!
const acceptedGalaxy = () => {
  const galaxy = offeredGalaxy()
  return acceptSealedPackageContract(galaxy, offer(galaxy).id, galaxy.activeCourierId).galaxy
}

describe('sealed package custody', () => {
  it('creates one deterministic Kestrel offer with inspectable terms and no package instance before acceptance', () => {
    const first = offeredGalaxy(402)
    const second = offeredGalaxy(402)
    const contract = offer(first)

    expect(first.sealedPackageContracts).toEqual(second.sealedPackageContracts)
    expect(first.sealedPackages).toEqual([])
    expect(contract).toMatchObject({ status: 'offered', terms: { sender: 'Kestrel Survey Exchange', recipient: 'Kestrel landing instrumentation clerk', declaredMassKg: 18, handlingClass: 'shielded calibration material' } })
    expect(sealedPackageExteriorForContract(first, contract.id)).toMatchObject({ sealMark: expect.stringContaining('unbroken') })
    expect(first.generalManifest.entries).toMatchObject([{ kind: 'contractOffered', contractId: contract.id }])
  })

  it('records inspection, acceptance, assigned courier custody, and a confirmed seal violation without automatic delivery', () => {
    const initial = offeredGalaxy()
    const contract = offer(initial)
    const inspectedOffer = inspectSealedPackage(initial, contract.id)
    const accepted = acceptSealedPackageContract(inspectedOffer.galaxy, contract.id, initial.activeCourierId)
    const packageRecord = sealedPackageForContract(accepted.galaxy, contract.id)!
    const inspectedPackage = inspectSealedPackage(accepted.galaxy, contract.id)
    const opened = violateSealedPackageSeal(inspectedPackage.galaxy, contract.id)

    expect(inspectedOffer.changed).toBe(true)
    expect(accepted.changed).toBe(true)
    expect(packageRecord).toMatchObject({ sealState: 'intact', custody: 'assignedToCourier', assignedCourierId: initial.activeCourierId })
    expect(opened.galaxy.sealedPackageContracts[0]!.status).toBe('accepted')
    expect(sealedPackageForContract(opened.galaxy, contract.id)).toMatchObject({ sealState: 'opened', revealedContents: { id: 'contents.kestrel-calibration-cores' } })
    expect(opened.galaxy.generalManifest.entries.map(entry => entry.kind)).toEqual(expect.arrayContaining(['contractOffered', 'packageInspected', 'contractAccepted', 'custodyTransferred', 'sealViolated']))
  })

  it('requires a physical destination visit and an explicit settlement, with different intact and tampered outcomes', () => {
    const intact = acceptedGalaxy()
    const contract = offer(intact)
    const hero = newHero({ name: 'Ari' })
    const before = hero.gold
    const blocked = deliverSealedPackage(intact, contract.id, hero)
    const reached = markSealedPackageDestinationReached(intact, contract.terms.destinationSiteId)
    const delivered = deliverSealedPackage(reached, contract.id, hero)

    expect(blocked.changed).toBe(false)
    expect(delivered.changed).toBe(true)
    expect(hero.gold - before).toBe(contract.terms.payment)
    expect(delivered.galaxy.sealedPackageContracts[0]!.status).toBe('completed')
    expect(sealedPackageForContract(delivered.galaxy, contract.id)).toMatchObject({ sealState: 'intact', custody: 'recipient' })

    const tamperedBase = acceptedGalaxy()
    const tamperedContract = offer(tamperedBase)
    const opened = violateSealedPackageSeal(tamperedBase, tamperedContract.id)
    const tamperedHero = newHero({ name: 'Ari' })
    const tampered = deliverSealedPackage(markSealedPackageDestinationReached(opened.galaxy, tamperedContract.terms.destinationSiteId), tamperedContract.id, tamperedHero)
    expect(tamperedHero.gold).toBe(tamperedContract.terms.payment - tamperedContract.terms.collateral)
    expect(tampered.galaxy.generalManifest.entries.at(-1)).toMatchObject({ kind: 'deliveryCompleted', detail: expect.stringContaining('Tampered') })
  })

  it('keeps decline and explicit deadline expiry distinct terminal outcomes', () => {
    const declinedBase = offeredGalaxy()
    const declined = declineSealedPackageContract(declinedBase, offer(declinedBase).id)
    expect(declined.galaxy.sealedPackageContracts[0]!.status).toBe('declined')
    expect(declined.galaxy.sealedPackages).toEqual([])
    expect(declined.galaxy.generalManifest.entries.at(-1)).toMatchObject({ kind: 'contractDeclined' })

    const expiring = acceptedGalaxy()
    const expired = expireSealedPackageContracts(expiring, offer(expiring).terms.deadlineReckoning + 1)
    expect(expired.galaxy.sealedPackageContracts[0]!.status).toBe('expired')
    expect(expired.galaxy.generalManifest.entries.slice(-2).map(entry => entry.kind)).toEqual(['contractExpired', 'deliveryFailed'])
  })

  it('records one Route Reckoning warning and one expiry while preserving intact or route-cache package state', () => {
    const accepted = acceptedGalaxy()
    const contract = offer(accepted)
    const warningMarks = contract.terms.deadlineReckoning - ROUTE_RECKONING_NEAR_EXPIRY_UNITS
    const warned = advanceGalaxyRouteReckoning(accepted, warningMarks)
    const warnedAgain = advanceGalaxyRouteReckoning(warned, 1)
    expect(warned.generalManifest.entries.filter(entry => entry.kind === 'contractNearingExpiry')).toHaveLength(1)
    expect(warnedAgain.generalManifest.entries.filter(entry => entry.kind === 'contractNearingExpiry')).toHaveLength(1)

    const expired = advanceGalaxyRouteReckoning(warnedAgain, contract.terms.deadlineReckoning - warnedAgain.routeReckoning + 1)
    const expiredTwice = advanceGalaxyRouteReckoning(expired, 20)
    expect(expired.sealedPackageContracts[0]!.status).toBe('expired')
    expect(sealedPackageForContract(expired, contract.id)).toMatchObject({ sealState: 'intact', custody: 'assignedToCourier' })
    expect(expiredTwice.generalManifest.entries.filter(entry => entry.kind === 'contractExpired')).toHaveLength(1)
    expect(expiredTwice.generalManifest.entries.filter(entry => entry.kind === 'deliveryFailed')).toHaveLength(1)

    const cached = loseSealedPackagesForCourier(warnedAgain, warnedAgain.activeCourierId, 'kestrel-connector', 1)
    const cachedAfterDeadline = advanceGalaxyRouteReckoning(cached, contract.terms.deadlineReckoning - cached.routeReckoning + 1)
    expect(cachedAfterDeadline.sealedPackageContracts[0]!.status).toBe('failed')
    expect(sealedPackageForContract(cachedAfterDeadline, contract.id)).toMatchObject({ sealState: 'intact', custody: 'routeCache' })
    expect(cachedAfterDeadline.generalManifest.entries.filter(entry => entry.kind === 'contractExpired')).toHaveLength(0)
  })

  it('expires offered and tampered packages through the same single Route Reckoning lifecycle', () => {
    const offered = offeredGalaxy(617)
    const offeredContract = offer(offered)
    const expiredOffer = advanceGalaxyRouteReckoning(offered, offeredContract.terms.deadlineReckoning + 1)
    expect(expiredOffer.sealedPackageContracts[0]).toMatchObject({ status: 'expired' })
    expect(expiredOffer.sealedPackageContracts[0]!.packageId).toBeUndefined()

    const tampered = acceptedGalaxy()
    const tamperedContract = offer(tampered)
    const opened = violateSealedPackageSeal(tampered, tamperedContract.id)
    const expiredTampered = advanceGalaxyRouteReckoning(opened.galaxy, tamperedContract.terms.deadlineReckoning + 1)
    expect(sealedPackageForContract(expiredTampered, tamperedContract.id)).toMatchObject({ sealState: 'opened', custody: 'assignedToCourier' })
    expect(expiredTampered.generalManifest.entries.filter(entry => entry.kind === 'contractExpired')).toHaveLength(1)

    const reachedLate = markSealedPackageDestinationReached(expiredTampered, tamperedContract.terms.destinationSiteId)
    const hero = newHero({ name: 'Ari' })
    expect(deliverSealedPackage(reachedLate, tamperedContract.id, hero).changed).toBe(false)
  })

  it('resolves the same scheduled expiry after a save/load boundary', () => {
    const accepted = acceptedGalaxy()
    const contract = offer(accepted)
    const justBeforeExpiry = advanceGalaxyRouteReckoning(accepted, contract.terms.deadlineReckoning)
    const reloaded = migrateGalaxy(JSON.parse(JSON.stringify(justBeforeExpiry)))!
    const uninterrupted = advanceGalaxyRouteReckoning(justBeforeExpiry, 1)
    const resumed = advanceGalaxyRouteReckoning(reloaded, 1)

    expect(resumed.sealedPackageContracts[0]!.status).toBe('expired')
    expect(resumed.generalManifest.entries).toEqual(uninterrupted.generalManifest.entries)
  })

  it('caches an assigned package on courier loss and lets a later courier recover its intact state', () => {
    const accepted = acceptedGalaxy()
    const contract = offer(accepted)
    const lost = loseSealedPackagesForCourier(accepted, accepted.activeCourierId, 'kestrel-connector', 1)
    const cache = lost.routeCaches.find(candidate => candidate.linkId === 'kestrel-connector')!
    const successor = lost.couriers.find(candidate => candidate.id !== lost.activeCourierId)!
    const worldAfterDeath = loseGalaxyCourier(lost, accepted.activeCourierId, 'test courier loss', 0)
    const recovered = recoverSealedPackageRouteCaches(worldAfterDeath, cache.linkId)
    const packageRecord = sealedPackageForContract(recovered.galaxy, contract.id)!

    expect(lost.sealedPackageContracts[0]!.status).toBe('failed')
    expect(cache.packages).toEqual([packageRecord.id])
    expect(worldAfterDeath.couriers.find(candidate => candidate.id === accepted.activeCourierId)?.status).toBe('dead')
    expect(successor.status).toBe('available')
    expect(packageRecord).toMatchObject({ sealState: 'intact', custody: 'atJomon' })
    expect(recovered.galaxy.generalManifest.entries.map(entry => entry.kind)).toEqual(expect.arrayContaining(['packageLost', 'deliveryFailed', 'custodyTransferred', 'packageRecovered']))
  })

  it('migrates representative legacy galaxy data additively without reinterpreting generic cargo', () => {
    const current = offeredGalaxy()
    const legacy = structuredClone(current) as unknown as Record<string, unknown>
    legacy.version = 1
    delete legacy.routeReckoning
    delete legacy.lastWorldTick
    delete legacy.sealedPackageContracts
    delete legacy.sealedPackages
    delete legacy.generalManifest
    for (const cache of legacy.routeCaches as Array<Record<string, unknown>>) delete cache.packages
    const migrated = migrateGalaxy(legacy)

    expect(migrated).toBeDefined()
    expect(migrated!.cargo).toEqual(current.cargo)
    expect(migrated!.contracts).toEqual(current.contracts)
    expect(migrated!.sealedPackageContracts).toEqual([])
    expect(migrated!.sealedPackages).toEqual([])
    expect(migrated!.version).toBe(2)
    expect(migrated!.routeReckoning).toBe(0)
    expect(migrated!.generalManifest).toEqual({ version: 2, nextSequence: 0, entries: [] })
    expect(migrated!.routeCaches.every(cache => Array.isArray(cache.packages))).toBe(true)
  })
})
