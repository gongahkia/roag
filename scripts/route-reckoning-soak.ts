import { createHash } from 'node:crypto'
import { performance } from 'node:perf_hooks'
import { acceptSealedPackageContract, advanceGalaxyRouteReckoning, createGalaxy, newHero } from '../src/engine'
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
  manifest: galaxy.generalManifest.entries.map(entry => [entry.id, entry.routeReckoning, entry.kind, entry.affectedEntityIds, entry.payload])
})).digest('hex')

const assertInvariants = (galaxy: GalaxyState): void => {
  if (!Number.isInteger(galaxy.routeReckoning) || galaxy.routeReckoning !== marks) throw new Error(`invalid Route Reckoning ${galaxy.routeReckoning}`)
  for (const site of Object.values(galaxy.sites)) {
    for (const value of [site.integrity, site.ecology, site.construction, site.supplies, site.salvage]) if (!Number.isInteger(value) || value < 0 || value > 100) throw new Error(`invalid site value at ${site.id}: ${value}`)
  }
  const ids = [...galaxy.events.map(event => event.id), ...galaxy.generalManifest.entries.map(entry => entry.id)]
  if (new Set(ids).size !== ids.length) throw new Error('duplicate chronicle or Manifest ID')
  if (galaxy.generalManifest.entries.length > 160 || galaxy.events.length > 240) throw new Error('bounded history exceeded')
  for (const contract of galaxy.sealedPackageContracts) {
    if (contract.status === 'accepted' && !contract.packageId) throw new Error(`accepted contract ${contract.id} has no package`)
    if ((contract.status === 'completed' || contract.status === 'expired') && contract.resolvedAtRouteReckoning === undefined) throw new Error(`terminal contract ${contract.id} lacks a canonical resolution time`)
  }
}

const simulate = (): { galaxy: GalaxyState; elapsedMs: number } => {
  const initial = createGalaxy(seed, newHero({ name: 'Soak Courier' }))
  const offered = initial.sealedPackageContracts[0]
  if (!offered) throw new Error('missing Kestrel calibration offer')
  const accepted = acceptSealedPackageContract(initial, offered.id, initial.activeCourierId)
  if (!accepted.changed) throw new Error(accepted.message)
  const started = performance.now()
  const galaxy = advanceGalaxyRouteReckoning(accepted.galaxy, marks)
  return { galaxy, elapsedMs: performance.now() - started }
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
  worldTicks: first.galaxy.lastWorldTick,
  manifestEntries: first.galaxy.generalManifest.entries.length,
  chronicleEvents: first.galaxy.events.length,
  firstFingerprint,
  secondFingerprint,
  firstElapsedMs: Number(first.elapsedMs.toFixed(2)),
  secondElapsedMs: Number(second.elapsedMs.toFixed(2)),
  marksPerMs: Number((marks / Math.max(first.elapsedMs, second.elapsedMs)).toFixed(2))
}, null, 2))
