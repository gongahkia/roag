import { describe, expect, it } from 'vitest'
import { assessCourierConversation } from './conversation'
import { classifyMedievalContent } from './content-safety'
import { DELEGATION_CONTRACT_VERSION, DELEGATION_TASK_DEFINITIONS, delegationTaskIdForOffer, offerDelegatedTask, type DelegationOfferInput } from './delegation'
import { SeededRng } from './rng'
import { SOCIAL_MEMORY_EVIDENCE_SURFACES, SOCIAL_MEMORY_LIMITS, socialMemoryEvidenceRoutesFor, socialMemoryRecallForPair, socialMemoryRecordForTask, validateSocialMemoryState } from './social-memory'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, foundationWorldCausalHistoryMatches, interruptFoundationWorldDelegatedTask, offerFoundationWorldDelegatedTask, replayFoundationWorldCausalHistory, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'

const selectedWorld = (seed: string) => chooseInitialCourier(createFoundationWorld({ seed }), 'crew:0')
const proposalFor = (interest: DelegationOfferInput['proposal']['materialInterest']) => ({
  version: DELEGATION_CONTRACT_VERSION,
  kind: 'request' as const,
  urgency: 'routine' as const,
  complexity: 'routine' as const,
  materialInterest: interest,
  contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
})
const wait = (id: string, durationMinutes: number) => ({
  id,
  kind: 'wait' as const,
  durationMinutes,
  contentSafety: classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
})
const contextFor = (world: ReturnType<typeof selectedWorld>) => ({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime,
  people: world.state.people.records,
  tasks: world.state.delegation.tasks
})

const acceptedOffer = (world: ReturnType<typeof selectedWorld>, prefix: string) => {
  const courierId = world.state.courier.initialCourierId!
  for (const recipient of world.state.people.records.filter(person => person.id !== courierId)) {
    for (const definition of DELEGATION_TASK_DEFINITIONS) {
      const interest = definition.relevantMaterialInterests.find(item => recipient.materialInterests.includes(item))
      const skill = definition.relevantSkills.find(item => recipient.work.skills.some(candidate => candidate.kind === item && candidate.level >= 1))
      if (!interest || !skill) continue
      const proposal = proposalFor(interest)
      const assessment = assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId: recipient.id, proposal })
      if (assessment.eligibility !== 'eligible') continue
      for (let attempt = 0; attempt < 8; attempt++) {
        const id = `${prefix}:${recipient.id}:${definition.family}:${attempt}`
        const taskId = delegationTaskIdForOffer(id)
        const roll = new SeededRng(`jomon-delegation:${DELEGATION_CONTRACT_VERSION}:${world.id}:${world.manifest.creation.digest}:agreement:${taskId}:1`).integer(6)
        if (roll !== 0) continue
        const offer: DelegationOfferInput = { version: DELEGATION_CONTRACT_VERSION, id, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal }
        const result = offerFoundationWorldDelegatedTask(world, offer)
        const task = result.state.delegation.tasks.find(item => item.id === taskId)!
        if (task.status === 'in-progress') return { world: result, task, offer }
      }
    }
  }
  throw new Error(`fixture did not find accepted offer for ${prefix}`)
}

const refusedOffer = (world: ReturnType<typeof selectedWorld>, prefix: string) => {
  const courierId = world.state.courier.initialCourierId!
  for (const recipient of world.state.people.records.filter(person => person.id !== courierId)) {
    const definition = DELEGATION_TASK_DEFINITIONS.find(candidate => candidate.relevantMaterialInterests.every(interest => !recipient.materialInterests.includes(interest)))
    if (!definition) continue
    const proposal = proposalFor(definition.relevantMaterialInterests[0]!)
    const assessment = assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId: recipient.id, proposal })
    if (assessment.eligibility !== 'eligible') continue
    const offer: DelegationOfferInput = { version: DELEGATION_CONTRACT_VERSION, id: `${prefix}:${recipient.id}:${definition.family}`, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal }
    const result = offerFoundationWorldDelegatedTask(world, offer)
    const task = result.state.delegation.tasks.find(item => item.offerId === offer.id)!
    if (task.status === 'refused') return { world: result, task }
  }
  throw new Error(`fixture did not find refused offer for ${prefix}`)
}

describe('source-linked social memory', () => {
  it('derives deterministic accepted, refused, completed, and interrupted records from real delegation sources', () => {
    const accepted = acceptedOffer(selectedWorld('social-accepted'), 'social:accepted')
    const refusal = refusedOffer(selectedWorld('social-refusal'), 'social:refusal')
    const completed = advanceFoundationWorldTime(accepted.world, wait('social:complete', accepted.task.plannedCompletionAtWorldTime! - accepted.world.state.temporal.worldTime))
    const interruptedBase = acceptedOffer(selectedWorld('social-interrupt'), 'social:interrupt')
    const interrupted = interruptFoundationWorldDelegatedTask(interruptedBase.world, { version: DELEGATION_CONTRACT_VERSION, taskId: interruptedBase.task.id, courierId: interruptedBase.task.courierId, reason: 'courier-recall' })

    expect(accepted.world.state.socialMemory.records.map(record => record.phase)).toEqual(['offer-accepted'])
    expect(refusal.world.state.socialMemory.records.map(record => record.phase)).toEqual(['offer-refused'])
    expect(completed.state.socialMemory.records.map(record => record.phase)).toEqual(['offer-accepted', 'task-completed'])
    expect(interrupted.state.socialMemory.records.map(record => record.phase)).toEqual(['offer-accepted', 'task-interrupted'])

    for (const world of [accepted.world, refusal.world, completed, interrupted]) {
      expect(validateSocialMemoryState(contextFor(world), world.state.socialMemory)).toEqual([])
      expect(world.state.socialMemory.records.every(record => record.participantPersonIds.length === 2 && record.participantPersonIds.every(id => world.state.people.records.some(person => person.id === id && person.identity.adult)))).toBe(true)
      expect(world.state.socialMemory.records.every(record => record.knownAtWorldTime === record.occurredAtWorldTime && record.factors.every(factor => factor.startsWith('source-') || factor.startsWith('disposition-') || factor.startsWith('significance-')))).toBe(true)
    }
    const completedTask = completed.state.delegation.tasks.find(task => task.id === accepted.task.id)!
    expect(completed.state.socialMemory.records.find(record => record.phase === 'task-completed')).toEqual(socialMemoryRecordForTask(contextFor(completed), completedTask, 'task-completed'))
  })

  it('links the courier and recipient to records, preserves foundation history, and makes retained recall modest and inspectable', () => {
    const accepted = acceptedOffer(selectedWorld('social-recall'), 'social:recall')
    const complete = advanceFoundationWorldTime(accepted.world, wait('social:recall-complete', accepted.task.plannedCompletionAtWorldTime! - accepted.world.state.temporal.worldTime))
    const courier = complete.state.people.records.find(person => person.id === accepted.task.courierId)!
    const recipient = complete.state.people.records.find(person => person.id === accepted.task.recipientId)!
    const recall = socialMemoryRecallForPair(complete.state.socialMemory, courier.id, recipient.id)
    const assessment = assessCourierConversation(complete, {
      version: DELEGATION_CONTRACT_VERSION,
      courierId: courier.id,
      recipientId: recipient.id,
      proposal: proposalFor(accepted.task.proposal.materialInterest)
    })

    expect(courier.memories.some(memory => memory.kind === 'foundation-history')).toBe(true)
    expect(courier.memories.filter(memory => memory.kind === 'social-memory').every(memory => complete.state.socialMemory.records.some(record => record.id === memory.socialMemoryId && memory.relation === 'courier'))).toBe(true)
    expect(recipient.memories.filter(memory => memory.kind === 'social-memory').every(memory => complete.state.socialMemory.records.some(record => record.id === memory.socialMemoryId && memory.relation === 'recipient'))).toBe(true)
    expect(recall).toMatchObject({ band: 'supportive', occurredAtWorldTime: accepted.task.plannedCompletionAtWorldTime })
    expect(assessment.recipientRememberedContext).toBe('supportive')
    expect(assessment.factors).toContain('recipient-recall:supportive')
    expect(assessment.eligibility).toBe('eligible')
  })

  it('compacts personal links deterministically while retaining a typed household record and closed evidence routes', () => {
    const world = selectedWorld('social-retention')
    const courierId = world.state.courier.initialCourierId!
    const recipient = world.state.people.records.find(person => person.id !== courierId)!
    const definition = DELEGATION_TASK_DEFINITIONS.find(item => item.relevantMaterialInterests.every(interest => !recipient.materialInterests.includes(interest)))!
    const proposal = proposalFor(definition.relevantMaterialInterests[0]!)
    const assessment = assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId: recipient.id, proposal })
    let delegation = world.state.delegation
    let people = world.state.people.records
    let social = world.state.socialMemory
    for (let index = 0; index < SOCIAL_MEMORY_LIMITS.retainedPersonalLinks + 2; index++) {
      const offer: DelegationOfferInput = { version: DELEGATION_CONTRACT_VERSION, id: `social-retention:${String(index).padStart(2, '0')}`, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal }
      const transition = offerDelegatedTask({ worldId: world.id, creationDigest: world.manifest.creation.digest, worldTime: 0, people }, delegation, people, social, offer, assessment)
      delegation = transition.state
      people = transition.people
      social = transition.socialMemory
    }
    const retainedRecipient = people.find(person => person.id === recipient.id)!
    expect(retainedRecipient.memories.filter(memory => memory.kind === 'social-memory')).toHaveLength(SOCIAL_MEMORY_LIMITS.retainedPersonalLinks)
    expect(social.records).toHaveLength(SOCIAL_MEMORY_LIMITS.retainedPersonalLinks + 2)
    expect(social.records.filter(record => record.retention === 'household-record-only')).toHaveLength(2)
    expect(validateSocialMemoryState({ worldId: world.id, creationDigest: world.manifest.creation.digest, worldTime: 0, people, tasks: delegation.tasks }, social)).toEqual([])
    const routes = socialMemoryEvidenceRoutesFor(social.records[0]!)
    expect(routes.map(route => route.surface)).toEqual(SOCIAL_MEMORY_EVIDENCE_SURFACES)
    expect(routes.filter(route => route.availability === 'rendered-now').map(route => route.surface)).toEqual(['management-sidebar'])
  })

  it('rejects unsafe, forged, orphaned, reordered, and replay-mismatched social state without creating anyone new', () => {
    const { world } = acceptedOffer(selectedWorld('social-rejections'), 'social:rejections')
    const unsafe = structuredClone(world.state.socialMemory)
    ;(unsafe.records[0]!.contentSafety.exclusions as unknown as Record<string, string>).slavery = 'present'
    const forged = structuredClone(world.state.socialMemory)
    forged.records[0]!.token = 'forged'
    const orphaned = structuredClone(world.state.socialMemory)
    orphaned.records[0]!.retention = 'household-record-only'
    const reordered = structuredClone(world.state.socialMemory)
    reordered.records = [...reordered.records, structuredClone(reordered.records[0]!)]
    const corruptedWorld = structuredClone(world)
    corruptedWorld.state.socialMemory = forged

    expect(validateSocialMemoryState(contextFor(world), unsafe).map(item => item.code)).toContain('content-safety.prohibited.slavery')
    expect(validateSocialMemoryState(contextFor(world), forged).map(item => item.code)).toContain('social-memory.invalid-token')
    expect(validateSocialMemoryState(contextFor(world), orphaned).map(item => item.code)).toContain('social-memory.invalid-retention')
    expect(validateSocialMemoryState(contextFor(world), reordered).map(item => item.code)).toEqual(expect.arrayContaining(['social-memory.duplicate-record-id', 'social-memory.noncanonical-order']))
    expect(validateFoundationWorld(corruptedWorld).map(item => item.code)).toContain('foundation-world.invalid-mutable-state')
    expect(world.state.people.records.map(person => person.id)).toEqual(world.crew.map(member => member.id).sort())
  })

  it('keeps social records in the causal replay/checkpoint projection without a derived command', () => {
    let world = acceptedOffer(selectedWorld('social-replay'), 'social:replay').world
    for (let index = 0; index < 9; index++) world = advanceFoundationWorldTime(world, wait(`social:replay:${index}`, 1))

    expect(world.state.causalHistory.tail.every(command => command.kind !== ('social-memory' as never))).toBe(true)
    expect(replayFoundationWorldCausalHistory(world)).toEqual(causalReplayProjectionForWorldState(world.state))
    expect(foundationWorldCausalHistoryMatches(world)).toBe(true)
    expect(validateFoundationWorld(world)).toEqual([])
  }, 15_000)
})
