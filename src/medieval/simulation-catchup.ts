import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { FidelityCadence, FidelityIndividualTier, FidelityInstitutionTier, FidelityPlaceTier, FidelityPlan } from './fidelity'
import { SeededRng } from './rng'

/**
 * The catch-up state is deliberately a kernel rather than an economy or task
 * system. It records which already-known records have processed their assigned
 * fidelity cadence. Later slices own the concrete market and delegation rules.
 */
export const SIMULATION_CATCH_UP_CONTRACT_VERSION = 2 as const
export const SIMULATION_OUTCOME_CAUSE_VERSION = 1 as const

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

export interface DelegatedWorkPlaceholder {
  id: string
  assigneePersonId: string
  status: 'active' | 'resolved'
  committedAtWorldTime: number
  progressIntervals: number
  resolvedAtWorldTime?: number
}

export interface SimulationCatchUpCursor {
  id: string
  targetKind: SimulationCatchUpTargetKind
  targetId: string
  tier: SimulationCatchUpTier
  processedAtWorldTime: number
  processedIntervals: number
  lastActionId: string
  outcomeToken: number
}

/** A closed link to facts that a future surface can inspect without hidden prose. */
export interface SimulationOutcomeCauseEvidence {
  source: SimulationOutcomeEvidenceSource
  targetKind: SimulationCatchUpTargetKind
  targetId: string
  actionId: string
  startedAtWorldTime: number
  processedAtWorldTime: number
  dueIntervals: number
}

/**
 * Outcome causes carry no free text. They only point to a known world record
 * (or the typed delegated-work fact), its accepted action, and cadence facts.
 */
export interface SimulationOutcomeCause {
  version: typeof SIMULATION_OUTCOME_CAUSE_VERSION
  kind: SimulationOutcomeCauseKind
  evidence: SimulationOutcomeCauseEvidence
  contentSafety: MedievalContentSafetyClassification
}

export interface SimulationCatchUpRecord {
  id: string
  actionId: string
  targetKind: SimulationCatchUpTargetKind
  targetId: string
  tier: SimulationCatchUpTier
  startedAtWorldTime: number
  processedAtWorldTime: number
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
  actionEvidence: readonly SimulationCatchUpActionEvidence[]
}

/** Minimal projection of an accepted temporal action; it contains no prose. */
export interface SimulationCatchUpActionEvidence {
  id: string
  startedAtWorldTime: number
  atWorldTime: number
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
const cursorIdFor = (targetKind: SimulationCatchUpTargetKind, targetId: string): string => `catch-up:${targetKind}:${targetId}`
const summaryClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('simulation-summary', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const causeClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('data', ['adult-labour', 'navigation'], 'not-applicable', ['simulation-summary'])
const validTier = (value: unknown): value is SimulationCatchUpTier => tiers.includes(value as SimulationCatchUpTier)
const validOutcomeDetail = (value: unknown): value is SimulationOutcomeDetail => value === 'detailed' || value === 'summary'
const detailForTier = (tier: SimulationCatchUpTier): SimulationOutcomeDetail => tier === 'distant-individual-summary' || tier === 'distant-settlement-summary' || tier === 'distant-institution-summary' ? 'summary' : 'detailed'
const evidenceSourceFor = (targetKind: SimulationCatchUpTargetKind): SimulationOutcomeEvidenceSource => targetKind === 'delegated-work' ? 'delegated-work-fact' : 'known-world-record'

export const simulationCatchUpContentRecords = (state: Pick<SimulationCatchUpState, 'records'>): readonly ClassifiedMedievalContent[] => state.records.flatMap(item => [
  {
    id: `simulation-catchup:${item.id}`,
    domain: 'simulation-summary' as const,
    classification: item.contentSafety
  },
  {
    id: `simulation-cause:${item.id}`,
    domain: 'data' as const,
    classification: item.cause?.contentSafety as MedievalContentSafetyClassification
  }
])

const audit = (state: Pick<SimulationCatchUpState, 'records'>): MedievalContentSafetyAudit => {
  const result = auditMedievalContentSafety(simulationCatchUpContentRecords(state))
  if (result.status === 'rejected') throw new Error(`simulation catch-up content rejected: ${result.diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
  return result
}

export const createSimulationCatchUpState = (): SimulationCatchUpState => {
  const state = {
    version: SIMULATION_CATCH_UP_CONTRACT_VERSION,
    cursors: [],
    delegatedWork: [],
    records: []
  } satisfies Omit<SimulationCatchUpState, 'contentSafetyAudit'>
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

const actionEvidenceFor = (context: SimulationCatchUpValidationContext, actionId: string): SimulationCatchUpActionEvidence | undefined => context.actionEvidence.find(item => item.id === actionId)

const validActionEvidence = (value: unknown): value is SimulationCatchUpActionEvidence => record(value)
  && hasOnlyKeys(value, ['id', 'startedAtWorldTime', 'atWorldTime'])
  && validId(value.id)
  && safeInteger(value.startedAtWorldTime)
  && safeInteger(value.atWorldTime)
  && value.startedAtWorldTime < value.atWorldTime

const validCauseEvidenceShape = (value: unknown): value is SimulationOutcomeCauseEvidence => record(value)
  && hasOnlyKeys(value, ['source', 'targetKind', 'targetId', 'actionId', 'startedAtWorldTime', 'processedAtWorldTime', 'dueIntervals'])
  && (value.source === 'known-world-record' || value.source === 'delegated-work-fact')
  && targetKinds.includes(value.targetKind as SimulationCatchUpTargetKind)
  && validId(value.targetId)
  && validId(value.actionId)
  && safeInteger(value.startedAtWorldTime)
  && safeInteger(value.processedAtWorldTime)
  && value.startedAtWorldTime < value.processedAtWorldTime
  && safeInteger(value.dueIntervals)
  && value.dueIntervals > 0

const validCauseShape = (value: unknown): value is SimulationOutcomeCause => record(value)
  && hasOnlyKeys(value, ['version', 'kind', 'evidence', 'contentSafety'])
  && value.version === SIMULATION_OUTCOME_CAUSE_VERSION
  && value.kind === 'fidelity-cadence'
  && validCauseEvidenceShape(value.evidence)
  && auditMedievalContentSafety([{ id: 'simulation-cause:validation', domain: 'data', classification: value.contentSafety }]).status === 'accepted'

const causeMatchesRecord = (cause: SimulationOutcomeCause, value: Pick<SimulationCatchUpRecord, 'actionId' | 'targetKind' | 'targetId' | 'startedAtWorldTime' | 'processedAtWorldTime' | 'dueIntervals'>, context: SimulationCatchUpValidationContext, delegatedWorkIds: ReadonlySet<string>): boolean => {
  const evidence = cause.evidence
  const action = actionEvidenceFor(context, evidence.actionId)
  return context.actionEvidence.every(validActionEvidence)
    && evidence.source === evidenceSourceFor(value.targetKind)
    && evidence.targetKind === value.targetKind
    && evidence.targetId === value.targetId
    && evidence.actionId === value.actionId
    && evidence.startedAtWorldTime === value.startedAtWorldTime
    && evidence.processedAtWorldTime === value.processedAtWorldTime
    && evidence.dueIntervals === value.dueIntervals
    && targetExists(context, evidence.targetKind, evidence.targetId, delegatedWorkIds)
    && action !== undefined
    && action.startedAtWorldTime === evidence.startedAtWorldTime
    && action.atWorldTime === evidence.processedAtWorldTime
}

const outcomeTokenFor = (context: Pick<SimulationCatchUpValidationContext, 'worldId' | 'creationDigest'>, targetKind: SimulationCatchUpTargetKind, targetId: string, processedAtWorldTime: number): number => new SeededRng(`jomon-simulation-catchup-v${SIMULATION_CATCH_UP_CONTRACT_VERSION}|${context.creationDigest}|${context.worldId}|${targetKind}|${targetId}|${processedAtWorldTime}`).integer(1_000_000)

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
  && hasOnlyKeys(value, ['id', 'targetKind', 'targetId', 'tier', 'processedAtWorldTime', 'processedIntervals', 'lastActionId', 'outcomeToken'])
  && validId(value.id)
  && targetKinds.includes(value.targetKind as SimulationCatchUpTargetKind)
  && validId(value.targetId)
  && value.id === cursorIdFor(value.targetKind as SimulationCatchUpTargetKind, value.targetId)
  && targetExists(context, value.targetKind as SimulationCatchUpTargetKind, value.targetId, delegatedWorkIds)
  && validTier(value.tier)
  && safeInteger(value.processedAtWorldTime)
  && value.processedAtWorldTime <= context.worldTime
  && safeInteger(value.processedIntervals)
  && validId(value.lastActionId)
  && safeInteger(value.outcomeToken)

const validCatchUpRecord = (value: unknown, context: SimulationCatchUpValidationContext, delegatedWorkIds: ReadonlySet<string>): value is SimulationCatchUpRecord => record(value)
  && hasOnlyKeys(value, ['id', 'actionId', 'targetKind', 'targetId', 'tier', 'startedAtWorldTime', 'processedAtWorldTime', 'dueIntervals', 'outcomeToken', 'outcomeDetail', 'cause', 'contentSafety'])
  && validId(value.id)
  && validId(value.actionId)
  && targetKinds.includes(value.targetKind as SimulationCatchUpTargetKind)
  && validId(value.targetId)
  && value.id === `catch-up:${value.actionId}:${value.targetKind}:${value.targetId}`
  && targetExists(context, value.targetKind as SimulationCatchUpTargetKind, value.targetId, delegatedWorkIds)
  && validTier(value.tier)
  && safeInteger(value.startedAtWorldTime)
  && safeInteger(value.processedAtWorldTime)
  && value.startedAtWorldTime < value.processedAtWorldTime
  && value.processedAtWorldTime <= context.worldTime
  && safeInteger(value.dueIntervals)
  && value.dueIntervals > 0
  && safeInteger(value.outcomeToken)
  && validOutcomeDetail(value.outcomeDetail)
  && value.outcomeDetail === detailForTier(value.tier as SimulationCatchUpTier)
  && validCauseShape(value.cause)
  && causeMatchesRecord(value.cause, value as unknown as SimulationCatchUpRecord, context, delegatedWorkIds)
  && value.outcomeToken === outcomeTokenFor(context, value.targetKind as SimulationCatchUpTargetKind, value.targetId, value.processedAtWorldTime)
  && auditMedievalContentSafety([{ id: `simulation-catchup:${value.id}`, domain: 'simulation-summary', classification: value.contentSafety }]).status === 'accepted'

/** Pure validation for the bounded durable catch-up cursor and outcome window. */
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
    if (!validCauseShape(item.cause)) {
      diagnostics.push(issue(validId(item.id) ? item.id : 'simulation-catchup:record', 'simulation-catchup.invalid-cause'))
      continue
    }
    if (!validCauseEvidenceShape(item.cause.evidence)) {
      diagnostics.push(issue(validId(item.id) ? item.id : 'simulation-catchup:record', 'simulation-catchup.invalid-evidence'))
      continue
    }
    if (!validId(item.actionId) || !targetKinds.includes(item.targetKind as SimulationCatchUpTargetKind) || !validId(item.targetId) || !safeInteger(item.startedAtWorldTime) || !safeInteger(item.processedAtWorldTime) || !safeInteger(item.dueIntervals)) continue
    if (!causeMatchesRecord(item.cause, item as unknown as SimulationCatchUpRecord, context, delegatedWorkIds)) diagnostics.push(issue(String(item.id), 'simulation-catchup.untraceable-cause'))
    if (safeInteger(item.outcomeToken) && item.outcomeToken !== outcomeTokenFor(context, item.targetKind as SimulationCatchUpTargetKind, String(item.targetId), Number(item.processedAtWorldTime))) diagnostics.push(issue(String(item.id), 'simulation-catchup.invalid-outcome-token'))
  }
  if (records.some((item, index) => index > 0 && record(item) && record(records[index - 1]) && (Number(records[index - 1]!.processedAtWorldTime) > Number(item.processedAtWorldTime) || (records[index - 1]!.processedAtWorldTime === item.processedAtWorldTime && String(records[index - 1]!.id) >= String(item.id))))) diagnostics.push(issue('simulation-catchup:records', 'simulation-catchup.noncanonical-order'))
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
  cadence: FidelityCadence
}

const dueIntervals = (cadence: FidelityCadence, startedAtWorldTime: number, processedAtWorldTime: number): number => {
  if (cadence.kind === 'every-time-bearing-action') return 1
  if (cadence.kind === 'not-scheduled') return 0
  return Math.floor(processedAtWorldTime / cadence.intervalMinutes) - Math.floor(startedAtWorldTime / cadence.intervalMinutes)
}

const targetsFor = (plan: FidelityPlan, state: SimulationCatchUpState): readonly CatchUpTarget[] => {
  const people = plan.individuals.map(item => ({ targetKind: 'person' as const, targetId: item.personId, tier: item.tier, cadence: item.cadence }))
  const markets = plan.places.map(item => ({ targetKind: 'market' as const, targetId: `market:${item.siteId}`, tier: item.tier, cadence: item.cadence }))
  const institutions = plan.institutions.map(item => ({ targetKind: 'institution' as const, targetId: item.institutionId, tier: item.tier, cadence: item.cadence }))
  const individualById = new Map(plan.individuals.map(item => [item.personId, item]))
  const delegatedWork = state.delegatedWork.filter(item => item.status === 'active').flatMap(item => {
    const assignment = individualById.get(item.assigneePersonId)
    return assignment === undefined ? [] : [{ targetKind: 'delegated-work' as const, targetId: item.id, tier: assignment.tier, cadence: assignment.cadence }]
  })
  return [...people, ...markets, ...institutions, ...delegatedWork].sort((left, right) => compare(cursorIdFor(left.targetKind, left.targetId), cursorIdFor(right.targetKind, right.targetId)))
}

const tokenFor = (plan: FidelityPlan, target: CatchUpTarget, processedAtWorldTime: number): number => outcomeTokenFor(plan, target.targetKind, target.targetId, processedAtWorldTime)

/**
 * Processes each target at most once per player action. Elapsed tiers retain
 * how many cadence boundaries were crossed, so long actions are bounded while
 * preserving an inspectable deterministic result.
 */
export const advanceSimulationCatchUpState = (state: SimulationCatchUpState, plan: FidelityPlan, action: { actionId: string; startedAtWorldTime: number; atWorldTime: number }): SimulationCatchUpTransition => {
  const currentActionEvidence = {
    id: action.actionId,
    startedAtWorldTime: action.startedAtWorldTime,
    atWorldTime: action.atWorldTime
  } satisfies SimulationCatchUpActionEvidence
  const existingActionEvidence = plan.actionEvidence.find(item => item.id === action.actionId)
  if (existingActionEvidence !== undefined && (existingActionEvidence.startedAtWorldTime !== action.startedAtWorldTime || existingActionEvidence.atWorldTime !== action.atWorldTime)) throw new SimulationCatchUpContractError([issue('simulation-catchup:action', 'simulation-catchup.invalid-record')])
  const actionEvidence = existingActionEvidence === undefined
    ? [...plan.actionEvidence, currentActionEvidence]
    : plan.actionEvidence
  const context: SimulationCatchUpValidationContext = {
    worldId: plan.worldId,
    creationDigest: plan.creationDigest,
    worldTime: action.atWorldTime,
    personIds: plan.individuals.map(item => item.personId),
    marketIds: plan.places.map(item => `market:${item.siteId}`),
    institutionIds: plan.institutions.map(item => item.institutionId),
    actionEvidence
  }
  const beforeDiagnostics = validateSimulationCatchUpState({ ...context, worldTime: action.startedAtWorldTime }, state)
  if (beforeDiagnostics.length) throw new SimulationCatchUpContractError(beforeDiagnostics)
  if (!validId(action.actionId) || !safeInteger(action.startedAtWorldTime) || !safeInteger(action.atWorldTime) || action.startedAtWorldTime >= action.atWorldTime) throw new SimulationCatchUpContractError([issue('simulation-catchup:action', 'simulation-catchup.invalid-record')])

  const records = targetsFor(plan, state).flatMap(target => {
    const intervals = dueIntervals(target.cadence, action.startedAtWorldTime, action.atWorldTime)
    if (!intervals) return []
    const outcomeToken = tokenFor(plan, target, action.atWorldTime)
    return [{
      id: `catch-up:${action.actionId}:${target.targetKind}:${target.targetId}`,
      actionId: action.actionId,
      targetKind: target.targetKind,
      targetId: target.targetId,
      tier: target.tier,
      startedAtWorldTime: action.startedAtWorldTime,
      processedAtWorldTime: action.atWorldTime,
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
          actionId: action.actionId,
          startedAtWorldTime: action.startedAtWorldTime,
          processedAtWorldTime: action.atWorldTime,
          dueIntervals: intervals
        },
        contentSafety: causeClassification()
      },
      contentSafety: summaryClassification()
    } satisfies SimulationCatchUpRecord]
  })
  const cursorById = new Map(state.cursors.map(item => [item.id, item]))
  for (const item of records) {
    const id = cursorIdFor(item.targetKind, item.targetId)
    const previous = cursorById.get(id)
    cursorById.set(id, {
      id,
      targetKind: item.targetKind,
      targetId: item.targetId,
      tier: item.tier,
      processedAtWorldTime: item.processedAtWorldTime,
      processedIntervals: (previous?.processedIntervals ?? 0) + item.dueIntervals,
      lastActionId: item.actionId,
      outcomeToken: item.outcomeToken
    })
  }
  const progressByTaskId = new Map(records.filter(item => item.targetKind === 'delegated-work').map(item => [item.targetId, item.dueIntervals]))
  const delegatedWork = state.delegatedWork.map(item => ({ ...item, progressIntervals: item.progressIntervals + (progressByTaskId.get(item.id) ?? 0) }))
  const retainedRecords = [...state.records, ...records].sort((left, right) => left.processedAtWorldTime - right.processedAtWorldTime || compare(left.id, right.id)).slice(-SIMULATION_CATCH_UP_LIMITS.records)
  const nextWithoutAudit = {
    version: SIMULATION_CATCH_UP_CONTRACT_VERSION,
    cursors: [...cursorById.values()].sort((left, right) => compare(left.id, right.id)),
    delegatedWork: [...delegatedWork].sort((left, right) => compare(left.id, right.id)),
    records: retainedRecords
  } satisfies Omit<SimulationCatchUpState, 'contentSafetyAudit'>
  const next: SimulationCatchUpState = { ...nextWithoutAudit, contentSafetyAudit: audit(nextWithoutAudit) }
  const diagnostics = validateSimulationCatchUpState(context, next)
  if (diagnostics.length) throw new SimulationCatchUpContractError(diagnostics)
  return { state: next, records }
}

export class SimulationCatchUpContractError extends Error {
  constructor(readonly diagnostics: readonly SimulationCatchUpDiagnostic[]) {
    super(`simulation catch-up rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'SimulationCatchUpContractError'
  }
}
