import { auditMedievalContentSafety, contentSafetyAuditMatches, validateMedievalContentSafety, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { SeededRng } from './rng'

/** This contract is intentionally independent of rendering and browser time. */
export const MEDIEVAL_TEMPORAL_CONTRACT_VERSION = 1 as const
export const MEDIEVAL_TIME_UNIT = 'minute' as const

/** Family bounds only; later gameplay decides the exact duration from its own state. */
export const TEMPORAL_ACTION_DURATION_LIMITS = {
  movement: { minimumMinutes: 1, maximumMinutes: 60 },
  wait: { minimumMinutes: 1, maximumMinutes: 1_440 },
  travel: { minimumMinutes: 1, maximumMinutes: 10_080 },
  work: { minimumMinutes: 1, maximumMinutes: 1_440 },
  rest: { minimumMinutes: 1, maximumMinutes: 1_440 },
  'delegated-task-commitment': { minimumMinutes: 1, maximumMinutes: 720 },
  'delegated-task-resolution': { minimumMinutes: 1, maximumMinutes: 720 }
} as const

export const TEMPORAL_EVENT_PRIORITIES = ['urgent', 'ordinary', 'deferred'] as const
export const TEMPORAL_RANDOM_STREAMS = ['action', 'event-resolution'] as const
export const TEMPORAL_LIMITS = {
  pendingEvents: 32,
  causalRecords: 256,
  identityLength: 96,
  randomUpperExclusive: 10_000
} as const

export type TemporalActionKind = keyof typeof TEMPORAL_ACTION_DURATION_LIMITS
export type TemporalEventPriority = typeof TEMPORAL_EVENT_PRIORITIES[number]
export type TemporalRandomStream = typeof TEMPORAL_RANDOM_STREAMS[number]
export type PureUiTemporalCommandKind = 'inspect' | 'settings-opened' | 'settings-closed' | 'route-changed' | 'browser-idle' | 'browser-paused' | 'browser-reloaded' | 'ui-event'

/** Immutable values copied from validated creation provenance, never browser state. */
export interface TemporalProvenance {
  version: typeof MEDIEVAL_TEMPORAL_CONTRACT_VERSION
  worldId: string
  creationDigest: string
  seed: string
}

export interface TemporalEventPayload {
  kind: 'action-resolution'
  sourceActionId: string
  creationDigest: string
}

export interface TemporalEventRequest {
  id: string
  dueAtWorldTime: number
  priority: TemporalEventPriority
  payload: TemporalEventPayload
  contentSafety: MedievalContentSafetyClassification
}

export interface PendingTemporalEvent extends TemporalEventRequest {
  /** Assigned after canonical request sorting, rather than input insertion order. */
  sequence: number
}

export interface TimeBearingTemporalAction {
  id: string
  kind: TemporalActionKind
  durationMinutes: number
  contentSafety: MedievalContentSafetyClassification
  events?: readonly TemporalEventRequest[]
}

/** Pure commands exist so accidental UI calls fail closed instead of advancing zero time. */
export interface PureUiTemporalCommand {
  kind: PureUiTemporalCommandKind
}

export type TemporalCommand = TimeBearingTemporalAction | PureUiTemporalCommand

export interface TemporalActionCausalRecord {
  kind: 'action-completed'
  id: string
  sequence: number
  actionSequence: number
  actionId: string
  actionKind: TemporalActionKind
  durationMinutes: number
  startedAtWorldTime: number
  atWorldTime: number
  scheduledEventIds: readonly string[]
  detail: string
  contentSafety: MedievalContentSafetyClassification
}

export interface TemporalDueEventCausalRecord {
  kind: 'event-resolved'
  id: string
  sequence: number
  atWorldTime: number
  event: PendingTemporalEvent
  rngStream: string
  randomValue: number
  detail: string
  contentSafety: MedievalContentSafetyClassification
}

export type TemporalCausalRecord = TemporalActionCausalRecord | TemporalDueEventCausalRecord

export interface DueTemporalEventResult {
  eventId: string
  dueAtWorldTime: number
  priority: TemporalEventPriority
  sequence: number
  randomValue: number
  causalRecord: TemporalDueEventCausalRecord
}

export interface MedievalTemporalState {
  version: typeof MEDIEVAL_TEMPORAL_CONTRACT_VERSION
  timeUnit: typeof MEDIEVAL_TIME_UNIT
  provenance: TemporalProvenance
  worldTime: number
  actionSequence: number
  nextEventSequence: number
  pendingEvents: readonly PendingTemporalEvent[]
  causalRecords: readonly TemporalCausalRecord[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

export type TemporalValidationDiagnosticCode =
  | 'temporal.malformed-state'
  | 'temporal.invalid-contract-version'
  | 'temporal.invalid-provenance'
  | 'temporal.provenance-mismatch'
  | 'temporal.invalid-time-unit'
  | 'temporal.invalid-world-time'
  | 'temporal.invalid-action-sequence'
  | 'temporal.invalid-event-sequence'
  | 'temporal.malformed-action'
  | 'temporal.pure-command'
  | 'temporal.invalid-action-id'
  | 'temporal.duplicate-action-id'
  | 'temporal.invalid-action-kind'
  | 'temporal.invalid-action-duration'
  | 'temporal.action-time-overflow'
  | 'temporal.malformed-event'
  | 'temporal.invalid-event-id'
  | 'temporal.duplicate-event-id'
  | 'temporal.invalid-event-due-time'
  | 'temporal.past-event'
  | 'temporal.invalid-event-priority'
  | 'temporal.invalid-event-payload'
  | 'temporal.invalid-event-provenance'
  | 'temporal.invalid-event-order'
  | 'temporal.queue-limit'
  | 'temporal.causal-record-limit'
  | 'temporal.malformed-causal-record'
  | 'temporal.invalid-causal-order'
  | 'temporal.invalid-content-audit'
  | MedievalContentSafetyDiagnosticCode

export interface TemporalValidationIssue {
  code: TemporalValidationDiagnosticCode
  recordId: string
}

export interface AcceptedTemporalValidation {
  version: typeof MEDIEVAL_TEMPORAL_CONTRACT_VERSION
  status: 'accepted'
  diagnostics: readonly []
}

export interface RejectedTemporalValidation {
  version: typeof MEDIEVAL_TEMPORAL_CONTRACT_VERSION
  status: 'rejected'
  diagnostics: readonly TemporalValidationIssue[]
}

export type TemporalValidation = AcceptedTemporalValidation | RejectedTemporalValidation

export interface TemporalTransition {
  state: MedievalTemporalState
  action: TemporalActionCausalRecord
  dueEvents: readonly DueTemporalEventResult[]
}

export class TemporalContractError extends Error {
  constructor(readonly diagnostics: readonly TemporalValidationIssue[]) {
    super(`temporal contract rejected: ${diagnostics.map(item => item.code).join(', ')}`)
    this.name = 'TemporalContractError'
  }
}

const isRecord = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const isSafeNonNegativeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const comparison = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const sortedExpected = [...expected].sort()
  return keys.length === sortedExpected.length && keys.every((key, index) => key === sortedExpected[index])
}
const includes = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const validIdentity = (value: unknown): value is string => typeof value === 'string' && value.length > 0 && value.length <= TEMPORAL_LIMITS.identityLength && /^[a-z0-9][a-z0-9:-]*$/.test(value)
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const priorityRank = (priority: TemporalEventPriority): number => TEMPORAL_EVENT_PRIORITIES.indexOf(priority)
const temporalIssue = (recordId: string, code: TemporalValidationDiagnosticCode): TemporalValidationIssue => ({ recordId, code })

const canonicalIssues = (issues: readonly TemporalValidationIssue[]): readonly TemporalValidationIssue[] => [...new Map(issues.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => comparison(left.recordId, right.recordId) || comparison(left.code, right.code))

const eventOrder = (left: Pick<PendingTemporalEvent, 'dueAtWorldTime' | 'priority' | 'id' | 'sequence'>, right: Pick<PendingTemporalEvent, 'dueAtWorldTime' | 'priority' | 'id' | 'sequence'>): number => left.dueAtWorldTime - right.dueAtWorldTime || priorityRank(left.priority) - priorityRank(right.priority) || comparison(left.id, right.id) || left.sequence - right.sequence

const requestedEventOrder = (left: TemporalEventRequest, right: TemporalEventRequest): number => left.dueAtWorldTime - right.dueAtWorldTime || priorityRank(left.priority) - priorityRank(right.priority) || comparison(left.id, right.id)

const actionDetail = (action: Pick<TimeBearingTemporalAction, 'id' | 'kind' | 'durationMinutes'>): string => `Action ${action.kind} (${action.id}) advanced ${action.durationMinutes} minute(s).`
const eventDetail = (event: Pick<PendingTemporalEvent, 'id'>, atWorldTime: number): string => `Scheduled event ${event.id} resolved at minute ${atWorldTime}.`

const eventContentRecord = (event: Pick<PendingTemporalEvent, 'id' | 'contentSafety'>): ClassifiedMedievalContent => ({ id: `temporal:event:${event.id}`, domain: 'event', classification: event.contentSafety })
const actionContentRecord = (action: Pick<TemporalActionCausalRecord, 'actionId' | 'contentSafety'>): ClassifiedMedievalContent => ({ id: `temporal:action:${action.actionId}`, domain: 'event', classification: action.contentSafety })

/** All pending and resolved displayable scheduler data remains policy-audited. */
export const temporalContentRecords = (state: Pick<MedievalTemporalState, 'pendingEvents' | 'causalRecords'>): readonly ClassifiedMedievalContent[] => [
  ...state.causalRecords.map(record => record.kind === 'action-completed' ? actionContentRecord(record) : eventContentRecord(record.event)),
  ...state.pendingEvents.map(eventContentRecord)
]

const validProvenance = (value: unknown): value is TemporalProvenance => isRecord(value) && hasOnlyKeys(value, ['version', 'worldId', 'creationDigest', 'seed']) && value.version === MEDIEVAL_TEMPORAL_CONTRACT_VERSION && validIdentity(value.worldId) && validIdentity(value.creationDigest) && typeof value.seed === 'string' && value.seed.length > 0

const validateContent = (recordId: string, classification: unknown): readonly TemporalValidationIssue[] => {
  const validation = validateMedievalContentSafety([{ id: recordId, domain: 'event', classification }])
  return validation.status === 'accepted' ? [] : validation.diagnostics.map(item => temporalIssue(item.contentId, item.code))
}

const validPriority = (value: unknown): value is TemporalEventPriority => includes(TEMPORAL_EVENT_PRIORITIES, value)
const validActionKind = (value: unknown): value is TemporalActionKind => Object.hasOwn(TEMPORAL_ACTION_DURATION_LIMITS, String(value))
const validRandomStream = (value: unknown): value is TemporalRandomStream => includes(TEMPORAL_RANDOM_STREAMS, value)

const validDuration = (kind: TemporalActionKind, durationMinutes: unknown): durationMinutes is number => {
  const limits = TEMPORAL_ACTION_DURATION_LIMITS[kind]
  return isSafeNonNegativeInteger(durationMinutes) && durationMinutes >= limits.minimumMinutes && durationMinutes <= limits.maximumMinutes
}

const validPayload = (payload: unknown, actionId: string | undefined, provenance: TemporalProvenance): payload is TemporalEventPayload => isRecord(payload)
  && hasOnlyKeys(payload, ['kind', 'sourceActionId', 'creationDigest'])
  && payload.kind === 'action-resolution'
  && validIdentity(payload.sourceActionId)
  && (actionId === undefined || payload.sourceActionId === actionId)
  && payload.creationDigest === provenance.creationDigest

const validEventStructure = (value: unknown, provenance: TemporalProvenance, actionId?: string): value is PendingTemporalEvent => isRecord(value)
  && hasOnlyKeys(value, ['id', 'dueAtWorldTime', 'priority', 'payload', 'contentSafety', 'sequence'])
  && validIdentity(value.id)
  && isSafeNonNegativeInteger(value.dueAtWorldTime)
  && validPriority(value.priority)
  && validPayload(value.payload, actionId, provenance)
  && isSafeNonNegativeInteger(value.sequence)

const validActionRecordStructure = (value: unknown): value is TemporalActionCausalRecord => isRecord(value)
  && hasOnlyKeys(value, ['kind', 'id', 'sequence', 'actionSequence', 'actionId', 'actionKind', 'durationMinutes', 'startedAtWorldTime', 'atWorldTime', 'scheduledEventIds', 'detail', 'contentSafety'])
  && value.kind === 'action-completed'
  && validIdentity(value.id)
  && isSafeNonNegativeInteger(value.sequence)
  && isSafeNonNegativeInteger(value.actionSequence)
  && validIdentity(value.actionId)
  && validActionKind(value.actionKind)
  && validDuration(value.actionKind, value.durationMinutes)
  && isSafeNonNegativeInteger(value.startedAtWorldTime)
  && isSafeNonNegativeInteger(value.atWorldTime)
  && Array.isArray(value.scheduledEventIds) && value.scheduledEventIds.every(validIdentity)
  && typeof value.detail === 'string'

const validEventRecordStructure = (value: unknown, provenance: TemporalProvenance): value is TemporalDueEventCausalRecord => isRecord(value)
  && hasOnlyKeys(value, ['kind', 'id', 'sequence', 'atWorldTime', 'event', 'rngStream', 'randomValue', 'detail', 'contentSafety'])
  && value.kind === 'event-resolved'
  && validIdentity(value.id)
  && isSafeNonNegativeInteger(value.sequence)
  && isSafeNonNegativeInteger(value.atWorldTime)
  && validEventStructure(value.event, provenance)
  && typeof value.rngStream === 'string'
  && isSafeNonNegativeInteger(value.randomValue) && value.randomValue < TEMPORAL_LIMITS.randomUpperExclusive
  && typeof value.detail === 'string'

const stateLike = (value: unknown): value is MedievalTemporalState => isRecord(value)
  && hasOnlyKeys(value, ['version', 'timeUnit', 'provenance', 'worldTime', 'actionSequence', 'nextEventSequence', 'pendingEvents', 'causalRecords', 'contentSafetyAudit'])

const stateIssue = (issues: TemporalValidationIssue[], recordId: string, code: TemporalValidationDiagnosticCode): void => { issues.push(temporalIssue(recordId, code)) }

/**
 * Validates a persisted scheduler state without consulting browser clocks or
 * mutating it. Expected provenance binds the state to one immutable world.
 */
export const validateMedievalTemporalState = (value: unknown, expectedProvenance?: TemporalProvenance): TemporalValidation => {
  const issues: TemporalValidationIssue[] = []
  if (!stateLike(value)) return { version: MEDIEVAL_TEMPORAL_CONTRACT_VERSION, status: 'rejected', diagnostics: [temporalIssue('temporal:state', 'temporal.malformed-state')] }

  if (value.version !== MEDIEVAL_TEMPORAL_CONTRACT_VERSION) stateIssue(issues, 'temporal:state', 'temporal.invalid-contract-version')
  if (value.timeUnit !== MEDIEVAL_TIME_UNIT) stateIssue(issues, 'temporal:state', 'temporal.invalid-time-unit')
  if (!validProvenance(value.provenance)) stateIssue(issues, 'temporal:provenance', 'temporal.invalid-provenance')
  else if (expectedProvenance !== undefined && !same(value.provenance, expectedProvenance)) stateIssue(issues, 'temporal:provenance', 'temporal.provenance-mismatch')
  if (!isSafeNonNegativeInteger(value.worldTime)) stateIssue(issues, 'temporal:state', 'temporal.invalid-world-time')
  if (!isSafeNonNegativeInteger(value.actionSequence)) stateIssue(issues, 'temporal:state', 'temporal.invalid-action-sequence')
  if (!isSafeNonNegativeInteger(value.nextEventSequence)) stateIssue(issues, 'temporal:state', 'temporal.invalid-event-sequence')
  if (!Array.isArray(value.pendingEvents) || value.pendingEvents.length > TEMPORAL_LIMITS.pendingEvents) stateIssue(issues, 'temporal:pending', Array.isArray(value.pendingEvents) ? 'temporal.queue-limit' : 'temporal.malformed-event')
  if (!Array.isArray(value.causalRecords) || value.causalRecords.length > TEMPORAL_LIMITS.causalRecords) stateIssue(issues, 'temporal:causal', Array.isArray(value.causalRecords) ? 'temporal.causal-record-limit' : 'temporal.malformed-causal-record')
  if (!isRecord(value.provenance) || !Array.isArray(value.pendingEvents) || !Array.isArray(value.causalRecords)) return { version: MEDIEVAL_TEMPORAL_CONTRACT_VERSION, status: 'rejected', diagnostics: canonicalIssues(issues) }

  const provenance = value.provenance as TemporalProvenance
  const pendingEvents = value.pendingEvents as unknown[]
  const causalRecords = value.causalRecords as unknown[]
  const eventsById = new Map<string, PendingTemporalEvent>()
  const resolvedEventIds = new Set<string>()
  const actionRecords = new Map<string, TemporalActionCausalRecord>()
  const actionScheduledIds = new Set<string>()
  const actionTimeline: TemporalActionCausalRecord[] = []
  const resolvedRecords: TemporalDueEventCausalRecord[] = []
  let elapsed = 0
  let expectedActionSequence = 0
  let expectedCausalSequence = 0
  let previousActionAt = 0
  let dueGroup: TemporalDueEventCausalRecord[] = []

  const validateDueGroup = (): void => {
    if (!dueGroup.length) return
    const ordered = [...dueGroup].sort((left, right) => eventOrder(left.event, right.event))
    if (!same(ordered.map(record => record.id), dueGroup.map(record => record.id))) stateIssue(issues, 'temporal:causal', 'temporal.invalid-causal-order')
    dueGroup = []
  }

  for (const candidate of causalRecords) {
    if (validActionRecordStructure(candidate)) {
      validateDueGroup()
      const action = candidate
      if (action.sequence !== expectedCausalSequence++) stateIssue(issues, action.id, 'temporal.invalid-causal-order')
      if (action.id !== `action:${action.actionId}` || action.actionSequence !== ++expectedActionSequence || action.startedAtWorldTime !== elapsed || action.atWorldTime !== elapsed + action.durationMinutes || action.atWorldTime > Number.MAX_SAFE_INTEGER || action.detail !== actionDetail({ id: action.actionId, kind: action.actionKind, durationMinutes: action.durationMinutes })) stateIssue(issues, action.id, 'temporal.invalid-causal-order')
      if (actionRecords.has(action.actionId)) stateIssue(issues, action.actionId, 'temporal.duplicate-action-id')
      actionRecords.set(action.actionId, action)
      actionTimeline.push(action)
      if (new Set(action.scheduledEventIds).size !== action.scheduledEventIds.length) stateIssue(issues, action.id, 'temporal.duplicate-event-id')
      for (const eventId of action.scheduledEventIds) {
        if (actionScheduledIds.has(eventId)) stateIssue(issues, eventId, 'temporal.duplicate-event-id')
        actionScheduledIds.add(eventId)
      }
      issues.push(...validateContent(`temporal:action:${action.actionId}`, action.contentSafety))
      elapsed = action.atWorldTime
      previousActionAt = action.atWorldTime
      continue
    }
    if (validEventRecordStructure(candidate, provenance)) {
      const resolved = candidate
      if (resolved.sequence !== expectedCausalSequence++) stateIssue(issues, resolved.id, 'temporal.invalid-causal-order')
      if (resolved.id !== `event:${resolved.event.id}` || resolved.atWorldTime !== previousActionAt || resolved.event.dueAtWorldTime > resolved.atWorldTime || resolved.detail !== eventDetail(resolved.event, resolved.atWorldTime) || !same(resolved.contentSafety, resolved.event.contentSafety)) stateIssue(issues, resolved.id, 'temporal.invalid-causal-order')
      if (resolvedEventIds.has(resolved.event.id) || eventsById.has(resolved.event.id)) stateIssue(issues, resolved.event.id, 'temporal.duplicate-event-id')
      if (!actionRecords.has(resolved.event.payload.sourceActionId)) stateIssue(issues, resolved.id, 'temporal.invalid-causal-order')
      resolvedEventIds.add(resolved.event.id)
      eventsById.set(resolved.event.id, resolved.event)
      resolvedRecords.push(resolved)
      if (resolved.rngStream !== temporalStreamSeedFor(provenance, 'event-resolution', resolved.event.id) || resolved.randomValue !== temporalRandomInteger(provenance, 'event-resolution', resolved.event.id, TEMPORAL_LIMITS.randomUpperExclusive)) stateIssue(issues, resolved.id, 'temporal.invalid-causal-order')
      dueGroup.push(resolved)
      issues.push(...validateContent(`temporal:event:${resolved.event.id}`, resolved.event.contentSafety))
      continue
    }
    stateIssue(issues, 'temporal:causal', 'temporal.malformed-causal-record')
    expectedCausalSequence++
  }
  validateDueGroup()

  for (const candidate of pendingEvents) {
    if (!validEventStructure(candidate, provenance)) {
      stateIssue(issues, 'temporal:pending', 'temporal.malformed-event')
      continue
    }
    if (candidate.dueAtWorldTime <= value.worldTime) stateIssue(issues, candidate.id, 'temporal.invalid-event-due-time')
    if (eventsById.has(candidate.id)) stateIssue(issues, candidate.id, 'temporal.duplicate-event-id')
    eventsById.set(candidate.id, candidate)
    issues.push(...validateContent(`temporal:event:${candidate.id}`, candidate.contentSafety))
  }

  const orderedPending = [...pendingEvents].sort((left, right) => eventOrder(left as PendingTemporalEvent, right as PendingTemporalEvent))
  if (!same(pendingEvents, orderedPending)) stateIssue(issues, 'temporal:pending', 'temporal.invalid-event-order')
  if (elapsed !== value.worldTime || expectedActionSequence !== value.actionSequence) stateIssue(issues, 'temporal:state', 'temporal.invalid-causal-order')

  const allEvents = [...eventsById.values()]
  const eventSequences = allEvents.map(event => event.sequence).sort((left, right) => left - right)
  if (eventSequences.length !== value.nextEventSequence || eventSequences.some((sequence, index) => sequence !== index)) stateIssue(issues, 'temporal:state', 'temporal.invalid-event-sequence')

  for (const event of allEvents) {
    const sourceAction = actionRecords.get(event.payload.sourceActionId)
    if (!sourceAction || !sourceAction.scheduledEventIds.includes(event.id) || event.dueAtWorldTime < sourceAction.startedAtWorldTime) stateIssue(issues, event.id, 'temporal.invalid-event-payload')
  }
  for (const resolved of resolvedRecords) {
    const firstActionMakingEventDue = actionTimeline.find(action => action.atWorldTime >= resolved.event.dueAtWorldTime)
    if (!firstActionMakingEventDue || resolved.atWorldTime !== firstActionMakingEventDue.atWorldTime) stateIssue(issues, resolved.id, 'temporal.invalid-causal-order')
  }
  for (const action of actionRecords.values()) {
    const scheduled = action.scheduledEventIds.map(id => eventsById.get(id)).filter((event): event is PendingTemporalEvent => event !== undefined)
    if (scheduled.length !== action.scheduledEventIds.length || !same(scheduled.map(event => event.id), [...scheduled].sort(requestedEventOrder).map(event => event.id))) stateIssue(issues, action.id, 'temporal.invalid-event-order')
  }
  if (!contentSafetyAuditMatches(temporalContentRecords({ pendingEvents: pendingEvents as PendingTemporalEvent[], causalRecords: causalRecords as TemporalCausalRecord[] }), value.contentSafetyAudit)) stateIssue(issues, 'temporal:state', 'temporal.invalid-content-audit')

  const diagnostics = canonicalIssues(issues)
  return diagnostics.length ? { version: MEDIEVAL_TEMPORAL_CONTRACT_VERSION, status: 'rejected', diagnostics } : { version: MEDIEVAL_TEMPORAL_CONTRACT_VERSION, status: 'accepted', diagnostics: [] }
}

export const isMedievalTemporalState = (value: unknown, expectedProvenance?: TemporalProvenance): value is MedievalTemporalState => validateMedievalTemporalState(value, expectedProvenance).status === 'accepted'

const checkedProvenance = (provenance: TemporalProvenance): TemporalProvenance => {
  if (!validProvenance(provenance)) throw new TemporalContractError([temporalIssue('temporal:provenance', 'temporal.invalid-provenance')])
  return structuredClone(provenance)
}

export const temporalStreamSeedFor = (provenance: TemporalProvenance, stream: TemporalRandomStream, identity: string): string => {
  const checked = checkedProvenance(provenance)
  if (!validRandomStream(stream) || !validIdentity(identity)) throw new TemporalContractError([temporalIssue('temporal:stream', 'temporal.invalid-provenance')])
  return `jomon:temporal:${MEDIEVAL_TEMPORAL_CONTRACT_VERSION}:${checked.worldId}:${checked.creationDigest}:${checked.seed}:${stream}:${identity}`
}

/** A fresh stream per identity makes reloads and unrelated insertion order irrelevant. */
export const temporalRandomInteger = (provenance: TemporalProvenance, stream: TemporalRandomStream, identity: string, maximumExclusive: number): number => {
  if (!Number.isSafeInteger(maximumExclusive) || maximumExclusive <= 0) throw new TemporalContractError([temporalIssue('temporal:stream', 'temporal.invalid-provenance')])
  return new SeededRng(temporalStreamSeedFor(provenance, stream, identity)).integer(maximumExclusive)
}

/** Shared overflow guard for every future time-bearing action implementation. */
export const temporalTimeAfterAction = (worldTime: number, durationMinutes: number): number => {
  if (!isSafeNonNegativeInteger(worldTime) || !isSafeNonNegativeInteger(durationMinutes) || durationMinutes <= 0) throw new TemporalContractError([temporalIssue('temporal:time', 'temporal.invalid-world-time')])
  if (worldTime + durationMinutes > Number.MAX_SAFE_INTEGER) throw new TemporalContractError([temporalIssue('temporal:time', 'temporal.action-time-overflow')])
  return worldTime + durationMinutes
}

const acceptedAudit = (records: readonly ClassifiedMedievalContent[]): MedievalContentSafetyAudit => {
  const audit = auditMedievalContentSafety(records)
  if (audit.status === 'rejected') throw new TemporalContractError(audit.diagnostics.map(item => temporalIssue(item.contentId, item.code)))
  return audit
}

export const createMedievalTemporalState = (provenance: TemporalProvenance): MedievalTemporalState => {
  const checked = checkedProvenance(provenance)
  return {
    version: MEDIEVAL_TEMPORAL_CONTRACT_VERSION,
    timeUnit: MEDIEVAL_TIME_UNIT,
    provenance: checked,
    worldTime: 0,
    actionSequence: 0,
    nextEventSequence: 0,
    pendingEvents: [],
    causalRecords: [],
    contentSafetyAudit: acceptedAudit([])
  }
}

const validateAction = (state: MedievalTemporalState, command: unknown): readonly TemporalValidationIssue[] => {
  const issues: TemporalValidationIssue[] = []
  if (!isRecord(command)) return [temporalIssue('temporal:action', 'temporal.malformed-action')]
  if (includes(['inspect', 'settings-opened', 'settings-closed', 'route-changed', 'browser-idle', 'browser-paused', 'browser-reloaded', 'ui-event'] as const, command.kind)) return [temporalIssue('temporal:action', 'temporal.pure-command')]
  if (!hasOnlyKeys(command, ['id', 'kind', 'durationMinutes', 'contentSafety', 'events']) && !hasOnlyKeys(command, ['id', 'kind', 'durationMinutes', 'contentSafety'])) return [temporalIssue('temporal:action', 'temporal.malformed-action')]
  if (!validIdentity(command.id)) stateIssue(issues, 'temporal:action', 'temporal.invalid-action-id')
  if (!validActionKind(command.kind)) stateIssue(issues, typeof command.id === 'string' ? command.id : 'temporal:action', 'temporal.invalid-action-kind')
  if (validActionKind(command.kind) && !validDuration(command.kind, command.durationMinutes)) stateIssue(issues, typeof command.id === 'string' ? command.id : 'temporal:action', 'temporal.invalid-action-duration')
  if (validIdentity(command.id) && state.causalRecords.some(record => record.kind === 'action-completed' && record.actionId === command.id)) stateIssue(issues, command.id, 'temporal.duplicate-action-id')
  if (validIdentity(command.id)) issues.push(...validateContent(`temporal:action:${command.id}`, command.contentSafety))
  const events = command.events
  if (events !== undefined && !Array.isArray(events)) stateIssue(issues, typeof command.id === 'string' ? command.id : 'temporal:action', 'temporal.malformed-event')
  if (Array.isArray(events)) {
    if (state.pendingEvents.length + events.length > TEMPORAL_LIMITS.pendingEvents) stateIssue(issues, typeof command.id === 'string' ? command.id : 'temporal:action', 'temporal.queue-limit')
    const knownIds = new Set([
      ...state.pendingEvents.map(event => event.id),
      ...state.causalRecords.filter((record): record is TemporalDueEventCausalRecord => record.kind === 'event-resolved').map(record => record.event.id)
    ])
    for (const candidate of events) {
      const eventId = isRecord(candidate) && typeof candidate.id === 'string' ? candidate.id : 'temporal:event'
      if (!isRecord(candidate) || !hasOnlyKeys(candidate, ['id', 'dueAtWorldTime', 'priority', 'payload', 'contentSafety'])) {
        stateIssue(issues, eventId, 'temporal.malformed-event')
        continue
      }
      if (!validIdentity(candidate.id)) stateIssue(issues, eventId, 'temporal.invalid-event-id')
      else if (knownIds.has(candidate.id)) stateIssue(issues, candidate.id, 'temporal.duplicate-event-id')
      if (validIdentity(candidate.id)) knownIds.add(candidate.id)
      if (!isSafeNonNegativeInteger(candidate.dueAtWorldTime)) stateIssue(issues, eventId, 'temporal.invalid-event-due-time')
      else if (candidate.dueAtWorldTime < state.worldTime) stateIssue(issues, eventId, 'temporal.past-event')
      if (!validPriority(candidate.priority)) stateIssue(issues, eventId, 'temporal.invalid-event-priority')
      if (!validIdentity(command.id) || !validPayload(candidate.payload, command.id, state.provenance)) stateIssue(issues, eventId, isRecord(candidate.payload) && candidate.payload.creationDigest !== state.provenance.creationDigest ? 'temporal.invalid-event-provenance' : 'temporal.invalid-event-payload')
      if (validIdentity(candidate.id)) issues.push(...validateContent(`temporal:event:${candidate.id}`, candidate.contentSafety))
    }
  }
  if (validActionKind(command.kind) && validDuration(command.kind, command.durationMinutes) && state.worldTime + command.durationMinutes > Number.MAX_SAFE_INTEGER) stateIssue(issues, typeof command.id === 'string' ? command.id : 'temporal:action', 'temporal.action-time-overflow')
  if (state.actionSequence >= Number.MAX_SAFE_INTEGER) stateIssue(issues, 'temporal:state', 'temporal.action-time-overflow')
  return canonicalIssues(issues)
}

/**
 * The only temporal mutation point. It advances exactly once for a valid
 * state-changing action, then resolves all due events in the documented order.
 */
export const advanceMedievalTemporalState = (state: MedievalTemporalState, command: TemporalCommand | unknown): TemporalTransition => {
  const current = validateMedievalTemporalState(state)
  if (current.status === 'rejected') throw new TemporalContractError(current.diagnostics)
  const actionIssues = validateAction(state, command)
  if (actionIssues.length) throw new TemporalContractError(actionIssues)
  const action = command as TimeBearingTemporalAction
  const requests = [...(action.events ?? [])].sort(requestedEventOrder)
  if (state.nextEventSequence + requests.length > Number.MAX_SAFE_INTEGER) throw new TemporalContractError([temporalIssue(action.id, 'temporal.action-time-overflow')])
  const scheduled = requests.map((request, index): PendingTemporalEvent => ({ ...structuredClone(request), sequence: state.nextEventSequence + index }))
  const atWorldTime = temporalTimeAfterAction(state.worldTime, action.durationMinutes)
  const allPending = [...state.pendingEvents.map(event => structuredClone(event)), ...scheduled].sort(eventOrder)
  const due = allPending.filter(event => event.dueAtWorldTime <= atWorldTime)
  const pendingEvents = allPending.filter(event => event.dueAtWorldTime > atWorldTime)
  if (state.causalRecords.length + 1 + due.length > TEMPORAL_LIMITS.causalRecords) throw new TemporalContractError([temporalIssue(action.id, 'temporal.causal-record-limit')])

  const actionRecord: TemporalActionCausalRecord = {
    kind: 'action-completed',
    id: `action:${action.id}`,
    sequence: state.causalRecords.length,
    actionSequence: state.actionSequence + 1,
    actionId: action.id,
    actionKind: action.kind,
    durationMinutes: action.durationMinutes,
    startedAtWorldTime: state.worldTime,
    atWorldTime,
    scheduledEventIds: scheduled.map(event => event.id),
    detail: actionDetail(action),
    contentSafety: structuredClone(action.contentSafety)
  }
  const dueRecords = due.map((event, index): TemporalDueEventCausalRecord => {
    const rngStream = temporalStreamSeedFor(state.provenance, 'event-resolution', event.id)
    const randomValue = temporalRandomInteger(state.provenance, 'event-resolution', event.id, TEMPORAL_LIMITS.randomUpperExclusive)
    return {
      kind: 'event-resolved',
      id: `event:${event.id}`,
      sequence: state.causalRecords.length + 1 + index,
      atWorldTime,
      event,
      rngStream,
      randomValue,
      detail: eventDetail(event, atWorldTime),
      contentSafety: structuredClone(event.contentSafety)
    }
  })
  const nextWithoutAudit = {
    version: MEDIEVAL_TEMPORAL_CONTRACT_VERSION,
    timeUnit: MEDIEVAL_TIME_UNIT,
    provenance: structuredClone(state.provenance),
    worldTime: atWorldTime,
    actionSequence: state.actionSequence + 1,
    nextEventSequence: state.nextEventSequence + scheduled.length,
    pendingEvents,
    causalRecords: [...state.causalRecords.map(record => structuredClone(record)), actionRecord, ...dueRecords]
  } satisfies Omit<MedievalTemporalState, 'contentSafetyAudit'>
  const next: MedievalTemporalState = { ...nextWithoutAudit, contentSafetyAudit: acceptedAudit(temporalContentRecords(nextWithoutAudit)) }
  const validation = validateMedievalTemporalState(next)
  if (validation.status === 'rejected') throw new TemporalContractError(validation.diagnostics)
  return {
    state: next,
    action: actionRecord,
    dueEvents: dueRecords.map(record => ({ eventId: record.event.id, dueAtWorldTime: record.event.dueAtWorldTime, priority: record.event.priority, sequence: record.event.sequence, randomValue: record.randomValue, causalRecord: record }))
  }
}
