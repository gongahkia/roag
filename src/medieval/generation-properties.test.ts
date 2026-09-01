import fc from 'fast-check'
import { describe, expect, it } from 'vitest'
import { PROHIBITED_MEDIEVAL_CONTENT_CLASSES, auditMedievalContentSafety } from './content-safety'
import { FRONTIER_LIMITS, commitAdjacentFrontierRegion, createInitialFrontierState, frontierContentRecords, frontierRegionIdFor, materializeFrontierRegion, revealFrontierFacts, validateFrontierState, type FrontierGenerationContext } from './frontier'
import { generationRetryPlan, resolveWorldGenerationConfig, type GenerationScale, type RegionSize, type WorldGenerationConfig, type WorldGenerationConfigRequest, type WorldGenerationPreset } from './generation-config'
import { INITIAL_WORLD_LIMITS, generateInitialWorld, initialWorldContentRecords, selectInitialWorldCandidate, validateInitialWorldCandidate, type InitialWorld } from './initial-world'
import { createFoundationWorld, foundationWorldContentSatisfiesSafetyPolicy, isReproducibleWorldManifest, recreateFoundationWorld } from './world'

const PROPERTY_RUNS = {
  deterministicWorlds: 24,
  validConfigurations: 48,
  invalidConfigurations: 64,
  contentSafety: 24,
  frontier: 32,
  diagnostics: 20
} as const

const PROPERTY_SEEDS = {
  deterministicWorlds: 20260901,
  validConfigurations: 20260902,
  invalidConfigurations: 20260903,
  contentSafety: 20260904,
  frontier: 20260905,
  diagnostics: 20260906
} as const

const arbitrarySeed = fc.array(fc.constantFrom(...'abcdefghijklmnopqrstuvwxyz0123456789-'), { minLength: 4, maxLength: 18 })
  .map(characters => `property-${characters.join('')}`)
const arbitraryScale = fc.constantFrom<GenerationScale>(1, 2, 3, 4, 5)
const arbitraryRegionSize = fc.constantFrom<RegionSize>('compact', 'standard', 'broad')
const arbitraryPreset = fc.constantFrom<WorldGenerationPreset>('sheltered-reach', 'watershed', 'far-coast')

/** Generates only input that the production resolver must accept, including both cross-field constraints. */
const arbitraryValidRequest: fc.Arbitrary<WorldGenerationConfigRequest> = fc.record({
  preset: arbitraryPreset,
  regionSize: arbitraryRegionSize,
  historyStep: fc.integer({ min: 4, max: 24 }),
  climate: fc.constantFrom<'cool-wet' | 'temperate' | 'warm-dry'>('cool-wet', 'temperate', 'warm-dry'),
  terrainRuggedness: arbitraryScale,
  waterwayDensity: arbitraryScale,
  settlementDensity: arbitraryScale,
  populationDensity: arbitraryScale,
  politicalFragmentation: arbitraryScale,
  resourceScarcity: arbitraryScale,
  ecologyComplexity: arbitraryScale,
  dangerPressure: arbitraryScale,
  eraPace: fc.constantFrom<'measured' | 'brisk' | 'pressing'>('measured', 'brisk', 'pressing'),
  simulationFidelity: fc.constantFrom<'focused' | 'balanced' | 'deep'>('focused', 'balanced', 'deep')
}).map(values => ({
  preset: values.preset,
  advanced: {
    regionSize: values.regionSize,
    historyYears: values.regionSize === 'broad' ? Math.max(200, values.historyStep * 25) : values.historyStep * 25,
    climate: values.climate,
    terrainRuggedness: values.terrainRuggedness,
    waterwayDensity: values.waterwayDensity,
    settlementDensity: values.settlementDensity,
    populationDensity: Math.min(values.populationDensity, values.settlementDensity + 1) as GenerationScale,
    politicalFragmentation: values.politicalFragmentation,
    resourceScarcity: values.resourceScarcity,
    ecologyComplexity: values.ecologyComplexity,
    dangerPressure: values.dangerPressure,
    eraPace: values.eraPace,
    simulationFidelity: values.simulationFidelity
  }
}))

const resolvedConfiguration = (request: WorldGenerationConfigRequest): WorldGenerationConfig => {
  const result = resolveWorldGenerationConfig(request)
  if (result.status !== 'valid') throw new Error(`generated valid configuration was rejected: ${result.issues.map(issue => issue.code).join(', ')}`)
  return result.configuration
}

const contextFor = (seed: string, configuration: WorldGenerationConfig): FrontierGenerationContext => ({
  seed,
  configuration,
  initialWorld: generateInitialWorld(seed, configuration).world
})

const expectBoundedInitialWorld = (world: InitialWorld): void => {
  expect(world.waterways.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.waterways)
  expect(world.seasons.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.seasons)
  expect(world.resources.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.resources)
  expect(world.ecologies.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.ecologies)
  expect(world.settlements.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.settlements)
  expect(world.institutions.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.institutions)
  expect(world.people.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.people)
  expect(world.routes.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.routes)
  expect(world.tradeLinks.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.tradeLinks)
  expect(world.routeHazards.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.routeHazards)
  expect(world.history.length).toBeLessThanOrEqual(INITIAL_WORLD_LIMITS.historyEvents)
}

const expectInitialWorldReferences = (world: InitialWorld): void => {
  const waterways = new Set(world.waterways.map(record => record.id))
  const resources = new Map(world.resources.map(record => [record.id, record]))
  const ecologies = new Set(world.ecologies.map(record => record.id))
  const settlements = new Map(world.settlements.map(record => [record.id, record]))
  const institutions = new Map(world.institutions.map(record => [record.id, record]))
  const people = new Map(world.people.map(record => [record.id, record]))
  const routes = new Map(world.routes.map(record => [record.id, record]))
  const seasons = new Set(world.seasons.map(record => record.id))

  expect(world.waterways.every(record => record.watershedId === world.watershed.id && (record.kind === 'estuary' || waterways.has(record.downstreamWaterwayId!)))).toBe(true)
  expect(world.seasons.every(record => record.climateId === world.climate.id)).toBe(true)
  expect(world.resources.every(record => waterways.has(record.waterwayId))).toBe(true)
  expect(world.ecologies.every(record => record.climateId === world.climate.id && record.resourceIds.every(id => resources.has(id)))).toBe(true)
  expect(world.settlements.every(record => waterways.has(record.waterwayId) && ecologies.has(record.ecologyId) && record.resourceIds.some(id => resources.get(id)?.waterwayId === record.waterwayId))).toBe(true)
  expect(world.institutions.every(record => settlements.has(record.settlementId))).toBe(true)
  expect(world.people.every(record => institutions.get(record.institutionId)?.settlementId === record.settlementId)).toBe(true)
  expect(world.routes.every(record => {
    const origin = settlements.get(record.originSettlementId)
    const destination = settlements.get(record.destinationSettlementId)
    return origin !== undefined && destination !== undefined && record.waterwayIds.includes(origin.waterwayId) && record.waterwayIds.includes(destination.waterwayId)
  })).toBe(true)
  expect(world.tradeLinks.every(record => routes.has(record.routeId) && settlements.get(record.originSettlementId)?.resourceIds.includes(record.resourceId) && resources.has(record.resourceId))).toBe(true)
  expect(world.routeHazards.every(record => routes.has(record.routeId) && seasons.has(record.seasonId))).toBe(true)
  expect(world.history.every(record => people.get(record.personId)?.institutionId === record.institutionId && people.get(record.personId)?.settlementId === record.settlementId && routes.has(record.routeId))).toBe(true)
}

const invalidHistoryYears = fc.integer({ min: -100, max: 700 }).filter(value => value < 100 || value > 600 || value % 25 !== 0)
const invalidRequests: fc.Arbitrary<unknown> = fc.oneof(
  fc.constant(null),
  fc.boolean(),
  fc.string(),
  invalidHistoryYears.map(historyYears => ({ preset: 'watershed', advanced: { historyYears } })),
  fc.integer({ min: 1, max: 3 }).map(settlementDensity => ({ preset: 'watershed', advanced: { settlementDensity, populationDensity: 5 } })),
  fc.integer({ min: 0, max: 999 }).map(value => ({ preset: 'watershed', advanced: { [`unknown-setting-${value}`]: value } }))
)

describe('medieval generation properties', () => {
  it('reproduces worlds, manifests, and initial frontier state for 24 seeded valid cases', () => {
    fc.assert(fc.property(arbitrarySeed, arbitraryValidRequest, (seed, request) => {
      const first = createFoundationWorld({ seed, configuration: request })
      const second = createFoundationWorld({ seed, configuration: request })
      const firstFrontier = createInitialFrontierState({ seed: first.manifest.creation.seed, configuration: first.manifest.creation.resolvedConfiguration, initialWorld: first.initialWorld })
      const secondFrontier = createInitialFrontierState({ seed: second.manifest.creation.seed, configuration: second.manifest.creation.resolvedConfiguration, initialWorld: second.initialWorld })

      expect(second).toEqual(first)
      expect(recreateFoundationWorld(first.manifest)).toEqual(first)
      expect(isReproducibleWorldManifest(first.manifest)).toBe(true)
      expect(secondFrontier).toEqual(firstFrontier)
    }), { seed: PROPERTY_SEEDS.deterministicWorlds, numRuns: PROPERTY_RUNS.deterministicWorlds, verbose: 2 })
  })

  it('keeps 48 valid resolved configurations bounded and causally connected', () => {
    fc.assert(fc.property(arbitrarySeed, arbitraryValidRequest, (seed, request) => {
      const configuration = resolvedConfiguration(request)
      const generated = generateInitialWorld(seed, configuration)

      expect(validateInitialWorldCandidate(generated.world, configuration)).toEqual([])
      expectBoundedInitialWorld(generated.world)
      expectInitialWorldReferences(generated.world)
      expect(generated.diagnostics.candidates.length).toBeGreaterThan(0)
      expect(generated.diagnostics.candidates.length).toBeLessThanOrEqual(4)
      expect(generated.diagnostics.candidates.map(candidate => candidate.attempt)).toEqual(generated.diagnostics.candidates.map((_, index) => index))
    }), { seed: PROPERTY_SEEDS.validConfigurations, numRuns: PROPERTY_RUNS.validConfigurations, verbose: 2 })
  })

  it('checks every preset and two representative advanced extremes against fixed output bounds', () => {
    const requests: readonly WorldGenerationConfigRequest[] = [
      ...(['sheltered-reach', 'watershed', 'far-coast'] as const).map(preset => ({ preset })),
      { preset: 'sheltered-reach', advanced: { regionSize: 'compact', historyYears: 100, climate: 'warm-dry', terrainRuggedness: 1, waterwayDensity: 1, settlementDensity: 1, populationDensity: 1, politicalFragmentation: 1, resourceScarcity: 5, ecologyComplexity: 1, dangerPressure: 1, eraPace: 'measured', simulationFidelity: 'focused' } },
      { preset: 'far-coast', advanced: { regionSize: 'broad', historyYears: 600, climate: 'cool-wet', terrainRuggedness: 5, waterwayDensity: 5, settlementDensity: 4, populationDensity: 5, politicalFragmentation: 5, resourceScarcity: 1, ecologyComplexity: 5, dangerPressure: 5, eraPace: 'pressing', simulationFidelity: 'deep' } }
    ]
    for (const [index, request] of requests.entries()) {
      const configuration = resolvedConfiguration(request)
      const generated = generateInitialWorld(`preset-bound-${index}`, configuration).world
      expectBoundedInitialWorld(generated)
      expectInitialWorldReferences(generated)
    }
  })

  it('fails closed with stable, short diagnostics for 64 malformed or incompatible configuration cases', () => {
    fc.assert(fc.property(invalidRequests, request => {
      const first = resolveWorldGenerationConfig(request)
      const second = resolveWorldGenerationConfig(request)

      expect(first).toEqual(second)
      expect(first.status).toBe('invalid')
      if (first.status === 'valid') throw new Error('invalid request unexpectedly resolved')
      expect(first.issues.length).toBeGreaterThan(0)
      expect(first.issues.length).toBeLessThanOrEqual(3)
      expect(first.issues.every(issue => issue.field.length > 0 && issue.code.length > 0 && issue.message.length > 0)).toBe(true)
    }), { seed: PROPERTY_SEEDS.invalidConfigurations, numRuns: PROPERTY_RUNS.invalidConfigurations, verbose: 2 })
  })

  it('accepts generated safety audits and rejects injected prohibited or unclassified data in 24 cases', () => {
    fc.assert(fc.property(arbitrarySeed, arbitraryValidRequest, fc.constantFrom(...PROHIBITED_MEDIEVAL_CONTENT_CLASSES), (seed, request, prohibited) => {
      const configuration = resolvedConfiguration(request)
      const world = createFoundationWorld({ seed, configuration: request })
      const frontier = createInitialFrontierState({ seed: world.manifest.creation.seed, configuration, initialWorld: world.initialWorld })
      const unsafe = structuredClone(world.initialWorld)
      ;(unsafe.history[0]!.contentSafety.exclusions as unknown as Record<string, string>)[prohibited] = 'present'
      const unclassified = structuredClone(world.initialWorld)
      delete (unclassified.routeHazards[0] as unknown as { contentSafety?: unknown }).contentSafety

      expect(foundationWorldContentSatisfiesSafetyPolicy(world)).toBe(true)
      expect(auditMedievalContentSafety([...initialWorldContentRecords(world.initialWorld), ...frontierContentRecords(frontier)])).toMatchObject({ status: 'accepted', diagnostics: [] })
      expect(validateInitialWorldCandidate(unsafe, configuration)).toContainEqual({ recordId: unsafe.history[0]!.id, code: `content-safety.prohibited.${prohibited}` })
      expect(auditMedievalContentSafety(initialWorldContentRecords(unclassified))).toMatchObject({ status: 'rejected', diagnostics: [{ contentId: unclassified.routeHazards[0]!.id, code: 'content-safety.missing-classification' }] })
    }), { seed: PROPERTY_SEEDS.contentSafety, numRuns: PROPERTY_RUNS.contentSafety, verbose: 2 })
  })

  it('keeps 32 frontier cases coordinate-stable, anchored, idempotent, materializable, and contradiction-safe', () => {
    fc.assert(fc.property(arbitrarySeed, arbitraryValidRequest, fc.integer({ min: 0, max: 120 }), (seed, request, materializedAtWorldTime) => {
      const configuration = resolvedConfiguration(request)
      const context = contextFor(seed, configuration)
      const state = createInitialFrontierState(context)
      const repeated = createInitialFrontierState(context)
      const coast = state.regions.find(region => region.status === 'known-but-unvisited')
      if (!coast || coast.status !== 'known-but-unvisited') throw new Error('initial frontier must provide one known coastward region')
      const initialIds = new Set([context.initialWorld.watershed.id, context.initialWorld.climate.id, ...context.initialWorld.waterways.map(record => record.id), ...context.initialWorld.settlements.map(record => record.id), ...context.initialWorld.routes.map(record => record.id), ...context.initialWorld.tradeLinks.map(record => record.id), ...context.initialWorld.ecologies.map(record => record.id), ...context.initialWorld.history.map(record => record.id)])
      const materialized = materializeFrontierRegion(context, state, coast.commitment.id, materializedAtWorldTime)
      const expanded = commitAdjacentFrontierRegion(context, state, coast.commitment.id, 'branch-north')
      const contradiction = { ...coast.revealedFacts[0]!, value: `${coast.revealedFacts[0]!.value} changed` }

      expect(repeated).toEqual(state)
      expect(validateFrontierState(context, state)).toEqual([])
      expect(state.regions.length).toBeLessThanOrEqual(FRONTIER_LIMITS.regions)
      expect(state.regions.every(region => region.commitment.id === frontierRegionIdFor(context.initialWorld, region.commitment.coordinate))).toBe(true)
      expect(state.regions.every(region => [region.commitment.anchor.watershedId, region.commitment.anchor.waterwayId, region.commitment.anchor.settlementId, region.commitment.anchor.routeId, region.commitment.anchor.tradeLinkId, region.commitment.anchor.climateId, region.commitment.anchor.ecologyId, region.commitment.anchor.historyEventId].every(id => initialIds.has(id)))).toBe(true)
      expect(revealFrontierFacts(context, state, [coast.revealedFacts[0]!])).toEqual(state)
      expect(() => revealFrontierFacts(context, state, [contradiction])).toThrow('contradictory-fact')
      expect(materializeFrontierRegion(context, state, coast.commitment.id, materializedAtWorldTime)).toEqual(materialized)
      expect(validateFrontierState(context, materialized)).toEqual([])
      expect(validateFrontierState(context, expanded)).toEqual([])
      expect(expanded.regions.at(-1)!.commitment.anchor).toEqual(coast.commitment.anchor)
    }), { seed: PROPERTY_SEEDS.frontier, numRuns: PROPERTY_RUNS.frontier, verbose: 2 })
  })

  it('keeps candidate exhaustion deterministic, bounded, and diagnostic-readable in 20 cases', () => {
    fc.assert(fc.property(arbitrarySeed, arbitraryValidRequest, (seed, request) => {
      const configuration = resolvedConfiguration(request)
      const source = generateInitialWorld(seed, configuration).world
      const plan = generationRetryPlan(seed, configuration)
      const exhaust = () => selectInitialWorldCandidate(
        plan,
        attempt => {
          const candidate = structuredClone(source)
          candidate.candidateAttempt = attempt.attempt
          candidate.resources[0]!.waterwayId = 'initial:waterway:missing'
          return candidate
        },
        candidate => validateInitialWorldCandidate(candidate, configuration)
      )
      const first = exhaust()
      const second = exhaust()

      expect(first).toEqual(second)
      expect(first.status).toBe('exhausted')
      if (first.status !== 'exhausted') throw new Error('deliberately invalid candidates must exhaust')
      expect(first.diagnostics.candidates).toHaveLength(4)
      expect(first.diagnostics.candidates.map(candidate => candidate.attempt)).toEqual([0, 1, 2, 3])
      expect(first.diagnostics.candidates.map(candidate => candidate.streamSeed)).toEqual(plan.map(candidate => candidate.streamSeed))
      expect(first.diagnostics.candidates.every(candidate =>
        candidate.status === 'rejected' &&
        candidate.issues.length > 0 &&
        candidate.issues.length <= INITIAL_WORLD_LIMITS.settlements + 1 &&
        candidate.issues.some(issue => issue.code === 'initial-world.invalid-resource' && issue.recordId.startsWith('initial:resource:')) &&
        candidate.issues.every(issue => issue.recordId.startsWith('initial:') && issue.code.startsWith('initial-world.'))
      )).toBe(true)
    }), { seed: PROPERTY_SEEDS.diagnostics, numRuns: PROPERTY_RUNS.diagnostics, verbose: 2 })
  })
})
