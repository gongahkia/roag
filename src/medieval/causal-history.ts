import { auditMedievalContentSafety, contentSafetyAuditMatches, classifyMedievalContent, validateMedievalContentSafety, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { hashSeed } from './rng'
import type { SimulationCatchUpState } from './simulation-catchup'
import type { MedievalTemporalState, TimeBearingTemporalAction } from './temporal'
import type { DurableJomonGrowthEvidence, WorldEraState } from './world-era'
import { isDelegationInterruptionInput, isDelegationOfferInput, type DelegationInterruptionInput, type DelegationOfferInput, type DelegationState } from './delegation'
import type { PersistentPersonRecord } from './persistent-person'
import type { AutonomyState } from './autonomy'
import type { SocialMemoryState } from './social-memory'
import { courierContinuityConfirmationIdFor, type CourierContinuityConfirmation } from './courier-continuity'
import type { WorldJomonState } from './world-state'
import { validateVesselPropActionState } from './vessel-prop-action'
import { JOMON_COMMODITY_IDS, type JomonCommodityId } from './commodity-catalogue'
import { VESSEL_CARGO_FAILURE_OUTCOMES, validateVesselCargoState, type VesselCargoFailureOutcome } from './cargo-hold'
import { validateSettlementTradingState, type SettlementTradingState } from './settlement-trading'

/**
 * The global mutable command journal. Domain-local temporal and catch-up
 * evidence remains in its owning subdomain; this records only reducer inputs.
 */
/** v7 adds one bounded local settlement-contract handoff. */
export const CAUSAL_HISTORY_CONTRACT_VERSION = 7 as const
export const CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION = 9 as const
export const CAUSAL_HISTORY_CHECKPOINT_VERSION = 7 as const
export const CAUSAL_HISTORY_SEGMENT_VERSION = 7 as const
/** v6 has cargo but no settlement-contract projection. */
export const LEGACY_SETTLEMENT_TRADE_CAUSAL_HISTORY_CONTRACT_VERSION = 6 as const
export const LEGACY_SETTLEMENT_TRADE_CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION = 8 as const
export const LEGACY_SETTLEMENT_TRADE_CAUSAL_HISTORY_CHECKPOINT_VERSION = 6 as const
export const LEGACY_SETTLEMENT_TRADE_CAUSAL_HISTORY_SEGMENT_VERSION = 6 as const
/** Read-only source versions accepted solely by full-envelope upgrade paths. */
export const LEGACY_CAUSAL_HISTORY_CONTRACT_VERSION = 4 as const
export const LEGACY_CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION = 6 as const
export const LEGACY_CAUSAL_HISTORY_CHECKPOINT_VERSION = 4 as const
export const LEGACY_CAUSAL_HISTORY_SEGMENT_VERSION = 4 as const
/** v5/v7 checkpoint evidence has prop actions but no bounded cargo lots. */
export const LEGACY_CARGO_CAUSAL_HISTORY_CONTRACT_VERSION = 5 as const
export const LEGACY_CARGO_CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION = 7 as const
export const LEGACY_CARGO_CAUSAL_HISTORY_CHECKPOINT_VERSION = 5 as const
export const LEGACY_CARGO_CAUSAL_HISTORY_SEGMENT_VERSION = 5 as const

export const CAUSAL_HISTORY_LIMITS = {
  retainedCommands: 8,
  checkpoints: 1,
  compactedSegments: 8,
  identityLength: 160
} as const

export type CausalCommandKind = 'initial-courier-selected' | 'tavern-courier-switched' | 'vessel-station-readout-recorded' | 'vessel-cargo-loaded' | 'vessel-cargo-unloaded' | 'vessel-cargo-failure-resolved' | 'vessel-cargo-recovered' | 'settlement-trade-accepted' | 'settlement-trade-refused' | 'settlement-trade-delivered' | 'courier-loss-resolved' | 'time-bearing-action' | 'deck-moved' | 'durable-jomon-growth' | 'delegation-offered' | 'delegation-interrupted'

export interface CausalHistoryContext {
  worldId: string
  creationDigest: string
}

export interface CausalHistoryCourierProjection {
  version: 3
  initialCourierId?: string
  activeCourierId?: string
  departedCourierIds?: readonly string[]
}

/** Read-only v4 projection accepted only while rebasing a valid older envelope. */
interface LegacyCausalHistoryCourierProjectionV1 {
  version: 1
  initialCourierId?: string
}

/** Read-only v5 projection accepted only while upgrading a valid v14/v13 envelope. */
interface LegacyCausalHistoryCourierProjectionV2 {
  version: 2
  initialCourierId?: string
  activeCourierId?: string
}

/** Local navigation is replayed with courier selection/movement, never inferred from browser state. */
export interface CausalHistoryNavigationProjection {
  version: 1
  courierId?: string
  coordinate?: { column: number; row: number }
}

/** People become journalled because delegation mutates work and source-linked recall. */
export interface CausalHistoryPeopleProjection {
  version: 5
  records: readonly PersistentPersonRecord[]
}

/** The intentionally narrow mutable result of currently journalled commands. */
export interface CausalReplayProjection {
  version: typeof CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION
  courier: CausalHistoryCourierProjection
  navigation?: CausalHistoryNavigationProjection
  people: CausalHistoryPeopleProjection
  temporal: MedievalTemporalState
  simulation: SimulationCatchUpState
  era: WorldEraState
  delegation: DelegationState
  autonomy: AutonomyState
  /** Delegation-derived recall is replayed with its source task/person mutations. */
  socialMemory: SocialMemoryState
  /** Latest bounded physical-prop outcomes are replayed with their commands. */
  jomon: WorldJomonState
  /** One physical local trade location and its current bounded contract. */
  settlementTrading: SettlementTradingState
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

/** A zero-time, source-bound transfer of the inhabited courier perspective. */
export interface TavernCourierSwitchedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'tavern-courier-switched'
  payload: {
    fromCourierId: string
    toCourierId: string
    propId: 'prop:task-ledger'
    coordinate: { column: number; row: number }
  }
  contentSafety: MedievalContentSafetyClassification
}

/** A zero-time acknowledgement of one source-backed non-ledger station readout. */
export interface VesselStationReadoutRecordedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'vessel-station-readout-recorded'
  payload: { propId: Exclude<import('./types').FoundationJomonPropId, 'prop:task-ledger'> }
  contentSafety: MedievalContentSafetyClassification
}

/** Exact physical hold transitions; they carry no source, map, or browser data. */
export interface VesselCargoLoadedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'vessel-cargo-loaded'
  payload: { propId: 'prop:cargo-hold-rack'; commodityId: JomonCommodityId; quantity: number }
  contentSafety: MedievalContentSafetyClassification
}

export interface VesselCargoUnloadedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'vessel-cargo-unloaded'
  payload: { propId: 'prop:cargo-hold-rack'; cargoId: string }
  contentSafety: MedievalContentSafetyClassification
}

export interface VesselCargoFailureResolvedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'vessel-cargo-failure-resolved'
  payload: { propId: 'prop:cargo-hold-rack'; cargoId: string; outcome: VesselCargoFailureOutcome }
  contentSafety: MedievalContentSafetyClassification
}

export interface VesselCargoRecoveredCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'vessel-cargo-recovered'
  payload: { propId: 'prop:cargo-hold-rack'; cargoId: string }
  contentSafety: MedievalContentSafetyClassification
}

/** One local physical settlement contract; source coordinates remain private to its owner. */
export interface SettlementTradeAcceptedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'settlement-trade-accepted'
  payload: { locationId: 'settlement-location:hearthford-mill-quay'; contractId: 'settlement-contract:hearthford-mill-ironwork' }
  contentSafety: MedievalContentSafetyClassification
}

export interface SettlementTradeRefusedCommand extends Omit<SettlementTradeAcceptedCommand, 'kind'> {
  kind: 'settlement-trade-refused'
}

export interface SettlementTradeDeliveredCommand extends Omit<SettlementTradeAcceptedCommand, 'kind' | 'payload'> {
  kind: 'settlement-trade-delivered'
  payload: { locationId: 'settlement-location:hearthford-mill-quay'; contractId: 'settlement-contract:hearthford-mill-ironwork'; propId: 'prop:cargo-hold-rack' }
}

/** One explicit permanent loss outcome; the reducer proves its successor/finalization result. */
export interface CourierLossResolvedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'courier-loss-resolved'
  payload: {
    confirmation: CourierContinuityConfirmation
    finalization: 'continue' | 'crew-extinction'
    successorId?: string
  }
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

export interface DeckMovedCommand {
  version: typeof CAUSAL_HISTORY_CONTRACT_VERSION
  sequence: number
  id: string
  token: string
  kind: 'deck-moved'
  payload: {
    actionId: string
    courierId: string
    direction: 'north-west' | 'north' | 'north-east' | 'west' | 'east' | 'south-west' | 'south' | 'south-east'
    from: { column: number; row: number }
    to: { column: number; row: number }
  }
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

export type CausalCommandEvent = InitialCourierSelectedCommand | TavernCourierSwitchedCommand | VesselStationReadoutRecordedCommand | VesselCargoLoadedCommand | VesselCargoUnloadedCommand | VesselCargoFailureResolvedCommand | VesselCargoRecoveredCommand | SettlementTradeAcceptedCommand | SettlementTradeRefusedCommand | SettlementTradeDeliveredCommand | CourierLossResolvedCommand | TimeBearingActionCommand | DeckMovedCommand | DurableJomonGrowthCommand | DelegationOfferedCommand | DelegationInterruptedCommand

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
  tavernCourierSwitched?: number
  vesselStationReadoutRecorded?: number
  vesselCargoLoaded?: number
  vesselCargoUnloaded?: number
  vesselCargoFailureResolved?: number
  vesselCargoRecovered?: number
  settlementTradeAccepted?: number
  settlementTradeRefused?: number
  settlementTradeDelivered?: number
  courierLossResolved?: number
  timeBearingAction: number
  durableJomonGrowth: number
  delegationOffered: number
  delegationInterrupted: number
  deckMoved?: number
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
  tavernCourierSwitched: events.filter(event => event.kind === 'tavern-courier-switched').length,
  vesselStationReadoutRecorded: events.filter(event => event.kind === 'vessel-station-readout-recorded').length,
  vesselCargoLoaded: events.filter(event => event.kind === 'vessel-cargo-loaded').length,
  vesselCargoUnloaded: events.filter(event => event.kind === 'vessel-cargo-unloaded').length,
  vesselCargoFailureResolved: events.filter(event => event.kind === 'vessel-cargo-failure-resolved').length,
  vesselCargoRecovered: events.filter(event => event.kind === 'vessel-cargo-recovered').length,
  settlementTradeAccepted: events.filter(event => event.kind === 'settlement-trade-accepted').length,
  settlementTradeRefused: events.filter(event => event.kind === 'settlement-trade-refused').length,
  settlementTradeDelivered: events.filter(event => event.kind === 'settlement-trade-delivered').length,
  courierLossResolved: events.filter(event => event.kind === 'courier-loss-resolved').length,
  timeBearingAction: events.filter(event => event.kind === 'time-bearing-action').length,
  durableJomonGrowth: events.filter(event => event.kind === 'durable-jomon-growth').length,
  delegationOffered: events.filter(event => event.kind === 'delegation-offered').length,
  delegationInterrupted: events.filter(event => event.kind === 'delegation-interrupted').length,
  deckMoved: events.filter(event => event.kind === 'deck-moved').length
})
const commandClassification = (kind: CausalCommandKind, payload: InitialCourierSelectedCommand['payload'] | TavernCourierSwitchedCommand['payload'] | VesselStationReadoutRecordedCommand['payload'] | VesselCargoLoadedCommand['payload'] | VesselCargoUnloadedCommand['payload'] | VesselCargoFailureResolvedCommand['payload'] | VesselCargoRecoveredCommand['payload'] | SettlementTradeAcceptedCommand['payload'] | SettlementTradeDeliveredCommand['payload'] | CourierLossResolvedCommand['payload'] | TimeBearingActionCommand['payload'] | DeckMovedCommand['payload'] | DurableJomonGrowthCommand['payload'] | DelegationOfferedCommand['payload'] | DelegationInterruptedCommand['payload']): MedievalContentSafetyClassification => {
  if (kind === 'time-bearing-action') return structuredClone((payload as TimeBearingActionCommand['payload']).action.contentSafety)
  if (kind === 'courier-loss-resolved') return structuredClone((payload as CourierLossResolvedCommand['payload']).confirmation.contentSafety)
  // The offer remains a classified contract in its payload; the journal entry
  // itself is an event, so it must carry an event-domain classification too.
  if (kind === 'delegation-offered') return classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
  if (kind === 'delegation-interrupted') return classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
  if (kind === 'deck-moved') return classifyMedievalContent('event', ['adult-labour', 'navigation'], 'adults-only', ['data'])
  if (kind === 'tavern-courier-switched') return classifyMedievalContent('event', ['adult-labour', 'civil-life', 'navigation'], 'adults-only', ['data'])
  if (kind === 'vessel-station-readout-recorded') return classifyMedievalContent('event', ['adult-labour', 'civil-life', 'navigation'], 'adults-only', ['data'])
  if (kind === 'vessel-cargo-loaded' || kind === 'vessel-cargo-unloaded' || kind === 'vessel-cargo-failure-resolved' || kind === 'vessel-cargo-recovered') return classifyMedievalContent('event', ['adult-labour', 'commerce', 'navigation'], 'adults-only', ['data'])
  if (kind === 'settlement-trade-accepted' || kind === 'settlement-trade-refused' || kind === 'settlement-trade-delivered') return classifyMedievalContent('event', ['adult-labour', 'civil-life', 'commerce'], 'adults-only', ['data'])
  return kind === 'initial-courier-selected'
    ? classifyMedievalContent('event', ['civil-life', 'travel'], 'adults-only', ['data'])
    : classifyMedievalContent('event', ['adult-labour', 'navigation'], 'adults-only', ['data'])
}
const commandPayloadIsShaped = (kind: CausalCommandKind, value: unknown): boolean => {
  if (!record(value)) return false
  if (kind === 'initial-courier-selected') return hasOnlyKeys(value, ['courierId']) && validId(value.courierId)
  if (kind === 'tavern-courier-switched') return hasOnlyKeys(value, ['fromCourierId', 'toCourierId', 'propId', 'coordinate'])
    && validId(value.fromCourierId) && validId(value.toCourierId) && value.fromCourierId !== value.toCourierId
    && value.propId === 'prop:task-ledger'
    && record(value.coordinate) && hasOnlyKeys(value.coordinate, ['column', 'row']) && safeInteger(value.coordinate.column) && safeInteger(value.coordinate.row)
  if (kind === 'vessel-station-readout-recorded') return hasOnlyKeys(value, ['propId'])
    && typeof value.propId === 'string'
    && ['prop:berth', 'prop:cargo-hold-rack', 'prop:chart-table', 'prop:galley-hearth', 'prop:gangplank', 'prop:repair-space-rack', 'prop:stores-rack'].includes(value.propId)
  if (kind === 'vessel-cargo-loaded') return hasOnlyKeys(value, ['propId', 'commodityId', 'quantity'])
    && value.propId === 'prop:cargo-hold-rack'
    && JOMON_COMMODITY_IDS.includes(value.commodityId as JomonCommodityId)
    && safeInteger(value.quantity) && value.quantity >= 1 && value.quantity <= 12
  if (kind === 'vessel-cargo-unloaded' || kind === 'vessel-cargo-recovered') return hasOnlyKeys(value, ['propId', 'cargoId'])
    && value.propId === 'prop:cargo-hold-rack' && validId(value.cargoId)
  if (kind === 'vessel-cargo-failure-resolved') return hasOnlyKeys(value, ['propId', 'cargoId', 'outcome'])
    && value.propId === 'prop:cargo-hold-rack' && validId(value.cargoId)
    && VESSEL_CARGO_FAILURE_OUTCOMES.includes(value.outcome as VesselCargoFailureOutcome)
  if (kind === 'settlement-trade-accepted' || kind === 'settlement-trade-refused') return hasOnlyKeys(value, ['locationId', 'contractId'])
    && value.locationId === 'settlement-location:hearthford-mill-quay'
    && value.contractId === 'settlement-contract:hearthford-mill-ironwork'
  if (kind === 'settlement-trade-delivered') return hasOnlyKeys(value, ['locationId', 'contractId', 'propId'])
    && value.locationId === 'settlement-location:hearthford-mill-quay'
    && value.contractId === 'settlement-contract:hearthford-mill-ironwork'
    && value.propId === 'prop:cargo-hold-rack'
  if (kind === 'courier-loss-resolved') {
    if (!hasOnlyKeys(value, ['confirmation', 'finalization', ...(Object.hasOwn(value, 'successorId') ? ['successorId'] : [])]) || !record(value.confirmation)) return false
    const confirmation = value.confirmation
    const evidenceIds = confirmation.evidenceIds
    const validConfirmation = hasOnlyKeys(confirmation, ['version', 'id', 'kind', 'outcome', 'courierId', 'atWorldTime', 'evidenceIds', 'contentSafety'])
      && confirmation.version === 1
      && confirmation.kind === 'confirmed-courier-continuity-loss'
      && (confirmation.outcome === 'death' || confirmation.outcome === 'departure')
      && validId(confirmation.id)
      && confirmation.id === courierContinuityConfirmationIdFor(confirmation.outcome as 'death' | 'departure', confirmation.courierId as string, confirmation.atWorldTime as number)
      && validId(confirmation.courierId)
      && safeInteger(confirmation.atWorldTime)
      && Array.isArray(evidenceIds)
      && evidenceIds.length > 0
      && evidenceIds.length <= 4
      && evidenceIds.every(item => validId(item))
      && new Set(evidenceIds).size === evidenceIds.length
      && evidenceIds.every((item, index) => index === 0 || String(evidenceIds[index - 1]) < String(item))
    if (!validConfirmation || (value.finalization !== 'continue' && value.finalization !== 'crew-extinction')) return false
    return value.finalization === 'continue'
      ? validId(value.successorId) && value.successorId !== confirmation.courierId
      : value.successorId === undefined
  }
  if (kind === 'time-bearing-action') return hasOnlyKeys(value, ['action']) && record(value.action) && typeof value.action.id === 'string' && typeof value.action.kind === 'string' && typeof value.action.durationMinutes === 'number' && value.action.contentSafety !== undefined
  if (kind === 'deck-moved') return hasOnlyKeys(value, ['actionId', 'courierId', 'direction', 'from', 'to'])
    && validId(value.actionId) && validId(value.courierId)
    && ['north-west', 'north', 'north-east', 'west', 'east', 'south-west', 'south', 'south-east'].includes(String(value.direction))
    && record(value.from) && hasOnlyKeys(value.from, ['column', 'row']) && safeInteger(value.from.column) && safeInteger(value.from.row)
    && record(value.to) && hasOnlyKeys(value.to, ['column', 'row']) && safeInteger(value.to.column) && safeInteger(value.to.row)
  if (kind === 'durable-jomon-growth') return hasOnlyKeys(value, ['evidence']) && record(value.evidence) && typeof value.evidence.id === 'string' && typeof value.evidence.kind === 'string' && value.evidence.source !== undefined && typeof value.evidence.atWorldTime === 'number'
  if (kind === 'delegation-offered') return hasOnlyKeys(value, ['offer']) && isDelegationOfferInput(value.offer)
  return hasOnlyKeys(value, ['interruption']) && isDelegationInterruptionInput(value.interruption)
}
const commandIdFor = (context: CausalHistoryContext, sequence: number, kind: CausalCommandKind, payload: unknown): string => `causal-command:${sequence}:${causalDigestFor('causal-command-id', { worldId: context.worldId, creationDigest: context.creationDigest, sequence, kind, payload })}`
const commandTokenFor = (context: CausalHistoryContext, sequence: number, kind: CausalCommandKind, payload: unknown): string => causalDigestFor('causal-command-token', { worldId: context.worldId, creationDigest: context.creationDigest, sequence, kind, payload })
const checkpointIdFor = (context: CausalHistoryContext, sequence: number, stateDigest: string): string => `causal-checkpoint:${sequence}:${causalDigestFor('causal-checkpoint-id', { worldId: context.worldId, creationDigest: context.creationDigest, sequence, stateDigest })}`
const checkpointTokenFor = (context: CausalHistoryContext, sequence: number, atWorldTime: number, stateDigest: string): string => causalDigestFor('causal-checkpoint-token', { worldId: context.worldId, creationDigest: context.creationDigest, sequence, atWorldTime, stateDigest })
const segmentIdFor = (context: CausalHistoryContext, start: number, end: number, stateDigest: string): string => `causal-segment:${start}-${end}:${causalDigestFor('causal-segment-id', { worldId: context.worldId, creationDigest: context.creationDigest, start, end, stateDigest })}`
type CausalSegmentTokenDraft = Omit<CausalHistoryCompactedSegment, 'id' | 'token' | 'version'> & { version: number }
const segmentTokenFor = (context: CausalHistoryContext, summary: CausalSegmentTokenDraft): string => causalDigestFor('causal-segment-token', { worldId: context.worldId, creationDigest: context.creationDigest, ...summary })

const validProjectionCourier = (value: unknown): boolean => {
  if (!record(value) || !(hasOnlyKeys(value, ['version']) || hasOnlyKeys(value, ['version', 'initialCourierId', 'activeCourierId', 'departedCourierIds']) || hasOnlyKeys(value, ['version', 'initialCourierId', 'departedCourierIds'])) || value.version !== 3) return false
  if ((value.initialCourierId !== undefined && !validId(value.initialCourierId)) || (value.activeCourierId !== undefined && !validId(value.activeCourierId))) return false
  if (value.initialCourierId === undefined) return value.activeCourierId === undefined
  const departedCourierIds = value.departedCourierIds
  if (!Array.isArray(departedCourierIds)) return false
  if (!departedCourierIds.every(item => validId(item)) || new Set(departedCourierIds).size !== departedCourierIds.length || !departedCourierIds.every((item, index) => index === 0 || String(departedCourierIds[index - 1]) < String(item))) return false
  return value.activeCourierId === undefined || !departedCourierIds.includes(value.activeCourierId)
}

const projectionShape = (value: unknown): value is CausalReplayProjection => record(value)
  && (hasOnlyKeys(value, ['version', 'courier', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory', 'jomon', 'settlementTrading']) || hasOnlyKeys(value, ['version', 'courier', 'navigation', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory', 'jomon', 'settlementTrading']))
  && value.version === CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION
  && validProjectionCourier(value.courier)
  && (value.navigation === undefined || (record(value.navigation) && (hasOnlyKeys(value.navigation, ['version']) || hasOnlyKeys(value.navigation, ['version', 'courierId', 'coordinate'])) && value.navigation.version === 1 && (value.navigation.courierId === undefined || validId(value.navigation.courierId)) && (value.navigation.coordinate === undefined || (record(value.navigation.coordinate) && hasOnlyKeys(value.navigation.coordinate, ['column', 'row']) && safeInteger(value.navigation.coordinate.column) && safeInteger(value.navigation.coordinate.row)))))
  && record(value.people)
  && hasOnlyKeys(value.people, ['version', 'records'])
  && value.people.version === 5
  && Array.isArray(value.people.records)
  && record(value.temporal)
  && record(value.simulation)
  && record(value.era)
  && record(value.delegation)
  && record(value.autonomy)
  && record(value.socialMemory)
  && record(value.jomon)
  && validateSettlementTradingState(value.settlementTrading).length === 0
  && value.jomon.version === 3
  && validateVesselPropActionState({ props: [
    { id: 'prop:berth', kind: 'berth', partition: 'berths' },
    { id: 'prop:cargo-hold-rack', kind: 'rack', partition: 'cargo-hold' },
    { id: 'prop:chart-table', kind: 'table', partition: 'chart-table' },
    { id: 'prop:galley-hearth', kind: 'hearth', partition: 'galley' },
    { id: 'prop:gangplank', kind: 'gangplank', partition: 'gangplank' },
    { id: 'prop:repair-space-rack', kind: 'rack', partition: 'repair-space' },
    { id: 'prop:stores-rack', kind: 'rack', partition: 'stores' },
    { id: 'prop:task-ledger', kind: 'ledger', partition: 'tavern' }
  ] }, value.jomon.propActions).length === 0
  && validateVesselCargoState(value.jomon.cargo, record(value.jomon.capacity) ? value.jomon.capacity.cargoUnits as number : undefined).length === 0

/** State-v16 replay has bounded cargo but no local settlement trade contract. */
const legacyProjectionV8Shape = (value: unknown): boolean => record(value)
  && (hasOnlyKeys(value, ['version', 'courier', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory', 'jomon']) || hasOnlyKeys(value, ['version', 'courier', 'navigation', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory', 'jomon']))
  && value.version === LEGACY_SETTLEMENT_TRADE_CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION
  && validProjectionCourier(value.courier)
  && (value.navigation === undefined || (record(value.navigation) && (hasOnlyKeys(value.navigation, ['version']) || hasOnlyKeys(value.navigation, ['version', 'courierId', 'coordinate'])) && value.navigation.version === 1 && (value.navigation.courierId === undefined || validId(value.navigation.courierId)) && (value.navigation.coordinate === undefined || (record(value.navigation.coordinate) && hasOnlyKeys(value.navigation.coordinate, ['column', 'row']) && safeInteger(value.navigation.coordinate.column) && safeInteger(value.navigation.coordinate.row)))))
  && record(value.people) && hasOnlyKeys(value.people, ['version', 'records']) && value.people.version === 5 && Array.isArray(value.people.records)
  && record(value.temporal) && record(value.simulation) && record(value.era) && record(value.delegation) && record(value.autonomy) && record(value.socialMemory)
  && record(value.jomon) && value.jomon.version === 3
  && validateVesselPropActionState({ props: [
    { id: 'prop:berth', kind: 'berth', partition: 'berths' },
    { id: 'prop:cargo-hold-rack', kind: 'rack', partition: 'cargo-hold' },
    { id: 'prop:chart-table', kind: 'table', partition: 'chart-table' },
    { id: 'prop:galley-hearth', kind: 'hearth', partition: 'galley' },
    { id: 'prop:gangplank', kind: 'gangplank', partition: 'gangplank' },
    { id: 'prop:repair-space-rack', kind: 'rack', partition: 'repair-space' },
    { id: 'prop:stores-rack', kind: 'rack', partition: 'stores' },
    { id: 'prop:task-ledger', kind: 'ledger', partition: 'tavern' }
  ] }, value.jomon.propActions).length === 0
  && validateVesselCargoState(value.jomon.cargo, record(value.jomon.capacity) ? value.jomon.capacity.cargoUnits as number : undefined).length === 0

/** State-v15 replay has bounded prop actions but no cargo lots. */
const legacyProjectionV7Shape = (value: unknown): boolean => record(value)
  && (hasOnlyKeys(value, ['version', 'courier', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory', 'jomon']) || hasOnlyKeys(value, ['version', 'courier', 'navigation', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory', 'jomon']))
  && value.version === LEGACY_CARGO_CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION
  && validProjectionCourier(value.courier)
  && (value.navigation === undefined || (record(value.navigation) && (hasOnlyKeys(value.navigation, ['version']) || hasOnlyKeys(value.navigation, ['version', 'courierId', 'coordinate'])) && value.navigation.version === 1 && (value.navigation.courierId === undefined || validId(value.navigation.courierId)) && (value.navigation.coordinate === undefined || (record(value.navigation.coordinate) && hasOnlyKeys(value.navigation.coordinate, ['column', 'row']) && safeInteger(value.navigation.coordinate.column) && safeInteger(value.navigation.coordinate.row)))))
  && record(value.people) && hasOnlyKeys(value.people, ['version', 'records']) && value.people.version === 5 && Array.isArray(value.people.records)
  && record(value.temporal) && record(value.simulation) && record(value.era) && record(value.delegation) && record(value.autonomy) && record(value.socialMemory)
  && record(value.jomon) && value.jomon.version === 2
  && validateVesselPropActionState({ props: [
    { id: 'prop:berth', kind: 'berth', partition: 'berths' },
    { id: 'prop:cargo-hold-rack', kind: 'rack', partition: 'cargo-hold' },
    { id: 'prop:chart-table', kind: 'table', partition: 'chart-table' },
    { id: 'prop:galley-hearth', kind: 'hearth', partition: 'galley' },
    { id: 'prop:gangplank', kind: 'gangplank', partition: 'gangplank' },
    { id: 'prop:repair-space-rack', kind: 'rack', partition: 'repair-space' },
    { id: 'prop:stores-rack', kind: 'rack', partition: 'stores' },
    { id: 'prop:task-ledger', kind: 'ledger', partition: 'tavern' }
  ] }, value.jomon.propActions).length === 0

const legacyProjectionShape = (value: unknown): boolean => record(value)
  && (hasOnlyKeys(value, ['version', 'courier', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory']) || hasOnlyKeys(value, ['version', 'courier', 'navigation', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory']))
  && value.version === 4
  && record(value.courier)
  && (hasOnlyKeys(value.courier, ['version']) || hasOnlyKeys(value.courier, ['version', 'initialCourierId']))
  && (value.courier as unknown as LegacyCausalHistoryCourierProjectionV1).version === 1
  && (value.courier.initialCourierId === undefined || validId(value.courier.initialCourierId))
  && (value.navigation === undefined || (record(value.navigation) && (hasOnlyKeys(value.navigation, ['version']) || hasOnlyKeys(value.navigation, ['version', 'courierId', 'coordinate'])) && value.navigation.version === 1 && (value.navigation.courierId === undefined || validId(value.navigation.courierId)) && (value.navigation.coordinate === undefined || (record(value.navigation.coordinate) && hasOnlyKeys(value.navigation.coordinate, ['column', 'row']) && safeInteger(value.navigation.coordinate.column) && safeInteger(value.navigation.coordinate.row)))))
  && record(value.people) && hasOnlyKeys(value.people, ['version', 'records']) && value.people.version === 5 && Array.isArray(value.people.records)
  && record(value.temporal) && record(value.simulation) && record(value.era) && record(value.delegation) && record(value.autonomy) && record(value.socialMemory)

const legacyProjectionV5Shape = (value: unknown): boolean => record(value)
  && (hasOnlyKeys(value, ['version', 'courier', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory']) || hasOnlyKeys(value, ['version', 'courier', 'navigation', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory']))
  && value.version === 5
  && record(value.courier)
  && (hasOnlyKeys(value.courier, ['version']) || hasOnlyKeys(value.courier, ['version', 'initialCourierId', 'activeCourierId']))
  && (value.courier as unknown as LegacyCausalHistoryCourierProjectionV2).version === 2
  && (value.courier.initialCourierId === undefined || validId(value.courier.initialCourierId))
  && (value.courier.activeCourierId === undefined || validId(value.courier.activeCourierId))
  && (value.courier.initialCourierId === undefined ? value.courier.activeCourierId === undefined : value.courier.activeCourierId !== undefined)
  && (value.navigation === undefined || (record(value.navigation) && (hasOnlyKeys(value.navigation, ['version']) || hasOnlyKeys(value.navigation, ['version', 'courierId', 'coordinate'])) && value.navigation.version === 1 && (value.navigation.courierId === undefined || validId(value.navigation.courierId)) && (value.navigation.coordinate === undefined || (record(value.navigation.coordinate) && hasOnlyKeys(value.navigation.coordinate, ['column', 'row']) && safeInteger(value.navigation.coordinate.column) && safeInteger(value.navigation.coordinate.row)))))
  && record(value.people) && hasOnlyKeys(value.people, ['version', 'records']) && value.people.version === 5 && Array.isArray(value.people.records)
  && record(value.temporal) && record(value.simulation) && record(value.era) && record(value.delegation) && record(value.autonomy) && record(value.socialMemory)

/** State-v14 replay had current courier continuity but no prop-action projection. */
const legacyProjectionV6Shape = (value: unknown): boolean => record(value)
  && (hasOnlyKeys(value, ['version', 'courier', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory']) || hasOnlyKeys(value, ['version', 'courier', 'navigation', 'people', 'temporal', 'simulation', 'era', 'delegation', 'autonomy', 'socialMemory']))
  && value.version === LEGACY_CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION
  && validProjectionCourier(value.courier)
  && (value.navigation === undefined || (record(value.navigation) && (hasOnlyKeys(value.navigation, ['version']) || hasOnlyKeys(value.navigation, ['version', 'courierId', 'coordinate'])) && value.navigation.version === 1 && (value.navigation.courierId === undefined || validId(value.navigation.courierId)) && (value.navigation.coordinate === undefined || (record(value.navigation.coordinate) && hasOnlyKeys(value.navigation.coordinate, ['column', 'row']) && safeInteger(value.navigation.coordinate.column) && safeInteger(value.navigation.coordinate.row)))))
  && record(value.people) && hasOnlyKeys(value.people, ['version', 'records']) && value.people.version === 5 && Array.isArray(value.people.records)
  && record(value.temporal) && record(value.simulation) && record(value.era) && record(value.delegation) && record(value.autonomy) && record(value.socialMemory)

export const causalReplayProjection = (value: Pick<CausalReplayProjection, 'courier' | 'people' | 'temporal' | 'simulation' | 'era' | 'delegation' | 'autonomy' | 'socialMemory' | 'jomon' | 'settlementTrading'> & Partial<Pick<CausalReplayProjection, 'navigation'>>): CausalReplayProjection => ({
  version: CAUSAL_HISTORY_REPLAY_PROJECTION_VERSION,
  courier: structuredClone(value.courier),
  ...(value.navigation === undefined ? {} : { navigation: structuredClone(value.navigation) }),
  people: structuredClone(value.people),
  temporal: structuredClone(value.temporal),
  simulation: structuredClone(value.simulation),
  era: structuredClone(value.era),
  delegation: structuredClone(value.delegation),
  autonomy: structuredClone(value.autonomy),
  socialMemory: structuredClone(value.socialMemory),
  jomon: structuredClone(value.jomon),
  settlementTrading: structuredClone(value.settlementTrading)
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

/**
 * Rebinds only a validated checkpoint projection during an explicit world
 * schema conversion. Commands, their tokens, and compacted evidence remain
 * untouched; callers must still prove full replay after conversion.
 */
export const rebaseCausalHistoryCheckpoint = (
  context: CausalHistoryContext,
  state: CausalHistoryState,
  projection: CausalReplayProjection
): CausalHistoryState => {
  const diagnostics = validateCausalHistoryState(context, state)
  if (diagnostics.length || !projectionShape(projection)) throw new CausalHistoryContractError(diagnostics.length ? diagnostics : [issue('causal-history:checkpoint', 'causal-history.invalid-checkpoint')])
  const rebased = {
    version: CAUSAL_HISTORY_CONTRACT_VERSION,
    checkpoint: checkpointFor(context, state.checkpoint.sequence, projection),
    tail: structuredClone(state.tail),
    compactedSegments: structuredClone(state.compactedSegments),
    contentSafetyAudit: structuredClone(state.contentSafetyAudit)
  } satisfies CausalHistoryState
  const result = validateCausalHistoryState(context, rebased)
  if (result.length) throw new CausalHistoryContractError(result)
  return rebased
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

/**
 * Converts a fully validated v4 journal into the current checkpoint contract.
 * Only an explicit envelope upgrade may call this.  Historic compacted
 * summaries remain bounded accounting records; their identities are rebased
 * because the segment token includes its contract version.
 */
export const upgradeLegacyCausalHistoryStateV4 = (
  context: CausalHistoryContext,
  value: unknown,
  checkpointProjection: CausalReplayProjection
): CausalHistoryState => {
  const diagnostics = validateLegacyCausalHistoryStateV4(context, value)
  if (diagnostics.length || !projectionShape(checkpointProjection) || !record(value) || !record(value.checkpoint) || !Array.isArray(value.tail) || !Array.isArray(value.compactedSegments)) throw new CausalHistoryContractError(diagnostics.length ? diagnostics : [issue('causal-history:legacy-upgrade', 'causal-history.invalid-checkpoint')])
  const tail = value.tail.map(command => ({ ...structuredClone(command as Record<string, unknown>), version: CAUSAL_HISTORY_CONTRACT_VERSION })) as CausalCommandEvent[]
  const compactedSegments = value.compactedSegments.map(segment => {
    const source = segment as Record<string, unknown>
    const draft = {
      version: CAUSAL_HISTORY_SEGMENT_VERSION,
      sequenceStart: source.sequenceStart as number,
      sequenceEnd: source.sequenceEnd as number,
      worldTimeStart: source.worldTimeStart as number,
      worldTimeEnd: source.worldTimeEnd as number,
      commandKinds: currentCountsForLegacy(source.commandKinds as CausalHistoryCommandKindCounts),
      provenanceDigest: source.provenanceDigest as string,
      stateDigest: source.stateDigest as string
    } satisfies Omit<CausalHistoryCompactedSegment, 'id' | 'token'>
    return {
      ...draft,
      id: segmentIdFor(context, draft.sequenceStart, draft.sequenceEnd, draft.stateDigest),
      token: segmentTokenFor(context, draft)
    }
  })
  const upgraded = {
    version: CAUSAL_HISTORY_CONTRACT_VERSION,
    checkpoint: checkpointFor(context, value.checkpoint.sequence as number, checkpointProjection),
    tail,
    compactedSegments,
    contentSafetyAudit: audit(tail)
  } satisfies CausalHistoryState
  const currentDiagnostics = validateCausalHistoryState(context, upgraded)
  if (currentDiagnostics.length) throw new CausalHistoryContractError(currentDiagnostics)
  return upgraded
}

/** Rebinds a validated v4 checkpoint while retaining its v4 journal contract. */
export const rebaseLegacyCausalHistoryCheckpointV4 = (
  context: CausalHistoryContext,
  value: unknown,
  projection: unknown
): unknown => {
  const diagnostics = validateLegacyCausalHistoryStateV4(context, value)
  if (diagnostics.length || !legacyProjectionV6Shape(projection) || !record(value) || !record(value.checkpoint)) throw new CausalHistoryContractError(diagnostics.length ? diagnostics : [issue('causal-history:legacy-rebase', 'causal-history.invalid-checkpoint')])
  const typedProjection = structuredClone(projection)
  const stateDigest = causalDigestFor('causal-replay-projection', typedProjection)
  const sequence = value.checkpoint.sequence as number
  const atWorldTime = (typedProjection as { temporal: { worldTime: number } }).temporal.worldTime
  const rebased = {
    version: LEGACY_CAUSAL_HISTORY_CONTRACT_VERSION,
    checkpoint: {
      version: LEGACY_CAUSAL_HISTORY_CHECKPOINT_VERSION,
      id: checkpointIdFor(context, sequence, stateDigest),
      token: checkpointTokenFor(context, sequence, atWorldTime, stateDigest),
      sequence,
      atWorldTime,
      provenanceDigest: context.creationDigest,
      stateDigest,
      projection: typedProjection
    },
    tail: structuredClone(value.tail),
    compactedSegments: structuredClone(value.compactedSegments),
    contentSafetyAudit: structuredClone(value.contentSafetyAudit)
  }
  const result = validateLegacyCausalHistoryStateV4(context, rebased)
  if (result.length) throw new CausalHistoryContractError(result)
  return rebased
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
  payload: InitialCourierSelectedCommand['payload'] | TavernCourierSwitchedCommand['payload'] | VesselStationReadoutRecordedCommand['payload'] | VesselCargoLoadedCommand['payload'] | VesselCargoUnloadedCommand['payload'] | VesselCargoFailureResolvedCommand['payload'] | VesselCargoRecoveredCommand['payload'] | SettlementTradeAcceptedCommand['payload'] | SettlementTradeDeliveredCommand['payload'] | CourierLossResolvedCommand['payload'] | TimeBearingActionCommand['payload'] | DeckMovedCommand['payload'] | DurableJomonGrowthCommand['payload'] | DelegationOfferedCommand['payload'] | DelegationInterruptedCommand['payload']
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
  if (kind === 'tavern-courier-switched') return { ...common, kind, payload: structuredClone(payload as TavernCourierSwitchedCommand['payload']) }
  if (kind === 'vessel-station-readout-recorded') return { ...common, kind, payload: structuredClone(payload as VesselStationReadoutRecordedCommand['payload']) }
  if (kind === 'vessel-cargo-loaded') return { ...common, kind, payload: structuredClone(payload as VesselCargoLoadedCommand['payload']) }
  if (kind === 'vessel-cargo-unloaded') return { ...common, kind, payload: structuredClone(payload as VesselCargoUnloadedCommand['payload']) }
  if (kind === 'vessel-cargo-failure-resolved') return { ...common, kind, payload: structuredClone(payload as VesselCargoFailureResolvedCommand['payload']) }
  if (kind === 'vessel-cargo-recovered') return { ...common, kind, payload: structuredClone(payload as VesselCargoRecoveredCommand['payload']) }
  if (kind === 'settlement-trade-accepted') return { ...common, kind, payload: structuredClone(payload as SettlementTradeAcceptedCommand['payload']) }
  if (kind === 'settlement-trade-refused') return { ...common, kind, payload: structuredClone(payload as SettlementTradeRefusedCommand['payload']) }
  if (kind === 'settlement-trade-delivered') return { ...common, kind, payload: structuredClone(payload as SettlementTradeDeliveredCommand['payload']) }
  if (kind === 'courier-loss-resolved') return { ...common, kind, payload: structuredClone(payload as CourierLossResolvedCommand['payload']) }
  if (kind === 'time-bearing-action') return { ...common, kind, payload: structuredClone(payload as TimeBearingActionCommand['payload']) }
  if (kind === 'deck-moved') return { ...common, kind, payload: structuredClone(payload as DeckMovedCommand['payload']) }
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
  const count = (key: keyof CausalHistoryCommandKindCounts): number => (left.commandKinds[key] ?? 0) + (right.commandKinds[key] ?? 0)
  const draft: Omit<CausalHistoryCompactedSegment, 'id' | 'token'> = {
    version: CAUSAL_HISTORY_SEGMENT_VERSION,
    sequenceStart: left.sequenceStart,
    sequenceEnd: right.sequenceEnd,
    worldTimeStart: left.worldTimeStart,
    worldTimeEnd: right.worldTimeEnd,
    commandKinds: {
      initialCourierSelected: count('initialCourierSelected'),
      tavernCourierSwitched: count('tavernCourierSwitched'),
      vesselStationReadoutRecorded: count('vesselStationReadoutRecorded'),
      vesselCargoLoaded: count('vesselCargoLoaded'),
      vesselCargoUnloaded: count('vesselCargoUnloaded'),
      vesselCargoFailureResolved: count('vesselCargoFailureResolved'),
      vesselCargoRecovered: count('vesselCargoRecovered'),
      settlementTradeAccepted: count('settlementTradeAccepted'),
      settlementTradeRefused: count('settlementTradeRefused'),
      settlementTradeDelivered: count('settlementTradeDelivered'),
      courierLossResolved: count('courierLossResolved'),
      timeBearingAction: count('timeBearingAction'),
      durableJomonGrowth: count('durableJomonGrowth'),
      delegationOffered: count('delegationOffered'),
      delegationInterrupted: count('delegationInterrupted'),
      deckMoved: count('deckMoved')
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
  if (!record(value) || !hasOnlyKeys(value, ['version', 'sequence', 'id', 'token', 'kind', 'payload', 'contentSafety']) || value.version !== CAUSAL_HISTORY_CONTRACT_VERSION || !safeInteger(value.sequence) || !validId(value.id) || typeof value.token !== 'string' || !['initial-courier-selected', 'tavern-courier-switched', 'vessel-station-readout-recorded', 'vessel-cargo-loaded', 'vessel-cargo-unloaded', 'vessel-cargo-failure-resolved', 'vessel-cargo-recovered', 'settlement-trade-accepted', 'settlement-trade-refused', 'settlement-trade-delivered', 'courier-loss-resolved', 'time-bearing-action', 'deck-moved', 'durable-jomon-growth', 'delegation-offered', 'delegation-interrupted'].includes(String(value.kind)) || !commandPayloadIsShaped(value.kind as CausalCommandKind, value.payload)) return false
  const kind = value.kind as CausalCommandKind
  const payload = value.payload as InitialCourierSelectedCommand['payload'] | CourierLossResolvedCommand['payload'] | TimeBearingActionCommand['payload'] | DurableJomonGrowthCommand['payload']
  const expectedId = commandIdFor(context, value.sequence, kind, value.payload)
  const expectedToken = commandTokenFor(context, value.sequence, kind, value.payload)
  if (value.id !== expectedId || value.token !== expectedToken || !same(value.contentSafety, commandClassification(kind, payload))) return false
  return validateMedievalContentSafety([{ id: `causal-history:${value.id}`, domain: 'event', classification: value.contentSafety }]).status === 'accepted'
}

const validCheckpoint = (context: CausalHistoryContext, value: unknown): value is CausalHistoryCheckpoint => {
  const projection = record(value) ? value.projection : undefined
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'token', 'sequence', 'atWorldTime', 'provenanceDigest', 'stateDigest', 'projection']) || value.version !== CAUSAL_HISTORY_CHECKPOINT_VERSION || !validId(value.id) || typeof value.token !== 'string' || !safeInteger(value.sequence) || !safeInteger(value.atWorldTime) || value.provenanceDigest !== context.creationDigest || typeof value.stateDigest !== 'string' || !projectionShape(projection) || !record(projection) || !record(projection.temporal) || projection.temporal.worldTime !== value.atWorldTime || causalDigestFor('causal-replay-projection', projection) !== value.stateDigest) return false
  return value.id === checkpointIdFor(context, value.sequence, value.stateDigest) && value.token === checkpointTokenFor(context, value.sequence, value.atWorldTime, value.stateDigest)
}

const validCounts = (value: unknown): value is CausalHistoryCommandKindCounts => record(value)
  && (hasOnlyKeys(value, ['initialCourierSelected', 'timeBearingAction', 'durableJomonGrowth', 'delegationOffered', 'delegationInterrupted'])
    || hasOnlyKeys(value, ['initialCourierSelected', 'timeBearingAction', 'durableJomonGrowth', 'delegationOffered', 'delegationInterrupted', 'deckMoved'])
    || hasOnlyKeys(value, ['initialCourierSelected', 'tavernCourierSwitched', 'timeBearingAction', 'durableJomonGrowth', 'delegationOffered', 'delegationInterrupted', 'deckMoved'])
    || hasOnlyKeys(value, ['initialCourierSelected', 'tavernCourierSwitched', 'courierLossResolved', 'timeBearingAction', 'durableJomonGrowth', 'delegationOffered', 'delegationInterrupted', 'deckMoved'])
    || hasOnlyKeys(value, ['initialCourierSelected', 'tavernCourierSwitched', 'vesselStationReadoutRecorded', 'courierLossResolved', 'timeBearingAction', 'durableJomonGrowth', 'delegationOffered', 'delegationInterrupted', 'deckMoved'])
    || hasOnlyKeys(value, ['initialCourierSelected', 'tavernCourierSwitched', 'vesselStationReadoutRecorded', 'vesselCargoLoaded', 'vesselCargoUnloaded', 'vesselCargoFailureResolved', 'vesselCargoRecovered', 'courierLossResolved', 'timeBearingAction', 'durableJomonGrowth', 'delegationOffered', 'delegationInterrupted', 'deckMoved'])
    || hasOnlyKeys(value, ['initialCourierSelected', 'tavernCourierSwitched', 'vesselStationReadoutRecorded', 'vesselCargoLoaded', 'vesselCargoUnloaded', 'vesselCargoFailureResolved', 'vesselCargoRecovered', 'settlementTradeAccepted', 'settlementTradeRefused', 'settlementTradeDelivered', 'courierLossResolved', 'timeBearingAction', 'durableJomonGrowth', 'delegationOffered', 'delegationInterrupted', 'deckMoved']))
  && safeInteger(value.initialCourierSelected) && safeInteger(value.timeBearingAction) && safeInteger(value.durableJomonGrowth) && safeInteger(value.delegationOffered) && safeInteger(value.delegationInterrupted) && (value.deckMoved === undefined || safeInteger(value.deckMoved)) && (value.tavernCourierSwitched === undefined || safeInteger(value.tavernCourierSwitched)) && (value.vesselStationReadoutRecorded === undefined || safeInteger(value.vesselStationReadoutRecorded)) && (value.vesselCargoLoaded === undefined || safeInteger(value.vesselCargoLoaded)) && (value.vesselCargoUnloaded === undefined || safeInteger(value.vesselCargoUnloaded)) && (value.vesselCargoFailureResolved === undefined || safeInteger(value.vesselCargoFailureResolved)) && (value.vesselCargoRecovered === undefined || safeInteger(value.vesselCargoRecovered)) && (value.settlementTradeAccepted === undefined || safeInteger(value.settlementTradeAccepted)) && (value.settlementTradeRefused === undefined || safeInteger(value.settlementTradeRefused)) && (value.settlementTradeDelivered === undefined || safeInteger(value.settlementTradeDelivered)) && (value.courierLossResolved === undefined || safeInteger(value.courierLossResolved))

const validCurrentCounts = (value: unknown): value is CausalHistoryCommandKindCounts => validCounts(value)
  && record(value)
  && hasOnlyKeys(value, ['initialCourierSelected', 'tavernCourierSwitched', 'vesselStationReadoutRecorded', 'vesselCargoLoaded', 'vesselCargoUnloaded', 'vesselCargoFailureResolved', 'vesselCargoRecovered', 'settlementTradeAccepted', 'settlementTradeRefused', 'settlementTradeDelivered', 'courierLossResolved', 'timeBearingAction', 'durableJomonGrowth', 'delegationOffered', 'delegationInterrupted', 'deckMoved'])

/** Read-only upgrade rebases closed historic count shapes to the current one. */
const currentCountsForLegacy = (value: CausalHistoryCommandKindCounts): CausalHistoryCommandKindCounts => ({
  initialCourierSelected: value.initialCourierSelected,
  tavernCourierSwitched: value.tavernCourierSwitched ?? 0,
  vesselStationReadoutRecorded: value.vesselStationReadoutRecorded ?? 0,
  vesselCargoLoaded: value.vesselCargoLoaded ?? 0,
  vesselCargoUnloaded: value.vesselCargoUnloaded ?? 0,
  vesselCargoFailureResolved: value.vesselCargoFailureResolved ?? 0,
  vesselCargoRecovered: value.vesselCargoRecovered ?? 0,
  settlementTradeAccepted: value.settlementTradeAccepted ?? 0,
  settlementTradeRefused: value.settlementTradeRefused ?? 0,
  settlementTradeDelivered: value.settlementTradeDelivered ?? 0,
  courierLossResolved: value.courierLossResolved ?? 0,
  timeBearingAction: value.timeBearingAction,
  durableJomonGrowth: value.durableJomonGrowth,
  delegationOffered: value.delegationOffered,
  delegationInterrupted: value.delegationInterrupted,
  deckMoved: value.deckMoved ?? 0
})
const validSegment = (context: CausalHistoryContext, value: unknown): value is CausalHistoryCompactedSegment => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'id', 'token', 'sequenceStart', 'sequenceEnd', 'worldTimeStart', 'worldTimeEnd', 'commandKinds', 'provenanceDigest', 'stateDigest']) || value.version !== CAUSAL_HISTORY_SEGMENT_VERSION || !validId(value.id) || typeof value.token !== 'string' || !safeInteger(value.sequenceStart) || !safeInteger(value.sequenceEnd) || value.sequenceStart < 1 || value.sequenceEnd < value.sequenceStart || !safeInteger(value.worldTimeStart) || !safeInteger(value.worldTimeEnd) || value.worldTimeEnd < value.worldTimeStart || !validCurrentCounts(value.commandKinds) || value.provenanceDigest !== context.creationDigest || typeof value.stateDigest !== 'string') return false
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
        const typedCommand = record(command) && typeof command.kind === 'string' && ['initial-courier-selected', 'tavern-courier-switched', 'vessel-station-readout-recorded', 'vessel-cargo-loaded', 'vessel-cargo-unloaded', 'vessel-cargo-failure-resolved', 'vessel-cargo-recovered', 'settlement-trade-accepted', 'settlement-trade-refused', 'settlement-trade-delivered', 'courier-loss-resolved', 'time-bearing-action', 'deck-moved', 'durable-jomon-growth', 'delegation-offered', 'delegation-interrupted'].includes(command.kind)
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

/**
 * Validates a complete v4 journal before a read-only envelope upgrade.  It is
 * intentionally not accepted by the current replay path: callers must rebase
 * its checkpoint into the v7 projection before using it as live mutable state.
 */
export const validateLegacyCausalHistoryStateV4 = (context: CausalHistoryContext, value: unknown): readonly CausalHistoryDiagnostic[] => {
  if (!validContext(context)) return [issue('causal-history:context', 'causal-history.invalid-context')]
  if (!record(value) || !hasOnlyKeys(value, ['version', 'checkpoint', 'tail', 'compactedSegments', 'contentSafetyAudit'])) return [issue('causal-history', 'causal-history.malformed-state')]
  const issues: CausalHistoryDiagnostic[] = []
  if (value.version !== LEGACY_CAUSAL_HISTORY_CONTRACT_VERSION) issues.push(issue('causal-history', 'causal-history.invalid-version'))
  const checkpoint = value.checkpoint
  const validLegacyCheckpoint = record(checkpoint)
    && hasOnlyKeys(checkpoint, ['version', 'id', 'token', 'sequence', 'atWorldTime', 'provenanceDigest', 'stateDigest', 'projection'])
    && checkpoint.version === LEGACY_CAUSAL_HISTORY_CHECKPOINT_VERSION
    && validId(checkpoint.id)
    && typeof checkpoint.token === 'string'
    && safeInteger(checkpoint.sequence)
    && safeInteger(checkpoint.atWorldTime)
    && checkpoint.provenanceDigest === context.creationDigest
    && typeof checkpoint.stateDigest === 'string'
    && (legacyProjectionV6Shape(checkpoint.projection) || legacyProjectionV5Shape(checkpoint.projection) || legacyProjectionShape(checkpoint.projection))
    && record(checkpoint.projection)
    && record(checkpoint.projection.temporal)
    && checkpoint.projection.temporal.worldTime === checkpoint.atWorldTime
    && causalDigestFor('causal-replay-projection', checkpoint.projection) === checkpoint.stateDigest
    && checkpoint.id === checkpointIdFor(context, checkpoint.sequence, checkpoint.stateDigest)
    && checkpoint.token === checkpointTokenFor(context, checkpoint.sequence, checkpoint.atWorldTime, checkpoint.stateDigest)
  if (!validLegacyCheckpoint) issues.push(issue('causal-history:checkpoint', 'causal-history.invalid-checkpoint'))

  const validLegacyCommand = (command: unknown): command is Record<string, unknown> => {
    if (!record(command) || !hasOnlyKeys(command, ['version', 'sequence', 'id', 'token', 'kind', 'payload', 'contentSafety']) || command.version !== LEGACY_CAUSAL_HISTORY_CONTRACT_VERSION || !safeInteger(command.sequence) || !validId(command.id) || typeof command.token !== 'string') return false
    const kind = command.kind
    if (typeof kind !== 'string' || !['initial-courier-selected', 'tavern-courier-switched', 'courier-loss-resolved', 'time-bearing-action', 'deck-moved', 'durable-jomon-growth', 'delegation-offered', 'delegation-interrupted'].includes(kind) || !commandPayloadIsShaped(kind as CausalCommandKind, command.payload)) return false
    const typedKind = kind as CausalCommandKind
    return command.id === commandIdFor(context, command.sequence, typedKind, command.payload)
      && command.token === commandTokenFor(context, command.sequence, typedKind, command.payload)
      && same(command.contentSafety, commandClassification(typedKind, command.payload as InitialCourierSelectedCommand['payload']))
      && validateMedievalContentSafety([{ id: `causal-history:${command.id}`, domain: 'event', classification: command.contentSafety }]).status === 'accepted'
  }
  if (!Array.isArray(value.tail)) issues.push(issue('causal-history:tail', 'causal-history.invalid-command'))
  else {
    if (value.tail.length > CAUSAL_HISTORY_LIMITS.retainedCommands) issues.push(issue('causal-history:tail', 'causal-history.command-limit'))
    const ids = new Set<string>()
    for (const command of value.tail) {
      const id = record(command) && typeof command.id === 'string' ? command.id : 'causal-history:command'
      if (!validLegacyCommand(command)) issues.push(issue(id, 'causal-history.invalid-command'))
      if (record(command) && typeof command.id === 'string') {
        if (ids.has(command.id)) issues.push(issue(command.id, 'causal-history.duplicate-command-id'))
        ids.add(command.id)
      }
    }
    const checkpointSequence = record(checkpoint) && safeInteger(checkpoint.sequence) ? checkpoint.sequence : 0
    if (value.tail.some((command, index) => !record(command) || command.sequence !== checkpointSequence + index + 1)) issues.push(issue('causal-history:tail', 'causal-history.invalid-command-order'))
  }
  const validLegacySegment = (segment: unknown): segment is Record<string, unknown> => {
    if (!record(segment) || !hasOnlyKeys(segment, ['version', 'id', 'token', 'sequenceStart', 'sequenceEnd', 'worldTimeStart', 'worldTimeEnd', 'commandKinds', 'provenanceDigest', 'stateDigest']) || segment.version !== LEGACY_CAUSAL_HISTORY_SEGMENT_VERSION || !validId(segment.id) || typeof segment.token !== 'string' || !safeInteger(segment.sequenceStart) || !safeInteger(segment.sequenceEnd) || segment.sequenceStart < 1 || segment.sequenceEnd < segment.sequenceStart || !safeInteger(segment.worldTimeStart) || !safeInteger(segment.worldTimeEnd) || segment.worldTimeEnd < segment.worldTimeStart || !validCounts(segment.commandKinds) || segment.provenanceDigest !== context.creationDigest || typeof segment.stateDigest !== 'string') return false
    const draft = {
      version: segment.version,
      sequenceStart: segment.sequenceStart,
      sequenceEnd: segment.sequenceEnd,
      worldTimeStart: segment.worldTimeStart,
      worldTimeEnd: segment.worldTimeEnd,
      commandKinds: segment.commandKinds as CausalHistoryCommandKindCounts,
      provenanceDigest: segment.provenanceDigest,
      stateDigest: segment.stateDigest
    } satisfies CausalSegmentTokenDraft
    return segment.id === segmentIdFor(context, segment.sequenceStart, segment.sequenceEnd, segment.stateDigest)
      && segment.token === segmentTokenFor(context, draft)
  }
  if (!Array.isArray(value.compactedSegments)) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment'))
  else {
    if (value.compactedSegments.length > CAUSAL_HISTORY_LIMITS.compactedSegments) issues.push(issue('causal-history:segments', 'causal-history.segment-limit'))
    let expectedStart = 1
    for (const segment of value.compactedSegments) {
      const id = record(segment) && typeof segment.id === 'string' ? segment.id : 'causal-history:segment'
      if (!validLegacySegment(segment)) issues.push(issue(id, 'causal-history.invalid-segment-token'))
      if (!record(segment) || segment.sequenceStart !== expectedStart) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment-order'))
      if (record(segment) && safeInteger(segment.sequenceEnd)) expectedStart = segment.sequenceEnd + 1
    }
    const checkpointSequence = record(checkpoint) && safeInteger(checkpoint.sequence) ? checkpoint.sequence : 0
    if (expectedStart !== checkpointSequence + 1) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment-order'))
  }
  try {
    if (!Array.isArray(value.tail) || !contentSafetyAuditMatches(contentRecordsFor(value.tail as CausalCommandEvent[]), value.contentSafetyAudit)) issues.push(issue('causal-history:content-safety', 'causal-history.invalid-content-audit'))
  } catch { issues.push(issue('causal-history:content-safety', 'causal-history.invalid-content-audit')) }
  return canonicalIssues(issues)
}

/**
 * Strict v5 journal validation for the cargo schema conversion. It accepts
 * only the former prop-action projection and never treats that source as live.
 */
export const validateLegacyCausalHistoryStateV5 = (context: CausalHistoryContext, value: unknown): readonly CausalHistoryDiagnostic[] => {
  if (!validContext(context)) return [issue('causal-history:context', 'causal-history.invalid-context')]
  if (!record(value) || !hasOnlyKeys(value, ['version', 'checkpoint', 'tail', 'compactedSegments', 'contentSafetyAudit'])) return [issue('causal-history', 'causal-history.malformed-state')]
  const issues: CausalHistoryDiagnostic[] = []
  if (value.version !== LEGACY_CARGO_CAUSAL_HISTORY_CONTRACT_VERSION) issues.push(issue('causal-history', 'causal-history.invalid-version'))
  const checkpoint = value.checkpoint
  const validCheckpointV5 = record(checkpoint)
    && hasOnlyKeys(checkpoint, ['version', 'id', 'token', 'sequence', 'atWorldTime', 'provenanceDigest', 'stateDigest', 'projection'])
    && checkpoint.version === LEGACY_CARGO_CAUSAL_HISTORY_CHECKPOINT_VERSION
    && validId(checkpoint.id) && typeof checkpoint.token === 'string'
    && safeInteger(checkpoint.sequence) && safeInteger(checkpoint.atWorldTime)
    && checkpoint.provenanceDigest === context.creationDigest
    && typeof checkpoint.stateDigest === 'string'
    && legacyProjectionV7Shape(checkpoint.projection)
    && record(checkpoint.projection) && record(checkpoint.projection.temporal)
    && checkpoint.projection.temporal.worldTime === checkpoint.atWorldTime
    && causalDigestFor('causal-replay-projection', checkpoint.projection) === checkpoint.stateDigest
    && checkpoint.id === checkpointIdFor(context, checkpoint.sequence, checkpoint.stateDigest)
    && checkpoint.token === checkpointTokenFor(context, checkpoint.sequence, checkpoint.atWorldTime, checkpoint.stateDigest)
  if (!validCheckpointV5) issues.push(issue('causal-history:checkpoint', 'causal-history.invalid-checkpoint'))

  const v5Kinds = ['initial-courier-selected', 'tavern-courier-switched', 'vessel-station-readout-recorded', 'courier-loss-resolved', 'time-bearing-action', 'deck-moved', 'durable-jomon-growth', 'delegation-offered', 'delegation-interrupted'] as const
  const validCommandV5 = (command: unknown): command is Record<string, unknown> => {
    if (!record(command) || !hasOnlyKeys(command, ['version', 'sequence', 'id', 'token', 'kind', 'payload', 'contentSafety']) || command.version !== LEGACY_CARGO_CAUSAL_HISTORY_CONTRACT_VERSION || !safeInteger(command.sequence) || !validId(command.id) || typeof command.token !== 'string' || typeof command.kind !== 'string' || !v5Kinds.includes(command.kind as typeof v5Kinds[number]) || !commandPayloadIsShaped(command.kind as CausalCommandKind, command.payload)) return false
    const kind = command.kind as CausalCommandKind
    return command.id === commandIdFor(context, command.sequence, kind, command.payload)
      && command.token === commandTokenFor(context, command.sequence, kind, command.payload)
      && same(command.contentSafety, commandClassification(kind, command.payload as InitialCourierSelectedCommand['payload']))
      && validateMedievalContentSafety([{ id: `causal-history:${command.id}`, domain: 'event', classification: command.contentSafety }]).status === 'accepted'
  }
  if (!Array.isArray(value.tail)) issues.push(issue('causal-history:tail', 'causal-history.invalid-command'))
  else {
    if (value.tail.length > CAUSAL_HISTORY_LIMITS.retainedCommands) issues.push(issue('causal-history:tail', 'causal-history.command-limit'))
    const ids = new Set<string>()
    for (const command of value.tail) {
      const id = record(command) && typeof command.id === 'string' ? command.id : 'causal-history:command'
      if (!validCommandV5(command)) issues.push(issue(id, 'causal-history.invalid-command'))
      if (record(command) && typeof command.id === 'string') {
        if (ids.has(command.id)) issues.push(issue(command.id, 'causal-history.duplicate-command-id'))
        ids.add(command.id)
      }
    }
    const checkpointSequence = record(checkpoint) && safeInteger(checkpoint.sequence) ? checkpoint.sequence : 0
    if (value.tail.some((command, index) => !record(command) || command.sequence !== checkpointSequence + index + 1)) issues.push(issue('causal-history:tail', 'causal-history.invalid-command-order'))
  }
  const validSegmentV5 = (segment: unknown): segment is Record<string, unknown> => {
    if (!record(segment) || !hasOnlyKeys(segment, ['version', 'id', 'token', 'sequenceStart', 'sequenceEnd', 'worldTimeStart', 'worldTimeEnd', 'commandKinds', 'provenanceDigest', 'stateDigest']) || segment.version !== LEGACY_CARGO_CAUSAL_HISTORY_SEGMENT_VERSION || !validId(segment.id) || typeof segment.token !== 'string' || !safeInteger(segment.sequenceStart) || !safeInteger(segment.sequenceEnd) || segment.sequenceStart < 1 || segment.sequenceEnd < segment.sequenceStart || !safeInteger(segment.worldTimeStart) || !safeInteger(segment.worldTimeEnd) || segment.worldTimeEnd < segment.worldTimeStart || !validCounts(segment.commandKinds) || segment.provenanceDigest !== context.creationDigest || typeof segment.stateDigest !== 'string') return false
    const draft: CausalSegmentTokenDraft = {
      version: segment.version,
      sequenceStart: segment.sequenceStart,
      sequenceEnd: segment.sequenceEnd,
      worldTimeStart: segment.worldTimeStart,
      worldTimeEnd: segment.worldTimeEnd,
      commandKinds: segment.commandKinds as CausalHistoryCommandKindCounts,
      provenanceDigest: segment.provenanceDigest,
      stateDigest: segment.stateDigest
    }
    return segment.id === segmentIdFor(context, segment.sequenceStart, segment.sequenceEnd, segment.stateDigest)
      && segment.token === segmentTokenFor(context, draft)
  }
  if (!Array.isArray(value.compactedSegments)) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment'))
  else {
    if (value.compactedSegments.length > CAUSAL_HISTORY_LIMITS.compactedSegments) issues.push(issue('causal-history:segments', 'causal-history.segment-limit'))
    let expectedStart = 1
    for (const segment of value.compactedSegments) {
      const id = record(segment) && typeof segment.id === 'string' ? segment.id : 'causal-history:segment'
      if (!validSegmentV5(segment)) issues.push(issue(id, 'causal-history.invalid-segment-token'))
      if (!record(segment) || segment.sequenceStart !== expectedStart) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment-order'))
      if (record(segment) && safeInteger(segment.sequenceEnd)) expectedStart = segment.sequenceEnd + 1
    }
    const checkpointSequence = record(checkpoint) && safeInteger(checkpoint.sequence) ? checkpoint.sequence : 0
    if (expectedStart !== checkpointSequence + 1) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment-order'))
  }
  try {
    if (!Array.isArray(value.tail) || !contentSafetyAuditMatches(contentRecordsFor(value.tail as CausalCommandEvent[]), value.contentSafetyAudit)) issues.push(issue('causal-history:content-safety', 'causal-history.invalid-content-audit'))
  } catch { issues.push(issue('causal-history:content-safety', 'causal-history.invalid-content-audit')) }
  return canonicalIssues(issues)
}

/** Rebinds fully validated v5 journal evidence to the v6/cargo projection. */
export const upgradeLegacyCausalHistoryStateV5 = (
  context: CausalHistoryContext,
  value: unknown,
  checkpointProjection: CausalReplayProjection
): CausalHistoryState => {
  const diagnostics = validateLegacyCausalHistoryStateV5(context, value)
  if (diagnostics.length || !projectionShape(checkpointProjection) || !record(value) || !record(value.checkpoint) || !Array.isArray(value.tail) || !Array.isArray(value.compactedSegments)) throw new CausalHistoryContractError(diagnostics.length ? diagnostics : [issue('causal-history:legacy-upgrade', 'causal-history.invalid-checkpoint')])
  const tail = value.tail.map(command => ({ ...structuredClone(command as Record<string, unknown>), version: CAUSAL_HISTORY_CONTRACT_VERSION })) as CausalCommandEvent[]
  const compactedSegments = value.compactedSegments.map(segment => {
    const source = segment as Record<string, unknown>
    const draft: Omit<CausalHistoryCompactedSegment, 'id' | 'token'> = {
      version: CAUSAL_HISTORY_SEGMENT_VERSION,
      sequenceStart: source.sequenceStart as number,
      sequenceEnd: source.sequenceEnd as number,
      worldTimeStart: source.worldTimeStart as number,
      worldTimeEnd: source.worldTimeEnd as number,
      commandKinds: currentCountsForLegacy(source.commandKinds as CausalHistoryCommandKindCounts),
      provenanceDigest: source.provenanceDigest as string,
      stateDigest: source.stateDigest as string
    }
    return { ...draft, id: segmentIdFor(context, draft.sequenceStart, draft.sequenceEnd, draft.stateDigest), token: segmentTokenFor(context, draft) }
  })
  const upgraded = {
    version: CAUSAL_HISTORY_CONTRACT_VERSION,
    checkpoint: checkpointFor(context, value.checkpoint.sequence as number, checkpointProjection),
    tail,
    compactedSegments,
    contentSafetyAudit: audit(tail)
  } satisfies CausalHistoryState
  const currentDiagnostics = validateCausalHistoryState(context, upgraded)
  if (currentDiagnostics.length) throw new CausalHistoryContractError(currentDiagnostics)
  return upgraded
}

/**
 * Strict v6 journal validation for the settlement-contract conversion. The
 * source has bounded cargo evidence but no local settlement-trade projection.
 * It is accepted only by an explicit full-envelope read-only upgrade.
 */
export const validateLegacyCausalHistoryStateV6 = (context: CausalHistoryContext, value: unknown): readonly CausalHistoryDiagnostic[] => {
  if (!validContext(context)) return [issue('causal-history:context', 'causal-history.invalid-context')]
  if (!record(value) || !hasOnlyKeys(value, ['version', 'checkpoint', 'tail', 'compactedSegments', 'contentSafetyAudit'])) return [issue('causal-history', 'causal-history.malformed-state')]
  const issues: CausalHistoryDiagnostic[] = []
  if (value.version !== LEGACY_SETTLEMENT_TRADE_CAUSAL_HISTORY_CONTRACT_VERSION) issues.push(issue('causal-history', 'causal-history.invalid-version'))
  const checkpoint = value.checkpoint
  const validCheckpointV6 = record(checkpoint)
    && hasOnlyKeys(checkpoint, ['version', 'id', 'token', 'sequence', 'atWorldTime', 'provenanceDigest', 'stateDigest', 'projection'])
    && checkpoint.version === LEGACY_SETTLEMENT_TRADE_CAUSAL_HISTORY_CHECKPOINT_VERSION
    && validId(checkpoint.id) && typeof checkpoint.token === 'string'
    && safeInteger(checkpoint.sequence) && safeInteger(checkpoint.atWorldTime)
    && checkpoint.provenanceDigest === context.creationDigest
    && typeof checkpoint.stateDigest === 'string'
    && legacyProjectionV8Shape(checkpoint.projection)
    && record(checkpoint.projection) && record(checkpoint.projection.temporal)
    && checkpoint.projection.temporal.worldTime === checkpoint.atWorldTime
    && causalDigestFor('causal-replay-projection', checkpoint.projection) === checkpoint.stateDigest
    && checkpoint.id === checkpointIdFor(context, checkpoint.sequence, checkpoint.stateDigest)
    && checkpoint.token === checkpointTokenFor(context, checkpoint.sequence, checkpoint.atWorldTime, checkpoint.stateDigest)
  if (!validCheckpointV6) issues.push(issue('causal-history:checkpoint', 'causal-history.invalid-checkpoint'))

  const v6Kinds = ['initial-courier-selected', 'tavern-courier-switched', 'vessel-station-readout-recorded', 'vessel-cargo-loaded', 'vessel-cargo-unloaded', 'vessel-cargo-failure-resolved', 'vessel-cargo-recovered', 'courier-loss-resolved', 'time-bearing-action', 'deck-moved', 'durable-jomon-growth', 'delegation-offered', 'delegation-interrupted'] as const
  const validCommandV6 = (command: unknown): command is Record<string, unknown> => {
    if (!record(command) || !hasOnlyKeys(command, ['version', 'sequence', 'id', 'token', 'kind', 'payload', 'contentSafety']) || command.version !== LEGACY_SETTLEMENT_TRADE_CAUSAL_HISTORY_CONTRACT_VERSION || !safeInteger(command.sequence) || !validId(command.id) || typeof command.token !== 'string' || typeof command.kind !== 'string' || !v6Kinds.includes(command.kind as typeof v6Kinds[number]) || !commandPayloadIsShaped(command.kind as CausalCommandKind, command.payload)) return false
    const kind = command.kind as CausalCommandKind
    return command.id === commandIdFor(context, command.sequence, kind, command.payload)
      && command.token === commandTokenFor(context, command.sequence, kind, command.payload)
      && same(command.contentSafety, commandClassification(kind, command.payload as InitialCourierSelectedCommand['payload']))
      && validateMedievalContentSafety([{ id: `causal-history:${command.id}`, domain: 'event', classification: command.contentSafety }]).status === 'accepted'
  }
  if (!Array.isArray(value.tail)) issues.push(issue('causal-history:tail', 'causal-history.invalid-command'))
  else {
    if (value.tail.length > CAUSAL_HISTORY_LIMITS.retainedCommands) issues.push(issue('causal-history:tail', 'causal-history.command-limit'))
    const ids = new Set<string>()
    for (const command of value.tail) {
      const id = record(command) && typeof command.id === 'string' ? command.id : 'causal-history:command'
      if (!validCommandV6(command)) issues.push(issue(id, 'causal-history.invalid-command'))
      if (record(command) && typeof command.id === 'string') {
        if (ids.has(command.id)) issues.push(issue(command.id, 'causal-history.duplicate-command-id'))
        ids.add(command.id)
      }
    }
    const checkpointSequence = record(checkpoint) && safeInteger(checkpoint.sequence) ? checkpoint.sequence : 0
    if (value.tail.some((command, index) => !record(command) || command.sequence !== checkpointSequence + index + 1)) issues.push(issue('causal-history:tail', 'causal-history.invalid-command-order'))
  }
  const validSegmentV6 = (segment: unknown): segment is Record<string, unknown> => {
    if (!record(segment) || !hasOnlyKeys(segment, ['version', 'id', 'token', 'sequenceStart', 'sequenceEnd', 'worldTimeStart', 'worldTimeEnd', 'commandKinds', 'provenanceDigest', 'stateDigest']) || segment.version !== LEGACY_SETTLEMENT_TRADE_CAUSAL_HISTORY_SEGMENT_VERSION || !validId(segment.id) || typeof segment.token !== 'string' || !safeInteger(segment.sequenceStart) || !safeInteger(segment.sequenceEnd) || segment.sequenceStart < 1 || segment.sequenceEnd < segment.sequenceStart || !safeInteger(segment.worldTimeStart) || !safeInteger(segment.worldTimeEnd) || segment.worldTimeEnd < segment.worldTimeStart || !validCounts(segment.commandKinds) || segment.provenanceDigest !== context.creationDigest || typeof segment.stateDigest !== 'string') return false
    const draft: CausalSegmentTokenDraft = {
      version: segment.version,
      sequenceStart: segment.sequenceStart,
      sequenceEnd: segment.sequenceEnd,
      worldTimeStart: segment.worldTimeStart,
      worldTimeEnd: segment.worldTimeEnd,
      commandKinds: segment.commandKinds as CausalHistoryCommandKindCounts,
      provenanceDigest: segment.provenanceDigest,
      stateDigest: segment.stateDigest
    }
    return segment.id === segmentIdFor(context, segment.sequenceStart, segment.sequenceEnd, segment.stateDigest)
      && segment.token === segmentTokenFor(context, draft)
  }
  if (!Array.isArray(value.compactedSegments)) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment'))
  else {
    if (value.compactedSegments.length > CAUSAL_HISTORY_LIMITS.compactedSegments) issues.push(issue('causal-history:segments', 'causal-history.segment-limit'))
    let expectedStart = 1
    for (const segment of value.compactedSegments) {
      const id = record(segment) && typeof segment.id === 'string' ? segment.id : 'causal-history:segment'
      if (!validSegmentV6(segment)) issues.push(issue(id, 'causal-history.invalid-segment-token'))
      if (!record(segment) || segment.sequenceStart !== expectedStart) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment-order'))
      if (record(segment) && safeInteger(segment.sequenceEnd)) expectedStart = segment.sequenceEnd + 1
    }
    const checkpointSequence = record(checkpoint) && safeInteger(checkpoint.sequence) ? checkpoint.sequence : 0
    if (expectedStart !== checkpointSequence + 1) issues.push(issue('causal-history:segments', 'causal-history.invalid-segment-order'))
  }
  try {
    if (!Array.isArray(value.tail) || !contentSafetyAuditMatches(contentRecordsFor(value.tail as CausalCommandEvent[]), value.contentSafetyAudit)) issues.push(issue('causal-history:content-safety', 'causal-history.invalid-content-audit'))
  } catch { issues.push(issue('causal-history:content-safety', 'causal-history.invalid-content-audit')) }
  return canonicalIssues(issues)
}

/** Rebinds fully proved v6 cargo evidence to the settlement-trade projection. */
export const upgradeLegacyCausalHistoryStateV6 = (
  context: CausalHistoryContext,
  value: unknown,
  checkpointProjection: CausalReplayProjection
): CausalHistoryState => {
  const diagnostics = validateLegacyCausalHistoryStateV6(context, value)
  if (diagnostics.length || !projectionShape(checkpointProjection) || !record(value) || !record(value.checkpoint) || !Array.isArray(value.tail) || !Array.isArray(value.compactedSegments)) throw new CausalHistoryContractError(diagnostics.length ? diagnostics : [issue('causal-history:legacy-upgrade', 'causal-history.invalid-checkpoint')])
  const tail = value.tail.map(command => ({ ...structuredClone(command as Record<string, unknown>), version: CAUSAL_HISTORY_CONTRACT_VERSION })) as CausalCommandEvent[]
  const compactedSegments = value.compactedSegments.map(segment => {
    const source = segment as Record<string, unknown>
    const draft: Omit<CausalHistoryCompactedSegment, 'id' | 'token'> = {
      version: CAUSAL_HISTORY_SEGMENT_VERSION,
      sequenceStart: source.sequenceStart as number,
      sequenceEnd: source.sequenceEnd as number,
      worldTimeStart: source.worldTimeStart as number,
      worldTimeEnd: source.worldTimeEnd as number,
      commandKinds: currentCountsForLegacy(source.commandKinds as CausalHistoryCommandKindCounts),
      provenanceDigest: source.provenanceDigest as string,
      stateDigest: source.stateDigest as string
    }
    return { ...draft, id: segmentIdFor(context, draft.sequenceStart, draft.sequenceEnd, draft.stateDigest), token: segmentTokenFor(context, draft) }
  })
  const upgraded = {
    version: CAUSAL_HISTORY_CONTRACT_VERSION,
    checkpoint: checkpointFor(context, value.checkpoint.sequence as number, checkpointProjection),
    tail,
    compactedSegments,
    contentSafetyAudit: audit(tail)
  } satisfies CausalHistoryState
  const currentDiagnostics = validateCausalHistoryState(context, upgraded)
  if (currentDiagnostics.length) throw new CausalHistoryContractError(currentDiagnostics)
  return upgraded
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
