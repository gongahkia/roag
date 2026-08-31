import assert from 'node:assert/strict'
import { createHash } from 'node:crypto'
import { DESTINATION_PARTITION_CONSEQUENCE_LIMIT, DESTINATION_PARTITION_HISTORY_LIMIT, DESTINATION_PARTITION_INTERVENTION_LIMIT, DESTINATION_PARTITION_RESOLVED_LIMIT, DESTINATION_PARTITION_SCHEDULE_LIMIT, advanceGalaxyRouteReckoning, commitRouteBoardTransit, createGalaxy, inspectGalaxyDestination, installGalaxyNeridaBypass, migrateGalaxy, resolveRouteBoardTransit, selectRouteBoardConnection } from '../src/engine'
import { newHero } from '../src/engine/run'

const advance = (source: ReturnType<typeof createGalaxy>, marks: number, chunked: boolean) => chunked ? Array.from({ length: Math.ceil(marks / 60) }, (_, index) => Math.min(60, marks - index * 60)).reduce((galaxy, step) => advanceGalaxyRouteReckoning(galaxy, step), source) : advanceGalaxyRouteReckoning(source, marks)
const travel = (source: ReturnType<typeof createGalaxy>, connectionId: string) => resolveRouteBoardTransit(commitRouteBoardTransit(selectRouteBoardConnection(source, connectionId).galaxy).galaxy).galaxy

const run = (chunked: boolean) => {
  let galaxy = advance(createGalaxy(7_301, newHero({ name: 'Soak Courier' })), 480, chunked)
  galaxy = travel(galaxy, 'route:kestrel-orison')
  galaxy = travel(galaxy, 'route:orison-nerida')
  galaxy = inspectGalaxyDestination(galaxy).galaxy
  galaxy = installGalaxyNeridaBypass(galaxy).galaxy
  galaxy = migrateGalaxy(JSON.parse(JSON.stringify(galaxy)))!
  galaxy = advance(galaxy, 180, chunked)
  galaxy = inspectGalaxyDestination(galaxy).galaxy
  galaxy = travel(galaxy, 'route:orison-nerida')
  galaxy = travel(galaxy, 'route:kestrel-orison')
  galaxy = advance(galaxy, 2_400, chunked)
  galaxy = migrateGalaxy(JSON.parse(JSON.stringify(galaxy)))!
  return galaxy
}

const fingerprint = (galaxy: ReturnType<typeof createGalaxy>): string => JSON.stringify({
  routeReckoning: galaxy.routeReckoning,
  routeBoard: galaxy.routeBoard,
  destinationWorld: galaxy.destinationWorld,
  generalManifest: galaxy.generalManifest,
  events: galaxy.events,
  sealedPackageContracts: galaxy.sealedPackageContracts,
  sealedPackages: galaxy.sealedPackages,
  routeCaches: galaxy.routeCaches
})

const whole = run(false)
const chunked = run(true)
assert.equal(fingerprint(chunked), fingerprint(whole), 'chunked living-world soak diverged from whole-step fingerprint')
assert.equal(whole.destinationWorld.reports['destination:nerida'].reportedCondition, 'pump-stabilized')
assert.equal(whole.generalManifest.entries.filter(entry => entry.kind === 'destinationIntervention').length, 1)
assert.equal(whole.generalManifest.entries.filter(entry => entry.kind === 'destinationDevelopmentResolved').length, 1)
assert.ok(whole.generalManifest.entries.length <= 160, 'General Manifest exceeded its bounded history')
assert.ok(whole.events.length <= 240, 'galaxy event history exceeded its bound')
for (const partition of Object.values(whole.destinationWorld.partitions)) {
  assert.ok(partition.scheduledDevelopments.length <= DESTINATION_PARTITION_SCHEDULE_LIMIT, `${partition.id} scheduled development bound exceeded`)
  assert.ok(partition.resolvedDevelopmentIds.length <= DESTINATION_PARTITION_RESOLVED_LIMIT, `${partition.id} resolved development bound exceeded`)
  assert.ok(partition.consequences.length <= DESTINATION_PARTITION_CONSEQUENCE_LIMIT, `${partition.id} consequence bound exceeded`)
  assert.ok(partition.interventions.length <= DESTINATION_PARTITION_INTERVENTION_LIMIT, `${partition.id} intervention bound exceeded`)
  assert.ok(partition.history.length <= DESTINATION_PARTITION_HISTORY_LIMIT, `${partition.id} local history bound exceeded`)
}

process.stdout.write(`${JSON.stringify({ routeReckoning: whole.routeReckoning, manifestEntries: whole.generalManifest.entries.length, fingerprintSha256: createHash('sha256').update(fingerprint(whole)).digest('hex') }, null, 2)}\n`)
