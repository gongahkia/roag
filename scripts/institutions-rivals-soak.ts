import assert from 'node:assert/strict'
import { createHash } from 'node:crypto'
import { INSTITUTION_ACTOR_LIMIT, INSTITUTION_ACTOR_MEMORY_LIMIT, INSTITUTION_CAUSAL_EVENT_LIMIT, INSTITUTION_OPERATION_LIMIT, INSTITUTION_REPORT_LIMIT, INSTITUTION_RESOLVED_OPERATION_LIMIT, acceptSealedPackageContract, advanceGalaxyRouteReckoning, commitRouteBoardTransit, createGalaxy, decideGalaxyInstitutionRequest, loseGalaxyCourier, loseSealedPackagesForCourier, migrateGalaxy, newHero, recoverSealedPackageRouteCaches, resolveRouteBoardTransit, selectGalaxyCourier, selectRouteBoardConnection, violateSealedPackageSeal } from '../src/engine'

const advance = (source: ReturnType<typeof createGalaxy>, marks: number, chunked: boolean) => chunked
  ? Array.from({ length: Math.ceil(marks / 60) }, (_, index) => Math.min(60, marks - index * 60)).reduce((galaxy, step) => advanceGalaxyRouteReckoning(galaxy, step), source)
  : advanceGalaxyRouteReckoning(source, marks)
const travel = (source: ReturnType<typeof createGalaxy>, connectionId: string) => resolveRouteBoardTransit(commitRouteBoardTransit(selectRouteBoardConnection(source, connectionId).galaxy).galaxy).galaxy

const run = (chunked: boolean) => {
  let galaxy = createGalaxy(91_503, newHero({ name: 'Institution Soak Courier' }))
  const contract = galaxy.sealedPackageContracts[0]!
  galaxy = acceptSealedPackageContract(galaxy, contract.id, galaxy.activeCourierId).galaxy
  galaxy = violateSealedPackageSeal(galaxy, contract.id).galaxy
  galaxy = advance(galaxy, 240, chunked)
  galaxy = travel(galaxy, 'route:kestrel-orison')
  galaxy = migrateGalaxy(JSON.parse(JSON.stringify(galaxy)))!
  galaxy = travel(galaxy, 'route:orison-nerida')
  galaxy = decideGalaxyInstitutionRequest(galaxy, 'assist').galaxy
  galaxy = advance(galaxy, 180, chunked)
  galaxy = migrateGalaxy(JSON.parse(JSON.stringify(galaxy)))!
  galaxy = advance(galaxy, 240, chunked)
  const originalCourierId = galaxy.activeCourierId
  galaxy = loseSealedPackagesForCourier(galaxy, originalCourierId, 'institution-soak-cache', 0)
  galaxy = loseGalaxyCourier(galaxy, originalCourierId, 'institution soak records a permanent courier loss')
  const replacement = galaxy.couriers.find(courier => courier.status === 'available')!
  galaxy = selectGalaxyCourier(galaxy, replacement.id).galaxy
  galaxy = recoverSealedPackageRouteCaches(galaxy, 'institution-soak-cache').galaxy
  galaxy = advance(galaxy, 2_400, chunked)
  galaxy = migrateGalaxy(JSON.parse(JSON.stringify(galaxy)))!
  return galaxy
}

const fingerprint = (galaxy: ReturnType<typeof createGalaxy>) => JSON.stringify({
  routeReckoning: galaxy.routeReckoning,
  routeBoard: galaxy.routeBoard,
  destinationWorld: galaxy.destinationWorld,
  institutionWorld: galaxy.institutionWorld,
  generalManifest: galaxy.generalManifest,
  sealedPackageContracts: galaxy.sealedPackageContracts,
  sealedPackages: galaxy.sealedPackages,
  routeCaches: galaxy.routeCaches,
  couriers: galaxy.couriers.map(courier => ({ id: courier.id, status: courier.status }))
})

const whole = run(false)
const chunked = run(true)
assert.equal(fingerprint(chunked), fingerprint(whole), 'chunked institutions-and-rivals soak diverged from whole-step fingerprint')
assert.equal(whole.institutionWorld.rival?.id, 'rival:iren-vos')
assert.equal(whole.institutionWorld.rival?.status, 'replaced')
assert.ok(whole.institutionWorld.actors.find(actor => actor.id === whole.institutionWorld.rival?.actorId)?.memories.some(memory => memory.kind === 'courier-replacement'), 'replacement courier was not retained in the rival file')
assert.equal(whole.couriers.find(courier => courier.id === 'voyager-crew-0')?.status, 'dead')
assert.ok(whole.routeCaches.every(cache => cache.recovered), 'route-cache package recovery regressed')
assert.ok(whole.generalManifest.entries.some(entry => entry.kind === 'institutionDecision'))
assert.ok(whole.generalManifest.entries.some(entry => entry.kind === 'institutionSuccession'))
assert.ok(whole.generalManifest.entries.some(entry => entry.kind === 'rivalEncounter'))
assert.ok(whole.institutionWorld.operations.length <= INSTITUTION_OPERATION_LIMIT)
assert.ok(whole.institutionWorld.resolvedOperationIds.length <= INSTITUTION_RESOLVED_OPERATION_LIMIT)
assert.ok(whole.institutionWorld.causalEvents.length <= INSTITUTION_CAUSAL_EVENT_LIMIT)
assert.ok(whole.institutionWorld.reports.length <= INSTITUTION_REPORT_LIMIT)
assert.ok(whole.institutionWorld.actors.length <= INSTITUTION_ACTOR_LIMIT)
assert.ok(whole.institutionWorld.actors.every(actor => actor.memories.length <= INSTITUTION_ACTOR_MEMORY_LIMIT))
for (const event of whole.institutionWorld.causalEvents) assert.ok(event.parentIds.every(parentId => whole.institutionWorld.causalEvents.some(parent => parent.id === parentId && parent.sequence < event.sequence)), `causal parent missing or cyclic for ${event.id}`)

process.stdout.write(`${JSON.stringify({ routeReckoning: whole.routeReckoning, manifestEntries: whole.generalManifest.entries.length, actors: whole.institutionWorld.actors.length, reports: whole.institutionWorld.reports.length, fingerprintSha256: createHash('sha256').update(fingerprint(whole)).digest('hex') }, null, 2)}\n`)
