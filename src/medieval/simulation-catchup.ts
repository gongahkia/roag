import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { FidelityCadence, FidelityIndividualTier, FidelityInstitutionTier, FidelityPlaceTier, FidelityPlan } from './fidelity'
import { SeededRng } from './rng'

/**
 * The catch-up state is deliberately a kernel rather than an economy or task
 * system. It records which already-known records have processed their assigned
 * fidelity cadence. Later slices own the concrete market and delegation rules.
 */
export const SIMULATION_CATCH_UP_CONTRACT_VERSION = 1 as const

export const SIMULATION_CATCH_UP_LIMITS = {
  cursors: 96,
  delegatedWork: 24,
  records: 96
} as const

export type SimulationCatchUpTargetKind = 'person' | 'market' | 'institution' | 'delegated-work'
export type SimulationCatchUpTier = FidelityIndividualTier | FidelityPlaceTier | FidelityInstitutionTier

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
  worldTime: number
  personIds: readonly string[]
  marketIds: readonly string[]
  institutionIds: readonly string[]
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
const validTier = (value: unknown): value is SimulationCatchUpTier => tiers.includes(value as SimulationCatchUpTier)

export const simulationCatchUpContentRecords = (state: Pick<SimulationCatchUpState, 'records'>): readonly ClassifiedMedievalContent[] => state.records.map(item => ({
  id: `simulation-catchup:${item.id}`,
  domain: 'simulation-summary',
  classification: item.contentSafety
}))

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
  && hasOnlyKeys(value, ['id', 'actionId', 'targetKind', 'targetId', 'tier', 'startedAtWorldTime', 'processedAtWorldTime', 'dueIntervals', 'outcomeToken', 'contentSafety'])
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
  if (records.some((item, index) => index > 0 && record(item) && record(records[index - 1]) && (Number(records[index - 1]!.processedAtWorldTime) > Number(item.processedAtWorldTime) || (records[index - 1]!.processedAtWorldTime === item.processedAtWorldTime && String(records[index - 1]!.id) >= String(item.id))))) diagnostics.push(issue('simulation-catchup:records', 'simulation-catchup.noncanonical-order'))
  try {
    if (!auditMedievalContentSafety(simulationCatchUpContentRecords({ records: records as SimulationCatchUpRecord[] })).status || !value.contentSafetyAudit || JSON.stringify(auditMedievalContentSafety(simulationCatchUpContentRecords({ records: records as SimulationCatchUpRecord[] }))) !== JSON.stringify(value.contentSafetyAudit)) diagnostics.push(issue('simulation-catchup:content-safety', 'simulation-catchup.invalid-content-audit'))
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

const tokenFor = (plan: FidelityPlan, target: CatchUpTarget, processedAtWorldTime: number): number => new SeededRng(`jomon-simulation-catchup-v${SIMULATION_CATCH_UP_CONTRACT_VERSION}|${plan.creationDigest}|${plan.worldId}|${target.targetKind}|${target.targetId}|${processedAtWorldTime}`).integer(1_000_000)

/**
 * Processes each target at most once per player action. Elapsed tiers retain
 * how many cadence boundaries were crossed, so long actions are bounded while
 * preserving an inspectable deterministic result.
 */
export const advanceSimulationCatchUpState = (state: SimulationCatchUpState, plan: FidelityPlan, action: { actionId: string; startedAtWorldTime: number; atWorldTime: number }): SimulationCatchUpTransition => {
  const context: SimulationCatchUpValidationContext = {
    worldTime: action.atWorldTime,
    personIds: plan.individuals.map(item => item.personId),
    marketIds: plan.places.map(item => `market:${item.siteId}`),
    institutionIds: plan.institutions.map(item => item.institutionId)
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
