import { auditMedievalContentSafety, classifyMedievalContent, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { PersistentPersonMaterialInterest, PersistentPersonRecord } from './persistent-person'
import { isValidFoundationWorld } from './world'
import type { FoundationWorld } from './types'

/**
 * A pure planning contract for a later delegation/negotiation reducer. It
 * assesses communication readiness only; it does not create an offer, change
 * a relationship, assign work, advance time, or resolve an outcome.
 */
export const CONVERSATION_CONTRACT_VERSION = 1 as const

export const CONVERSATION_LEVELS = [1, 2, 3, 4, 5] as const
export type ConversationLevel = typeof CONVERSATION_LEVELS[number]

export const CONVERSATION_APPROACHES = [
  'direct-request',
  'shared-context',
  'terms-outline',
  'contingency-check',
  'reciprocal-options'
] as const
export type ConversationApproach = typeof CONVERSATION_APPROACHES[number]

export const CONVERSATION_PROPOSAL_KINDS = ['request', 'coordination', 'terms'] as const
export const CONVERSATION_URGENCY_BANDS = ['routine', 'pressing'] as const
export const CONVERSATION_COMPLEXITY_BANDS = ['routine', 'involved'] as const

export type ConversationProposalKind = typeof CONVERSATION_PROPOSAL_KINDS[number]
export type ConversationUrgencyBand = typeof CONVERSATION_URGENCY_BANDS[number]
export type ConversationComplexityBand = typeof CONVERSATION_COMPLEXITY_BANDS[number]
export type ConversationClarityBand = 'limited' | 'basic' | 'clear' | 'precise' | 'thorough' | 'not-assessable'
export type ConversationReadinessSupport = 'minimal' | 'limited' | 'clear' | 'strong' | 'expert'
export type ConversationEligibility = 'eligible' | 'blocked'
export type ConversationRapportBand = 'strained' | 'unformed' | 'steady' | 'strong' | 'not-assessable'
export type ConversationInterestAlignment = 'aligned' | 'not-aligned' | 'not-assessable'
export type ConversationCapacityBand = 'none' | 'limited' | 'ready' | 'not-assessable'
export type ConversationCurrentWorkBand = 'idle' | 'committed' | 'not-assessable'
export type ConversationCommitmentBand = 'none' | 'active' | 'not-assessable'
export type ConversationNeedsPressure = 'settled' | 'pressured' | 'urgent' | 'not-assessable'
export type ConversationHealthBand = 'steady' | 'strained' | 'injured' | 'recovering' | 'not-assessable'
export type ConversationSafetyPressure = 'settled' | 'elevated' | 'urgent' | 'not-assessable'
export type ConversationRiskBand = 'low' | 'guarded' | 'high' | 'not-assessable'
export type ConversationAgreementReadiness = 'unlikely' | 'conditional' | 'open' | 'not-assessable'

/** Hard barriers mean no ask or coordination attempt is presently assessable. */
export const CONVERSATION_BARRIERS = [
  'active-courier-missing',
  'courier-missing',
  'courier-not-active',
  'courier-dead',
  'courier-unavailable',
  'recipient-missing',
  'recipient-is-courier',
  'recipient-dead',
  'recipient-not-colocated',
  'recipient-unavailable',
  'recipient-no-capacity',
  'recipient-current-work',
  'recipient-active-commitment',
  'recipient-needs-protected',
  'recipient-safety-protected',
  'recipient-health-protected'
] as const
export type ConversationBarrier = typeof CONVERSATION_BARRIERS[number]

export type ConversationFactorCode =
  | `barrier:${ConversationBarrier}`
  | `courier-level:${ConversationLevel}`
  | `proposal-kind:${ConversationProposalKind}`
  | `proposal-urgency:${ConversationUrgencyBand}`
  | `proposal-complexity:${ConversationComplexityBand}`
  | `clarity:${Exclude<ConversationClarityBand, 'not-assessable'>}`
  | `readiness-support:${ConversationReadinessSupport}`
  | `recipient-rapport:${Exclude<ConversationRapportBand, 'not-assessable'>}`
  | `recipient-interest:${Exclude<ConversationInterestAlignment, 'not-assessable'>}`
  | `recipient-capacity:${Exclude<ConversationCapacityBand, 'not-assessable'>}`
  | `recipient-current-work:${Exclude<ConversationCurrentWorkBand, 'not-assessable'>}`
  | `recipient-commitment:${Exclude<ConversationCommitmentBand, 'not-assessable'>}`
  | `recipient-needs:${Exclude<ConversationNeedsPressure, 'not-assessable'>}`
  | `recipient-health:${Exclude<ConversationHealthBand, 'not-assessable'>}`
  | `recipient-safety:${Exclude<ConversationSafetyPressure, 'not-assessable'>}`
  | `eligibility:${ConversationEligibility}`
  | `risk:${Exclude<ConversationRiskBand, 'not-assessable'>}`
  | `readiness:${Exclude<ConversationAgreementReadiness, 'not-assessable'>}`

/** Runtime vocabulary for renderers and tests; the assessment returns only these codes. */
export const CONVERSATION_FACTOR_CODES: readonly ConversationFactorCode[] = [
  ...CONVERSATION_BARRIERS.map(barrier => `barrier:${barrier}` as ConversationFactorCode),
  ...CONVERSATION_LEVELS.map(level => `courier-level:${level}` as ConversationFactorCode),
  ...CONVERSATION_PROPOSAL_KINDS.map(kind => `proposal-kind:${kind}` as ConversationFactorCode),
  ...CONVERSATION_URGENCY_BANDS.map(urgency => `proposal-urgency:${urgency}` as ConversationFactorCode),
  ...CONVERSATION_COMPLEXITY_BANDS.map(complexity => `proposal-complexity:${complexity}` as ConversationFactorCode),
  ...(['limited', 'basic', 'clear', 'precise', 'thorough'] as const).map(clarity => `clarity:${clarity}` as ConversationFactorCode),
  ...(['minimal', 'limited', 'clear', 'strong', 'expert'] as const).map(support => `readiness-support:${support}` as ConversationFactorCode),
  ...(['strained', 'unformed', 'steady', 'strong'] as const).map(rapport => `recipient-rapport:${rapport}` as ConversationFactorCode),
  ...(['aligned', 'not-aligned'] as const).map(alignment => `recipient-interest:${alignment}` as ConversationFactorCode),
  ...(['none', 'limited', 'ready'] as const).map(capacity => `recipient-capacity:${capacity}` as ConversationFactorCode),
  ...(['idle', 'committed'] as const).map(work => `recipient-current-work:${work}` as ConversationFactorCode),
  ...(['none', 'active'] as const).map(commitment => `recipient-commitment:${commitment}` as ConversationFactorCode),
  ...(['settled', 'pressured', 'urgent'] as const).map(needs => `recipient-needs:${needs}` as ConversationFactorCode),
  ...(['steady', 'strained', 'injured', 'recovering'] as const).map(health => `recipient-health:${health}` as ConversationFactorCode),
  ...(['settled', 'elevated', 'urgent'] as const).map(safety => `recipient-safety:${safety}` as ConversationFactorCode),
  'eligibility:eligible',
  'eligibility:blocked',
  'risk:low',
  'risk:guarded',
  'risk:high',
  'readiness:unlikely',
  'readiness:conditional',
  'readiness:open'
]

export interface ConversationProposal {
  version: typeof CONVERSATION_CONTRACT_VERSION
  kind: ConversationProposalKind
  urgency: ConversationUrgencyBand
  complexity: ConversationComplexityBand
  materialInterest: PersistentPersonMaterialInterest
  contentSafety: MedievalContentSafetyClassification
}

export interface ConversationAssessmentRequest {
  version: typeof CONVERSATION_CONTRACT_VERSION
  courierId: string
  recipientId: string
  proposal: ConversationProposal
}

export interface ConversationCapabilities {
  level: ConversationLevel
  approaches: readonly ConversationApproach[]
  clarity: Exclude<ConversationClarityBand, 'not-assessable'>
  readinessSupport: ConversationReadinessSupport
}

export interface ConversationAssessment {
  version: typeof CONVERSATION_CONTRACT_VERSION
  courierId: string
  recipientId: string
  eligibility: ConversationEligibility
  barriers: readonly ConversationBarrier[]
  unlockedApproaches: readonly ConversationApproach[]
  taskClarity: ConversationClarityBand
  recipientRapport: ConversationRapportBand
  recipientInterestAlignment: ConversationInterestAlignment
  recipientCapacity: ConversationCapacityBand
  recipientCurrentWork: ConversationCurrentWorkBand
  recipientCommitment: ConversationCommitmentBand
  recipientNeedsPressure: ConversationNeedsPressure
  recipientHealth: ConversationHealthBand
  recipientSafetyPressure: ConversationSafetyPressure
  risk: ConversationRiskBand
  agreementReadiness: ConversationAgreementReadiness
  factors: readonly ConversationFactorCode[]
  contentSafety: MedievalContentSafetyClassification
}

export type ConversationDiagnosticCode =
  | 'conversation.invalid-world'
  | 'conversation.malformed-request'
  | 'conversation.invalid-version'
  | 'conversation.invalid-courier-id'
  | 'conversation.invalid-recipient-id'
  | 'conversation.malformed-proposal'
  | 'conversation.invalid-proposal-version'
  | 'conversation.invalid-proposal-kind'
  | 'conversation.invalid-proposal-urgency'
  | 'conversation.invalid-proposal-complexity'
  | 'conversation.invalid-proposal-interest'
  | 'conversation.invalid-proposal-content'
  | 'conversation.invalid-conversation-level'
  | MedievalContentSafetyDiagnosticCode

export interface ConversationDiagnostic {
  code: ConversationDiagnosticCode
  recordId: string
}

const capabilities: Readonly<Record<ConversationLevel, ConversationCapabilities>> = {
  1: { level: 1, approaches: ['direct-request'], clarity: 'limited', readinessSupport: 'minimal' },
  2: { level: 2, approaches: ['direct-request', 'shared-context'], clarity: 'basic', readinessSupport: 'limited' },
  3: { level: 3, approaches: ['direct-request', 'shared-context', 'terms-outline'], clarity: 'clear', readinessSupport: 'clear' },
  4: { level: 4, approaches: ['direct-request', 'shared-context', 'terms-outline', 'contingency-check'], clarity: 'precise', readinessSupport: 'strong' },
  5: { level: 5, approaches: ['direct-request', 'shared-context', 'terms-outline', 'contingency-check', 'reciprocal-options'], clarity: 'thorough', readinessSupport: 'expert' }
}

const readinessContributions: Readonly<Record<ConversationReadinessSupport, number>> = {
  minimal: 0,
  limited: 1,
  clear: 2,
  strong: 3,
  expert: 4
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const isLevel = (value: unknown): value is ConversationLevel => CONVERSATION_LEVELS.includes(value as ConversationLevel)
const validId = (value: unknown): value is string => typeof value === 'string' && value.length > 0 && value.length <= 96 && /^[a-z][a-z0-9:._-]*$/i.test(value)
const issue = (recordId: string, code: ConversationDiagnosticCode): ConversationDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly ConversationDiagnostic[]): readonly ConversationDiagnostic[] => [...new Map(diagnostics.map(diagnostic => [`${diagnostic.recordId}\u0000${diagnostic.code}`, diagnostic])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const canonicalCodes = <Code extends string>(codes: readonly Code[]): readonly Code[] => [...new Set(codes)].sort(compare)
const includes = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)

const proposalSafety = (value: unknown): readonly ConversationDiagnostic[] => {
  if (!record(value)) return [issue('conversation:proposal', 'conversation.malformed-proposal')]
  const audit = auditMedievalContentSafety([{ id: 'conversation:proposal', domain: 'contract', classification: value.contentSafety }])
  return audit.status === 'accepted' ? [] : audit.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code))
}

/** Validates the whole foundation-world boundary before an assessment can read it. */
export const validateConversationAssessmentRequest = (world: unknown, request: unknown): readonly ConversationDiagnostic[] => {
  const diagnostics: ConversationDiagnostic[] = []
  if (!isValidFoundationWorld(world)) diagnostics.push(issue('conversation:world', 'conversation.invalid-world'))
  if (!record(request) || !hasOnlyKeys(request, ['version', 'courierId', 'recipientId', 'proposal'])) return canonicalDiagnostics([...diagnostics, issue('conversation:request', 'conversation.malformed-request')])
  if (request.version !== CONVERSATION_CONTRACT_VERSION) diagnostics.push(issue('conversation:request', 'conversation.invalid-version'))
  if (!validId(request.courierId)) diagnostics.push(issue('conversation:courier', 'conversation.invalid-courier-id'))
  if (!validId(request.recipientId)) diagnostics.push(issue('conversation:recipient', 'conversation.invalid-recipient-id'))
  if (!record(request.proposal) || !hasOnlyKeys(request.proposal, ['version', 'kind', 'urgency', 'complexity', 'materialInterest', 'contentSafety'])) {
    diagnostics.push(issue('conversation:proposal', 'conversation.malformed-proposal'))
  } else {
    if (request.proposal.version !== CONVERSATION_CONTRACT_VERSION) diagnostics.push(issue('conversation:proposal', 'conversation.invalid-proposal-version'))
    if (!includes(CONVERSATION_PROPOSAL_KINDS, request.proposal.kind)) diagnostics.push(issue('conversation:proposal', 'conversation.invalid-proposal-kind'))
    if (!includes(CONVERSATION_URGENCY_BANDS, request.proposal.urgency)) diagnostics.push(issue('conversation:proposal', 'conversation.invalid-proposal-urgency'))
    if (!includes(CONVERSATION_COMPLEXITY_BANDS, request.proposal.complexity)) diagnostics.push(issue('conversation:proposal', 'conversation.invalid-proposal-complexity'))
    if (!includes(['waterway-knowledge', 'route-safety', 'trade-ledgers', 'goods-care', 'craft-work', 'vessel-upkeep', 'household-upkeep', 'household-security', 'provisions', 'care-work', 'records'] as const, request.proposal.materialInterest)) diagnostics.push(issue('conversation:proposal', 'conversation.invalid-proposal-interest'))
    const safety = proposalSafety(request.proposal)
    if (safety.length) diagnostics.push(...safety)
  }
  return canonicalDiagnostics(diagnostics)
}

/** The only level mapping. Persistent `identity.conversation` remains authoritative. */
export const conversationCapabilitiesForLevel = (value: unknown): ConversationCapabilities => {
  if (!isLevel(value)) throw new ConversationContractError([issue('conversation:level', 'conversation.invalid-conversation-level')])
  return { ...capabilities[value], approaches: [...capabilities[value].approaches] }
}

const clarityFor = (capability: ConversationCapabilities, complexity: ConversationComplexityBand): Exclude<ConversationClarityBand, 'not-assessable'> => {
  if (complexity === 'routine') return capability.clarity
  const levels: readonly Exclude<ConversationClarityBand, 'not-assessable'>[] = ['limited', 'basic', 'clear', 'precise', 'thorough']
  return levels[Math.max(0, levels.indexOf(capability.clarity) - 1)]!
}

const rapportFor = (recipient: PersistentPersonRecord, courierId: string): ConversationRapportBand => {
  const relationship = recipient.relationships.find(item => item.targetPersonId === courierId)
  if (!relationship || relationship.standing === 0) return 'unformed'
  if (relationship.standing < 0) return 'strained'
  return relationship.standing === 1 ? 'steady' : 'strong'
}

const capacityFor = (recipient: PersistentPersonRecord): ConversationCapacityBand => recipient.work.capacity.current === 0
  ? 'none'
  : recipient.work.capacity.current === 1 ? 'limited' : 'ready'

const needsPressureFor = (recipient: PersistentPersonRecord): ConversationNeedsPressure => {
  const highest = Math.max(recipient.needs.nourishment, recipient.needs.rest, recipient.needs.shelter, recipient.needs.safety)
  return highest >= 5 ? 'urgent' : highest >= 3 ? 'pressured' : 'settled'
}

const safetyPressureFor = (recipient: PersistentPersonRecord): ConversationSafetyPressure => recipient.needs.safety >= 5
  ? 'urgent'
  : recipient.needs.safety >= 3 ? 'elevated' : 'settled'

const currentWorkFor = (recipient: PersistentPersonRecord): ConversationCurrentWorkBand => recipient.work.current.status === 'committed' ? 'committed' : 'idle'
const commitmentFor = (recipient: PersistentPersonRecord): ConversationCommitmentBand => recipient.commitments.some(commitment => commitment.status === 'active') ? 'active' : 'none'
const assessmentClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])

const readinessFor = (
  capability: ConversationCapabilities,
  rapport: Exclude<ConversationRapportBand, 'not-assessable'>,
  alignment: Exclude<ConversationInterestAlignment, 'not-assessable'>,
  capacity: Exclude<ConversationCapacityBand, 'not-assessable'>,
  needs: Exclude<ConversationNeedsPressure, 'not-assessable'>,
  health: Exclude<ConversationHealthBand, 'not-assessable'>,
  safety: Exclude<ConversationSafetyPressure, 'not-assessable'>,
  proposal: ConversationProposal
): ConversationAgreementReadiness => {
  let score = readinessContributions[capability.readinessSupport]
  score += rapport === 'strong' ? 2 : rapport === 'steady' ? 1 : rapport === 'strained' ? -2 : 0
  score += alignment === 'aligned' ? 1 : -1
  score -= capacity === 'limited' ? 1 : 0
  score -= needs === 'pressured' ? 1 : 0
  score -= health === 'recovering' ? 1 : 0
  score -= safety === 'elevated' ? 1 : 0
  score -= proposal.urgency === 'pressing' ? 1 : 0
  score -= proposal.complexity === 'involved' ? 1 : 0
  const uncapped: ConversationAgreementReadiness = score <= 0 ? 'unlikely' : score <= 3 ? 'conditional' : 'open'
  if (rapport === 'strained') return 'unlikely'
  if (rapport === 'unformed' || alignment === 'not-aligned' || capacity === 'limited' || needs === 'pressured' || health === 'recovering' || safety === 'elevated' || proposal.complexity === 'involved') return uncapped === 'open' ? 'conditional' : uncapped
  return uncapped
}

const riskFor = (
  rapport: Exclude<ConversationRapportBand, 'not-assessable'>,
  capacity: Exclude<ConversationCapacityBand, 'not-assessable'>,
  needs: Exclude<ConversationNeedsPressure, 'not-assessable'>,
  health: Exclude<ConversationHealthBand, 'not-assessable'>,
  safety: Exclude<ConversationSafetyPressure, 'not-assessable'>,
  proposal: ConversationProposal
): ConversationRiskBand => {
  const pressure = Number(rapport === 'strained')
    + Number(capacity === 'limited')
    + Number(needs === 'pressured')
    + Number(health === 'recovering')
    + Number(safety === 'elevated')
    + Number(proposal.urgency === 'pressing')
    + Number(proposal.complexity === 'involved')
  return pressure === 0 ? 'low' : pressure <= 2 ? 'guarded' : 'high'
}

const factorCodesFor = (
  capability: ConversationCapabilities | undefined,
  proposal: ConversationProposal,
  eligibility: ConversationEligibility,
  barriers: readonly ConversationBarrier[],
  clarity: ConversationClarityBand,
  rapport: ConversationRapportBand,
  alignment: ConversationInterestAlignment,
  capacity: ConversationCapacityBand,
  currentWork: ConversationCurrentWorkBand,
  commitment: ConversationCommitmentBand,
  needs: ConversationNeedsPressure,
  health: ConversationHealthBand,
  safety: ConversationSafetyPressure,
  risk: ConversationRiskBand,
  readiness: ConversationAgreementReadiness
): readonly ConversationFactorCode[] => canonicalCodes([
  `proposal-kind:${proposal.kind}` as ConversationFactorCode,
  `proposal-urgency:${proposal.urgency}` as ConversationFactorCode,
  `proposal-complexity:${proposal.complexity}` as ConversationFactorCode,
  `eligibility:${eligibility}` as ConversationFactorCode,
  ...(capability === undefined ? [] : [
    `courier-level:${capability.level}` as ConversationFactorCode,
    `readiness-support:${capability.readinessSupport}` as ConversationFactorCode
  ]),
  ...barriers.map(barrier => `barrier:${barrier}` as ConversationFactorCode),
  ...(clarity === 'not-assessable' ? [] : [`clarity:${clarity}` as ConversationFactorCode]),
  ...(rapport === 'not-assessable' ? [] : [`recipient-rapport:${rapport}` as ConversationFactorCode]),
  ...(alignment === 'not-assessable' ? [] : [`recipient-interest:${alignment}` as ConversationFactorCode]),
  ...(capacity === 'not-assessable' ? [] : [`recipient-capacity:${capacity}` as ConversationFactorCode]),
  ...(currentWork === 'not-assessable' ? [] : [`recipient-current-work:${currentWork}` as ConversationFactorCode]),
  ...(commitment === 'not-assessable' ? [] : [`recipient-commitment:${commitment}` as ConversationFactorCode]),
  ...(needs === 'not-assessable' ? [] : [`recipient-needs:${needs}` as ConversationFactorCode]),
  ...(health === 'not-assessable' ? [] : [`recipient-health:${health}` as ConversationFactorCode]),
  ...(safety === 'not-assessable' ? [] : [`recipient-safety:${safety}` as ConversationFactorCode]),
  ...(risk === 'not-assessable' ? [] : [`risk:${risk}` as ConversationFactorCode]),
  ...(readiness === 'not-assessable' ? [] : [`readiness:${readiness}` as ConversationFactorCode])
])

/** The shared evaluator has no mutation or time authority. */
const assessValidCourierConversation = (validWorld: FoundationWorld, validRequest: ConversationAssessmentRequest): ConversationAssessment => {
  const courier = validWorld.state.people.records.find(person => person.id === validRequest.courierId)
  const recipient = validWorld.state.people.records.find(person => person.id === validRequest.recipientId)
  const capability = courier === undefined ? undefined : conversationCapabilitiesForLevel(courier.identity.conversation)
  const barriers: ConversationBarrier[] = []

  if (validWorld.state.courier.initialCourierId === undefined) barriers.push('active-courier-missing')
  else if (validWorld.state.courier.initialCourierId !== validRequest.courierId) barriers.push('courier-not-active')
  if (!courier) barriers.push('courier-missing')
  else {
    if (courier.life.status !== 'living') barriers.push('courier-dead')
    if (courier.work.availability !== 'available') barriers.push('courier-unavailable')
  }
  if (!recipient) barriers.push('recipient-missing')
  else {
    if (recipient.id === validRequest.courierId) barriers.push('recipient-is-courier')
    if (recipient.life.status !== 'living') barriers.push('recipient-dead')
    if (!courier || courier.location.kind !== recipient.location.kind || courier.location.id !== recipient.location.id) barriers.push('recipient-not-colocated')
    if (recipient.work.availability !== 'available') barriers.push('recipient-unavailable')
    if (recipient.work.capacity.current === 0) barriers.push('recipient-no-capacity')
    if (recipient.work.current.status === 'committed') barriers.push('recipient-current-work')
    if (recipient.commitments.some(commitment => commitment.status === 'active')) barriers.push('recipient-active-commitment')
    if (needsPressureFor(recipient) === 'urgent') barriers.push('recipient-needs-protected')
    if (safetyPressureFor(recipient) === 'urgent') barriers.push('recipient-safety-protected')
    if (recipient.health.condition === 'strained' || recipient.health.condition === 'injured') barriers.push('recipient-health-protected')
  }

  const canonicalBarriers = canonicalCodes(barriers)
  const eligibility: ConversationEligibility = canonicalBarriers.length === 0 ? 'eligible' : 'blocked'
  const clarity = capability === undefined ? 'not-assessable' : clarityFor(capability, validRequest.proposal.complexity)
  const rapport = recipient ? rapportFor(recipient, validRequest.courierId) : 'not-assessable'
  const alignment = recipient ? recipient.materialInterests.includes(validRequest.proposal.materialInterest) ? 'aligned' : 'not-aligned' : 'not-assessable'
  const capacity = recipient ? capacityFor(recipient) : 'not-assessable'
  const currentWork = recipient ? currentWorkFor(recipient) : 'not-assessable'
  const commitment = recipient ? commitmentFor(recipient) : 'not-assessable'
  const needs = recipient ? needsPressureFor(recipient) : 'not-assessable'
  const health = recipient ? recipient.health.condition : 'not-assessable'
  const safety = recipient ? safetyPressureFor(recipient) : 'not-assessable'
  const risk = eligibility === 'blocked' || rapport === 'not-assessable' || capacity === 'not-assessable' || needs === 'not-assessable' || health === 'not-assessable' || safety === 'not-assessable'
    ? 'not-assessable'
    : riskFor(rapport, capacity, needs, health, safety, validRequest.proposal)
  const agreementReadiness = capability === undefined || eligibility === 'blocked' || rapport === 'not-assessable' || alignment === 'not-assessable' || capacity === 'not-assessable' || needs === 'not-assessable' || health === 'not-assessable' || safety === 'not-assessable'
    ? 'not-assessable'
    : readinessFor(capability, rapport, alignment, capacity, needs, health, safety, validRequest.proposal)

  return {
    version: CONVERSATION_CONTRACT_VERSION,
    courierId: validRequest.courierId,
    recipientId: validRequest.recipientId,
    eligibility,
    barriers: canonicalBarriers,
    unlockedApproaches: capability?.approaches ?? [],
    taskClarity: clarity,
    recipientRapport: rapport,
    recipientInterestAlignment: alignment,
    recipientCapacity: capacity,
    recipientCurrentWork: currentWork,
    recipientCommitment: commitment,
    recipientNeedsPressure: needs,
    recipientHealth: health,
    recipientSafetyPressure: safety,
    risk,
    agreementReadiness,
    factors: factorCodesFor(capability, validRequest.proposal, eligibility, canonicalBarriers, clarity, rapport, alignment, capacity, currentWork, commitment, needs, health, safety, risk, agreementReadiness),
    contentSafety: assessmentClassification()
  }
}

/**
 * Assesses a future conversation without creating an offer or changing the
 * world. Recipient conditions remain authoritative; conversation only expands
 * approaches, clarity, and bounded readiness support after hard barriers pass.
 */
export const assessCourierConversation = (world: FoundationWorld | unknown, request: ConversationAssessmentRequest | unknown): ConversationAssessment => {
  const diagnostics = validateConversationAssessmentRequest(world, request)
  if (diagnostics.length) throw new ConversationContractError(diagnostics)
  return assessValidCourierConversation(world as FoundationWorld, request as ConversationAssessmentRequest)
}

/**
 * Internal replay hook. The world reducer reaches it only after the public
 * transition validated the complete foundation world; replay cannot call the
 * public wrapper because validating an intermediate journal projection would
 * recursively replay that same journal.
 */
export const assessCourierConversationForValidatedReplay = (world: FoundationWorld, request: ConversationAssessmentRequest): ConversationAssessment => assessValidCourierConversation(world, request)

export class ConversationContractError extends Error {
  constructor(readonly diagnostics: readonly ConversationDiagnostic[]) {
    super(`conversation assessment rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'ConversationContractError'
  }
}
