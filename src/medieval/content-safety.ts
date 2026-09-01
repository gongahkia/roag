/**
 * This policy is an input contract for every generated medieval record. It is
 * deliberately metadata-first: text is not trusted to classify itself, and
 * unclassified records are rejected before they can become world data.
 */
export const MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION = 1 as const

export const PROHIBITED_MEDIEVAL_CONTENT_CLASSES = [
  'sexual-violence',
  'slavery',
  'torture',
  'child-harm-or-endangerment'
] as const

export type ProhibitedMedievalContentClass = typeof PROHIBITED_MEDIEVAL_CONTENT_CLASSES[number]

export const MEDIEVAL_CONTENT_DOMAINS = [
  'history',
  'person',
  'place',
  'event',
  'contract',
  'hazard',
  'rumour',
  'enemy-behaviour',
  'simulation-summary',
  'player-facing-text',
  'template',
  'data'
] as const

export type MedievalContentDomain = typeof MEDIEVAL_CONTENT_DOMAINS[number]

export const MEDIEVAL_CONTENT_SAFETY_TAGS = [
  'adult-labour',
  'civil-life',
  'commerce',
  'craft',
  'environment',
  'navigation',
  'nonviolent-tension',
  'ordinary-hardship',
  'settlement',
  'travel'
] as const

export type MedievalContentSafetyTag = typeof MEDIEVAL_CONTENT_SAFETY_TAGS[number]
export type MedievalContentParticipantScope = 'adults-only' | 'not-applicable'

/**
 * Every prohibited class must be affirmatively excluded. An absent or unknown
 * exclusion is rejected, which keeps new generated-content families fail-closed.
 */
export type MedievalContentExclusions = Readonly<Record<ProhibitedMedievalContentClass, 'excluded'>>

export interface MedievalContentSafetyClassification {
  policyVersion: typeof MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
  /** All policy domains served by this record, including its primary domain. */
  domains: readonly MedievalContentDomain[]
  participantScope: MedievalContentParticipantScope
  tags: readonly MedievalContentSafetyTag[]
  exclusions: MedievalContentExclusions
}

/** A small, reusable contract for future generated templates and data. */
export interface ClassifiedMedievalContent {
  id: string
  domain: MedievalContentDomain
  classification: MedievalContentSafetyClassification
}

export type MedievalContentSafetyDiagnosticCode =
  | 'content-safety.malformed-content-record'
  | 'content-safety.missing-content-id'
  | 'content-safety.duplicate-content-id'
  | 'content-safety.unknown-content-domain'
  | 'content-safety.missing-classification'
  | 'content-safety.malformed-classification'
  | 'content-safety.unknown-policy-version'
  | 'content-safety.missing-classification-domains'
  | 'content-safety.unknown-classification-domain'
  | 'content-safety.duplicate-classification-domain'
  | 'content-safety.noncanonical-classification-domain-order'
  | 'content-safety.content-domain-not-classified'
  | 'content-safety.unknown-participant-scope'
  | 'content-safety.missing-tags'
  | 'content-safety.unknown-tag'
  | 'content-safety.duplicate-tag'
  | 'content-safety.noncanonical-tag-order'
  | 'content-safety.malformed-exclusions'
  | `content-safety.prohibited.${ProhibitedMedievalContentClass}`

export interface MedievalContentSafetyDiagnostic {
  code: MedievalContentSafetyDiagnosticCode
  contentId: string
}

export interface AcceptedMedievalContentSafetyValidation {
  policyVersion: typeof MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
  status: 'accepted'
  diagnostics: readonly []
}

export interface RejectedMedievalContentSafetyValidation {
  policyVersion: typeof MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
  status: 'rejected'
  diagnostics: readonly MedievalContentSafetyDiagnostic[]
}

export type MedievalContentSafetyValidation = AcceptedMedievalContentSafetyValidation | RejectedMedievalContentSafetyValidation

export interface MedievalContentSafetyAuditRecord {
  id: string
  domain: MedievalContentDomain
  domains: readonly MedievalContentDomain[]
}

/** Accepted audits are persisted as world-generation provenance. */
export interface MedievalContentSafetyAudit {
  policyVersion: typeof MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
  status: 'accepted'
  reviewed: readonly MedievalContentSafetyAuditRecord[]
  diagnostics: readonly []
}

const isRecord = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const includes = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const isDomain = (value: unknown): value is MedievalContentDomain => includes(MEDIEVAL_CONTENT_DOMAINS, value)
const isTag = (value: unknown): value is MedievalContentSafetyTag => includes(MEDIEVAL_CONTENT_SAFETY_TAGS, value)
const hasOnlyKeys = (value: Record<string, unknown>, keys: readonly string[]): boolean => Object.keys(value).every(key => keys.includes(key)) && keys.every(key => Object.hasOwn(value, key))
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1

const exclusions = (): MedievalContentExclusions => ({
  'sexual-violence': 'excluded',
  slavery: 'excluded',
  torture: 'excluded',
  'child-harm-or-endangerment': 'excluded'
})

/**
 * Generator code should use this rather than hand-writing policy metadata.
 * Runtime validation still verifies imported or saved data independently.
 */
export const classifyMedievalContent = (
  domain: MedievalContentDomain,
  tags: readonly MedievalContentSafetyTag[],
  participantScope: MedievalContentParticipantScope,
  additionalDomains: readonly MedievalContentDomain[] = []
): MedievalContentSafetyClassification => ({
  policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
  domains: [...new Set([domain, ...additionalDomains])].sort(compare),
  participantScope,
  tags: [...new Set(tags)].sort(compare),
  exclusions: exclusions()
})

const diagnostic = (contentId: string, code: MedievalContentSafetyDiagnosticCode): MedievalContentSafetyDiagnostic => ({ contentId, code })

const classificationDiagnostic = (contentId: string, domain: MedievalContentDomain, value: unknown): MedievalContentSafetyDiagnostic | undefined => {
  if (!isRecord(value)) return diagnostic(contentId, 'content-safety.missing-classification')
  if (!hasOnlyKeys(value, ['policyVersion', 'domains', 'participantScope', 'tags', 'exclusions'])) return diagnostic(contentId, 'content-safety.malformed-classification')
  if (value.policyVersion !== MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION) return diagnostic(contentId, 'content-safety.unknown-policy-version')
  const domains = value.domains
  if (!Array.isArray(domains) || domains.length === 0) return diagnostic(contentId, 'content-safety.missing-classification-domains')
  if (!domains.every(isDomain)) return diagnostic(contentId, 'content-safety.unknown-classification-domain')
  if (new Set(domains).size !== domains.length) return diagnostic(contentId, 'content-safety.duplicate-classification-domain')
  if (!domains.every((classifiedDomain, index) => index === 0 || compare(domains[index - 1]!, classifiedDomain) < 0)) return diagnostic(contentId, 'content-safety.noncanonical-classification-domain-order')
  if (!domains.includes(domain)) return diagnostic(contentId, 'content-safety.content-domain-not-classified')
  if (value.participantScope !== 'adults-only' && value.participantScope !== 'not-applicable') return diagnostic(contentId, 'content-safety.unknown-participant-scope')
  const tags = value.tags
  if (!Array.isArray(tags) || tags.length === 0) return diagnostic(contentId, 'content-safety.missing-tags')
  if (!tags.every(isTag)) return diagnostic(contentId, 'content-safety.unknown-tag')
  if (new Set(tags).size !== tags.length) return diagnostic(contentId, 'content-safety.duplicate-tag')
  if (!tags.every((tag, index) => index === 0 || compare(tags[index - 1]!, tag) < 0)) return diagnostic(contentId, 'content-safety.noncanonical-tag-order')
  if (!isRecord(value.exclusions) || !hasOnlyKeys(value.exclusions, PROHIBITED_MEDIEVAL_CONTENT_CLASSES)) return diagnostic(contentId, 'content-safety.malformed-exclusions')
  for (const prohibitedClass of PROHIBITED_MEDIEVAL_CONTENT_CLASSES) {
    if (value.exclusions[prohibitedClass] !== 'excluded') return diagnostic(contentId, `content-safety.prohibited.${prohibitedClass}`)
  }
  return undefined
}

interface OrderedContent {
  value: unknown
  index: number
  id: string
}

const orderedContent = (content: readonly unknown[]): readonly OrderedContent[] => content
  .map((value, index) => ({ value, index, id: isRecord(value) && typeof value.id === 'string' && value.id ? value.id : `#${index}` }))
  .sort((left, right) => compare(left.id, right.id) || left.index - right.index)

/**
 * Validation is deterministic: input order is canonicalized by content id and
 * every rejection uses a stable code. It never retries or mutates content.
 */
export const validateMedievalContentSafety = (content: readonly unknown[]): MedievalContentSafetyValidation => {
  const diagnostics: MedievalContentSafetyDiagnostic[] = []
  const seenIds = new Set<string>()
  for (const candidate of orderedContent(content)) {
    if (!isRecord(candidate.value)) {
      diagnostics.push(diagnostic(candidate.id, 'content-safety.malformed-content-record'))
      continue
    }
    if (typeof candidate.value.id !== 'string' || !candidate.value.id) {
      diagnostics.push(diagnostic(candidate.id, 'content-safety.missing-content-id'))
      continue
    }
    if (seenIds.has(candidate.value.id)) {
      diagnostics.push(diagnostic(candidate.value.id, 'content-safety.duplicate-content-id'))
      continue
    }
    seenIds.add(candidate.value.id)
    if (!isDomain(candidate.value.domain)) {
      diagnostics.push(diagnostic(candidate.value.id, 'content-safety.unknown-content-domain'))
      continue
    }
    const failure = classificationDiagnostic(candidate.value.id, candidate.value.domain, candidate.value.classification)
    if (failure) diagnostics.push(failure)
  }
  if (diagnostics.length) return { policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, status: 'rejected', diagnostics }
  return { policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, status: 'accepted', diagnostics: [] }
}

export const auditMedievalContentSafety = (content: readonly unknown[]): MedievalContentSafetyAudit | RejectedMedievalContentSafetyValidation => {
  const validation = validateMedievalContentSafety(content)
  if (validation.status === 'rejected') return validation
  return {
    policyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
    status: 'accepted',
    reviewed: orderedContent(content).map(candidate => {
      const value = candidate.value as ClassifiedMedievalContent
      return { id: value.id, domain: value.domain, domains: [...value.classification.domains] }
    }),
    diagnostics: []
  }
}

const isAuditRecord = (value: unknown): value is MedievalContentSafetyAuditRecord => {
  if (!isRecord(value) || !hasOnlyKeys(value, ['id', 'domain', 'domains']) || typeof value.id !== 'string' || !value.id || !isDomain(value.domain) || !Array.isArray(value.domains) || value.domains.length === 0 || !value.domains.every(isDomain) || new Set(value.domains).size !== value.domains.length) return false
  const domains = value.domains
  return domains.every((domain, index) => index === 0 || compare(domains[index - 1]!, domain) < 0) && domains.includes(value.domain)
}

export const isMedievalContentSafetyAudit = (value: unknown): value is MedievalContentSafetyAudit => {
  if (!isRecord(value) || !hasOnlyKeys(value, ['policyVersion', 'status', 'reviewed', 'diagnostics']) || value.policyVersion !== MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION || value.status !== 'accepted' || !Array.isArray(value.reviewed) || !Array.isArray(value.diagnostics) || value.diagnostics.length !== 0) return false
  const reviewed = value.reviewed as unknown[]
  if (!reviewed.every(isAuditRecord)) return false
  return reviewed.every((item, index) => index === 0 || compare((reviewed[index - 1] as { id: string }).id, (item as { id: string }).id) < 0)
}

export const contentSafetyAuditMatches = (content: readonly unknown[], audit: unknown): audit is MedievalContentSafetyAudit => {
  const expected = auditMedievalContentSafety(content)
  return expected.status === 'accepted' && JSON.stringify(expected) === JSON.stringify(audit)
}
