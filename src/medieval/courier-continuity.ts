import { classifyMedievalContent, validateMedievalContentSafety, type MedievalContentSafetyClassification } from './content-safety'
import { COURIER_LOSS_LASTING_CONSEQUENCE_OBLIGATIONS, assessCourierLoss, type CourierLossLastingConsequenceObligation } from './courier-loss-policy'
import { initialHouseholdActiveCrew, validateInitialHouseholdStructure } from './initial-household'
import { validatePersistentPeople, type PersistentPersonRecord, type PersistentPersonValidationContext } from './persistent-person'

/**
 * The one renderer-independent owner for an already-confirmed permanent loss
 * of the current courier. It has no world, storage, renderer, input, or clock
 * authority: the owning world reducer supplies the canonical current minute.
 */
export const COURIER_CONTINUITY_CONTRACT_VERSION = 1 as const

export const COURIER_CONTINUITY_LIMITS = {
  evidenceIds: 4,
  identityLength: 160
} as const

export const COURIER_LOSS_OUTCOMES = ['death', 'departure'] as const
export type CourierLossOutcome = typeof COURIER_LOSS_OUTCOMES[number]

export interface CourierContinuityConfirmation {
  version: typeof COURIER_CONTINUITY_CONTRACT_VERSION
  id: string
  kind: 'confirmed-courier-continuity-loss'
  outcome: CourierLossOutcome
  courierId: string
  atWorldTime: number
  evidenceIds: readonly string[]
  contentSafety: MedievalContentSafetyClassification
}

export interface CourierContinuityCourierState {
  initialCourierId?: string
  activeCourierId?: string
  departedCourierIds: readonly string[]
}

export interface CourierContinuityNavigationState {
  courierId?: string
  coordinate?: { column: number; row: number }
}

export interface CourierContinuityAssessmentRequest {
  version: typeof COURIER_CONTINUITY_CONTRACT_VERSION
  peopleContext: PersistentPersonValidationContext
  people: readonly PersistentPersonRecord[]
  courier: CourierContinuityCourierState
  navigation: CourierContinuityNavigationState
  confirmation: CourierContinuityConfirmation
}

export type CourierContinuityFinalization =
  | { kind: 'continue'; successorId: string }
  | { kind: 'crew-extinction' }

export interface CourierContinuityAssessment {
  version: typeof COURIER_CONTINUITY_CONTRACT_VERSION
  confirmation: CourierContinuityConfirmation
  people: readonly PersistentPersonRecord[]
  departedCourierIds: readonly string[]
  successorIds: readonly string[]
  finalization: CourierContinuityFinalization
  lastingConsequenceObligations: readonly CourierLossLastingConsequenceObligation[]
}

export type CourierContinuityDiagnosticCode =
  | 'courier-continuity.malformed-request'
  | 'courier-continuity.invalid-contract-version'
  | 'courier-continuity.invalid-courier-state'
  | 'courier-continuity.invalid-navigation'
  | 'courier-continuity.initial-courier-missing'
  | 'courier-continuity.active-courier-missing'
  | 'courier-continuity.active-courier-departed'
  | 'courier-continuity.confirmation-courier-mismatch'
  | 'courier-continuity.invalid-confirmation'
  | 'courier-continuity.invalid-confirmation-id'
  | 'courier-continuity.invalid-confirmation-time'
  | 'courier-continuity.invalid-evidence'
  | 'courier-continuity.duplicate-evidence'
  | 'courier-continuity.noncanonical-evidence-order'
  | 'courier-continuity.invalid-content-safety'
  | 'courier-continuity.child-related-input'
  | 'courier-continuity.invalid-current-person'
  | 'courier-continuity.duplicate-loss'
  | 'courier-continuity.invalid-departure-state'
  | 'courier-continuity.invalid-death-state'
  | 'courier-continuity.invalid-authoritative-people'

export interface CourierContinuityDiagnostic {
  recordId: string
  code: CourierContinuityDiagnosticCode | ReturnType<typeof validatePersistentPeople>[number]['code'] | ReturnType<typeof validateInitialHouseholdStructure>[number]['code']
}

export class CourierContinuityContractError extends Error {
  constructor(readonly diagnostics: readonly CourierContinuityDiagnostic[]) {
    super(`courier continuity rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'CourierContinuityContractError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort(compare)
  const expectedKeys = [...expected].sort(compare)
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const validId = (value: unknown, prefix?: string): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= COURIER_CONTINUITY_LIMITS.identityLength
  && /^[a-z][a-z0-9-]*(?::[a-z0-9][a-z0-9-]*)+$/u.test(value)
  && (prefix === undefined || value.startsWith(prefix))
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const canonical = (values: readonly string[]): boolean => values.every((value, index) => index === 0 || compare(values[index - 1]!, value) < 0)
const diagnostic = (recordId: string, code: CourierContinuityDiagnostic['code']): CourierContinuityDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly CourierContinuityDiagnostic[]): readonly CourierContinuityDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const sameCoordinate = (left: { column: number; row: number }, right: { column: number; row: number }): boolean => left.column === right.column && left.row === right.row

/** The ID is derived solely from the bounded outcome subject and canonical minute. */
export const courierContinuityConfirmationIdFor = (outcome: CourierLossOutcome, courierId: string, atWorldTime: number): string => `courier-loss:${outcome}:${courierId}:${atWorldTime}`

const validConfirmation = (value: unknown): value is CourierContinuityConfirmation => record(value)
  && hasOnlyKeys(value, ['version', 'id', 'kind', 'outcome', 'courierId', 'atWorldTime', 'evidenceIds', 'contentSafety'])
  && value.version === COURIER_CONTINUITY_CONTRACT_VERSION
  && value.kind === 'confirmed-courier-continuity-loss'
  && (value.outcome === 'death' || value.outcome === 'departure')
  && validId(value.courierId, 'crew:')
  && safeInteger(value.atWorldTime)
  && typeof value.id === 'string'
  && value.id === courierContinuityConfirmationIdFor(value.outcome, value.courierId, value.atWorldTime)
  && Array.isArray(value.evidenceIds)
  && value.evidenceIds.length > 0
  && value.evidenceIds.length <= COURIER_CONTINUITY_LIMITS.evidenceIds
  && value.evidenceIds.every(item => validId(item, 'loss-evidence:'))
  && new Set(value.evidenceIds).size === value.evidenceIds.length
  && canonical(value.evidenceIds)
  && validateMedievalContentSafety([{ id: value.id, domain: 'event', classification: value.contentSafety }]).status === 'accepted'
  && record(value.contentSafety)
  && value.contentSafety.participantScope === 'adults-only'

const validCourierState = (value: unknown): value is CourierContinuityCourierState => record(value)
  && hasOnlyKeys(value, ['initialCourierId', 'activeCourierId', 'departedCourierIds'])
  && validId(value.initialCourierId, 'crew:')
  && validId(value.activeCourierId, 'crew:')
  && Array.isArray(value.departedCourierIds)
  && value.departedCourierIds.every(item => validId(item, 'crew:'))
  && new Set(value.departedCourierIds).size === value.departedCourierIds.length
  && canonical(value.departedCourierIds)

const validNavigation = (value: unknown): value is CourierContinuityNavigationState => record(value)
  && hasOnlyKeys(value, ['courierId', 'coordinate'])
  && validId(value.courierId, 'crew:')
  && record(value.coordinate)
  && hasOnlyKeys(value.coordinate, ['column', 'row'])
  && safeInteger(value.coordinate.column)
  && safeInteger(value.coordinate.row)

const deathRecordFor = (person: PersistentPersonRecord, atWorldTime: number): PersistentPersonRecord => ({
  ...structuredClone(person),
  life: { status: 'dead', birth: structuredClone(person.life.birth), death: { atWorldTime } },
  work: { ...structuredClone(person.work), current: { status: 'idle' }, availability: 'unavailable' }
})

/**
 * A policy assessment has no departure vocabulary. This owner therefore uses
 * its lasting obligations for both outcomes and filters the policy's ordered
 * living set through previously recorded permanent departures.
 */
const successorIdsFor = (
  request: CourierContinuityAssessmentRequest,
  people: readonly PersistentPersonRecord[],
  departedCourierIds: readonly string[],
  lostCourierId: string
): readonly string[] => {
  const departed = new Set(departedCourierIds)
  return initialHouseholdActiveCrew(request.peopleContext.crew)
    .filter(member => member.id !== lostCourierId && !departed.has(member.id))
    .filter(member => people.find(person => person.id === member.id)?.life.status === 'living')
    .map(member => member.id)
}

/**
 * Validates an explicit adult-only confirmation and returns a deterministic
 * state plan. It never mutates the submitted confirmation, people, or state.
 */
export const assessCourierContinuityLoss = (value: CourierContinuityAssessmentRequest | unknown): CourierContinuityAssessment => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'peopleContext', 'people', 'courier', 'navigation', 'confirmation'])) {
    throw new CourierContinuityContractError([diagnostic('courier-continuity:request', 'courier-continuity.malformed-request')])
  }
  const request = value as CourierContinuityAssessmentRequest
  const issues: CourierContinuityDiagnostic[] = []
  if (request.version !== COURIER_CONTINUITY_CONTRACT_VERSION) issues.push(diagnostic('courier-continuity:request', 'courier-continuity.invalid-contract-version'))
  if (!validCourierState(request.courier)) issues.push(diagnostic('world-state:courier', 'courier-continuity.invalid-courier-state'))
  if (!validNavigation(request.navigation)) issues.push(diagnostic('world-state:navigation', 'courier-continuity.invalid-navigation'))
  if (!validConfirmation(request.confirmation)) {
    const candidate = record(request.confirmation) && typeof request.confirmation.id === 'string' ? request.confirmation.id : 'courier-loss:confirmation'
    issues.push(diagnostic(candidate, 'courier-continuity.invalid-confirmation'))
    if (record(request.confirmation) && typeof request.confirmation.id === 'string' && (!validId(request.confirmation.id, 'courier-loss:') || request.confirmation.id !== courierContinuityConfirmationIdFor(request.confirmation.outcome as CourierLossOutcome, String(request.confirmation.courierId), request.confirmation.atWorldTime as number))) issues.push(diagnostic(candidate, 'courier-continuity.invalid-confirmation-id'))
    if (record(request.confirmation) && request.confirmation.atWorldTime !== request.peopleContext?.worldTime) issues.push(diagnostic(candidate, 'courier-continuity.invalid-confirmation-time'))
    if (record(request.confirmation) && Array.isArray(request.confirmation.evidenceIds)) {
      if (new Set(request.confirmation.evidenceIds).size !== request.confirmation.evidenceIds.length) issues.push(diagnostic(candidate, 'courier-continuity.duplicate-evidence'))
      if (!canonical(request.confirmation.evidenceIds.filter((item): item is string => typeof item === 'string'))) issues.push(diagnostic(candidate, 'courier-continuity.noncanonical-evidence-order'))
    } else issues.push(diagnostic(candidate, 'courier-continuity.invalid-evidence'))
    if (record(request.confirmation) && (!record(request.confirmation.contentSafety) || request.confirmation.contentSafety.participantScope !== 'adults-only')) issues.push(diagnostic(candidate, 'courier-continuity.child-related-input'))
    if (record(request.confirmation) && request.confirmation.contentSafety !== undefined && validateMedievalContentSafety([{ id: candidate, domain: 'event', classification: request.confirmation.contentSafety }]).status === 'rejected') issues.push(diagnostic(candidate, 'courier-continuity.invalid-content-safety'))
  }

  try {
    issues.push(...validateInitialHouseholdStructure(request.peopleContext.crew).map(item => diagnostic(item.recordId, item.code)))
    issues.push(...validatePersistentPeople(request.peopleContext, request.people).map(item => diagnostic(item.recordId, item.code)))
  } catch {
    issues.push(diagnostic('courier-continuity:people', 'courier-continuity.invalid-authoritative-people'))
  }

  if (validCourierState(request.courier)) {
    if (request.courier.initialCourierId === undefined) issues.push(diagnostic('world-state:courier', 'courier-continuity.initial-courier-missing'))
    if (request.courier.activeCourierId === undefined) issues.push(diagnostic('world-state:courier', 'courier-continuity.active-courier-missing'))
    if (request.courier.activeCourierId !== undefined && request.courier.departedCourierIds.includes(request.courier.activeCourierId)) issues.push(diagnostic('world-state:courier', 'courier-continuity.active-courier-departed'))
  }
  if (validCourierState(request.courier) && validNavigation(request.navigation) && request.navigation.courierId !== request.courier.activeCourierId) issues.push(diagnostic('world-state:navigation', 'courier-continuity.invalid-navigation'))
  if (validConfirmation(request.confirmation)) {
    if (request.confirmation.atWorldTime !== request.peopleContext.worldTime) issues.push(diagnostic(request.confirmation.id, 'courier-continuity.invalid-confirmation-time'))
    if (request.confirmation.courierId !== request.courier.activeCourierId) issues.push(diagnostic(request.confirmation.id, 'courier-continuity.confirmation-courier-mismatch'))
  }

  const preliminary = canonicalDiagnostics(issues)
  if (preliminary.length) throw new CourierContinuityContractError(preliminary)

  const confirmation = request.confirmation
  const current = request.people.find(person => person.id === confirmation.courierId)
  if (!current || current.life.status !== 'living' || current.identity.adult !== true) throw new CourierContinuityContractError([diagnostic(confirmation.courierId, 'courier-continuity.invalid-current-person')])
  if (request.courier.departedCourierIds.includes(current.id)) throw new CourierContinuityContractError([diagnostic(current.id, 'courier-continuity.duplicate-loss')])
  if (confirmation.outcome === 'death' && (current.work.current.status !== 'idle' || current.commitments.some(commitment => commitment.status === 'active'))) {
    throw new CourierContinuityContractError([diagnostic(current.id, 'courier-continuity.invalid-death-state')])
  }

  const people = confirmation.outcome === 'death'
    ? request.people.map(person => person.id === current.id ? deathRecordFor(person, confirmation.atWorldTime) : structuredClone(person))
    : request.people.map(person => structuredClone(person))
  const departedCourierIds = confirmation.outcome === 'departure'
    ? [...request.courier.departedCourierIds, current.id].sort(compare)
    : [...request.courier.departedCourierIds]

  if (confirmation.outcome === 'death') {
    try {
      assessCourierLoss({
        version: 1,
        actionTime: { timeUnit: 'world-minute', worldTime: confirmation.atWorldTime },
        peopleContext: request.peopleContext,
        people,
        loss: {
          id: confirmation.id,
          kind: 'confirmed-courier-loss',
          courierId: confirmation.courierId,
          atWorldTime: confirmation.atWorldTime,
          evidenceIds: confirmation.evidenceIds,
          contentSafety: confirmation.contentSafety
        },
        knownFacts: []
      })
    } catch {
      throw new CourierContinuityContractError([diagnostic(confirmation.id, 'courier-continuity.invalid-death-state')])
    }
  }

  const successorIds = successorIdsFor(request, people, departedCourierIds, confirmation.courierId)
  const finalization: CourierContinuityFinalization = successorIds.length
    ? { kind: 'continue', successorId: successorIds[0]! }
    : { kind: 'crew-extinction' }
  return {
    version: COURIER_CONTINUITY_CONTRACT_VERSION,
    confirmation: structuredClone(confirmation),
    people,
    departedCourierIds,
    successorIds,
    finalization,
    lastingConsequenceObligations: [...COURIER_LOSS_LASTING_CONSEQUENCE_OBLIGATIONS]
  }
}

/** Canonical no-free-text metadata for a caller constructing a confirmation. */
export const courierContinuityContentSafety = (): MedievalContentSafetyClassification => classifyMedievalContent(
  'event',
  ['adult-labour', 'ordinary-hardship'],
  'adults-only',
  ['data']
)
