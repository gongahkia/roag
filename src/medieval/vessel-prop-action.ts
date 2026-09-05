import { classifyMedievalContent, validateMedievalContentSafety, type MedievalContentSafetyClassification } from './content-safety'
import { FOUNDATION_JOMON_PROP_DEFINITIONS, type FoundationJomon, type FoundationJomonPropId } from './types'

/**
 * The bounded mutable record for physical vessel outcomes. It retains no prop
 * geometry, source values, people, route facts, or browser-facing text.
 */
export const VESSEL_PROP_ACTION_STATE_VERSION = 1 as const

export const VESSEL_PROP_ACTION_KINDS = [
  'station-readout-recorded',
  'tavern-courier-switched'
] as const
export type VesselPropActionKind = typeof VESSEL_PROP_ACTION_KINDS[number]

export interface VesselPropLatestAction {
  kind: VesselPropActionKind
  recordedAtWorldTime: number
  causalSequence: number
}

export interface VesselPropActionRecord {
  propId: FoundationJomonPropId
  latestAction?: VesselPropLatestAction
}

export interface VesselPropActionState {
  version: typeof VESSEL_PROP_ACTION_STATE_VERSION
  records: readonly VesselPropActionRecord[]
}

export interface VesselPropActionFeedback {
  propId: FoundationJomonPropId
  action: VesselPropLatestAction
  label: string
  text: string
  presentationState: 'neutral' | 'ready'
  contentSafety: MedievalContentSafetyClassification
}

export type VesselPropActionDiagnostic =
  | 'vessel-prop-action.malformed-state'
  | 'vessel-prop-action.invalid-version'
  | 'vessel-prop-action.invalid-record'
  | 'vessel-prop-action.noncanonical-record-order'
  | 'vessel-prop-action.invalid-prop'
  | 'vessel-prop-action.invalid-action'
  | 'vessel-prop-action.invalid-time'
  | 'vessel-prop-action.invalid-sequence'
  | 'vessel-prop-action.invalid-content-safety'

export class VesselPropActionContractError extends Error {
  constructor(readonly diagnostics: readonly VesselPropActionDiagnostic[]) {
    super(`vessel prop action rejected: ${diagnostics.join(', ')}`)
    this.name = 'VesselPropActionContractError'
  }
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const positiveInteger = (value: unknown): value is number => safeInteger(value) && value > 0
const keys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort()
  const wanted = [...expected].sort()
  return actual.length === wanted.length && actual.every((key, index) => key === wanted[index])
}
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const propIds = (): readonly FoundationJomonPropId[] => FOUNDATION_JOMON_PROP_DEFINITIONS.map(prop => prop.id)
const actionSafety = (): MedievalContentSafetyClassification => classifyMedievalContent('event', ['adult-labour', 'civil-life', 'navigation'], 'adults-only', ['data'])

const labelFor = (propId: FoundationJomonPropId): string => {
  if (propId === 'prop:berth') return 'Berth'
  if (propId === 'prop:cargo-hold-rack') return 'Cargo hold rack'
  if (propId === 'prop:chart-table') return 'Chart table'
  if (propId === 'prop:galley-hearth') return 'Galley hearth'
  if (propId === 'prop:gangplank') return 'Gangplank'
  if (propId === 'prop:repair-space-rack') return 'Repair-space rack'
  if (propId === 'prop:stores-rack') return 'Stores rack'
  return 'Tavern task ledger'
}

const actionAllowedFor = (propId: FoundationJomonPropId, action: VesselPropLatestAction): boolean => action.kind === 'tavern-courier-switched'
  ? propId === 'prop:task-ledger'
  : propId !== 'prop:task-ledger'

const validAction = (propId: FoundationJomonPropId, value: unknown, worldTime?: number, causalSequence?: number): value is VesselPropLatestAction => record(value)
  && keys(value, ['kind', 'recordedAtWorldTime', 'causalSequence'])
  && (value.kind === 'station-readout-recorded' || value.kind === 'tavern-courier-switched')
  && safeInteger(value.recordedAtWorldTime)
  && positiveInteger(value.causalSequence)
  && (worldTime === undefined || value.recordedAtWorldTime <= worldTime)
  && (causalSequence === undefined || value.causalSequence <= causalSequence)
  && actionAllowedFor(propId, value as unknown as VesselPropLatestAction)

/** Canonical source identity belongs to immutable Jomon provenance. */
export const initialVesselPropActionState = (jomon: Pick<FoundationJomon, 'props'>): VesselPropActionState => {
  const expected = propIds()
  if (!Array.isArray(jomon.props) || jomon.props.length !== expected.length || !jomon.props.every((prop, index) => prop.id === expected[index])) {
    throw new VesselPropActionContractError(['vessel-prop-action.invalid-prop'])
  }
  return { version: VESSEL_PROP_ACTION_STATE_VERSION, records: expected.map(propId => ({ propId })) }
}

/** Strict state validation is structural; full causal equivalence belongs to replay. */
export const validateVesselPropActionState = (
  jomon: Pick<FoundationJomon, 'props'>,
  value: unknown,
  worldTime?: number,
  causalSequence?: number
): readonly VesselPropActionDiagnostic[] => {
  if (!record(value) || !keys(value, ['version', 'records'])) return ['vessel-prop-action.malformed-state']
  const diagnostics: VesselPropActionDiagnostic[] = []
  if (value.version !== VESSEL_PROP_ACTION_STATE_VERSION) diagnostics.push('vessel-prop-action.invalid-version')
  const expected = propIds()
  if (!Array.isArray(jomon.props) || !same(jomon.props.map(prop => prop.id), expected)) diagnostics.push('vessel-prop-action.invalid-prop')
  if (!Array.isArray(value.records) || value.records.length !== expected.length) return [...diagnostics, 'vessel-prop-action.malformed-state']
  for (const [index, item] of value.records.entries()) {
    const propId = expected[index]
    if (!propId || !record(item) || !keys(item, ['propId', 'latestAction'].filter(key => key !== 'latestAction' || Object.hasOwn(item, key))) || item.propId !== propId) {
      diagnostics.push(record(item) && typeof item.propId === 'string' && expected.includes(item.propId as FoundationJomonPropId) ? 'vessel-prop-action.noncanonical-record-order' : 'vessel-prop-action.invalid-record')
      continue
    }
    if (Object.hasOwn(item, 'latestAction') && !validAction(propId, item.latestAction, worldTime, causalSequence)) {
      const action = record(item.latestAction) ? item.latestAction : undefined
      diagnostics.push(action?.recordedAtWorldTime !== undefined && (!safeInteger(action.recordedAtWorldTime) || (worldTime !== undefined && action.recordedAtWorldTime > worldTime))
        ? 'vessel-prop-action.invalid-time'
        : action?.causalSequence !== undefined && (!positiveInteger(action.causalSequence) || (causalSequence !== undefined && action.causalSequence > causalSequence))
          ? 'vessel-prop-action.invalid-sequence'
          : 'vessel-prop-action.invalid-action')
    }
  }
  return [...new Set(diagnostics)].sort()
}

export const recordVesselPropAction = (
  jomon: Pick<FoundationJomon, 'props'>,
  state: VesselPropActionState,
  propId: FoundationJomonPropId,
  action: VesselPropLatestAction
): VesselPropActionState => {
  const diagnostics = validateVesselPropActionState(jomon, state)
  if (diagnostics.length || !propIds().includes(propId) || !validAction(propId, action)) {
    throw new VesselPropActionContractError(diagnostics.length ? diagnostics : ['vessel-prop-action.invalid-action'])
  }
  const prior = state.records.find(item => item.propId === propId)?.latestAction
  if (prior && action.causalSequence <= prior.causalSequence) throw new VesselPropActionContractError(['vessel-prop-action.invalid-sequence'])
  return {
    version: VESSEL_PROP_ACTION_STATE_VERSION,
    records: state.records.map(item => item.propId === propId ? { propId, latestAction: structuredClone(action) } : structuredClone(item))
  }
}

/** Bounded player-facing phrasing is derived from typed state, never persisted as action text. */
export const vesselPropActionFeedback = (record: VesselPropActionRecord): VesselPropActionFeedback | undefined => {
  if (record.latestAction === undefined || !propIds().includes(record.propId) || !validAction(record.propId, record.latestAction)) return undefined
  const action = structuredClone(record.latestAction)
  const label = labelFor(record.propId)
  const text = action.kind === 'tavern-courier-switched'
    ? `${label}: courier switch recorded.`
    : `${label}: bounded station readout recorded.`
  const feedback: VesselPropActionFeedback = {
    propId: record.propId,
    action,
    label,
    text,
    presentationState: action.kind === 'tavern-courier-switched' ? 'ready' : 'neutral',
    contentSafety: actionSafety()
  }
  if (validateMedievalContentSafety([{ id: `vessel-prop-action:${record.propId}`, domain: 'event', classification: feedback.contentSafety }]).status === 'rejected') {
    throw new VesselPropActionContractError(['vessel-prop-action.invalid-content-safety'])
  }
  return feedback
}

export const vesselPropActionFeedbacks = (state: VesselPropActionState): readonly VesselPropActionFeedback[] => state.records
  .map(vesselPropActionFeedback)
  .filter((item): item is VesselPropActionFeedback => item !== undefined)
