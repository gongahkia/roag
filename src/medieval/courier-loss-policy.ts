import { validateMedievalContentSafety, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { EFFECT_MODEL_CONTRACT_VERSION, validateEffectResolutionRequest, type EffectActionTime, type EffectKnownFact, type EffectModelDiagnosticCode } from './effects'
import { MYSTICAL_EFFECT_FORMS, MYSTICAL_EFFECT_POLICY_VERSION, MYSTICAL_EFFECT_RARITIES, MYSTICAL_EFFECT_RARITY_AVAILABILITY, MYSTICAL_EFFECT_SOURCE_CLASSES, MYSTICAL_EFFECT_ULTRA_RARE_RESERVATIONS, type MysticalEffectPolicyAuditRecord } from './mystical-effect-policy'
import { PERSISTENT_PERSON_LIMITS, validatePersistentPeople, type PersistentPersonRecord, type PersistentPersonValidationContext, type PersistentPersonValidationDiagnosticCode } from './persistent-person'
import { MEDIEVAL_TIME_UNIT } from './temporal'
import type { ChronicleReason, FoundationCrewMember } from './types'

/**
 * A policy-only assessment for a future owner that has confirmed a courier
 * loss. It neither changes a person nor creates a chronicle.
 */
export const COURIER_LOSS_POLICY_VERSION = 1 as const

export const COURIER_LOSS_POLICY_LIMITS = {
  safeguardReservations: 2,
  lossEvidence: 4,
  identityLength: PERSISTENT_PERSON_LIMITS.identityLength
} as const

/** This exact existing vocabulary remains owned by the chronicle contract. */
export const COURIER_LOSS_CREW_EXTINCTION_REASON: ChronicleReason = 'crew-extinction'

export const COURIER_LOSS_SAFEGUARD_KINDS = ['prevention', 'revival'] as const
export type CourierLossSafeguardKind = typeof COURIER_LOSS_SAFEGUARD_KINDS[number]

/** Future reducers must keep these consequences instead of silently repairing loss. */
export const COURIER_LOSS_LASTING_CONSEQUENCE_OBLIGATIONS = [
  'retain-confirmed-death-record',
  'no-automatic-replacement',
  'no-automatic-resurrection',
  'no-automatic-task-reassignment',
  'no-automatic-relationship-erasure',
  'no-automatic-possession-transfer',
  'no-history-rewrite'
] as const
export type CourierLossLastingConsequenceObligation = typeof COURIER_LOSS_LASTING_CONSEQUENCE_OBLIGATIONS[number]

export interface CourierLossConfirmation {
  id: string
  kind: 'confirmed-courier-loss'
  courierId: string
  atWorldTime: number
  evidenceIds: readonly string[]
  contentSafety: MedievalContentSafetyClassification
}

/**
 * A reference to an already-audited mystical-policy reservation. It remains
 * unavailable here: this gives a later owner provenance to evaluate, not an
 * authority to avert or reverse a loss.
 */
export interface CourierLossSafeguardReservation {
  kind: CourierLossSafeguardKind
  mysticalPolicyVersion: typeof MYSTICAL_EFFECT_POLICY_VERSION
  source: { factId: string; factRevision: number }
  policyRecord: MysticalEffectPolicyAuditRecord
}

/**
 * `peopleContext` and `people` are the existing persistent-person validator
 * inputs. They are a read-only snapshot, never a second person or world model.
 */
export interface CourierLossAssessmentRequest {
  version: typeof COURIER_LOSS_POLICY_VERSION
  actionTime: EffectActionTime
  peopleContext: PersistentPersonValidationContext
  people: readonly PersistentPersonRecord[]
  loss: CourierLossConfirmation
  knownFacts: readonly EffectKnownFact[]
  safeguardReservations?: readonly CourierLossSafeguardReservation[]
}

export interface CourierLossSuccessorCandidate {
  crewId: string
  personId: string
  availability: 'available' | 'committed' | 'unavailable'
  status: 'immediate' | 'temporarily-unavailable'
}

export type CourierLossFinalizationIntent =
  | { kind: 'none' }
  | {
    kind: 'read-only-chronicle-finalization-intent'
    reason: typeof COURIER_LOSS_CREW_EXTINCTION_REASON
    path: 'existing-read-only-chronicle-path'
  }

export interface CourierLossSafeguardIntent {
  kind: CourierLossSafeguardKind
  status: 'unavailable'
  reason: 'no-current-safeguard-authority' | 'future-ultra-rare-reservation-only'
  policyRecordId?: string
}

export interface CourierLossAssessment {
  version: typeof COURIER_LOSS_POLICY_VERSION
  actionTime: EffectActionTime
  confirmedLoss: {
    id: string
    courierId: string
    atWorldTime: number
    evidenceIds: readonly string[]
    outcome: 'permanent-loss-required'
    reasonCodes: readonly ['confirmed-courier-loss', 'permanent-loss-default']
  }
  immediateSuccessorCandidates: readonly CourierLossSuccessorCandidate[]
  eligibleLivingCrew: readonly CourierLossSuccessorCandidate[]
  finalization: CourierLossFinalizationIntent
  safeguards: readonly CourierLossSafeguardIntent[]
  lastingConsequenceObligations: readonly CourierLossLastingConsequenceObligation[]
}

export type CourierLossPolicyDiagnosticCode =
  | 'courier-loss-policy.malformed-request'
  | 'courier-loss-policy.invalid-contract-version'
  | 'courier-loss-policy.invalid-action-time'
  | 'courier-loss-policy.action-time-context-mismatch'
  | 'courier-loss-policy.invalid-authoritative-crew'
  | 'courier-loss-policy.unknown-courier'
  | 'courier-loss-policy.ineligible-courier'
  | 'courier-loss-policy.unconfirmed-loss'
  | 'courier-loss-policy.invalid-loss-time'
  | 'courier-loss-policy.malformed-loss'
  | 'courier-loss-policy.invalid-loss-id'
  | 'courier-loss-policy.invalid-loss-evidence'
  | 'courier-loss-policy.duplicate-loss-evidence'
  | 'courier-loss-policy.noncanonical-loss-evidence-order'
  | 'courier-loss-policy.invalid-loss-safety'
  | 'courier-loss-policy.child-related-input'
  | 'courier-loss-policy.malformed-safeguard-reservations'
  | 'courier-loss-policy.safeguard-reservation-limit'
  | 'courier-loss-policy.malformed-safeguard-reservation'
  | 'courier-loss-policy.duplicate-safeguard-kind'
  | 'courier-loss-policy.noncanonical-safeguard-order'
  | 'courier-loss-policy.invalid-safeguard-kind'
  | 'courier-loss-policy.invalid-safeguard-policy-version'
  | 'courier-loss-policy.unauthorized-safeguard'
  | 'courier-loss-policy.ungrounded-safeguard'
  | 'courier-loss-policy.stale-safeguard-source'
  | 'courier-loss-policy.invalid-safeguard-policy-record'
  | PersistentPersonValidationDiagnosticCode
  | EffectModelDiagnosticCode
  | MedievalContentSafetyDiagnosticCode

export interface CourierLossPolicyDiagnostic {
  recordId: string
  code: CourierLossPolicyDiagnosticCode
}

export type CourierLossPolicyValidation =
  | { version: typeof COURIER_LOSS_POLICY_VERSION; status: 'accepted'; diagnostics: readonly [] }
  | { version: typeof COURIER_LOSS_POLICY_VERSION; status: 'rejected'; diagnostics: readonly CourierLossPolicyDiagnostic[] }

export class CourierLossPolicyContractError extends Error {
  constructor(readonly diagnostics: readonly CourierLossPolicyDiagnostic[]) {
    super(`courier loss policy rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'CourierLossPolicyContractError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const keys = [...expected].sort(compare)
  return actual.length === keys.length && actual.every((key, index) => key === keys[index])
}
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const positiveInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value > 0
const nonNegativeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const validId = (value: unknown, prefix?: string): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= COURIER_LOSS_POLICY_LIMITS.identityLength
  && /^[a-z][a-z0-9-]*(?::[a-z0-9][a-z0-9-]*)+$/u.test(value)
  && (prefix === undefined || value.startsWith(prefix))
const canonical = (values: readonly string[]): boolean => values.every((value, index) => index === 0 || compare(values[index - 1]!, value) < 0)
const diagnostic = (recordId: string, code: CourierLossPolicyDiagnosticCode): CourierLossPolicyDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly CourierLossPolicyDiagnostic[]): readonly CourierLossPolicyDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))

const validSafety = (id: string, domain: 'event' | 'history' | 'person', value: unknown): boolean => validateMedievalContentSafety([{ id, domain, classification: value }]).status === 'accepted'

/** Only checks the immutable crew references needed to bind person eligibility. */
const validCrewMember = (value: unknown): value is FoundationCrewMember => record(value)
  && hasOnlyKeys(value, ['id', 'name', 'contentSafety', 'role', 'conversation', 'equipment', 'history', 'historyContentSafety', 'relationships', 'eligible'])
  && validId(value.id, 'crew:')
  && typeof value.name === 'string'
  && value.name.length > 0
  && typeof value.role === 'string'
  && positiveInteger(value.conversation)
  && Array.isArray(value.equipment)
  && value.equipment.every(item => typeof item === 'string' && item.length > 0)
  && typeof value.history === 'string'
  && Array.isArray(value.relationships)
  && typeof value.eligible === 'boolean'
  && validSafety(value.id, 'person', value.contentSafety)
  && validSafety(`${value.id}:history`, 'history', value.historyContentSafety)

const validCrew = (value: unknown): value is readonly FoundationCrewMember[] => Array.isArray(value)
  && value.length > 0
  && value.length <= PERSISTENT_PERSON_LIMITS.records
  && value.every(validCrewMember)
  && new Set(value.map(member => member.id)).size === value.length
  && canonical(value.map(member => member.id))

const validActionTime = (value: unknown): value is EffectActionTime => record(value)
  && hasOnlyKeys(value, ['timeUnit', 'worldTime'])
  && value.timeUnit === MEDIEVAL_TIME_UNIT
  && nonNegativeInteger(value.worldTime)

const lossShape = (value: unknown): value is Record<string, unknown> => record(value)
  && hasOnlyKeys(value, ['id', 'kind', 'courierId', 'atWorldTime', 'evidenceIds', 'contentSafety'])

const reservationShape = (value: unknown): value is Record<string, unknown> => record(value)
  && hasOnlyKeys(value, ['kind', 'mysticalPolicyVersion', 'source', 'policyRecord'])

const policyRecordShape = (value: unknown): value is Record<string, unknown> => record(value)
  && hasOnlyKeys(value, ['id', 'form', 'rarity', 'effectId', 'effectCategory', 'sourceClass', 'sourceFactId', 'conditionIds', 'costIds', 'auditEvidenceIds', 'availability', 'useBoundary', 'futureReservation', 'status'])

const boundedCanonicalIds = (value: unknown, maximum: number): value is readonly string[] => Array.isArray(value)
  && value.length > 0
  && value.length <= maximum
  && value.every(item => validId(item))
  && new Set(value).size === value.length
  && canonical(value)

const validAvailability = (value: unknown, rarity: unknown): boolean => record(value)
  && hasOnlyKeys(value, ['scope', 'maximumDiscoveries', 'maximumConcurrentEligibility'])
  && value.scope === 'per-world'
  && oneOf(MYSTICAL_EFFECT_RARITIES, rarity)
  && value.maximumDiscoveries === MYSTICAL_EFFECT_RARITY_AVAILABILITY[rarity].maximumDiscoveriesPerWorld
  && value.maximumConcurrentEligibility === MYSTICAL_EFFECT_RARITY_AVAILABILITY[rarity].maximumConcurrentEligibility

const validUseBoundary = (value: unknown): boolean => record(value) && (
  (hasOnlyKeys(value, ['kind', 'maximumWorldMinutes']) && value.kind === 'finite-duration' && positiveInteger(value.maximumWorldMinutes) && value.maximumWorldMinutes <= 1_440)
  || (hasOnlyKeys(value, ['kind', 'maximumUses']) && value.kind === 'limited-use' && positiveInteger(value.maximumUses) && value.maximumUses <= 4)
)

const validPolicyRecord = (value: unknown): value is MysticalEffectPolicyAuditRecord => policyRecordShape(value)
  && validId(value.id, 'mystical-policy:')
  && oneOf(MYSTICAL_EFFECT_FORMS, value.form)
  && oneOf(MYSTICAL_EFFECT_RARITIES, value.rarity)
  && typeof value.effectCategory === 'string'
  && validId(value.effectId, 'effect:')
  && oneOf(MYSTICAL_EFFECT_SOURCE_CLASSES, value.sourceClass)
  && validId(value.sourceFactId, 'fact:')
  && boundedCanonicalIds(value.conditionIds, 4)
  && boundedCanonicalIds(value.costIds, 3)
  && boundedCanonicalIds(value.auditEvidenceIds, 12)
  && validAvailability(value.availability, value.rarity)
  && validUseBoundary(value.useBoundary)
  && record(value.futureReservation)
  && hasOnlyKeys(value.futureReservation, ['kind', 'status'])
  && oneOf(MYSTICAL_EFFECT_ULTRA_RARE_RESERVATIONS, value.futureReservation.kind)
  && value.futureReservation.status === 'deferred-no-authority'
  && value.status === 'policy-audited-only'

const baseEffectValidation = (actionTime: unknown, knownFacts: unknown) => validateEffectResolutionRequest({
  version: EFFECT_MODEL_CONTRACT_VERSION,
  actionTime,
  knownFacts,
  effects: [],
  appliedCounterplayIds: []
})

const knownFactMap = (value: unknown): ReadonlyMap<string, EffectKnownFact> => {
  if (!Array.isArray(value)) return new Map()
  const facts = value.filter(item => record(item) && validId(item.id, 'fact:') && typeof item.kind === 'string' && positiveInteger(item.revision)) as EffectKnownFact[]
  return new Map(facts.map(fact => [fact.id, fact]))
}

const lossRecordId = (value: unknown): string => record(value) && typeof value.id === 'string' ? value.id : 'courier-loss:confirmation'

/**
 * Validation only. The persistent-person owner validates every record and the
 * effects owner validates the known-fact boundary used by a safeguard reference.
 */
export const validateCourierLossAssessmentRequest = (value: unknown): CourierLossPolicyValidation => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'actionTime', 'peopleContext', 'people', 'loss', 'knownFacts', ...(Object.hasOwn(value, 'safeguardReservations') ? ['safeguardReservations'] : [])])) {
    return { version: COURIER_LOSS_POLICY_VERSION, status: 'rejected', diagnostics: [diagnostic('courier-loss:request', 'courier-loss-policy.malformed-request')] }
  }

  const issues: CourierLossPolicyDiagnostic[] = []
  if (value.version !== COURIER_LOSS_POLICY_VERSION) issues.push(diagnostic('courier-loss:request', 'courier-loss-policy.invalid-contract-version'))
  if (!validActionTime(value.actionTime)) issues.push(diagnostic('courier-loss:request', 'courier-loss-policy.invalid-action-time'))

  const effectValidation = baseEffectValidation(value.actionTime, value.knownFacts)
  if (effectValidation.status === 'rejected') issues.push(...effectValidation.diagnostics.map(item => diagnostic('courier-loss:known-facts', item.code)))
  const facts = knownFactMap(value.knownFacts)

  const context = value.peopleContext
  if (!record(context) || !nonNegativeInteger(context.worldTime) || !validCrew(context.crew)) {
    issues.push(diagnostic('courier-loss:people-context', 'courier-loss-policy.invalid-authoritative-crew'))
  } else {
    if (validActionTime(value.actionTime) && context.worldTime !== value.actionTime.worldTime) issues.push(diagnostic('courier-loss:people-context', 'courier-loss-policy.action-time-context-mismatch'))
    try {
      issues.push(...validatePersistentPeople(context as unknown as PersistentPersonValidationContext, value.people).map(item => diagnostic(item.recordId, item.code)))
    } catch {
      issues.push(diagnostic('courier-loss:people-context', 'courier-loss-policy.invalid-authoritative-crew'))
    }
  }

  const lossId = lossRecordId(value.loss)
  if (!lossShape(value.loss)) {
    issues.push(diagnostic(lossId, 'courier-loss-policy.malformed-loss'))
  } else {
    const loss = value.loss as unknown as CourierLossConfirmation
    if (!validId(loss.id, 'courier-loss:')) issues.push(diagnostic(lossId, 'courier-loss-policy.invalid-loss-id'))
    if (loss.kind !== 'confirmed-courier-loss') issues.push(diagnostic(lossId, 'courier-loss-policy.unconfirmed-loss'))
    if (!validId(loss.courierId, 'crew:')) issues.push(diagnostic(lossId, 'courier-loss-policy.unknown-courier'))
    if (!nonNegativeInteger(loss.atWorldTime) || !validActionTime(value.actionTime) || loss.atWorldTime !== value.actionTime.worldTime) issues.push(diagnostic(lossId, 'courier-loss-policy.invalid-loss-time'))
    if (!Array.isArray(loss.evidenceIds) || loss.evidenceIds.length === 0 || loss.evidenceIds.length > COURIER_LOSS_POLICY_LIMITS.lossEvidence || !loss.evidenceIds.every(item => validId(item, 'loss-evidence:'))) {
      issues.push(diagnostic(lossId, 'courier-loss-policy.invalid-loss-evidence'))
    } else {
      if (new Set(loss.evidenceIds).size !== loss.evidenceIds.length) issues.push(diagnostic(lossId, 'courier-loss-policy.duplicate-loss-evidence'))
      if (!canonical(loss.evidenceIds)) issues.push(diagnostic(lossId, 'courier-loss-policy.noncanonical-loss-evidence-order'))
    }
    if (!validSafety(loss.id, 'event', loss.contentSafety)) issues.push(diagnostic(lossId, 'courier-loss-policy.invalid-loss-safety'))
    if (!record(loss.contentSafety) || loss.contentSafety.participantScope !== 'adults-only') issues.push(diagnostic(lossId, 'courier-loss-policy.child-related-input'))
  }

  if (record(context) && validCrew(context.crew) && Array.isArray(value.people) && lossShape(value.loss)) {
    const loss = value.loss as unknown as CourierLossConfirmation
    const crew = context.crew
    const sourceCrew = crew.find(member => member.id === loss.courierId)
    const person = value.people.find(candidate => record(candidate) && candidate.id === loss.courierId) as PersistentPersonRecord | undefined
    if (!sourceCrew || !person) issues.push(diagnostic(lossId, 'courier-loss-policy.unknown-courier'))
    else if (!sourceCrew.eligible) issues.push(diagnostic(lossId, 'courier-loss-policy.ineligible-courier'))
    else if (person.life.status !== 'dead' || person.life.death?.atWorldTime !== loss.atWorldTime) issues.push(diagnostic(lossId, 'courier-loss-policy.unconfirmed-loss'))
  }

  if (Object.hasOwn(value, 'safeguardReservations')) {
    if (!Array.isArray(value.safeguardReservations)) {
      issues.push(diagnostic('courier-loss:safeguards', 'courier-loss-policy.malformed-safeguard-reservations'))
    } else {
      if (value.safeguardReservations.length > COURIER_LOSS_POLICY_LIMITS.safeguardReservations) issues.push(diagnostic('courier-loss:safeguards', 'courier-loss-policy.safeguard-reservation-limit'))
      const kinds: string[] = []
      for (const reservation of value.safeguardReservations) {
        const recordId = reservationShape(reservation) && policyRecordShape(reservation.policyRecord) && typeof reservation.policyRecord.id === 'string' ? reservation.policyRecord.id : 'courier-loss:safeguard'
        if (!reservationShape(reservation)) {
          issues.push(diagnostic(recordId, 'courier-loss-policy.malformed-safeguard-reservation'))
          continue
        }
        if (!oneOf(COURIER_LOSS_SAFEGUARD_KINDS, reservation.kind)) issues.push(diagnostic(recordId, 'courier-loss-policy.invalid-safeguard-kind'))
        else kinds.push(reservation.kind)
        if (reservation.mysticalPolicyVersion !== MYSTICAL_EFFECT_POLICY_VERSION) issues.push(diagnostic(recordId, 'courier-loss-policy.invalid-safeguard-policy-version'))
        if (!record(reservation.source) || !hasOnlyKeys(reservation.source, ['factId', 'factRevision']) || !validId(reservation.source.factId, 'fact:') || !positiveInteger(reservation.source.factRevision)) {
          issues.push(diagnostic(recordId, 'courier-loss-policy.ungrounded-safeguard'))
        }
        if (!validPolicyRecord(reservation.policyRecord)) {
          issues.push(diagnostic(recordId, 'courier-loss-policy.invalid-safeguard-policy-record'))
          continue
        }
        if (!record(reservation.source) || reservation.source.factId !== reservation.policyRecord.sourceFactId) issues.push(diagnostic(recordId, 'courier-loss-policy.ungrounded-safeguard'))
        const fact = record(reservation.source) && typeof reservation.source.factId === 'string' ? facts.get(reservation.source.factId) : undefined
        if (!fact || !record(reservation.source) || fact.revision !== reservation.source.factRevision) issues.push(diagnostic(recordId, 'courier-loss-policy.stale-safeguard-source'))
        if (reservation.policyRecord.rarity !== 'ultra-rare' || reservation.policyRecord.futureReservation?.kind !== 'future-courier-loss-safeguard' || reservation.policyRecord.futureReservation?.status !== 'deferred-no-authority') issues.push(diagnostic(recordId, 'courier-loss-policy.unauthorized-safeguard'))
      }
      if (new Set(kinds).size !== kinds.length) issues.push(diagnostic('courier-loss:safeguards', 'courier-loss-policy.duplicate-safeguard-kind'))
      if (!canonical(kinds)) issues.push(diagnostic('courier-loss:safeguards', 'courier-loss-policy.noncanonical-safeguard-order'))
    }
  }

  const diagnostics = canonicalDiagnostics(issues)
  return diagnostics.length === 0
    ? { version: COURIER_LOSS_POLICY_VERSION, status: 'accepted', diagnostics: [] }
    : { version: COURIER_LOSS_POLICY_VERSION, status: 'rejected', diagnostics }
}

const successorCandidates = (context: PersistentPersonValidationContext, people: readonly PersistentPersonRecord[], lostCourierId: string): readonly CourierLossSuccessorCandidate[] => context.crew
  .filter(member => member.eligible && member.id !== lostCourierId)
  .map(member => {
    const person = people.find(candidate => candidate.id === member.id)
    if (!person || person.life.status !== 'living') return undefined
    const immediate = person.work.availability === 'available'
    return {
      crewId: member.id,
      personId: person.id,
      availability: person.work.availability,
      status: immediate ? 'immediate' as const : 'temporarily-unavailable' as const
    }
  })
  .filter((candidate): candidate is CourierLossSuccessorCandidate => candidate !== undefined)

/**
 * Returns only policy intent. In particular, an ultra-rare reservation is not
 * an authorization: confirmed loss remains permanent in every result.
 */
export const assessCourierLoss = (value: CourierLossAssessmentRequest | unknown): CourierLossAssessment => {
  const validation = validateCourierLossAssessmentRequest(value)
  if (validation.status === 'rejected') throw new CourierLossPolicyContractError(validation.diagnostics)
  const request = value as CourierLossAssessmentRequest
  const eligibleLivingCrew = successorCandidates(request.peopleContext, request.people, request.loss.courierId)
  const immediateSuccessorCandidates = eligibleLivingCrew.filter(candidate => candidate.status === 'immediate')
  const reservations = request.safeguardReservations ?? []
  const safeguards = COURIER_LOSS_SAFEGUARD_KINDS.map(kind => {
    const reservation = reservations.find(candidate => candidate.kind === kind)
    return reservation === undefined
      ? { kind, status: 'unavailable' as const, reason: 'no-current-safeguard-authority' as const }
      : { kind, status: 'unavailable' as const, reason: 'future-ultra-rare-reservation-only' as const, policyRecordId: reservation.policyRecord.id }
  })
  const finalization: CourierLossFinalizationIntent = eligibleLivingCrew.length === 0
    ? { kind: 'read-only-chronicle-finalization-intent', reason: COURIER_LOSS_CREW_EXTINCTION_REASON, path: 'existing-read-only-chronicle-path' }
    : { kind: 'none' }

  return {
    version: COURIER_LOSS_POLICY_VERSION,
    actionTime: { timeUnit: request.actionTime.timeUnit, worldTime: request.actionTime.worldTime },
    confirmedLoss: {
      id: request.loss.id,
      courierId: request.loss.courierId,
      atWorldTime: request.loss.atWorldTime,
      evidenceIds: [...request.loss.evidenceIds],
      outcome: 'permanent-loss-required',
      reasonCodes: ['confirmed-courier-loss', 'permanent-loss-default']
    },
    immediateSuccessorCandidates: immediateSuccessorCandidates.map(candidate => ({ ...candidate })),
    eligibleLivingCrew: eligibleLivingCrew.map(candidate => ({ ...candidate })),
    finalization,
    safeguards,
    lastingConsequenceObligations: [...COURIER_LOSS_LASTING_CONSEQUENCE_OBLIGATIONS]
  }
}
