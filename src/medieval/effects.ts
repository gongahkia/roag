import { validateMedievalContentSafety, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'

/**
 * A pure, renderer-independent vocabulary for future effect consumers. It is
 * deliberately not FoundationWorld state, a combat resolver, or a recovery
 * system. Callers provide only facts they are already allowed to know.
 */
export const EFFECT_MODEL_CONTRACT_VERSION = 1 as const
export const EFFECT_MODEL_TIME_UNIT = 'minute' as const

export const EFFECT_MODEL_LIMITS = {
  knownFacts: 32,
  effects: 24,
  evidencePerEffect: 4,
  conflictReferencesPerEffect: 8,
  chainPrerequisitesPerEffect: 4,
  chainDepth: 4,
  counterplaySelections: 24,
  durationMinutes: 1_440,
  stackContributors: 4,
  magnitude: 3,
  priority: 9,
  identityLength: 96
} as const

/** Every listed family is a semantic input category, not implemented gameplay. */
export const EFFECT_SOURCE_CATEGORIES = [
  'health',
  'injury',
  'exhaustion',
  'preparation',
  'equipment',
  'relic',
  'totem',
  'boon',
  'curse',
  'crew-support',
  'enemy-weakness',
  'environmental-interaction'
] as const
export type EffectSourceCategory = typeof EFFECT_SOURCE_CATEGORIES[number]

export const EFFECT_KNOWN_FACT_KINDS = [
  'actor',
  'condition',
  'preparation',
  'equipment',
  'grounded-object',
  'grounded-practice',
  'crew-support',
  'enemy',
  'environment'
] as const
export type EffectKnownFactKind = typeof EFFECT_KNOWN_FACT_KINDS[number]

export const EFFECT_TARGET_SCOPES = ['self', 'known-ally', 'known-enemy', 'known-environment'] as const
export type EffectTargetScope = typeof EFFECT_TARGET_SCOPES[number]

export const EFFECT_SOURCE_GROUNDINGS = [
  'ordinary-condition',
  'material-object',
  'place-bound-practice',
  'crew-relationship',
  'environmental-condition'
] as const
export type EffectSourceGrounding = typeof EFFECT_SOURCE_GROUNDINGS[number]

export type EffectPolarity = 'aid' | 'hindrance'
export type EffectStackingMode = 'additive' | 'exclusive'

/** A caller-owned, revisioned fact already known to the resolving context. */
export interface EffectKnownFact {
  id: string
  kind: EffectKnownFactKind
  revision: number
}

/** Only canonical action-world time may make a finite effect active or expired. */
export interface EffectActionTime {
  timeUnit: typeof EFFECT_MODEL_TIME_UNIT
  worldTime: number
}

export interface EffectSourceReference {
  factId: string
  factRevision: number
  grounding: EffectSourceGrounding
}

export interface EffectTargetReference {
  factId: string
  factRevision: number
  scope: EffectTargetScope
}

export interface EffectEvidenceReference {
  id: string
  factId: string
  factRevision: number
}

export interface EffectDuration {
  startsAtWorldTime: number
  expiresAtWorldTime: number
}

export interface EffectStacking {
  groupId: string
  mode: EffectStackingMode
  cap: number
}

/** The named, known-fact response that can suppress this particular effect. */
export interface EffectCounterplay {
  id: string
  factId: string
  factRevision: number
}

/**
 * An application-authored or generated candidate supplied by an owning domain.
 * Its scalar is semantic only; v1 does not interpret it as damage, healing,
 * persuasion, control, combat chance, or any mutable-world transition.
 */
export interface EffectCandidate {
  version: typeof EFFECT_MODEL_CONTRACT_VERSION
  id: string
  category: EffectSourceCategory
  polarity: EffectPolarity
  magnitude: number
  priority: number
  source: EffectSourceReference
  target: EffectTargetReference
  evidence: readonly EffectEvidenceReference[]
  duration: EffectDuration
  stacking: EffectStacking
  conflictsWith: readonly string[]
  chainAfter: readonly string[]
  counterplay: EffectCounterplay
  contentSafety: MedievalContentSafetyClassification
}

/**
 * This input has no world, manifest, renderer, action, callback, cache, or
 * persistence field. Its fact set is the complete knowledge boundary.
 */
export interface EffectResolutionRequest {
  version: typeof EFFECT_MODEL_CONTRACT_VERSION
  actionTime: EffectActionTime
  knownFacts: readonly EffectKnownFact[]
  effects: readonly EffectCandidate[]
  appliedCounterplayIds: readonly string[]
}

export type EffectSuppressionReason =
  | 'not-started-at-action-time'
  | 'expired-at-action-time'
  | 'countered-by-known-response'
  | 'conflict-lost'
  | 'chain-prerequisite-suppressed'
  | 'stack-cap-reached'

export interface ResolvedEffect {
  effectId: string
  category: EffectSourceCategory
  polarity: EffectPolarity
  magnitude: number
  priority: number
  stackGroupId: string
  chainDepth: number
  sourceFactId: string
  targetFactId: string
  evidenceIds: readonly string[]
  counterplayId: string
}

export interface SuppressedEffect {
  effectId: string
  reason: EffectSuppressionReason
  relatedId?: string
}

export interface EffectResolutionGroup {
  groupId: string
  targetFactId: string
  polarity: EffectPolarity
  totalMagnitude: number
  effectIds: readonly string[]
}

/** Explanations contain only supplied source/evidence/target IDs. */
export interface EffectResolutionExplanation {
  effectId: string
  outcome: 'active' | 'suppressed'
  reason: 'active' | EffectSuppressionReason
  sourceFactId: string
  targetFactId: string
  evidenceIds: readonly string[]
  counterplayId: string
  relatedId?: string
}

export interface EffectResolution {
  version: typeof EFFECT_MODEL_CONTRACT_VERSION
  actionTime: EffectActionTime
  active: readonly ResolvedEffect[]
  suppressed: readonly SuppressedEffect[]
  groups: readonly EffectResolutionGroup[]
  explanations: readonly EffectResolutionExplanation[]
}

export type EffectModelDiagnosticCode =
  | 'effects.malformed-request'
  | 'effects.invalid-contract-version'
  | 'effects.malformed-action-time'
  | 'effects.wall-clock-input'
  | 'effects.invalid-action-time'
  | 'effects.known-fact-limit'
  | 'effects.malformed-known-fact'
  | 'effects.invalid-known-fact-id'
  | 'effects.unknown-known-fact-kind'
  | 'effects.invalid-known-fact-revision'
  | 'effects.duplicate-known-fact-id'
  | 'effects.effect-limit'
  | 'effects.malformed-effect'
  | 'effects.invalid-effect-version'
  | 'effects.invalid-effect-id'
  | 'effects.duplicate-effect-id'
  | 'effects.unknown-source-category'
  | 'effects.invalid-polarity'
  | 'effects.invalid-magnitude'
  | 'effects.invalid-priority'
  | 'effects.malformed-source'
  | 'effects.malformed-target'
  | 'effects.malformed-evidence'
  | 'effects.evidence-limit'
  | 'effects.invalid-evidence-id'
  | 'effects.duplicate-evidence-id'
  | 'effects.noncanonical-evidence-order'
  | 'effects.malformed-duration'
  | 'effects.invalid-duration'
  | 'effects.malformed-stacking'
  | 'effects.invalid-stack-group-id'
  | 'effects.invalid-stacking-mode'
  | 'effects.invalid-stack-cap'
  | 'effects.conflicting-stack-rule'
  | 'effects.malformed-conflicts'
  | 'effects.conflict-reference-limit'
  | 'effects.invalid-conflict-id'
  | 'effects.duplicate-conflict-id'
  | 'effects.noncanonical-conflict-order'
  | 'effects.unknown-conflict'
  | 'effects.self-conflict'
  | 'effects.nonreciprocal-conflict'
  | 'effects.malformed-chain'
  | 'effects.chain-reference-limit'
  | 'effects.invalid-chain-id'
  | 'effects.duplicate-chain-id'
  | 'effects.noncanonical-chain-order'
  | 'effects.unknown-chain-prerequisite'
  | 'effects.self-chain-prerequisite'
  | 'effects.cyclic-chain'
  | 'effects.chain-depth-limit'
  | 'effects.malformed-counterplay'
  | 'effects.invalid-counterplay-id'
  | 'effects.invalid-counterplay-selection'
  | 'effects.counterplay-selection-limit'
  | 'effects.duplicate-counterplay-selection'
  | 'effects.unknown-counterplay-selection'
  | 'effects.conflicting-counterplay'
  | 'effects.missing-content-safety'
  | 'effects.unknown-fact'
  | 'effects.stale-fact'
  | 'effects.ineligible-source'
  | 'effects.ineligible-target'
  | 'effects.ineligible-counterplay'
  | 'effects.invalid-source-grounding'
  | MedievalContentSafetyDiagnosticCode

export interface EffectModelDiagnostic {
  recordId: string
  code: EffectModelDiagnosticCode
}

export interface AcceptedEffectResolutionValidation {
  version: typeof EFFECT_MODEL_CONTRACT_VERSION
  status: 'accepted'
  diagnostics: readonly []
}

export interface RejectedEffectResolutionValidation {
  version: typeof EFFECT_MODEL_CONTRACT_VERSION
  status: 'rejected'
  diagnostics: readonly EffectModelDiagnostic[]
}

export type EffectResolutionValidation = AcceptedEffectResolutionValidation | RejectedEffectResolutionValidation

export class EffectModelContractError extends Error {
  constructor(readonly diagnostics: readonly EffectModelDiagnostic[]) {
    super(`effect model rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'EffectModelContractError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const plainRecord = (value: unknown): value is Record<string, unknown> => Boolean(value)
  && typeof value === 'object'
  && !Array.isArray(value)
  && (Object.getPrototypeOf(value) === Object.prototype || Object.getPrototypeOf(value) === null)
const hasOnlyKeys = (value: Record<string, unknown>, keys: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const expected = [...keys].sort(compare)
  return actual.length === expected.length && actual.every((key, index) => key === expected[index])
}
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const nonNegativeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const positiveInteger = (value: unknown): value is number => nonNegativeInteger(value) && value > 0
const validIdentity = (value: unknown, prefix?: string): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= EFFECT_MODEL_LIMITS.identityLength
  && /^[a-z][a-z0-9]*(?::[a-z][a-z0-9-]*)+$/u.test(value)
  && (prefix === undefined || value.startsWith(prefix))
const canonical = (values: readonly string[]): boolean => values.every((value, index) => index === 0 || compare(values[index - 1]!, value) < 0)
const diagnostic = (recordId: string, code: EffectModelDiagnosticCode): EffectModelDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly EffectModelDiagnostic[]): readonly EffectModelDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const candidateRecordId = (value: unknown, index: number): string => plainRecord(value) && typeof value.id === 'string' ? value.id : `effect:#${index}`
const factRecordId = (value: unknown, index: number): string => plainRecord(value) && typeof value.id === 'string' ? value.id : `fact:#${index}`

const sourceFactKinds: Readonly<Record<EffectSourceCategory, readonly EffectKnownFactKind[]>> = {
  health: ['condition'],
  injury: ['condition'],
  exhaustion: ['condition'],
  preparation: ['preparation'],
  equipment: ['equipment'],
  relic: ['grounded-object'],
  totem: ['grounded-object'],
  boon: ['grounded-object', 'grounded-practice'],
  curse: ['grounded-object', 'grounded-practice'],
  'crew-support': ['crew-support'],
  'enemy-weakness': ['enemy'],
  'environmental-interaction': ['environment']
}

const sourceGroundings: Readonly<Record<EffectSourceCategory, readonly EffectSourceGrounding[]>> = {
  health: ['ordinary-condition'],
  injury: ['ordinary-condition'],
  exhaustion: ['ordinary-condition'],
  preparation: ['material-object', 'place-bound-practice'],
  equipment: ['material-object'],
  relic: ['material-object'],
  totem: ['material-object'],
  boon: ['material-object', 'place-bound-practice'],
  curse: ['material-object', 'place-bound-practice'],
  'crew-support': ['crew-relationship'],
  'enemy-weakness': ['ordinary-condition'],
  'environmental-interaction': ['environmental-condition']
}

const targetFactKind = (scope: EffectTargetScope): EffectKnownFactKind => {
  if (scope === 'known-enemy') return 'enemy'
  if (scope === 'known-environment') return 'environment'
  return 'actor'
}

const referenceIssue = (
  issues: EffectModelDiagnostic[],
  recordId: string,
  reference: unknown,
  facts: ReadonlyMap<string, EffectKnownFact>,
  expectedKinds: readonly EffectKnownFactKind[] | undefined,
  malformedCode: EffectModelDiagnosticCode,
  ineligibleCode: EffectModelDiagnosticCode
): void => {
  if (!plainRecord(reference) || !hasOnlyKeys(reference, ['factId', 'factRevision'])) {
    issues.push(diagnostic(recordId, malformedCode))
    return
  }
  if (!validIdentity(reference.factId, 'fact:') || !positiveInteger(reference.factRevision)) {
    issues.push(diagnostic(recordId, malformedCode))
    return
  }
  const fact = facts.get(reference.factId)
  if (!fact) issues.push(diagnostic(recordId, 'effects.unknown-fact'))
  else if (fact.revision !== reference.factRevision) issues.push(diagnostic(recordId, 'effects.stale-fact'))
  else if (expectedKinds && !expectedKinds.includes(fact.kind)) issues.push(diagnostic(recordId, ineligibleCode))
}

const effectShape = (value: unknown): value is Record<string, unknown> => plainRecord(value)
  && hasOnlyKeys(value, ['version', 'id', 'category', 'polarity', 'magnitude', 'priority', 'source', 'target', 'evidence', 'duration', 'stacking', 'conflictsWith', 'chainAfter', 'counterplay', 'contentSafety'])

const graphDepths = (effects: ReadonlyMap<string, EffectCandidate>): Map<string, number> => {
  const depths = new Map<string, number>()
  const depthFor = (id: string): number => {
    const current = depths.get(id)
    if (current !== undefined) return current
    const effect = effects.get(id)!
    const depth = effect.chainAfter.length === 0 ? 0 : Math.max(...effect.chainAfter.map(depthFor)) + 1
    depths.set(id, depth)
    return depth
  }
  for (const id of effects.keys()) depthFor(id)
  return depths
}

const sourceAndTargetIssues = (issues: EffectModelDiagnostic[], effect: EffectCandidate, facts: ReadonlyMap<string, EffectKnownFact>): void => {
  const source = effect.source
  if (!plainRecord(source) || !hasOnlyKeys(source, ['factId', 'factRevision', 'grounding'])) {
    issues.push(diagnostic(effect.id, 'effects.malformed-source'))
  } else {
    referenceIssue(issues, effect.id, { factId: source.factId, factRevision: source.factRevision }, facts, sourceFactKinds[effect.category], 'effects.malformed-source', 'effects.ineligible-source')
    if (!oneOf(EFFECT_SOURCE_GROUNDINGS, source.grounding) || !sourceGroundings[effect.category].includes(source.grounding)) issues.push(diagnostic(effect.id, 'effects.invalid-source-grounding'))
  }
  const target = effect.target
  if (!plainRecord(target) || !hasOnlyKeys(target, ['factId', 'factRevision', 'scope']) || !oneOf(EFFECT_TARGET_SCOPES, target.scope)) {
    issues.push(diagnostic(effect.id, 'effects.malformed-target'))
  } else {
    referenceIssue(issues, effect.id, { factId: target.factId, factRevision: target.factRevision }, facts, [targetFactKind(target.scope)], 'effects.malformed-target', 'effects.ineligible-target')
  }
}

/**
 * Validation is intentionally fail-closed. Top-level input sets may arrive in
 * any order; descriptors' nested ID lists must already be canonical because
 * they are durable authored/generated meaning rather than a caller set.
 */
export const validateEffectResolutionRequest = (value: unknown): EffectResolutionValidation => {
  const issues: EffectModelDiagnostic[] = []
  if (!plainRecord(value) || !hasOnlyKeys(value, ['version', 'actionTime', 'knownFacts', 'effects', 'appliedCounterplayIds'])) {
    return { version: EFFECT_MODEL_CONTRACT_VERSION, status: 'rejected', diagnostics: [diagnostic('effects:request', 'effects.malformed-request')] }
  }
  if (value.version !== EFFECT_MODEL_CONTRACT_VERSION) issues.push(diagnostic('effects:request', 'effects.invalid-contract-version'))
  if (!plainRecord(value.actionTime)) issues.push(diagnostic('effects:request', 'effects.malformed-action-time'))
  else {
    const actionKeys = Object.keys(value.actionTime)
    if (actionKeys.some(key => /wall|clock|date|timestamp/iu.test(key))) issues.push(diagnostic('effects:request', 'effects.wall-clock-input'))
    if (!hasOnlyKeys(value.actionTime, ['timeUnit', 'worldTime'])) issues.push(diagnostic('effects:request', 'effects.malformed-action-time'))
    else if (value.actionTime.timeUnit !== EFFECT_MODEL_TIME_UNIT || !nonNegativeInteger(value.actionTime.worldTime)) issues.push(diagnostic('effects:request', 'effects.invalid-action-time'))
  }

  const knownFacts = Array.isArray(value.knownFacts) ? value.knownFacts : []
  if (!Array.isArray(value.knownFacts)) issues.push(diagnostic('effects:request', 'effects.malformed-known-fact'))
  if (knownFacts.length > EFFECT_MODEL_LIMITS.knownFacts) issues.push(diagnostic('effects:request', 'effects.known-fact-limit'))
  const facts = new Map<string, EffectKnownFact>()
  for (const [index, candidate] of knownFacts.entries()) {
    const recordId = factRecordId(candidate, index)
    if (!plainRecord(candidate) || !hasOnlyKeys(candidate, ['id', 'kind', 'revision'])) {
      issues.push(diagnostic(recordId, 'effects.malformed-known-fact'))
      continue
    }
    if (!validIdentity(candidate.id, 'fact:')) issues.push(diagnostic(recordId, 'effects.invalid-known-fact-id'))
    if (!oneOf(EFFECT_KNOWN_FACT_KINDS, candidate.kind)) issues.push(diagnostic(recordId, 'effects.unknown-known-fact-kind'))
    if (!positiveInteger(candidate.revision)) issues.push(diagnostic(recordId, 'effects.invalid-known-fact-revision'))
    if (typeof candidate.id === 'string' && facts.has(candidate.id)) issues.push(diagnostic(candidate.id, 'effects.duplicate-known-fact-id'))
    if (validIdentity(candidate.id, 'fact:') && oneOf(EFFECT_KNOWN_FACT_KINDS, candidate.kind) && positiveInteger(candidate.revision) && !facts.has(candidate.id)) {
      facts.set(candidate.id, { id: candidate.id, kind: candidate.kind, revision: candidate.revision })
    }
  }

  const candidates = Array.isArray(value.effects) ? value.effects : []
  if (!Array.isArray(value.effects)) issues.push(diagnostic('effects:request', 'effects.malformed-effect'))
  if (candidates.length > EFFECT_MODEL_LIMITS.effects) issues.push(diagnostic('effects:request', 'effects.effect-limit'))
  const effects = new Map<string, EffectCandidate>()
  for (const [index, candidate] of candidates.entries()) {
    const recordId = candidateRecordId(candidate, index)
    if (!effectShape(candidate)) {
      issues.push(diagnostic(recordId, 'effects.malformed-effect'))
      continue
    }
    if (candidate.version !== EFFECT_MODEL_CONTRACT_VERSION) issues.push(diagnostic(recordId, 'effects.invalid-effect-version'))
    if (!validIdentity(candidate.id, 'effect:')) issues.push(diagnostic(recordId, 'effects.invalid-effect-id'))
    if (!oneOf(EFFECT_SOURCE_CATEGORIES, candidate.category)) issues.push(diagnostic(recordId, 'effects.unknown-source-category'))
    if (candidate.polarity !== 'aid' && candidate.polarity !== 'hindrance') issues.push(diagnostic(recordId, 'effects.invalid-polarity'))
    if (!positiveInteger(candidate.magnitude) || candidate.magnitude > EFFECT_MODEL_LIMITS.magnitude) issues.push(diagnostic(recordId, 'effects.invalid-magnitude'))
    if (!nonNegativeInteger(candidate.priority) || candidate.priority > EFFECT_MODEL_LIMITS.priority) issues.push(diagnostic(recordId, 'effects.invalid-priority'))
    if (!plainRecord(candidate.contentSafety)) issues.push(diagnostic(recordId, 'effects.missing-content-safety'))
    else issues.push(...validateMedievalContentSafety([{ id: candidate.id, domain: 'data', classification: candidate.contentSafety }]).diagnostics.map(item => diagnostic(recordId, item.code)))

    const category = oneOf(EFFECT_SOURCE_CATEGORIES, candidate.category) ? candidate.category : undefined
    if (category !== undefined) sourceAndTargetIssues(issues, candidate as unknown as EffectCandidate, facts)
    else if (!plainRecord(candidate.source) || !hasOnlyKeys(candidate.source, ['factId', 'factRevision', 'grounding'])) issues.push(diagnostic(recordId, 'effects.malformed-source'))

    if (!Array.isArray(candidate.evidence)) issues.push(diagnostic(recordId, 'effects.malformed-evidence'))
    else {
      if (candidate.evidence.length === 0 || candidate.evidence.length > EFFECT_MODEL_LIMITS.evidencePerEffect) issues.push(diagnostic(recordId, 'effects.evidence-limit'))
      const evidenceIds: string[] = []
      for (const evidence of candidate.evidence) {
        if (!plainRecord(evidence) || !hasOnlyKeys(evidence, ['id', 'factId', 'factRevision'])) {
          issues.push(diagnostic(recordId, 'effects.malformed-evidence'))
          continue
        }
        if (!validIdentity(evidence.id, 'evidence:')) issues.push(diagnostic(recordId, 'effects.invalid-evidence-id'))
        else evidenceIds.push(evidence.id)
        referenceIssue(issues, recordId, { factId: evidence.factId, factRevision: evidence.factRevision }, facts, undefined, 'effects.malformed-evidence', 'effects.ineligible-source')
      }
      if (new Set(evidenceIds).size !== evidenceIds.length) issues.push(diagnostic(recordId, 'effects.duplicate-evidence-id'))
      if (!canonical(evidenceIds)) issues.push(diagnostic(recordId, 'effects.noncanonical-evidence-order'))
    }

    if (!plainRecord(candidate.duration) || !hasOnlyKeys(candidate.duration, ['startsAtWorldTime', 'expiresAtWorldTime'])) issues.push(diagnostic(recordId, 'effects.malformed-duration'))
    else if (!nonNegativeInteger(candidate.duration.startsAtWorldTime) || !nonNegativeInteger(candidate.duration.expiresAtWorldTime) || candidate.duration.expiresAtWorldTime <= candidate.duration.startsAtWorldTime || candidate.duration.expiresAtWorldTime - candidate.duration.startsAtWorldTime > EFFECT_MODEL_LIMITS.durationMinutes) issues.push(diagnostic(recordId, 'effects.invalid-duration'))

    if (!plainRecord(candidate.stacking) || !hasOnlyKeys(candidate.stacking, ['groupId', 'mode', 'cap'])) issues.push(diagnostic(recordId, 'effects.malformed-stacking'))
    else {
      if (!validIdentity(candidate.stacking.groupId, 'stack:')) issues.push(diagnostic(recordId, 'effects.invalid-stack-group-id'))
      if (candidate.stacking.mode !== 'additive' && candidate.stacking.mode !== 'exclusive') issues.push(diagnostic(recordId, 'effects.invalid-stacking-mode'))
      if (!positiveInteger(candidate.stacking.cap) || candidate.stacking.cap > EFFECT_MODEL_LIMITS.stackContributors || (candidate.stacking.mode === 'exclusive' && candidate.stacking.cap !== 1)) issues.push(diagnostic(recordId, 'effects.invalid-stack-cap'))
    }

    const validateReferences = (references: unknown, maximum: number, malformed: EffectModelDiagnosticCode, limit: EffectModelDiagnosticCode, invalid: EffectModelDiagnosticCode, duplicate: EffectModelDiagnosticCode, order: EffectModelDiagnosticCode): readonly string[] => {
      if (!Array.isArray(references)) {
        issues.push(diagnostic(recordId, malformed))
        return []
      }
      if (references.length > maximum) issues.push(diagnostic(recordId, limit))
      const ids = references.filter((reference): reference is string => typeof reference === 'string')
      if (ids.length !== references.length || !ids.every(id => validIdentity(id, 'effect:'))) issues.push(diagnostic(recordId, invalid))
      if (new Set(ids).size !== ids.length) issues.push(diagnostic(recordId, duplicate))
      if (!canonical(ids)) issues.push(diagnostic(recordId, order))
      return ids
    }
    validateReferences(candidate.conflictsWith, EFFECT_MODEL_LIMITS.conflictReferencesPerEffect, 'effects.malformed-conflicts', 'effects.conflict-reference-limit', 'effects.invalid-conflict-id', 'effects.duplicate-conflict-id', 'effects.noncanonical-conflict-order')
    validateReferences(candidate.chainAfter, EFFECT_MODEL_LIMITS.chainPrerequisitesPerEffect, 'effects.malformed-chain', 'effects.chain-reference-limit', 'effects.invalid-chain-id', 'effects.duplicate-chain-id', 'effects.noncanonical-chain-order')

    if (!plainRecord(candidate.counterplay) || !hasOnlyKeys(candidate.counterplay, ['id', 'factId', 'factRevision'])) issues.push(diagnostic(recordId, 'effects.malformed-counterplay'))
    else {
      if (!validIdentity(candidate.counterplay.id, 'counterplay:')) issues.push(diagnostic(recordId, 'effects.invalid-counterplay-id'))
      referenceIssue(issues, recordId, { factId: candidate.counterplay.factId, factRevision: candidate.counterplay.factRevision }, facts, ['preparation', 'equipment', 'grounded-object', 'crew-support', 'environment'], 'effects.malformed-counterplay', 'effects.ineligible-counterplay')
    }

    if (validIdentity(candidate.id, 'effect:') && !effects.has(candidate.id)) effects.set(candidate.id, candidate as unknown as EffectCandidate)
    else if (typeof candidate.id === 'string' && effects.has(candidate.id)) issues.push(diagnostic(candidate.id, 'effects.duplicate-effect-id'))
  }

  const baseDiagnostics = canonicalDiagnostics(issues)
  if (baseDiagnostics.length) return { version: EFFECT_MODEL_CONTRACT_VERSION, status: 'rejected', diagnostics: baseDiagnostics }

  const counterplays = new Map<string, EffectCounterplay>()
  for (const effect of effects.values()) {
    for (const conflictId of effect.conflictsWith) {
      const conflict = effects.get(conflictId)
      if (!conflict) issues.push(diagnostic(effect.id, 'effects.unknown-conflict'))
      else if (conflictId === effect.id) issues.push(diagnostic(effect.id, 'effects.self-conflict'))
      else if (!conflict.conflictsWith.includes(effect.id)) issues.push(diagnostic(effect.id, 'effects.nonreciprocal-conflict'))
    }
    for (const prerequisiteId of effect.chainAfter) {
      if (!effects.has(prerequisiteId)) issues.push(diagnostic(effect.id, 'effects.unknown-chain-prerequisite'))
      else if (prerequisiteId === effect.id) issues.push(diagnostic(effect.id, 'effects.self-chain-prerequisite'))
    }
    const existingCounterplay = counterplays.get(effect.counterplay.id)
    if (existingCounterplay && (existingCounterplay.factId !== effect.counterplay.factId || existingCounterplay.factRevision !== effect.counterplay.factRevision)) issues.push(diagnostic(effect.id, 'effects.conflicting-counterplay'))
    else counterplays.set(effect.counterplay.id, effect.counterplay)
  }

  const crossReferenceDiagnostics = canonicalDiagnostics(issues)
  if (crossReferenceDiagnostics.length) return { version: EFFECT_MODEL_CONTRACT_VERSION, status: 'rejected', diagnostics: crossReferenceDiagnostics }

  const visiting = new Set<string>()
  const visited = new Set<string>()
  const visit = (id: string): void => {
    if (visited.has(id) || !effects.has(id)) return
    if (visiting.has(id)) {
      issues.push(diagnostic(id, 'effects.cyclic-chain'))
      return
    }
    visiting.add(id)
    for (const dependency of effects.get(id)!.chainAfter) visit(dependency)
    visiting.delete(id)
    visited.add(id)
  }
  for (const id of effects.keys()) visit(id)
  if (!issues.some(item => item.code === 'effects.cyclic-chain')) {
    for (const [id, depth] of graphDepths(effects)) if (depth > EFFECT_MODEL_LIMITS.chainDepth) issues.push(diagnostic(id, 'effects.chain-depth-limit'))
  }

  const stackRules = new Map<string, EffectStacking & Pick<EffectCandidate, 'target' | 'polarity'>>()
  for (const effect of effects.values()) {
    const existing = stackRules.get(effect.stacking.groupId)
    if (existing && (existing.mode !== effect.stacking.mode || existing.cap !== effect.stacking.cap || existing.target.factId !== effect.target.factId || existing.target.factRevision !== effect.target.factRevision || existing.polarity !== effect.polarity)) issues.push(diagnostic(effect.id, 'effects.conflicting-stack-rule'))
    else stackRules.set(effect.stacking.groupId, { ...effect.stacking, target: effect.target, polarity: effect.polarity })
  }

  const selections = Array.isArray(value.appliedCounterplayIds) ? value.appliedCounterplayIds : []
  if (!Array.isArray(value.appliedCounterplayIds)) issues.push(diagnostic('effects:request', 'effects.invalid-counterplay-selection'))
  if (selections.length > EFFECT_MODEL_LIMITS.counterplaySelections) issues.push(diagnostic('effects:request', 'effects.counterplay-selection-limit'))
  const selectionIds = selections.filter((selection): selection is string => typeof selection === 'string')
  if (selectionIds.length !== selections.length || !selectionIds.every(selection => validIdentity(selection, 'counterplay:'))) issues.push(diagnostic('effects:request', 'effects.invalid-counterplay-selection'))
  if (new Set(selectionIds).size !== selectionIds.length) issues.push(diagnostic('effects:request', 'effects.duplicate-counterplay-selection'))
  for (const id of selectionIds) if (!counterplays.has(id)) issues.push(diagnostic(id, 'effects.unknown-counterplay-selection'))

  const diagnostics = canonicalDiagnostics(issues)
  return diagnostics.length === 0
    ? { version: EFFECT_MODEL_CONTRACT_VERSION, status: 'accepted', diagnostics: [] }
    : { version: EFFECT_MODEL_CONTRACT_VERSION, status: 'rejected', diagnostics }
}

const conflictWinner = (left: EffectCandidate, right: EffectCandidate): EffectCandidate => left.priority !== right.priority
  ? left.priority > right.priority ? left : right
  : compare(left.id, right.id) <= 0 ? left : right

/**
 * Resolves only the supplied semantic candidates. It neither advances time nor
 * mutates its request, and emits no record ID that was not supplied by it.
 */
export const resolveEffects = (value: EffectResolutionRequest | unknown): EffectResolution => {
  const validation = validateEffectResolutionRequest(value)
  if (validation.status === 'rejected') throw new EffectModelContractError(validation.diagnostics)
  const request = value as EffectResolutionRequest
  const effects = new Map([...request.effects].map(effect => [effect.id, effect] as const))
  const depths = graphDepths(effects)
  const ordered = [...effects.values()].sort((left, right) => depths.get(left.id)! - depths.get(right.id)! || right.priority - left.priority || compare(left.id, right.id))
  const appliedCounterplay = new Set(request.appliedCounterplayIds)
  const suppressed = new Map<string, SuppressedEffect>()

  for (const effect of ordered) {
    if (request.actionTime.worldTime < effect.duration.startsAtWorldTime) suppressed.set(effect.id, { effectId: effect.id, reason: 'not-started-at-action-time' })
    else if (request.actionTime.worldTime >= effect.duration.expiresAtWorldTime) suppressed.set(effect.id, { effectId: effect.id, reason: 'expired-at-action-time' })
    else if (appliedCounterplay.has(effect.counterplay.id)) suppressed.set(effect.id, { effectId: effect.id, reason: 'countered-by-known-response', relatedId: effect.counterplay.id })
  }

  for (const effect of ordered) {
    if (suppressed.has(effect.id)) continue
    for (const conflictId of effect.conflictsWith) {
      const other = effects.get(conflictId)!
      if (suppressed.has(other.id)) continue
      const winner = conflictWinner(effect, other)
      const loser = winner.id === effect.id ? other : effect
      suppressed.set(loser.id, { effectId: loser.id, reason: 'conflict-lost', relatedId: winner.id })
    }
  }

  const active: ResolvedEffect[] = []
  const activeIds = new Set<string>()
  const groupCounts = new Map<string, number>()
  for (const effect of ordered) {
    if (suppressed.has(effect.id)) continue
    const blockedBy = effect.chainAfter.find(prerequisiteId => !activeIds.has(prerequisiteId))
    if (blockedBy !== undefined) {
      suppressed.set(effect.id, { effectId: effect.id, reason: 'chain-prerequisite-suppressed', relatedId: blockedBy })
      continue
    }
    const count = groupCounts.get(effect.stacking.groupId) ?? 0
    if (count >= effect.stacking.cap) {
      suppressed.set(effect.id, { effectId: effect.id, reason: 'stack-cap-reached', relatedId: effect.stacking.groupId })
      continue
    }
    groupCounts.set(effect.stacking.groupId, count + 1)
    activeIds.add(effect.id)
    active.push({
      effectId: effect.id,
      category: effect.category,
      polarity: effect.polarity,
      magnitude: effect.magnitude,
      priority: effect.priority,
      stackGroupId: effect.stacking.groupId,
      chainDepth: depths.get(effect.id)!,
      sourceFactId: effect.source.factId,
      targetFactId: effect.target.factId,
      evidenceIds: [...effect.evidence.map(evidence => evidence.id)],
      counterplayId: effect.counterplay.id
    })
  }

  const groups = [...new Set(active.map(effect => effect.stackGroupId))].sort(compare).map(groupId => {
    const contributors = active.filter(effect => effect.stackGroupId === groupId)
    return {
      groupId,
      targetFactId: contributors[0]!.targetFactId,
      polarity: contributors[0]!.polarity,
      totalMagnitude: contributors.reduce((total, effect) => total + effect.magnitude, 0),
      effectIds: contributors.map(effect => effect.effectId)
    }
  })
  const suppressedOrdered = [...suppressed.values()].sort((left, right) => compare(left.effectId, right.effectId))
  const explanations = ordered.map(effect => {
    const suppression = suppressed.get(effect.id)
    return {
      effectId: effect.id,
      outcome: suppression ? 'suppressed' as const : 'active' as const,
      reason: suppression ? suppression.reason : 'active' as const,
      sourceFactId: effect.source.factId,
      targetFactId: effect.target.factId,
      evidenceIds: [...effect.evidence.map(evidence => evidence.id)],
      counterplayId: effect.counterplay.id,
      ...(suppression?.relatedId === undefined ? {} : { relatedId: suppression.relatedId })
    }
  })
  return {
    version: EFFECT_MODEL_CONTRACT_VERSION,
    actionTime: { timeUnit: EFFECT_MODEL_TIME_UNIT, worldTime: request.actionTime.worldTime },
    active,
    suppressed: suppressedOrdered,
    groups,
    explanations
  }
}
