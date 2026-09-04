import { describe, expect, it } from 'vitest'
import { assessCourierConversation } from './conversation'
import { classifyMedievalContent } from './content-safety'
import { assessCourierContinuityLoss, courierContinuityConfirmationIdFor, courierContinuityContentSafety } from './courier-continuity'
import { DELEGATION_CONTRACT_VERSION, DELEGATION_TASK_DEFINITIONS, delegationTaskIdForOffer, type DelegationOfferInput } from './delegation'
import { initialHouseholdActiveCrew } from './initial-household'
import { SeededRng } from './rng'
import { assessTavernCourierSwitch } from './world'
import { chooseInitialCourier, createFoundationWorld, offerFoundationWorldDelegatedTask, replayFoundationWorldCausalHistory, resolveCourierContinuityLoss, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'

const selectedWorld = (seed: string) => {
  const world = createFoundationWorld({ seed, configuration: { preset: 'watershed' } })
  return chooseInitialCourier(world, initialHouseholdActiveCrew(world.crew)[0]!.id)
}

const confirmationFor = (world: ReturnType<typeof selectedWorld>, outcome: 'death' | 'departure') => {
  const courierId = world.state.courier.activeCourierId!
  const atWorldTime = world.state.temporal.worldTime
  return {
    version: 1 as const,
    id: courierContinuityConfirmationIdFor(outcome, courierId, atWorldTime),
    kind: 'confirmed-courier-continuity-loss' as const,
    outcome,
    courierId,
    atWorldTime,
    evidenceIds: ['loss-evidence:confirmed'] as const,
    contentSafety: courierContinuityContentSafety()
  }
}

const acceptedDelegationFor = (world: ReturnType<typeof selectedWorld>, recipientId: string) => {
  const recipient = world.state.people.records.find(person => person.id === recipientId)!
  const courierId = world.state.courier.activeCourierId!
  for (const definition of DELEGATION_TASK_DEFINITIONS) {
    const interest = definition.relevantMaterialInterests.find(item => recipient.materialInterests.includes(item))
    const skill = definition.relevantSkills.find(item => recipient.work.skills.some(candidate => candidate.kind === item && candidate.level >= 1))
    if (!interest || !skill) continue
    const proposal: DelegationOfferInput['proposal'] = {
      version: DELEGATION_CONTRACT_VERSION,
      kind: 'request',
      urgency: 'routine',
      complexity: 'routine',
      materialInterest: interest,
      contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
    }
    if (assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId, proposal }).eligibility !== 'eligible') continue
    for (let attempt = 0; attempt < 8; attempt++) {
      const id = `continuity-delegation:${recipientId}:${definition.family}:${attempt}`
      const taskId = delegationTaskIdForOffer(id)
      if (new SeededRng(`jomon-delegation:${DELEGATION_CONTRACT_VERSION}:${world.id}:${world.manifest.creation.digest}:agreement:${taskId}:1`).integer(6) !== 0) continue
      const offered = offerFoundationWorldDelegatedTask(world, { version: DELEGATION_CONTRACT_VERSION, id, courierId, recipientId, family: definition.family, approach: 'direct-request', proposal })
      if (offered.state.delegation.tasks.find(task => task.id === taskId)?.status === 'in-progress') return offered
    }
  }
  throw new Error('expected a deterministic accepted delegation fixture')
}

describe('courier continuity', () => {
  it('records a permanent adult death at the exact minute and transfers only active/navigation ownership', () => {
    const source = selectedWorld('continuity-death')
    const before = structuredClone(source)
    const lost = source.state.people.records.find(person => person.id === source.state.courier.activeCourierId)!
    const result = resolveCourierContinuityLoss(source, confirmationFor(source, 'death'))

    expect(result.status).toBe('continued')
    if (result.status !== 'continued') throw new Error('expected a surviving successor')
    const next = result.world
    const successorId = initialHouseholdActiveCrew(source.crew)[1]!.id
    const dead = next.state.people.records.find(person => person.id === lost.id)!
    expect(next.state.courier).toEqual({ version: 3, initialCourierId: source.state.courier.initialCourierId, activeCourierId: successorId, departedCourierIds: [] })
    expect(next.state.navigation).toEqual({ version: 1, courierId: successorId, coordinate: source.state.navigation.coordinate })
    expect(next.state.temporal).toEqual(source.state.temporal)
    expect(dead.life).toEqual({ status: 'dead', birth: lost.life.birth, death: { atWorldTime: source.state.temporal.worldTime } })
    expect(dead.work).toMatchObject({ availability: 'unavailable', current: { status: 'idle' } })
    expect(dead.possessions).toEqual(lost.possessions)
    expect(dead.relationships).toEqual(lost.relationships)
    expect(dead.memories).toEqual(lost.memories)
    expect(next.state.causalHistory.tail.at(-1)).toMatchObject({ kind: 'courier-loss-resolved', payload: { confirmation: confirmationFor(source, 'death'), finalization: 'continue', successorId } })
    expect(replayFoundationWorldCausalHistory(next)).toEqual(causalReplayProjectionForWorldState(next.state))
    expect(validateFoundationWorld(next)).toEqual([])
    expect(source).toEqual(before)
  })

  it('permanently excludes a departure without rewriting the persistent person and keeps tavern switching stricter', () => {
    const source = selectedWorld('continuity-departure')
    const beforePerson = structuredClone(source.state.people.records.find(person => person.id === source.state.courier.activeCourierId)!)
    const result = resolveCourierContinuityLoss(source, confirmationFor(source, 'departure'))

    expect(result.status).toBe('continued')
    if (result.status !== 'continued') throw new Error('expected a surviving successor')
    const next = result.world
    expect(next.state.courier.departedCourierIds).toEqual([beforePerson.id])
    expect(next.state.people.records.find(person => person.id === beforePerson.id)).toEqual(beforePerson)
    expect(assessTavernCourierSwitch(next).candidates.map(candidate => candidate.id)).not.toContain(beforePerson.id)
    expect(validateFoundationWorld(next)).toEqual([])
  })

  it('selects the first complete eligible living successor even while that survivor is temporarily unavailable', () => {
    const source = selectedWorld('continuity-committed-successor')
    const firstSuccessorId = initialHouseholdActiveCrew(source.crew)[1]!.id
    const committed = acceptedDelegationFor(source, firstSuccessorId)
    expect(committed.state.people.records.find(person => person.id === firstSuccessorId)?.work.availability).toBe('committed')
    const result = resolveCourierContinuityLoss(committed, confirmationFor(committed, 'death'))

    expect(result.status).toBe('continued')
    if (result.status !== 'continued') throw new Error('expected a living successor')
    expect(result.world.state.courier.activeCourierId).toBe(firstSuccessorId)
    expect(result.world.state.people.records.find(person => person.id === firstSuccessorId)?.work.availability).toBe('committed')
    expect(validateFoundationWorld(result.world)).toEqual([])
  })

  it('fails closed without changing a submitted world when confirmation evidence is stale, unsafe, or mismatched', () => {
    const source = selectedWorld('continuity-rejection')
    const before = structuredClone(source)
    const stale = { ...confirmationFor(source, 'death'), atWorldTime: source.state.temporal.worldTime + 1 }
    const wrongCourier = { ...confirmationFor(source, 'departure'), courierId: 'crew:1', id: courierContinuityConfirmationIdFor('departure', 'crew:1', source.state.temporal.worldTime) }
    const unsafe = { ...confirmationFor(source, 'death'), contentSafety: classifyMedievalContent('event', ['sexual-violence'], 'adults-only') }

    expect(() => resolveCourierContinuityLoss(source, stale)).toThrow('courier continuity rejected')
    expect(() => resolveCourierContinuityLoss(source, wrongCourier)).toThrow('courier continuity rejected')
    expect(() => resolveCourierContinuityLoss(source, unsafe)).toThrow('courier continuity rejected')
    expect(source).toEqual(before)
  })

  it('emits crew-extinction only for a canonical loss with no eligible living successor', () => {
    const source = selectedWorld('continuity-extinction')
    const people = source.state.people.records.map(person => person.id === source.state.courier.activeCourierId
      ? structuredClone(person)
      : {
          ...structuredClone(person),
          life: { status: 'dead' as const, birth: structuredClone(person.life.birth), death: { atWorldTime: 0 } },
          work: { ...structuredClone(person.work), availability: 'unavailable' as const, current: { status: 'idle' as const } }
        })
    const assessment = assessCourierContinuityLoss({
      version: 1,
      peopleContext: {
        seed: source.manifest.creation.seed,
        configurationFingerprint: source.manifest.creation.configurationFingerprint,
        initialWorld: source.initialWorld,
        frontier: source.state.geography.frontier,
        jomon: source.jomon,
        crew: source.crew,
        siteIds: source.state.sites.sites.map(site => site.id),
        worldTime: 0
      },
      people,
      courier: { initialCourierId: source.state.courier.initialCourierId, activeCourierId: source.state.courier.activeCourierId, departedCourierIds: [] },
      navigation: { courierId: source.state.navigation.courierId, coordinate: source.state.navigation.coordinate },
      confirmation: confirmationFor(source, 'departure')
    })

    expect(assessment.finalization).toEqual({ kind: 'crew-extinction' })
    expect(assessment.successorIds).toEqual([])
  })
})
