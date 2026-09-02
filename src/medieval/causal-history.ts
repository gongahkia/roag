import { auditMedievalContentSafety, contentSafetyAuditMatches, classifyMedievalContent, validateMedievalContentSafety, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { hashSeed } from './rng'
import type { SimulationCatchUpState } from './simulation-catchup'
import type { MedievalTemporalState, TimeBearingTemporalAction } from './temporal'
import type { DurableJomonGrowthEvidence, WorldEraState } from './world-era'
import { isDelegationInterruptionInput, isDelegationOfferInput, type DelegationInterruptionInput, type DelegationOfferInput, type DelegationState } from './delegation'
import type { PersistentPersonRecord } from './persistent-person'

/**
 * The global mutable command journal. Domain-local temporal and catch-up
 * evidence remains in its owning subdomain; this records only reducer inputs.
 */
export const CAUSAL_HISTORY_CONTRACT_VERSION = 2 as const
export const CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION = 2 as const
export const CAUSAL_HISTORY_CHECKPOINT_VERSION = 2 as const
export const CAUSAL_HISTORY_SEGMENT_VERSION = 2 as const

export const CAUSAL_HISTORY_LIMITS = {
  retainedCommands: 8,
  checkpoints: 1,
  compactedSegments: 8,
  identityLength: 160
} as const

export type CausalCommandKind = 'initial-courier-selected' | 'time-bearing-action' | 'durable-jomon-growth' | 'delegation-offered' | 'delegation-interrupted'

export interface CausalHistoryContext {
  worldId: string
  creationDigest: string
}

export interface CausalHistoryCourierProjection {
  version: 1
  initialCourierId?: string
}

/** People become journalled only because delegation now mutates their work/memory state. */
export interface CausalHistoryPeopleProjection {
  version: 4
  records: readonly PersistentPersonRecord[]
}

/** The intentionally narrow mutable result of currently journalled commands. */
export interface CausalReplayProjection {
  version: typeof CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION
  courier: CausalHistoryCourierProjection
  people: CausalHistoryPeopleProjection
  temporal: MedievalTemporalState
  simulation: SimulationCatchUpState
  era: WorldEraState
  delegation: DelegationState
}

export interface InitialCourierSelectedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'initial-courier-selected'
  payload: { courierId: string }
  contentSafety: MedievalContentSafetyClassification
}

export interface TimeBearingActionCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'time-bearing-action'
  payload: { action: TimeBearingTemporalAction }
  contentSafety: MedievalContentSafetyClassification
}

export interface DurableJomonGrowthCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'durable-jomon-growth'
  payload: { evidence: DurableJomonGrowthEvidence }
  contentSafety: MedievalContentSafetyClassification
}

export interface DelegationOfferedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'delegation-offered'
  payload: { offer: DelegationOfferInput }
  contentSafety: MedievalContentSafetyClassification
}

export interface DelegationInterruptedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'delegation-interrupted'
  payload: { interruption: DelegationInterruptionInput }
  contentSafety: MedievalContentSafetyClassification
}

export type CausalCommandEvent = InitialCourierSelectedCommand | TimeBearingActionCommand | DurableJomonGrowthCommand | DelegationOfferedCommand | DelegationInterruptedCommand

export interface CausalHistoryCheckpoint {
  version: typeof CAUSAL_HISTORY_CHECKPOINT_VERSION
  id: string
  token: string
  sequence: number
  atWorldTime: number
  provenanceDigest: string
  stateDigest: string
  projection: CausalReplayProjection
}

export interface CausalHistoryCommandKindCounts {
  initialCourierSelected: number
  timeBearingAction: number
  durableJomonGrowth: number
  delegationOffered: number
  delegationInterrupted: number
}

/** A typed accounting record for commands no longer retained in the replay tail. */
export interface CausalHistoryCompactedSegment {
  version: typeof CAUSAL_HISTORY_SEGMENT_VERSION
  id: string
  token: string
  sequenceStart: number
  sequenceEnd: number
  worldTimeStart: number
  worldTimeEnd: number
  commandKinds: CausalHistoryCommandKindCounts
  provenanceDigest: string
  stateDigest: string
}

export interface CausalHistoryState {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  checkpoint: CausalHistoryCheckpoint
  tail: readonly CausalCommandEvent[]
  compactedSegments: readonly CausalHistoryCompactedSegment[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

export type CausalHistoryDiagnosticCode =
  | 'causal-history.malformed-state'
  | 'causal-history.invalid-version'
  | 'causal-history.invalid-context'
  | 'causal-history.invalid-checkpoint'
  | 'causal-history.invalid-checkpoint-token'
  | 'causal-history.invalid-command'
  | 'causal-history.invalid-command-token'
  | 'causal-history.invalid-command-payload'
  | 'causal-history.invalid-command-order'
  | 'causal-history.duplicate-command-id'
  | 'causal-history.command-limit'
  | 'causal-history.invalid-segment'
  | 'causal-history.invalid-segment-token'
  | 'causal-history.invalid-segment-order'
  | 'causal-history.segment-limit'
  | 'causal-history.invalid-content-audit'
  | 'causal-history.replay-failed'
  | 'causal-history.replay-mismatch'
  | MedievalContentSafetyDiagnosticCode

export interface CausalHistoryDiagnostic {
  code: CausalHistoryDiagnosticCode
  recordId: string
}

export class CausalHistoryContractError extends Error {
  constructor(readonly diagnostics: readonly CausalHistoryDiagnostic[]) {
    super(`causal history rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'CausalHistoryContractError'
  }
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const validId = (value: unknown): value is string => typeof value === 'string' && value.length > 0 && value.length <= CAUSAL_HISTORY_LIMITS.identityLength && /^[a-z][a-z0-9:._-]*$/i.test(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const issue = (recordId: string, code: CausalHistoryDiagnosticCode): CausalHistoryDiagnostic => ({ recordId, code })
const canonicalIssues = (issues: readonly CausalHistoryDiagnostic[]): readonly CausalHistoryDiagnostic[] => [...new Map(issues.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))

/** Small canonical serialization for deterministic IDs and equality, never a save format. */
export const canonicalCausalJson = (value: unknown): string => {
  if (value === null) return 'null'
  if (typeof value === 'string' || typeof value === 'boolean') return JSON.stringify(value)
  if (typeof value === 'number') {
    if (!Number.isFinite(value)) throw new Error('causal history requires finite values')
    return JSON.stringify(Object.is(value, -0) ? 0 : value)
  }
  if (Array.isArray(value)) return `[${value.map(canonicalCausalJson).join(',')}]`
  if (!record(value) || (Object.getPrototypeOf(value) !== Object.prototype && Object.getPrototypeOf(value) !== null)) throw new Error('causal history requires serializable records')
  return `{${Object.keys(value).sort().map(key => `${JSON.stringify(key)}:${canonicalCausalJson(value[key])}`).join(',')}}`
}

const same = (left: unknown, right: unknown): boolean => {
  try { return canonicalCausalJson(left) === canonicalCausalJson(right) } catch { return false }
}

export const causalDigestFor = (scope: string, value: unknown): string => {
  const source = `${scope}|${canonicalCausalJson(value)}`
  return `${hashSeed(source).toString(36)}-${hashSeed([...source].reverse().join(''), 0x9e3779b9).toString(36)}`
}

const validContext = (context: CausalHistoryContext): boolean => validId(context.worldId) && typeof context.creationDigest === 'string' && context.creationDigest.length > 0 && context.creationDigest.length <= CAUSAL_HISTORY_LIMITS.identityLength
const commandCountFor = (events: readonly CausalCommandEvent[]): CausalHistoryCommandKindCounts => ({
  initialCourierSelected: events.filter(event => event.kind === 'initial-courier-selected').length,
  timeBearingAction: events.filter(event => event.kind === 'time-bearing-action').length,
  durableJomonGrowth: events.filter(event => event.kind === 'durable-jomon-growth').length,
  delegationOffered: events.filter(event => event.kind === 'delegation-offered').length,
  delegationInterrupted: events.filter(event => event.kind === 'delegation-interrupted').length
})
const commandClassification = (kind: CausalCommandKind, payload: InitialCourierSelectedCommand['payload'] | TimeBearingActionCommand['payload'] | DurableJomonGrowthCommand['payload'] | DelegationOfferedCommand['payload'] | DelegationInterruptedCommand['payload']): MedievalContentSafetyClassification => {
  if (kind === 'time-bearing-action') return structuredClone((payload as TimeBearingActionCommand['payload']).action.contentSafety)
  // The offer remains a classified contract in its payload; the journal entry
  // itself is an event, so it must carry an event-domain classification too.
  if (kind === 'delegation-offered') return classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
  if (kind === 'delegation-interrupted') return classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
  return kind === 'initial-courier-selected'
    ? classifyMedievalContent('event', ['civil-life', 'travel'], 'adults-only', ['data'])
    : classifyMedievalContent('event', ['adult-labour', 'navigation'], 'adults-only', ['data'])
}
const commandPayloadIsShaped = (kind: CausalCommandKind, value: unknown): boolean => {
  if (!record(value)) return false
  if (kind === 'initial-courier-selected') return hasOnlyKeys(value, ['courierId']) && validId(value.courierId)
  if (kind === 'time-bearing-action') return hasOnlyKeys(value, ['action']) && record(value.action) && typeof value.action.id === 'string' && typeof value.action.kind === 'string' && typeof value.action.durationMinutes === 'number' && value.action.contentSafety !== undefined
  if (kind === 'durable-jomon-growth') return hasOnlyKeys(value, ['evidence']) && record(value.evidence) && typeof value.evidence.id === 'string' && typeof value.evidence.kind === 'string' && value.evidence.source !== undefined && typeof value.evidence.atWorldTime === 'number'
  if (kind === 'delegation-offered') return hasOnlyKeys(value, ['offer']) && isDelegationOfferInput(value.offer)
  return hasOnlyKeys(value, ['interruption']) && isDelegationInterruptionInput(value.interruption)
}
const commandIdFor = (context: CausalHistoryContext, sequence: number, kind: CausalCommandKind, payload: unknown): string => `causal-command:${sequence}:${causalDigestFor('causal-command-id', { worldId: context.worldId, creationDigest: context.creationDigest, sequence, kind, payload })}`
const commandTokenFor = (context: CausalHistoryContext, sequence: number, kind: CausalCommandKind, payload: unknown): string => causalDigestFor('causal-command-token', { worldId: context.worldId, creationDigest: context.creationDigest, sequence, kind, payload })
const checkpointIdFor = (context: CausalHistoryContext, sequence: number, stateDigest: string): string => `causal-checkpoint:${sequence}:${causalDigestFor('causal-checkpoint-id', { worldId: context.worldId, creationDigest: context.creationDigest, sequence, stateDigest })}`
const checkpointTokenFor = (context: CausalHistoryContext, sequence: number, atWorldTime: number, stateDigest: string): string => causalDigestFor('causal-checkpoint-token', { worldId: context.worldId, creationDigest: context.creationDigest, sequence, atWorldTime, stateDigest })
const segmentIdFor = (context: CausalHistoryContext, start: number, end: number, stateDigest: string): string => `causal-segment:${start}-${end}:${causalDigestFor('causal-segment-id', { worldId: context.worldId, creationDigest: context.creationDigest, start, end, stateDigest })}`
const segmentTokenFor = (context: CausalHistoryContext, summary: Omit<CausalHistoryCompactedSegment, 'id' | 'token'>): string => causalDigestFor('causal-segment-token', { worldId: context.worldId, creationDigest: context.creationDigest, ...summary })

const projectionShape = (value: unknown): value is CausalReplayProjection => record(value)
  && hasOnlyKeys(value, ['version', 'courier', 'people', 'temporal', 'simulation', 'era', 'delegation'])
  && value.version === CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION
  && record(value.courier)
  && (hasOnlyKeys(value.courier, ['version']) || hasOnlyKeys(value.courier, ['version', 'initialCourierId']))
  && value.courier.version === 1
  && (value.courier.initialCourierId === undefined || validId(value.courier.initialCourierId))
  && record(value.people)
  && hasOnlyKeys(value.people, ['version', 'records'])
  && value.people.version === 4
  && Array.isArray(value.people.records)
  && record(value.temporal)
  && record(value.simulation)
  && record(value.era)
  && record(value.delegation)

export const causalReplayProjection = (value: Pick<CausalReplayProjection, 'courier' | 'people' | 'temporal' | 'simulation' | 'era' | 'delegation'>): CausalReplayProjection => ({
  version: CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION,
  courier: structuredClone(value.courier),
  people: structuredClone(value.people),
  temporal: structuredClone(value.temporal),
  simulation: structuredClone(value.simulation),
  era: structuredClone(value.era),
  delegation: structuredClone(value.delegation)
})

export const causalReplayProjectionDigest = (projection: CausalReplayProjection): string => causalDigestFor('causal-replay-projection', projection)

const checkpointFor = (context: CausalHistoryContext, sequence: number, projection: CausalReplayProjection): CausalHistoryCheckpoint => {
  const stateDigest = causalReplayProjectionDigest(projection)
  const atWorldTime = projection.temporal.worldTime
  const id = checkpointIdFor(context, sequence, stateDigest)
  return {
    version: CAUSAL_HISTORY_CHECKPOINT_VERSION,
    id,
    token: checkpointTokenFor(context, sequence, atWorldTime, stateDigest),
    sequence,
    atWorldTime,
    provenanceDigest: context.creationDigest,
    stateDigest,
    projection: structuredClone(projection)
  }
}

const contentRecordsFor = (events: readonly CausalCommandEvent[]): readonly ClassifiedMedievalContent[] => events.map(event => ({
  id: `causal-history:${event.id}`,
  domain: 'event' as const,
  classification: event.contentSafety
}))

export const causalHistoryContentRecords = (state: Pick<CausalHistoryState, 'tail'>): readonly ClassifiedMedievalContent[] => contentRecordsFor(state.tail)

const audit = (events: readonly CausalCommandEvent[]): MedievalContentSafetyAudit => {
  const result = auditMedievalContentSafety(contentRecordsFor(events))
  if (result.status === 'rejected') throw new CausalHistoryContractError(result.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  return result
}

export const createCausalHistoryState = (context: CausalHistoryContext, genesis: CausalReplayProjection): CausalHistoryState => {
  if (!validContext(context) || !projectionShape(genesis)) throw new CausalHistoryContractError([issue('causal-history:genesis', 'causal-history.invalid-context')])
  const state: CausalHistoryState = {
    version: CAUSAL_HISTORY_CONTRACT_VERSION,
    checkpoint: checkpointFor(context, 0, genesis),
    tail: [],
    compactedSegments: [],
    contentSafetyAudit: audit([])
  }
  const diagnostics = validateCausalHistoryState(context, state)
  if (diagnostics.length) throw new CausalHistoryContractError(diagnostics)
  return state
}

export const createCausalCommand = (
  context: CausalHistoryContext,
  state: CausalHistoryState,
  kind: CausalCommandKind,
  payload: InitialCourierSelectedCommand['payload'] | TimeBearingActionCommand['payload'] | DurableJomonGrowthCommand['payload'] | DelegationOfferedCommand['payload'] | DelegationInterruptedCommand['payload']
): CausalCommandEvent => {
  const diagnostics = validateCausalHistoryState(context, state)
  if (diagnostics.length) throw new CausalHistoryContractError(diagnostics)
  if (!commandPayloadIsShaped(kind, payload)) throw new CausalHistoryContractError([issue('causal-history:command', 'causal-history.invalid-command-payload')])
  const sequence = state.checkpoint.sequence + state.tail.length + 1
  const id = commandIdFor(context, sequence, kind, payload)
  const common = {
    version: CAUSAL_HISTORY_CONTRACT_VERSION,
    sequence,
    id,
    token: commandTokenFor(context, sequence, kind, payload),
    kind,
    contentSafety: commandClassification(kind, payload)
  } as const
  if (kind === 'initial-courier-selected') return { ...common, kind, payload: structuredClone(payload as InitialCourierSelectedCommand['payload']) }
  if (kind === 'time-bearing-action') return { ...common, kind, payload: structuredClone(payload as TimeBearingActionCommand['payload']) }
  if (kind === 'durable-jomon-growth') return { ...common, kind, payload: structuredClone(payload as DurableJomonGrowthCommand['payload']) }
  if (kind === 'delegation-offered') return { ...common, kind, payload: structuredClone(payload as DelegationOfferedCommand['payload']) }
  return { ...common, kind, payload: structuredClone(payload as DelegationInterruptedCommand['payload']) }
}

const segmentFor = (context: CausalHistoryContext, checkpoint: CausalHistoryCheckpoint, events: readonly CausalCommandEvent[], projection: CausalReplayProjection): CausalHistoryCompactedSegment => {
  const draft: Omit<CausalHistoryCompactedSegment, 'id' | 'token'> = {
    version: CAUSAL_HISTORY_SEGMENT_VERSION,
    sequenceStart: checkpoint.sequence + 1,
    sequenceEnd: events.at(-1)!.sequence,
    worldTimeStart: checkpoint.atWorldTime,
    worldTimeEnd: projection.temporal.worldTime,
    commandKinds: commandCountFor(events),
    provenanceDigest: context.creationDigest,
    stateDigest: causalReplayProjectionDigest(projection)
  }
  return { ...draft, id: segmentIdFor(context, draft.sequenceStart, draft.sequenceEnd, draft.stateDigest), token: segmentTokenFor(context, draft) }
}

const mergeSegments = (context: CausalHistoryContext, left: CausalHistoryCompactedSegment, right: CausalHistoryCompactedSegment): CausalHistoryCompactedSegment => {
  const draft: Omit<CausalHistoryCompactedSegment, 'id' | 'token'> = {
    version: CAUSAL_HISTORY_SEGMENT_VERSION,
    sequenceStart: left.sequenceStart,
    sequenceEnd: right.sequenceEnd,
    worldTimeStart: left.worldTimeStart,
    worldTimeEnd: right.worldTimeEnd,
    commandKinds: {
      initialCourierSelected: left.commandKinds.initialCourierSelected + right.commandKinds.initialCourierSelected,
      timeBearingAction: left.commandKinds.timeBearingAction + right.commandKinds.timeBearingAction,
      durableJomonGrowth: left.commandKinds.durableJomonGrowth + right.commandKinds.durableJomonGrowth,
      delegationOffered: left.commandKinds.delegationOffered + right.commandKinds.delegationOffered,
      delegationInterrupted: left.commandKinds.delegationInterrupted + right.commandKinds.delegationInterrupted
    },
    provenanceDigest: context.creationDigest,
    stateDigest: right.stateDigest
  }
  return { ...draft, id: segmentIdFor(context, draft.sequenceStart, draft.sequenceEnd, draft.stateDigest), token: segmentTokenFor(context, draft) }
}

/** Appends one already-applied reducer command; compaction checkpoints its exact result. */
export const appendCausalCommand = (context: CausalHistoryContext, state: CausalHistoryState, command: CausalCommandEvent, resultingProjection: CausalReplayProjection): CausalHistoryState => {
  const diagnostics = validateCausalHistoryState(context, state)
  if (diagnostics.length) throw new CausalHistoryContractError(diagnostics)
  if (!projectionShape(resultingProjection)) throw new CausalHistoryContractError([issue('causal-history:projection', 'causal-history.replay-mismatch')])
  const expected = createCausalCommand(context, state, command.kind, command.payload)
  if (!same(command, expected)) throw new CausalHistoryContractError([issue(command.id, 'causal-history.invalid-command-token')])
  const tail = [...state.tail, structuredClone(command)]
  if (tail.length <= CAUSAL_HISTORY_LIMITS.retainedCommands) {
    const nextWithoutAudit = { version: CAUSAL_HISTORY_CONTRACT_VERSION, checkpoint: structuredClone(state.checkpoint), tail, compactedSegments: structuredClone(state.compactedSegments) } satisfies Omit<CausalHistoryState, 'contentSafetyAudit'>
    const next = { ...nextWithoutAudit, contentSafetyAudit: audit(nextWithoutAudit.tail) }
    return next
  }
  const compacted = segmentFor(context, state.checkpoint, tail, resultingProjection)
  let summaries = [...state.compactedSegments, compacted]
  while (summaries.length > CAUSAL_HISTORY_LIMITS.compactedSegments) summaries = [mergeSegments(context, summaries[0]!, summaries[1]!), ...summaries.slice(2)]
  const nextWithoutAudit = { version: CAUSAL_HISTORY_CONTRACT_VERSION, checkpoint: checkpointFor(context, command.sequence, resultingProjection), tail: [] as const, compactedSegments: summaries } satisfies Omit<CausalHistoryState, 'contentSafetyAudit'>
  return { ...nextWithoutAudit, contentSafetyAudit: audit([]) }
}

const validCommand = (context: CausalHistoryContext, value: unknown): value is CausalCommandEvent => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'sequence', 'id', 'token', 'kind', 'payload', 'contentSafety']) || value.version !== CAUSAL_HISTORY_CONTRACT_VERSION || !safeInteger(value.sequence) || !validId(value.id) || typeof value.token !== 'string' || !['initial-courier-selected', 'time-bearing-action', 'durable-jomon-growth', 'delegation-offered', 'delegation-interrupted'].includes(String(value.kind)) || !commandPayloadIsShaped(value.kind as CausalCommandKind, value.payload)) return false
  const kind = value.kind as CausalCommandKind
  const payload = value.payload as InitialCourierSelectedCommand['payload'] | TimeBearingActionCommand['payload'] | DurableJomonGrowthCommand['payload']
  const expectedId = commandIdFor(context, value.sequence, kind, value.payload)
  const expectedToken = commandTokenFor(context, value.sequence, kind, value.payload)
  if (value.id !== expectedId || value.token !== expectedToken || !same(value.contentSafety, commandClassification(kind, payload))) return false
  return validateMedievalContentSafety([{ id: `causal-history:${value.id}`, domain: 'event', classification: value.contentSafety }]).status === 'accepted'
}

const validCheckpoint = (context: CausalHistoryContext, value: unknown): value is CausalHistoryCheckpoint => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'token', 'sequence', 'atWorldTime', 'provenanceDigest', 'stateDigest', 'projection']) || value.version !== CAUSAL_HISTORY_CHECKPOINT_VERSION || !validId(value.id) || typeof value.token !== 'string' || !safeInteger(value.sequence) || !safeInteger(value.atWorldTime) || value.provenanceDigest !== context.creationDigest || typeof value.stateDigest !== 'string' || !projectionShape(value.projection) || value.projection.temporal.worldTime !== value.atWorldTime || causalReplayProjectionDigest(value.projection) !== value.stateDigest) return false
  return value.id === checkpointIdFor(context, value.sequence, value.stateDigest) && value.token === checkpointTokenFor(context, value.sequence, value.atWorldTime, value.stateDigest)
}

const validCounts = (value: unknown): value is CausalHistoryCommandKindCounts => record(value) && hasOnlyKeys(value, ['initialCourierSelected', 'timeBearingAction', 'durableJomonGrowth', 'delegationOffered', 'delegationInterrupted']) && safeInteger(value.initialCourierSelected) && safeInteger(value.timeBearingAction) && safeInteger(value.durableJomonGrowth) && safeInteger(value.delegationOffered) && safeInteger(value.delegationInterrupted)
const validSegment = (context: CausalHistoryContext, value: unknown): value is CausalHistoryCompactedSegment => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'token', 'sequenceStart', 'sequenceEnd', 'worldTimeStart', 'worldTimeEnd', 'commandKinds', 'provenanceDigest', 'stateDigest']) || value.version !== CAUSAL_HISTORY_SEGMENT_VERSION || !validId(value.id) || typeof value.token !== 'string' || !safeInteger(value.sequenceStart) || !safeInteger(value.sequenceEnd) || value.sequenceStart < 1 || value.sequenceEnd < value.sequenceStart || !safeInteger(value.worldTimeStart) || !safeInteger(value.worldTimeEnd) || value.worldTimeEnd < value.worldTimeStart || !validCounts(value.commandKinds) || value.provenanceDigest !== context.creationDigest || typeof value.stateDigest !== 'string') return false
  const draft: Omit<CausalHistoryCompactedSegment, 'id' | 'token'> = { version: value.version, sequenceStart: value.sequenceStart, sequenceEnd: value.sequenceEnd, worldTimeStart: value.worldTimeStart, worldTimeEnd: value.worldTimeEnd, commandKinds: value.commandKinds, provenanceDigest: value.provenanceDigest, stateDigest: value.stateDigest }
  return value.id === segmentIdFor(context, value.sequenceStart, value.sequenceEnd, value.stateDigest) && value.token === segmentTokenFor(context, draft)
}

/** Structural/token/content validation. Full replay equality belongs to world validation. */
export const validateCausalHistoryState = (context: CausalHistoryContext, value: unknown): readonly CausalHistoryDiagnostic[] => {
  if (!validContext(context)) return [issue('causal-history:context', 'causal-history.invalid-context')]
  if (!record(value) || !hasOnlyKeys(value, ['version', 'checkpoint', 'tail', 'compactedSegments', 'contentSafetyAudit'])) return [issue('causal-history', 'causal-history.malformed-state')]
  const issues: CausalHistoryDiagnostic[] = []
  if (value.version !== CAUSAL_HISTORY_CONTRACT_VERSION) issues.push(issue('causal-history', 'causal-history.invalid-version'))
  if (!validCheckpoint(context, value.checkpoint)) issues.push(issue('causal-history:checkpoint', 'causal-history.invalid-checkpoint'))
  if (!Array.isArray(value.tail)) issues.push(issue('causal-history:tail', 'causal-history.invalid-command'))
  else {
    if (value.tail.length > CAUSAL_HISTORY_LIMITS.retainedCommands) issues.push(issue('causal-history:tail', 'causal-history.command-limit'))
    const ids = new Set<string>()
    for (const command of value.tail) {
      const id = record(command) && typeof command.id === 'string' ? command.id : 'causal-history:command'
      if (!validCommand(context, command)) {
        const typedCommand = record(command) && typeof command.kind === 'string' && ['initial-courier-selected', 'time-bearing-action', 'durable-jomon-growth', 'delegation-offered', 'delegation-interrupted'].includes(command.kind)
        const payloadShaped = typedCommand && commandPayloadIsShaped(command.kind as CausalCommandKind, command.payload)
        const tokenExpected = typedCommand && payloadShaped && safeInteger(command.sequence)
        issues.push(issue(id, !typedCommand ? 'causal-history.invalid-command' : !payloadShaped ? 'causal-history.invalid-command-payload' : tokenExpected ? 'causal-history.invalid-command-token' : 'causal-history.invalid-command'))
      }
      if (record(command) && typeof command.id === 'string') {
        if (ids.has(command.id)) issues.push(issue(command.id, 'causal-history.duplicate-command-id'))
        ids.add(command.id)
      }
    }
    const checkpointSequence = record(value.checkpoint) && safeInteger(value.checkpoint.sequence) ? value.checkpoint.sequence : 0
    if (value.tail.some((command, index) => !record(command) || command.sequence !== checkpointSequence + index + 1)) issues.push(issue('causal-history:tail', 'causal-history.invalid-command-order'))
  }
  if (!Array.isArray(value.compactedSegments)) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment'))
  else {
    if (value.compactedSegments.length > CAUSAL_HISTORY_LIMITS.compactedSegments) issues.push(issue('causal-history:segments', 'causal-history.segment-limit'))
    let expectedStart = 1
    for (const segment of value.compactedSegments) {
      const id = record(segment) && typeof segment.id === 'string' ? segment.id : 'causal-history:segment'
      if (!validSegment(context, segment)) issues.push(issue(id, 'causal-history.invalid-segment-token'))
      if (!record(segment) || segment.sequenceStart !== expectedStart) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment-order'))
      if (record(segment) && safeInteger(segment.sequenceEnd)) expectedStart = segment.sequenceEnd + 1
    }
    const checkpointSequence = record(value.checkpoint) && safeInteger(value.checkpoint.sequence) ? value.checkpoint.sequence : 0
    if (expectedStart !== checkpointSequence + 1) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment-order'))
  }
  try {
    if (!Array.isArray(value.tail) || !contentSafetyAuditMatches(contentRecordsFor(value.tail as CausalCommandEvent[]), value.contentSafetyAudit)) issues.push(issue('causal-history:content-safety', 'causal-history.invalid-content-audit'))
  } catch { issues.push(issue('causal-history:content-safety', 'causal-history.invalid-content-audit')) }
  return canonicalIssues(issues)
}

export type CausalHistoryReplayExecutor = (projection: CausalReplayProjection, command: CausalCommandEvent) => CausalReplayProjection

/** Replays the canonical retained tail without invoking public world transitions. */
export const replayCausalHistory = (context: CausalHistoryContext, state: CausalHistoryState, execute: CausalHistoryReplayExecutor): CausalReplayProjection => {
  const diagnostics = validateCausalHistoryState(context, state)
  if (diagnostics.length) throw new CausalHistoryContractError(diagnostics)
  let projection = structuredClone(state.checkpoint.projection)
  try {
    for (const command of state.tail) projection = causalReplayProjection(execute(projection, structuredClone(command)))
  } catch {
    throw new CausalHistoryContractError([issue('causal-history:replay', 'causal-history.replay-failed')])
  }
  return projection
}

/** Validates that a saved state is exactly reconstructible from its checkpoint and tail. */
export const validateCausalHistoryReplay = (context: CausalHistoryContext, state: CausalHistoryState, present: CausalReplayProjection, execute: CausalHistoryReplayExecutor): readonly CausalHistoryDiagnostic[] => {
  try {
    const replayed = replayCausalHistory(context, state, execute)
    return same(replayed, present) ? [] : [issue('causal-history:replay', 'causal-history.replay-mismatch')]
  } catch (error) {
    return error instanceof CausalHistoryContractError ? error.diagnostics : [issue('causal-history:replay', 'causal-history.replay-failed')]
  }
}
