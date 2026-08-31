import { KESTREL_CALIBRATION_CASE, sealedPackageDefinition } from '../package-content'
import type { GalaxyState, Hero, PackageCustodyState, SealedPackage, SealedPackageContract } from '../types'
import { appendGeneralManifest, type ManifestReferences } from './manifest'
import { ROUTE_RECKONING_NEAR_EXPIRY_UNITS, ROUTE_RECKONING_UNITS_PER_CYCLE, sectorDayFromRouteReckoning } from './route-reckoning'

const PACKAGE_HOLD_CAPACITY = 12

type PackageMutation = { galaxy: GalaxyState; changed: boolean; message: string }
const copyGalaxy = (source: GalaxyState): GalaxyState => structuredClone(source)
const packageForContract = (galaxy: GalaxyState, contract: SealedPackageContract): SealedPackage | undefined => contract.packageId ? galaxy.sealedPackages.find(candidate => candidate.id === contract.packageId) : undefined
const contractForPackage = (galaxy: GalaxyState, packageId: string): SealedPackageContract | undefined => galaxy.sealedPackageContracts.find(contract => contract.packageId === packageId)
const packageCapacityUsed = (galaxy: GalaxyState): number => galaxy.sealedPackages.reduce((total, packageRecord) => {
  if (packageRecord.custody === 'routeCache' || packageRecord.custody === 'recipient' || packageRecord.custody === 'abandoned') return total
  return total + (contractForPackage(galaxy, packageRecord.id)?.terms.holdUnits ?? 0)
}, 0)

const manifestEntry = (galaxy: GalaxyState, kind: Parameters<typeof appendGeneralManifest>[1]['kind'], detail: string, options: ManifestReferences = {}): number => appendGeneralManifest(galaxy, { ...options, kind, detail, source: kind === 'contractExpired' || kind === 'contractNearingExpiry' ? 'contract' : 'custody' })

const custodyLabel = (custody: PackageCustodyState): string => custody === 'atJomon' ? 'Jomon custody' : custody === 'assignedToCourier' ? 'assigned courier custody' : custody === 'routeCache' ? 'route-cache custody' : custody === 'recipient' ? 'recipient custody' : 'abandoned custody'
const packageTerminal = (status: SealedPackageContract['status']): boolean => status === 'declined' || status === 'completed' || status === 'failed' || status === 'expired'
const routeCacheIdFor = (galaxy: GalaxyState, linkId: string, chunk: number): string => `sealed-cache:${galaxy.seed}:${linkId}:${chunk}:${galaxy.generalManifest.nextSequence}`

/** Generates the one authored M1 offer without changing legacy cargo contracts. */
export const addKestrelSealedPackageOffer = (source: GalaxyState): GalaxyState => {
  const galaxy = copyGalaxy(source)
  const destination = galaxy.sites[galaxy.activeSiteId]
  if (!destination) return galaxy
  const definition = KESTREL_CALIBRATION_CASE
  const id = `sealed-contract:${galaxy.seed}:${definition.id}:${destination.id}`
  if (galaxy.sealedPackageContracts.some(contract => contract.id === id)) return galaxy
  const contract: SealedPackageContract = {
    version: 2,
    id,
    definitionId: definition.id,
    status: 'offered',
    offeredAtSectorDay: galaxy.sectorDay,
    offeredAtRouteReckoning: galaxy.routeReckoning,
    terms: {
      ...structuredClone(definition.terms),
      destinationSiteId: destination.id,
      destinationLabel: destination.name,
      deadlineDay: galaxy.sectorDay + 4,
      deadlineReckoning: galaxy.routeReckoning + 4 * ROUTE_RECKONING_UNITS_PER_CYCLE
    }
  }
  galaxy.sealedPackageContracts.push(contract)
  manifestEntry(galaxy, 'contractOffered', `${definition.title} offered for ${destination.name}.`, { contractId: contract.id })
  return galaxy
}

export const offeredSealedPackageContract = (galaxy: GalaxyState): SealedPackageContract | undefined => galaxy.sealedPackageContracts.find(contract => contract.status === 'offered')
export const sealedPackageForContract = (galaxy: GalaxyState, contractId: string): SealedPackage | undefined => {
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  return contract ? packageForContract(galaxy, contract) : undefined
}
export const sealedPackageExteriorForContract = (galaxy: GalaxyState, contractId: string) => {
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined
  return packageRecord?.exterior ?? (contract ? sealedPackageDefinition(contract.definitionId)?.exterior : undefined)
}
export const sealedPackageHoldUsed = (galaxy: GalaxyState): number => galaxy.cargo.reduce((total, cargo) => total + cargo.units, 0) + packageCapacityUsed(galaxy)

export const acceptSealedPackageContract = (source: GalaxyState, contractId: string, courierId: string): PackageMutation => {
  const galaxy = copyGalaxy(source)
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  const courier = galaxy.couriers.find(candidate => candidate.id === courierId)
  if (!contract || contract.status !== 'offered') return { galaxy, changed: false, message: 'That sealed-package offer is no longer available.' }
  if (!courier || courier.status !== 'available') return { galaxy, changed: false, message: 'Assign an available courier before accepting this package.' }
  if (sealedPackageHoldUsed(galaxy) + contract.terms.holdUnits > PACKAGE_HOLD_CAPACITY) return { galaxy, changed: false, message: 'Jomon custody hold lacks capacity for this package.' }
  const definition = sealedPackageDefinition(contract.definitionId)
  if (!definition) return { galaxy, changed: false, message: 'The package definition cannot be read.' }
  const packageId = `sealed-package:${galaxy.seed}:${contract.id}`
  const packageRecord: SealedPackage = {
    version: 2,
    id: packageId,
    contractId: contract.id,
    definitionId: definition.id,
    hiddenContentsId: definition.hiddenContentsId,
    exterior: structuredClone(definition.exterior),
    sealState: 'intact',
    custody: 'assignedToCourier',
    assignedCourierId: courier.id
  }
  contract.status = 'accepted'
  contract.packageId = packageId
  contract.assignedCourierId = courier.id
  contract.acceptedAtSectorDay = galaxy.sectorDay
  contract.acceptedAtRouteReckoning = galaxy.routeReckoning
  galaxy.sealedPackages.push(packageRecord)
  manifestEntry(galaxy, 'contractAccepted', `${definition.title} accepted under seal.`, { contractId: contract.id, packageId, courierId: courier.id })
  manifestEntry(galaxy, 'custodyTransferred', `${definition.title} transferred from Jomon custody to ${courier.name}.`, { contractId: contract.id, packageId, courierId: courier.id })
  return { galaxy, changed: true, message: `${definition.title} assigned to ${courier.name}.` }
}

export const declineSealedPackageContract = (source: GalaxyState, contractId: string): PackageMutation => {
  const galaxy = copyGalaxy(source)
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  if (!contract || contract.status !== 'offered') return { galaxy, changed: false, message: 'That sealed-package offer cannot be declined now.' }
  contract.status = 'declined'
  contract.resolvedAtSectorDay = galaxy.sectorDay
  contract.resolvedAtRouteReckoning = galaxy.routeReckoning
  manifestEntry(galaxy, 'contractDeclined', `Declined ${sealedPackageDefinition(contract.definitionId)?.title ?? contract.id}.`, { contractId: contract.id })
  return { galaxy, changed: true, message: 'Offer declined. The decision is recorded in the General Manifest.' }
}

export const inspectSealedPackage = (source: GalaxyState, contractId: string): PackageMutation => {
  const galaxy = copyGalaxy(source)
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined
  if (!contract || packageTerminal(contract.status)) return { galaxy, changed: false, message: 'No inspectable sealed package is in custody.' }
  if (!packageRecord && contract.status === 'offered') {
    manifestEntry(galaxy, 'packageInspected', `Exterior inspection recorded for offered ${sealedPackageDefinition(contract.definitionId)?.title ?? contract.id}; contents remain sealed.`, { contractId: contract.id })
    return { galaxy, changed: true, message: 'Exterior inspection recorded. Acceptance has not transferred custody.' }
  }
  if (!packageRecord) return { galaxy, changed: false, message: 'No inspectable sealed package is in custody.' }
  const sequence = manifestEntry(galaxy, 'packageInspected', `Exterior inspection recorded for ${sealedPackageDefinition(contract.definitionId)?.title ?? packageRecord.id}; contents remain sealed.`, { contractId: contract.id, packageId: packageRecord.id, courierId: packageRecord.assignedCourierId })
  packageRecord.inspectedAtSequence = sequence
  return { galaxy, changed: true, message: 'Exterior inspection recorded. Contents remain unknown.' }
}

export const violateSealedPackageSeal = (source: GalaxyState, contractId: string): PackageMutation => {
  const galaxy = copyGalaxy(source)
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined
  const definition = contract ? sealedPackageDefinition(contract.definitionId) : undefined
  if (!contract || !packageRecord || !definition || contract.status !== 'accepted') return { galaxy, changed: false, message: 'This contract cannot be opened.' }
  if (packageRecord.sealState !== 'intact') return { galaxy, changed: false, message: 'The seal has already been violated.' }
  if (packageRecord.custody === 'routeCache' || packageRecord.custody === 'recipient' || packageRecord.custody === 'abandoned') return { galaxy, changed: false, message: 'The package is not available to open.' }
  packageRecord.sealState = 'opened'
  packageRecord.revealedContents = structuredClone(definition.contents)
  const sequence = manifestEntry(galaxy, 'sealViolated', `${definition.title} opened: ${definition.contents.knowledge}`, { contractId: contract.id, packageId: packageRecord.id, courierId: packageRecord.assignedCourierId })
  packageRecord.openedAtSequence = sequence
  return { galaxy, changed: true, message: `Seal violated. ${definition.contents.danger}` }
}

/** Marks a physical visit; it intentionally performs no delivery. */
export const markSealedPackageDestinationReached = (source: GalaxyState, siteId: string): GalaxyState => {
  const galaxy = copyGalaxy(source)
  for (const contract of galaxy.sealedPackageContracts) {
    if (contract.status === 'accepted' && contract.terms.destinationSiteId === siteId) {
      contract.destinationReachedAtSectorDay ??= galaxy.sectorDay
      contract.destinationReachedAtRouteReckoning ??= galaxy.routeReckoning
    }
  }
  return galaxy
}

export const deliverSealedPackage = (source: GalaxyState, contractId: string, hero: Hero): PackageMutation => {
  const galaxy = copyGalaxy(source)
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined
  if (!contract || !packageRecord || contract.status !== 'accepted') return { galaxy, changed: false, message: 'No active sealed package can be delivered.' }
  if (contract.terms.destinationSiteId !== galaxy.activeSiteId || contract.destinationReachedAtSectorDay === undefined) return { galaxy, changed: false, message: 'The recipient will only accept this package after a physical Kestrel landing.' }
  if (contract.terms.deadlineReckoning < galaxy.routeReckoning) return expireSealedPackageContracts(galaxy, galaxy.routeReckoning)
  const intact = packageRecord.sealState === 'intact'
  const payment = intact ? contract.terms.payment : Math.max(0, contract.terms.payment - contract.terms.collateral)
  packageRecord.custody = 'recipient'
  packageRecord.routeCacheId = undefined
  contract.status = 'completed'
  contract.resolvedAtSectorDay = galaxy.sectorDay
  contract.resolvedAtRouteReckoning = galaxy.routeReckoning
  hero.gold += payment
  manifestEntry(galaxy, 'custodyTransferred', `${sealedPackageDefinition(contract.definitionId)?.title ?? packageRecord.id} transferred to ${contract.terms.recipient}.`, { contractId: contract.id, packageId: packageRecord.id, courierId: packageRecord.assignedCourierId })
  manifestEntry(galaxy, 'deliveryCompleted', `${intact ? 'Intact' : 'Tampered'} delivery settled for ${payment} credits (${intact ? contract.terms.intactSettlement : contract.terms.tamperedSettlement}).`, { contractId: contract.id, packageId: packageRecord.id, courierId: packageRecord.assignedCourierId })
  return { galaxy, changed: true, message: `Delivery complete: ${payment} credits (${intact ? 'intact' : 'tampered'} settlement).` }
}

export const refuseSealedPackage = (source: GalaxyState, contractId: string): PackageMutation => {
  const galaxy = copyGalaxy(source)
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined
  if (!contract || !packageRecord || contract.status !== 'accepted') return { galaxy, changed: false, message: 'No active sealed package can be refused.' }
  packageRecord.custody = 'abandoned'
  packageRecord.routeCacheId = undefined
  contract.status = 'failed'
  contract.resolvedAtSectorDay = galaxy.sectorDay
  contract.resolvedAtRouteReckoning = galaxy.routeReckoning
  manifestEntry(galaxy, 'deliveryFailed', `Delivery refused for ${sealedPackageDefinition(contract.definitionId)?.title ?? packageRecord.id}.`, { contractId: contract.id, packageId: packageRecord.id, courierId: packageRecord.assignedCourierId })
  return { galaxy, changed: true, message: 'Package refused. The contract is closed and the custody failure is recorded.' }
}

export const abandonSealedPackage = (source: GalaxyState, contractId: string): PackageMutation => {
  const galaxy = copyGalaxy(source)
  const contract = galaxy.sealedPackageContracts.find(candidate => candidate.id === contractId)
  const packageRecord = contract ? packageForContract(galaxy, contract) : undefined
  if (!contract || !packageRecord || contract.status !== 'accepted') return { galaxy, changed: false, message: 'No active sealed package can be abandoned.' }
  packageRecord.custody = 'abandoned'
  packageRecord.routeCacheId = undefined
  contract.status = 'failed'
  contract.resolvedAtSectorDay = galaxy.sectorDay
  contract.resolvedAtRouteReckoning = galaxy.routeReckoning
  manifestEntry(galaxy, 'custodyTransferred', `${sealedPackageDefinition(contract.definitionId)?.title ?? packageRecord.id} abandoned from ${custodyLabel('assignedToCourier')}.`, { contractId: contract.id, packageId: packageRecord.id, courierId: packageRecord.assignedCourierId })
  manifestEntry(galaxy, 'deliveryFailed', `Abandonment closed ${sealedPackageDefinition(contract.definitionId)?.title ?? packageRecord.id}.`, { contractId: contract.id, packageId: packageRecord.id, courierId: packageRecord.assignedCourierId })
  return { galaxy, changed: true, message: 'Package abandoned. This is a recorded custody failure, not a delivery.' }
}

export const applySealedPackageDeadlineTransitions = (galaxy: GalaxyState): number => {
  let changed = 0
  for (const contract of galaxy.sealedPackageContracts) {
    if (contract.status !== 'offered' && contract.status !== 'accepted') continue
    const packageRecord = packageForContract(galaxy, contract)
    if (galaxy.routeReckoning === contract.terms.deadlineReckoning - ROUTE_RECKONING_NEAR_EXPIRY_UNITS && contract.nearingExpiryNotifiedAtRouteReckoning === undefined) {
      contract.nearingExpiryNotifiedAtRouteReckoning = galaxy.routeReckoning
      manifestEntry(galaxy, 'contractNearingExpiry', `${sealedPackageDefinition(contract.definitionId)?.title ?? contract.id} has 240 Route Reckoning marks remaining.`, { contractId: contract.id, packageId: packageRecord?.id, courierId: packageRecord?.assignedCourierId })
      changed++
    }
    if (contract.terms.deadlineReckoning >= galaxy.routeReckoning) continue
    contract.status = 'expired'
    contract.resolvedAtRouteReckoning = galaxy.routeReckoning
    contract.resolvedAtSectorDay = sectorDayFromRouteReckoning(galaxy.routeReckoning)
    manifestEntry(galaxy, 'contractExpired', `${sealedPackageDefinition(contract.definitionId)?.title ?? contract.id} expired at Route Reckoning ${galaxy.routeReckoning}.`, { contractId: contract.id, packageId: packageRecord?.id, courierId: packageRecord?.assignedCourierId })
    manifestEntry(galaxy, 'deliveryFailed', `Deadline failure recorded for ${sealedPackageDefinition(contract.definitionId)?.title ?? contract.id}.`, { contractId: contract.id, packageId: packageRecord?.id, courierId: packageRecord?.assignedCourierId })
    changed++
  }
  return changed
}

export const expireSealedPackageContracts = (source: GalaxyState, routeReckoning = source.routeReckoning): PackageMutation => {
  const galaxy = copyGalaxy(source)
  galaxy.routeReckoning = Math.max(galaxy.routeReckoning, Math.floor(routeReckoning))
  galaxy.sectorDay = sectorDayFromRouteReckoning(galaxy.routeReckoning)
  const changed = applySealedPackageDeadlineTransitions(galaxy)
  const expired = galaxy.sealedPackageContracts.filter(contract => contract.status === 'expired').length - source.sealedPackageContracts.filter(contract => contract.status === 'expired').length
  return { galaxy, changed: changed > 0, message: expired ? `${expired} sealed-package contract${expired === 1 ? '' : 's'} expired.` : 'No sealed-package deadline has expired.' }
}

export const loseSealedPackagesForCourier = (source: GalaxyState, courierId: string, linkId: string, chunk: number): GalaxyState => {
  const galaxy = copyGalaxy(source)
  const packages = galaxy.sealedPackages.filter(packageRecord => packageRecord.custody === 'assignedToCourier' && packageRecord.assignedCourierId === courierId)
  if (!packages.length) return galaxy
  let cache = galaxy.routeCaches.find(candidate => candidate.linkId === linkId && candidate.chunk === chunk && !candidate.recovered)
  if (!cache) {
    cache = { id: routeCacheIdFor(galaxy, linkId, chunk), linkId, chunk, cargo: [], packages: [], recovered: false }
    galaxy.routeCaches.push(cache)
  }
  for (const packageRecord of packages) {
    const contract = contractForPackage(galaxy, packageRecord.id)
    packageRecord.custody = 'routeCache'
    packageRecord.routeCacheId = cache.id
    if (!cache.packages.includes(packageRecord.id)) cache.packages.push(packageRecord.id)
    if (contract?.status === 'accepted') {
      contract.status = 'failed'
      contract.resolvedAtSectorDay = galaxy.sectorDay
      contract.resolvedAtRouteReckoning = galaxy.routeReckoning
    }
    manifestEntry(galaxy, 'packageLost', `${sealedPackageDefinition(packageRecord.definitionId)?.title ?? packageRecord.id} lost with ${courierId}; cache ${cache.id} marks the last custody record.`, { contractId: contract?.id, packageId: packageRecord.id, courierId, routeCacheId: cache.id })
    manifestEntry(galaxy, 'deliveryFailed', `Courier loss closed ${sealedPackageDefinition(packageRecord.definitionId)?.title ?? packageRecord.id}.`, { contractId: contract?.id, packageId: packageRecord.id, courierId, routeCacheId: cache.id })
  }
  return galaxy
}

export const recoverSealedPackageRouteCaches = (source: GalaxyState, linkId: string): PackageMutation => {
  const galaxy = copyGalaxy(source)
  const caches = galaxy.routeCaches.filter(cache => cache.linkId === linkId && !cache.recovered && cache.packages.length)
  if (!caches.length) return { galaxy, changed: false, message: 'No sealed-package cache is recorded on this route.' }
  let recovered = 0
  for (const cache of caches) {
    for (const packageId of cache.packages) {
      const packageRecord = galaxy.sealedPackages.find(candidate => candidate.id === packageId)
      if (!packageRecord || packageRecord.custody !== 'routeCache') continue
      const contract = contractForPackage(galaxy, packageRecord.id)
      packageRecord.custody = 'atJomon'
      packageRecord.routeCacheId = undefined
      manifestEntry(galaxy, 'custodyTransferred', `${sealedPackageDefinition(packageRecord.definitionId)?.title ?? packageRecord.id} recovered into Jomon custody.`, { contractId: contract?.id, packageId: packageRecord.id, routeCacheId: cache.id })
      manifestEntry(galaxy, 'packageRecovered', `${sealedPackageDefinition(packageRecord.definitionId)?.title ?? packageRecord.id} recovered from ${cache.id}; the failed contract remains closed.`, { contractId: contract?.id, packageId: packageRecord.id, routeCacheId: cache.id })
      recovered++
    }
    if (!cache.cargo.length) cache.recovered = true
  }
  return { galaxy, changed: recovered > 0, message: recovered ? `Recovered ${recovered} sealed package${recovered === 1 ? '' : 's'} to Jomon custody. Failed contracts remain closed.` : 'No sealed package could be recovered from this cache.' }
}

export const sealedPackageCustodyLabel = custodyLabel
