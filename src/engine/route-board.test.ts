import { describe, expect, it } from 'vitest'
import { acceptSealedPackageContract } from './sealed-packages'
import { clearRouteBoardConnectionSelection, commitRouteBoardTransit, createGalaxy, migrateGalaxy, resolveRouteBoardTransit, selectRouteBoardConnection } from './galaxy'
import { routeBoardConnection, routeBoardConnectionsFor, routeBoardDestination, routeBoardOtherDestination } from './route-board'
import { newHero } from './run'

const galaxy = (seed = 811) => createGalaxy(seed, newHero({ name: 'Ari' }), 0)

describe('Jomon Route Board', () => {
  it('creates a stable branching Kestrel network with safe and elevated initial choices', () => {
    const first = galaxy(401)
    const second = galaxy(401)
    const current = routeBoardDestination(first.routeBoard.currentDestinationId)!
    const routes = routeBoardConnectionsFor(current.id)

    expect(first.routeBoard).toEqual(second.routeBoard)
    expect(first.version).toBe(6)
    expect(current).toMatchObject({ id: 'destination:kestrel', label: 'Kestrel Landing', siteId: first.activeSiteId })
    expect(routes.filter(route => route.status === 'open').map(route => [route.id, route.durationMarks, route.risk])).toEqual([
      ['route:kestrel-orison', 360, 'low'],
      ['route:kestrel-halcyon', 180, 'elevated']
    ])
    expect(first.routeBoard.knownDestinationIds).toHaveLength(5)
  })

  it('keeps unreachable routes unselectable and restores a cancelled preview without canonical mutation', () => {
    const initial = galaxy()
    const blocked = selectRouteBoardConnection(initial, 'route:orison-borealis')
    const preview = selectRouteBoardConnection(initial, 'route:kestrel-orison')
    const cancelled = clearRouteBoardConnectionSelection(preview.galaxy)

    expect(blocked.changed).toBe(false)
    expect(preview.changed).toBe(true)
    expect(preview.galaxy.routeBoard.selectedConnectionId).toBe('route:kestrel-orison')
    expect(cancelled.changed).toBe(true)
    expect(cancelled.galaxy.routeBoard).toEqual(initial.routeBoard)
    expect(cancelled.galaxy.routeReckoning).toBe(initial.routeReckoning)
    expect(cancelled.galaxy.generalManifest.entries).toEqual(initial.generalManifest.entries)
  })

  it('commits one stored transit and resolves its exact canonical duration once', () => {
    const initial = galaxy(109)
    const preview = selectRouteBoardConnection(initial, 'route:kestrel-halcyon')
    const committed = commitRouteBoardTransit(preview.galaxy)
    const duplicate = commitRouteBoardTransit(committed.galaxy)
    const transit = committed.galaxy.routeBoard.transit!
    const arrived = resolveRouteBoardTransit(committed.galaxy)
    const resolvedAgain = resolveRouteBoardTransit(arrived.galaxy)
    const destination = routeBoardDestination(transit.toDestinationId)!

    expect(committed.changed).toBe(true)
    expect(duplicate.changed).toBe(false)
    expect(committed.galaxy.generalManifest.entries.filter(entry => entry.kind === 'routeCommitted')).toHaveLength(1)
    expect(committed.galaxy.generalManifest.entries.filter(entry => entry.kind === 'routeDeparted')).toHaveLength(1)
    expect(arrived.changed).toBe(true)
    expect(arrived.galaxy.routeReckoning).toBe(transit.durationMarks + transit.consequence.additionalMarks)
    expect(arrived.galaxy.routeBoard).toMatchObject({ currentDestinationId: destination.id, history: [{ connectionId: transit.connectionId, transitId: transit.id }] })
    expect(arrived.galaxy.activeSiteId).toBe(destination.siteId)
    expect(arrived.galaxy.sites[destination.siteId]!.discovered).toBe(true)
    expect(arrived.galaxy.generalManifest.entries.filter(entry => entry.kind === 'routeTransitDelayed')).toHaveLength(transit.consequence.kind === 'navigationDelay' ? 1 : 0)
    expect(arrived.galaxy.generalManifest.entries.filter(entry => entry.kind === 'routeArrived')).toHaveLength(1)
    expect(resolvedAgain.changed).toBe(false)
    expect(resolvedAgain.galaxy).toEqual(arrived.galaxy)
  })

  it('stores a seeded navigation delay before presentation and resolves identically after a save-like reload', () => {
    const initial = galaxy(2)
    const committed = commitRouteBoardTransit(selectRouteBoardConnection(initial, 'route:kestrel-halcyon').galaxy).galaxy
    const reloaded = migrateGalaxy(JSON.parse(JSON.stringify(committed)))!
    const directArrival = resolveRouteBoardTransit(committed)
    const reloadedArrival = resolveRouteBoardTransit(reloaded)

    expect(committed.routeBoard.transit).toMatchObject({ consequence: { kind: 'navigationDelay', additionalMarks: 60 } })
    expect(reloaded.routeBoard.transit).toEqual(committed.routeBoard.transit)
    expect(reloadedArrival.galaxy).toEqual(directArrival.galaxy)
    expect(directArrival.galaxy.generalManifest.entries.filter(entry => entry.kind === 'routeTransitDelayed')).toHaveLength(1)
  })

  it('expires an accepted Kestrel package during canonical transit exactly once', () => {
    const initial = galaxy(53)
    const contract = initial.sealedPackageContracts[0]!
    const accepted = acceptSealedPackageContract(initial, contract.id, initial.activeCourierId).galaxy
    const connection = routeBoardConnection('route:kestrel-orison')!
    accepted.sealedPackageContracts[0]!.terms.deadlineReckoning = connection.durationMarks - 1
    const preview = selectRouteBoardConnection(accepted, connection.id)
    const committed = commitRouteBoardTransit(preview.galaxy)
    const arrived = resolveRouteBoardTransit(committed.galaxy)
    const reread = migrateGalaxy(JSON.parse(JSON.stringify(arrived.galaxy)))!

    expect(arrived.galaxy.sealedPackageContracts[0]).toMatchObject({ status: 'expired', resolvedAtRouteReckoning: connection.durationMarks })
    expect(arrived.galaxy.generalManifest.entries.filter(entry => entry.kind === 'contractExpired')).toHaveLength(1)
    expect(arrived.galaxy.generalManifest.entries.filter(entry => entry.kind === 'deliveryFailed')).toHaveLength(1)
    expect(reread.sealedPackageContracts[0]).toMatchObject({ status: 'expired', resolvedAtRouteReckoning: connection.durationMarks })
    expect(reread.generalManifest.entries.filter(entry => entry.kind === 'contractExpired')).toHaveLength(1)
  })

  it('does not settle an accepted Kestrel package merely by returning Jomon to Kestrel', () => {
    const initial = galaxy(89)
    const contract = initial.sealedPackageContracts[0]!
    const accepted = acceptSealedPackageContract(initial, contract.id, initial.activeCourierId).galaxy
    const atOrison = resolveRouteBoardTransit(commitRouteBoardTransit(selectRouteBoardConnection(accepted, 'route:kestrel-orison').galaxy).galaxy).galaxy
    const backAtKestrel = resolveRouteBoardTransit(commitRouteBoardTransit(selectRouteBoardConnection(atOrison, 'route:kestrel-orison').galaxy).galaxy).galaxy

    expect(backAtKestrel.routeBoard.currentDestinationId).toBe('destination:kestrel')
    expect(backAtKestrel.activeSiteId).toBe(contract.terms.destinationSiteId)
    expect(backAtKestrel.sealedPackageContracts[0]).toMatchObject({ status: 'accepted' })
    expect(backAtKestrel.sealedPackageContracts[0]!.destinationReachedAtRouteReckoning).toBeUndefined()
    expect(backAtKestrel.generalManifest.entries.some(entry => entry.kind === 'deliveryCompleted')).toBe(false)
  })

  it('migrates v2 location and committed transit records deterministically without advancing time', () => {
    const legacy = structuredClone(galaxy(307)) as unknown as Record<string, unknown>
    legacy.version = 2
    legacy.activeSiteId = 'sector-00:site-03'
    delete legacy.routeBoard
    const migrated = migrateGalaxy(legacy)!
    const origin = galaxy(311)
    const preview = selectRouteBoardConnection(origin, 'route:kestrel-orison')
    const committed = commitRouteBoardTransit(preview.galaxy).galaxy
    const resumed = migrateGalaxy(JSON.parse(JSON.stringify(committed)))!
    const directArrival = resolveRouteBoardTransit(committed)
    const resumedArrival = resolveRouteBoardTransit(resumed)

    expect(migrated).toMatchObject({ version: 6, routeReckoning: 0, activeSiteId: 'sector-00:site-03', routeBoard: { currentDestinationId: 'destination:halcyon', networkId: 'helios-intake-v1' }, destinationWorld: { partitions: { 'destination:nerida': { condition: 'pump-watch', lastProcessedRouteReckoning: 0 } } }, institutionWorld: { operations: [], causalEvents: [] } })
    expect(migrateGalaxy(JSON.parse(JSON.stringify(migrated)))!).toEqual(migrated)
    expect(resumed.routeBoard.transit).toEqual(committed.routeBoard.transit)
    expect(resumedArrival.galaxy).toEqual(directArrival.galaxy)
    expect(routeBoardOtherDestination(routeBoardConnection('route:kestrel-orison')!, 'destination:kestrel')?.id).toBe('destination:orison')
  })

  it('migrates a v5 accepted delivery without inventing a run, offer, elapsed time, or tactical history', () => {
    const initial = galaxy(313)
    const accepted = acceptSealedPackageContract(initial, initial.sealedPackageContracts[0]!.id, initial.activeCourierId).galaxy
    const legacy = structuredClone(accepted) as unknown as Record<string, unknown>
    legacy.version = 5
    delete legacy.deliveryRun
    const migrated = migrateGalaxy(legacy)!
    const reopened = migrateGalaxy(JSON.parse(JSON.stringify(migrated)))!

    expect(migrated).toMatchObject({ version: 6, routeReckoning: 0, sealedPackageContracts: [{ status: 'accepted' }] })
    expect(migrated.deliveryRun).toBeUndefined()
    expect(reopened).toEqual(migrated)
  })
})
