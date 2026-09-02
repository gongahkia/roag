import { describe, expect, it } from 'vitest'
import { AUTONOMY_CHOICE_KINDS, AUTONOMY_FACTOR_CODES, AUTONOMY_LIMITS, AUTONOMY_NEED_KEYS, AUTONOMY_NEED_WINDOW_MINUTES, AUTONOMY_STATE_VERSION, autonomyProjection, reconcileAutonomyState, validateAutonomyPlanState, validateAutonomyState } from './autonomy'
import { classifyMedievalContent } from './content-safety'
import { DELEGATION_CONTRACT_VERSION, DELEGATION_TASK_DEFINITIONS, delegationTaskIdForOffer, type DelegationOfferInput } from './delegation'
import { createFidelityPlan } from './fidelity'
import { SeededRng } from './rng'
import { simulationCatchUpProjection } from './simulation-catchup'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, offerFoundationWorldDelegatedTask, replayFoundationWorldCausalHistory, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'
import { worldEraProjection } from './world-era'

const safety = () => classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const wait = (id: string, durationMinutes: number) => ({ id, kind: 'wait' as const, durationMinutes, contentSafety: safety() })
const travel = (id: string, durationMinutes: number) => ({ id, kind: 'travel' as const, durationMinutes, contentSafety: safety() })
const selectedWorld = (seed: string, preset: 'sheltered-reach' | 'watershed' | 'far-coast' = 'sheltered-reach') => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset } }), 'crew:0')
const simulationProjectionFor = (world: ReturnType<typeof selectedWorld>) => simulationCatchUpProjection({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime
}, world.state.simulation)
const contextFor = (world: ReturnType<typeof selectedWorld>) => ({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime,
  activeCourierId: world.state.courier.initialCourierId,
  people: world.state.people.records,
  delegation: world.state.delegation,
  era: world.state.era,
  simulation: world.state.simulation,
  plan: createFidelityPlan({ world, activeCourierId: world.state.courier.initialCourierId!, loadedLocations: [] })
})

const acceptedOffer = (world: ReturnType<typeof selectedWorld>, prefix: string) => {
  const courierId = world.state.courier.initialCourierId!
  for (const recipient of world.state.people.records.filter(person => person.id !== courierId)) {
    for (const definition of DELEGATION_TASK_DEFINITIONS) {
      const interest = definition.relevantMaterialInterests.find(item => recipient.materialInterests.includes(item))
      const skill = definition.relevantSkills.find(item => recipient.work.skills.some(candidate => candidate.kind === item && candidate.level >= 1))
      if (!interest || !skill) continue
      const proposal = {
        version: DELEGATION_CONTRACT_VERSION,
        kind: 'request' as const,
        urgency: 'routine' as const,
        complexity: 'routine' as const,
        materialInterest: interest,
        contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
      }
      for (let attempt = 0; attempt < 8; attempt++) {
        const id = `${prefix}:${recipient.id}:${definition.family}:${attempt}`
        const roll = new SeededRng(`jomon-delegation:${DELEGATION_CONTRACT_VERSION}:${world.id}:${world.manifest.creation.digest}:agreement:${delegationTaskIdForOffer(id)}:1`).integer(6)
        if (roll !== 0) continue
        const offer: DelegationOfferInput = { version: DELEGATION_CONTRACT_VERSION, id, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal }
        const result = offerFoundationWorldDelegatedTask(world, offer)
        const task = result.state.delegation.tasks.find(candidate => candidate.id === delegationTaskIdForOffer(id))!
        if (task.status === 'in-progress') return { world: result, task }
      }
    }
  }
  throw new Error('fixture did not find an accepted delegated task')
}

describe('deterministic medieval autonomy', () => {
  it('uses the closed settled-to-urgent need scale and produces the required detailed and summary choice projection', () => {
    const start = selectedWorld('autonomy-tiers')
    const advanced = advanceFoundationWorldTime(start, wait('wait:autonomy-tiers', 120))
    const observations = advanced.state.autonomy.observations
    const tiers = new Set(observations.map(observation => observation.tier))

    expect(AUTONOMY_NEED_WINDOW_MINUTES).toBe(30)
    expect(advanced.state.autonomy.version).toBe(AUTONOMY_STATE_VERSION)
    expect(advanced.state.autonomy.processing).toEqual(start.state.people.records.map(person => ({ personId: person.id, processedNeedWindows: 4 })).sort((left, right) => left.personId.localeCompare(right.personId)))
    for (const person of advanced.state.people.records) {
      const before = start.state.people.records.find(candidate => candidate.id === person.id)!
      for (const key of AUTONOMY_NEED_KEYS) expect(person.needs[key]).toBe(Math.min(5, before.needs[key] + 1))
    }
    expect(tiers).toEqual(new Set(['loaded', 'nearby', 'recurring', 'distant-individual-summary']))
    expect(observations.some(observation => observation.detail === 'summary' && observation.tier === 'distant-individual-summary')).toBe(true)
    expect(observations.filter(observation => observation.detail === 'detailed').every(observation => ['loaded', 'nearby', 'recurring'].includes(observation.tier))).toBe(true)
    expect(observations.every(observation => AUTONOMY_CHOICE_KINDS.includes(observation.choice) && observation.factors.every(factor => AUTONOMY_FACTOR_CODES.includes(factor)) && observation.factors.join(',') === [...observation.factors].sort().join(','))).toBe(true)
    expect(advanced.state.autonomy.observations.length).toBeLessThanOrEqual(AUTONOMY_LIMITS.observations)
    expect(validateFoundationWorld(advanced)).toEqual([])
  })

  it('is deterministic and partition-invariant at need/catch-up/era boundaries, including delegated work', () => {
    const source = acceptedOffer(selectedWorld('autonomy-partition'), 'autonomy-task')
    expect(source.world.state.autonomy.observations.find(observation => observation.personId === source.task.recipientId)?.choice).toBe('continue-accepted-delegated-work')
    const single = advanceFoundationWorldTime(source.world, travel('travel:autonomy-single', 7_199))
    const split = advanceFoundationWorldTime(
      advanceFoundationWorldTime(source.world, wait('wait:autonomy-first', 29)),
      travel('travel:autonomy-second', 7_170)
    )

    expect(single.state.temporal.worldTime).toBe(7_200)
    expect(single.state.people).toEqual(split.state.people)
    expect(autonomyProjection(single.state.autonomy)).toEqual(autonomyProjection(split.state.autonomy))
    expect(single.state.delegation).toEqual(split.state.delegation)
    expect(simulationProjectionFor(single)).toEqual(simulationProjectionFor(split))
    expect(worldEraProjection(single.state.era)).toEqual(worldEraProjection(split.state.era))
    expect(single.state.delegation.tasks[0]!.status).toBe('completed')
    expect(single.state.autonomy.observations.some(observation => observation.choice === 'address-urgent-personal-needs')).toBe(true)
    expect(single.state.temporal.causalRecords).not.toEqual(split.state.temporal.causalRecords)
    expect(validateFoundationWorld(single)).toEqual([])
    expect(validateFoundationWorld(split)).toEqual([])
  }, 20_000)

  it('uses active delegated work, recovery, urgent needs, household readiness/support, then hold as the closed priority order', () => {
    const advanced = advanceFoundationWorldTime(selectedWorld('autonomy-priority'), wait('wait:autonomy-priority', 120))
    const target = advanced.state.people.records.find(person => person.id !== 'crew:0' && advanced.state.autonomy.observations.some(observation => observation.personId === person.id))!
    const original = contextFor(advanced)
    const withPerson = (change: (person: typeof target) => typeof target) => {
      const people = original.people.map(person => person.id === target.id ? change(structuredClone(person)) : structuredClone(person))
      return { ...original, people }
    }
    const recovery = reconcileAutonomyState(advanced.state.autonomy, withPerson(person => ({
      ...person,
      health: {
        ...person.health,
        condition: 'recovering',
        recovery: { status: 'recovering', injuryId: 'recovery:1', completeAtWorldTime: 121 },
        injuries: [{ id: 'recovery:1', kind: 'strain', receivedAtWorldTime: 0, recovery: 'recovering', contentSafety: person.health.injuries[0]?.contentSafety ?? person.identity.contentSafety }]
      }
    })))
    const urgent = reconcileAutonomyState(advanced.state.autonomy, withPerson(person => ({ ...person, needs: { nourishment: 5, rest: 0, shelter: 0, safety: 0 } })))
    const support = reconcileAutonomyState(advanced.state.autonomy, withPerson(person => ({
      ...person,
      materialInterests: ['records'],
      needs: { nourishment: 0, rest: 0, shelter: 0, safety: 0 },
      relationships: person.relationships.map(relationship => relationship.targetPersonId === 'crew:0' ? { ...relationship, standing: 2 as const } : relationship)
    })))
    const opposed = reconcileAutonomyState(advanced.state.autonomy, withPerson(person => ({
      ...person,
      materialInterests: ['records'],
      needs: { nourishment: 0, rest: 0, shelter: 0, safety: 0 },
      relationships: person.relationships.map(relationship => relationship.targetPersonId === 'crew:0' ? { ...relationship, standing: -2 as const } : relationship)
    })))
    const hold = reconcileAutonomyState(advanced.state.autonomy, withPerson(person => ({ ...person, household: { kind: 'site-household', anchor: { kind: 'site', id: advanced.state.sites.sites[0]!.id } }, home: { kind: 'site', id: advanced.state.sites.sites[0]!.id }, location: { kind: 'site', id: advanced.state.sites.sites[0]!.id }, materialInterests: ['records'] })))

    expect(recovery.observations.find(observation => observation.personId === target.id)?.choice).toBe('seek-recovery')
    expect(urgent.observations.find(observation => observation.personId === target.id)?.choice).toBe('address-urgent-personal-needs')
    expect(support.observations.find(observation => observation.personId === target.id)?.choice).toBe('support-household')
    expect(opposed.observations.find(observation => observation.personId === target.id)?.choice).toBe('hold-wait')
    expect(hold.observations.find(observation => observation.personId === target.id)?.choice).toBe('hold-wait')
    expect(advanced.state.autonomy.observations.find(observation => observation.personId === target.id)?.choice).toMatch(/maintain-household-readiness|support-household|hold-wait/)
  })

  it('keeps unavailable, deferred, historical, and uninstantiated records out of active autonomous work', () => {
    const world = advanceFoundationWorldTime(selectedWorld('autonomy-exclusion'), wait('wait:autonomy-exclusion', 120))
    const context = contextFor(world)
    const unavailableId = world.state.autonomy.observations[0]!.personId
    const unavailable = reconcileAutonomyState(world.state.autonomy, {
      ...context,
      people: context.people.map(person => person.id === unavailableId ? { ...structuredClone(person), work: { ...person.work, availability: 'unavailable' as const, capacity: { ...person.work.capacity, current: 0 } } } : structuredClone(person))
    })
    const deferred = context.plan.individuals.find(assignment => assignment.tier === 'deferred')!
    const seedIds = world.initialWorld.people.map(person => person.id)
    const frontierIds = world.state.geography.frontier.regions.map(region => region.commitment.id)

    expect(unavailable.observations.some(observation => observation.personId === unavailableId)).toBe(false)
    expect(world.state.autonomy.observations.some(observation => observation.personId === deferred.personId)).toBe(false)
    expect(world.state.autonomy.processing.map(item => item.personId)).toEqual(world.state.people.records.map(person => person.id))
    expect(world.state.autonomy.processing.every(item => !seedIds.includes(item.personId) && !frontierIds.includes(item.personId))).toBe(true)
  })

  it('rejects tampered records, references, tokens, factors, and content safety fail-closed', () => {
    const world = advanceFoundationWorldTime(selectedWorld('autonomy-validation'), wait('wait:autonomy-validation', 120))
    const context = contextFor(world)
    const forgedToken = structuredClone(world.state.autonomy)
    forgedToken.observations[0]!.token++
    const unknownFactor = structuredClone(world.state.autonomy)
    ;(unknownFactor.observations[0]!.factors as string[]).push('free-text-rationale')
    const unsafe = structuredClone(world.state.autonomy)
    ;(unsafe.observations[0]!.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const duplicate = structuredClone(world.state.autonomy)
    duplicate.observations = [structuredClone(duplicate.observations[0]!), structuredClone(duplicate.observations[0]!)]
    const invalidReference = structuredClone(world.state.autonomy)
    invalidReference.observations[0]!.cursorId = 'catch-up:person:not-present'
    const invalidTier = structuredClone(world.state.autonomy)
    ;(invalidTier.observations[0] as { tier: string }).tier = 'deferred'

    expect(validateAutonomyState(context, forgedToken).map(item => item.code)).toContain('autonomy.invalid-observation')
    expect(validateAutonomyState(context, unknownFactor).map(item => item.code)).toContain('autonomy.invalid-observation')
    expect(validateAutonomyState(context, unsafe).map(item => item.code)).toContain('content-safety.prohibited.torture')
    expect(validateAutonomyState(context, duplicate).map(item => item.code)).toContain('autonomy.duplicate-id')
    expect(validateAutonomyState(context, invalidReference).map(item => item.code)).toContain('autonomy.invalid-observation')
    expect(validateAutonomyState(context, invalidTier).map(item => item.code)).toContain('autonomy.invalid-observation')
    expect(validateAutonomyPlanState(context, { ...world.state.autonomy, observations: [] }).map(item => item.code)).toContain('autonomy.invalid-plan-projection')
  })

  it('retains replay authority through compaction and leaves pure commands and unrelated domains untouched', () => {
    let world = selectedWorld('autonomy-replay')
    const before = structuredClone(world)
    expect(() => advanceFoundationWorldTime(world, { kind: 'inspect' })).toThrow('temporal contract rejected')
    expect(world).toEqual(before)
    for (let index = 0; index < 9; index++) world = advanceFoundationWorldTime(world, wait(`wait:autonomy-checkpoint:${index}`, 30))

    expect(world.state.causalHistory.checkpoint.projection.autonomy).toMatchObject({ version: AUTONOMY_STATE_VERSION })
    expect(world.state.causalHistory.checkpoint.sequence).toBeLessThan(world.state.temporal.actionSequence + 1)
    expect(replayFoundationWorldCausalHistory(world)).toEqual(causalReplayProjectionForWorldState(world.state))
    expect(world.jomon).toEqual(before.jomon)
    expect(world.crew).toEqual(before.crew)
    expect(world.initialWorld).toEqual(before.initialWorld)
    expect(world.state.routes).toEqual(before.state.routes)
    expect(world.state.markets).toEqual(before.state.markets)
    expect(validateFoundationWorld(world)).toEqual([])
  }, 20_000)
})
