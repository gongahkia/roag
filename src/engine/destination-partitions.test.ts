import { describe, expect, it } from 'vitest'
import { DESTINATION_PARTITION_CONSEQUENCE_LIMIT, DESTINATION_PARTITION_HISTORY_LIMIT, DESTINATION_PARTITION_IDS, DESTINATION_PARTITION_INTERVENTION_LIMIT, DESTINATION_PARTITION_RESOLVED_LIMIT, DESTINATION_PARTITION_SCHEDULE_LIMIT, NERIDA_BYPASS_COST_MARKS, advanceDestinationWorld, cloneDestinationWorld, createDestinationWorld, destinationReportFreshness } from './destination-partitions'
import { destinationConditionDetail } from './destination-readout'
import { advanceGalaxyRouteReckoning, clearRouteBoardConnectionSelection, commitRouteBoardTransit, createGalaxy, inspectGalaxyDestination, installGalaxyNeridaBypass, migrateGalaxy, reconcileGalaxy, resolveRouteBoardTransit, routeBoardConnectionForGalaxy, selectRouteBoardConnection } from './galaxy'
import { routeBoardConnection } from './route-board'
import { newHero } from './run'

const galaxy = (seed = 911) => createGalaxy(seed, newHero({ name: 'Ari' }), 0)
const travel = (source: ReturnType<typeof galaxy>, connectionId: string) => resolveRouteBoardTransit(commitRouteBoardTransit(selectRouteBoardConnection(source, connectionId).galaxy).galaxy).galaxy
const atNeridaAfterAbsence = () => travel(travel(galaxy(913), 'route:kestrel-orison'), 'route:orison-nerida')

describe('persistent destination partitions', () => {
  it('advances loaded and remote destinations through active Route Reckoning only', () => {
    const initial = galaxy()
    const advanced = advanceGalaxyRouteReckoning(initial, 480)

    expect(advanced.routeBoard.currentDestinationId).toBe('destination:kestrel')
    expect(advanced.destinationWorld.partitions['destination:nerida']).toMatchObject({ condition: 'cavitation-restriction', lastProcessedRouteReckoning: 480 })
    expect(advanced.destinationWorld.reports['destination:nerida']).toMatchObject({ reportedCondition: 'pump-watch', source: 'initial-chart' })
    expect(reconcileGalaxy(advanced, Number.MAX_SAFE_INTEGER)).toEqual(advanced)
  })

  it('is equivalent for large or chunked canonical advancement', () => {
    const initial = galaxy(917)
    const whole = advanceGalaxyRouteReckoning(initial, 1_000)
    const chunked = [240, 160, 300, 300].reduce((current, marks) => advanceGalaxyRouteReckoning(current, marks), initial)

    expect(chunked).toEqual(whole)
  })

  it('does not depend on partition iteration order or another partition RNG stream', () => {
    const initial = createDestinationWorld(919)
    const ordered = advanceDestinationWorld(initial, 919, 1_000)
    const reversed = advanceDestinationWorld(initial, 919, 1_000, [...DESTINATION_PARTITION_IDS].reverse())
    const withUnrelatedActivity = cloneDestinationWorld(initial)
    withUnrelatedActivity.partitions['destination:kestrel'].scheduledDevelopments.push({ id: 'development:kestrel:independent-audit', kind: 'kestrel-inspection-audit', dueAtRouteReckoning: 120 })
    const isolated = advanceDestinationWorld(withUnrelatedActivity, 919, 1_000)

    expect(reversed.world).toEqual(ordered.world)
    expect(isolated.world.partitions['destination:nerida']).toEqual(ordered.world.partitions['destination:nerida'])
  })

  it('keeps reading reports and cancelling a Route Board preview non-mutating to destination truth', () => {
    const initial = galaxy(929)
    const before = structuredClone(initial.destinationWorld)
    const inspected = inspectGalaxyDestination(initial)
    const preview = selectRouteBoardConnection(initial, 'route:kestrel-orison')
    const cancelled = clearRouteBoardConnectionSelection(preview.galaxy)

    expect(initial.destinationWorld).toEqual(before)
    expect(inspected.galaxy.destinationWorld.partitions).toEqual(before.partitions)
    expect(inspected.galaxy.generalManifest).toEqual(initial.generalManifest)
    expect(cancelled.galaxy.destinationWorld).toEqual(before)
    expect(cancelled.galaxy.routeReckoning).toBe(initial.routeReckoning)
  })

  it('persists scheduled development identities over a save/reload boundary and resolves each once', () => {
    const beforeCavitation = advanceGalaxyRouteReckoning(galaxy(937), 479)
    const reloaded = migrateGalaxy(JSON.parse(JSON.stringify(beforeCavitation)))!
    const uninterrupted = advanceGalaxyRouteReckoning(beforeCavitation, 1)
    const resumed = advanceGalaxyRouteReckoning(reloaded, 1)
    const repeated = advanceGalaxyRouteReckoning(resumed, 200)
    const nerida = repeated.destinationWorld.partitions['destination:nerida']

    expect(resumed.destinationWorld).toEqual(uninterrupted.destinationWorld)
    expect(nerida.resolvedDevelopmentIds.filter(id => id === 'development:nerida:pump-cavitation')).toHaveLength(1)
    expect(nerida.history.filter(entry => entry.id === 'history:development:nerida:pump-cavitation')).toHaveLength(1)
  })

  it('keeps unseen truth hidden behind an aged report until arrival refreshes it', () => {
    const unseen = advanceGalaxyRouteReckoning(galaxy(941), 480)
    const report = unseen.destinationWorld.reports['destination:nerida']
    const arrived = atNeridaAfterAbsence()

    expect(unseen.destinationWorld.partitions['destination:nerida'].condition).toBe('cavitation-restriction')
    expect(report.reportedCondition).toBe('pump-watch')
    expect(destinationReportFreshness(report, unseen.routeReckoning)).toBe('stale')
    expect(arrived.routeReckoning).toBe(660)
    expect(arrived.destinationWorld.reports['destination:nerida']).toMatchObject({ reportedCondition: 'cavitation-restriction', source: 'arrival', confidence: 'confirmed' })
    expect(arrived.generalManifest.entries.filter(entry => entry.kind === 'destinationReportReceived')).toHaveLength(1)
  })

  it('uses confirmed destination knowledge to change a practical route property', () => {
    const arrived = atNeridaAfterAbsence()
    const connection = routeBoardConnectionForGalaxy(arrived, routeBoardConnection('route:orison-nerida')!)

    expect(connection).toMatchObject({ durationMarks: 360, risk: 'high', warning: expect.stringContaining('pump-chain restriction') })
  })

  it('completes the Nerida warning, restriction, intervention, and later stabilization arc durably', () => {
    const restricted = atNeridaAfterAbsence()
    const installed = installGalaxyNeridaBypass(restricted)
    const stabilizingRoute = routeBoardConnectionForGalaxy(installed.galaxy, routeBoardConnection('route:orison-nerida')!)
    const verifiedTruth = advanceGalaxyRouteReckoning(installed.galaxy, 180)
    const verified = inspectGalaxyDestination(verifiedTruth)
    const repeatedInspection = inspectGalaxyDestination(verified.galaxy)

    expect(installed.changed).toBe(true)
    expect(installed.galaxy.routeReckoning).toBe(restricted.routeReckoning + NERIDA_BYPASS_COST_MARKS)
    expect(installed.galaxy.destinationWorld.partitions['destination:nerida']).toMatchObject({ condition: 'bypass-stabilizing', interventions: [{ id: 'intervention:nerida:bypass-installation', costMarks: NERIDA_BYPASS_COST_MARKS }] })
    expect(stabilizingRoute).toMatchObject({ durationMarks: 360, risk: 'high' })
    expect(verified.galaxy.destinationWorld.reports['destination:nerida']).toMatchObject({ reportedCondition: 'pump-stabilized', source: 'local-inspection' })
    expect(verified.galaxy.generalManifest.entries.filter(entry => entry.kind === 'destinationIntervention')).toHaveLength(1)
    expect(verified.galaxy.generalManifest.entries.filter(entry => entry.kind === 'destinationDevelopmentResolved')).toHaveLength(1)
    expect(repeatedInspection.galaxy.generalManifest.entries.filter(entry => entry.kind === 'destinationDevelopmentResolved')).toHaveLength(1)
    expect(routeBoardConnectionForGalaxy(verified.galaxy, routeBoardConnection('route:orison-nerida')!).durationMarks).toBe(300)
  })

  it('migrates v3 saves deterministically without simulating their historical Route Reckoning', () => {
    const legacy = structuredClone(galaxy(947)) as unknown as Record<string, unknown>
    legacy.version = 3
    legacy.routeReckoning = 900
    legacy.lastWorldTick = 2
    delete legacy.destinationWorld
    const first = migrateGalaxy(legacy)!
    const reopened = migrateGalaxy(JSON.parse(JSON.stringify(first)))!

    expect(first).toMatchObject({ version: 4, routeReckoning: 900, destinationWorld: { partitions: { 'destination:nerida': { condition: 'pump-watch', lastProcessedRouteReckoning: 900, scheduledDevelopments: [{ id: 'development:nerida:pump-cavitation', dueAtRouteReckoning: 1_380 }] } } } })
    expect(reopened).toEqual(first)
  })

  it('bounds durable partition state after substantial canonical advancement', () => {
    const advanced = advanceGalaxyRouteReckoning(galaxy(953), 100_000)

    for (const partition of Object.values(advanced.destinationWorld.partitions)) {
      expect(partition.scheduledDevelopments.length).toBeLessThanOrEqual(DESTINATION_PARTITION_SCHEDULE_LIMIT)
      expect(partition.resolvedDevelopmentIds.length).toBeLessThanOrEqual(DESTINATION_PARTITION_RESOLVED_LIMIT)
      expect(partition.consequences.length).toBeLessThanOrEqual(DESTINATION_PARTITION_CONSEQUENCE_LIMIT)
      expect(partition.interventions.length).toBeLessThanOrEqual(DESTINATION_PARTITION_INTERVENTION_LIMIT)
      expect(partition.history.length).toBeLessThanOrEqual(DESTINATION_PARTITION_HISTORY_LIMIT)
      expect(partition.lastProcessedRouteReckoning).toBe(100_000)
    }
  })

  it('keeps new destination language free of legacy carrier and final-destination terminology', () => {
    const restricted = atNeridaAfterAbsence()
    const intervention = installGalaxyNeridaBypass(restricted)
    const playerFacing = [destinationConditionDetail('cavitation-restriction'), intervention.message, ...intervention.galaxy.generalManifest.entries.filter(entry => entry.source === 'destination').map(entry => entry.detail)].join(' ')

    expect(playerFacing).not.toMatch(/voyager|new edo/i)
  })
})
