import { describe, expect, it } from 'vitest'
import { assessCourierConversation } from './conversation'
import { DELEGATION_CONTRACT_VERSION, DELEGATION_FACTOR_CODES, DELEGATION_LIMITS, DELEGATION_TASK_DEFINITIONS, DELEGATION_TASK_FAMILIES, delegationDefinitionFor, delegationTaskIdForOffer, offerDelegatedTask, validateDelegationState, validateDelegationTaskDefinitions, type DelegationOfferInput } from './delegation'
import { auditMedievalContentSafety, classifyMedievalContent } from './content-safety'
import { SeededRng } from './rng'
import { simulationCatchUpProjection } from './simulation-catchup'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, foundationWorldCausalHistoryMatches, interruptFoundationWorldDelegatedTask, offerFoundationWorldDelegatedTask, replayFoundationWorldCausalHistory, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState, medievalWorldStateContentRecords } from './world-state'
import { SOCIAL_MEMORY_LIMITS } from './social-memory'

const selectedWorld = (seed: string) => chooseInitialCourier(createFoundationWorld({ seed }), 'crew:0')
const wait = (id: string, durationMinutes: number) => ({
  id,
  kind: 'wait' as const,
  durationMinutes,
  contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
})
const delegationContext = (world: ReturnType<typeof selectedWorld>) => ({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime,
  people: world.state.people.records
})
const proposalFor = (interest: DelegationOfferInput['proposal']['materialInterest']) => ({
  version: DELEGATION_CONTRACT_VERSION,
  kind: 'request' as const,
  urgency: 'routine' as const,
  complexity: 'routine' as const,
  materialInterest: interest,
  contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
})

const offersFor = (world: ReturnType<typeof selectedWorld>, prefix: string): readonly DelegationOfferInput[] => {
  const courierId = world.state.courier.initialCourierId!
  const offers: DelegationOfferInput[] = []
  for (const recipient of world.state.people.records.filter(person => person.id !== courierId)) {
    for (const definition of DELEGATION_TASK_DEFINITIONS) {
      const interest = definition.relevantMaterialInterests.find(item => recipient.materialInterests.includes(item))
      if (!interest) continue
      const assessment = assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId: recipient.id, proposal: proposalFor(interest) })
      if (assessment.eligibility !== 'eligible' || !assessment.unlockedApproaches.includes('direct-request')) continue
      offers.push({ version: DELEGATION_CONTRACT_VERSION, id: `${prefix}:${recipient.id}:${definition.family}`, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal: proposalFor(interest) })
    }
  }
  return offers
}

const acceptedOffer = (world: ReturnType<typeof selectedWorld>, prefix: string) => {
  const courierId = world.state.courier.initialCourierId!
  for (const recipient of world.state.people.records.filter(person => person.id !== courierId)) {
    for (const definition of DELEGATION_TASK_DEFINITIONS) {
      const interest = definition.relevantMaterialInterests.find(item => recipient.materialInterests.includes(item))
      const skill = definition.relevantSkills.find(item => recipient.work.skills.some(candidate => candidate.kind === item && candidate.level >= 1))
      if (!interest || !skill) continue
      const assessment = assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId: recipient.id, proposal: proposalFor(interest) })
      if (assessment.eligibility !== 'eligible') continue
      for (let attempt = 0; attempt < 8; attempt++) {
        const id = `${prefix}:${recipient.id}:${definition.family}:${attempt}`
        const taskId = delegationTaskIdForOffer(id)
        const roll = new SeededRng(`jomon-delegation:${DELEGATION_CONTRACT_VERSION}:${world.id}:${world.manifest.creation.digest}:agreement:${taskId}:1`).integer(6)
        if (roll !== 0) continue
        const offer: DelegationOfferInput = { version: DELEGATION_CONTRACT_VERSION, id, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal: proposalFor(interest) }
        const candidate = offerFoundationWorldDelegatedTask(world, offer)
        const task = candidate.state.delegation.tasks.find(item => item.offerId === offer.id)!
        if (task.status === 'in-progress') return { world: candidate, offer, task }
      }
    }
  }
  throw new Error(`fixture did not produce an accepted delegation offer for ${prefix}`)
}

const refusalOffer = (world: ReturnType<typeof selectedWorld>, prefix: string) => {
  const courierId = world.state.courier.initialCourierId!
  for (const recipient of world.state.people.records.filter(person => person.id !== courierId)) {
    for (const definition of DELEGATION_TASK_DEFINITIONS) {
      if (definition.relevantMaterialInterests.some(interest => recipient.materialInterests.includes(interest))) continue
      const interest = definition.relevantMaterialInterests[0]!
      const assessment = assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId: recipient.id, proposal: proposalFor(interest) })
      if (assessment.eligibility !== 'eligible') continue
      const offer: DelegationOfferInput = { version: DELEGATION_CONTRACT_VERSION, id: `${prefix}:${recipient.id}:${definition.family}`, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal: proposalFor(interest) }
      const result = offerFoundationWorldDelegatedTask(world, offer)
      const task = result.state.delegation.tasks.find(item => item.offerId === offer.id)!
      if (task.status === 'refused') return { world: result, offer, task }
    }
  }
  throw new Error(`fixture did not produce a refused delegation offer for ${prefix}`)
}

describe('constrained deterministic delegation', () => {
  it('defines every approved task family as validated data with no domain effect contract', () => {
    expect(DELEGATION_TASK_FAMILIES).toHaveLength(21)
    expect(new Set(DELEGATION_TASK_FAMILIES).size).toBe(DELEGATION_TASK_FAMILIES.length)
    expect(validateDelegationTaskDefinitions()).toEqual([])
    expect(DELEGATION_TASK_DEFINITIONS.every(definition => definition.progressBasis === 'elapsed-in-world-minutes' && definition.durationMinutes.routine >= 5 && definition.futureDomainHooks.length > 0)).toBe(true)
    expect(delegationDefinitionFor('maintenance')).toMatchObject({ riskBand: 'guarded', durationMinutes: { routine: 30, involved: 60 } })
    expect(() => delegationDefinitionFor('unapproved-work' as never)).toThrow('delegation.invalid-definition')
    const malformed = structuredClone(DELEGATION_TASK_DEFINITIONS)
    ;(malformed[0]!.futureDomainHooks as string[]).push('economy-that-does-not-exist')
    expect(validateDelegationTaskDefinitions(malformed).map(item => item.code)).toContain('delegation.invalid-definition')
  })

  it('gates a deterministic offer through conversation, then reserves one recipient capacity unit and journals one command', () => {
    const start = selectedWorld('delegation-accepted')
    const { world, offer, task } = acceptedOffer(start, 'offer:accepted')
    const recipient = world.state.people.records.find(person => person.id === task.recipientId)!
    const beforeRecipient = start.state.people.records.find(person => person.id === task.recipientId)!

    expect(task).toMatchObject({ id: delegationTaskIdForOffer(offer.id), status: 'in-progress', progressMinutes: 0, acceptedAtWorldTime: 1 })
    expect(task.assessment.eligibility).toBe('eligible')
    expect(task.agreement.status).toBe('accepted')
    expect(task.agreement.factors).toEqual([...task.agreement.factors].sort())
    expect(task.agreement.factors.every(factor => DELEGATION_FACTOR_CODES.includes(factor))).toBe(true)
    expect(recipient.work).toMatchObject({ availability: 'committed', current: { status: 'committed' } })
    expect(recipient.work.capacity.current).toBe(beforeRecipient.work.capacity.current - 1)
    expect(recipient.commitments).toEqual(expect.arrayContaining([expect.objectContaining({ kind: 'delegated-task', status: 'active' })]))
    expect(world.state.simulation.delegatedWork).toEqual([expect.objectContaining({ id: task.id, assigneePersonId: task.recipientId, status: 'active', progressIntervals: 0 })])
    expect(world.state.causalHistory.tail.map(command => command.kind)).toEqual(['initial-courier-selected', 'delegation-offered'])
    expect(world.state.temporal.causalRecords.at(-1)).toMatchObject({ actionKind: 'delegated-task-commitment', durationMinutes: 1 })
    expect(replayFoundationWorldCausalHistory(world)).toEqual(causalReplayProjectionForWorldState(world.state))
    expect(foundationWorldCausalHistoryMatches(world)).toBe(true)
    expect(validateFoundationWorld(world)).toEqual([])
  })

  it('records a typed refusal with evidence but no active task, person-work mutation, or scheduler placeholder', () => {
    const start = selectedWorld('delegation-refusal')
    const { world, task } = refusalOffer(start, 'offer:refusal')
    const recipient = world.state.people.records.find(person => person.id === task.recipientId)!

    expect(task).toMatchObject({ status: 'refused', progressMinutes: 0, agreement: { status: 'refused' }, outcome: { kind: 'refused', atWorldTime: 1 } })
    expect(task.agreement.factors).toContain('recipient-interest-misaligned')
    expect(recipient.work).toMatchObject({ availability: 'available', current: { status: 'idle' } })
    expect(recipient.commitments).toEqual([])
    expect(world.state.simulation.delegatedWork).toEqual([])
    expect(recipient.memories.some(memory => memory.kind === 'social-memory')).toBe(true)
    expect(validateFoundationWorld(world)).toEqual([])
  })

  it('retains immutable foundation history plus a canonical bounded source-linked social-memory tail for both participants', () => {
    const world = selectedWorld('delegation-memory-bound')
    const courierId = world.state.courier.initialCourierId!
    const recipient = world.state.people.records.find(person => person.id !== courierId)!
    const definition = DELEGATION_TASK_DEFINITIONS.find(candidate => candidate.relevantMaterialInterests.every(interest => !recipient.materialInterests.includes(interest)))!
    const proposal = proposalFor(definition.relevantMaterialInterests[0]!)
    const assessment = assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId: recipient.id, proposal })
    let state = world.state.delegation
    let people = world.state.people.records
    let socialMemory = world.state.socialMemory

    for (let index = 0; index < SOCIAL_MEMORY_LIMITS.retainedPersonalLinks + 2; index++) {
      const offer: DelegationOfferInput = {
        version: DELEGATION_CONTRACT_VERSION,
        id: `memory-refusal:${String(index).padStart(2, '0')}`,
        courierId,
        recipientId: recipient.id,
        family: definition.family,
        approach: 'direct-request',
        proposal
      }
      const transition = offerDelegatedTask({ worldId: world.id, creationDigest: world.manifest.creation.digest, worldTime: 0, people }, state, people, socialMemory, offer, assessment)
      expect(transition.task.status).toBe('refused')
      state = transition.state
      people = transition.people
      socialMemory = transition.socialMemory
    }

    const retained = people.find(person => person.id === recipient.id)!
    expect(retained.memories.filter(memory => memory.kind === 'foundation-history')).toHaveLength(1)
    expect(retained.memories.filter(memory => memory.kind === 'social-memory')).toHaveLength(SOCIAL_MEMORY_LIMITS.retainedPersonalLinks)
    expect(retained.memories.map(memory => memory.id)).toEqual([...retained.memories.map(memory => memory.id)].sort())
    expect(validateDelegationState({ worldId: world.id, creationDigest: world.manifest.creation.digest, worldTime: 0, people }, state)).toEqual([])
  })

  it('completes at the canonical due minute, releases work, and preserves task/person/catch-up projections across time partitions', () => {
    const start = selectedWorld('delegation-completion-partition')
    const first = acceptedOffer(start, 'offer:partition')
    const remaining = first.task.plannedCompletionAtWorldTime! - first.world.state.temporal.worldTime
    const single = advanceFoundationWorldTime(first.world, wait('wait:single', remaining))
    const split = advanceFoundationWorldTime(advanceFoundationWorldTime(first.world, wait('wait:split-a', 2)), wait('wait:split-b', remaining - 2))
    const task = single.state.delegation.tasks.find(item => item.id === first.task.id)!
    const recipient = single.state.people.records.find(person => person.id === task.recipientId)!

    expect(task).toMatchObject({ status: 'completed', progressMinutes: remaining, outcome: { kind: 'completed', atWorldTime: first.task.plannedCompletionAtWorldTime } })
    expect(task.outcome?.atWorldTime).toBe(first.task.plannedCompletionAtWorldTime)
    expect(recipient.work).toMatchObject({ availability: 'available', current: { status: 'idle' } })
    expect(recipient.commitments).toEqual(expect.arrayContaining([expect.objectContaining({ kind: 'delegated-task', status: 'resolved', resolvedAtWorldTime: task.outcome?.atWorldTime })]))
    expect(single.state.simulation.delegatedWork).toEqual([expect.objectContaining({ id: task.id, status: 'resolved', resolvedAtWorldTime: task.outcome?.atWorldTime })])
    expect(single.state.delegation).toEqual(split.state.delegation)
    expect(single.state.people).toEqual(split.state.people)
    expect(single.state.socialMemory).toEqual(split.state.socialMemory)
    expect(simulationCatchUpProjection({ worldId: single.id, creationDigest: single.manifest.creation.digest, worldTime: single.state.temporal.worldTime }, single.state.simulation)).toEqual(simulationCatchUpProjection({ worldId: split.id, creationDigest: split.manifest.creation.digest, worldTime: split.state.temporal.worldTime }, split.state.simulation))
    expect(single.state.temporal.causalRecords).not.toEqual(split.state.temporal.causalRecords)
    expect(validateFoundationWorld(single)).toEqual([])
    expect(validateFoundationWorld(split)).toEqual([])
  })

  it('interrupts only active work through the active courier and releases the exact commitment at resolution time', () => {
    const first = acceptedOffer(selectedWorld('delegation-interruption'), 'offer:interrupt')
    const interrupted = interruptFoundationWorldDelegatedTask(first.world, {
      version: DELEGATION_CONTRACT_VERSION,
      taskId: first.task.id,
      courierId: first.offer.courierId,
      reason: 'courier-recall'
    })
    const task = interrupted.state.delegation.tasks.find(item => item.id === first.task.id)!
    const recipient = interrupted.state.people.records.find(person => person.id === task.recipientId)!

    expect(task).toMatchObject({ status: 'interrupted', progressMinutes: 1, outcome: { kind: 'interrupted', reason: 'courier-recall', atWorldTime: 2 } })
    expect(recipient.work).toMatchObject({ availability: 'available', current: { status: 'idle' } })
    expect(recipient.commitments).toEqual(expect.arrayContaining([expect.objectContaining({ kind: 'delegated-task', status: 'cancelled', resolvedAtWorldTime: 2 })]))
    expect(interrupted.state.simulation.delegatedWork).toEqual([expect.objectContaining({ id: task.id, status: 'resolved', resolvedAtWorldTime: 2 })])
    expect(interrupted.state.causalHistory.tail.map(command => command.kind)).toEqual(['initial-courier-selected', 'delegation-offered', 'delegation-interrupted'])
    expect(validateFoundationWorld(interrupted)).toEqual([])
  })

  it('keeps task-owned people and registry state authoritative through journal checkpoint compaction and continuation', () => {
    const first = acceptedOffer(selectedWorld('delegation-checkpoint'), 'offer:checkpoint')
    const remaining = first.task.plannedCompletionAtWorldTime! - first.world.state.temporal.worldTime
    let compacted = advanceFoundationWorldTime(first.world, wait('wait:checkpoint-complete', remaining))
    for (let index = 0; index < 6; index++) compacted = advanceFoundationWorldTime(compacted, wait(`wait:checkpoint:${index}`, 1))

    expect(compacted.state.causalHistory.checkpoint.sequence).toBeGreaterThan(0)
    expect(compacted.state.causalHistory.checkpoint.projection.people).toEqual(compacted.state.people)
    expect(compacted.state.causalHistory.checkpoint.projection.delegation).toEqual(compacted.state.delegation)
    expect(compacted.state.causalHistory.checkpoint.projection.socialMemory).toEqual(compacted.state.socialMemory)
    expect(replayFoundationWorldCausalHistory(compacted)).toEqual(causalReplayProjectionForWorldState(compacted.state))

    const continued = advanceFoundationWorldTime(compacted, wait('wait:checkpoint-continuation', 1))
    expect(continued.state.delegation).toEqual(compacted.state.delegation)
    expect(continued.state.people).toEqual(compacted.state.people)
    expect(continued.state.socialMemory).toEqual(compacted.state.socialMemory)
    expect(validateFoundationWorld(continued)).toEqual([])
  }, 20_000)

  it('cannot use a high conversation value or an unlocked approach to bypass current committed work, and preserves caller state on rejection', () => {
    const maximum = selectedWorld('delegation-maximum:10')
    const original = structuredClone(maximum)
    const locked = offersFor(maximum, 'offer:locked')[0]!
    const blockedApproach: DelegationOfferInput = { ...locked, id: 'offer:locked-approach', approach: 'reciprocal-options' }
    if (maximum.state.people.records.find(person => person.id === 'crew:0')!.identity.conversation < 5) {
      expect(() => offerFoundationWorldDelegatedTask(maximum, blockedApproach)).toThrow('delegation.approach-locked')
      expect(maximum).toEqual(original)
    }

    const first = acceptedOffer(maximum, 'offer:maximum')
    const nextOffer: DelegationOfferInput = { ...first.offer, id: 'offer:second-committed' }
    expect(() => offerFoundationWorldDelegatedTask(first.world, nextOffer)).toThrow('delegation.conversation-blocked')
    expect(first.world.state.delegation.tasks).toHaveLength(1)
  })

  it('does not offer, progress, resolve, or journal delegated work from a pure UI command', () => {
    const first = acceptedOffer(selectedWorld('delegation-pure-command'), 'offer:pure-command')
    const before = structuredClone(first.world)

    expect(() => advanceFoundationWorldTime(first.world, { kind: 'inspect' })).toThrow('temporal contract rejected')
    expect(first.world).toEqual(before)
    expect(first.world.state.delegation.tasks[0]!.status).toBe('in-progress')
    expect(first.world.state.socialMemory).toEqual(before.state.socialMemory)
  })

  it('rejects malformed, unsafe, duplicate, unordered, and bounded task registry state without instantiating another person', () => {
    const { world } = acceptedOffer(selectedWorld('delegation-validation'), 'offer:validation')
    const context = delegationContext(world)
    const duplicate = structuredClone(world.state.delegation)
    duplicate.tasks = [structuredClone(duplicate.tasks[0]!), structuredClone(duplicate.tasks[0]!)]
    const unordered = structuredClone(world.state.delegation)
    unordered.tasks = [...unordered.tasks, { ...structuredClone(unordered.tasks[0]!), id: 'delegated-task:a-second', offerId: 'a-second' }]
    const unsafe = structuredClone(world.state.delegation)
    ;(unsafe.tasks[0]!.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const excessive = structuredClone(world.state.delegation)
    excessive.tasks = Array.from({ length: DELEGATION_LIMITS.tasks + 1 }, () => structuredClone(excessive.tasks[0]!))

    expect(validateDelegationState(context, duplicate).map(item => item.code)).toContain('delegation.duplicate-task-id')
    expect(validateDelegationState(context, unordered).map(item => item.code)).toContain('delegation.invalid-lifecycle')
    expect(validateDelegationState(context, unsafe).map(item => item.code)).toContain('content-safety.prohibited.torture')
    expect(validateDelegationState(context, excessive).map(item => item.code)).toContain('delegation.task-limit')
    const unsafeMemoryWorld = structuredClone(world)
    ;(unsafeMemoryWorld.state.people.records.find(person => person.id === world.state.courier.initialCourierId!)!.memories.find(memory => memory.kind === 'social-memory')!.contentSafety.exclusions as unknown as Record<string, string>).slavery = 'present'
    expect(auditMedievalContentSafety(medievalWorldStateContentRecords(unsafeMemoryWorld.state)).diagnostics.map(item => item.code)).toContain('content-safety.prohibited.slavery')
    expect(validateFoundationWorld(unsafeMemoryWorld).map(item => item.code)).toEqual(expect.arrayContaining(['foundation-world.invalid-mutable-state', 'foundation-world.invalid-causal-history']))
    expect(world.state.people.records.map(person => person.id)).toEqual(world.crew.map(member => member.id).sort())
  })
})
