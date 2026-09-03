import { validateMedievalContentSafety, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { EFFECT_MODEL_CONTRACT_VERSION, validateEffectResolutionRequest, type EffectActionTime, type EffectKnownFact, type EffectModelDiagnosticCode } from './effects'
import { MYSTICAL_EFFECT_FORMS, MYSTICAL_EFFECT_POLICY_VERSION, MYSTICAL_EFFECT_RARITIES, MYSTICAL_EFFECT_RARITY_AVAILABILITY, MYSTICAL_EFFECT_SOURCE_CLASSES, MYSTICAL_EFFECT_ULTRA_RARE_RESERVATIONS, type MysticalEffectPolicyAuditRecord } from './mystical-effect-policy'
import { MEDIEVAL_TIME_UNIT } from './temporal'
import type { ChronicleReason } from './types'
import { MEDIEVAL_WORLD_STATE_LIMITS, WORLD_JOMON_STATE_VERSION, type WorldJomonState } from './world-state'

/**
 * A policy-only assessment for future Jomon integrity owners. It does not
 * mutate integrity, schedule work, or create a chronicle.
 */
export const JOMON_INTEGRITY_POLICY_VERSION = 1 as const

export const JOMON_INTEGRITY_POLICY_LIMITS = {
  causalEvidence: 6,
  identityLength: 96
} as const

/** This exact existing vocabulary remains owned by the chronicle contract. */
export const JOMON_INTEGRITY_LOSS_REASON: ChronicleReason = 'jomon-loss'

export const JOMON_INTEGRITY_BANDS = ['sound', 'weathered', 'damaged', 'critical', 'collapsed'] as const
export type JomonIntegrityBand = typeof JOMON_INTEGRITY_BANDS[number]

export const JOMON_INTEGRITY_INCIDENT_KINDS = ['ordinary-condition', 'partial-disaster', 'terminal-collapse'] as const
export type JomonIntegrityIncidentKind = typeof JOMON_INTEGRITY_INCIDENT_KINDS[number]

export const JOMON_INTEGRITY_EVIDENCE_KINDS = [
  'grounded-cause',
  'material-cost',
  'labour-cost',
  'action-time-cost',
  'risk-cost',
  'recovery-trade-off',
  'adverse-counterplay',
  'collapse-evidence'
] as const
export type JomonIntegrityEvidenceKind = typeof JOMON_INTEGRITY_EVIDENCE_KINDS[number]

/** These are obligations for a future owner, never action, inventory, or task records. */
export const JOMON_INTEGRITY_RECOVERY_OBLIGATIONS = [
  'grounded-cause',
  'material-labour-time-or-risk-cost',
  'recovery-trade-off',
  'inspectable-causal-chain'
] as const
export type JomonIntegrityRecoveryObligation = typeof JOMON_INTEGRITY_RECOVERY_OBLIGATIONS[number]

export interface JomonIntegrityIncident {
  id: string
  kind: JomonIntegrityIncidentKind
  contentSafety: MedievalContentSafetyClassification
}

/**
 * Entries remain ID-sorted, while `afterEvidenceId` gives the inspectable
 * causal predecessor. It must name an earlier entry, preventing cycles.
 */
export interface JomonIntegrityCausalEvidence {
  id: string
  kind: JomonIntegrityEvidenceKind
  factId: string
  factRevision: number
  afterEvidenceId?: string
}

/**
 * This is a provenance reference to an already-audited mystical-policy result.
 * Its presence cannot authorize rescue, repair, or terminal-loss override.
 */
export interface JomonIntegritySafeguardReservation {
  mysticalPolicyVersion: typeof MYSTICAL_EFFECT_POLICY_VERSION
  source: { factId: string; factRevision: number }
  policyRecord: MysticalEffectPolicyAuditRecord
  causalEvidenceIds: readonly string[]
  adverseCounterplayEvidenceId: string
  recoveryTradeOffEvidenceId: string
}

/** Input is a read-only existing Jomon state, action time, and known-fact set. */
export interface JomonIntegrityAssessmentRequest {
  version: typeof JOMON_INTEGRITY_POLICY_VERSION
  actionTime: EffectActionTime
  jomon: WorldJomonState
  knownFacts: readonly EffectKnownFact[]
  incident: JomonIntegrityIncident
  causalEvidence: readonly JomonIntegrityCausalEvidence[]
  safeguardReservation?: JomonIntegritySafeguardReservation
}

export type JomonIntegrityRepairIntent =
  | { kind: 'not-needed' }
  | { kind: 'future-repair-required'; obligationIds: readonly JomonIntegrityRecoveryObligation[]; causalEvidenceIds: readonly string[] }

export type JomonIntegrityRescueIntent =
  | { kind: 'not-needed' }
  | { kind: 'future-non-mystical-rescue-required'; obligationIds: readonly JomonIntegrityRecoveryObligation[]; causalEvidenceIds: readonly string[] }

export type JomonIntegrityFinalizationIntent =
  | { kind: 'none' }
  | {
    kind: 'read-only-chronicle-finalization-intent'
    reason: typeof JOMON_INTEGRITY_LOSS_REASON
    path: 'existing-read-only-chronicle-path'
  }

export type JomonIntegritySafeguardIntent =
  | { status: 'unavailable'; reason: 'no-current-jomon-safeguard-authority' }
  | {
    status: 'unavailable'
    reason: 'future-ultra-rare-reservation-only'
    policyRecordId: string
    sourceFactId: string
    conditionIds: readonly string[]
    costIds: readonly string[]
    causalEvidenceIds: readonly string[]
    adverseCounterplayEvidenceId: string
    recoveryTradeOffEvidenceId: string
  }

export interface JomonIntegrityAssessment {
  version: typeof JOMON_INTEGRITY_POLICY_VERSION
  actionTime: EffectActionTime
  vesselId: 'vessel:jomon'
  integrity: { current: number; maximum: number; band: JomonIntegrityBand }
  incident: { id: string; kind: JomonIntegrityIncidentKind; causalEvidenceIds: readonly string[] }
  repair: JomonIntegrityRepairIntent
  rescue: JomonIntegrityRescueIntent
  collapse: { status: 'not-collapsed' } | { status: 'confirmed-collapse'; causalEvidenceIds: readonly string[] }
  terminalLoss: 'not-terminal' | 'confirmed-jomon-loss'
  finalization: JomonIntegrityFinalizationIntent
  safeguard: JomonIntegritySafeguardIntent
}

export type JomonIntegrityPolicyDiagnosticCode =
  | 'jomon-integrity-policy.malformed-request'
  | 'jomon-integrity-policy.invalid-contract-version'
  | 'jomon-integrity-policy.invalid-action-time'
  | 'jomon-integrity-policy.invalid-jomon-state'
  | 'jomon-integrity-policy.malformed-incident'
  | 'jomon-integrity-policy.invalid-incident-id'
  | 'jomon-integrity-policy.unknown-incident-kind'
  | 'jomon-integrity-policy.invalid-incident-safety'
  | 'jomon-integrity-policy.child-related-input'
  | 'jomon-integrity-policy.malformed-causal-evidence'
  | 'jomon-integrity-policy.causal-evidence-limit'
  | 'jomon-integrity-policy.invalid-causal-evidence-id'
  | 'jomon-integrity-policy.duplicate-causal-evidence-id'
  | 'jomon-integrity-policy.noncanonical-causal-evidence-order'
  | 'jomon-integrity-policy.unknown-causal-evidence-kind'
  | 'jomon-integrity-policy.unknown-causal-fact'
  | 'jomon-integrity-policy.stale-causal-fact'
  | 'jomon-integrity-policy.ineligible-causal-fact'
  | 'jomon-integrity-policy.invalid-causal-chain'
  | 'jomon-integrity-policy.missing-grounded-cause'
  | 'jomon-integrity-policy.missing-recovery-cost'
  | 'jomon-integrity-policy.missing-recovery-trade-off'
  | 'jomon-integrity-policy.missing-collapse-evidence'
  | 'jomon-integrity-policy.contradictory-integrity-incident'
  | 'jomon-integrity-policy.malformed-safeguard-reservation'
  | 'jomon-integrity-policy.invalid-safeguard-policy-version'
  | 'jomon-integrity-policy.invalid-safeguard-policy-record'
  | 'jomon-integrity-policy.unauthorized-safeguard'
  | 'jomon-integrity-policy.ungrounded-safeguard'
  | 'jomon-integrity-policy.stale-safeguard-source'
  | 'jomon-integrity-policy.invalid-safeguard-chain'
  | 'jomon-integrity-policy.invalid-safeguard-counterplay'
  | 'jomon-integrity-policy.invalid-safeguard-recovery-trade-off'
  | EffectModelDiagnosticCode
  | MedievalContentSafetyDiagnosticCode

export interface JomonIntegrityPolicyDiagnostic {
  recordId: string
  code: JomonIntegrityPolicyDiagnosticCode
}

export type JomonIntegrityPolicyValidation =
  | { version: typeof JOMON_INTEGRITY_POLICY_VERSION; status: 'accepted'; diagnostics: readonly [] }
  | { version: typeof JOMON_INTEGRITY_POLICY_VERSION; status: 'rejected'; diagnostics: readonly JomonIntegrityPolicyDiagnostic[] }

export class JomonIntegrityPolicyContractError extends Error {
  constructor(readonly diagnostics: readonly JomonIntegrityPolicyDiagnostic[]) {
    super(`jomon integrity policy rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'JomonIntegrityPolicyContractError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const keys = [...expected].sort(compare)
  return actual.length === keys.length && actual.every((key, index) => key === keys[index])
}
const hasRequiredAndOnlyOptionalKeys = (value: Record<string, unknown>, required: readonly string[], optional: readonly string[]): boolean => required.every(key => Object.hasOwn(value, key)) && Object.keys(value).every(key => required.includes(key) || optional.includes(key))
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const positiveInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value > 0
const nonNegativeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const validId = (value: unknown, prefix?: string): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= JOMON_INTEGRITY_POLICY_LIMITS.identityLength
  && /^[a-z][a-z0-9-]*(?::[a-z0-9][a-z0-9-]*)+$/u.test(value)
  && (prefix === undefined || value.startsWith(prefix))
const canonical = (values: readonly string[]): boolean => values.every((value, index) => index === 0 || compare(values[index - 1]!, value) < 0)
const diagnostic = (recordId: string, code: JomonIntegrityPolicyDiagnosticCode): JomonIntegrityPolicyDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly JomonIntegrityPolicyDiagnostic[]): readonly JomonIntegrityPolicyDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))

const validActionTime = (value: unknown): value is EffectActionTime => record(value)
  && hasOnlyKeys(value, ['timeUnit', 'worldTime'])
  && value.timeUnit === MEDIEVAL_TIME_UNIT
  && nonNegativeInteger(value.worldTime)

/** Uses the exact existing Jomon state shape, but does not construct or mutate it. */
const validJomonState = (value: unknown): value is WorldJomonState => record(value)
  && hasOnlyKeys(value, ['version', 'vesselId', 'operationalStatus', 'location', 'integrity', 'capacity'])
  && value.version === WORLD_JOMON_STATE_VERSION
  && value.vesselId === 'vessel:jomon'
  && value.operationalStatus === 'moored'
  && record(value.location)
  && hasOnlyKeys(value.location, ['kind', 'id'])
  && (value.location.kind === 'site' || value.location.kind === 'quay')
  && validId(value.location.id)
  && record(value.integrity)
  && hasOnlyKeys(value.integrity, ['current', 'maximum'])
  && nonNegativeInteger(value.integrity.current)
  && positiveInteger(value.integrity.maximum)
  && value.integrity.maximum <= MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum
  && value.integrity.current <= value.integrity.maximum
  && record(value.capacity)
  && hasOnlyKeys(value.capacity, ['cargoUnits', 'berthSlots', 'workSlots'])
  && nonNegativeInteger(value.capacity.cargoUnits)
  && nonNegativeInteger(value.capacity.berthSlots)
  && nonNegativeInteger(value.capacity.workSlots)
  && value.capacity.cargoUnits <= MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum
  && value.capacity.berthSlots <= MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum
  && value.capacity.workSlots <= MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum

const incidentShape = (value: unknown): value is Record<string, unknown> => record(value) && hasOnlyKeys(value, ['id', 'kind', 'contentSafety'])
const evidenceShape = (value: unknown): value is Record<string, unknown> => record(value) && hasRequiredAndOnlyOptionalKeys(value, ['id', 'kind', 'factId', 'factRevision'], ['afterEvidenceId'])
const safeguardShape = (value: unknown): value is Record<string, unknown> => record(value)
  && hasOnlyKeys(value, ['mysticalPolicyVersion', 'source', 'policyRecord', 'causalEvidenceIds', 'adverseCounterplayEvidenceId', 'recoveryTradeOffEvidenceId'])
const policyRecordShape = (value: unknown): value is Record<string, unknown> => record(value)
  && hasOnlyKeys(value, ['id', 'form', 'rarity', 'effectId', 'effectCategory', 'sourceClass', 'sourceFactId', 'conditionIds', 'costIds', 'auditEvidenceIds', 'availability', 'useBoundary', 'futureReservation', 'status'])
const validSafety = (id: string, value: unknown): boolean => validateMedievalContentSafety([{ id, domain: 'event', classification: value }]).status === 'accepted'

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

/** Integer-only thresholds avoid rounding and platform-specific comparison behavior. */
export const jomonIntegrityBand = (integrity: Pick<WorldJomonState['integrity'], 'current' | 'maximum'>): JomonIntegrityBand => {
  if (integrity.current === 0) return 'collapsed'
  if (integrity.current === integrity.maximum) return 'sound'
  if (integrity.current * 4 >= integrity.maximum * 3) return 'weathered'
  if (integrity.current * 2 >= integrity.maximum) return 'damaged'
  return 'critical'
}

const incidentRecordId = (value: unknown): string => record(value) && typeof value.id === 'string' ? value.id : 'jomon-incident:assessment'
const recoveryCostKinds: readonly JomonIntegrityEvidenceKind[] = ['material-cost', 'labour-cost', 'action-time-cost', 'risk-cost']
const causalEvidenceFactKinds: Readonly<Record<JomonIntegrityEvidenceKind, readonly EffectKnownFact['kind'][]>> = {
  'grounded-cause': ['condition', 'environment', 'grounded-object', 'grounded-practice'],
  'material-cost': ['equipment', 'grounded-object'],
  'labour-cost': ['crew-support'],
  'action-time-cost': ['condition', 'preparation'],
  'risk-cost': ['condition', 'environment'],
  'recovery-trade-off': ['condition', 'preparation', 'equipment', 'grounded-object', 'environment'],
  'adverse-counterplay': ['preparation', 'equipment', 'grounded-object', 'crew-support', 'environment'],
  'collapse-evidence': ['condition', 'environment', 'grounded-object', 'grounded-practice']
}
const safeguardSourceFactKinds: Readonly<Record<MysticalEffectPolicyAuditRecord['sourceClass'], readonly EffectKnownFact['kind'][]>> = {
  'material-object': ['grounded-object'],
  'specific-place': ['grounded-practice'],
  'bounded-practice': ['grounded-practice'],
  'relationship-condition': ['crew-support']
}

/**
 * Validates only input evidence and future policy eligibility. Existing world,
 * effects, mystical-policy, and chronicle owners retain their own authority.
 */
export const validateJomonIntegrityAssessmentRequest = (value: unknown): JomonIntegrityPolicyValidation => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'actionTime', 'jomon', 'knownFacts', 'incident', 'causalEvidence', ...(Object.hasOwn(value, 'safeguardReservation') ? ['safeguardReservation'] : [])])) {
    return { version: JOMON_INTEGRITY_POLICY_VERSION, status: 'rejected', diagnostics: [diagnostic('jomon-integrity:request', 'jomon-integrity-policy.malformed-request')] }
  }

  const issues: JomonIntegrityPolicyDiagnostic[] = []
  if (value.version !== JOMON_INTEGRITY_POLICY_VERSION) issues.push(diagnostic('jomon-integrity:request', 'jomon-integrity-policy.invalid-contract-version'))
  if (!validActionTime(value.actionTime)) issues.push(diagnostic('jomon-integrity:request', 'jomon-integrity-policy.invalid-action-time'))
  if (!validJomonState(value.jomon)) issues.push(diagnostic('jomon-integrity:jomon', 'jomon-integrity-policy.invalid-jomon-state'))

  const effectValidation = baseEffectValidation(value.actionTime, value.knownFacts)
  if (effectValidation.status === 'rejected') issues.push(...effectValidation.diagnostics.map(item => diagnostic('jomon-integrity:known-facts', item.code)))
  const facts = knownFactMap(value.knownFacts)

  const incidentId = incidentRecordId(value.incident)
  let incident: JomonIntegrityIncident | undefined
  if (!incidentShape(value.incident)) {
    issues.push(diagnostic(incidentId, 'jomon-integrity-policy.malformed-incident'))
  } else {
    incident = value.incident as unknown as JomonIntegrityIncident
    if (!validId(incident.id, 'jomon-incident:')) issues.push(diagnostic(incidentId, 'jomon-integrity-policy.invalid-incident-id'))
    if (!oneOf(JOMON_INTEGRITY_INCIDENT_KINDS, incident.kind)) issues.push(diagnostic(incidentId, 'jomon-integrity-policy.unknown-incident-kind'))
    if (!validSafety(incident.id, incident.contentSafety)) issues.push(diagnostic(incidentId, 'jomon-integrity-policy.invalid-incident-safety'))
    if (!record(incident.contentSafety) || incident.contentSafety.participantScope !== 'not-applicable') issues.push(diagnostic(incidentId, 'jomon-integrity-policy.child-related-input'))
  }

  const evidence: JomonIntegrityCausalEvidence[] = []
  if (!Array.isArray(value.causalEvidence)) {
    issues.push(diagnostic(incidentId, 'jomon-integrity-policy.malformed-causal-evidence'))
  } else {
    if (value.causalEvidence.length > JOMON_INTEGRITY_POLICY_LIMITS.causalEvidence) issues.push(diagnostic(incidentId, 'jomon-integrity-policy.causal-evidence-limit'))
    const ids: string[] = []
    for (const candidate of value.causalEvidence) {
      if (!evidenceShape(candidate)) {
        issues.push(diagnostic(incidentId, 'jomon-integrity-policy.malformed-causal-evidence'))
        continue
      }
      const item = candidate as unknown as JomonIntegrityCausalEvidence
      evidence.push(item)
      if (!validId(item.id, 'jomon-evidence:')) issues.push(diagnostic(incidentId, 'jomon-integrity-policy.invalid-causal-evidence-id'))
      else ids.push(item.id)
      if (!oneOf(JOMON_INTEGRITY_EVIDENCE_KINDS, item.kind)) issues.push(diagnostic(item.id, 'jomon-integrity-policy.unknown-causal-evidence-kind'))
      const fact = facts.get(item.factId)
      if (!validId(item.factId, 'fact:') || !positiveInteger(item.factRevision) || !fact) issues.push(diagnostic(item.id, 'jomon-integrity-policy.unknown-causal-fact'))
      else if (fact.revision !== item.factRevision) issues.push(diagnostic(item.id, 'jomon-integrity-policy.stale-causal-fact'))
      else if (oneOf(JOMON_INTEGRITY_EVIDENCE_KINDS, item.kind) && !causalEvidenceFactKinds[item.kind].includes(fact.kind)) issues.push(diagnostic(item.id, 'jomon-integrity-policy.ineligible-causal-fact'))
    }
    if (new Set(ids).size !== ids.length) issues.push(diagnostic(incidentId, 'jomon-integrity-policy.duplicate-causal-evidence-id'))
    if (!canonical(ids)) issues.push(diagnostic(incidentId, 'jomon-integrity-policy.noncanonical-causal-evidence-order'))
    const previous = new Set<string>()
    for (const item of evidence) {
      if (item.kind === 'grounded-cause') {
        if (item.afterEvidenceId !== undefined) issues.push(diagnostic(item.id, 'jomon-integrity-policy.invalid-causal-chain'))
      } else if (!validId(item.afterEvidenceId, 'jomon-evidence:') || !previous.has(item.afterEvidenceId)) {
        issues.push(diagnostic(item.id, 'jomon-integrity-policy.invalid-causal-chain'))
      }
      previous.add(item.id)
    }
    if (incident?.kind !== 'ordinary-condition' && evidence.filter(item => item.kind === 'grounded-cause').length !== 1) issues.push(diagnostic(incidentId, 'jomon-integrity-policy.missing-grounded-cause'))
  }

  const band = validJomonState(value.jomon) ? jomonIntegrityBand(value.jomon.integrity) : undefined
  if (incident && band) {
    const recoveryEvidence = evidence.filter(item => recoveryCostKinds.includes(item.kind))
    const recoveryTradeOff = evidence.filter(item => item.kind === 'recovery-trade-off')
    if (incident.kind === 'ordinary-condition' && !(['sound', 'weathered'] as readonly JomonIntegrityBand[]).includes(band)) issues.push(diagnostic(incident.id, 'jomon-integrity-policy.contradictory-integrity-incident'))
    if (incident.kind === 'partial-disaster') {
      if (!(['damaged', 'critical'] as readonly JomonIntegrityBand[]).includes(band)) issues.push(diagnostic(incident.id, 'jomon-integrity-policy.contradictory-integrity-incident'))
      if (recoveryEvidence.length === 0) issues.push(diagnostic(incident.id, 'jomon-integrity-policy.missing-recovery-cost'))
      if (recoveryTradeOff.length === 0) issues.push(diagnostic(incident.id, 'jomon-integrity-policy.missing-recovery-trade-off'))
    }
    if (incident.kind === 'terminal-collapse') {
      if (band !== 'collapsed') issues.push(diagnostic(incident.id, 'jomon-integrity-policy.contradictory-integrity-incident'))
      if (!evidence.some(item => item.kind === 'collapse-evidence')) issues.push(diagnostic(incident.id, 'jomon-integrity-policy.missing-collapse-evidence'))
    }
    if (incident.kind !== 'ordinary-condition' && evidence.length === 0) issues.push(diagnostic(incident.id, 'jomon-integrity-policy.missing-grounded-cause'))
    if (incident.kind === 'ordinary-condition' && evidence.length !== 0) issues.push(diagnostic(incident.id, 'jomon-integrity-policy.contradictory-integrity-incident'))
  }

  if (Object.hasOwn(value, 'safeguardReservation')) {
    if (!safeguardShape(value.safeguardReservation)) {
      issues.push(diagnostic('jomon-integrity:safeguard', 'jomon-integrity-policy.malformed-safeguard-reservation'))
    } else {
      const reservation = value.safeguardReservation as unknown as JomonIntegritySafeguardReservation
      const recordId = policyRecordShape(reservation.policyRecord) && typeof reservation.policyRecord.id === 'string' ? reservation.policyRecord.id : 'jomon-integrity:safeguard'
      if (reservation.mysticalPolicyVersion !== MYSTICAL_EFFECT_POLICY_VERSION) issues.push(diagnostic(recordId, 'jomon-integrity-policy.invalid-safeguard-policy-version'))
      if (!record(reservation.source) || !hasOnlyKeys(reservation.source, ['factId', 'factRevision']) || !validId(reservation.source.factId, 'fact:') || !positiveInteger(reservation.source.factRevision)) issues.push(diagnostic(recordId, 'jomon-integrity-policy.ungrounded-safeguard'))
      if (!validPolicyRecord(reservation.policyRecord)) {
        issues.push(diagnostic(recordId, 'jomon-integrity-policy.invalid-safeguard-policy-record'))
      } else {
        if (!record(reservation.source) || reservation.source.factId !== reservation.policyRecord.sourceFactId) issues.push(diagnostic(recordId, 'jomon-integrity-policy.ungrounded-safeguard'))
        const source = record(reservation.source) && typeof reservation.source.factId === 'string' ? facts.get(reservation.source.factId) : undefined
        if (!source || !record(reservation.source) || source.revision !== reservation.source.factRevision) issues.push(diagnostic(recordId, 'jomon-integrity-policy.stale-safeguard-source'))
        else if (!safeguardSourceFactKinds[reservation.policyRecord.sourceClass].includes(source.kind)) issues.push(diagnostic(recordId, 'jomon-integrity-policy.ungrounded-safeguard'))
        if (reservation.policyRecord.rarity !== 'ultra-rare' || reservation.policyRecord.futureReservation?.kind !== 'future-jomon-loss-safeguard' || reservation.policyRecord.futureReservation?.status !== 'deferred-no-authority') issues.push(diagnostic(recordId, 'jomon-integrity-policy.unauthorized-safeguard'))
      }
      if (!boundedCanonicalIds(reservation.causalEvidenceIds, JOMON_INTEGRITY_POLICY_LIMITS.causalEvidence) || !reservation.causalEvidenceIds.every(id => evidence.some(item => item.id === id))) issues.push(diagnostic(recordId, 'jomon-integrity-policy.invalid-safeguard-chain'))
      const counterplay = evidence.find(item => item.id === reservation.adverseCounterplayEvidenceId)
      if (!validId(reservation.adverseCounterplayEvidenceId, 'jomon-evidence:') || counterplay?.kind !== 'adverse-counterplay') issues.push(diagnostic(recordId, 'jomon-integrity-policy.invalid-safeguard-counterplay'))
      const tradeOff = evidence.find(item => item.id === reservation.recoveryTradeOffEvidenceId)
      if (!validId(reservation.recoveryTradeOffEvidenceId, 'jomon-evidence:') || tradeOff?.kind !== 'recovery-trade-off') issues.push(diagnostic(recordId, 'jomon-integrity-policy.invalid-safeguard-recovery-trade-off'))
    }
  }

  const diagnostics = canonicalDiagnostics(issues)
  return diagnostics.length === 0
    ? { version: JOMON_INTEGRITY_POLICY_VERSION, status: 'accepted', diagnostics: [] }
    : { version: JOMON_INTEGRITY_POLICY_VERSION, status: 'rejected', diagnostics }
}

/** Returns assessment intent only; no returned branch changes world state. */
export const assessJomonIntegrity = (value: JomonIntegrityAssessmentRequest | unknown): JomonIntegrityAssessment => {
  const validation = validateJomonIntegrityAssessmentRequest(value)
  if (validation.status === 'rejected') throw new JomonIntegrityPolicyContractError(validation.diagnostics)
  const request = value as JomonIntegrityAssessmentRequest
  const band = jomonIntegrityBand(request.jomon.integrity)
  const evidenceIds = request.causalEvidence.map(item => item.id)
  const partial = request.incident.kind === 'partial-disaster'
  const critical = band === 'critical'
  const terminal = request.incident.kind === 'terminal-collapse'
  const obligations = [...JOMON_INTEGRITY_RECOVERY_OBLIGATIONS]
  const safeguard = request.safeguardReservation === undefined
    ? { status: 'unavailable' as const, reason: 'no-current-jomon-safeguard-authority' as const }
    : {
      status: 'unavailable' as const,
      reason: 'future-ultra-rare-reservation-only' as const,
      policyRecordId: request.safeguardReservation.policyRecord.id,
      sourceFactId: request.safeguardReservation.source.factId,
      conditionIds: [...request.safeguardReservation.policyRecord.conditionIds],
      costIds: [...request.safeguardReservation.policyRecord.costIds],
      causalEvidenceIds: [...request.safeguardReservation.causalEvidenceIds],
      adverseCounterplayEvidenceId: request.safeguardReservation.adverseCounterplayEvidenceId,
      recoveryTradeOffEvidenceId: request.safeguardReservation.recoveryTradeOffEvidenceId
    }
  return {
    version: JOMON_INTEGRITY_POLICY_VERSION,
    actionTime: { timeUnit: request.actionTime.timeUnit, worldTime: request.actionTime.worldTime },
    vesselId: request.jomon.vesselId,
    integrity: { current: request.jomon.integrity.current, maximum: request.jomon.integrity.maximum, band },
    incident: { id: request.incident.id, kind: request.incident.kind, causalEvidenceIds: [...evidenceIds] },
    repair: partial ? { kind: 'future-repair-required', obligationIds: obligations, causalEvidenceIds: [...evidenceIds] } : { kind: 'not-needed' },
    rescue: partial && critical ? { kind: 'future-non-mystical-rescue-required', obligationIds: obligations, causalEvidenceIds: [...evidenceIds] } : { kind: 'not-needed' },
    collapse: terminal ? { status: 'confirmed-collapse', causalEvidenceIds: [...evidenceIds] } : { status: 'not-collapsed' },
    terminalLoss: terminal ? 'confirmed-jomon-loss' : 'not-terminal',
    finalization: terminal
      ? { kind: 'read-only-chronicle-finalization-intent', reason: JOMON_INTEGRITY_LOSS_REASON, path: 'existing-read-only-chronicle-path' }
      : { kind: 'none' },
    safeguard
  }
}
