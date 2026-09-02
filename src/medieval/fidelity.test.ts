import { describe, expect, it } from 'vitest'
import { FIDELITY_BUDGETS, FIDELITY_PLANNING_CONTRACT_VERSION, FidelityPlanningContractError, createFidelityPlan, validateFidelityPlanningRequest, type FidelityPlanningRequest } from './fidelity'
import { chooseInitialCourier, createFoundationWorld } from './world'
import { createAutonomyState } from './autonomy'

const selectedWorld = (seed: string, preset: 'sheltered-reach' | 'watershed' | 'far-coast' = 'watershed') => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset } }), 'crew:0')
const requestFor = (world: ReturnType<typeof selectedWorld>, loadedLocations: FidelityPlanningRequest['loadedLocations'] = []): FidelityPlanningRequest => ({ world, activeCourierId: 'crew:0', loadedLocations })
const codes = (value: unknown): readonly string[] => validateFidelityPlanningRequest(value).map(diagnostic => diagnostic.code)

describe('deterministic medieval fidelity plans', () => {
  it('reproduces an identical plan from equivalent world and explicit context inputs', () => {
    const first = selectedWorld('fidelity repeat')
    const second = selectedWorld('fidelity repeat')
    const firstPlan = createFidelityPlan(requestFor(first))
    const secondPlan = createFidelityPlan(requestFor(second))

    expect(firstPlan).toEqual(secondPlan)
    expect(firstPlan).toMatchObject({
      version: FIDELITY_PLANNING_CONTRACT_VERSION,
      worldId: first.id,
      creationDigest: first.manifest.creation.digest,
      worldTime: 0,
      simulationFidelity: 'balanced',
      jomon: { vesselId: 'vessel:jomon', tier: 'loaded-place', cadence: { kind: 'time-window', intervalMinutes: 1, nextEligibleAtWorldTime: 1 } },
      activeCourier: { personId: 'crew:0', tier: 'loaded' }
    })
  })

  it('canonicalizes location-context order so insertion order cannot change a plan', () => {
    const world = selectedWorld('fidelity order', 'far-coast')
    const locations = world.state.sites.sites.slice(1).map(site => ({ kind: 'site' as const, id: site.id }))
    const forward = createFidelityPlan(requestFor(world, locations))
    const reverse = createFidelityPlan(requestFor(world, [...locations].reverse()))

    expect(reverse).toEqual(forward)
    expect(forward.loadedLocations).toEqual([
      { kind: 'jomon', id: 'vessel:jomon' },
      { kind: 'site', id: world.state.jomon.location.id },
      ...[...locations].sort((left, right) => left.id.localeCompare(right.id))
    ])
  })

  it('uses the documented focused, balanced, and deep budgets with stable overflow prioritization', () => {
    const fixtures = [
      ['focused', selectedWorld('fidelity focused', 'sheltered-reach')],
      ['balanced', selectedWorld('fidelity balanced', 'watershed')],
      ['deep', selectedWorld('fidelity deep', 'far-coast')]
    ] as const

    for (const [fidelity, world] of fixtures) {
      const plan = createFidelityPlan(requestFor(world))
      expect(plan.budget).toEqual(FIDELITY_BUDGETS[fidelity])
      expect(plan.individuals.filter(assignment => assignment.tier === 'loaded')).toHaveLength(Math.min(world.state.people.records.length, plan.budget.loadedPeople))
      expect(plan.individuals.find(assignment => assignment.personId === 'crew:0')).toMatchObject({ tier: 'loaded' })
      expect(plan.places.filter(assignment => assignment.tier === 'loaded-place').length).toBeLessThanOrEqual(plan.budget.loadedPlaces)
      expect(plan.places.filter(assignment => assignment.tier === 'distant-settlement-summary').length).toBeLessThanOrEqual(plan.budget.distantSettlementSummaries)
      expect(plan.institutions.filter(assignment => assignment.tier === 'distant-institution-summary').length).toBeLessThanOrEqual(plan.budget.distantInstitutionSummaries)
    }

    const overflow = createFidelityPlan(requestFor(fixtures[0][1]))
    expect(overflow.individuals.filter(assignment => assignment.tier === 'loaded')).toHaveLength(2)
    expect(overflow.individuals.filter(assignment => assignment.tier === 'nearby')).toHaveLength(1)
    expect(overflow.individuals.filter(assignment => assignment.tier === 'recurring')).toHaveLength(1)
    expect(overflow.individuals.filter(assignment => assignment.tier === 'distant-individual-summary')).toHaveLength(1)
    expect(overflow.individuals.filter(assignment => assignment.tier === 'deferred')).toHaveLength(1)
    expect(createFidelityPlan(requestFor(fixtures[0][1]))).toEqual(overflow)
  })

  it('preserves all full person records while assigning loaded, nearby, recurring, distant, and deferred processing tiers', () => {
    const world = selectedWorld('fidelity preservation', 'sheltered-reach')
    const before = structuredClone(world.state.people.records)
    const plan = createFidelityPlan(requestFor(world))

    expect(world.state.people.records).toEqual(before)
    expect(plan.individuals.map(assignment => assignment.personId)).toEqual(before.map(person => person.id))
    expect(new Set(plan.individuals.map(assignment => assignment.tier))).toEqual(new Set(['loaded', 'nearby', 'recurring', 'distant-individual-summary', 'deferred']))
    expect(plan.individuals.find(assignment => assignment.tier === 'nearby')?.cadence).toEqual({ kind: 'time-window', intervalMinutes: 5, nextEligibleAtWorldTime: 5 })
    expect(plan.individuals.find(assignment => assignment.tier === 'recurring')?.recurrenceReasons).toEqual(expect.arrayContaining(['relationship', 'memory']))
    expect(plan.individuals.find(assignment => assignment.tier === 'distant-individual-summary')?.cadence).toEqual({ kind: 'time-window', intervalMinutes: 120, nextEligibleAtWorldTime: 120 })
  })

  it('makes Jomon, the courier, loaded sites, and materialized-region context explicit and rejects invalid courier/location context', () => {
    const world = selectedWorld('fidelity context', 'far-coast')
    const extraSite = world.state.sites.sites.find(site => site.id !== world.state.jomon.location.id)!
    const valid = createFidelityPlan(requestFor(world, [{ kind: 'site', id: extraSite.id }]))
    const mismatch = { ...requestFor(world), activeCourierId: 'crew:1' }
    const missingLocation = structuredClone(world)
    missingLocation.state.people.records[0]!.location = { kind: 'site', id: extraSite.id }

    expect(valid.loadedLocations).toContainEqual({ kind: 'site', id: extraSite.id })
    expect(valid.places.find(place => place.siteId === extraSite.id)).toMatchObject({ tier: 'loaded-place' })
    expect(codes(mismatch)).toContain('fidelity.invalid-active-courier')
    expect(codes(requestFor(missingLocation))).toContain('fidelity.invalid-courier-location')
    expect(codes({ ...requestFor(world), loadedLocations: [{ kind: 'site', id: extraSite.id }, { kind: 'site', id: extraSite.id }] })).toContain('fidelity.duplicate-loaded-location')
    expect(codes({ ...requestFor(world), loadedLocations: [{ kind: 'frontier-region', id: 'frontier:not-materialized' }] })).toContain('fidelity.invalid-loaded-location')
  })

  it('keeps dead people as historical entries and never introduces initial seeds or frontier commitments as agents', () => {
    const world = selectedWorld('fidelity historical', 'sheltered-reach')
    const historical = structuredClone(world)
    historical.state.people.records[1]!.life = { status: 'dead', birth: historical.state.people.records[1]!.life.birth, death: { atWorldTime: 0 } }
    historical.state.people.records[1]!.work.availability = 'unavailable'
    historical.state.autonomy = createAutonomyState(historical.state.people.records, 0)
    const plan = createFidelityPlan(requestFor(historical))
    const seededIds = new Set(world.initialWorld.people.map(person => person.id))
    const commitmentIds = new Set(world.state.geography.frontier.regions.map(region => region.commitment.id))

    expect(plan.individuals.find(assignment => assignment.personId === historical.state.people.records[1]!.id)).toMatchObject({ tier: 'historical-only', cadence: { kind: 'not-scheduled' } })
    expect(plan.individuals.every(assignment => !seededIds.has(assignment.personId) && !commitmentIds.has(assignment.personId))).toBe(true)
    expect(plan.individuals.map(assignment => assignment.personId)).toEqual(world.state.people.records.map(person => person.id))
  })

  it('fails closed on malformed worlds, over-budget loaded places, and invalid context without mutating zero-time state', () => {
    const world = selectedWorld('fidelity invalid', 'sheltered-reach')
    const before = structuredClone(world)
    const allSites = world.state.sites.sites.map(site => ({ kind: 'site' as const, id: site.id }))
    const malformed = { activeCourierId: 'crew:0', loadedLocations: [], world: { version: 4 } }

    expect(codes(malformed)).toEqual(['fidelity.invalid-world'])
    expect(codes({ ...requestFor(world), loadedLocations: allSites })).toContain('fidelity.loaded-location-budget-exceeded')
    expect(() => createFidelityPlan({ ...requestFor(world), activeCourierId: 'crew:not-present' })).toThrow(FidelityPlanningContractError)
    expect(world).toEqual(before)
    expect(world.state.temporal).toMatchObject({ worldTime: 0, actionSequence: 0, pendingEvents: [], causalRecords: [] })
  })
})
