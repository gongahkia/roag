import { describe, expect, it } from 'vitest'
import { assessCourierConversation } from './conversation'
import { classifyMedievalContent } from './content-safety'
import { DELEGATION_CONTRACT_VERSION, DELEGATION_TASK_DEFINITIONS, type DelegatedTaskStatus, type DelegationOfferInput } from './delegation'
import { createManagementSidebarModel } from './management-sidebar'
import { autonomyProjection } from './autonomy'
import { simulationCatchUpProjection } from './simulation-catchup'
import { socialMemoryRecallForPair } from './social-memory'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, foundationWorldCausalHistoryMatches, interruptFoundationWorldDelegatedTask, offerFoundationWorldDelegatedTask, replayFoundationWorldCausalHistory, validateFoundationWorld } from './world'
import { causalReplayProjectionForWorldState } from './world-state'
import { worldEraProjection } from './world-era'
import type { FoundationWorld } from './types'

const safety = () => classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const wait = (id: string, durationMinutes: number) => ({ id, kind: 'wait' as const, durationMinutes, contentSafety: safety() })
const selectedWorld = (seed: string) => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset: 'far-coast' } }), 'crew:0')

const proposalFor = (materialInterest: DelegationOfferInput['proposal']['materialInterest']) => ({
  version: DELEGATION_CONTRACT_VERSION,
  kind: 'request' as const,
  urgency: 'routine' as const,
  complexity: 'routine' as const,
  materialInterest,
  contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
})

/**
 * Deliberately discovers public outcomes rather than reproducing delegation's
 * private random-stream formula. Each attempted API call receives the same
 * immutable source world, so discovery does not mutate the fixture.
 */
const discoverOffer = (
  world: FoundationWorld,
  prefix: string,
  status: DelegatedTaskStatus,
  acceptsTask: (task: { readonly plannedCompletionAtWorldTime?: number }) => boolean = () => true
) => {
  const courierId = world.state.courier.initialCourierId
  if (courierId === undefined) throw new Error('fixture requires an active courier')
  let ordinal = 0
  for (const recipient of [...world.state.people.records].sort((left, right) => left.id.localeCompare(right.id))) {
    if (recipient.id === courierId) continue
    for (const definition of DELEGATION_TASK_DEFINITIONS) {
      const matchingInterest = definition.relevantMaterialInterests.find(interest => recipient.materialInterests.includes(interest))
      const matchingSkill = definition.relevantSkills.some(skill => recipient.work.skills.some(candidate => candidate.kind === skill && candidate.level >= 1))
      const materialInterest = status === 'in-progress'
        ? matchingInterest
        : definition.relevantMaterialInterests.every(interest => !recipient.materialInterests.includes(interest))
          ? definition.relevantMaterialInterests[0]
          : undefined
      if (materialInterest === undefined || (status === 'in-progress' && !matchingSkill)) continue
      for (let attempt = 0; attempt < 8; attempt++) {
        const proposal = proposalFor(materialInterest)
        const assessment = assessCourierConversation(world, {
          version: DELEGATION_CONTRACT_VERSION,
          courierId,
          recipientId: recipient.id,
          proposal
        })
        if (assessment.eligibility !== 'eligible' || !assessment.unlockedApproaches.includes('direct-request')) continue
        const offer: DelegationOfferInput = {
          version: DELEGATION_CONTRACT_VERSION,
          id: `${prefix}:${recipient.id}:${definition.family}:${String(ordinal++).padStart(2, '0')}`,
          courierId,
          recipientId: recipient.id,
          family: definition.family,
          approach: 'direct-request',
          proposal
        }
        const next = offerFoundationWorldDelegatedTask(world, offer)
        const task = next.state.delegation.tasks.find(candidate => candidate.offerId === offer.id)
        if (task?.status === status && acceptsTask(task)) return { world: next, offer, task }
      }
    }
  }
  throw new Error(`fixture did not discover a ${status} offer`)
}

/** A mismatch is a truthful, deterministic refusal without guessing any roll. */
const refusalOfferFor = (world: FoundationWorld, id: string, recipientId?: string): DelegationOfferInput => {
  const courierId = world.state.courier.initialCourierId
  if (courierId === undefined) throw new Error('fixture requires an active courier')
  const recipients = [...world.state.people.records]
    .filter(person => person.id !== courierId && (recipientId === undefined || person.id === recipientId))
    .sort((left, right) => left.id.localeCompare(right.id))
  for (const recipient of recipients) {
    const definition = DELEGATION_TASK_DEFINITIONS.find(candidate => candidate.relevantMaterialInterests.every(interest => !recipient.materialInterests.includes(interest)))
    if (!definition) continue
    const proposal = proposalFor(definition.relevantMaterialInterests[0]!)
    const assessment = assessCourierConversation(world, {
      version: DELEGATION_CONTRACT_VERSION,
      courierId,
      recipientId: recipient.id,
      proposal
    })
    if (assessment.eligibility === 'eligible' && assessment.unlockedApproaches.includes('direct-request')) {
      return { version: DELEGATION_CONTRACT_VERSION, id, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal }
    }
  }
  throw new Error('fixture did not find an eligible material-interest mismatch')
}

const simulationProjectionFor = (world: FoundationWorld) => simulationCatchUpProjection({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: world.state.temporal.worldTime
}, world.state.simulation)

const canonicalDelegationProjection = (world: FoundationWorld) => ({
  delegation: world.state.delegation,
  people: world.state.people,
  socialMemory: world.state.socialMemory,
  autonomy: autonomyProjection(world.state.autonomy),
  simulation: simulationProjectionFor(world),
  era: worldEraProjection(world.state.era)
})

describe('delegation/social/autonomy cross-contract regressions', () => {
  it('reproduces public offers and canonical task completion across equivalent time partitions', () => {
    const firstSource = selectedWorld('delegation-cross-lifecycle')
    const secondSource = selectedWorld('delegation-cross-lifecycle')
    const first = discoverOffer(firstSource, 'cross-accepted', 'in-progress')
    const second = offerFoundationWorldDelegatedTask(secondSource, first.offer)
    const secondTask = second.state.delegation.tasks.find(task => task.id === first.task.id)
    if (!secondTask) throw new Error('repeated valid offer must retain its stable task ID')

    expect(second).toEqual(first.world)
    expect(secondTask.agreement.status).toBe('accepted')
    expect(first.world.state.causalHistory.tail.map(command => command.kind)).toContain('delegation-offered')
    expect(first.world.state.simulation.delegatedWork.some(item => item.id === first.task.id && item.status === 'active')).toBe(true)
    expect(first.world.state.people.records.find(person => person.id === first.task.recipientId)?.work.current.status).toBe('committed')

    const remaining = first.task.plannedCompletionAtWorldTime! - first.world.state.temporal.worldTime
    const oneAction = advanceFoundationWorldTime(first.world, wait('cross-completion-once', remaining))
    const partitioned = advanceFoundationWorldTime(
      advanceFoundationWorldTime(first.world, wait('cross-completion-first', 2)),
      wait('cross-completion-second', remaining - 2)
    )
    const completedTask = oneAction.state.delegation.tasks.find(task => task.id === first.task.id)
    if (!completedTask) throw new Error('completed task must remain in the bounded registry')

    expect(completedTask).toMatchObject({ status: 'completed', outcome: { kind: 'completed', atWorldTime: first.task.plannedCompletionAtWorldTime } })
    expect(oneAction.state.people.records.find(person => person.id === first.task.recipientId)?.work).toMatchObject({ availability: 'available', current: { status: 'idle' } })
    expect(oneAction.state.simulation.delegatedWork.find(item => item.id === first.task.id)).toMatchObject({ status: 'resolved', resolvedAtWorldTime: completedTask.outcome?.atWorldTime })
    expect(oneAction.state.socialMemory.records.some(record => record.source.taskId === first.task.id && record.phase === 'offer-accepted')).toBe(true)
    expect(oneAction.state.socialMemory.records.some(record => record.source.taskId === first.task.id && record.phase === 'task-completed')).toBe(true)
    expect(canonicalDelegationProjection(partitioned)).toEqual(canonicalDelegationProjection(oneAction))
    expect(partitioned.state.temporal.causalRecords).not.toEqual(oneAction.state.temporal.causalRecords)
    expect(partitioned.state.causalHistory.tail).not.toEqual(oneAction.state.causalHistory.tail)
    expect(replayFoundationWorldCausalHistory(oneAction)).toEqual(causalReplayProjectionForWorldState(oneAction.state))
    expect(foundationWorldCausalHistoryMatches(oneAction)).toBe(true)
    expect(validateFoundationWorld(partitioned)).toEqual([])
  }, 20_000)

  it('keeps refusal and interruption inspectable, while a due-crossing recall completes without emitting interruption evidence', () => {
    const source = selectedWorld('delegation-cross-interruption')
    const refusal = discoverOffer(source, 'cross-refusal', 'refused')
    expect(refusal.task).toMatchObject({ status: 'refused', outcome: { kind: 'refused' } })
    expect(refusal.world.state.socialMemory.records.find(record => record.source.taskId === refusal.task.id)).toMatchObject({ phase: 'offer-refused', disposition: 'declined' })

    const dueSource = discoverOffer(source, 'cross-due', 'in-progress')
    const beforeDue = advanceFoundationWorldTime(dueSource.world, wait('cross-due-before', dueSource.task.plannedCompletionAtWorldTime! - dueSource.world.state.temporal.worldTime - 1))
    const beforeRejectedRecall = structuredClone(beforeDue)
    expect(() => interruptFoundationWorldDelegatedTask(beforeDue, {
      version: DELEGATION_CONTRACT_VERSION,
      taskId: dueSource.task.id,
      courierId: dueSource.task.courierId,
      reason: 'courier-recall'
    })).toThrow('delegation.task-not-active')
    expect(beforeDue).toEqual(beforeRejectedRecall)

    const dueCompleted = advanceFoundationWorldTime(beforeDue, wait('cross-due-complete', 1))
    expect(dueCompleted.state.delegation.tasks.find(task => task.id === dueSource.task.id)).toMatchObject({ status: 'completed' })
    expect(dueCompleted.state.socialMemory.records.some(record => record.source.taskId === dueSource.task.id && record.phase === 'task-interrupted')).toBe(false)

    const active = discoverOffer(selectedWorld('delegation-cross-real-interruption'), 'cross-interrupt', 'in-progress')
    const interrupted = interruptFoundationWorldDelegatedTask(active.world, {
      version: DELEGATION_CONTRACT_VERSION,
      taskId: active.task.id,
      courierId: active.task.courierId,
      reason: 'courier-cancellation'
    })
    const task = interrupted.state.delegation.tasks.find(candidate => candidate.id === active.task.id)
    if (!task) throw new Error('interrupted task must remain inspectable')
    expect(task).toMatchObject({ status: 'interrupted', outcome: { kind: 'interrupted', reason: 'courier-cancellation' } })
    expect(interrupted.state.people.records.find(person => person.id === task.recipientId)?.commitments.some(commitment => commitment.id.includes(task.id) && commitment.status === 'cancelled')).toBe(true)
    expect(interrupted.state.simulation.delegatedWork.find(item => item.id === task.id)).toMatchObject({ status: 'resolved', resolvedAtWorldTime: task.outcome?.atWorldTime })
    expect(interrupted.state.socialMemory.records.find(record => record.source.taskId === task.id && record.phase === 'task-interrupted')).toMatchObject({ disposition: 'interrupted', occurredAtWorldTime: task.outcome?.atWorldTime })
    expect(interrupted.state.causalHistory.tail.map(command => command.kind)).toContain('delegation-interrupted')
    expect(validateFoundationWorld(interrupted)).toEqual([])
  })

  it('routes the latest retained shared-work result consistently into recall, conversation context, and the sidebar without changing relationship authority', () => {
    const accepted = discoverOffer(
      selectedWorld('delegation-cross-recall'),
      'cross-recall-accepted',
      'in-progress',
      task => task.plannedCompletionAtWorldTime !== undefined && task.plannedCompletionAtWorldTime < 29
    )
    const completed = advanceFoundationWorldTime(accepted.world, wait('cross-recall-complete', accepted.task.plannedCompletionAtWorldTime! - accepted.world.state.temporal.worldTime))
    const courierId = accepted.task.courierId
    const recipientId = accepted.task.recipientId
    const cooperativeRecall = socialMemoryRecallForPair(completed.state.socialMemory, courierId, recipientId)
    expect(cooperativeRecall.band).toBe('supportive')

    const refusalOffer = refusalOfferFor(completed, 'cross-recall-refusal', recipientId)
    const relationships = structuredClone(completed.state.people.records.find(person => person.id === recipientId)?.relationships)
    const refused = offerFoundationWorldDelegatedTask(completed, refusalOffer)
    const refusedTask = refused.state.delegation.tasks.find(task => task.offerId === refusalOffer.id)
    if (!refusedTask) throw new Error('refusal must retain a real task record')
    const recall = socialMemoryRecallForPair(refused.state.socialMemory, courierId, recipientId)
    const afterAssessment = assessCourierConversation(refused, {
      version: DELEGATION_CONTRACT_VERSION,
      courierId,
      recipientId,
      proposal: refusalOffer.proposal
    })
    const sourceRecord = refused.state.socialMemory.records.find(record => record.id === recall.recordId)
    const sidebarRecord = createManagementSidebarModel(refused).sections
      .find(section => section.id === 'history')?.facts
      .find(fact => fact.value.kind === 'social-memory' && fact.value.socialMemoryId === recall.recordId)

    expect(refusedTask.status).toBe('refused')
    expect(recall).toMatchObject({ band: 'unsettled', occurredAtWorldTime: refusedTask.outcome?.atWorldTime })
    expect(afterAssessment.recipientRememberedContext).toBe('unsettled')
    expect(afterAssessment.factors).toContain('recipient-recall:unsettled')
    expect(refused.state.people.records.find(person => person.id === recipientId)?.relationships).toEqual(relationships)
    expect(sourceRecord).toBeDefined()
    expect(sidebarRecord).toMatchObject({
      source: { type: 'social-memory-record', label: 'social-memory', recordId: sourceRecord?.id },
      recordedAtWorldTime: sourceRecord?.occurredAtWorldTime,
      discoveredAtWorldTime: sourceRecord?.knownAtWorldTime,
      freshness: { kind: 'timeless' }
    })
    expect(sidebarRecord?.value).toMatchObject({ taskId: refusedTask.id, phase: 'offer-refused', disposition: 'declined' })

    const unremembered = selectedWorld('delegation-cross-recall-step')
    const stepOffer = refusalOfferFor(unremembered, 'cross-recall-step-refusal')
    const baseline = assessCourierConversation(unremembered, {
      version: DELEGATION_CONTRACT_VERSION,
      courierId: stepOffer.courierId,
      recipientId: stepOffer.recipientId,
      proposal: stepOffer.proposal
    })
    const remembered = offerFoundationWorldDelegatedTask(unremembered, stepOffer)
    const recalled = assessCourierConversation(remembered, {
      version: DELEGATION_CONTRACT_VERSION,
      courierId: stepOffer.courierId,
      recipientId: stepOffer.recipientId,
      proposal: stepOffer.proposal
    })
    const readinessOrder = ['unlikely', 'conditional', 'open'] as const
    const baselineReadiness = readinessOrder.indexOf(baseline.agreementReadiness as typeof readinessOrder[number])
    const recalledReadiness = readinessOrder.indexOf(recalled.agreementReadiness as typeof readinessOrder[number])
    expect(baseline.recipientRememberedContext).toBe('none')
    expect(recalled.recipientRememberedContext).toBe('unsettled')
    expect(Math.abs(recalledReadiness - baselineReadiness)).toBeLessThanOrEqual(1)
  })

  it('derives autonomy only from time-bearing commands, gives active delegated work precedence, and rejects forged derived state', () => {
    const source = selectedWorld('delegation-cross-autonomy')
    const pureBefore = structuredClone(source)
    const recipientId = source.state.people.records.find(person => person.id !== source.state.courier.initialCourierId)!.id
    const probe = refusalOfferFor(source, 'cross-autonomy-probe', recipientId)
    expect(assessCourierConversation(source, {
      version: DELEGATION_CONTRACT_VERSION,
      courierId: probe.courierId,
      recipientId: probe.recipientId,
      proposal: probe.proposal
    }).eligibility).toBe('eligible')
    expect(createManagementSidebarModel(source).worldId).toBe(source.id)
    expect(source).toEqual(pureBefore)

    const active = discoverOffer(source, 'cross-autonomy-accepted', 'in-progress')
    const elapsedBeforeCompletion = Math.min(4, active.task.plannedCompletionAtWorldTime! - active.world.state.temporal.worldTime - 1)
    const underway = advanceFoundationWorldTime(active.world, wait('cross-autonomy-underway', elapsedBeforeCompletion))
    const activeObservation = underway.state.autonomy.observations.find(observation => observation.personId === active.task.recipientId)
    expect(activeObservation?.choice).toBe('continue-accepted-delegated-work')
    expect(underway.state.delegation.tasks.map(task => task.id)).toEqual([active.task.id])
    expect(underway.state.socialMemory.records.every(record => record.source.taskId === active.task.id)).toBe(true)
    expect(underway.state.causalHistory.tail.every(command => command.kind !== ('autonomy-choice' as never))).toBe(true)

    const completed = advanceFoundationWorldTime(underway, wait('cross-autonomy-finish', active.task.plannedCompletionAtWorldTime! - underway.state.temporal.worldTime))
    const completedObservation = completed.state.autonomy.observations.find(observation => observation.personId === active.task.recipientId)
    expect(completedObservation?.choice).not.toBe('continue-accepted-delegated-work')
    expect(completed.state.people.records.map(person => person.id)).toEqual(source.state.people.records.map(person => person.id))
    expect(completed.initialWorld.people.every(seed => !completed.state.people.records.some(person => person.id === seed.id))).toBe(true)
    expect(completed.state.geography).toEqual(source.state.geography)
    expect(completed.state.routes).toEqual(source.state.routes)
    expect(completed.state.markets).toEqual(source.state.markets)
    expect(completed.jomon).toEqual(source.jomon)

    const forged = structuredClone(completed)
    if (!forged.state.autonomy.observations[0]) throw new Error('fixture requires an autonomy observation')
    ;(forged.state.autonomy.observations[0] as { choice: string }).choice = 'caller-selected-npc-choice'
    const untouched = structuredClone(forged)
    expect(validateFoundationWorld(forged)).not.toEqual([])
    expect(() => advanceFoundationWorldTime(forged, wait('cross-autonomy-forged', 1))).toThrow('complete medieval foundation contract')
    expect(forged).toEqual(untouched)
  })
})
