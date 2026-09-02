import { auditMedievalContentSafety, classifyMedievalContent, contentSafetyAuditMatches, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { DelegatedTaskRecord, DelegationTaskFamily } from './delegation'
import { PERSISTENT_PERSON_LIMITS, type PersistentPersonMemory, type PersistentPersonRecord } from './persistent-person'
import { hashSeed } from './rng'

/**
 * Social memory is a compact household record of concrete shared delegation
 * events. It is neither a reputation score nor a hierarchy, vendetta, or
 * generic narrative-event system.
 */
export const SOCIAL_MEMORY_CONTRACT_VERSION = 1 as const
export const SOCIAL_MEMORY_STATE_VERSION = 1 as const

export const SOCIAL_MEMORY_LIMITS = {
  records: 48,
  retainedPersonalLinks: 11,
  identityLength: 160
} as const

export const SOCIAL_MEMORY_PHASES = [
  'offer-accepted',
  'offer-refused',
  'task-completed',
  'task-interrupted'
] as const
export type SocialMemoryPhase = typeof SOCIAL_MEMORY_PHASES[number]

export const SOCIAL_MEMORY_DISPOSITIONS = ['cooperative', 'declined', 'interrupted'] as const
export type SocialMemoryDisposition = typeof SOCIAL_MEMORY_DISPOSITIONS[number]

export const SOCIAL_MEMORY_SIGNIFICANCE = ['routine', 'notable'] as const
export type SocialMemorySignificance = typeof SOCIAL_MEMORY_SIGNIFICANCE[number]

export const SOCIAL_MEMORY_FACTOR_CODES = [
  'source-delegation-offer',
  'source-canonical-task-completion',
  'source-delegation-interruption',
  'disposition-cooperative',
  'disposition-declined',
  'disposition-interrupted',
  'significance-routine',
  'significance-notable'
] as const
export type SocialMemoryFactorCode = typeof SOCIAL_MEMORY_FACTOR_CODES[number]

export const SOCIAL_MEMORY_EVIDENCE_SURFACES = [
  'management-sidebar',
  'tavern-conversation',
  'ledger',
  'message',
  'physical-notice',
  'rumour',
  'goods-mark',
  'route-evidence',
  'visible-work'
] as const
export type SocialMemoryEvidenceSurface = typeof SOCIAL_MEMORY_EVIDENCE_SURFACES[number]
export type SocialMemoryEvidenceAvailability = 'rendered-now' | 'future-route-only'

export const SOCIAL_MEMORY_RECALL_BANDS = ['none', 'supportive', 'unsettled'] as const
export type SocialMemoryRecallBand = typeof SOCIAL_MEMORY_RECALL_BANDS[number]

export interface SocialMemoryTaskSource {
  kind: 'delegation-offer' | 'canonical-task-completion' | 'delegation-interruption'
  taskId: string
  offerId: string
  family: DelegationTaskFamily
}

/** A record is retained even after individual references have aged out. */
export interface SocialMemoryRecord {
  version: typeof SOCIAL_MEMORY_CONTRACT_VERSION
  id: string
  phase: SocialMemoryPhase
  source: SocialMemoryTaskSource
  participantPersonIds: readonly string[]
  occurredAtWorldTime: number
  knownAtWorldTime: number
  disposition: SocialMemoryDisposition
  significance: SocialMemorySignificance
  factors: readonly SocialMemoryFactorCode[]
  retention: 'participant-retained' | 'household-record-only'
  token: string
  contentSafety: MedievalContentSafetyClassification
}

export interface SocialMemoryState {
  version: typeof SOCIAL_MEMORY_STATE_VERSION
  records: readonly SocialMemoryRecord[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

export interface SocialMemoryContext {
  worldId: string
  creationDigest: string
  worldTime: number
  people: readonly PersistentPersonRecord[]
  tasks: readonly DelegatedTaskRecord[]
}

export interface SocialMemoryTransition {
  state: SocialMemoryState
  people: readonly PersistentPersonRecord[]
  createdRecordIds: readonly string[]
}

export interface SocialMemoryRecall {
  band: SocialMemoryRecallBand
  recordId?: string
  occurredAtWorldTime?: number
}

export interface SocialMemoryEvidenceRoute {
  surface: SocialMemoryEvidenceSurface
  availability: SocialMemoryEvidenceAvailability
  sourceRecordId: string
  knownAtWorldTime: number
}

export type SocialMemoryDiagnosticCode =
  | 'social-memory.malformed-state'
  | 'social-memory.invalid-version'
  | 'social-memory.invalid-context'
  | 'social-memory.record-limit'
  | 'social-memory.invalid-record'
  | 'social-memory.duplicate-record-id'
  | 'social-memory.noncanonical-order'
  | 'social-memory.invalid-source'
  | 'social-memory.invalid-participant'
  | 'social-memory.invalid-token'
  | 'social-memory.invalid-retention'
  | 'social-memory.invalid-person-link'
  | 'social-memory.missing-task-record'
  | 'social-memory.unexpected-record'
  | 'social-memory.invalid-content-audit'
  | MedievalContentSafetyDiagnosticCode

export interface SocialMemoryDiagnostic {
  recordId: string
  code: SocialMemoryDiagnosticCode
}

export class SocialMemoryContractError extends Error {
  constructor(readonly diagnostics: readonly SocialMemoryDiagnostic[]) {
    super(`social memory rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'SocialMemoryContractError'
  }
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const validId = (value: unknown): value is string => typeof value === 'string' && value.length > 0 && value.length <= SOCIAL_MEMORY_LIMITS.identityLength && /^[a-z][a-z0-9:._-]*$/i.test(value)
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const sortedById = <Value extends { id: string }>(values: readonly Value[]): readonly Value[] => [...values].sort((left, right) => compare(left.id, right.id))
const isSortedById = <Value extends { id: string }>(values: readonly Value[]): boolean => values.every((value, index) => index === 0 || compare(values[index - 1]!.id, value.id) < 0)
const canonicalFactors = (factors: readonly SocialMemoryFactorCode[]): readonly SocialMemoryFactorCode[] => [...new Set(factors)].sort(compare)
const issue = (recordId: string, code: SocialMemoryDiagnosticCode): SocialMemoryDiagnostic => ({ recordId, code })
const canonicalIssues = (issues: readonly SocialMemoryDiagnostic[]): readonly SocialMemoryDiagnostic[] => [...new Map(issues.map(value => [`${value.recordId}\u0000${value.code}`, value])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const classification = (): MedievalContentSafetyClassification => classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data', 'history'])
const socialContent = (value: Pick<SocialMemoryState, 'records'>): readonly ClassifiedMedievalContent[] => value.records.map(item => ({ id: `social-memory:${item.id}`, domain: 'event' as const, classification: item.contentSafety }))
const audit = (value: Pick<SocialMemoryState, 'records'>): MedievalContentSafetyAudit => {
  const result = auditMedievalContentSafety(socialContent(value))
  if (result.status === 'rejected') throw new SocialMemoryContractError(result.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  return result
}

const phaseForTask = (task: DelegatedTaskRecord): readonly SocialMemoryPhase[] => {
  if (task.status === 'refused') return ['offer-refused']
  if (task.status === 'in-progress') return ['offer-accepted']
  if (task.status === 'completed') return ['offer-accepted', 'task-completed']
  return ['offer-accepted', 'task-interrupted']
}

const occurredAtFor = (task: DelegatedTaskRecord, phase: SocialMemoryPhase): number => phase === 'offer-accepted' || phase === 'offer-refused'
  ? task.offeredAtWorldTime
  : task.outcome!.atWorldTime

const sourceKindFor = (phase: SocialMemoryPhase): SocialMemoryTaskSource['kind'] => phase === 'task-completed'
  ? 'canonical-task-completion'
  : phase === 'task-interrupted'
    ? 'delegation-interruption'
    : 'delegation-offer'

const dispositionFor = (phase: SocialMemoryPhase): SocialMemoryDisposition => phase === 'offer-refused'
  ? 'declined'
  : phase === 'task-interrupted'
    ? 'interrupted'
    : 'cooperative'

const significanceFor = (task: DelegatedTaskRecord, phase: SocialMemoryPhase): SocialMemorySignificance => phase === 'task-interrupted' || task.assessment.risk === 'high' ? 'notable' : 'routine'
const socialMemoryIdFor = (task: DelegatedTaskRecord, phase: SocialMemoryPhase): string => `social-memory:${task.id}:${phase}`
const tokenFor = (context: Pick<SocialMemoryContext, 'worldId' | 'creationDigest'>, task: DelegatedTaskRecord, phase: SocialMemoryPhase, occurredAtWorldTime: number): string => {
  const material = `jomon-social-memory-v${SOCIAL_MEMORY_CONTRACT_VERSION}|${context.worldId}|${context.creationDigest}|${task.id}|${task.offerId}|${phase}|${occurredAtWorldTime}`
  return `${hashSeed(material).toString(36)}-${hashSeed([...material].reverse().join(''), 0x9e3779b9).toString(36)}`
}

/** Stable record identity for a real delegation result; it never depends on action batching. */
export const socialMemoryRecordForTask = (context: Pick<SocialMemoryContext, 'worldId' | 'creationDigest'>, task: DelegatedTaskRecord, phase: SocialMemoryPhase, retention: SocialMemoryRecord['retention'] = 'participant-retained'): SocialMemoryRecord => {
  const occurredAtWorldTime = occurredAtFor(task, phase)
  const disposition = dispositionFor(phase)
  const significance = significanceFor(task, phase)
  return {
    version: SOCIAL_MEMORY_CONTRACT_VERSION,
    id: socialMemoryIdFor(task, phase),
    phase,
    source: { kind: sourceKindFor(phase), taskId: task.id, offerId: task.offerId, family: task.family },
    participantPersonIds: [task.courierId, task.recipientId].sort(compare),
    occurredAtWorldTime,
    knownAtWorldTime: occurredAtWorldTime,
    disposition,
    significance,
    factors: canonicalFactors([
      sourceKindFor(phase) === 'delegation-offer' ? 'source-delegation-offer' : sourceKindFor(phase) === 'canonical-task-completion' ? 'source-canonical-task-completion' : 'source-delegation-interruption',
      disposition === 'cooperative' ? 'disposition-cooperative' : disposition === 'declined' ? 'disposition-declined' : 'disposition-interrupted',
      significance === 'routine' ? 'significance-routine' : 'significance-notable'
    ]),
    retention,
    token: tokenFor(context, task, phase, occurredAtWorldTime),
    contentSafety: classification()
  }
}

const expectedRecords = (context: SocialMemoryContext): readonly SocialMemoryRecord[] => sortedById(context.tasks.flatMap(task => phaseForTask(task).map(phase => socialMemoryRecordForTask(context, task, phase))))

const validTaskForSocialContext = (value: unknown): value is DelegatedTaskRecord => record(value)
  && validId(value.id)
  && typeof value.offerId === 'string' && value.offerId.length > 0 && value.offerId.length <= 48
  && validId(value.courierId)
  && validId(value.recipientId)
  && value.courierId !== value.recipientId
  && typeof value.family === 'string'
  && (value.status === 'refused' || value.status === 'in-progress' || value.status === 'completed' || value.status === 'interrupted')
  && safeInteger(value.offeredAtWorldTime)
  && (value.status === 'in-progress' || (record(value.outcome) && safeInteger(value.outcome.atWorldTime)))

const validContext = (context: SocialMemoryContext): boolean => validId(context.worldId)
  && typeof context.creationDigest === 'string' && context.creationDigest.length > 0 && context.creationDigest.length <= SOCIAL_MEMORY_LIMITS.identityLength
  && safeInteger(context.worldTime)
  && Array.isArray(context.people)
  && Array.isArray(context.tasks)
  && context.people.every(person => record(person) && validId(person.id) && Array.isArray(person.memories))
  && context.tasks.every(validTaskForSocialContext)

const validRecordShape = (value: unknown): value is SocialMemoryRecord => record(value)
  && hasOnlyKeys(value, ['version', 'id', 'phase', 'source', 'participantPersonIds', 'occurredAtWorldTime', 'knownAtWorldTime', 'disposition', 'significance', 'factors', 'retention', 'token', 'contentSafety'])
  && value.version === SOCIAL_MEMORY_CONTRACT_VERSION
  && validId(value.id)
  && oneOf(SOCIAL_MEMORY_PHASES, value.phase)
  && record(value.source)
  && hasOnlyKeys(value.source, ['kind', 'taskId', 'offerId', 'family'])
  && (value.source.kind === 'delegation-offer' || value.source.kind === 'canonical-task-completion' || value.source.kind === 'delegation-interruption')
  && validId(value.source.taskId)
  && typeof value.source.offerId === 'string' && value.source.offerId.length > 0 && value.source.offerId.length <= 48
  && typeof value.source.family === 'string'
  && Array.isArray(value.participantPersonIds)
  && value.participantPersonIds.length === 2
  && value.participantPersonIds.every(validId)
  && value.participantPersonIds[0] !== value.participantPersonIds[1]
  && isSortedById(value.participantPersonIds.map(id => ({ id })))
  && safeInteger(value.occurredAtWorldTime)
  && safeInteger(value.knownAtWorldTime)
  && value.knownAtWorldTime >= value.occurredAtWorldTime
  && oneOf(SOCIAL_MEMORY_DISPOSITIONS, value.disposition)
  && oneOf(SOCIAL_MEMORY_SIGNIFICANCE, value.significance)
  && Array.isArray(value.factors)
  && value.factors.length > 0
  && value.factors.every(factor => oneOf(SOCIAL_MEMORY_FACTOR_CODES, factor))
  && same(value.factors, canonicalFactors(value.factors))
  && (value.retention === 'participant-retained' || value.retention === 'household-record-only')
  && typeof value.token === 'string' && value.token.length > 0 && value.token.length <= SOCIAL_MEMORY_LIMITS.identityLength
  && auditMedievalContentSafety([{ id: `social-memory:${value.id}`, domain: 'event', classification: value.contentSafety }]).status === 'accepted'

const socialMemoryLinksFor = (people: readonly PersistentPersonRecord[]): readonly { personId: string; memory: Extract<PersistentPersonMemory, { kind: 'social-memory' }> }[] => people.flatMap(person => person.memories.flatMap(memory => memory.kind === 'social-memory' ? [{ personId: person.id, memory }] : []))

/** Validates expected sources and the source-linked personal memory boundary. */
export const validateSocialMemoryState = (context: SocialMemoryContext, value: unknown): readonly SocialMemoryDiagnostic[] => {
  const issues: SocialMemoryDiagnostic[] = []
  if (!validContext(context)) return [issue('social-memory:context', 'social-memory.invalid-context')]
  if (!record(value) || !hasOnlyKeys(value, ['version', 'records', 'contentSafetyAudit'])) return [issue('social-memory', 'social-memory.malformed-state')]
  if (value.version !== SOCIAL_MEMORY_STATE_VERSION) issues.push(issue('social-memory', 'social-memory.invalid-version'))
  if (!Array.isArray(value.records)) return canonicalIssues([...issues, issue('social-memory:records', 'social-memory.malformed-state')])
  const records = value.records
  if (records.length > SOCIAL_MEMORY_LIMITS.records) issues.push(issue('social-memory:records', 'social-memory.record-limit'))
  const rawContent = records.flatMap(candidate => record(candidate) && typeof candidate.id === 'string' && candidate.contentSafety !== undefined
    ? [{ id: `social-memory:${candidate.id}`, domain: 'event' as const, classification: candidate.contentSafety as MedievalContentSafetyClassification }]
    : [])
  const rawAudit = auditMedievalContentSafety(rawContent)
  if (rawAudit.status === 'rejected') issues.push(...rawAudit.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  if (!records.every(validRecordShape)) issues.push(issue('social-memory:records', 'social-memory.invalid-record'))
  const typedRecords = records.filter(validRecordShape)
  if (new Set(typedRecords.map(item => item.id)).size !== typedRecords.length) issues.push(issue('social-memory:records', 'social-memory.duplicate-record-id'))
  if (!isSortedById(typedRecords)) issues.push(issue('social-memory:records', 'social-memory.noncanonical-order'))
  const peopleById = new Map(context.people.map(person => [person.id, person]))
  const expected = expectedRecords(context)
  const expectedById = new Map(expected.map(item => [item.id, item]))
  for (const candidate of typedRecords) {
    const expectedRecord = expectedById.get(candidate.id)
    if (!expectedRecord) {
      issues.push(issue(candidate.id, 'social-memory.unexpected-record'))
      continue
    }
    if (!candidate.participantPersonIds.every(personId => {
      const participant = peopleById.get(personId)
      return participant !== undefined && record(participant.identity) && participant.identity.adult === true && record(participant.life) && participant.life.status === 'living'
    })) issues.push(issue(candidate.id, 'social-memory.invalid-participant'))
    const sourceTask = context.tasks.find(task => task.id === expectedRecord.source.taskId)
    if (!sourceTask) {
      issues.push(issue(candidate.id, 'social-memory.invalid-source'))
      continue
    }
    const expectedWithRetention = socialMemoryRecordForTask(context, sourceTask, candidate.phase, candidate.retention)
    if (!same(candidate, expectedWithRetention)) {
      if (candidate.token !== expectedWithRetention.token) issues.push(issue(candidate.id, 'social-memory.invalid-token'))
      else if (!same(candidate.source, expectedWithRetention.source)) issues.push(issue(candidate.id, 'social-memory.invalid-source'))
      else issues.push(issue(candidate.id, 'social-memory.invalid-record'))
    }
  }
  for (const expectedRecord of expected) if (!typedRecords.some(item => item.id === expectedRecord.id)) issues.push(issue(expectedRecord.id, 'social-memory.missing-task-record'))

  const recordsById = new Map(typedRecords.map(item => [item.id, item]))
  const links = socialMemoryLinksFor(context.people)
  for (const link of links) {
    const social = recordsById.get(link.memory.socialMemoryId)
    if (!social || !social.participantPersonIds.includes(link.personId) || link.memory.atWorldTime !== social.occurredAtWorldTime || !same(link.memory.contentSafety, social.contentSafety)) {
      issues.push(issue(link.memory.id, 'social-memory.invalid-person-link'))
      continue
    }
    const expectedRelation = link.personId === context.tasks.find(task => task.id === social.source.taskId)?.courierId ? 'courier' : 'recipient'
    if (link.memory.relation !== expectedRelation) issues.push(issue(link.memory.id, 'social-memory.invalid-person-link'))
  }
  for (const social of typedRecords) {
    const retained = links.some(link => link.memory.socialMemoryId === social.id)
    if ((retained && social.retention !== 'participant-retained') || (!retained && social.retention !== 'household-record-only')) issues.push(issue(social.id, 'social-memory.invalid-retention'))
  }
  try {
    if (!contentSafetyAuditMatches(socialContent({ records: typedRecords }), value.contentSafetyAudit)) issues.push(issue('social-memory:content-safety', 'social-memory.invalid-content-audit'))
  } catch { issues.push(issue('social-memory:content-safety', 'social-memory.invalid-content-audit')) }
  return canonicalIssues(issues)
}

/** Empty genesis contains no mutable social assertion. */
export const createSocialMemoryState = (): SocialMemoryState => {
  const stateWithoutAudit = { version: SOCIAL_MEMORY_STATE_VERSION, records: [] as const } satisfies Omit<SocialMemoryState, 'contentSafetyAudit'>
  return { ...stateWithoutAudit, contentSafetyAudit: audit(stateWithoutAudit) }
}

const personalLinkFor = (personId: string, task: DelegatedTaskRecord, social: SocialMemoryRecord): Extract<PersistentPersonMemory, { kind: 'social-memory' }> => ({
  id: `social-memory-link:${hashSeed(`jomon-social-memory-link|${social.id}|${personId}`).toString(36)}`,
  kind: 'social-memory',
  socialMemoryId: social.id,
  relation: personId === task.courierId ? 'courier' : 'recipient',
  atWorldTime: social.occurredAtWorldTime,
  contentSafety: structuredClone(social.contentSafety)
})

const retainPersonalMemories = (person: PersistentPersonRecord): PersistentPersonRecord => {
  const immutableOrOther = person.memories.filter(memory => memory.kind !== 'social-memory')
  const social = person.memories.filter(memory => memory.kind === 'social-memory')
    .sort((left, right) => right.atWorldTime - left.atWorldTime || compare(right.id, left.id))
    .slice(0, Math.min(SOCIAL_MEMORY_LIMITS.retainedPersonalLinks, Math.max(0, PERSISTENT_PERSON_LIMITS.memoriesPerPerson - immutableOrOther.length)))
  return { ...person, memories: [...immutableOrOther, ...social].sort((left, right) => compare(left.id, right.id)) }
}

const syncRetention = (records: readonly SocialMemoryRecord[], people: readonly PersistentPersonRecord[]): readonly SocialMemoryRecord[] => {
  const retained = new Set(socialMemoryLinksFor(people).map(link => link.memory.socialMemoryId))
  return sortedById(records.map(item => ({ ...item, retention: retained.has(item.id) ? 'participant-retained' as const : 'household-record-only' as const })))
}

/**
 * Derives all task-linked social records from one delegation transition. It
 * runs after person work changes, never appends a new player command, and is
 * intentionally the only writer of mutable social-memory links.
 */
export const reconcileDelegationSocialMemory = (
  previousContext: SocialMemoryContext,
  nextContext: SocialMemoryContext,
  state: SocialMemoryState,
  people: readonly PersistentPersonRecord[]
): SocialMemoryTransition => {
  const before = validateSocialMemoryState(previousContext, state)
  if (before.length) throw new SocialMemoryContractError(before)
  const existing = new Map(state.records.map(item => [item.id, item]))
  const expected = expectedRecords(nextContext)
  const additions = expected.filter(item => !existing.has(item.id))
  if (state.records.length + additions.length > SOCIAL_MEMORY_LIMITS.records) throw new SocialMemoryContractError([issue('social-memory:records', 'social-memory.record-limit')])
  const taskById = new Map(nextContext.tasks.map(task => [task.id, task]))
  let nextPeople = people.map(person => structuredClone(person))
  for (const social of additions) {
    const task = taskById.get(social.source.taskId)!
    nextPeople = nextPeople.map(person => {
      if (person.id !== task.courierId && person.id !== task.recipientId) return person
      const link = personalLinkFor(person.id, task, social)
      return person.memories.some(memory => memory.id === link.id) ? person : { ...person, memories: [...person.memories, link] }
    })
  }
  nextPeople = nextPeople.map(retainPersonalMemories).sort((left, right) => compare(left.id, right.id))
  const records = syncRetention([...state.records, ...additions], nextPeople)
  const stateWithoutAudit = { version: SOCIAL_MEMORY_STATE_VERSION, records } satisfies Omit<SocialMemoryState, 'contentSafetyAudit'>
  const next = { ...stateWithoutAudit, contentSafetyAudit: audit(stateWithoutAudit) }
  const diagnostics = validateSocialMemoryState({ ...nextContext, people: nextPeople }, next)
  if (diagnostics.length) throw new SocialMemoryContractError(diagnostics)
  return { state: next, people: nextPeople, createdRecordIds: additions.map(item => item.id) }
}

/** Relevant retained shared work is a modest context signal, never authority. */
export const socialMemoryRecallForPair = (state: SocialMemoryState, firstPersonId: string, secondPersonId: string): SocialMemoryRecall => {
  const matches = state.records.filter(item => item.retention === 'participant-retained' && item.participantPersonIds.includes(firstPersonId) && item.participantPersonIds.includes(secondPersonId))
    .sort((left, right) => right.occurredAtWorldTime - left.occurredAtWorldTime || compare(right.id, left.id))
  const selected = matches[0]
  if (!selected) return { band: 'none' }
  return {
    band: selected.disposition === 'cooperative' ? 'supportive' : 'unsettled',
    recordId: selected.id,
    occurredAtWorldTime: selected.occurredAtWorldTime
  }
}

/** Only the sidebar exists today; all other routes remain typed future contracts. */
export const socialMemoryEvidenceRoutesFor = (value: SocialMemoryRecord): readonly SocialMemoryEvidenceRoute[] => SOCIAL_MEMORY_EVIDENCE_SURFACES.map(surface => ({
  surface,
  availability: surface === 'management-sidebar' ? 'rendered-now' : 'future-route-only',
  sourceRecordId: value.id,
  knownAtWorldTime: value.knownAtWorldTime
}))

export const socialMemoryContentRecords = (state: Pick<SocialMemoryState, 'records'>): readonly ClassifiedMedievalContent[] => socialContent(state)
