import { describe, expect, it } from 'vitest'
import { auditMedievalContentSafety, classifyMedievalContent } from './content-safety'
import { CONVERSATION_CONTRACT_VERSION, CONVERSATION_FACTOR_CODES, CONVERSATION_LEVELS, ConversationContractError, assessCourierConversation, conversationCapabilitiesForLevel, validateConversationAssessmentRequest, type ConversationAssessmentRequest, type ConversationProposal } from './conversation'
import type { PersistentPersonRecord } from './persistent-person'
import { PERSISTENT_PERSON_MATERIAL_INTERESTS } from './persistent-person'
import { medievalWorldStateContentRecords } from './world-state'
import { chooseInitialCourier, createFoundationWorld, validateFoundationWorld } from './world'
import type { FoundationWorld } from './types'

const proposal = (overrides: Partial<ConversationProposal> = {}): ConversationProposal => ({
  version: CONVERSATION_CONTRACT_VERSION,
  kind: 'request',
  urgency: 'routine',
  complexity: 'routine',
  materialInterest: 'route-safety',
  contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data']),
  ...overrides
})

const selectedWorld = (seed = 'conversation-contract'): FoundationWorld => chooseInitialCourier(createFoundationWorld({ seed }), 'crew:0')
const recipientFor = (world: FoundationWorld): PersistentPersonRecord => world.state.people.records.find(person => person.id !== world.state.courier.initialCourierId)!
const requestFor = (world: FoundationWorld, recipientId = recipientFor(world).id, proposalOverrides: Partial<ConversationProposal> = {}): ConversationAssessmentRequest => ({
  version: CONVERSATION_CONTRACT_VERSION,
  courierId: world.state.courier.initialCourierId!,
  recipientId,
  proposal: proposal(proposalOverrides)
})

const worldWithCourierLevel = (level: number): FoundationWorld => {
  for (let attempt = 0; attempt < 24; attempt++) {
    const world = createFoundationWorld({ seed: `conversation-level:${level}:${attempt}` })
    const courier = world.state.people.records.find(person => person.identity.conversation === level)
    if (courier) return chooseInitialCourier(world, courier.id)
  }
  throw new Error(`fixture did not find courier level ${level}`)
}

const withPeople = (world: FoundationWorld, change: (people: PersistentPersonRecord[]) => void): FoundationWorld => {
  const next = structuredClone(world)
  change(next.state.people.records as PersistentPersonRecord[])
  const audit = auditMedievalContentSafety(medievalWorldStateContentRecords(next.state))
  if (audit.status === 'rejected') throw new Error('test fixture must remain content-safe')
  next.state.contentSafetyAudit = audit
  expect(validateFoundationWorld(next)).toEqual([])
  return next
}

const withRecipient = (world: FoundationWorld, change: (recipient: PersistentPersonRecord) => void): FoundationWorld => withPeople(world, people => change(people.find(person => person.id === recipientFor(world).id)!))

const activeCommitment = (person: PersistentPersonRecord) => ({
  id: `${person.id}:commitment:conversation`,
  kind: 'vessel-duty' as const,
  status: 'active' as const,
  createdAtWorldTime: 0,
  detail: 'Keep the mooring watch.',
  contentSafety: classifyMedievalContent('contract', ['adult-labour', 'navigation'], 'adults-only', ['person'])
})

const injuredHealth = (person: PersistentPersonRecord) => ({
  condition: 'injured' as const,
  injuries: [{
    id: `${person.id}:injury:conversation`,
    kind: 'minor-wound' as const,
    receivedAtWorldTime: 0,
    recovery: 'none' as const,
    contentSafety: classifyMedievalContent('hazard', ['environment', 'ordinary-hardship'], 'adults-only', ['person'])
  }],
  recovery: { status: 'none' as const }
})

const worldWithRecipientStanding = (standing: -2 | 0 | 2): { world: FoundationWorld; courierId: string; recipientId: string } => {
  for (let attempt = 0; attempt < 24; attempt++) {
    const initial = createFoundationWorld({ seed: `conversation-standing:${standing}:${attempt}` })
    for (const recipient of initial.state.people.records) {
      const relationship = recipient.relationships.find(item => item.standing === standing)
      if (relationship) {
        const world = chooseInitialCourier(initial, relationship.targetPersonId)
        return { world, courierId: relationship.targetPersonId, recipientId: recipient.id }
      }
    }
  }
  throw new Error(`fixture did not find recipient standing ${standing}`)
}

describe('courier conversation assessment', () => {
  it('maps every authoritative persistent conversation level to bounded approaches, clarity, and readiness support', () => {
    const expected = [
      { level: 1, approaches: ['direct-request'], clarity: 'limited', readinessSupport: 'minimal' },
      { level: 2, approaches: ['direct-request', 'shared-context'], clarity: 'basic', readinessSupport: 'limited' },
      { level: 3, approaches: ['direct-request', 'shared-context', 'terms-outline'], clarity: 'clear', readinessSupport: 'clear' },
      { level: 4, approaches: ['direct-request', 'shared-context', 'terms-outline', 'contingency-check'], clarity: 'precise', readinessSupport: 'strong' },
      { level: 5, approaches: ['direct-request', 'shared-context', 'terms-outline', 'contingency-check', 'reciprocal-options'], clarity: 'thorough', readinessSupport: 'expert' }
    ] as const
    expect(CONVERSATION_LEVELS.map(level => conversationCapabilitiesForLevel(level))).toEqual(expected)
    for (const capability of expected) {
      const world = worldWithCourierLevel(capability.level)
      const assessment = assessCourierConversation(world, requestFor(world))
      expect(assessment).toMatchObject({
        eligibility: 'eligible',
        taskClarity: capability.clarity,
        unlockedApproaches: capability.approaches
      })
    }
    expect(() => conversationCapabilitiesForLevel(6)).toThrow('conversation.invalid-conversation-level')
  })

  it('assesses a co-located living crew recipient deterministically without mutating world, time, scheduler, era, people, or journal state', () => {
    const world = selectedWorld()
    const recipient = recipientFor(world)
    const request = requestFor(world, recipient.id, { materialInterest: recipient.materialInterests[0]! })
    const before = structuredClone(world)
    const first = assessCourierConversation(world, request)
    const reorderedRequest = { proposal: request.proposal, recipientId: request.recipientId, courierId: request.courierId, version: request.version }
    const second = assessCourierConversation(structuredClone(world), reorderedRequest)

    expect(first).toEqual(second)
    expect(first).toMatchObject({ eligibility: 'eligible', barriers: [], recipientInterestAlignment: 'aligned' })
    expect(first.factors).toEqual([...first.factors].sort())
    expect(new Set(first.factors).size).toBe(first.factors.length)
    expect(first.factors.every(code => CONVERSATION_FACTOR_CODES.includes(code))).toBe(true)
    expect(first.factors).toEqual(expect.arrayContaining([
      `courier-level:${world.state.people.records.find(person => person.id === request.courierId)!.identity.conversation}`,
      'proposal-kind:request',
      'recipient-interest:aligned',
      'eligibility:eligible',
      `clarity:${first.taskClarity}`,
      `risk:${first.risk}`,
      `readiness:${first.agreementReadiness}`
    ]))
    expect(auditMedievalContentSafety([{ id: 'conversation:assessment', domain: 'contract', classification: first.contentSafety }]).status).toBe('accepted')
    expect(world).toEqual(before)
    expect(world.state.temporal).toEqual(before.state.temporal)
    expect(world.state.simulation).toEqual(before.state.simulation)
    expect(world.state.era).toEqual(before.state.era)
    expect(world.state.causalHistory).toEqual(before.state.causalHistory)
    expect(world.state.people).toEqual(before.state.people)
  })

  it('makes recipient-directed relationship and material interests constrain readiness without resolving agreement', () => {
    const expected = new Map<(-2 | 0 | 2), 'strained' | 'unformed' | 'strong'>([[-2, 'strained'], [0, 'unformed'], [2, 'strong']])
    for (const standing of [-2, 0, 2] as const) {
      const specimen = worldWithRecipientStanding(standing)
      const recipient = specimen.world.state.people.records.find(person => person.id === specimen.recipientId)!
      const aligned = assessCourierConversation(specimen.world, {
        version: 1,
        courierId: specimen.courierId,
        recipientId: recipient.id,
        proposal: proposal({ materialInterest: recipient.materialInterests[0]! })
      })
      const notAlignedInterest = PERSISTENT_PERSON_MATERIAL_INTERESTS.find(interest => !recipient.materialInterests.includes(interest))!
      const notAligned = assessCourierConversation(specimen.world, {
        version: 1,
        courierId: specimen.courierId,
        recipientId: recipient.id,
        proposal: proposal({ materialInterest: notAlignedInterest })
      })

      expect(aligned.recipientRapport).toBe(expected.get(standing))
      expect(aligned.recipientInterestAlignment).toBe('aligned')
      expect(notAligned.recipientInterestAlignment).toBe('not-aligned')
      expect(notAligned.agreementReadiness).not.toBe('open')
    }
  })

  it('returns explicit blockers for missing/non-active/self/dead/separated/unavailable/over-capacity/committed recipients and protected needs, safety, or health', () => {
    const world = selectedWorld('conversation-barriers')
    const recipient = recipientFor(world)

    expect(assessCourierConversation(createFoundationWorld({ seed: 'conversation-unselected' }), {
      version: 1, courierId: 'crew:0', recipientId: 'crew:1', proposal: proposal()
    }).barriers).toContain('active-courier-missing')
    expect(assessCourierConversation(world, { ...requestFor(world), courierId: 'crew:2' }).barriers).toContain('courier-not-active')
    expect(assessCourierConversation(world, { ...requestFor(world), courierId: 'crew:missing' }).barriers).toContain('courier-missing')
    expect(assessCourierConversation(world, { ...requestFor(world), recipientId: 'person:missing' }).barriers).toContain('recipient-missing')
    expect(assessCourierConversation(world, requestFor(world, world.state.courier.initialCourierId!)).barriers).toContain('recipient-is-courier')

    const deadCourierBase = createFoundationWorld({ seed: 'conversation-dead-courier' })
    const deadCourier = withPeople(deadCourierBase, people => {
      const person = people.find(candidate => candidate.id === 'crew:0')!
      person.life = { status: 'dead', birth: person.life.birth, death: { atWorldTime: 0 } }
      person.work.availability = 'unavailable'
    })
    expect(assessCourierConversation(deadCourier, { version: 1, courierId: 'crew:0', recipientId: 'crew:1', proposal: proposal() }).barriers).toEqual(expect.arrayContaining(['active-courier-missing', 'courier-dead', 'courier-unavailable']))

    const deadRecipient = withRecipient(world, person => {
      person.life = { status: 'dead', birth: person.life.birth, death: { atWorldTime: 0 } }
      person.work.availability = 'unavailable'
    })
    const separated = withRecipient(world, person => { person.location = { kind: 'site', id: world.state.sites.sites[0]!.id } })
    const unavailable = withRecipient(world, person => { person.work.availability = 'unavailable' })
    const noCapacity = withRecipient(world, person => {
      person.work.capacity.current = 0
      person.work.availability = 'unavailable'
    })
    const committed = withRecipient(world, person => {
      const commitment = activeCommitment(person)
      person.commitments = [commitment]
      person.work = { ...person.work, current: { status: 'committed', commitmentId: commitment.id, startedAtWorldTime: 0 }, availability: 'committed' }
    })
    const needsProtected = withRecipient(world, person => { person.needs = { ...person.needs, nourishment: 5 } })
    const safetyProtected = withRecipient(world, person => { person.needs = { ...person.needs, safety: 5 } })
    const injured = withRecipient(world, person => { person.health = injuredHealth(person) })
    const results: readonly [FoundationWorld, string][] = [
      [deadRecipient, 'recipient-dead'],
      [separated, 'recipient-not-colocated'],
      [unavailable, 'recipient-unavailable'],
      [noCapacity, 'recipient-no-capacity'],
      [committed, 'recipient-current-work'],
      [committed, 'recipient-active-commitment'],
      [needsProtected, 'recipient-needs-protected'],
      [safetyProtected, 'recipient-safety-protected'],
      [injured, 'recipient-health-protected']
    ]
    for (const [specimen, barrier] of results) {
      const assessment = assessCourierConversation(specimen, requestFor(specimen, recipient.id, { materialInterest: recipient.materialInterests[0]! }))
      expect(assessment).toMatchObject({ eligibility: 'blocked', agreementReadiness: 'not-assessable' })
      expect(assessment.barriers).toContain(barrier)
    }
  })

  it('treats recovery and ordinary needs as risk/readiness constraints, and maximum conversation cannot bypass hard recipient blockers', () => {
    const world = selectedWorld('conversation-pressure')
    const recipient = recipientFor(world)
    const recovering = withRecipient(world, person => {
      person.health = {
        condition: 'recovering',
        injuries: [{ ...injuredHealth(person).injuries[0]!, recovery: 'recovering' }],
        recovery: { status: 'recovering', injuryId: `${person.id}:injury:conversation`, completeAtWorldTime: 0 }
      }
    })
    const pressured = withRecipient(world, person => { person.needs = { ...person.needs, rest: 3 } })
    const recoveringAssessment = assessCourierConversation(recovering, requestFor(recovering, recipient.id, { materialInterest: recipient.materialInterests[0]! }))
    const pressuredAssessment = assessCourierConversation(pressured, requestFor(pressured, recipient.id, { materialInterest: recipient.materialInterests[0]! }))
    expect(recoveringAssessment).toMatchObject({ eligibility: 'eligible', recipientHealth: 'recovering' })
    expect(pressuredAssessment).toMatchObject({ eligibility: 'eligible', recipientNeedsPressure: 'pressured' })
    expect(recoveringAssessment.risk).not.toBe('low')
    expect(pressuredAssessment.agreementReadiness).not.toBe('open')

    let maximum: FoundationWorld | undefined
    for (let attempt = 0; attempt < 24 && maximum === undefined; attempt++) {
      const initial = createFoundationWorld({ seed: `conversation-maximum:${attempt}` })
      const courier = initial.state.people.records.find(person => person.identity.conversation === 5)
      if (courier) maximum = chooseInitialCourier(initial, courier.id)
    }
    if (!maximum) throw new Error('fixture did not generate a level-five courier')
    const maximumRecipient = recipientFor(maximum)
    const blockedMaximum = withRecipient(maximum, person => {
      if (person.id !== maximumRecipient.id) return
      person.work.availability = 'unavailable'
    })
    const assessment = assessCourierConversation(blockedMaximum, requestFor(blockedMaximum, maximumRecipient.id, { materialInterest: maximumRecipient.materialInterests[0]! }))
    expect(assessment.unlockedApproaches).toHaveLength(5)
    expect(assessment).toMatchObject({ eligibility: 'blocked', agreementReadiness: 'not-assessable' })
    expect(assessment.barriers).toContain('recipient-unavailable')
  })

  it('rejects malformed worlds and proposal/request values fail-closed with canonical readable diagnostics', () => {
    const world = selectedWorld('conversation-rejection')
    const validRequest = requestFor(world)
    const malformedProposal = { ...validRequest, proposal: { ...validRequest.proposal, kind: 'force' } }
    const unsafeProposal = { ...validRequest, proposal: { ...validRequest.proposal, contentSafety: undefined } }
    const malformedWorld = { version: 10 }

    expect(validateConversationAssessmentRequest(world, malformedProposal).map(item => item.code)).toEqual(['conversation.invalid-proposal-kind'])
    expect(validateConversationAssessmentRequest(world, unsafeProposal).map(item => item.code)).toEqual(['content-safety.missing-classification'])
    expect(validateConversationAssessmentRequest(world, { ...validRequest, courierId: 'not a valid id' }).map(item => item.code)).toEqual(['conversation.invalid-courier-id'])
    expect(validateConversationAssessmentRequest(malformedWorld, validRequest).map(item => item.code)).toEqual(['conversation.invalid-world'])
    for (const [candidateWorld, candidateRequest] of [[world, malformedProposal], [world, unsafeProposal], [malformedWorld, validRequest]] as const) {
      try {
        assessCourierConversation(candidateWorld, candidateRequest)
        throw new Error('malformed assessment should reject')
      } catch (error) {
        expect(error).toBeInstanceOf(ConversationContractError)
        const diagnostics = (error as ConversationContractError).diagnostics
        expect(diagnostics).toEqual([...diagnostics].sort((left, right) => left.recordId.localeCompare(right.recordId) || left.code.localeCompare(right.code)))
      }
    }
  })
})
