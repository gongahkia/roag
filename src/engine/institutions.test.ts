import { describe, expect, it } from 'vitest'
import { abandonSealedPackage, acceptSealedPackageContract, advanceGalaxyRouteReckoning, commitRouteBoardTransit, createGalaxy, declineSealedPackageContract, decideGalaxyInstitutionRequest, deliverSealedPackage, expireSealedPackageContracts, INSTITUTION_ACTOR_LIMIT, INSTITUTION_ACTOR_MEMORY_LIMIT, INSTITUTION_CAUSAL_EVENT_LIMIT, INSTITUTION_OPERATION_LIMIT, INSTITUTION_REPORT_LIMIT, INSTITUTION_RESOLVED_OPERATION_LIMIT, institutionDefinition, institutionReportFreshness, loseGalaxyCourier, markSealedPackageDestinationReached, migrateGalaxy, newHero, refuseSealedPackage, resolveRouteBoardTransit, routeBoardConnection, routeBoardConnectionForGalaxy, selectGalaxyCourier, selectRouteBoardConnection, violateSealedPackageSeal } from '../engine'
import { rngFor } from '../rng'
import { installGalaxyNeridaBypass } from './galaxy'

const hero = () => newHero({ name: 'Institution Test Courier' })
const accept = (source = createGalaxy(8_501, hero())) => {
  const contract = source.sealedPackageContracts[0]!
  return acceptSealedPackageContract(source, contract.id, source.activeCourierId).galaxy
}
const breach = (source = accept()) => violateSealedPackageSeal(source, source.sealedPackageContracts[0]!.id).galaxy
const travel = (source: ReturnType<typeof createGalaxy>, connectionId: string) => resolveRouteBoardTransit(commitRouteBoardTransit(selectRouteBoardConnection(source, connectionId).galaxy).galaxy).galaxy
const nerida = (source: ReturnType<typeof createGalaxy>) => travel(travel(source, 'route:kestrel-orison'), 'route:orison-nerida')
const portStanding = (galaxy: ReturnType<typeof createGalaxy>) => galaxy.institutionWorld.states['institution:nerida-port-continuity'].standing

describe('Nerida institutions and recurring rivals', () => {
  it('initializes authored institutions and actors deterministically without advancing a closed campaign', () => {
    const first = createGalaxy(8_511, hero())
    const second = createGalaxy(8_511, hero())
    expect(first.institutionWorld).toEqual(second.institutionWorld)
    expect(first.institutionWorld.actors.map(actor => [actor.id, actor.name, actor.civilization])).toEqual([
      ['actor:iren-vos', 'Iren Vos', 'human'],
      ['actor:sava-tesh', 'Sava Tesh', 'taal'],
      ['actor:mera-lio', 'Mera Lio', 'human']
    ])
    expect(institutionDefinition('institution:closure-eight').publicPosture).toContain('pressure change')
    expect(advanceGalaxyRouteReckoning(first, 0)).toEqual(first)
  })

  it('advances only through Route Reckoning and is invariant to batching and institution record order', () => {
    const initial = breach(accept(createGalaxy(8_521, hero())))
    const batched = advanceGalaxyRouteReckoning(initial, 960)
    const chunked = [240, 180, 240, 300].reduce((galaxy, marks) => advanceGalaxyRouteReckoning(galaxy, marks), initial)
    const reordered = structuredClone(initial)
    reordered.institutionWorld.states = Object.fromEntries(Object.entries(reordered.institutionWorld.states).reverse()) as typeof reordered.institutionWorld.states
    const reorderedAdvanced = advanceGalaxyRouteReckoning(reordered, 960)
    expect(chunked).toEqual(batched)
    expect(reorderedAdvanced).toEqual(batched)
    expect(initial.institutionWorld.operations).toHaveLength(1)
    expect(batched.institutionWorld.operations).toHaveLength(0)
  })

  it('persists named rival identity and memory across reload without sharing an unrelated RNG stream', () => {
    const initial = breach(accept(createGalaxy(8_523, hero())))
    const baseline = advanceGalaxyRouteReckoning(initial, 240)
    const unrelated = rngFor(initial.seed, 'galaxy', 'unrelated-institution-consumer')
    for (let draw = 0; draw < 64; draw++) unrelated.next()
    const afterUnrelatedDraws = advanceGalaxyRouteReckoning(initial, 240)
    const reloaded = migrateGalaxy(JSON.parse(JSON.stringify(afterUnrelatedDraws)))!
    expect(afterUnrelatedDraws).toEqual(baseline)
    expect(reloaded.institutionWorld.rival).toEqual(afterUnrelatedDraws.institutionWorld.rival)
    expect(reloaded.institutionWorld.actors.find(actor => actor.id === 'actor:iren-vos')).toEqual(afterUnrelatedDraws.institutionWorld.actors.find(actor => actor.id === 'actor:iren-vos'))
  })

  it('creates Iren Vos once from a seal breach, records a memory, and applies known route pressure exactly once', () => {
    const opened = breach()
    const retried = violateSealedPackageSeal(opened, opened.sealedPackageContracts[0]!.id)
    expect(opened.institutionWorld.rival).toMatchObject({ id: 'rival:iren-vos', actorId: 'actor:iren-vos', status: 'active' })
    expect(retried.changed).toBe(false)
    const advanced = advanceGalaxyRouteReckoning(opened, 240)
    const repeated = advanceGalaxyRouteReckoning(advanced, 240)
    const connection = routeBoardConnection('route:orison-nerida')!
    expect(routeBoardConnectionForGalaxy(advanced, connection)).toMatchObject({ durationMarks: 360, risk: 'high' })
    expect(repeated.institutionWorld.knownRouteModifiers).toEqual(advanced.institutionWorld.knownRouteModifiers)
    expect(advanced.generalManifest.entries.filter(entry => entry.kind === 'rivalEmergence')).toHaveLength(1)
    expect(advanced.generalManifest.entries.filter(entry => entry.kind === 'institutionOperationKnown')).toHaveLength(1)
  })

  it('keeps reports sourced, fresh, and absent until an operation is known, while inspection stays non-mutating', () => {
    const hidden = advanceGalaxyRouteReckoning(createGalaxy(8_526, hero()), 660)
    const beforeInspection = structuredClone(hidden)
    expect(hidden.institutionWorld.reports).toEqual([])
    expect(routeBoardConnectionForGalaxy(hidden, routeBoardConnection('route:orison-nerida')!)).toMatchObject({ durationMarks: 300 })
    expect(institutionDefinition('institution:blue-intake-board').name).toBe("Blue Intake Residents' Board")
    expect(hidden).toEqual(beforeInspection)

    const known = advanceGalaxyRouteReckoning(breach(accept(createGalaxy(8_527, hero()))), 240)
    const report = known.institutionWorld.reports.find(candidate => candidate.actorId === 'actor:iren-vos')!
    expect(report).toMatchObject({ source: 'institution-notice', confidence: 'confirmed', destinationId: 'destination:nerida' })
    expect(institutionReportFreshness(report, known.routeReckoning)).toBe('current')
    expect(institutionReportFreshness(report, known.routeReckoning + 121)).toBe('aging')
    expect(institutionReportFreshness(report, known.routeReckoning + 361)).toBe('stale')
  })

  it('applies configured, distinct package-outcome relationships exactly once', () => {
    const declinedBase = createGalaxy(8_528, hero())
    const declined = declineSealedPackageContract(declinedBase, declinedBase.sealedPackageContracts[0]!.id).galaxy

    const refusedBase = accept(createGalaxy(8_529, hero()))
    const refused = refuseSealedPackage(refusedBase, refusedBase.sealedPackageContracts[0]!.id).galaxy

    const abandonedBase = accept(createGalaxy(8_530, hero()))
    const abandoned = abandonSealedPackage(abandonedBase, abandonedBase.sealedPackageContracts[0]!.id).galaxy

    const expiredBase = accept(createGalaxy(8_532, hero()))
    const expired = expireSealedPackageContracts(expiredBase, expiredBase.sealedPackageContracts[0]!.terms.deadlineReckoning + 1).galaxy

    const intactBase = accept(createGalaxy(8_533, hero()))
    const intactContract = intactBase.sealedPackageContracts[0]!
    const intact = deliverSealedPackage(markSealedPackageDestinationReached(intactBase, intactContract.terms.destinationSiteId), intactContract.id, newHero({ name: 'Settlement Courier' })).galaxy

    const tamperedBase = breach(accept(createGalaxy(8_534, hero())))
    const tamperedContract = tamperedBase.sealedPackageContracts[0]!
    const tampered = deliverSealedPackage(markSealedPackageDestinationReached(tamperedBase, tamperedContract.terms.destinationSiteId), tamperedContract.id, newHero({ name: 'Tampered Settlement Courier' })).galaxy

    const signatures = [declined, refused, abandoned, expired, intact, tampered]
      .map(portStanding)
      .map(standing => [standing.trust, standing.scrutiny, standing.obligation, standing.grievance].join(':'))
    expect(new Set(signatures).size).toBe(signatures.length)
    const once = advanceGalaxyRouteReckoning(abandoned, 120)
    expect(portStanding(once)).toEqual(portStanding(abandoned))
    expect(once.generalManifest.entries.filter(entry => entry.kind === 'institutionRelationshipChanged')).toHaveLength(abandoned.generalManifest.entries.filter(entry => entry.kind === 'institutionRelationshipChanged').length)
  })

  it('supports a second rival emergence path through the existing Nerida bypass without duplicating the rival', () => {
    const atNerida = nerida(advanceGalaxyRouteReckoning(createGalaxy(8_531, hero()), 480))
    const bypass = installGalaxyNeridaBypass(atNerida)
    expect(bypass.changed).toBe(true)
    expect(bypass.galaxy.institutionWorld.rival).toMatchObject({ id: 'rival:iren-vos' })
    const repeated = installGalaxyNeridaBypass(bypass.galaxy)
    expect(repeated.changed).toBe(false)
    expect(bypass.galaxy.generalManifest.entries.filter(entry => entry.kind === 'rivalEmergence')).toHaveLength(1)
  })

  it('keeps operation truth in M4 Nerida partitions while exposing only known reports and route modifiers', () => {
    const afterCavitation = advanceGalaxyRouteReckoning(createGalaxy(8_541, hero()), 660)
    const partition = afterCavitation.destinationWorld.partitions['destination:nerida']
    expect(partition.pressure.value).toBeLessThan(70)
    expect(partition.consequences.some(consequence => consequence.id === 'consequence:operation:blue-intake:habitat-relief')).toBe(true)
    expect(afterCavitation.institutionWorld.reports).toEqual([])
    expect(routeBoardConnectionForGalaxy(afterCavitation, routeBoardConnection('route:orison-nerida')!).durationMarks).toBe(300)
  })

  it('records a confirmed Nerida decision exactly once and adapts Iren Vos after prior compliance', () => {
    const atNerida = nerida(breach(accept(createGalaxy(8_551, hero()))))
    const complied = decideGalaxyInstitutionRequest(atNerida, 'comply')
    expect(complied.changed).toBe(true)
    const second = decideGalaxyInstitutionRequest(complied.galaxy, 'refuse')
    expect(second.changed).toBe(false)
    const operation = complied.galaxy.institutionWorld.operations.find(candidate => candidate.actorId === 'actor:iren-vos')
    expect(operation).toMatchObject({ kind: 'iren-evidence-inspection', purpose: expect.stringContaining('compliance review') })
    const resolved = advanceGalaxyRouteReckoning(complied.galaxy, 240)
    expect(resolved.institutionWorld.knownRouteModifiers.find(modifier => modifier.operationId === operation!.id)).toMatchObject({ durationMarks: 30, warning: expect.stringContaining('compliance review') })
    expect(resolved.generalManifest.entries.filter(entry => entry.kind === 'institutionDecision')).toHaveLength(1)
  })

  it('preserves a rival memory through courier death and replacement without reviving the courier', () => {
    const opened = breach(accept(createGalaxy(8_561, hero())))
    const lost = loseGalaxyCourier(opened, opened.activeCourierId, 'test loss during a documented landing')
    const replacementId = lost.couriers.find(courier => courier.id !== lost.activeCourierId && courier.status === 'available')!.id
    const selected = selectGalaxyCourier(lost, replacementId).galaxy
    const actor = selected.institutionWorld.actors.find(candidate => candidate.id === 'actor:iren-vos')!
    expect(selected.couriers.find(courier => courier.id === opened.activeCourierId)?.status).toBe('dead')
    expect(selected.activeCourierId).toBe(replacementId)
    expect(actor.memories.map(memory => memory.kind)).toEqual(expect.arrayContaining(['seal-breach', 'courier-loss', 'courier-replacement']))
    expect(selected.institutionWorld.rival?.originalCourierId).toBe(opened.activeCourierId)
  })

  it('creates only one briefed successor after an assisted Closure Eight repair', () => {
    const bypassed = installGalaxyNeridaBypass(nerida(advanceGalaxyRouteReckoning(createGalaxy(8_571, hero()), 480))).galaxy
    const assisted = decideGalaxyInstitutionRequest(bypassed, 'assist')
    const repaired = advanceGalaxyRouteReckoning(assisted.galaxy, 180)
    const succeeded = advanceGalaxyRouteReckoning(repaired, 240)
    const successor = succeeded.institutionWorld.actors.find(actor => actor.id === 'actor:nadi-rell')
    expect(successor).toMatchObject({ status: 'active', memories: [expect.objectContaining({ kind: 'briefing', witnessed: false })] })
    expect(succeeded.institutionWorld.rival).toMatchObject({ actorId: 'actor:nadi-rell', status: 'replaced' })
    expect(advanceGalaxyRouteReckoning(succeeded, 480).institutionWorld.actors.filter(actor => actor.id === 'actor:nadi-rell')).toHaveLength(1)
  })

  it('migrates v4 without historical institutional action and retains stable v5 state across reload', () => {
    const legacy = JSON.parse(JSON.stringify(advanceGalaxyRouteReckoning(createGalaxy(8_581, hero()), 900))) as Record<string, unknown>
    legacy.version = 4
    delete legacy.institutionWorld
    const migrated = migrateGalaxy(legacy)!
    const reloaded = migrateGalaxy(JSON.parse(JSON.stringify(migrated)))!
    expect(migrated).toMatchObject({ version: 6, routeReckoning: 900, institutionWorld: { operations: [], causalEvents: [], lastProcessedManifestSequence: migrated.generalManifest.nextSequence - 1 } })
    expect(reloaded).toEqual(migrated)
  })

  it('compacts malformed oversized causal histories without losing Manifest-referenced facts or leaving dangling parents', () => {
    const oversized = createGalaxy(8_586, hero())
    const referencedId = 'causal:synthetic:0'
    oversized.institutionWorld.causalEvents = Array.from({ length: INSTITUTION_CAUSAL_EVENT_LIMIT + 8 }, (_, sequence) => ({
      version: 1 as const,
      id: `causal:synthetic:${sequence}`,
      sequence,
      atRouteReckoning: sequence,
      kind: 'manifest-observed' as const,
      parentIds: sequence === 0 ? [] : [`causal:synthetic:${sequence - 1}`],
      knownToJomon: false,
      learnedThrough: 'manifest' as const,
      summary: `Synthetic causal record ${sequence}`,
      effects: { sequence }
    }))
    oversized.institutionWorld.nextCausalSequence = oversized.institutionWorld.causalEvents.length
    oversized.generalManifest.entries[0] = { ...oversized.generalManifest.entries[0]!, causalEventId: referencedId, affectedEntityIds: [...(oversized.generalManifest.entries[0]!.affectedEntityIds ?? []), referencedId] }
    const migrated = migrateGalaxy(JSON.parse(JSON.stringify(oversized)))!
    expect(migrated.institutionWorld.causalEvents.length).toBeLessThanOrEqual(INSTITUTION_CAUSAL_EVENT_LIMIT)
    expect(migrated.institutionWorld.causalEvents.some(event => event.id === referencedId)).toBe(true)
    for (const event of migrated.institutionWorld.causalEvents) expect(event.parentIds.every(parentId => migrated.institutionWorld.causalEvents.some(parent => parent.id === parentId && parent.sequence < event.sequence))).toBe(true)
  })

  it('keeps causal, actor, report, and operation collections within their explicit bounds', () => {
    const advanced = advanceGalaxyRouteReckoning(breach(accept(createGalaxy(8_591, hero()))), 100_000)
    const world = advanced.institutionWorld
    expect(world.operations.length).toBeLessThanOrEqual(INSTITUTION_OPERATION_LIMIT)
    expect(world.resolvedOperationIds.length).toBeLessThanOrEqual(INSTITUTION_RESOLVED_OPERATION_LIMIT)
    expect(world.causalEvents.length).toBeLessThanOrEqual(INSTITUTION_CAUSAL_EVENT_LIMIT)
    expect(world.reports.length).toBeLessThanOrEqual(INSTITUTION_REPORT_LIMIT)
    expect(world.actors.length).toBeLessThanOrEqual(INSTITUTION_ACTOR_LIMIT)
    expect(world.actors.every(actor => actor.memories.length <= INSTITUTION_ACTOR_MEMORY_LIMIT)).toBe(true)
    for (const event of world.causalEvents) expect(event.parentIds.every(parentId => world.causalEvents.some(parent => parent.id === parentId && parent.sequence < event.sequence))).toBe(true)
  })
})
