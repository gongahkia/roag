import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { FidelityCadence, FidelityIndividualTier, FidelityInstitutionTier, FidelityPlaceTier, FidelityPlan } from './fidelity'
import { SeededRng } from './rng'

/**
 * This is a deterministic scheduling/provenance kernel, not a domain
 * simulation. Its canonical output says which stable cadence windows have
 * become due. Later people, market, and institution systems consume those
 * windows to apply their own rules.
 */
export const SIMULATION_CATCH_UP_CONTRACT_VERSION = 3 as const
export const SIMULATION_OUTCOME_CAUSE_VERSION = 2 as const
export const SIMULATION_CATCH_UP_PROJECTION_VERSION = 1 as const

export const SIMULATION_CATCH_UP_LIMITS = {
  cursors: 96,
  delegatedWork: 24,
  records: 96
} as const

export type SimulationCatchUpTargetKind = 'person' | 'market' | 'institution' | 'delegated-work'
export type SimulationCatchUpTier = FidelityIndividualTier | FidelityPlaceTier | FidelityInstitutionTier
export type SimulationOutcomeDetail = 'detailed' | 'summary'
export type SimulationOutcomeCauseKind = 'fidelity-cadence'
export type SimulationOutcomeEvidenceSource = 'known-world-record' | 'delegated-work-fact'
export type SimulationCadenceMinutes = 1 | 5 | 30 | 120 | 240

export interface DelegatedWorkPlaceholder {
  id: string
  assigneePersonId: string
  status: 'active' | 'resolved'
  committedAtWorldTime: number
  progressIntervals: number
  resolvedAtWorldTime?: number
}

/** A durable cursor aggregates a target's complete canonical windows. */
export interface SimulationCatchUpCursor {
  id: string
  targetKind: SimulationCatchUpTargetKind
  targetId: string
  tier: SimulationCatchUpTier
  cadenceMinutes: SimulationCadenceMinutes
  processedThroughWorldTime: number
  processedIntervals: number
  outcomeToken: number
}

/** A closed link to a stable cadence window; it deliberately has no action ID. */
export interface SimulationOutcomeCauseEvidence {
  source: SimulationOutcomeEvidenceSource
  targetKind: SimulationCatchUpTargetKind
  targetId: string
  cadenceMinutes: SimulationCadenceMinutes
  windowStartWorldTime: number
  windowEndWorldTime: number
  dueIntervals: number
}

export interface SimulationOutcomeCause {
  version: typeof SIMULATION_OUTCOME_CAUSE_VERSION
  kind: SimulationOutcomeCauseKind
  evidence: SimulationOutcomeCauseEvidence
  contentSafety: MedievalContentSafetyClassification
}

/**
 * A bounded audit observation of a target's due windows. The identity and
 * token name the final canonical window, never the action that observed it.
 */
export interface SimulationCatchUpRecord {
  id: string
  targetKind: SimulationCatchUpTargetKind
  targetId: string
  tier: SimulationCatchUpTier
  cadenceMinutes: SimulationCadenceMinutes
  windowStartWorldTime: number
  windowEndWorldTime: number
  dueIntervals: number
  outcomeToken: number
  outcomeDetail: SimulationOutcomeDetail
  cause: SimulationOutcomeCause
  contentSafety: MedievalContentSafetyClassification
}

export interface SimulationCatchUpState {
  version: typeof SIMULATION_CATCH_UP_CONTRACT_VERSION
  cursors: readonly SimulationCatchUpCursor[]
  delegatedWork: readonly DelegatedWorkPlaceholder[]
  records: readonly SimulationCatchUpRecord[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

export interface SimulationCatchUpValidationContext {
  worldId: string
  creationDigest: string
  worldTime: number
  personIds: readonly string[]
  marketIds: readonly string[]
  institutionIds: readonly string[]
}

/** The partition-invariant input future domain systems must consume. */
export interface SimulationCatchUpProjection {
  version: typeof SIMULATION_CATCH_UP_PROJECTION_VERSION
  worldId: string
  creationDigest: string
  worldTime: number
  cursors: readonly SimulationCatchUpCursor[]
  delegatedWork: readonly DelegatedWorkPlaceholder[]
}

export interface SimulationCatchUpTransition {
  state: SimulationCatchUpState
  records: readonly SimulationCatchUpRecord[]
}

export type SimulationCatchUpDiagnosticCode =
  | 'simulation-catchup.malformed-state'
  | 'simulation-catchup.invalid-version'
  | 'simulation-catchup.invalid-cursor'
  | 'simulation-catchup.invalid-delegated-work'
  | 'simulation-catchup.invalid-record'
  | 'simulation-catchup.invalid-cause'
  | 'simulation-catchup.invalid-evidence'
  | 'simulation-catchup.untraceable-cause'
  | 'simulation-catchup.invalid-outcome-token'
  | 'simulation-catchup.invalid-reference'
  | 'simulation-catchup.duplicate-id'
  | 'simulation-catchup.noncanonical-order'
  | 'simulation-catchup.budget-exceeded'
  | 'simulation-catchup.invalid-content-audit'
  | MedievalContentSafetyDiagnosticCode

export interface SimulationCatchUpDiagnostic {
  code: SimulationCatchUpDiagnosticCode
  recordId: string
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const validId = (value: unknown): value is string => typeof value === 'string' && value.length > 0
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const issue = (recordId: string, code: SimulationCatchUpDiagnosticCode): SimulationCatchUpDiagnostic => ({ recordId, code })
const canonicalIssues = (issues: readonly SimulationCatchUpDiagnostic[]): readonly SimulationCatchUpDiagnostic[] => [...new Map(issues.map(value => [`${value.recordId}\u0000${value.code}`, value])).values()].sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const targetKinds: readonly SimulationCatchUpTargetKind[] = ['person', 'market', 'institution', 'delegated-work']
const tiers: readonly SimulationCatchUpTier[] = ['loaded', 'nearby', 'recurring', 'distant-individual-summary', 'deferred', 'historical-only', 'loaded-place', 'distant-settlement-summary', 'loaded-institution', 'distant-institution-summary']
const cadenceMinutes: readonly SimulationCadenceMinutes[] = [1, 5, 30, 120, 240]
const cursorIdFor = (targetKind: SimulationCatchUpTargetKind, targetId: string): string => `catch-up:${targetKind}:${targetId}`
const recordIdFor = (targetKind: SimulationCatchUpTargetKind, targetId: string, cadence: SimulationCadenceMinutes, windowEndWorldTime: number): string => `catch-up:${targetKind}:${targetId}:cadence:${cadence}:window:${windowEndWorldTime}`
const summaryClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('simulation-summary', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const causeClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('data', ['adult-labour', 'navigation'], 'not-applicable', ['simulation-summary'])
const validTier = (value: unknown): value is SimulationCatchUpTier => tiers.includes(value as SimulationCatchUpTier)
const validCadenceMinutes = (value: unknown): value is SimulationCadenceMinutes => cadenceMinutes.includes(value as SimulationCadenceMinutes)
const validOutcomeDetail = (value: unknown): value is SimulationOutcomeDetail => value === 'detailed' || value === 'summary'
const detailForTier = (tier: SimulationCatchUpTier): SimulationOutcomeDetail => tier === 'distant-individual-summary' || tier === 'distant-settlement-summary' || tier === 'distant-institution-summary' ? 'summary' : 'detailed'
const evidenceSourceFor = (targetKind: SimulationCatchUpTargetKind): SimulationOutcomeEvidenceSource => targetKind === 'delegated-work' ? 'delegated-work-fact' : 'known-world-record'
const cadenceForTier = (tier: SimulationCatchUpTier): SimulationCadenceMinutes | undefined => {
  if (tier === 'loaded' || tier === 'loaded-place' || tier === 'loaded-institution') return 1
  if (tier === 'nearby') return 5
  if (tier === 'recurring') return 30
  if (tier === 'distant-individual-summary') return 120
  if (tier === 'distant-settlement-summary' || tier === 'distant-institution-summary') return 240
  return undefined
}

export const simulationCatchUpContentRecords = (state: Pick<SimulationCatchUpState, 'records'>): readonly ClassifiedMedievalContent[] => state.records.flatMap(item => [
  { id: `simulation-catchup:${item.id}`, domain: 'simulation-summary' as const, classification: item.contentSafety },
  { id: `simulation-cause:${item.id}`, domain: 'data' as const, classification: item.cause?.contentSafety as MedievalContentSafetyClassification }
])

const audit = (state: Pick<SimulationCatchUpState, 'records'>): MedievalContentSafetyAudit => {
  const result = auditMedievalContentSafety(simulationCatchUpContentRecords(state))
  if (result.status === 'rejected') throw new Error(`simulation catch-up content rejected: ${result.diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
  return result
}

export const createSimulationCatchUpState = (): SimulationCatchUpState => {
  const state = { version: SIMULATION_CATCH_UP_CONTRACT_VERSION, cursors: [], delegatedWork: [], records: [] } satisfies Omit<SimulationCatchUpState, 'contentSafetyAudit'>
  return { ...state, contentSafetyAudit: audit(state) }
}

/** Adds future delegation data without inventing its social or UI workflow. */
export const withDelegatedWorkPlaceholder = (
  state: SimulationCatchUpState,
  context: SimulationCatchUpValidationContext,
  delegatedWork: DelegatedWorkPlaceholder
): SimulationCatchUpState => {
  const diagnostics = validateSimulationCatchUpState(context, state)
  if (diagnostics.length) throw new SimulationCatchUpContractError(diagnostics)
  const nextWithoutAudit = {
    version: SIMULATION_CATCH_UP_CONTRACT_VERSION,
    cursors: structuredClone(state.cursors),
    delegatedWork: [...state.delegatedWork, structuredClone(delegatedWork)].sort((left, right) => compare(left.id, right.id)),
    records: structuredClone(state.records)
  } satisfies Omit<SimulationCatchUpState, 'contentSafetyAudit'>
  const next: SimulationCatchUpState = { ...nextWithoutAudit, contentSafetyAudit: audit(nextWithoutAudit) }
  const nextDiagnostics = validateSimulationCatchUpState(context, next)
  if (nextDiagnostics.length) throw new SimulationCatchUpContractError(nextDiagnostics)
  return next
}

const targetExists = (context: SimulationCatchUpValidationContext, targetKind: SimulationCatchUpTargetKind, targetId: string, delegatedWorkIds: ReadonlySet<string>): boolean => {
  if (targetKind === 'person') return context.personIds.includes(targetId)
  if (targetKind === 'market') return context.marketIds.includes(targetId)
  if (targetKind === 'institution') return context.institutionIds.includes(targetId)
  return delegatedWorkIds.has(targetId)
}

const validCauseEvidenceShape = (value: unknown): value is SimulationOutcomeCauseEvidence => record(value)
  && hasOnlyKeys(value, ['source', 'targetKind', 'targetId', 'cadenceMinutes', 'windowStartWorldTime', 'windowEndWorldTime', 'dueIntervals'])
  && (value.source === 'known-world-record' || value.source === 'delegated-work-fact')
  && targetKinds.includes(value.targetKind as SimulationCatchUpTargetKind)
  && validId(value.targetId)
  && validCadenceMinutes(value.cadenceMinutes)
  && safeInteger(value.windowStartWorldTime)
  && safeInteger(value.windowEndWorldTime)
  && value.windowStartWorldTime > 0
  && value.windowStartWorldTime <= value.windowEndWorldTime
  && safeInteger(value.dueIntervals)
  && value.dueIntervals > 0

const validCauseEnvelope = (value: unknown): value is Record<string, unknown> => record(value)
  && hasOnlyKeys(value, ['version', 'kind', 'evidence', 'contentSafety'])
  && value.version === SIMULATION_OUTCOME_CAUSE_VERSION
  && value.kind === 'fidelity-cadence'

const validCauseShape = (value: unknown): value is SimulationOutcomeCause => validCauseEnvelope(value)
  && validCauseEvidenceShape(value.evidence)
  && auditMedievalContentSafety([{ id: 'simulation-cause:validation', domain: 'data', classification: value.contentSafety }]).status === 'accepted'

const outcomeTokenFor = (
  context: Pick<SimulationCatchUpValidationContext, 'worldId' | 'creationDigest'>,
  targetKind: SimulationCatchUpTargetKind,
  targetId: string,
  cadence: SimulationCadenceMinutes,
  windowEndWorldTime: number
): number => new SeededRng(`jomon-simulation-catchup-v${SIMULATION_CATCH_UP_CONTRACT_VERSION}|${context.creationDigest}|${context.worldId}|${targetKind}|${targetId}|cadence:${cadence}|window:${windowEndWorldTime}`).integer(1_000_000)

const validDelegatedWork = (value: unknown, context: SimulationCatchUpValidationContext): value is DelegatedWorkPlaceholder => record(value)
  && ((value.status === 'active' && hasOnlyKeys(value, ['id', 'assigneePersonId', 'status', 'committedAtWorldTime', 'progressIntervals']))
    || (value.status === 'resolved' && hasOnlyKeys(value, ['id', 'assigneePersonId', 'status', 'committedAtWorldTime', 'progressIntervals', 'resolvedAtWorldTime']) && safeInteger(value.resolvedAtWorldTime) && Number(value.resolvedAtWorldTime) >= Number(value.committedAtWorldTime)))
  && validId(value.id)
  && validId(value.assigneePersonId)
  && context.personIds.includes(value.assigneePersonId)
  && safeInteger(value.committedAtWorldTime)
  && value.committedAtWorldTime <= context.worldTime
  && safeInteger(value.progressIntervals)

const validCursor = (value: unknown, context: SimulationCatchUpValidationContext, delegatedWorkIds: ReadonlySet<string>): value is SimulationCatchUpCursor => record(value)
  && hasOnlyKeys(value, ['id', 'targetKind', 'targetId', 'tier', 'cadenceMinutes', 'processedThroughWorldTime', 'processedIntervals', 'outcomeToken'])
  && validId(value.id)
  && targetKinds.includes(value.targetKind as SimulationCatchUpTargetKind)
  && validId(value.targetId)
  && value.id === cursorIdFor(value.targetKind as SimulationCatchUpTargetKind, value.targetId)
  && targetExists(context, value.targetKind as SimulationCatchUpTargetKind, value.targetId, delegatedWorkIds)
  && validTier(value.tier)
  && validCadenceMinutes(value.cadenceMinutes)
  && cadenceForTier(value.tier as SimulationCatchUpTier) === value.cadenceMinutes
  && safeInteger(value.processedThroughWorldTime)
  && value.processedThroughWorldTime > 0
  && value.processedThroughWorldTime <= context.worldTime
  && value.processedThroughWorldTime % value.cadenceMinutes === 0
  && safeInteger(value.processedIntervals)
  && value.processedIntervals > 0
  && safeInteger(value.outcomeToken)
  && value.outcomeToken === outcomeTokenFor(context, value.targetKind as SimulationCatchUpTargetKind, value.targetId, value.cadenceMinutes as SimulationCadenceMinutes, value.processedThroughWorldTime)

const causeMatchesRecord = (cause: SimulationOutcomeCause, value: Pick<SimulationCatchUpRecord, 'targetKind' | 'targetId' | 'cadenceMinutes' | 'windowStartWorldTime' | 'windowEndWorldTime' | 'dueIntervals'>, context: SimulationCatchUpValidationContext, delegatedWorkIds: ReadonlySet<string>): boolean => {
  const evidence = cause.evidence
  return evidence.source === evidenceSourceFor(value.targetKind)
    && evidence.targetKind === value.targetKind
    && evidence.targetId === value.targetId
    && evidence.cadenceMinutes === value.cadenceMinutes
    && evidence.windowStartWorldTime === value.windowStartWorldTime
    && evidence.windowEndWorldTime === value.windowEndWorldTime
    && evidence.dueIntervals === value.dueIntervals
    && targetExists(context, evidence.targetKind, evidence.targetId, delegatedWorkIds)
}

const validCatchUpRecord = (value: unknown, context: SimulationCatchUpValidationContext, delegatedWorkIds: ReadonlySet<string>): value is SimulationCatchUpRecord => record(value)
  && hasOnlyKeys(value, ['id', 'targetKind', 'targetId', 'tier', 'cadenceMinutes', 'windowStartWorldTime', 'windowEndWorldTime', 'dueIntervals', 'outcomeToken', 'outcomeDetail', 'cause', 'contentSafety'])
  && validId(value.id)
  && targetKinds.includes(value.targetKind as SimulationCatchUpTargetKind)
  && validId(value.targetId)
  && validTier(value.tier)
  && validCadenceMinutes(value.cadenceMinutes)
  && cadenceForTier(value.tier as SimulationCatchUpTier) === value.cadenceMinutes
  && safeInteger(value.windowStartWorldTime)
  && safeInteger(value.windowEndWorldTime)
  && value.windowStartWorldTime > 0
  && value.windowStartWorldTime <= value.windowEndWorldTime
  && value.windowEndWorldTime <= context.worldTime
  && value.windowStartWorldTime % value.cadenceMinutes === 0
  && value.windowEndWorldTime % value.cadenceMinutes === 0
  && safeInteger(value.dueIntervals)
  && value.dueIntervals > 0
  && value.windowStartWorldTime === value.windowEndWorldTime - (value.dueIntervals - 1) * value.cadenceMinutes
  && value.id === recordIdFor(value.targetKind as SimulationCatchUpTargetKind, value.targetId, value.cadenceMinutes as SimulationCadenceMinutes, value.windowEndWorldTime)
  && targetExists(context, value.targetKind as SimulationCatchUpTargetKind, value.targetId, delegatedWorkIds)
  && safeInteger(value.outcomeToken)
  && validOutcomeDetail(value.outcomeDetail)
  && value.outcomeDetail === detailForTier(value.tier as SimulationCatchUpTier)
  && validCauseShape(value.cause)
  && causeMatchesRecord(value.cause, value as unknown as SimulationCatchUpRecord, context, delegatedWorkIds)
  && value.outcomeToken === outcomeTokenFor(context, value.targetKind as SimulationCatchUpTargetKind, value.targetId, value.cadenceMinutes as SimulationCadenceMinutes, value.windowEndWorldTime)
  && auditMedievalContentSafety([{ id: `simulation-catchup:${value.id}`, domain: 'simulation-summary', classification: value.contentSafety }]).status === 'accepted'

/** Pure validation for bounded durable scheduling/provenance state. */
export const validateSimulationCatchUpState = (context: SimulationCatchUpValidationContext, value: unknown): readonly SimulationCatchUpDiagnostic[] => {
  const diagnostics: SimulationCatchUpDiagnostic[] = []
  if (!record(value) || !hasOnlyKeys(value, ['version', 'cursors', 'delegatedWork', 'records', 'contentSafetyAudit'])) return [issue('simulation-catchup', 'simulation-catchup.malformed-state')]
  if (value.version !== SIMULATION_CATCH_UP_CONTRACT_VERSION) diagnostics.push(issue('simulation-catchup', 'simulation-catchup.invalid-version'))
  if (!Array.isArray(value.delegatedWork) || value.delegatedWork.length > SIMULATION_CATCH_UP_LIMITS.delegatedWork) diagnostics.push(issue('simulation-catchup:delegated-work', Array.isArray(value.delegatedWork) && value.delegatedWork.length > SIMULATION_CATCH_UP_LIMITS.delegatedWork ? 'simulation-catchup.budget-exceeded' : 'simulation-catchup.invalid-delegated-work'))
  const delegatedWork = Array.isArray(value.delegatedWork) ? value.delegatedWork : []
  const delegatedWorkIds = new Set(delegatedWork.filter(record).map(item => item.id).filter(validId))
  if (delegatedWorkIds.size !== delegatedWork.length) diagnostics.push(issue('simulation-catchup:delegated-work', 'simulation-catchup.duplicate-id'))
  if (!delegatedWork.every(item => validDelegatedWork(item, context))) diagnostics.push(issue('simulation-catchup:delegated-work', 'simulation-catchup.invalid-delegated-work'))
  if (delegatedWork.some((item, index) => index > 0 && record(item) && record(delegatedWork[index - 1]) && String(delegatedWork[index - 1]!.id) >= String(item.id))) diagnostics.push(issue('simulation-catchup:delegated-work', 'simulation-catchup.noncanonical-order'))

  if (!Array.isArray(value.cursors) || value.cursors.length > SIMULATION_CATCH_UP_LIMITS.cursors) diagnostics.push(issue('simulation-catchup:cursors', Array.isArray(value.cursors) && value.cursors.length > SIMULATION_CATCH_UP_LIMITS.cursors ? 'simulation-catchup.budget-exceeded' : 'simulation-catchup.invalid-cursor'))
  const cursors = Array.isArray(value.cursors) ? value.cursors : []
  if (new Set(cursors.filter(record).map(item => item.id)).size !== cursors.length) diagnostics.push(issue('simulation-catchup:cursors', 'simulation-catchup.duplicate-id'))
  if (!cursors.every(item => validCursor(item, context, delegatedWorkIds))) diagnostics.push(issue('simulation-catchup:cursors', 'simulation-catchup.invalid-cursor'))
  if (cursors.some((item, index) => index > 0 && record(item) && record(cursors[index - 1]) && String(cursors[index - 1]!.id) >= String(item.id))) diagnostics.push(issue('simulation-catchup:cursors', 'simulation-catchup.noncanonical-order'))

  if (!Array.isArray(value.records) || value.records.length > SIMULATION_CATCH_UP_LIMITS.records) diagnostics.push(issue('simulation-catchup:records', Array.isArray(value.records) && value.records.length > SIMULATION_CATCH_UP_LIMITS.records ? 'simulation-catchup.budget-exceeded' : 'simulation-catchup.invalid-record'))
  const records = Array.isArray(value.records) ? value.records : []
  if (new Set(records.filter(record).map(item => item.id)).size !== records.length) diagnostics.push(issue('simulation-catchup:records', 'simulation-catchup.duplicate-id'))
  if (!records.every(item => validCatchUpRecord(item, context, delegatedWorkIds))) diagnostics.push(issue('simulation-catchup:records', 'simulation-catchup.invalid-record'))
  for (const item of records) {
    if (!record(item)) continue
    if (!validCauseEnvelope(item.cause)) { diagnostics.push(issue(validId(item.id) ? item.id : 'simulation-catchup:record', 'simulation-catchup.invalid-cause')); continue }
    if (!validCauseEvidenceShape(item.cause.evidence)) { diagnostics.push(issue(validId(item.id) ? item.id : 'simulation-catchup:record', 'simulation-catchup.invalid-evidence')); continue }
    if (!validCauseShape(item.cause)) { diagnostics.push(issue(validId(item.id) ? item.id : 'simulation-catchup:record', 'simulation-catchup.invalid-cause')); continue }
    if (!validId(item.targetId) || !targetKinds.includes(item.targetKind as SimulationCatchUpTargetKind) || !validCadenceMinutes(item.cadenceMinutes) || !safeInteger(item.windowEndWorldTime)) continue
    if (!causeMatchesRecord(item.cause, item as unknown as SimulationCatchUpRecord, context, delegatedWorkIds)) diagnostics.push(issue(String(item.id), 'simulation-catchup.untraceable-cause'))
    if (safeInteger(item.outcomeToken) && item.outcomeToken !== outcomeTokenFor(context, item.targetKind as SimulationCatchUpTargetKind, String(item.targetId), item.cadenceMinutes as SimulationCadenceMinutes, Number(item.windowEndWorldTime))) diagnostics.push(issue(String(item.id), 'simulation-catchup.invalid-outcome-token'))
  }
  if (records.some((item, index) => index > 0 && record(item) && record(records[index - 1]) && (Number(records[index - 1]!.windowEndWorldTime) > Number(item.windowEndWorldTime) || (records[index - 1]!.windowEndWorldTime === item.windowEndWorldTime && String(records[index - 1]!.id) >= String(item.id))))) diagnostics.push(issue('simulation-catchup:records', 'simulation-catchup.noncanonical-order'))

  const cursorById = new Map(cursors.filter(item => validCursor(item, context, delegatedWorkIds)).map(item => [item.id, item]))
  for (const delegated of delegatedWork.filter(item => validDelegatedWork(item, context))) {
    const cursor = cursorById.get(cursorIdFor('delegated-work', delegated.id))
    if (delegated.status === 'active' && delegated.progressIntervals !== (cursor?.processedIntervals ?? 0)) diagnostics.push(issue(delegated.id, 'simulation-catchup.invalid-delegated-work'))
  }
  try {
    const contentAudit = auditMedievalContentSafety(simulationCatchUpContentRecords({ records: records as SimulationCatchUpRecord[] }))
    if (contentAudit.status === 'rejected') diagnostics.push(...contentAudit.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
    if (contentAudit.status !== 'accepted' || !value.contentSafetyAudit || JSON.stringify(contentAudit) !== JSON.stringify(value.contentSafetyAudit)) diagnostics.push(issue('simulation-catchup:content-safety', 'simulation-catchup.invalid-content-audit'))
  } catch { diagnostics.push(issue('simulation-catchup:content-safety', 'simulation-catchup.invalid-content-audit')) }
  return canonicalIssues(diagnostics)
}

interface CatchUpTarget {
  targetKind: SimulationCatchUpTargetKind
  targetId: string
  tier: SimulationCatchUpTier
  cadence: { kind: 'time-window'; intervalMinutes: SimulationCadenceMinutes; nextEligibleAtWorldTime: number }
}

const targetsFor = (plan: FidelityPlan, state: SimulationCatchUpState): readonly CatchUpTarget[] => {
  const scheduled = <Tier extends SimulationCatchUpTier>(targetKind: SimulationCatchUpTargetKind, targetId: string, tier: Tier, cadence: FidelityCadence): CatchUpTarget | undefined => cadence.kind === 'time-window' ? { targetKind, targetId, tier, cadence } : undefined
  const people = plan.individuals.flatMap(item => {
    const target = scheduled('person', item.personId, item.tier, item.cadence)
    return target === undefined ? [] : [target]
  })
  const markets = plan.places.flatMap(item => {
    const target = scheduled('market', `market:${item.siteId}`, item.tier, item.cadence)
    return target === undefined ? [] : [target]
  })
  const institutions = plan.institutions.flatMap(item => {
    const target = scheduled('institution', item.institutionId, item.tier, item.cadence)
    return target === undefined ? [] : [target]
  })
  const individualById = new Map(plan.individuals.map(item => [item.personId, item]))
  const delegatedWork = state.delegatedWork.filter(item => item.status === 'active').flatMap(item => {
    const assignment = individualById.get(item.assigneePersonId)
    const target = assignment === undefined ? undefined : scheduled('delegated-work', item.id, assignment.tier, assignment.cadence)
    return target === undefined ? [] : [target]
  })
  return [...people, ...markets, ...institutions, ...delegatedWork].sort((left, right) => compare(cursorIdFor(left.targetKind, left.targetId), cursorIdFor(right.targetKind, right.targetId)))
}

const tokenFor = (plan: FidelityPlan, target: CatchUpTarget, windowEndWorldTime: number): number => outcomeTokenFor(plan, target.targetKind, target.targetId, target.cadence.intervalMinutes, windowEndWorldTime)

const validationContextForPlan = (plan: FidelityPlan, worldTime: number): SimulationCatchUpValidationContext => ({
  worldId: plan.worldId,
  creationDigest: plan.creationDigest,
  worldTime,
  personIds: plan.individuals.map(item => item.personId),
  marketIds: plan.places.map(item => `market:${item.siteId}`),
  institutionIds: plan.institutions.map(item => item.institutionId)
})

/**
 * Validates that a persisted cursor is the complete canonical projection for
 * this plan and world minute. This deliberately checks no player action IDs:
 * a 5-minute wait and a 2+3-minute wait must arrive at the same cursors.
 */
export const validateSimulationCatchUpPlanState = (state: SimulationCatchUpState, plan: FidelityPlan, worldTime = plan.worldTime): readonly SimulationCatchUpDiagnostic[] => {
  const context = validationContextForPlan(plan, worldTime)
  const diagnostics = [...validateSimulationCatchUpState(context, state)]
  if (!safeInteger(worldTime)) return canonicalIssues([...diagnostics, issue('simulation-catchup:plan', 'simulation-catchup.invalid-record')])
  const targetByCursorId = new Map(targetsFor(plan, state).map(target => [cursorIdFor(target.targetKind, target.targetId), target]))
  const delegatedById = new Map(state.delegatedWork.map(item => [item.id, item]))
  const cursorById = new Map(state.cursors.map(item => [item.id, item]))
  for (const [id, target] of targetByCursorId) {
    const cursor = cursorById.get(id)
    const cadence = target.cadence.intervalMinutes
    const expectedThroughWorldTime = Math.floor(worldTime / cadence) * cadence
    const delegated = target.targetKind === 'delegated-work' ? delegatedById.get(target.targetId) : undefined
    const expectedIntervals = target.targetKind === 'delegated-work'
      ? Math.max(0, Math.floor(worldTime / cadence) - Math.floor((delegated?.committedAtWorldTime ?? worldTime) / cadence))
      : Math.floor(worldTime / cadence)
    if (expectedIntervals === 0) {
      if (cursor !== undefined) diagnostics.push(issue(id, 'simulation-catchup.invalid-cursor'))
      continue
    }
    if (!cursor
      || cursor.tier !== target.tier
      || cursor.cadenceMinutes !== cadence
      || cursor.processedThroughWorldTime !== expectedThroughWorldTime
      || cursor.processedIntervals !== expectedIntervals
      || cursor.outcomeToken !== tokenFor(plan, target, expectedThroughWorldTime)) diagnostics.push(issue(id, 'simulation-catchup.invalid-cursor'))
  }
  for (const cursor of state.cursors) {
    const target = targetByCursorId.get(cursor.id)
    if (!target || target.tier !== cursor.tier || target.cadence.intervalMinutes !== cursor.cadenceMinutes) diagnostics.push(issue(cursor.id, 'simulation-catchup.invalid-reference'))
  }
  for (const delegated of state.delegatedWork) {
    if (delegated.status !== 'active') continue
    const cursor = cursorById.get(cursorIdFor('delegated-work', delegated.id))
    if (delegated.progressIntervals !== (cursor?.processedIntervals ?? 0)) diagnostics.push(issue(delegated.id, 'simulation-catchup.invalid-delegated-work'))
  }
  return canonicalIssues(diagnostics)
}

/**
 * Returns the projection future domain rules must consume. It intentionally
 * omits bounded observation records, whose count depends on player action
 * partitioning, so equivalent elapsed time has the same projection.
 */
export const simulationCatchUpProjection = (context: Pick<SimulationCatchUpValidationContext, 'worldId' | 'creationDigest' | 'worldTime'>, state: SimulationCatchUpState): SimulationCatchUpProjection => ({
  version: SIMULATION_CATCH_UP_PROJECTION_VERSION,
  worldId: context.worldId,
  creationDigest: context.creationDigest,
  worldTime: context.worldTime,
  cursors: [...structuredClone(state.cursors)].sort((left, right) => compare(left.id, right.id)),
  delegatedWork: [...structuredClone(state.delegatedWork)].sort((left, right) => compare(left.id, right.id))
})

/**
 * Folds crossed cadence windows per target. Work is O(planned targets), never
 * O(elapsed minutes × population); action IDs/timestamps have no role in a
 * generated window identity or token.
 */
export const advanceSimulationCatchUpState = (state: SimulationCatchUpState, plan: FidelityPlan, action: { actionId: string; startedAtWorldTime: number; atWorldTime: number }): SimulationCatchUpTransition => {
  if (!validId(action.actionId) || !safeInteger(action.startedAtWorldTime) || !safeInteger(action.atWorldTime) || action.startedAtWorldTime >= action.atWorldTime) throw new SimulationCatchUpContractError([issue('simulation-catchup:action', 'simulation-catchup.invalid-record')])
  const beforeDiagnostics = validateSimulationCatchUpPlanState(state, plan, action.startedAtWorldTime)
  if (beforeDiagnostics.length) throw new SimulationCatchUpContractError(beforeDiagnostics)

  const cursorById = new Map(state.cursors.map(item => [item.id, item]))
  const records = targetsFor(plan, state).flatMap(target => {
    const cadence = target.cadence.intervalMinutes
    const startIndex = Math.floor(action.startedAtWorldTime / cadence)
    const endIndex = Math.floor(action.atWorldTime / cadence)
    const intervals = endIndex - startIndex
    if (!intervals) return []
    const windowStartWorldTime = (startIndex + 1) * cadence
    const windowEndWorldTime = endIndex * cadence
    const outcomeToken = tokenFor(plan, target, windowEndWorldTime)
    const item: SimulationCatchUpRecord = {
      id: recordIdFor(target.targetKind, target.targetId, cadence, windowEndWorldTime),
      targetKind: target.targetKind,
      targetId: target.targetId,
      tier: target.tier,
      cadenceMinutes: cadence,
      windowStartWorldTime,
      windowEndWorldTime,
      dueIntervals: intervals,
      outcomeToken,
      outcomeDetail: detailForTier(target.tier),
      cause: {
        version: SIMULATION_OUTCOME_CAUSE_VERSION,
        kind: 'fidelity-cadence',
        evidence: {
          source: evidenceSourceFor(target.targetKind),
          targetKind: target.targetKind,
          targetId: target.targetId,
          cadenceMinutes: cadence,
          windowStartWorldTime,
          windowEndWorldTime,
          dueIntervals: intervals
        },
        contentSafety: causeClassification()
      },
      contentSafety: summaryClassification()
    }
    const id = cursorIdFor(item.targetKind, item.targetId)
    const previous = cursorById.get(id)
    cursorById.set(id, {
      id,
      targetKind: item.targetKind,
      targetId: item.targetId,
      tier: item.tier,
      cadenceMinutes: item.cadenceMinutes,
      processedThroughWorldTime: item.windowEndWorldTime,
      processedIntervals: (previous?.processedIntervals ?? 0) + item.dueIntervals,
      outcomeToken: item.outcomeToken
    })
    return [item]
  })
  const progressByTaskId = new Map(records.filter(item => item.targetKind === 'delegated-work').map(item => [item.targetId, item.dueIntervals]))
  const delegatedWork = state.delegatedWork.map(item => ({ ...item, progressIntervals: item.progressIntervals + (progressByTaskId.get(item.id) ?? 0) }))
  const retainedRecords = [...state.records, ...records]
    .sort((left, right) => left.windowEndWorldTime - right.windowEndWorldTime || compare(left.id, right.id))
    .slice(-SIMULATION_CATCH_UP_LIMITS.records)
  const nextWithoutAudit = {
    version: SIMULATION_CATCH_UP_CONTRACT_VERSION,
    cursors: [...cursorById.values()].sort((left, right) => compare(left.id, right.id)),
    delegatedWork: [...delegatedWork].sort((left, right) => compare(left.id, right.id)),
    records: retainedRecords
  } satisfies Omit<SimulationCatchUpState, 'contentSafetyAudit'>
  const next: SimulationCatchUpState = { ...nextWithoutAudit, contentSafetyAudit: audit(nextWithoutAudit) }
  const diagnostics = validateSimulationCatchUpPlanState(next, plan, action.atWorldTime)
  if (diagnostics.length) throw new SimulationCatchUpContractError(diagnostics)
  return { state: next, records }
}

export class SimulationCatchUpContractError extends Error {
  constructor(readonly diagnostics: readonly SimulationCatchUpDiagnostic[]) {
    super(`simulation catch-up rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'SimulationCatchUpContractError'
  }
}
