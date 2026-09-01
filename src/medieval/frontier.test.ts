import { describe, expect, it } from 'vitest'
import { PROHIBITED_MEDIEVAL_CONTENT_CLASSES, classifyMedievalContent } from './content-safety'
import { createFrontierKnowledgeSource, createInitialFrontierState, commitAdjacentFrontierRegion, frontierRegionIdFor, materializeFrontierRegion, revealFrontierFacts, revealNamedFrontierPerson, validateFrontierState, type FrontierGenerationContext, type FrontierRevealedFact } from './frontier'
import { resolveWorldGenerationConfig } from './generation-config'
import { FRONTIER_LIMITS } from './frontier'
import { generateInitialWorld } from './initial-world'

const contextFor = (seed = 'frontier-ledger-29'): FrontierGenerationContext => {
  const resolution = resolveWorldGenerationConfig({ preset: 'watershed' })
  if (resolution.status !== 'valid') throw new Error('fixture configuration must resolve')
  return {
    seed,
    configuration: resolution.configuration,
    initialWorld: generateInitialWorld(seed, resolution.configuration).world
  }
}

const knownCoast = (state: ReturnType<typeof createInitialFrontierState>) => {
  const region = state.regions.find(candidate => candidate.status === 'known-but-unvisited')
  if (!region || region.status !== 'known-but-unvisited') throw new Error('initial frontier should expose a chart-known coastward region')
  return region
}

describe('medieval expanding-frontier contracts', () => {
  it('gives rooted coordinates and stable identities to the same seed, resolved configuration, and initial world', () => {
    const context = contextFor()
    const first = createInitialFrontierState(context)
    const second = createInitialFrontierState(context)

    expect(second).toEqual(first)
    expect(first.regions.map(region => region.commitment.coordinate)).toEqual([{ x: -1, y: 0 }, { x: 1, y: 0 }, { x: 2, y: 0 }])
    expect(first.regions.map(region => region.commitment.id)).toEqual(first.regions.map(region => frontierRegionIdFor(context.initialWorld, region.commitment.coordinate)))
    expect(first.regions.map(region => region.commitment.generationOrder)).toEqual([0, 1, 2])
    expect(first.regions[2]!.commitment.connection).toMatchObject({ kind: 'parent-region-link', parentRegionId: first.regions[1]!.commitment.id })
    expect(first.regions[0]!.status).toBe('ungenerated')
    expect(knownCoast(first).revealedFacts).toHaveLength(1)
    expect(validateFrontierState(context, first)).toEqual([])
  })

  it('roots every commitment in real watershed, waterway, settlement, route, trade, climate, ecology, and history records', () => {
    const context = contextFor('causal-frontier')
    const state = createInitialFrontierState(context)
    const initialIds = new Set([
      context.initialWorld.watershed.id,
      context.initialWorld.climate.id,
      ...context.initialWorld.waterways.map(record => record.id),
      ...context.initialWorld.settlements.map(record => record.id),
      ...context.initialWorld.routes.map(record => record.id),
      ...context.initialWorld.tradeLinks.map(record => record.id),
      ...context.initialWorld.ecologies.map(record => record.id),
      ...context.initialWorld.history.map(record => record.id)
    ])

    for (const region of state.regions) {
      const anchor = region.commitment.anchor
      expect(anchor.initialWorldId).toBe(context.initialWorld.id)
      expect([anchor.watershedId, anchor.waterwayId, anchor.settlementId, anchor.routeId, anchor.tradeLinkId, anchor.climateId, anchor.ecologyId, anchor.historyEventId].every(id => initialIds.has(id))).toBe(true)
    }

    const coast = knownCoast(state)
    const expanded = commitAdjacentFrontierRegion(context, state, coast.commitment.id, 'branch-north')
    const child = expanded.regions.at(-1)!
    expect(child.commitment.connection).toMatchObject({ kind: 'parent-region-link', parentRegionId: coast.commitment.id })
    expect(child.commitment.anchor).toEqual(coast.commitment.anchor)
    expect(validateFrontierState(context, expanded)).toEqual([])
  })

  it('preserves source-labelled pre-arrival facts without time advancement and materializes a stable static plan later', () => {
    const context = contextFor('known-frontier')
    const initial = createInitialFrontierState(context)
    const coast = knownCoast(initial)
    const siteFact: FrontierRevealedFact = {
      id: `${coast.commitment.id}:fact:site-name`,
      regionId: coast.commitment.id,
      subjectKind: 'site',
      subjectId: `${coast.commitment.id}:site:0`,
      kind: 'site-name',
      value: 'Candle Landing',
      source: createFrontierKnowledgeSource({
        kind: 'traveller',
        sourceRecordId: coast.commitment.anchor.routeId,
        reportedAtWorldTime: 3,
        freshnessAtWorldTime: 4
      }),
      knownAtWorldTime: 5,
      contentSafety: classifyMedievalContent('rumour', ['navigation', 'settlement', 'travel'], 'not-applicable', ['player-facing-text'])
    }
    const known = revealFrontierFacts(context, initial, [siteFact])

    expect(initial).not.toBe(known)
    expect(knownCoast(initial).revealedFacts).toHaveLength(1)
    expect(knownCoast(known).revealedFacts).toEqual(expect.arrayContaining([siteFact]))
    const first = materializeFrontierRegion(context, known, coast.commitment.id, 6)
    const second = materializeFrontierRegion(context, known, coast.commitment.id, 6)
    const later = materializeFrontierRegion(context, known, coast.commitment.id, 20)
    const materialized = first.regions.find(region => region.commitment.id === coast.commitment.id)
    if (!materialized || materialized.status !== 'materialized') throw new Error('known region should materialize into a static plan')

    expect(first).toEqual(second)
    expect(materialized.materialization.site.name).toBe('Candle Landing')
    const laterRegion = later.regions.find(region => region.commitment.id === coast.commitment.id)
    if (!laterRegion || laterRegion.status !== 'materialized') throw new Error('later plan should materialize')
    expect(laterRegion.materialization.site).toEqual(materialized.materialization.site)
    expect(validateFrontierState(context, first)).toEqual([])
  })

  it('commits a revealed name to one stable future-person record and materialization trigger without allocating a mutable person', () => {
    const context = contextFor('named-frontier')
    const state = createInitialFrontierState(context)
    const coast = knownCoast(state)
    const source = {
      kind: 'letter' as const,
      sourceRecordId: coast.commitment.anchor.historyEventId,
      reportedAtWorldTime: 2,
      freshnessAtWorldTime: 3
    }
    const named = revealNamedFrontierPerson(context, state, coast.commitment.id, 'letter:barge-keeper', source)
    const repeated = revealNamedFrontierPerson(context, named, coast.commitment.id, 'letter:barge-keeper', source)
    const namedRegion = knownCoast(named)
    const person = namedRegion.namedPeople[0]!
    const materialized = materializeFrontierRegion(context, named, coast.commitment.id, 4)
    const materializedRegion = materialized.regions.find(region => region.commitment.id === coast.commitment.id)
    if (!materializedRegion || materializedRegion.status !== 'materialized') throw new Error('named region should materialize')

    expect(repeated).toEqual(named)
    expect(person.futurePersonId).toMatch(/^person:frontier:/)
    expect(person.instantiationTrigger).toBe('region-materialized')
    expect(namedRegion).not.toHaveProperty('mutablePeople')
    expect(materializedRegion.materialization.namedPersonPlans).toEqual([expect.objectContaining({ commitmentId: person.id, futurePersonId: person.futurePersonId, name: person.name, trigger: 'region-materialized' })])
  })

  it('rejects contradictory regional claims before they can replace an earlier fact', () => {
    const context = contextFor('contradiction-frontier')
    const state = createInitialFrontierState(context)
    const coast = knownCoast(state)
    const firstFact = coast.revealedFacts[0]!
    const contradiction = { ...firstFact, id: `${firstFact.id}:contradiction`, value: 'Different Coast' }
    const forged = structuredClone(state)
    const forgedCoast = knownCoast(forged)
    forgedCoast.revealedFacts = [...forgedCoast.revealedFacts, contradiction]

    expect(validateFrontierState(context, forged)).toContainEqual({ recordId: contradiction.id, code: 'frontier.contradictory-fact' })
    expect(() => revealFrontierFacts(context, state, [contradiction])).toThrow('frontier contract rejected')
    expect(knownCoast(state).revealedFacts).toEqual([firstFact])
  })

  it('fails closed for unsafe and unclassified future knowledge', () => {
    const context = contextFor('safe-frontier')
    const state = createInitialFrontierState(context)

    for (const prohibited of PROHIBITED_MEDIEVAL_CONTENT_CLASSES) {
      const unsafe = structuredClone(state)
      const fact = knownCoast(unsafe).revealedFacts[0]!
      ;(fact.contentSafety.exclusions as unknown as Record<string, string>)[prohibited] = 'present'
      expect(validateFrontierState(context, unsafe)).toContainEqual({ recordId: fact.id, code: `content-safety.prohibited.${prohibited}` })
    }

    const unclassified = structuredClone(state)
    const fact = knownCoast(unclassified).revealedFacts[0]!
    delete (fact.source as unknown as { contentSafety?: unknown }).contentSafety
    expect(validateFrontierState(context, unclassified)).toContainEqual({ recordId: `${fact.id}:source`, code: 'content-safety.missing-classification' })
  })

  it('keeps anonymous commitments and named pre-arrival identities within fixed bounds', () => {
    const context = contextFor('bounded-frontier')
    const state = createInitialFrontierState(context)
    const coast = knownCoast(state)
    expect(state.regions).toHaveLength(3)
    expect(state.regions.every(region => region.commitment.anonymousRoles.length <= FRONTIER_LIMITS.anonymousRolesPerRegion && region.commitment.anonymousInstitutions.length <= FRONTIER_LIMITS.anonymousInstitutionsPerRegion)).toBe(true)
    expect(state.regions.filter(region => region.status === 'ungenerated').every(region => !('revealedFacts' in region) && !('namedPeople' in region))).toBe(true)

    const source = { kind: 'trader' as const, sourceRecordId: coast.commitment.anchor.tradeLinkId, reportedAtWorldTime: 0, freshnessAtWorldTime: 0 }
    let named = state
    for (const key of ['one', 'two', 'three']) named = revealNamedFrontierPerson(context, named, coast.commitment.id, key, source)
    expect(knownCoast(named).namedPeople).toHaveLength(FRONTIER_LIMITS.namedPeoplePerRegion)
    expect(() => revealNamedFrontierPerson(context, named, coast.commitment.id, 'four', source)).toThrow('budget-exceeded')

    const oversized = structuredClone(state)
    oversized.regions = Array.from({ length: FRONTIER_LIMITS.regions + 1 }, () => structuredClone(state.regions[0]!))
    expect(validateFrontierState(context, oversized)).toContainEqual({ recordId: 'frontier:state', code: 'frontier.budget-exceeded' })
  })
})
