import { describe, expect, it } from 'vitest'
import { MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, PROHIBITED_MEDIEVAL_CONTENT_CLASSES } from './content-safety'
import { generationRetryPlan, resolveWorldGenerationConfig } from './generation-config'
import { INITIAL_WORLD_GENERATION_STAGES, INITIAL_WORLD_GENERATOR_VERSION, INITIAL_WORLD_LIMITS, generateInitialWorld, selectInitialWorldCandidate, validateInitialWorldCandidate } from './initial-world'

const configurationFor = (request: Parameters<typeof resolveWorldGenerationConfig>[0] = {}) => {
  const resolution = resolveWorldGenerationConfig(request)
  if (resolution.status !== 'valid') throw new Error('test configuration must resolve')
  return resolution.configuration
}

describe('medieval initial-world generation', () => {
  it('reproduces the ordered initial-world graph and its policy-versioned diagnostics from seed and resolved configuration', () => {
    const configuration = configurationFor({ preset: 'far-coast', advanced: { climate: 'temperate', historyYears: 350 } })
    const first = generateInitialWorld('reed-ledger-47', configuration)
    const second = generateInitialWorld('reed-ledger-47', configuration)

    expect(second).toEqual(first)
    expect(first.diagnostics).toEqual({
      version: 1,
      generatorVersion: INITIAL_WORLD_GENERATOR_VERSION,
      contentSafetyPolicyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      selectedAttempt: 0,
      candidates: [{
        attempt: 0,
        streamSeed: generationRetryPlan('reed-ledger-47', configuration)[0]!.streamSeed,
        status: 'accepted',
        issues: []
      }]
    })
    expect(validateInitialWorldCandidate(first.world, configuration)).toEqual([])
  })

  it('reports only actual deterministic candidate stages without changing the generated world', () => {
    const configuration = configurationFor()
    const trace: { stage: string; status: string; candidateAttempt: number; completedStages: number; totalStages: number; streamSeed: string }[] = []
    const generated = generateInitialWorld('progress-basin', configuration, progress => trace.push(progress))

    expect(trace.map(({ streamSeed: _streamSeed, ...progress }) => progress)).toEqual([
      ...INITIAL_WORLD_GENERATION_STAGES.slice(0, -1).flatMap((stage, index) => [
        { stage, status: 'started', candidateAttempt: 0, completedStages: index, totalStages: INITIAL_WORLD_GENERATION_STAGES.length },
        { stage, status: 'completed', candidateAttempt: 0, completedStages: index + 1, totalStages: INITIAL_WORLD_GENERATION_STAGES.length }
      ]),
      { stage: 'validation', status: 'accepted', candidateAttempt: 0, completedStages: INITIAL_WORLD_GENERATION_STAGES.length, totalStages: INITIAL_WORLD_GENERATION_STAGES.length }
    ])
    expect(trace.map(progress => progress.streamSeed)).toEqual(Array(trace.length).fill(generationRetryPlan('progress-basin', configuration)[0]!.streamSeed))
    expect(generateInitialWorld('progress-basin', configuration)).toEqual(generated)
  })

  it('keeps every downstream record attached to a viable upstream place, resource, institution, route, and actor', () => {
    const configuration = configurationFor()
    const world = generateInitialWorld('causal-basin', configuration).world
    const settlements = new Map(world.settlements.map(record => [record.id, record]))
    const institutions = new Map(world.institutions.map(record => [record.id, record]))
    const people = new Map(world.people.map(record => [record.id, record]))
    const routes = new Map(world.routes.map(record => [record.id, record]))
    const waterways = new Set(world.waterways.map(record => record.id))
    const resources = new Map(world.resources.map(record => [record.id, record]))

    expect(world.climate.watershedId).toBe(world.watershed.id)
    expect(world.seasons.every(season => season.climateId === world.climate.id)).toBe(true)
    expect(world.settlements.every(settlement => settlement.resourceIds.some(id => resources.get(id)?.waterwayId === settlement.waterwayId))).toBe(true)
    expect(world.settlements.every(settlement => waterways.has(settlement.waterwayId))).toBe(true)
    expect(world.institutions.every(institution => settlements.has(institution.settlementId))).toBe(true)
    expect(world.people.every(person => institutions.get(person.institutionId)?.settlementId === person.settlementId)).toBe(true)
    expect(world.routes.every(route => {
      const origin = settlements.get(route.originSettlementId)
      const destination = settlements.get(route.destinationSettlementId)
      return origin !== undefined && destination !== undefined && route.waterwayIds.includes(origin.waterwayId) && route.waterwayIds.includes(destination.waterwayId)
    })).toBe(true)
    expect(world.tradeLinks.every(link => {
      const route = routes.get(link.routeId)
      const origin = settlements.get(link.originSettlementId)
      return route !== undefined && origin?.resourceIds.includes(link.resourceId) === true && route.originSettlementId === link.originSettlementId && route.destinationSettlementId === link.destinationSettlementId
    })).toBe(true)
    expect(world.history.every(event => {
      const person = people.get(event.personId)
      const route = routes.get(event.routeId)
      return person?.institutionId === event.institutionId && person.settlementId === event.settlementId && (route?.originSettlementId === event.settlementId || route?.destinationSettlementId === event.settlementId)
    })).toBe(true)
  })

  it('makes every resolved generation control causally visible in the appropriate initial-world output', () => {
    const baseline = configurationFor()
    const altered = configurationFor({
      advanced: {
        regionSize: 'broad',
        historyYears: 400,
        climate: 'warm-dry',
        terrainRuggedness: 5,
        waterwayDensity: 5,
        settlementDensity: 4,
        populationDensity: 5,
        politicalFragmentation: 5,
        resourceScarcity: 5,
        ecologyComplexity: 5,
        dangerPressure: 5,
        eraPace: 'pressing',
        simulationFidelity: 'deep'
      }
    })
    const first = generateInitialWorld('controls-basin', baseline).world
    const second = generateInitialWorld('controls-basin', altered).world

    expect(second.watershed.spanLeagues).not.toBe(first.watershed.spanLeagues)
    expect(second.historyHorizonYears).toBe(400)
    expect(second.climate.profile).toBe('warm-dry')
    expect(second.watershed.reliefLevel).toBe(5)
    expect(second.waterways).toHaveLength(7)
    expect(second.settlements).toHaveLength(5)
    expect(second.settlements.every(record => record.populationBand === 5)).toBe(true)
    expect(second.institutions).toHaveLength(25)
    expect(second.resources).toHaveLength(3)
    expect(second.resources.every(record => record.availabilityLevel === 1)).toBe(true)
    expect(second.ecologies).toHaveLength(5)
    expect(second.routeHazards).toHaveLength(5)
    expect(second.routeHazards.every(record => record.pressureLevel === 5)).toBe(true)
    expect(second.climate.seasonPace).toBe('pressing')
    expect(second.generationDetail).toBe('deep')
    expect(second.people.every(record => record.detailLevel === 'deep')).toBe(true)
  })

  it('rejects invalid candidates in retry order, selects the first valid candidate, and reports bounded exhaustion', () => {
    const configuration = configurationFor()
    const plan = generationRetryPlan('retry-basin', configuration)
    const valid = generateInitialWorld('retry-basin', configuration).world
    const selected = selectInitialWorldCandidate(
      plan,
      attempt => {
        const candidate = structuredClone(valid)
        candidate.candidateAttempt = attempt.attempt
        if (attempt.attempt === 0) candidate.routes[0]!.originSettlementId = 'initial:settlement:missing'
        return candidate
      },
      candidate => validateInitialWorldCandidate(candidate, configuration)
    )

    expect(selected.status).toBe('selected')
    if (selected.status !== 'selected') throw new Error('candidate one should be selected')
    expect(selected.diagnostics.selectedAttempt).toBe(1)
    expect(selected.diagnostics.candidates).toEqual([
      expect.objectContaining({ attempt: 0, status: 'rejected', issues: expect.arrayContaining([expect.objectContaining({ code: 'initial-world.invalid-route' })]) }),
      expect.objectContaining({ attempt: 1, status: 'accepted', issues: [] })
    ])

    const exhausted = selectInitialWorldCandidate(
      plan,
      attempt => {
        const candidate = structuredClone(valid)
        candidate.candidateAttempt = attempt.attempt
        candidate.resources[0]!.waterwayId = 'initial:waterway:missing'
        return candidate
      },
      candidate => validateInitialWorldCandidate(candidate, configuration)
    )
    expect(exhausted.status).toBe('exhausted')
    if (exhausted.status !== 'exhausted') throw new Error('all candidates should be rejected')
    expect(exhausted.diagnostics.candidates).toHaveLength(4)
    expect(exhausted.diagnostics.candidates.every(candidate => candidate.status === 'rejected')).toBe(true)
    expect(exhausted.diagnostics.candidates.map(candidate => candidate.streamSeed)).toEqual(plan.map(candidate => candidate.streamSeed))
  })

  it('fails closed for every prohibited safety class and for an unknown required classification', () => {
    const configuration = configurationFor()
    const generated = generateInitialWorld('policy-basin', configuration).world

    for (const prohibited of PROHIBITED_MEDIEVAL_CONTENT_CLASSES) {
      const unsafe = structuredClone(generated)
      ;(unsafe.history[0]!.contentSafety.exclusions as unknown as Record<string, string>)[prohibited] = 'present'
      expect(validateInitialWorldCandidate(unsafe, configuration)).toContainEqual({
        recordId: unsafe.history[0]!.id,
        code: `content-safety.prohibited.${prohibited}`
      })
    }

    const unclassified = structuredClone(generated)
    delete (unclassified.routeHazards[0] as unknown as { contentSafety?: unknown }).contentSafety
    expect(validateInitialWorldCandidate(unclassified, configuration)).toContainEqual({
      recordId: unclassified.routeHazards[0]!.id,
      code: 'content-safety.missing-classification'
    })
  })

  it('keeps every generated collection within its fixed foundation-world budget across all presets', () => {
    for (const preset of ['sheltered-reach', 'watershed', 'far-coast'] as const) {
      const world = generateInitialWorld(`bounds-${preset}`, configurationFor({ preset })).world
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
  })
})
