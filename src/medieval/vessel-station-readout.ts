import { auditMedievalContentSafety, classifyMedievalContent, type MedievalContentSafetyClassification } from './content-safety'
import type { JomonPaletteToken } from './palette'
import { TERMINAL_STATE_PRESENTATIONS, terminalNonColorCueFor, type TerminalNonColorCue, type TerminalPresentationState } from './terminal-semantics'
import { assessVesselPropOperationForVerifiedWorld, type VesselProximityOperationReason, type VesselProximityOperationSource, type VesselProximityOperationPropId } from './vessel-proximity-operation'
import { commodityCatalogue, type JomonCommodityId } from './commodity-catalogue'
import { vesselCargoUsedUnits, type VesselCargoCondition, type VesselCargoStatus } from './cargo-hold'
import type { FoundationWorld } from './types'

/**
 * Bounded, renderer-neutral inspection data for the seven non-ledger stations.
 * It is derived after world validation and neither records a prop action nor
 * becomes mutable prop state.
 */
export const VESSEL_STATION_READOUT_CONTRACT_VERSION = 2 as const

export const VESSEL_STATION_READOUT_PROP_IDS = [
  'prop:berth',
  'prop:cargo-hold-rack',
  'prop:chart-table',
  'prop:galley-hearth',
  'prop:gangplank',
  'prop:repair-space-rack',
  'prop:stores-rack'
] as const satisfies readonly VesselProximityOperationPropId[]
export type VesselStationReadoutPropId = typeof VESSEL_STATION_READOUT_PROP_IDS[number]

export type VesselStationReadoutValue =
  | { kind: 'berth-capacity'; berthSlots: number }
  | { kind: 'cargo-capacity'; cargoUnits: number; usedUnits: number; lots: readonly { commodityId: JomonCommodityId; quantity: number; condition: VesselCargoCondition; status: VesselCargoStatus }[] }
  | { kind: 'route-comparison-unavailable' }
  | { kind: 'galley-unmodeled' }
  | { kind: 'quay-travel-unavailable'; operationalStatus: 'moored' }
  | { kind: 'repair-integrity'; current: number; maximum: number }
  | { kind: 'stores-unmodeled' }

export interface VesselStationReadout {
  version: typeof VESSEL_STATION_READOUT_CONTRACT_VERSION
  source: VesselProximityOperationSource
  label: 'Berth' | 'Cargo hold rack' | 'Chart table' | 'Galley hearth' | 'Gangplank' | 'Repair-space rack' | 'Stores rack'
  value: VesselStationReadoutValue
  /** Present only for the two explicitly deferred action domains. */
  reason?: Extract<VesselProximityOperationReason, 'route-comparison-not-implemented' | 'quay-travel-not-implemented'>
  factSourceId: string
  paletteToken: JomonPaletteToken
  presentationState: TerminalPresentationState
  nonColorCue: TerminalNonColorCue
  text: string
  accessibilityText: string
  contentSafety: MedievalContentSafetyClassification
}

export class VesselStationReadoutContractError extends Error {
  constructor(readonly code: 'invalid-operation' | 'invalid-readout') {
    super(`vessel station readout rejected: ${code}`)
    this.name = 'VesselStationReadoutContractError'
  }
}

const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const keys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort()
  const sorted = [...expected].sort()
  return actual.length === sorted.length && actual.every((key, index) => key === sorted[index])
}
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0 && value <= 100
const safety = (): MedievalContentSafetyClassification => classifyMedievalContent('template', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])
const paletteFor = (state: TerminalPresentationState): JomonPaletteToken => TERMINAL_STATE_PRESENTATIONS[state].paletteToken

interface Definition {
  propId: VesselStationReadoutPropId
  propKind: VesselStationReadout['source']['propKind']
  areaId: VesselStationReadout['source']['areaId']
  valueKind: VesselStationReadoutValue['kind']
  label: VesselStationReadout['label']
  state: TerminalPresentationState
  factSourceId: string
  value: (world: FoundationWorld) => VesselStationReadoutValue
  reason?: VesselStationReadout['reason']
  text: (value: VesselStationReadoutValue) => string
}

const DEFINITIONS: readonly Definition[] = [
  {
    propId: 'prop:berth', propKind: 'berth', areaId: 'berths', valueKind: 'berth-capacity', label: 'Berth', state: 'neutral', factSourceId: 'world-state:jomon',
    value: world => ({ kind: 'berth-capacity', berthSlots: world.state.jomon.capacity.berthSlots }),
    text: value => `Berth capacity: ${(value as Extract<VesselStationReadoutValue, { kind: 'berth-capacity' }>).berthSlots} slots. Rest and recovery are not modeled.`
  },
  {
    propId: 'prop:cargo-hold-rack', propKind: 'rack', areaId: 'cargo-hold', valueKind: 'cargo-capacity', label: 'Cargo hold rack', state: 'neutral', factSourceId: 'world-state:jomon:cargo',
    value: world => ({
      kind: 'cargo-capacity',
      cargoUnits: world.state.jomon.capacity.cargoUnits,
      usedUnits: vesselCargoUsedUnits(world.state.jomon.cargo),
      lots: world.state.jomon.cargo.lots.map(lot => ({ commodityId: lot.commodityId, quantity: lot.quantity, condition: lot.condition, status: lot.status }))
    }),
    text: value => {
      const cargo = value as Extract<VesselStationReadoutValue, { kind: 'cargo-capacity' }>
      if (!cargo.lots.length) return `Cargo hold: ${cargo.usedUnits}/${cargo.cargoUnits} units occupied. No cargo lots are recorded.`
      const nameFor = (commodityId: JomonCommodityId): string => commodityCatalogue().commodities.find(commodity => commodity.id === commodityId)?.name ?? 'Unknown commodity'
      const lots = cargo.lots.map(lot => `${nameFor(lot.commodityId)} ×${lot.quantity}: ${lot.condition}, ${lot.status}.`).join(' ')
      return `Cargo hold: ${cargo.usedUnits}/${cargo.cargoUnits} units occupied. ${lots}`
    }
  },
  {
    propId: 'prop:chart-table', propKind: 'table', areaId: 'chart-table', valueKind: 'route-comparison-unavailable', label: 'Chart table', state: 'warning', factSourceId: 'deck-prop-binding:prop:chart-table', reason: 'route-comparison-not-implemented',
    value: () => ({ kind: 'route-comparison-unavailable' }),
    text: () => 'Route comparison is not yet implemented.'
  },
  {
    propId: 'prop:galley-hearth', propKind: 'hearth', areaId: 'galley', valueKind: 'galley-unmodeled', label: 'Galley hearth', state: 'warning', factSourceId: 'deck-prop-binding:prop:galley-hearth',
    value: () => ({ kind: 'galley-unmodeled' }),
    text: () => 'Meals, rations, and cooking are not modeled.'
  },
  {
    propId: 'prop:gangplank', propKind: 'gangplank', areaId: 'gangplank', valueKind: 'quay-travel-unavailable', label: 'Gangplank', state: 'warning', factSourceId: 'world-state:jomon', reason: 'quay-travel-not-implemented',
    value: world => ({ kind: 'quay-travel-unavailable', operationalStatus: world.state.jomon.operationalStatus }),
    text: () => 'Jomon is moored. Quay departure and travel are not yet implemented.'
  },
  {
    propId: 'prop:repair-space-rack', propKind: 'rack', areaId: 'repair-space', valueKind: 'repair-integrity', label: 'Repair-space rack', state: 'neutral', factSourceId: 'world-state:jomon',
    value: world => ({ kind: 'repair-integrity', current: world.state.jomon.integrity.current, maximum: world.state.jomon.integrity.maximum }),
    text: value => {
      const integrity = value as Extract<VesselStationReadoutValue, { kind: 'repair-integrity' }>
      return `Jomon integrity: ${integrity.current}/${integrity.maximum}. Repair work is not yet implemented.`
    }
  },
  {
    propId: 'prop:stores-rack', propKind: 'rack', areaId: 'stores', valueKind: 'stores-unmodeled', label: 'Stores rack', state: 'warning', factSourceId: 'deck-prop-binding:prop:stores-rack',
    value: () => ({ kind: 'stores-unmodeled' }),
    text: () => 'Onboard provisions and inventory are not yet modeled.'
  }
]

const definitionFor = (propId: unknown): Definition | undefined => DEFINITIONS.find(definition => definition.propId === propId)

const rawReadoutFor = (world: FoundationWorld, propId: VesselStationReadoutPropId): VesselStationReadout => {
  const definition = definitionFor(propId)
  const operation = assessVesselPropOperationForVerifiedWorld(world, world.state, propId)
  if (!definition || operation.proximity !== 'at-anchor' || operation.availability !== 'readout' || operation.reason !== definition.reason) throw new VesselStationReadoutContractError('invalid-operation')
  const value = definition.value(world)
  const text = definition.text(value)
  const promptState = definition.state
  return {
    version: VESSEL_STATION_READOUT_CONTRACT_VERSION,
    source: structuredClone(operation.source),
    label: definition.label,
    value,
    ...(definition.reason === undefined ? {} : { reason: definition.reason }),
    factSourceId: definition.factSourceId,
    paletteToken: paletteFor(promptState),
    presentationState: promptState,
    nonColorCue: terminalNonColorCueFor(promptState),
    text,
    accessibilityText: `${definition.label}. ${text} Source ${definition.factSourceId}. This is a zero-time readout; Enter reports the same bounded station result and Escape cancels without mutation.`,
    contentSafety: safety()
  }
}

/** Creates only an at-anchor readout after the full FoundationWorld boundary has validated. */
export const createVesselStationReadoutForVerifiedWorld = (world: FoundationWorld, propId: VesselStationReadoutPropId): VesselStationReadout => {
  const readout = rawReadoutFor(world, propId)
  if (!validateVesselStationReadout(readout)) throw new VesselStationReadoutContractError('invalid-readout')
  return readout
}

/** Shape validation rejects raw state, coordinates, identities, and extension fields. */
export const validateVesselStationReadout = (value: unknown): value is VesselStationReadout => {
  if (!record(value) || !keys(value, ['version', 'source', 'label', 'value', 'reason', 'factSourceId', 'paletteToken', 'presentationState', 'nonColorCue', 'text', 'accessibilityText', 'contentSafety'].filter(key => key !== 'reason' || Object.hasOwn(value, 'reason')))
    || value.version !== VESSEL_STATION_READOUT_CONTRACT_VERSION || !record(value.source) || !keys(value.source, ['propBindingId', 'propId', 'propKind', 'areaId']) || !definitionFor(value.source.propId)) return false
  const definition = definitionFor(value.source.propId)!
  if (value.source.propBindingId !== `deck-prop-binding:${definition.propId}` || value.source.propKind !== definition.propKind || value.source.areaId !== definition.areaId || value.label !== definition.label || value.factSourceId !== definition.factSourceId || value.presentationState !== definition.state || value.paletteToken !== paletteFor(definition.state) || !same(value.nonColorCue, terminalNonColorCueFor(definition.state)) || !same(value.contentSafety, safety()) || auditMedievalContentSafety([{ id: `vessel-station:${definition.propId}`, domain: 'template', classification: value.contentSafety }]).status === 'rejected') return false
  if (value.reason !== definition.reason || !record(value.value)) return false
  const readoutValue = value.value
  const valueValid = readoutValue.kind === definition.valueKind && (readoutValue.kind === 'berth-capacity' && keys(readoutValue, ['kind', 'berthSlots']) && safeInteger(readoutValue.berthSlots)
    || readoutValue.kind === 'cargo-capacity' && keys(readoutValue, ['kind', 'cargoUnits', 'usedUnits', 'lots']) && safeInteger(readoutValue.cargoUnits) && safeInteger(readoutValue.usedUnits) && readoutValue.usedUnits <= readoutValue.cargoUnits && Array.isArray(readoutValue.lots) && readoutValue.lots.length <= 24 && readoutValue.lots.every(lot => record(lot) && keys(lot, ['commodityId', 'quantity', 'condition', 'status']) && typeof lot.commodityId === 'string' && commodityCatalogue().commodities.some(commodity => commodity.id === lot.commodityId) && safeInteger(lot.quantity) && lot.quantity >= 1 && lot.quantity <= 12 && ['sound', 'spoiled', 'damaged'].includes(String(lot.condition)) && ['in-hold', 'lost'].includes(String(lot.status)))
    || readoutValue.kind === 'route-comparison-unavailable' && keys(readoutValue, ['kind'])
    || readoutValue.kind === 'galley-unmodeled' && keys(readoutValue, ['kind'])
    || readoutValue.kind === 'quay-travel-unavailable' && keys(readoutValue, ['kind', 'operationalStatus']) && readoutValue.operationalStatus === 'moored'
    || readoutValue.kind === 'repair-integrity' && keys(readoutValue, ['kind', 'current', 'maximum']) && safeInteger(readoutValue.current) && safeInteger(readoutValue.maximum) && readoutValue.maximum >= 1 && readoutValue.current <= readoutValue.maximum
    || readoutValue.kind === 'stores-unmodeled' && keys(readoutValue, ['kind']))
  return Boolean(valueValid) && typeof value.text === 'string' && value.text === definition.text(readoutValue as VesselStationReadoutValue) && typeof value.accessibilityText === 'string'
}

/** Exact source equivalence is reserved for the world/presentation boundary. */
export const vesselStationReadoutMatchesVerifiedWorld = (world: FoundationWorld, value: unknown): value is VesselStationReadout => {
  if (!validateVesselStationReadout(value) || !definitionFor(value.source.propId)) return false
  try { return same(value, rawReadoutFor(world, value.source.propId as VesselStationReadoutPropId)) } catch { return false }
}
