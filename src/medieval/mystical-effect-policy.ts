import { EFFECT_MODEL_CONTRACT_VERSION, EFFECT_MODEL_LIMITS, validateEffectResolutionRequest, type EffectActionTime, type EffectCandidate, type EffectKnownFact, type EffectKnownFactKind, type EffectModelDiagnosticCode } from './effects'
import { validateMedievalContentSafety, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'

/**
 * A policy/audit contract for future authored mystical definitions. It does not
 * generate, select, resolve, persist, or apply an effect to a world.
 */
export const MYSTICAL_EFFECT_POLICY_VERSION = 1 as const

/** Low mysticism starts scarce; ordinary or common supernatural supply is absent. */
export const MYSTICAL_EFFECT_RARITIES = ['scarce', 'rare', 'exceptional', 'ultra-rare'] as const
export type MysticalEffectRarity = typeof MYSTICAL_EFFECT_RARITIES[number]

export const MYSTICAL_EFFECT_FORMS = ['relic', 'totem', 'boon', 'curse'] as const
export type MysticalEffectForm = typeof MYSTICAL_EFFECT_FORMS[number]

export const MYSTICAL_EFFECT_SOURCE_CLASSES = [
  'material-object',
  'specific-place',
  'bounded-practice',
  'relationship-condition'
] as const
export type MysticalEffectSourceClass = typeof MYSTICAL_EFFECT_SOURCE_CLASSES[number]

export const MYSTICAL_EFFECT_CONDITION_KINDS = [
  'object-available',
  'specific-place-known',
  'bounded-practice-observed',
  'relationship-established',
  'ordinary-condition-known'
] as const
export type MysticalEffectConditionKind = typeof MYSTICAL_EFFECT_CONDITION_KINDS[number]

export const MYSTICAL_EFFECT_COST_KINDS = [
  'material-expenditure',
  'action-time-commitment',
  'exposure-to-risk',
  'relationship-obligation',
  'foregone-opportunity'
] as const
export type MysticalEffectCostKind = typeof MYSTICAL_EFFECT_COST_KINDS[number]

export const MYSTICAL_EFFECT_AUDIT_EVIDENCE_KINDS = [
  'source-provenance',
  'eligibility-condition',
  'cost-trade-off',
  'availability-bound',
  'adverse-counterplay'
] as const
export type MysticalEffectAuditEvidenceKind = typeof MYSTICAL_EFFECT_AUDIT_EVIDENCE_KINDS[number]

/** Reservations are labels only: later loss systems own all actual safeguards. */
export const MYSTICAL_EFFECT_ULTRA_RARE_RESERVATIONS = [
  'future-courier-loss-safeguard',
  'future-jomon-loss-safeguard'
] as const
export type MysticalEffectUltraRareReservationKind = typeof MYSTICAL_EFFECT_ULTRA_RARE_RESERVATIONS[number]

/** These are never descriptor capabilities and cannot be authorized by this module. */
export const MYSTICAL_EFFECT_FORBIDDEN_CAPABILITIES = [
  'generic-mage-class',
  'spell-list',
  'arbitrary-casting',
  'unlimited-supernatural-power',
  'courier-death-prevention',
  'courier-revival',
  'jomon-rescue',
  'jomon-collapse-prevention',
  'terminal-loss-override'
] as const

export const MYSTICAL_EFFECT_POLICY_LIMITS = {
  definitions: 12,
  conditionsPerDefinition: 4,
  costsPerDefinition: 3,
  auditEvidencePerDefinition: 12,
  limitedUses: 4,
  identityLength: EFFECT_MODEL_LIMITS.identityLength
} as const

export const MYSTICAL_EFFECT_RARITY_AVAILABILITY: Readonly<Record<MysticalEffectRarity, {
  maximumDiscoveriesPerWorld: number
  maximumConcurrentEligibility: number
}>> = {
  scarce: { maximumDiscoveriesPerWorld: 6, maximumConcurrentEligibility: 2 },
  rare: { maximumDiscoveriesPerWorld: 3, maximumConcurrentEligibility: 1 },
  exceptional: { maximumDiscoveriesPerWorld: 1, maximumConcurrentEligibility: 1 },
  'ultra-rare': { maximumDiscoveriesPerWorld: 1, maximumConcurrentEligibility: 1 }
}

export interface MysticalEffectSourceProvenance {
  sourceClass: MysticalEffectSourceClass
  factId: string
  factRevision: number
}

export interface MysticalEffectCondition {
  id: string
  kind: MysticalEffectConditionKind
  factId: string
  factRevision: number
}

export interface MysticalEffectAvailability {
  scope: 'per-world'
  maximumDiscoveries: number
  maximumConcurrentEligibility: number
}

/** This is a stated eligibility boundary, not a use counter or a clock transition. */
export type MysticalEffectUseBoundary =
  | { kind: 'finite-duration'; maximumWorldMinutes: number }
  | { kind: 'limited-use'; maximumUses: number }

export interface MysticalEffectCostTradeOff {
  id: string
  kind: MysticalEffectCostKind
  factId: string
  factRevision: number
}

export interface MysticalEffectAuditEvidence {
  id: string
  kind: MysticalEffectAuditEvidenceKind
  factId: string
  factRevision: number
}

export interface MysticalEffectFutureReservation {
  kind: MysticalEffectUltraRareReservationKind
  status: 'deferred-no-authority'
}

/**
 * `effect` remains the existing semantic candidate owned by effects.ts. This
 * policy only audits whether a future owner may consider that candidate.
 */
export interface MysticalEffectPolicyDefinition {
  version: typeof MYSTICAL_EFFECT_POLICY_VERSION
  id: string
  form: MysticalEffectForm
  rarity: MysticalEffectRarity
  source: MysticalEffectSourceProvenance
  conditions: readonly MysticalEffectCondition[]
  availability: MysticalEffectAvailability
  useBoundary: MysticalEffectUseBoundary
  costs: readonly MysticalEffectCostTradeOff[]
  auditEvidence: readonly MysticalEffectAuditEvidence[]
  effect: EffectCandidate
  contentSafety: MedievalContentSafetyClassification
  futureReservation?: MysticalEffectFutureReservation
}

/** The request is application-owned compiled data, never a world, save, or content-pack shape. */
export interface MysticalEffectPolicyAuditRequest {
  version: typeof MYSTICAL_EFFECT_POLICY_VERSION
  actionTime: EffectActionTime
  knownFacts: readonly EffectKnownFact[]
  definitions: readonly MysticalEffectPolicyDefinition[]
}

export interface MysticalEffectPolicyAuditRecord {
  id: string
  form: MysticalEffectForm
  rarity: MysticalEffectRarity
  effectId: string
  effectCategory: EffectCandidate['category']
  sourceClass: MysticalEffectSourceClass
  sourceFactId: string
  conditionIds: readonly string[]
  costIds: readonly string[]
  auditEvidenceIds: readonly string[]
  availability: MysticalEffectAvailability
  useBoundary: MysticalEffectUseBoundary
  futureReservation?: MysticalEffectFutureReservation
  status: 'policy-audited-only'
}

export type MysticalEffectPolicyDiagnosticCode =
  | 'mystical-effect-policy.malformed-request'
  | 'mystical-effect-policy.invalid-contract-version'
  | 'mystical-effect-policy.definition-limit'
  | 'mystical-effect-policy.malformed-definition'
  | 'mystical-effect-policy.invalid-definition-version'
  | 'mystical-effect-policy.invalid-definition-id'
  | 'mystical-effect-policy.duplicate-definition-id'
  | 'mystical-effect-policy.noncanonical-definition-order'
  | 'mystical-effect-policy.unknown-form'
  | 'mystical-effect-policy.unknown-rarity'
  | 'mystical-effect-policy.ineligible-effect-category'
  | 'mystical-effect-policy.malformed-source-provenance'
  | 'mystical-effect-policy.source-provenance-mismatch'
  | 'mystical-effect-policy.ungrounded-source'
  | 'mystical-effect-policy.invalid-source-class-combination'
  | 'mystical-effect-policy.malformed-condition'
  | 'mystical-effect-policy.condition-limit'
  | 'mystical-effect-policy.invalid-condition-id'
  | 'mystical-effect-policy.duplicate-condition-id'
  | 'mystical-effect-policy.noncanonical-condition-order'
  | 'mystical-effect-policy.unknown-condition-kind'
  | 'mystical-effect-policy.unknown-condition-fact'
  | 'mystical-effect-policy.stale-condition-fact'
  | 'mystical-effect-policy.missing-required-condition'
  | 'mystical-effect-policy.malformed-availability'
  | 'mystical-effect-policy.unbounded-availability'
  | 'mystical-effect-policy.malformed-use-boundary'
  | 'mystical-effect-policy.unbounded-use-boundary'
  | 'mystical-effect-policy.malformed-cost'
  | 'mystical-effect-policy.cost-limit'
  | 'mystical-effect-policy.invalid-cost-id'
  | 'mystical-effect-policy.duplicate-cost-id'
  | 'mystical-effect-policy.noncanonical-cost-order'
  | 'mystical-effect-policy.unknown-cost-kind'
  | 'mystical-effect-policy.free-or-no-trade-off'
  | 'mystical-effect-policy.unknown-cost-fact'
  | 'mystical-effect-policy.stale-cost-fact'
  | 'mystical-effect-policy.malformed-audit-evidence'
  | 'mystical-effect-policy.audit-evidence-limit'
  | 'mystical-effect-policy.invalid-audit-evidence-id'
  | 'mystical-effect-policy.duplicate-audit-evidence-id'
  | 'mystical-effect-policy.noncanonical-audit-evidence-order'
  | 'mystical-effect-policy.unknown-audit-evidence-kind'
  | 'mystical-effect-policy.unknown-audit-evidence-fact'
  | 'mystical-effect-policy.stale-audit-evidence-fact'
  | 'mystical-effect-policy.missing-audit-evidence'
  | 'mystical-effect-policy.missing-adverse-counterplay'
  | 'mystical-effect-policy.invalid-future-reservation'
  | 'mystical-effect-policy.non-ultra-rare-reservation'
  | 'mystical-effect-policy.missing-content-safety'
  | EffectModelDiagnosticCode
  | MedievalContentSafetyDiagnosticCode

export interface MysticalEffectPolicyDiagnostic {
  recordId: string
  code: MysticalEffectPolicyDiagnosticCode
}

export type MysticalEffectPolicyValidation =
  | { version: typeof MYSTICAL_EFFECT_POLICY_VERSION; status: 'accepted'; diagnostics: readonly [] }
  | { version: typeof MYSTICAL_EFFECT_POLICY_VERSION; status: 'rejected'; diagnostics: readonly MysticalEffectPolicyDiagnostic[] }

export interface MysticalEffectPolicyAudit {
  version: typeof MYSTICAL_EFFECT_POLICY_VERSION
  status: 'accepted'
  reviewed: readonly MysticalEffectPolicyAuditRecord[]
  diagnostics: readonly []
}

export class MysticalEffectPolicyContractError extends Error {
  constructor(readonly diagnostics: readonly MysticalEffectPolicyDiagnostic[]) {
    super(`mystical effect policy rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'MysticalEffectPolicyContractError'
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
const validId = (value: unknown, prefix: string): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= MYSTICAL_EFFECT_POLICY_LIMITS.identityLength
  && /^[a-z][a-z0-9-]*(?::[a-z][a-z0-9-]*)+$/u.test(value)
  && value.startsWith(prefix)
const canonical = (ids: readonly string[]): boolean => ids.every((id, index) => index === 0 || compare(ids[index - 1]!, id) < 0)
const diagnostic = (recordId: string, code: MysticalEffectPolicyDiagnosticCode): MysticalEffectPolicyDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly MysticalEffectPolicyDiagnostic[]): readonly MysticalEffectPolicyDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const definitionId = (value: unknown, index: number): string => record(value) && typeof value.id === 'string' ? value.id : `mystical-policy:#${index}`

const definitionShape = (value: unknown): value is Record<string, unknown> => record(value)
  && hasRequiredAndOnlyOptionalKeys(value, ['version', 'id', 'form', 'rarity', 'source', 'conditions', 'availability', 'useBoundary', 'costs', 'auditEvidence', 'effect', 'contentSafety'], ['futureReservation'])
const conditionShape = (value: unknown): value is Record<string, unknown> => record(value) && hasOnlyKeys(value, ['id', 'kind', 'factId', 'factRevision'])
const costShape = (value: unknown): value is Record<string, unknown> => record(value) && hasOnlyKeys(value, ['id', 'kind', 'factId', 'factRevision'])
const auditEvidenceShape = (value: unknown): value is Record<string, unknown> => record(value) && hasOnlyKeys(value, ['id', 'kind', 'factId', 'factRevision'])

const sourceClassRules: Readonly<Record<MysticalEffectSourceClass, {
  forms: readonly MysticalEffectForm[]
  categories: readonly EffectCandidate['category'][]
  sourceFactKind: EffectKnownFactKind
  grounding: EffectCandidate['source']['grounding']
  requiredCondition: MysticalEffectConditionKind
}>> = {
  'material-object': {
    forms: ['relic', 'totem', 'boon', 'curse'],
    categories: ['relic', 'totem', 'boon', 'curse'],
    sourceFactKind: 'grounded-object',
    grounding: 'material-object',
    requiredCondition: 'object-available'
  },
  'specific-place': {
    forms: ['boon', 'curse'],
    categories: ['boon', 'curse'],
    sourceFactKind: 'grounded-practice',
    grounding: 'place-bound-practice',
    requiredCondition: 'specific-place-known'
  },
  'bounded-practice': {
    forms: ['boon', 'curse'],
    categories: ['boon', 'curse'],
    sourceFactKind: 'grounded-practice',
    grounding: 'place-bound-practice',
    requiredCondition: 'bounded-practice-observed'
  },
  'relationship-condition': {
    forms: ['boon'],
    categories: ['crew-support'],
    sourceFactKind: 'crew-support',
    grounding: 'crew-relationship',
    requiredCondition: 'relationship-established'
  }
}

const effectCategoriesForForm: Readonly<Record<MysticalEffectForm, readonly EffectCandidate['category'][]>> = {
  relic: ['relic'],
  totem: ['totem'],
  boon: ['boon', 'crew-support'],
  curse: ['curse']
}

const validReference = (factId: unknown, factRevision: unknown, facts: ReadonlyMap<string, EffectKnownFact>, unknownCode: MysticalEffectPolicyDiagnosticCode, staleCode: MysticalEffectPolicyDiagnosticCode, issues: MysticalEffectPolicyDiagnostic[], recordId: string): EffectKnownFact | undefined => {
  if (!validId(factId, 'fact:') || !positiveInteger(factRevision)) {
    issues.push(diagnostic(recordId, unknownCode))
    return undefined
  }
  const fact = facts.get(factId)
  if (!fact) issues.push(diagnostic(recordId, unknownCode))
  else if (fact.revision !== factRevision) issues.push(diagnostic(recordId, staleCode))
  return fact && fact.revision === factRevision ? fact : undefined
}

const validateBoundedReferences = <Value extends Record<string, unknown>>(
  values: unknown,
  maximum: number,
  shape: (value: unknown) => value is Value,
  prefix: string,
  malformed: MysticalEffectPolicyDiagnosticCode,
  limit: MysticalEffectPolicyDiagnosticCode,
  invalidId: MysticalEffectPolicyDiagnosticCode,
  duplicate: MysticalEffectPolicyDiagnosticCode,
  noncanonical: MysticalEffectPolicyDiagnosticCode,
  validate: (value: Value) => void,
  issues: MysticalEffectPolicyDiagnostic[],
  recordId: string
): readonly Value[] => {
  if (!Array.isArray(values)) {
    issues.push(diagnostic(recordId, malformed))
    return []
  }
  if (values.length === 0 || values.length > maximum) issues.push(diagnostic(recordId, limit))
  const valid: Value[] = []
  const ids: string[] = []
  for (const value of values) {
    if (!shape(value)) {
      issues.push(diagnostic(recordId, malformed))
      continue
    }
    if (!validId(value.id, prefix)) issues.push(diagnostic(recordId, invalidId))
    else ids.push(value.id)
    valid.push(value)
    validate(value)
  }
  if (new Set(ids).size !== ids.length) issues.push(diagnostic(recordId, duplicate))
  if (!canonical(ids)) issues.push(diagnostic(recordId, noncanonical))
  return valid
}

const effectValidationFor = (actionTime: unknown, knownFacts: unknown, effect: unknown) => validateEffectResolutionRequest({
  version: EFFECT_MODEL_CONTRACT_VERSION,
  actionTime,
  knownFacts,
  effects: [effect],
  appliedCounterplayIds: []
})

const baseEffectValidation = (actionTime: unknown, knownFacts: unknown) => validateEffectResolutionRequest({
  version: EFFECT_MODEL_CONTRACT_VERSION,
  actionTime,
  knownFacts,
  effects: [],
  appliedCounterplayIds: []
})

/**
 * Validates only policy eligibility. Existing effects.ts stays the owner of
 * candidate semantics, time, stacking, chains, and counterplay resolution.
 */
export const validateMysticalEffectPolicyAuditRequest = (value: unknown): MysticalEffectPolicyValidation => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'actionTime', 'knownFacts', 'definitions'])) {
    return { version: MYSTICAL_EFFECT_POLICY_VERSION, status: 'rejected', diagnostics: [diagnostic('mystical-policy:request', 'mystical-effect-policy.malformed-request')] }
  }
  const issues: MysticalEffectPolicyDiagnostic[] = []
  if (value.version !== MYSTICAL_EFFECT_POLICY_VERSION) issues.push(diagnostic('mystical-policy:request', 'mystical-effect-policy.invalid-contract-version'))

  const base = baseEffectValidation(value.actionTime, value.knownFacts)
  if (base.status === 'rejected') issues.push(...base.diagnostics.map(item => diagnostic('mystical-policy:request', item.code)))
  const facts = new Map<string, EffectKnownFact>()
  if (Array.isArray(value.knownFacts)) {
    for (const fact of value.knownFacts) {
      if (record(fact) && validId(fact.id, 'fact:') && oneOf(['actor', 'condition', 'preparation', 'equipment', 'grounded-object', 'grounded-practice', 'crew-support', 'enemy', 'environment'] as const, fact.kind) && positiveInteger(fact.revision) && !facts.has(fact.id)) {
        facts.set(fact.id, { id: fact.id, kind: fact.kind, revision: fact.revision })
      }
    }
  }

  if (!Array.isArray(value.definitions)) {
    issues.push(diagnostic('mystical-policy:request', 'mystical-effect-policy.malformed-definition'))
    return { version: MYSTICAL_EFFECT_POLICY_VERSION, status: 'rejected', diagnostics: canonicalDiagnostics(issues) }
  }
  if (value.definitions.length === 0 || value.definitions.length > MYSTICAL_EFFECT_POLICY_LIMITS.definitions) issues.push(diagnostic('mystical-policy:request', 'mystical-effect-policy.definition-limit'))
  const seenDefinitions = new Set<string>()
  const definitionIds: string[] = []
  const seenEffectIds = new Set<string>()
  for (const [index, candidate] of value.definitions.entries()) {
    const recordId = definitionId(candidate, index)
    if (!definitionShape(candidate)) {
      issues.push(diagnostic(recordId, 'mystical-effect-policy.malformed-definition'))
      continue
    }
    if (candidate.version !== MYSTICAL_EFFECT_POLICY_VERSION) issues.push(diagnostic(recordId, 'mystical-effect-policy.invalid-definition-version'))
    if (!validId(candidate.id, 'mystical-policy:')) issues.push(diagnostic(recordId, 'mystical-effect-policy.invalid-definition-id'))
    else {
      definitionIds.push(candidate.id)
      if (seenDefinitions.has(candidate.id)) issues.push(diagnostic(candidate.id, 'mystical-effect-policy.duplicate-definition-id'))
      seenDefinitions.add(candidate.id)
    }
    const form = oneOf(MYSTICAL_EFFECT_FORMS, candidate.form) ? candidate.form : undefined
    const rarity = oneOf(MYSTICAL_EFFECT_RARITIES, candidate.rarity) ? candidate.rarity : undefined
    if (!form) issues.push(diagnostic(recordId, 'mystical-effect-policy.unknown-form'))
    if (!rarity) issues.push(diagnostic(recordId, 'mystical-effect-policy.unknown-rarity'))
    if (!record(candidate.contentSafety)) issues.push(diagnostic(recordId, 'mystical-effect-policy.missing-content-safety'))
    else issues.push(...validateMedievalContentSafety([{ id: recordId, domain: 'data', classification: candidate.contentSafety }]).diagnostics.map(item => diagnostic(recordId, item.code)))

    const effectValidation = effectValidationFor(value.actionTime, value.knownFacts, candidate.effect)
    if (effectValidation.status === 'rejected') {
      issues.push(...effectValidation.diagnostics.map(item => diagnostic(recordId, item.code)))
      continue
    }
    const effect = candidate.effect as EffectCandidate
    if (seenEffectIds.has(effect.id)) issues.push(diagnostic(recordId, 'effects.duplicate-effect-id'))
    seenEffectIds.add(effect.id)
    if (form && !effectCategoriesForForm[form].includes(effect.category)) issues.push(diagnostic(recordId, 'mystical-effect-policy.ineligible-effect-category'))

    const source = candidate.source
    let sourceProvenance: MysticalEffectSourceProvenance | undefined
    let sourceFact: EffectKnownFact | undefined
    if (!record(source) || !hasOnlyKeys(source, ['sourceClass', 'factId', 'factRevision']) || !oneOf(MYSTICAL_EFFECT_SOURCE_CLASSES, source.sourceClass)) {
      issues.push(diagnostic(recordId, 'mystical-effect-policy.malformed-source-provenance'))
    } else {
      sourceProvenance = source as unknown as MysticalEffectSourceProvenance
      sourceFact = validReference(sourceProvenance.factId, sourceProvenance.factRevision, facts, 'mystical-effect-policy.ungrounded-source', 'effects.stale-fact', issues, recordId)
      if (sourceProvenance.factId !== effect.source.factId || sourceProvenance.factRevision !== effect.source.factRevision) issues.push(diagnostic(recordId, 'mystical-effect-policy.source-provenance-mismatch'))
    }

    const conditions = validateBoundedReferences(candidate.conditions, MYSTICAL_EFFECT_POLICY_LIMITS.conditionsPerDefinition, conditionShape, 'condition:', 'mystical-effect-policy.malformed-condition', 'mystical-effect-policy.condition-limit', 'mystical-effect-policy.invalid-condition-id', 'mystical-effect-policy.duplicate-condition-id', 'mystical-effect-policy.noncanonical-condition-order', condition => {
      if (!oneOf(MYSTICAL_EFFECT_CONDITION_KINDS, condition.kind)) issues.push(diagnostic(recordId, 'mystical-effect-policy.unknown-condition-kind'))
      validReference(condition.factId, condition.factRevision, facts, 'mystical-effect-policy.unknown-condition-fact', 'mystical-effect-policy.stale-condition-fact', issues, recordId)
    }, issues, recordId)

    if (!record(candidate.availability) || !hasOnlyKeys(candidate.availability, ['scope', 'maximumDiscoveries', 'maximumConcurrentEligibility'])) {
      issues.push(diagnostic(recordId, 'mystical-effect-policy.malformed-availability'))
    } else if (!rarity || candidate.availability.scope !== 'per-world' || !positiveInteger(candidate.availability.maximumDiscoveries) || !positiveInteger(candidate.availability.maximumConcurrentEligibility) || candidate.availability.maximumDiscoveries > MYSTICAL_EFFECT_RARITY_AVAILABILITY[rarity].maximumDiscoveriesPerWorld || candidate.availability.maximumConcurrentEligibility > MYSTICAL_EFFECT_RARITY_AVAILABILITY[rarity].maximumConcurrentEligibility || candidate.availability.maximumConcurrentEligibility > candidate.availability.maximumDiscoveries) {
      issues.push(diagnostic(recordId, 'mystical-effect-policy.unbounded-availability'))
    }

    if (!record(candidate.useBoundary) || !Object.hasOwn(candidate.useBoundary, 'kind')) {
      issues.push(diagnostic(recordId, 'mystical-effect-policy.malformed-use-boundary'))
    } else if (candidate.useBoundary.kind === 'finite-duration') {
      if (!hasOnlyKeys(candidate.useBoundary, ['kind', 'maximumWorldMinutes']) || !positiveInteger(candidate.useBoundary.maximumWorldMinutes) || candidate.useBoundary.maximumWorldMinutes !== effect.duration.expiresAtWorldTime - effect.duration.startsAtWorldTime) issues.push(diagnostic(recordId, 'mystical-effect-policy.unbounded-use-boundary'))
    } else if (candidate.useBoundary.kind === 'limited-use') {
      if (!hasOnlyKeys(candidate.useBoundary, ['kind', 'maximumUses']) || !positiveInteger(candidate.useBoundary.maximumUses) || candidate.useBoundary.maximumUses > MYSTICAL_EFFECT_POLICY_LIMITS.limitedUses) issues.push(diagnostic(recordId, 'mystical-effect-policy.unbounded-use-boundary'))
    } else issues.push(diagnostic(recordId, 'mystical-effect-policy.malformed-use-boundary'))

    const costs = validateBoundedReferences(candidate.costs, MYSTICAL_EFFECT_POLICY_LIMITS.costsPerDefinition, costShape, 'cost:', 'mystical-effect-policy.malformed-cost', 'mystical-effect-policy.cost-limit', 'mystical-effect-policy.invalid-cost-id', 'mystical-effect-policy.duplicate-cost-id', 'mystical-effect-policy.noncanonical-cost-order', cost => {
      if (!oneOf(MYSTICAL_EFFECT_COST_KINDS, cost.kind)) issues.push(diagnostic(recordId, 'mystical-effect-policy.unknown-cost-kind'))
      validReference(cost.factId, cost.factRevision, facts, 'mystical-effect-policy.unknown-cost-fact', 'mystical-effect-policy.stale-cost-fact', issues, recordId)
    }, issues, recordId)
    if (costs.length === 0) issues.push(diagnostic(recordId, 'mystical-effect-policy.free-or-no-trade-off'))

    const auditEvidence = validateBoundedReferences(candidate.auditEvidence, MYSTICAL_EFFECT_POLICY_LIMITS.auditEvidencePerDefinition, auditEvidenceShape, 'audit:', 'mystical-effect-policy.malformed-audit-evidence', 'mystical-effect-policy.audit-evidence-limit', 'mystical-effect-policy.invalid-audit-evidence-id', 'mystical-effect-policy.duplicate-audit-evidence-id', 'mystical-effect-policy.noncanonical-audit-evidence-order', evidence => {
      if (!oneOf(MYSTICAL_EFFECT_AUDIT_EVIDENCE_KINDS, evidence.kind)) issues.push(diagnostic(recordId, 'mystical-effect-policy.unknown-audit-evidence-kind'))
      validReference(evidence.factId, evidence.factRevision, facts, 'mystical-effect-policy.unknown-audit-evidence-fact', 'mystical-effect-policy.stale-audit-evidence-fact', issues, recordId)
    }, issues, recordId)

    if (sourceProvenance && form) {
      const rule = sourceClassRules[sourceProvenance.sourceClass]
      if (!rule.forms.includes(form) || !rule.categories.includes(effect.category) || sourceFact?.kind !== rule.sourceFactKind || effect.source.grounding !== rule.grounding) issues.push(diagnostic(recordId, 'mystical-effect-policy.invalid-source-class-combination'))
      const requiredCondition = conditions.some(condition => condition.kind === rule.requiredCondition && (sourceProvenance.sourceClass === 'specific-place' ? facts.get(condition.factId as string)?.kind === 'environment' : condition.factId === sourceProvenance.factId && condition.factRevision === sourceProvenance.factRevision))
      if (!requiredCondition) issues.push(diagnostic(recordId, 'mystical-effect-policy.missing-required-condition'))
    }

    const sourceEvidence = sourceProvenance !== undefined && auditEvidence.some(evidence => evidence.kind === 'source-provenance' && evidence.factId === sourceProvenance.factId && evidence.factRevision === sourceProvenance.factRevision)
    const conditionEvidence = conditions.every(condition => auditEvidence.some(evidence => evidence.kind === 'eligibility-condition' && evidence.factId === condition.factId && evidence.factRevision === condition.factRevision))
    const costEvidence = costs.every(cost => auditEvidence.some(evidence => evidence.kind === 'cost-trade-off' && evidence.factId === cost.factId && evidence.factRevision === cost.factRevision))
    const availabilityEvidence = sourceProvenance !== undefined && auditEvidence.some(evidence => evidence.kind === 'availability-bound' && evidence.factId === sourceProvenance.factId && evidence.factRevision === sourceProvenance.factRevision)
    if (!sourceEvidence || !conditionEvidence || !costEvidence || !availabilityEvidence) issues.push(diagnostic(recordId, 'mystical-effect-policy.missing-audit-evidence'))
    if (effect.polarity === 'hindrance' && !auditEvidence.some(evidence => evidence.kind === 'adverse-counterplay' && evidence.factId === effect.counterplay.factId && evidence.factRevision === effect.counterplay.factRevision)) issues.push(diagnostic(recordId, 'mystical-effect-policy.missing-adverse-counterplay'))

    if (candidate.futureReservation !== undefined) {
      if (!record(candidate.futureReservation) || !hasOnlyKeys(candidate.futureReservation, ['kind', 'status']) || !oneOf(MYSTICAL_EFFECT_ULTRA_RARE_RESERVATIONS, candidate.futureReservation.kind) || candidate.futureReservation.status !== 'deferred-no-authority') issues.push(diagnostic(recordId, 'mystical-effect-policy.invalid-future-reservation'))
      else if (rarity !== 'ultra-rare') issues.push(diagnostic(recordId, 'mystical-effect-policy.non-ultra-rare-reservation'))
    }
  }
  if (!canonical(definitionIds)) issues.push(diagnostic('mystical-policy:request', 'mystical-effect-policy.noncanonical-definition-order'))
  const diagnostics = canonicalDiagnostics(issues)
  return diagnostics.length === 0
    ? { version: MYSTICAL_EFFECT_POLICY_VERSION, status: 'accepted', diagnostics: [] }
    : { version: MYSTICAL_EFFECT_POLICY_VERSION, status: 'rejected', diagnostics }
}

/**
 * Produces an inspectable policy-only audit. It never resolves a candidate,
 * advances action time, writes a record, or grants a future reservation use.
 */
export const auditMysticalEffectPolicies = (value: MysticalEffectPolicyAuditRequest | unknown): MysticalEffectPolicyAudit => {
  const validation = validateMysticalEffectPolicyAuditRequest(value)
  if (validation.status === 'rejected') throw new MysticalEffectPolicyContractError(validation.diagnostics)
  const request = value as MysticalEffectPolicyAuditRequest
  return {
    version: MYSTICAL_EFFECT_POLICY_VERSION,
    status: 'accepted',
    reviewed: request.definitions.map(definition => ({
      id: definition.id,
      form: definition.form,
      rarity: definition.rarity,
      effectId: definition.effect.id,
      effectCategory: definition.effect.category,
      sourceClass: definition.source.sourceClass,
      sourceFactId: definition.source.factId,
      conditionIds: definition.conditions.map(condition => condition.id),
      costIds: definition.costs.map(cost => cost.id),
      auditEvidenceIds: definition.auditEvidence.map(evidence => evidence.id),
      availability: { ...definition.availability },
      useBoundary: { ...definition.useBoundary },
      ...(definition.futureReservation === undefined ? {} : { futureReservation: { ...definition.futureReservation } }),
      status: 'policy-audited-only'
    })),
    diagnostics: []
  }
}
