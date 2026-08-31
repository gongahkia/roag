import { createHash } from 'node:crypto'
import { performance } from 'node:perf_hooks'
import { acceptSealedPackageContract, advanceGalaxyRouteReckoning, commitRouteBoardTransit, createGalaxy, newHero, resolveRouteBoardTransit, selectRouteBoardConnection } from '../src/engine'
import type { GalaxyState } from '../src/types'

const seed = Number(process.env.ROUTE_RECKONING_SOAK_SEED ?? 71)
const marks = Number(process.env.ROUTE_RECKONING_SOAK_MARKS ?? 100_000)
if (!Number.isInteger(seed) || !Number.isInteger(marks) || marks < 1) throw new Error('ROUTE_RECKONING_SOAK_SEED and ROUTE_RECKONING_SOAK_MARKS must be positive integers.')

const fingerprint = (galaxy: GalaxyState): string => createHash('sha256').update(JSON.stringify({
  routeReckoning: galaxy.routeReckoning,
  lastWorldTick: galaxy.lastWorldTick,
  sites: Object.values(galaxy.sites).sort((left, right) => left.id.localeCompare(right.id)).map(site => [site.id, site.control, site.integrity, site.ecology, site.construction, site.supplies, site.salvage, site.market]),
  contracts: galaxy.contracts.map(contract => [contract.id, contract.status, contract.deadlineReckoning]),
  packages: galaxy.sealedPackages.map(packageRecord => [packageRecord.id, packageRecord.sealState, packageRecord.custody]),
  sealedContracts: galaxy.sealedPackageContracts.map(contract => [contract.id, contract.status, contract.terms.deadlineReckoning, contract.nearingExpiryNotifiedAtRouteReckoning]),
  events: galaxy.events.map(event => [event.id, event.routeReckoning, event.kind]),
  manifest: galaxy.generalManifest.entries.map(entry => [entry.id, entry.routeReckoning, entry.kind, entry.affectedEntityIds, entry.payload]),
  routeBoard: [galaxy.routeBoard.networkId, galaxy.routeBoard.currentDestinationId, galaxy.routeBoard.nextTransitSequence, galaxy.routeBoard.history]
})).digest('hex')

const assertInvariants = (galaxy: GalaxyState): void => {
  if (!Number.isInteger(galaxy.routeReckoning) || galaxy.routeReckoning !== marks) throw new Error(`invalid Route Reckoning ${galaxy.routeReckoning}`)
  for (const site of Object.values(galaxy.sites)) {
    for (const value of [site.integrity, site.ecology, site.construction, site.supplies, site.salvage]) if (!Number.isInteger(value) || value < 0 || value > 100) throw new Error(`invalid site value at ${site.id}: ${value}`)
  }
  const ids = [...galaxy.events.map(event => event.id), ...galaxy.generalManifest.entries.map(entry => entry.id)]
  if (new Set(ids).size !== ids.length) throw new Error('duplicate chronicle or Manifest ID')
  if (galaxy.generalManifest.entries.length > 160 || galaxy.events.length > 240) throw new Error('bounded history exceeded')
  if (galaxy.routeBoard.transit) throw new Error('completed route soak left a transit unresolved')
  if (galaxy.routeBoard.history.length > 24) throw new Error('bounded Route Board history exceeded')
  for (const contract of galaxy.sealedPackageContracts) {
    if (contract.status === 'accepted' && !contract.packageId) throw new Error(`accepted contract ${contract.id} has no package`)
    if ((contract.status === 'completed' || contract.status === 'expired') && contract.resolvedAtRouteReckoning === undefined) throw new Error(`terminal contract ${contract.id} lacks a canonical resolution time`)
  }
}

const resolveRoute = (source: GalaxyState, connectionId: string): GalaxyState => {
  const selected = selectRouteBoardConnection(source, connectionId)
  if (!selected.changed) throw new Error(selected.message)
  const committed = commitRouteBoardTransit(selected.galaxy)
  if (!committed.changed) throw new Error(committed.message)
  const resolved = resolveRouteBoardTransit(committed.galaxy)
  if (!resolved.changed) throw new Error(resolved.message)
  return resolved.galaxy
}

const simulate = (): { galaxy: GalaxyState; elapsedMs: number; routeTransits: number } => {
  const initial = createGalaxy(seed, newHero({ name: 'Soak Courier' }))
  const offered = initial.sealedPackageContracts[0]
  if (!offered) throw new Error('missing Kestrel calibration offer')
  const accepted = acceptSealedPackageContract(initial, offered.id, initial.activeCourierId)
  if (!accepted.changed) throw new Error(accepted.message)
  const started = performance.now()
  let galaxy = accepted.galaxy
  let remainingMarks = marks
  let routeTransits = 0
  while (remainingMarks >= 720) {
    const routed = resolveRoute(resolveRoute(galaxy, 'route:kestrel-orison'), 'route:kestrel-orison')
    const routedMarks = routed.routeReckoning - galaxy.routeReckoning
    if (routedMarks > remainingMarks) break
    galaxy = routed
    remainingMarks -= routedMarks
    routeTransits += 2
  }
  galaxy = advanceGalaxyRouteReckoning(galaxy, remainingMarks)
  return { galaxy, elapsedMs: performance.now() - started, routeTransits }
}

const first = simulate()
const second = simulate()
assertInvariants(first.galaxy)
assertInvariants(second.galaxy)
const firstFingerprint = fingerprint(first.galaxy)
const secondFingerprint = fingerprint(second.galaxy)
if (firstFingerprint !== secondFingerprint) throw new Error(`non-deterministic fingerprint: ${firstFingerprint} !== ${secondFingerprint}`)

console.log(JSON.stringify({
  seed,
  marks,
  routeReckoning: first.galaxy.routeReckoning,
  routeTransits: first.routeTransits,
  routeHistoryEntries: first.galaxy.routeBoard.history.length,
  worldTicks: first.galaxy.lastWorldTick,
  manifestEntries: first.galaxy.generalManifest.entries.length,
  chronicleEvents: first.galaxy.events.length,
  firstFingerprint,
  secondFingerprint,
  firstElapsedMs: Number(first.elapsedMs.toFixed(2)),
  secondElapsedMs: Number(second.elapsedMs.toFixed(2)),
  marksPerMs: Number((marks / Math.max(first.elapsedMs, second.elapsedMs)).toFixed(2))
}, null, 2))
