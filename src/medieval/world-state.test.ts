import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, foundationWorldTemporalStateMatches, recordDurableJomonGrowth } from './world'
import { isMedievalWorldState, MEDIEVAL_WORLD_STATE_LIMITS, MEDIEVAL_WORLD_STATE_VERSION, WORLD_CAUSAL_HISTORY_STATE_VERSION, WORLD_COURIER_STATE_VERSION, WORLD_ERA_STATE_VERSION, WORLD_GEOGRAPHY_STATE_VERSION, WORLD_INSTITUTIONS_STATE_VERSION, WORLD_JOMON_STATE_VERSION, WORLD_MARKETS_STATE_VERSION, WORLD_PEOPLE_STATE_VERSION, WORLD_ROUTES_STATE_VERSION, WORLD_SITES_STATE_VERSION, validateMedievalWorldState, type WorldStateValidationContext } from './world-state'

const safety = () => classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])

const contextFor = (world: ReturnType<typeof createFoundationWorld>): WorldStateValidationContext => ({
  seed: world.manifest.creation.seed,
  configuration: world.manifest.creation.resolvedConfiguration,
  initialWorld: world.initialWorld,
  jomon: world.jomon,
  crew: world.crew
})

const codes = (world: ReturnType<typeof createFoundationWorld>, state: unknown): readonly string[] => validateMedievalWorldState(contextFor(world), state).map(issue => issue.code)

describe('versioned medieval mutable world state', () => {
  it('constructs canonically ordered bounded registries from stable generated IDs and references', () => {
    const world = createFoundationWorld({ seed: 'state-ledger' })
    const state = world.state

    expect(state.version).toBe(MEDIEVAL_WORLD_STATE_VERSION)
    expect({ geography: state.geography.version, sites: state.sites.version, routes: state.routes.version, markets: state.markets.version, people: state.people.version, institutions: state.institutions.version, causalHistory: state.causalHistory.version, jomon: state.jomon.version, courier: state.courier.version, era: state.era.version }).toEqual({ geography: WORLD_GEOGRAPHY_STATE_VERSION, sites: WORLD_SITES_STATE_VERSION, routes: WORLD_ROUTES_STATE_VERSION, markets: WORLD_MARKETS_STATE_VERSION, people: WORLD_PEOPLE_STATE_VERSION, institutions: WORLD_INSTITUTIONS_STATE_VERSION, causalHistory: WORLD_CAUSAL_HISTORY_STATE_VERSION, jomon: WORLD_JOMON_STATE_VERSION, courier: WORLD_COURIER_STATE_VERSION, era: WORLD_ERA_STATE_VERSION })
    expect(validateMedievalWorldState(contextFor(world), state)).toEqual([])
    expect(state.sites.sites.map(site => site.id)).toEqual([...state.sites.sites.map(site => site.id)].sort())
    expect(state.routes.conditions.map(route => route.id)).toEqual([...state.routes.conditions.map(route => route.id)].sort())
    expect(state.markets.markets.map(market => market.siteId)).toEqual(state.sites.sites.map(site => site.id))
    expect(state.people.records.map(person => person.id)).toEqual([...state.people.records.map(person => person.id)].sort())
    expect(state.institutions.registry.map(institution => institution.siteId)).toEqual([...world.initialWorld.institutions].sort((left, right) => left.id.localeCompare(right.id)).map(institution => institution.settlementId))
    expect(state.geography.frontier.regions).toHaveLength(3)
    expect(state.sites.sites.length).toBeLessThanOrEqual(MEDIEVAL_WORLD_STATE_LIMITS.sites)
    expect(state.people.records.length).toBeLessThanOrEqual(MEDIEVAL_WORLD_STATE_LIMITS.people)
    expect(state.era).toMatchObject({ era: 'base', activePlayMinutes: 0, durableGrowthEvidence: [], transitions: [] })
  })

  it('keeps immutable creation evidence separate while selection and scheduler history persist in state', () => {
    const world = createFoundationWorld({ seed: 'state-separation' })
    const manifest = structuredClone(world.manifest)
    const selected = chooseInitialCourier(world, 'crew:0')
    const advanced = advanceFoundationWorldTime(selected, {
      id: 'wait:state-separation',
      kind: 'wait',
      durationMinutes: 1,
      contentSafety: safety()
    })

    expect(selected.manifest).toEqual(manifest)
    expect(advanced.manifest).toEqual(manifest)
    expect(advanced.state.courier.initialCourierId).toBe('crew:0')
    expect(advanced.state.temporal.worldTime).toBe(1)
    expect(advanced.state.causalHistory.tail.map(record => record.kind)).toEqual(['initial-courier-selected', 'time-bearing-action'])
    expect(foundationWorldTemporalStateMatches(advanced)).toBe(true)
  })

  it('fails closed on malformed versions, duplicated IDs, and cross-domain reference changes', () => {
    const world = createFoundationWorld({ seed: 'state-rejection' })
    const unknownVersion = structuredClone(world.state)
    unknownVersion.version = 99 as never
    const duplicatePerson = structuredClone(world.state)
    duplicatePerson.people.records = [...duplicatePerson.people.records, structuredClone(duplicatePerson.people.records[0]!)]
    const brokenRoute = structuredClone(world.state)
    brokenRoute.routes.conditions[0]!.originSiteId = 'site:not-present'
    const malformed = { ...structuredClone(world.state), extensionBag: {} }

    expect(codes(world, unknownVersion)).toContain('world-state.invalid-version')
    expect(codes(world, duplicatePerson)).toContain('world-state.duplicate-id')
    expect(isMedievalWorldState(contextFor(world), duplicatePerson)).toBe(false)
    expect(isMedievalWorldState(contextFor(world), brokenRoute)).toBe(false)
    expect(codes(world, malformed)).toEqual(['world-state.malformed-state'])
  })

  it('rejects a changed mutable content audit or a frontier root that no longer matches immutable provenance', () => {
    const world = createFoundationWorld({ seed: 'state-safety' })
    const changedAudit = structuredClone(world.state)
    changedAudit.contentSafetyAudit.policyVersion = 99 as never
    const changedRoot = structuredClone(world.state)
    changedRoot.geography.frontier.regions[0]!.commitment.anchor.routeId = 'initial:route:not-real'

    expect(codes(world, changedAudit)).toContain('world-state.invalid-content-audit')
    expect(isMedievalWorldState(contextFor(world), changedRoot)).toBe(false)
  })

  it('rejects a dead persistent person as the selected active courier', () => {
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'state-dead-courier' }), 'crew:0')
    const forged = structuredClone(selected)
    forged.state.people.records[0]!.life = { status: 'dead', birth: forged.state.people.records[0]!.life.birth, death: { atWorldTime: 0 } }
    forged.state.people.records[0]!.work.availability = 'unavailable'

    expect(codes(selected, forged.state)).toContain('world-state.invalid-courier')
    expect(isMedievalWorldState(contextFor(selected), forged.state)).toBe(false)
  })

  it('rejects forged era totals, transitions, tokens, model versions, and growth records', () => {
    const selected = chooseInitialCourier(createFoundationWorld({ seed: 'state-era-rejection' }), 'crew:0')
    const advanced = advanceFoundationWorldTime(selected, {
      id: 'travel:state-era-rejection',
      kind: 'travel',
      durationMinutes: 7_200,
      contentSafety: safety()
    })
    const grown = recordDurableJomonGrowth(advanced, {
      id: 'growth:state-era-rejection:refit',
      kind: 'workspace-refit',
      source: { kind: 'jomon-vessel', id: 'vessel:jomon' },
      atWorldTime: 7_200
    })
    const badTotal = structuredClone(grown.state)
    badTotal.era.progressUnits++
    const badToken = structuredClone(grown.state)
    badToken.era.transitions[0]!.token++
    const badModel = structuredClone(grown.state)
    badModel.era.modelVersion = 99 as never
    const badGrowth = structuredClone(grown.state)
    badGrowth.era.durableGrowthEvidence[0]!.source.id = 'prop:not-jomon'

    expect(codes(grown, badTotal)).toContain('world-era.invalid-progress-units')
    expect(codes(grown, badToken)).toContain('world-era.invalid-transition-token')
    expect(codes(grown, badModel)).toContain('world-era.invalid-model-version')
    expect(codes(grown, badGrowth)).toContain('world-era.invalid-growth-source')
  })
})
