import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld } from './world'
import {
  COURIER_LOSS_CREW_EXTINCTION_REASON,
  COURIER_LOSS_LASTING_CONSEQUENCE_OBLIGATIONS,
  COURIER_LOSS_POLICY_VERSION,
  assessCourierLoss,
  validateCourierLossAssessmentRequest,
  type CourierLossAssessmentRequest,
  type CourierLossSafeguardReservation
} from './courier-loss-policy'
import { MYSTICAL_EFFECT_POLICY_VERSION, type MysticalEffectPolicyAuditRecord } from './mystical-effect-policy'
import type { PersistentPersonRecord, PersistentPersonValidationContext } from './persistent-person'

const lossSafety = () => classifyMedievalContent('event', ['ordinary-hardship'], 'adults-only')
const actionSafety = () => classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])

const sourceWorld = () => {
  const selected = chooseInitialCourier(createFoundationWorld({ seed: 'courier-loss-policy' }), 'crew:0')
  return advanceFoundationWorldTime(selected, {
    id: 'wait:courier-loss-policy',
    kind: 'wait',
    durationMinutes: 12,
    contentSafety: actionSafety()
  })
}

const peopleContext = (world: ReturnType<typeof sourceWorld>): PersistentPersonValidationContext => ({
  seed: world.manifest.creation.seed,
  configurationFingerprint: world.manifest.creation.configurationFingerprint,
  initialWorld: world.initialWorld,
  frontier: world.state.geography.frontier,
  jomon: world.jomon,
  crew: world.crew,
  siteIds: world.state.sites.sites.map(site => site.id),
  worldTime: world.state.temporal.worldTime
})

const confirmedLossPeople = (world: ReturnType<typeof sourceWorld>): readonly PersistentPersonRecord[] => {
  const people = structuredClone(world.state.people.records)
  const courier = people.find(person => person.id === 'crew:0')!
  courier.life = { ...courier.life, status: 'dead', death: { atWorldTime: world.state.temporal.worldTime } }
  courier.work = { ...courier.work, availability: 'unavailable', current: { status: 'idle' } }
  return people
}

const policyRecord = (): MysticalEffectPolicyAuditRecord => ({
  id: 'mystical-policy:courier-ward',
  form: 'relic',
  rarity: 'ultra-rare',
  effectId: 'effect:courier-ward',
  effectCategory: 'relic',
  sourceClass: 'material-object',
  sourceFactId: 'fact:object:courier-ward',
  conditionIds: ['condition:courier-ward'],
  costIds: ['cost:courier-ward'],
  auditEvidenceIds: ['audit:courier-ward:availability', 'audit:courier-ward:cost', 'audit:courier-ward:source'],
  availability: { scope: 'per-world', maximumDiscoveries: 1, maximumConcurrentEligibility: 1 },
  useBoundary: { kind: 'limited-use', maximumUses: 1 },
  futureReservation: { kind: 'future-courier-loss-safeguard', status: 'deferred-no-authority' },
  status: 'policy-audited-only'
})

const preventionReservation = (): CourierLossSafeguardReservation => ({
  kind: 'prevention',
  mysticalPolicyVersion: MYSTICAL_EFFECT_POLICY_VERSION,
  source: { factId: 'fact:object:courier-ward', factRevision: 1 },
  policyRecord: policyRecord()
})

const request = (overrides: Partial<CourierLossAssessmentRequest> = {}): CourierLossAssessmentRequest => {
  const world = sourceWorld()
  return {
    version: COURIER_LOSS_POLICY_VERSION,
    actionTime: { timeUnit: 'minute', worldTime: world.state.temporal.worldTime },
    peopleContext: peopleContext(world),
    people: confirmedLossPeople(world),
    loss: {
      id: 'courier-loss:crew-0:confirmed',
      kind: 'confirmed-courier-loss',
      courierId: 'crew:0',
      atWorldTime: world.state.temporal.worldTime,
      evidenceIds: ['loss-evidence:crew-0:confirmed'],
      contentSafety: lossSafety()
    },
    knownFacts: [{ id: 'fact:object:courier-ward', kind: 'grounded-object', revision: 1 }],
    ...overrides
  }
}

const codes = (value: unknown): readonly string[] => validateCourierLossAssessmentRequest(value).diagnostics.map(diagnostic => diagnostic.code)

describe('courier loss policy assessment boundary', () => {
  it('assesses a confirmed courier loss canonically and does not mutate authoritative inputs', () => {
    const input = request()
    const before = structuredClone(input)
    const first = assessCourierLoss(input)
    const second = assessCourierLoss(structuredClone(input))

    expect(validateCourierLossAssessmentRequest(input)).toEqual({ version: 1, status: 'accepted', diagnostics: [] })
    expect(first).toEqual(second)
    expect(first.confirmedLoss).toEqual({
      id: 'courier-loss:crew-0:confirmed',
      courierId: 'crew:0',
      atWorldTime: 12,
      evidenceIds: ['loss-evidence:crew-0:confirmed'],
      outcome: 'permanent-loss-required',
      reasonCodes: ['confirmed-courier-loss', 'permanent-loss-default']
    })
    expect(first.eligibleLivingCrew.map(candidate => candidate.crewId)).toEqual(['crew:1', 'crew:2', 'crew:3', 'crew:4', 'crew:5'])
    expect(first.immediateSuccessorCandidates).toEqual(first.eligibleLivingCrew)
    expect(first.finalization).toEqual({ kind: 'none' })
    expect(input).toEqual(before)
  })

  it('keeps permanent loss as the default and emits policy obligations rather than a replacement, transfer, or history mutation', () => {
    const input = request()
    const assessment = assessCourierLoss(input)

    expect(assessment.confirmedLoss.outcome).toBe('permanent-loss-required')
    expect(assessment.lastingConsequenceObligations).toEqual(COURIER_LOSS_LASTING_CONSEQUENCE_OBLIGATIONS)
    expect(assessment.lastingConsequenceObligations).toEqual(expect.arrayContaining([
      'retain-confirmed-death-record',
      'no-automatic-replacement',
      'no-automatic-resurrection',
      'no-automatic-task-reassignment',
      'no-automatic-relationship-erasure',
      'no-automatic-possession-transfer',
      'no-history-rewrite'
    ]))
    expect(Object.keys(assessment)).not.toEqual(expect.arrayContaining(['people', 'replacement', 'history', 'possessions']))
    expect(input.people).toEqual(request().people)
  })

  it('keeps prevention and revival unavailable, even with a valid ultra-rare mystical-policy reservation', () => {
    const defaultAssessment = assessCourierLoss(request())
    const reservationAssessment = assessCourierLoss(request({ safeguardReservations: [preventionReservation()] }))

    expect(defaultAssessment.safeguards).toEqual([
      { kind: 'prevention', status: 'unavailable', reason: 'no-current-safeguard-authority' },
      { kind: 'revival', status: 'unavailable', reason: 'no-current-safeguard-authority' }
    ])
    expect(reservationAssessment.safeguards).toEqual([
      { kind: 'prevention', status: 'unavailable', reason: 'future-ultra-rare-reservation-only', policyRecordId: 'mystical-policy:courier-ward' },
      { kind: 'revival', status: 'unavailable', reason: 'no-current-safeguard-authority' }
    ])
    expect(reservationAssessment.confirmedLoss.outcome).toBe('permanent-loss-required')
  })

  it('orders immediate successors separately from eligible living crew who are temporarily unavailable', () => {
    const input = request()
    const unavailable = input.people.find(person => person.id === 'crew:2')!
    unavailable.work = { ...unavailable.work, availability: 'unavailable', current: { status: 'idle' } }
    const assessment = assessCourierLoss(input)

    expect(assessment.eligibleLivingCrew.map(candidate => [candidate.crewId, candidate.status, candidate.availability])).toEqual([
      ['crew:1', 'immediate', 'available'],
      ['crew:2', 'temporarily-unavailable', 'unavailable'],
      ['crew:3', 'immediate', 'available'],
      ['crew:4', 'immediate', 'available'],
      ['crew:5', 'immediate', 'available']
    ])
    expect(assessment.immediateSuccessorCandidates.map(candidate => candidate.crewId)).toEqual(['crew:1', 'crew:3', 'crew:4', 'crew:5'])
    expect(assessment.finalization).toEqual({ kind: 'none' })
  })

  it('does not finalize a world merely because every eligible living successor is temporarily unavailable', () => {
    const input = request()
    for (const person of input.people) {
      if (person.id !== 'crew:0') person.work = { ...person.work, availability: 'unavailable', current: { status: 'idle' } }
    }

    const assessment = assessCourierLoss(input)
    expect(assessment.immediateSuccessorCandidates).toEqual([])
    expect(assessment.eligibleLivingCrew).toHaveLength(5)
    expect(assessment.eligibleLivingCrew.every(candidate => candidate.status === 'temporarily-unavailable')).toBe(true)
    expect(assessment.finalization).toEqual({ kind: 'none' })
  })

  it('emits only the canonical crew-extinction finalization intent when no eligible living crew remains', () => {
    const input = request()
    for (const person of input.people) {
      if (person.id === 'crew:0') continue
      person.life = { ...person.life, status: 'dead', death: { atWorldTime: input.actionTime.worldTime } }
      person.work = { ...person.work, availability: 'unavailable', current: { status: 'idle' } }
    }

    const assessment = assessCourierLoss(input)
    expect(COURIER_LOSS_CREW_EXTINCTION_REASON).toBe('crew-extinction')
    expect(assessment.eligibleLivingCrew).toEqual([])
    expect(assessment.immediateSuccessorCandidates).toEqual([])
    expect(assessment.finalization).toEqual({
      kind: 'read-only-chronicle-finalization-intent',
      reason: 'crew-extinction',
      path: 'existing-read-only-chronicle-path'
    })
    expect(Object.keys(assessment.finalization)).not.toContain('world')
  })

  it('fails closed for malformed, unsafe, stale, duplicate, noncanonical, child-related, and unauthorized inputs', () => {
    const unsafe = request()
    ;(unsafe.loss.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const stale = request()
    stale.loss.atWorldTime++
    const duplicateEvidence = request()
    duplicateEvidence.loss.evidenceIds = ['loss-evidence:crew-0:confirmed', 'loss-evidence:crew-0:confirmed']
    const noncanonicalEvidence = request()
    noncanonicalEvidence.loss.evidenceIds = ['loss-evidence:crew-0:z', 'loss-evidence:crew-0:a']
    const childRelated = request()
    childRelated.loss.contentSafety = classifyMedievalContent('event', ['ordinary-hardship'], 'not-applicable')
    const unauthorized = request({ safeguardReservations: [preventionReservation()] })
    unauthorized.safeguardReservations![0]!.policyRecord.rarity = 'rare'
    unauthorized.safeguardReservations![0]!.policyRecord.availability = { scope: 'per-world', maximumDiscoveries: 3, maximumConcurrentEligibility: 1 }
    const ungrounded = request({ safeguardReservations: [preventionReservation()] })
    ungrounded.safeguardReservations![0]!.source.factId = 'fact:object:not-known'
    const duplicateSafeguard = request({ safeguardReservations: [preventionReservation(), preventionReservation()] })
    const unsupportedAuthority = request({ safeguardReservations: [preventionReservation()] }) as unknown as { safeguardReservations: Array<Record<string, unknown>> }
    unsupportedAuthority.safeguardReservations[0]!.authority = 'granted'

    expect(codes({})).toEqual(['courier-loss-policy.malformed-request'])
    expect(codes(unsafe)).toContain('courier-loss-policy.invalid-loss-safety')
    expect(codes(stale)).toEqual(expect.arrayContaining(['courier-loss-policy.invalid-loss-time', 'courier-loss-policy.unconfirmed-loss']))
    expect(codes(duplicateEvidence)).toContain('courier-loss-policy.duplicate-loss-evidence')
    expect(codes(noncanonicalEvidence)).toContain('courier-loss-policy.noncanonical-loss-evidence-order')
    expect(codes(childRelated)).toContain('courier-loss-policy.child-related-input')
    expect(codes(unauthorized)).toContain('courier-loss-policy.unauthorized-safeguard')
    expect(codes(ungrounded)).toEqual(expect.arrayContaining(['courier-loss-policy.ungrounded-safeguard', 'courier-loss-policy.stale-safeguard-source']))
    expect(codes(duplicateSafeguard)).toEqual(expect.arrayContaining(['courier-loss-policy.duplicate-safeguard-kind', 'courier-loss-policy.noncanonical-safeguard-order']))
    expect(codes(unsupportedAuthority)).toContain('courier-loss-policy.malformed-safeguard-reservation')
  })

  it('rejects a living subject, noncanonical crew context, and a malformed persistent-person snapshot through existing owners', () => {
    const living = request()
    const person = living.people.find(candidate => candidate.id === 'crew:0')!
    person.life = { ...person.life, status: 'living', death: null }
    person.work = { ...person.work, availability: 'available', current: { status: 'idle' } }
    const reorderedCrew = request()
    reorderedCrew.peopleContext = { ...reorderedCrew.peopleContext, crew: [...reorderedCrew.peopleContext.crew].reverse() }
    const malformedPerson = request()
    ;(malformedPerson.people[0] as unknown as Record<string, unknown>).extension = 'not-authoritative'

    expect(codes(living)).toContain('courier-loss-policy.unconfirmed-loss')
    expect(codes(reorderedCrew)).toContain('courier-loss-policy.invalid-authoritative-crew')
    expect(codes(malformedPerson)).toContain('persistent-person.malformed-record')
  })
})
