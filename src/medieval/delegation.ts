import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { ConversationApproach, ConversationAssessment, ConversationComplexityBand, ConversationProposal, ConversationProposalKind, ConversationUrgencyBand } from './conversation'
import type { PersistentPersonMaterialInterest, PersistentPersonRecord, PersistentPersonSkillKind } from './persistent-person'
import { SeededRng, hashSeed } from './rng'

/**
 * Delegation describes work agreements and their person-history evidence only.
 * Its v1 outcomes deliberately do not alter Jomon, cargo, markets, routes,
 * health, people beyond work reservations, or any later gameplay domain.
 */
export const DELEGATION_CONTRACT_VERSION = 1 as const
export const DELEGATION_STATE_VERSION = 1 as const

export const DELEGATION_LIMITS = {
  tasks: 24,
  activeTasks: 6,
  terminalTasks: 18,
  identityLength: 48,
  agreementRollExclusive: 6,
  taskMemoriesPerPerson: 11
} as const

export const DELEGATION_TASK_FAMILIES = [
  'maintenance',
  'rigging',
  'cooking',
  'treatment',
  'cargo-handling',
  'trade-research',
  'barter',
  'bookkeeping',
  'scouting',
  'charting',
  'gathering',
  'hunting',
  'guiding',
  'watch-duty',
  'guarding',
  'rescue',
  'evacuation',
  'recruitment',
  'correspondence',
  'witness-work',
  'negotiation'
] as const
export type DelegationTaskFamily = typeof DELEGATION_TASK_FAMILIES[number]

export type DelegationTaskRiskBand = 'low' | 'guarded' | 'high'
export type DelegationProgressBasis = 'elapsed-in-world-minutes'
export type DelegationFutureDomainHook = 'jomon-integrity' | 'cargo' | 'markets' | 'route-knowledge' | 'health' | 'people' | 'frontier' | 'institutions'
export type DelegatedTaskStatus = 'refused' | 'in-progress' | 'completed' | 'interrupted'
export type DelegationInterruptionReason = 'courier-recall' | 'courier-cancellation'
export type DelegationAgreementStatus = 'accepted' | 'refused'
export type DelegationCompletionBand = 'steady' | 'strained'

export interface DelegationTaskDefinition {
  family: DelegationTaskFamily
  relevantMaterialInterests: readonly PersistentPersonMaterialInterest[]
  relevantSkills: readonly PersistentPersonSkillKind[]
  allowedComplexities: readonly ConversationComplexityBand[]
  allowedUrgencies: readonly ConversationUrgencyBand[]
  durationMinutes: Readonly<Record<ConversationComplexityBand, number>>
  progressBasis: DelegationProgressBasis
  riskBand: DelegationTaskRiskBand
  futureDomainHooks: readonly DelegationFutureDomainHook[]
}

const definition = (
  family: DelegationTaskFamily,
  relevantMaterialInterests: readonly PersistentPersonMaterialInterest[],
  relevantSkills: readonly PersistentPersonSkillKind[],
  routineMinutes: number,
  involvedMinutes: number,
  riskBand: DelegationTaskRiskBand,
  futureDomainHooks: readonly DelegationFutureDomainHook[]
): DelegationTaskDefinition => ({
  family,
  relevantMaterialInterests: [...relevantMaterialInterests].sort(),
  relevantSkills: [...relevantSkills].sort(),
  allowedComplexities: ['routine', 'involved'],
  allowedUrgencies: ['routine', 'pressing'],
  durationMinutes: { routine: routineMinutes, involved: involvedMinutes },
  progressBasis: 'elapsed-in-world-minutes',
  riskBand,
  futureDomainHooks: [...futureDomainHooks].sort()
})

/** The data boundary for all approved v1 work families; hooks have no effects yet. */
export const DELEGATION_TASK_DEFINITIONS: readonly DelegationTaskDefinition[] = [
  definition('barter', ['goods-care', 'trade-ledgers'], ['commerce'], 20, 45, 'guarded', ['markets']),
  definition('bookkeeping', ['records', 'trade-ledgers'], ['commerce', 'record-keeping'], 20, 40, 'low', ['markets']),
  definition('cargo-handling', ['goods-care'], ['hauling'], 20, 40, 'guarded', ['cargo']),
  definition('charting', ['records', 'waterway-knowledge'], ['navigation', 'record-keeping'], 30, 60, 'guarded', ['route-knowledge']),
  definition('cooking', ['household-upkeep', 'provisions'], ['care', 'provisioning'], 15, 30, 'low', ['people']),
  definition('correspondence', ['records', 'trade-ledgers'], ['commerce', 'record-keeping'], 20, 45, 'low', ['institutions']),
  definition('evacuation', ['household-security', 'provisions'], ['command', 'hauling'], 30, 60, 'high', ['people']),
  definition('gathering', ['provisions'], ['fishing', 'hauling'], 30, 60, 'guarded', ['frontier']),
  definition('guarding', ['household-security'], ['guarding'], 30, 60, 'high', ['people']),
  definition('guiding', ['route-safety', 'waterway-knowledge'], ['navigation'], 30, 60, 'high', ['route-knowledge']),
  definition('hunting', ['provisions'], ['fishing', 'guarding'], 30, 60, 'high', ['frontier']),
  definition('maintenance', ['craft-work', 'vessel-upkeep'], ['craft'], 30, 60, 'guarded', ['jomon-integrity']),
  definition('negotiation', ['records', 'trade-ledgers'], ['commerce', 'command'], 20, 45, 'guarded', ['institutions', 'markets']),
  definition('recruitment', ['household-upkeep', 'records'], ['command', 'commerce'], 30, 60, 'guarded', ['people']),
  definition('rescue', ['care-work', 'household-security'], ['care', 'guarding'], 30, 60, 'high', ['people']),
  definition('rigging', ['route-safety', 'vessel-upkeep'], ['craft', 'navigation'], 20, 50, 'guarded', ['jomon-integrity']),
  definition('scouting', ['route-safety', 'waterway-knowledge'], ['guarding', 'navigation'], 30, 60, 'high', ['frontier', 'route-knowledge']),
  definition('trade-research', ['records', 'trade-ledgers'], ['commerce', 'record-keeping'], 30, 60, 'low', ['markets']),
  definition('treatment', ['care-work'], ['care'], 20, 40, 'guarded', ['health']),
  definition('watch-duty', ['household-security', 'route-safety'], ['guarding', 'navigation'], 30, 60, 'guarded', ['people']),
  definition('witness-work', ['records', 'route-safety'], ['command', 'record-keeping'], 20, 45, 'guarded', ['institutions'])
]

export interface DelegationOfferInput {
  version: typeof DELEGATION_CONTRACT_VERSION
  id: string
  courierId: string
  recipientId: string
  family: DelegationTaskFamily
  approach: ConversationApproach
  proposal: ConversationProposal
}

export interface DelegationInterruptionInput {
  version: typeof DELEGATION_CONTRACT_VERSION
  taskId: string
  courierId: string
  reason: DelegationInterruptionReason
}

export interface DelegationAgreementEvidence {
  status: DelegationAgreementStatus
  roll: number
  threshold: number
  token: number
  factors: readonly DelegationFactorCode[]
}

export type DelegationTaskOutcome =
  | { kind: 'refused'; atWorldTime: number; token: number; factors: readonly DelegationFactorCode[] }
  | { kind: 'completed'; atWorldTime: number; completionBand: DelegationCompletionBand; roll: number; token: number; factors: readonly DelegationFactorCode[] }
  | { kind: 'interrupted'; atWorldTime: number; reason: DelegationInterruptionReason; token: number; factors: readonly DelegationFactorCode[] }

export interface DelegatedTaskRecord {
  version: typeof DELEGATION_CONTRACT_VERSION
  id: string
  offerId: string
  family: DelegationTaskFamily
  courierId: string
  recipientId: string
  approach: ConversationApproach
  proposal: ConversationProposal
  assessment: ConversationAssessment
  status: DelegatedTaskStatus
  offeredAtWorldTime: number
  acceptedAtWorldTime?: number
  plannedCompletionAtWorldTime?: number
  progressMinutes: number
  agreement: DelegationAgreementEvidence
  outcome: DelegationTaskOutcome | null
  contentSafety: MedievalContentSafetyClassification
}

export interface DelegationState {
  version: typeof DELEGATION_STATE_VERSION
  tasks: readonly DelegatedTaskRecord[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

export interface DelegationContext {
  worldId: string
  creationDigest: string
  worldTime: number
  people: readonly PersistentPersonRecord[]
}

export interface DelegationOfferTransition {
  state: DelegationState
  people: readonly PersistentPersonRecord[]
  task: DelegatedTaskRecord
}

export interface DelegationProgressTransition {
  state: DelegationState
  people: readonly PersistentPersonRecord[]
  resolvedTaskIds: readonly string[]
}

export type DelegationFactorCode =
  | 'conversation-eligible'
  | 'approach-unlocked'
  | 'family-proposal-aligned'
  | 'recipient-interest-aligned'
  | 'recipient-interest-misaligned'
  | 'recipient-skill-relevant'
  | 'recipient-skill-unsuitable'
  | 'recipient-capacity-ready'
  | 'recipient-capacity-limited'
  | 'recipient-rapport-strained'
  | 'recipient-rapport-unformed'
  | 'recipient-rapport-steady'
  | 'recipient-rapport-strong'
  | 'recipient-needs-settled'
  | 'recipient-needs-pressured'
  | 'recipient-health-steady'
  | 'recipient-health-recovering'
  | 'task-risk-low'
  | 'task-risk-guarded'
  | 'task-risk-high'
  | 'task-complexity-routine'
  | 'task-complexity-involved'
  | 'task-urgency-routine'
  | 'task-urgency-pressing'
  | 'agreement-accepted'
  | 'agreement-refused'
  | 'outcome-completed-steady'
  | 'outcome-completed-strained'
  | 'outcome-interrupted-courier-recall'
  | 'outcome-interrupted-courier-cancellation'
  | `agreement-roll:${0 | 1 | 2 | 3 | 4 | 5}`
  | `agreement-threshold:${0 | 1 | 2 | 3 | 4 | 5}`
  | `completion-roll:${0 | 1 | 2 | 3 | 4 | 5}`

/** Runtime vocabulary ensures stored explanations remain closed and prose-free. */
export const DELEGATION_FACTOR_CODES: readonly DelegationFactorCode[] = [
  'conversation-eligible',
  'approach-unlocked',
  'family-proposal-aligned',
  'recipient-interest-aligned',
  'recipient-interest-misaligned',
  'recipient-skill-relevant',
  'recipient-skill-unsuitable',
  'recipient-capacity-ready',
  'recipient-capacity-limited',
  'recipient-rapport-strained',
  'recipient-rapport-unformed',
  'recipient-rapport-steady',
  'recipient-rapport-strong',
  'recipient-needs-settled',
  'recipient-needs-pressured',
  'recipient-health-steady',
  'recipient-health-recovering',
  'task-risk-low',
  'task-risk-guarded',
  'task-risk-high',
  'task-complexity-routine',
  'task-complexity-involved',
  'task-urgency-routine',
  'task-urgency-pressing',
  'agreement-accepted',
  'agreement-refused',
  'outcome-completed-steady',
  'outcome-completed-strained',
  'outcome-interrupted-courier-recall',
  'outcome-interrupted-courier-cancellation',
  ...([0, 1, 2, 3, 4, 5] as const).flatMap(value => [
    `agreement-roll:${value}` as DelegationFactorCode,
    `agreement-threshold:${value}` as DelegationFactorCode,
    `completion-roll:${value}` as DelegationFactorCode
  ])
]

export type DelegationDiagnosticCode =
  | 'delegation.malformed-state'
  | 'delegation.invalid-version'
  | 'delegation.invalid-context'
  | 'delegation.invalid-definition'
  | 'delegation.invalid-offer'
  | 'delegation.invalid-offer-id'
  | 'delegation.invalid-task-id'
  | 'delegation.duplicate-task-id'
  | 'delegation.task-limit'
  | 'delegation.active-task-limit'
  | 'delegation.terminal-task-limit'
  | 'delegation.invalid-reference'
  | 'delegation.invalid-assessment'
  | 'delegation.conversation-blocked'
  | 'delegation.approach-locked'
  | 'delegation.invalid-family-context'
  | 'delegation.invalid-lifecycle'
  | 'delegation.invalid-agreement'
  | 'delegation.invalid-outcome'
  | 'delegation.invalid-work-link'
  | 'delegation.invalid-memory'
  | 'delegation.invalid-content-audit'
  | 'delegation.malformed-interruption'
  | 'delegation.invalid-interruption'
  | 'delegation.task-not-active'
  | 'delegation.task-due-before-interruption'
  | 'delegation.duplicate-interruption'
  | MedievalContentSafetyDiagnosticCode

export interface DelegationDiagnostic {
  code: DelegationDiagnosticCode
  recordId: string
}

export class DelegationContractError extends Error {
  constructor(readonly diagnostics: readonly DelegationDiagnostic[]) {
    super(`delegation rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'DelegationContractError'
  }
}

const interests: readonly PersistentPersonMaterialInterest[] = ['waterway-knowledge', 'route-safety', 'trade-ledgers', 'goods-care', 'craft-work', 'vessel-upkeep', 'household-upkeep', 'household-security', 'provisions', 'care-work', 'records']
const skills: readonly PersistentPersonSkillKind[] = ['navigation', 'commerce', 'craft', 'care', 'record-keeping', 'guarding', 'provisioning', 'hauling', 'fishing', 'performance', 'command']
const approaches: readonly ConversationApproach[] = ['direct-request', 'shared-context', 'terms-outline', 'contingency-check', 'reciprocal-options']
const proposalKinds: readonly ConversationProposalKind[] = ['request', 'coordination', 'terms']
const urgencies: readonly ConversationUrgencyBand[] = ['routine', 'pressing']
const complexities: readonly ConversationComplexityBand[] = ['routine', 'involved']
const hooks: readonly DelegationFutureDomainHook[] = ['jomon-integrity', 'cargo', 'markets', 'route-knowledge', 'health', 'people', 'frontier', 'institutions']
const risks: readonly DelegationTaskRiskBand[] = ['low', 'guarded', 'high']
const interruptionReasons: readonly DelegationInterruptionReason[] = ['courier-recall', 'courier-cancellation']
const statuses: readonly DelegatedTaskStatus[] = ['refused', 'in-progress', 'completed', 'interrupted']

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const validId = (value: unknown): value is string => typeof value === 'string' && value.length > 0 && value.length <= DELEGATION_LIMITS.identityLength && /^[a-z][a-z0-9:._-]*$/i.test(value)
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const canonical = <Value extends string>(values: readonly Value[]): readonly Value[] => [...new Set(values)].sort(compare)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const issue = (recordId: string, code: DelegationDiagnosticCode): DelegationDiagnostic => ({ recordId, code })
const canonicalIssues = (issues: readonly DelegationDiagnostic[]): readonly DelegationDiagnostic[] => [...new Map(issues.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)

export const delegationTaskIdForOffer = (offerId: string): string => `delegated-task:${offerId}`
export const delegationCommitmentIdForTask = (taskId: string): string => `delegated-task-commitment:${taskId}`
export const delegationOfferActionId = (offerId: string): string => `delegation-offer:${offerId}`
export const delegationInterruptionActionId = (taskId: string, reason: DelegationInterruptionReason): string => `delegation-interruption:${taskId}:${reason}`

const taskClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const taskMemoryClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('history', ['adult-labour', 'civil-life'], 'adults-only', ['player-facing-text'])
const taskMemoryDetail = (taskId: string, phase: 'agreement' | 'refusal' | 'completion' | 'interruption'): string => `Delegated task ${taskId} ${phase}.`
const taskMemoryId = (taskId: string, personId: string, phase: 'agreement' | 'refusal' | 'completion' | 'interruption'): string => `task-memory:${taskId}:${personId}:${phase}`
const taskToken = (context: Pick<DelegationContext, 'worldId' | 'creationDigest'>, scope: string, taskId: string, boundary: number): number => hashSeed(`jomon-delegation:${DELEGATION_CONTRACT_VERSION}:${context.worldId}:${context.creationDigest}:${scope}:${taskId}:${boundary}`)
const taskRoll = (context: Pick<DelegationContext, 'worldId' | 'creationDigest'>, scope: string, taskId: string, boundary: number): number => new SeededRng(`jomon-delegation:${DELEGATION_CONTRACT_VERSION}:${context.worldId}:${context.creationDigest}:${scope}:${taskId}:${boundary}`).integer(DELEGATION_LIMITS.agreementRollExclusive)
const validFactors = (value: unknown): value is readonly DelegationFactorCode[] => Array.isArray(value)
  && value.every(factor => oneOf(DELEGATION_FACTOR_CODES, factor))
  && same(value, canonical(value as DelegationFactorCode[]))

export const delegationDefinitionFor = (family: DelegationTaskFamily): DelegationTaskDefinition => {
  const definition = DELEGATION_TASK_DEFINITIONS.find(candidate => candidate.family === family)
  if (!definition) throw new DelegationContractError([issue('delegation:family', 'delegation.invalid-definition')])
  return structuredClone(definition)
}

/** Validates the declarative v1 family table before a task may use it. */
export const validateDelegationTaskDefinitions = (value: unknown = DELEGATION_TASK_DEFINITIONS): readonly DelegationDiagnostic[] => {
  if (!Array.isArray(value) || value.length !== DELEGATION_TASK_FAMILIES.length) return [issue('delegation:definitions', 'delegation.invalid-definition')]
  const diagnostics: DelegationDiagnostic[] = []
  const seen = new Set<string>()
  for (const candidate of value) {
    const id = record(candidate) && typeof candidate.family === 'string' ? candidate.family : 'delegation:definition'
    if (!record(candidate) || !hasOnlyKeys(candidate, ['family', 'relevantMaterialInterests', 'relevantSkills', 'allowedComplexities', 'allowedUrgencies', 'durationMinutes', 'progressBasis', 'riskBand', 'futureDomainHooks']) || !oneOf(DELEGATION_TASK_FAMILIES, candidate.family) || seen.has(candidate.family) || !Array.isArray(candidate.relevantMaterialInterests) || !candidate.relevantMaterialInterests.length || !candidate.relevantMaterialInterests.every(item => oneOf(interests, item)) || !same(candidate.relevantMaterialInterests, canonical(candidate.relevantMaterialInterests)) || !Array.isArray(candidate.relevantSkills) || !candidate.relevantSkills.length || !candidate.relevantSkills.every(item => oneOf(skills, item)) || !same(candidate.relevantSkills, canonical(candidate.relevantSkills)) || !Array.isArray(candidate.allowedComplexities) || !same(candidate.allowedComplexities, complexities) || !Array.isArray(candidate.allowedUrgencies) || !same(candidate.allowedUrgencies, urgencies) || !record(candidate.durationMinutes) || !hasOnlyKeys(candidate.durationMinutes, ['routine', 'involved']) || !safeInteger(candidate.durationMinutes.routine) || candidate.durationMinutes.routine < 5 || candidate.durationMinutes.routine > 1_440 || !safeInteger(candidate.durationMinutes.involved) || candidate.durationMinutes.involved < candidate.durationMinutes.routine || candidate.durationMinutes.involved > 1_440 || candidate.progressBasis !== 'elapsed-in-world-minutes' || !oneOf(risks, candidate.riskBand) || !Array.isArray(candidate.futureDomainHooks) || !candidate.futureDomainHooks.every(item => oneOf(hooks, item)) || !same(candidate.futureDomainHooks, canonical(candidate.futureDomainHooks))) diagnostics.push(issue(id, 'delegation.invalid-definition'))
    if (record(candidate) && typeof candidate.family === 'string') seen.add(candidate.family)
  }
  if (DELEGATION_TASK_FAMILIES.some(family => !seen.has(family))) diagnostics.push(issue('delegation:definitions', 'delegation.invalid-definition'))
  return canonicalIssues(diagnostics)
}

const validProposal = (value: unknown): value is ConversationProposal => record(value)
  && hasOnlyKeys(value, ['version', 'kind', 'urgency', 'complexity', 'materialInterest', 'contentSafety'])
  && value.version === 1
  && oneOf(proposalKinds, value.kind)
  && oneOf(urgencies, value.urgency)
  && oneOf(complexities, value.complexity)
  && oneOf(interests, value.materialInterest)
  && auditMedievalContentSafety([{ id: 'delegation:proposal', domain: 'contract', classification: value.contentSafety }]).status === 'accepted'

export const isDelegationOfferInput = (value: unknown): value is DelegationOfferInput => record(value)
  && hasOnlyKeys(value, ['version', 'id', 'courierId', 'recipientId', 'family', 'approach', 'proposal'])
  && value.version === DELEGATION_CONTRACT_VERSION
  && validId(value.id)
  && validId(value.courierId)
  && validId(value.recipientId)
  && value.courierId !== value.recipientId
  && oneOf(DELEGATION_TASK_FAMILIES, value.family)
  && oneOf(approaches, value.approach)
  && validProposal(value.proposal)

export const isDelegationInterruptionInput = (value: unknown): value is DelegationInterruptionInput => record(value)
  && hasOnlyKeys(value, ['version', 'taskId', 'courierId', 'reason'])
  && value.version === DELEGATION_CONTRACT_VERSION
  && validId(value.taskId)
  && validId(value.courierId)
  && oneOf(interruptionReasons, value.reason)

export const delegationOfferTemporalAction = (offer: DelegationOfferInput) => ({
  id: delegationOfferActionId(offer.id),
  kind: 'delegated-task-commitment' as const,
  durationMinutes: 1,
  contentSafety: taskClassification()
})

export const delegationInterruptionTemporalAction = (interruption: DelegationInterruptionInput) => ({
  id: delegationInterruptionActionId(interruption.taskId, interruption.reason),
  kind: 'delegated-task-resolution' as const,
  durationMinutes: 1,
  contentSafety: taskClassification()
})

const validContext = (value: DelegationContext): boolean => validId(value.worldId)
  && typeof value.creationDigest === 'string'
  && value.creationDigest.length > 0
  && value.creationDigest.length <= DELEGATION_LIMITS.identityLength
  && safeInteger(value.worldTime)
  && Array.isArray(value.people)

const assessmentMatchesOffer = (assessment: ConversationAssessment, offer: DelegationOfferInput): boolean => assessment.version === 1
  && assessment.courierId === offer.courierId
  && assessment.recipientId === offer.recipientId
  && Array.isArray(assessment.unlockedApproaches)
  && Array.isArray(assessment.factors)
  && auditMedievalContentSafety([{ id: 'delegation:assessment', domain: 'contract', classification: assessment.contentSafety }]).status === 'accepted'

const recipientSkill = (recipient: PersistentPersonRecord, definition: DelegationTaskDefinition): boolean => recipient.work.skills.some(skill => definition.relevantSkills.includes(skill.kind) && skill.level >= 1)
const rapportFactor = (assessment: ConversationAssessment): DelegationFactorCode => `recipient-rapport:${assessment.recipientRapport === 'not-assessable' ? 'unformed' : assessment.recipientRapport}` as DelegationFactorCode
const capacityFactor = (assessment: ConversationAssessment): DelegationFactorCode => assessment.recipientCapacity === 'ready' ? 'recipient-capacity-ready' : 'recipient-capacity-limited'
const needsFactor = (assessment: ConversationAssessment): DelegationFactorCode => assessment.recipientNeedsPressure === 'settled' ? 'recipient-needs-settled' : 'recipient-needs-pressured'
const healthFactor = (assessment: ConversationAssessment): DelegationFactorCode => assessment.recipientHealth === 'recovering' ? 'recipient-health-recovering' : 'recipient-health-steady'
const riskFactor = (risk: DelegationTaskRiskBand): DelegationFactorCode => `task-risk:${risk}` as DelegationFactorCode
const urgencyFactor = (urgency: ConversationUrgencyBand): DelegationFactorCode => `task-urgency:${urgency}` as DelegationFactorCode
const complexityFactor = (complexity: ConversationComplexityBand): DelegationFactorCode => `task-complexity:${complexity}` as DelegationFactorCode
const rollFactor = (roll: number): DelegationFactorCode => `agreement-roll:${roll as 0 | 1 | 2 | 3 | 4 | 5}`
const thresholdFactor = (threshold: number): DelegationFactorCode => `agreement-threshold:${threshold as 0 | 1 | 2 | 3 | 4 | 5}`
const completionRollFactor = (roll: number): DelegationFactorCode => `completion-roll:${roll as 0 | 1 | 2 | 3 | 4 | 5}`

const agreementThresholdFor = (assessment: ConversationAssessment, interestMatches: boolean, skillMatches: boolean, definition: DelegationTaskDefinition): number => {
  let score = assessment.agreementReadiness === 'open' ? 3 : assessment.agreementReadiness === 'conditional' ? 2 : 0
  score += assessment.recipientRapport === 'strong' ? 2 : assessment.recipientRapport === 'steady' ? 1 : assessment.recipientRapport === 'strained' ? -2 : 0
  score += interestMatches ? 1 : -2
  score += skillMatches ? 1 : -2
  score += assessment.recipientCapacity === 'ready' ? 1 : 0
  score -= definition.riskBand === 'high' ? 1 : 0
  score -= assessment.recipientNeedsPressure === 'pressured' ? 1 : 0
  score -= assessment.recipientHealth === 'recovering' ? 1 : 0
  score -= assessment.recipientSafetyPressure === 'elevated' ? 1 : 0
  score -= assessment.taskClarity === 'limited' || assessment.taskClarity === 'basic' ? 1 : 0
  return Math.max(0, Math.min(DELEGATION_LIMITS.agreementRollExclusive - 1, score))
}

const agreementFor = (context: DelegationContext, offer: DelegationOfferInput, assessment: ConversationAssessment, recipient: PersistentPersonRecord, definition: DelegationTaskDefinition): DelegationAgreementEvidence => {
  const taskId = delegationTaskIdForOffer(offer.id)
  const interestMatches = recipient.materialInterests.some(interest => definition.relevantMaterialInterests.includes(interest))
  const skillMatches = recipientSkill(recipient, definition)
  const roll = taskRoll(context, 'agreement', taskId, context.worldTime)
  const threshold = agreementThresholdFor(assessment, interestMatches, skillMatches, definition)
  const accepted = interestMatches && skillMatches && roll <= threshold
  const factors = canonical([
    'conversation-eligible',
    'approach-unlocked',
    'family-proposal-aligned',
    interestMatches ? 'recipient-interest-aligned' : 'recipient-interest-misaligned',
    skillMatches ? 'recipient-skill-relevant' : 'recipient-skill-unsuitable',
    capacityFactor(assessment),
    rapportFactor(assessment),
    needsFactor(assessment),
    healthFactor(assessment),
    riskFactor(definition.riskBand),
    urgencyFactor(offer.proposal.urgency),
    complexityFactor(offer.proposal.complexity),
    rollFactor(roll),
    thresholdFactor(threshold),
    accepted ? 'agreement-accepted' : 'agreement-refused'
  ] as DelegationFactorCode[])
  return { status: accepted ? 'accepted' : 'refused', roll, threshold, token: taskToken(context, 'agreement', taskId, context.worldTime), factors }
}

const appendMemory = (person: PersistentPersonRecord, task: DelegatedTaskRecord, phase: 'agreement' | 'refusal' | 'completion' | 'interruption', atWorldTime: number): PersistentPersonRecord => {
  const memory = {
    id: taskMemoryId(task.id, person.id, phase),
    kind: 'task-evidence' as const,
    atWorldTime,
    detail: taskMemoryDetail(task.id, phase),
    contentSafety: taskMemoryClassification()
  }
  const memories = person.memories.some(existing => existing.id === memory.id) ? person.memories : [...person.memories, memory]
  const foundation = memories.filter(item => item.kind === 'foundation-history')
  const retained = memories
    .filter(item => item.kind !== 'foundation-history')
    .sort((left, right) => right.atWorldTime - left.atWorldTime || compare(right.id, left.id))
    .slice(0, Math.max(0, DELEGATION_LIMITS.taskMemoriesPerPerson))
  return { ...person, memories: [...foundation, ...retained].sort((left, right) => compare(left.id, right.id)) }
}

const appendTaskMemories = (people: readonly PersistentPersonRecord[], task: DelegatedTaskRecord, phase: 'agreement' | 'refusal' | 'completion' | 'interruption', atWorldTime: number): readonly PersistentPersonRecord[] => people.map(person => person.id === task.courierId || person.id === task.recipientId ? appendMemory(person, task, phase, atWorldTime) : structuredClone(person)).sort((left, right) => compare(left.id, right.id))

const reserveWorker = (people: readonly PersistentPersonRecord[], task: DelegatedTaskRecord): readonly PersistentPersonRecord[] => people.map(person => {
  if (person.id !== task.recipientId) return structuredClone(person)
  const commitmentId = delegationCommitmentIdForTask(task.id)
  return {
    ...person,
    work: {
      ...person.work,
      capacity: { ...person.work.capacity, current: person.work.capacity.current - 1 },
      current: { status: 'committed' as const, commitmentId, startedAtWorldTime: task.acceptedAtWorldTime! },
      availability: 'committed' as const
    },
    commitments: [...person.commitments, {
      id: commitmentId,
      kind: 'delegated-task' as const,
      status: 'active' as const,
      createdAtWorldTime: task.acceptedAtWorldTime!,
      detail: `Delegated task ${task.id} commitment.`,
      contentSafety: taskClassification()
    }].sort((left, right) => compare(left.id, right.id))
  }
}).sort((left, right) => compare(left.id, right.id))

const releaseWorker = (people: readonly PersistentPersonRecord[], task: DelegatedTaskRecord, status: 'resolved' | 'cancelled', atWorldTime: number): readonly PersistentPersonRecord[] => people.map(person => {
  if (person.id !== task.recipientId) return structuredClone(person)
  const commitmentId = delegationCommitmentIdForTask(task.id)
  return {
    ...person,
    work: {
      ...person.work,
      capacity: { ...person.work.capacity, current: Math.min(person.work.capacity.maximum, person.work.capacity.current + 1) },
      current: { status: 'idle' as const },
      availability: 'available' as const
    },
    commitments: person.commitments.map(commitment => commitment.id === commitmentId
      ? { ...commitment, status, resolvedAtWorldTime: atWorldTime }
      : structuredClone(commitment)).sort((left, right) => compare(left.id, right.id))
  }
}).sort((left, right) => compare(left.id, right.id))

const taskContentRecords = (state: Pick<DelegationState, 'tasks'>): readonly ClassifiedMedievalContent[] => state.tasks.flatMap(task => [
  { id: `delegation:${task.id}`, domain: 'contract' as const, classification: task.contentSafety },
  { id: `delegation:${task.id}:proposal`, domain: 'contract' as const, classification: task.proposal.contentSafety },
  { id: `delegation:${task.id}:assessment`, domain: 'contract' as const, classification: task.assessment.contentSafety }
])

export const delegationContentRecords = taskContentRecords

const audit = (state: Pick<DelegationState, 'tasks'>): MedievalContentSafetyAudit => {
  const result = auditMedievalContentSafety(taskContentRecords(state))
  if (result.status === 'rejected') throw new DelegationContractError(result.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  return result
}

export const createDelegationState = (): DelegationState => {
  const state = { version: DELEGATION_STATE_VERSION, tasks: [] } satisfies Omit<DelegationState, 'contentSafetyAudit'>
  return { ...state, contentSafetyAudit: audit(state) }
}

const validAssessmentShape = (value: unknown): value is ConversationAssessment => record(value)
  && hasOnlyKeys(value, ['version', 'courierId', 'recipientId', 'eligibility', 'barriers', 'unlockedApproaches', 'taskClarity', 'recipientRapport', 'recipientInterestAlignment', 'recipientCapacity', 'recipientCurrentWork', 'recipientCommitment', 'recipientNeedsPressure', 'recipientHealth', 'recipientSafetyPressure', 'risk', 'agreementReadiness', 'factors', 'contentSafety'])
  && value.version === 1
  && validId(value.courierId)
  && validId(value.recipientId)
  && (value.eligibility === 'eligible' || value.eligibility === 'blocked')
  && Array.isArray(value.barriers)
  && Array.isArray(value.unlockedApproaches)
  && Array.isArray(value.factors)
  && auditMedievalContentSafety([{ id: 'delegation:assessment', domain: 'contract', classification: value.contentSafety }]).status === 'accepted'

const validAgreement = (context: DelegationContext, task: DelegatedTaskRecord, value: unknown): value is DelegationAgreementEvidence => record(value)
  && hasOnlyKeys(value, ['status', 'roll', 'threshold', 'token', 'factors'])
  && (value.status === 'accepted' || value.status === 'refused')
  && safeInteger(value.roll) && value.roll < DELEGATION_LIMITS.agreementRollExclusive
  && safeInteger(value.threshold) && value.threshold < DELEGATION_LIMITS.agreementRollExclusive
  && safeInteger(value.token)
  && validFactors(value.factors)
  && value.token === taskToken(context, 'agreement', task.id, task.offeredAtWorldTime)

const validOutcome = (context: DelegationContext, task: DelegatedTaskRecord, value: unknown): value is DelegationTaskOutcome => {
  if (!record(value) || !safeInteger(value.atWorldTime) || !safeInteger(value.token) || !validFactors(value.factors)) return false
  if (value.kind === 'refused') return hasOnlyKeys(value, ['kind', 'atWorldTime', 'token', 'factors']) && value.atWorldTime === task.offeredAtWorldTime && value.token === taskToken(context, 'refusal', task.id, value.atWorldTime)
  if (value.kind === 'completed') return hasOnlyKeys(value, ['kind', 'atWorldTime', 'completionBand', 'roll', 'token', 'factors']) && value.atWorldTime === task.plannedCompletionAtWorldTime && (value.completionBand === 'steady' || value.completionBand === 'strained') && safeInteger(value.roll) && value.roll < DELEGATION_LIMITS.agreementRollExclusive && value.token === taskToken(context, 'completion', task.id, value.atWorldTime)
  return hasOnlyKeys(value, ['kind', 'atWorldTime', 'reason', 'token', 'factors']) && oneOf(interruptionReasons, value.reason) && value.atWorldTime >= (task.acceptedAtWorldTime ?? Number.MAX_SAFE_INTEGER) && value.atWorldTime < (task.plannedCompletionAtWorldTime ?? 0) && value.token === taskToken(context, `interruption:${value.reason}`, task.id, value.atWorldTime)
}

const validTaskShape = (value: unknown): value is DelegatedTaskRecord => record(value)
  && hasOnlyKeys(value, value.status === 'refused'
    ? ['version', 'id', 'offerId', 'family', 'courierId', 'recipientId', 'approach', 'proposal', 'assessment', 'status', 'offeredAtWorldTime', 'progressMinutes', 'agreement', 'outcome', 'contentSafety']
    : ['version', 'id', 'offerId', 'family', 'courierId', 'recipientId', 'approach', 'proposal', 'assessment', 'status', 'offeredAtWorldTime', 'acceptedAtWorldTime', 'plannedCompletionAtWorldTime', 'progressMinutes', 'agreement', 'outcome', 'contentSafety'])
  && value.version === DELEGATION_CONTRACT_VERSION
  && validId(value.id)
  && validId(value.offerId)
  && validId(value.courierId)
  && validId(value.recipientId)
  && value.courierId !== value.recipientId
  && oneOf(DELEGATION_TASK_FAMILIES, value.family)
  && oneOf(approaches, value.approach)
  && validProposal(value.proposal)
  && validAssessmentShape(value.assessment)
  && oneOf(statuses, value.status)
  && safeInteger(value.offeredAtWorldTime)
  && safeInteger(value.progressMinutes)
  && auditMedievalContentSafety([{ id: 'delegation:task', domain: 'contract', classification: value.contentSafety }]).status === 'accepted'

const validActiveWorkLink = (person: PersistentPersonRecord, task: DelegatedTaskRecord): boolean => person.life.status === 'living'
  && person.work.availability === 'committed'
  && person.work.current.status === 'committed'
  && person.work.current.commitmentId === delegationCommitmentIdForTask(task.id)
  && person.work.current.startedAtWorldTime === task.acceptedAtWorldTime
  && person.work.capacity.current > 0
  && person.work.capacity.current < person.work.capacity.maximum
  && person.commitments.some(commitment => commitment.id === delegationCommitmentIdForTask(task.id) && commitment.kind === 'delegated-task' && commitment.status === 'active' && commitment.createdAtWorldTime === task.acceptedAtWorldTime)

const validTerminalWorkLink = (person: PersistentPersonRecord, task: DelegatedTaskRecord, status: 'resolved' | 'cancelled'): boolean => (person.work.current.status !== 'committed' || person.work.current.commitmentId !== delegationCommitmentIdForTask(task.id))
  && person.commitments.some(commitment => commitment.id === delegationCommitmentIdForTask(task.id) && commitment.kind === 'delegated-task' && commitment.status === status && commitment.resolvedAtWorldTime === task.outcome?.atWorldTime)

/** Validates bounded task state and its live person-work links. */
export const validateDelegationState = (context: DelegationContext, value: unknown): readonly DelegationDiagnostic[] => {
  const definitionIssues = validateDelegationTaskDefinitions()
  if (!validContext(context)) return canonicalIssues([...definitionIssues, issue('delegation:context', 'delegation.invalid-context')])
  if (!record(value) || !hasOnlyKeys(value, ['version', 'tasks', 'contentSafetyAudit'])) return canonicalIssues([...definitionIssues, issue('delegation', 'delegation.malformed-state')])
  const issues: DelegationDiagnostic[] = [...definitionIssues]
  if (value.version !== DELEGATION_STATE_VERSION) issues.push(issue('delegation', 'delegation.invalid-version'))
  if (!Array.isArray(value.tasks)) return canonicalIssues([...issues, issue('delegation:tasks', 'delegation.malformed-state')])
  const tasks = value.tasks
  if (tasks.length > DELEGATION_LIMITS.tasks) issues.push(issue('delegation:tasks', 'delegation.task-limit'))
  const active = tasks.filter(task => record(task) && task.status === 'in-progress')
  const terminal = tasks.filter(task => record(task) && task.status !== 'in-progress')
  if (active.length > DELEGATION_LIMITS.activeTasks) issues.push(issue('delegation:tasks', 'delegation.active-task-limit'))
  if (terminal.length > DELEGATION_LIMITS.terminalTasks) issues.push(issue('delegation:tasks', 'delegation.terminal-task-limit'))
  const ids = new Set<string>()
  const recipients = new Set<string>()
  for (const candidate of tasks) {
    const id = record(candidate) && typeof candidate.id === 'string' ? candidate.id : 'delegation:task'
    if (!validTaskShape(candidate)) { issues.push(issue(id, 'delegation.invalid-lifecycle')); continue }
    const task = candidate
    if (ids.has(task.id)) issues.push(issue(task.id, 'delegation.duplicate-task-id'))
    ids.add(task.id)
    if (task.id !== delegationTaskIdForOffer(task.offerId)) issues.push(issue(task.id, 'delegation.invalid-task-id'))
    const definition = DELEGATION_TASK_DEFINITIONS.find(item => item.family === task.family)
    const courier = context.people.find(person => person.id === task.courierId)
    const recipient = context.people.find(person => person.id === task.recipientId)
    if (!definition || !courier || !recipient || !courier.identity.adult || !recipient.identity.adult) issues.push(issue(task.id, 'delegation.invalid-reference'))
    if (!assessmentMatchesOffer(task.assessment, { version: 1, id: task.offerId, courierId: task.courierId, recipientId: task.recipientId, family: task.family, approach: task.approach, proposal: task.proposal })) issues.push(issue(task.id, 'delegation.invalid-assessment'))
    if (!definition || !definition.relevantMaterialInterests.includes(task.proposal.materialInterest) || !definition.allowedComplexities.includes(task.proposal.complexity) || !definition.allowedUrgencies.includes(task.proposal.urgency)) issues.push(issue(task.id, 'delegation.invalid-family-context'))
    if (task.offeredAtWorldTime > context.worldTime || !validAgreement(context, task, task.agreement)) issues.push(issue(task.id, 'delegation.invalid-agreement'))
    else if (definition && recipient) {
      const expectedAgreement = agreementFor({ ...context, worldTime: task.offeredAtWorldTime }, {
        version: DELEGATION_CONTRACT_VERSION,
        id: task.offerId,
        courierId: task.courierId,
        recipientId: task.recipientId,
        family: task.family,
        approach: task.approach,
        proposal: task.proposal
      }, task.assessment, recipient, definition)
      if (!same(task.agreement, expectedAgreement)) issues.push(issue(task.id, 'delegation.invalid-agreement'))
    }
    const duration = definition?.durationMinutes[task.proposal.complexity]
    if (task.status === 'refused') {
      if (task.agreement.status !== 'refused' || task.acceptedAtWorldTime !== undefined || task.plannedCompletionAtWorldTime !== undefined || task.progressMinutes !== 0 || !validOutcome(context, task, task.outcome) || task.outcome?.kind !== 'refused') issues.push(issue(task.id, 'delegation.invalid-lifecycle'))
    } else {
      if (task.agreement.status !== 'accepted' || !safeInteger(task.acceptedAtWorldTime) || task.acceptedAtWorldTime !== task.offeredAtWorldTime || !safeInteger(task.plannedCompletionAtWorldTime) || task.plannedCompletionAtWorldTime !== task.acceptedAtWorldTime + duration || task.plannedCompletionAtWorldTime > Number.MAX_SAFE_INTEGER || task.progressMinutes > duration) issues.push(issue(task.id, 'delegation.invalid-lifecycle'))
      if (task.status === 'in-progress') {
        if (task.outcome !== null || task.progressMinutes !== context.worldTime - task.acceptedAtWorldTime || task.progressMinutes >= duration || task.plannedCompletionAtWorldTime <= context.worldTime || !recipient || !validActiveWorkLink(recipient, task)) issues.push(issue(task.id, 'delegation.invalid-work-link'))
        if (recipients.has(task.recipientId)) issues.push(issue(task.id, 'delegation.invalid-work-link'))
        recipients.add(task.recipientId)
      } else if (task.status === 'completed') {
        if (task.progressMinutes !== duration || !validOutcome(context, task, task.outcome) || task.outcome?.kind !== 'completed' || !recipient || !validTerminalWorkLink(recipient, task, 'resolved')) issues.push(issue(task.id, 'delegation.invalid-lifecycle'))
      } else if (task.status === 'interrupted') {
        if (!validOutcome(context, task, task.outcome) || task.outcome?.kind !== 'interrupted' || task.progressMinutes !== task.outcome.atWorldTime - task.acceptedAtWorldTime || !recipient || !validTerminalWorkLink(recipient, task, 'cancelled')) issues.push(issue(task.id, 'delegation.invalid-lifecycle'))
      }
    }
  }
  if (tasks.some((task, index) => index > 0 && record(task) && record(tasks[index - 1]) && String(tasks[index - 1]!.id) >= String(task.id))) issues.push(issue('delegation:tasks', 'delegation.invalid-lifecycle'))
  try {
    const contentAudit = auditMedievalContentSafety(taskContentRecords({ tasks: tasks as DelegatedTaskRecord[] }))
    if (contentAudit.status !== 'accepted' || !same(contentAudit, value.contentSafetyAudit)) issues.push(issue('delegation:content-safety', 'delegation.invalid-content-audit'))
  } catch { issues.push(issue('delegation:content-safety', 'delegation.invalid-content-audit')) }
  return canonicalIssues(issues)
}

/** Creates one deterministic agreement/refusal after the required conversation gate. */
export const offerDelegatedTask = (context: DelegationContext, state: DelegationState, people: readonly PersistentPersonRecord[], offer: DelegationOfferInput, assessment: ConversationAssessment): DelegationOfferTransition => {
  const stateDiagnostics = validateDelegationState({ ...context, people }, state)
  if (stateDiagnostics.length) throw new DelegationContractError(stateDiagnostics)
  if (!isDelegationOfferInput(offer)) throw new DelegationContractError([issue('delegation:offer', 'delegation.invalid-offer')])
  if (!assessmentMatchesOffer(assessment, offer)) throw new DelegationContractError([issue(offer.id, 'delegation.invalid-assessment')])
  if (assessment.eligibility !== 'eligible') throw new DelegationContractError([issue(offer.id, 'delegation.conversation-blocked')])
  if (!assessment.unlockedApproaches.includes(offer.approach)) throw new DelegationContractError([issue(offer.id, 'delegation.approach-locked')])
  const definition = delegationDefinitionFor(offer.family)
  if (!definition.relevantMaterialInterests.includes(offer.proposal.materialInterest) || !definition.allowedComplexities.includes(offer.proposal.complexity) || !definition.allowedUrgencies.includes(offer.proposal.urgency)) throw new DelegationContractError([issue(offer.id, 'delegation.invalid-family-context')])
  const taskId = delegationTaskIdForOffer(offer.id)
  if (state.tasks.some(task => task.id === taskId)) throw new DelegationContractError([issue(taskId, 'delegation.duplicate-task-id')])
  if (state.tasks.length >= DELEGATION_LIMITS.tasks) throw new DelegationContractError([issue('delegation:tasks', 'delegation.task-limit')])
  if (state.tasks.filter(task => task.status !== 'in-progress').length >= DELEGATION_LIMITS.terminalTasks) throw new DelegationContractError([issue('delegation:tasks', 'delegation.terminal-task-limit')])
  const recipient = people.find(person => person.id === offer.recipientId)
  const courier = people.find(person => person.id === offer.courierId)
  if (!recipient || !courier || recipient.life.status !== 'living' || courier.life.status !== 'living' || !recipient.identity.adult || !courier.identity.adult) throw new DelegationContractError([issue(taskId, 'delegation.invalid-reference')])
  const agreement = agreementFor(context, offer, assessment, recipient, definition)
  if (agreement.status === 'accepted' && (state.tasks.filter(task => task.status === 'in-progress').length >= DELEGATION_LIMITS.activeTasks || recipient.work.capacity.current < 2)) throw new DelegationContractError([issue(taskId, 'delegation.active-task-limit')])
  const common = {
    version: DELEGATION_CONTRACT_VERSION,
    id: taskId,
    offerId: offer.id,
    family: offer.family,
    courierId: offer.courierId,
    recipientId: offer.recipientId,
    approach: offer.approach,
    proposal: structuredClone(offer.proposal),
    assessment: structuredClone(assessment),
    offeredAtWorldTime: context.worldTime,
    agreement,
    contentSafety: taskClassification()
  }
  const task: DelegatedTaskRecord = agreement.status === 'refused'
    ? {
        ...common,
        status: 'refused',
        progressMinutes: 0,
        outcome: { kind: 'refused', atWorldTime: context.worldTime, token: taskToken(context, 'refusal', taskId, context.worldTime), factors: canonical([...agreement.factors, 'agreement-refused']) }
      }
    : {
        ...common,
        status: 'in-progress',
        acceptedAtWorldTime: context.worldTime,
        plannedCompletionAtWorldTime: context.worldTime + definition.durationMinutes[offer.proposal.complexity],
        progressMinutes: 0,
        outcome: null
      }
  const updatedPeople = agreement.status === 'accepted'
    ? appendTaskMemories(reserveWorker(people, task), task, 'agreement', context.worldTime)
    : appendTaskMemories(people, task, 'refusal', context.worldTime)
  const nextWithoutAudit = { version: DELEGATION_STATE_VERSION, tasks: [...state.tasks, task].sort((left, right) => compare(left.id, right.id)) } satisfies Omit<DelegationState, 'contentSafetyAudit'>
  const next = { ...nextWithoutAudit, contentSafetyAudit: audit(nextWithoutAudit) }
  const diagnostics = validateDelegationState({ ...context, people: updatedPeople }, next)
  if (diagnostics.length) throw new DelegationContractError(diagnostics)
  return { state: next, people: updatedPeople, task }
}

const completedTask = (context: DelegationContext, task: DelegatedTaskRecord): DelegatedTaskRecord => {
  const atWorldTime = task.plannedCompletionAtWorldTime!
  const roll = taskRoll(context, 'completion', task.id, atWorldTime)
  const completionBand: DelegationCompletionBand = task.agreement.threshold >= roll ? 'steady' : 'strained'
  return {
    ...task,
    status: 'completed',
    progressMinutes: atWorldTime - task.acceptedAtWorldTime!,
    outcome: {
      kind: 'completed',
      atWorldTime,
      completionBand,
      roll,
      token: taskToken(context, 'completion', task.id, atWorldTime),
      factors: canonical([...task.agreement.factors, completionRollFactor(roll), completionBand === 'steady' ? 'outcome-completed-steady' : 'outcome-completed-strained'])
    }
  }
}

/** Folds only tasks whose canonical due minute lies in this action interval. */
export const advanceDelegatedTasks = (context: DelegationContext, state: DelegationState, people: readonly PersistentPersonRecord[], action: { startedAtWorldTime: number; atWorldTime: number }): DelegationProgressTransition => {
  if (!safeInteger(action.startedAtWorldTime) || !safeInteger(action.atWorldTime) || action.startedAtWorldTime >= action.atWorldTime || action.atWorldTime !== context.worldTime) throw new DelegationContractError([issue('delegation:action', 'delegation.invalid-lifecycle')])
  const before = validateDelegationState({ ...context, worldTime: action.startedAtWorldTime, people }, state)
  if (before.length) throw new DelegationContractError(before)
  const due = state.tasks.filter(task => task.status === 'in-progress' && task.plannedCompletionAtWorldTime! <= action.atWorldTime)
    .sort((left, right) => left.plannedCompletionAtWorldTime! - right.plannedCompletionAtWorldTime! || compare(left.id, right.id))
  let nextPeople = structuredClone(people)
  const byId = new Map(due.map(task => [task.id, completedTask(context, task)]))
  for (const task of due) {
    const completed = byId.get(task.id)!
    nextPeople = appendTaskMemories(releaseWorker(nextPeople, completed, 'resolved', completed.outcome!.atWorldTime), completed, 'completion', completed.outcome!.atWorldTime)
  }
  const tasks = state.tasks.map(task => byId.get(task.id) ?? structuredClone(task)).sort((left, right) => compare(left.id, right.id))
  const nextWithoutAudit = { version: DELEGATION_STATE_VERSION, tasks } satisfies Omit<DelegationState, 'contentSafetyAudit'>
  const next = { ...nextWithoutAudit, contentSafetyAudit: audit(nextWithoutAudit) }
  const diagnostics = validateDelegationState({ ...context, people: nextPeople }, next)
  if (diagnostics.length) throw new DelegationContractError(diagnostics)
  return { state: next, people: nextPeople, resolvedTaskIds: due.map(task => task.id) }
}

/** Applies a v1 courier recall/cancellation after its one-minute resolution action. */
export const interruptDelegatedTask = (context: DelegationContext, state: DelegationState, people: readonly PersistentPersonRecord[], interruption: DelegationInterruptionInput): DelegationProgressTransition => {
  const diagnostics = validateDelegationState(context, state)
  if (diagnostics.length) throw new DelegationContractError(diagnostics)
  if (!isDelegationInterruptionInput(interruption)) throw new DelegationContractError([issue('delegation:interruption', 'delegation.malformed-interruption')])
  const task = state.tasks.find(candidate => candidate.id === interruption.taskId)
  if (!task || task.status !== 'in-progress') throw new DelegationContractError([issue(interruption.taskId, 'delegation.task-not-active')])
  if (task.courierId !== interruption.courierId) throw new DelegationContractError([issue(interruption.taskId, 'delegation.invalid-interruption')])
  if (task.plannedCompletionAtWorldTime! <= context.worldTime) throw new DelegationContractError([issue(interruption.taskId, 'delegation.task-due-before-interruption')])
  const interrupted: DelegatedTaskRecord = {
    ...task,
    status: 'interrupted',
    progressMinutes: context.worldTime - task.acceptedAtWorldTime!,
    outcome: {
      kind: 'interrupted',
      atWorldTime: context.worldTime,
      reason: interruption.reason,
      token: taskToken(context, `interruption:${interruption.reason}`, task.id, context.worldTime),
      factors: canonical([...task.agreement.factors, interruption.reason === 'courier-recall' ? 'outcome-interrupted-courier-recall' : 'outcome-interrupted-courier-cancellation'])
    }
  }
  const nextPeople = appendTaskMemories(releaseWorker(people, interrupted, 'cancelled', context.worldTime), interrupted, 'interruption', context.worldTime)
  const nextWithoutAudit = { version: DELEGATION_STATE_VERSION, tasks: state.tasks.map(candidate => candidate.id === task.id ? interrupted : structuredClone(candidate)).sort((left, right) => compare(left.id, right.id)) } satisfies Omit<DelegationState, 'contentSafetyAudit'>
  const next = { ...nextWithoutAudit, contentSafetyAudit: audit(nextWithoutAudit) }
  const nextDiagnostics = validateDelegationState({ ...context, people: nextPeople }, next)
  if (nextDiagnostics.length) throw new DelegationContractError(nextDiagnostics)
  return { state: next, people: nextPeople, resolvedTaskIds: [task.id] }
}
