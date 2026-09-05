import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyClassification } from './content-safety'
import { cargoLotIdFor } from './cargo-hold'
import { namedSettlementProfile, validateNamedSettlementProfile } from './settlement-profile'
import type { JomonDeckPlan, JomonDeckPlanAreaId } from './jomon-deck-plan'

/**
 * One deliberately local physical freight handoff. It establishes neither a
 * route nor a market: the public tally receives a burden and Jomon's existing
 * hold is the only available delivery station.
 */
export const SETTLEMENT_TRADING_CONTRACT_VERSION = 1 as const
export const SETTLEMENT_TRADING_LOCATION_ID = 'settlement-location:hearthford-mill-quay' as const
export const SETTLEMENT_TRADING_CONTRACT_ID = 'settlement-contract:hearthford-mill-ironwork' as const
export const SETTLEMENT_TRADING_SOURCE_AREA_ID = 'quay-approach' as const satisfies JomonDeckPlanAreaId
export const SETTLEMENT_TRADING_DELIVERY_PROP_ID = 'prop:cargo-hold-rack' as const
export const SETTLEMENT_TRADING_COMMODITY_ID = 'commodity:ironwork' as const
export const SETTLEMENT_TRADING_QUANTITY = 1 as const

export const SETTLEMENT_TRADE_STATUSES = ['offered', 'accepted', 'refused', 'delivered'] as const
export type SettlementTradeStatus = typeof SETTLEMENT_TRADE_STATUSES[number]

export interface SettlementTradingLocation {
  id: typeof SETTLEMENT_TRADING_LOCATION_ID
  profileId: 'settlement-profile:hearthford-mill-quay'
  sourceAreaId: typeof SETTLEMENT_TRADING_SOURCE_AREA_ID
  serviceId: 'public-tally-table'
}

/** A material obligation before the cargo-hold receipt mints its lot. */
export interface SettlementTradeBurden {
  commodityId: typeof SETTLEMENT_TRADING_COMMODITY_ID
  quantity: typeof SETTLEMENT_TRADING_QUANTITY
  deliveryPropId: typeof SETTLEMENT_TRADING_DELIVERY_PROP_ID
}

export interface SettlementTradeContractOffered {
  id: typeof SETTLEMENT_TRADING_CONTRACT_ID
  locationId: typeof SETTLEMENT_TRADING_LOCATION_ID
  status: 'offered'
}

export interface SettlementTradeContractAccepted extends Omit<SettlementTradeContractOffered, 'status'> {
  status: 'accepted'
  burden: SettlementTradeBurden
  recordedAtWorldTime: number
  causalSequence: number
}

export interface SettlementTradeContractRefused extends Omit<SettlementTradeContractOffered, 'status'> {
  status: 'refused'
  recordedAtWorldTime: number
  causalSequence: number
}

export interface SettlementTradeContractDelivered extends Omit<SettlementTradeContractOffered, 'status'> {
  status: 'delivered'
  cargoId: string
  recordedAtWorldTime: number
  causalSequence: number
}

export type SettlementTradeContract = SettlementTradeContractOffered | SettlementTradeContractAccepted | SettlementTradeContractRefused | SettlementTradeContractDelivered

export interface SettlementTradingState {
  version: typeof SETTLEMENT_TRADING_CONTRACT_VERSION
  locations: readonly [SettlementTradingLocation]
  contracts: readonly [SettlementTradeContract]
}

export type SettlementTradingDiagnostic =
  | 'settlement-trading.malformed-state'
  | 'settlement-trading.invalid-version'
  | 'settlement-trading.invalid-location'
  | 'settlement-trading.invalid-contract'
  | 'settlement-trading.invalid-status'
  | 'settlement-trading.invalid-burden'
  | 'settlement-trading.invalid-evidence'
  | 'settlement-trading.invalid-cargo-reference'
  | 'settlement-trading.invalid-content-safety'

export class SettlementTradingContractError extends Error {
  constructor(readonly diagnostics: readonly SettlementTradingDiagnostic[]) {
    super(`settlement trading rejected: ${diagnostics.join(', ')}`)
    this.name = 'SettlementTradingContractError'
  }
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const keys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort()
  const canonical = [...expected].sort()
  return actual.length === canonical.length && actual.every((key, index) => key === canonical[index])
}
const safeTime = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const sequence = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 1
const cargoId = (value: unknown): value is string => typeof value === 'string' && /^cargo:[1-9][0-9]*:commodity:ironwork$/u.test(value)

const safety = (): MedievalContentSafetyClassification => classifyMedievalContent('contract', ['civil-life', 'trade'], 'adults-only', ['player-facing-text'])

const canonicalLocation = (): SettlementTradingLocation => ({
  id: SETTLEMENT_TRADING_LOCATION_ID,
  profileId: 'settlement-profile:hearthford-mill-quay',
  sourceAreaId: SETTLEMENT_TRADING_SOURCE_AREA_ID,
  serviceId: 'public-tally-table'
})

const canonicalOfferedContract = (): SettlementTradeContractOffered => ({
  id: SETTLEMENT_TRADING_CONTRACT_ID,
  locationId: SETTLEMENT_TRADING_LOCATION_ID,
  status: 'offered'
})

const canonicalBurden = (): SettlementTradeBurden => ({
  commodityId: SETTLEMENT_TRADING_COMMODITY_ID,
  quantity: SETTLEMENT_TRADING_QUANTITY,
  deliveryPropId: SETTLEMENT_TRADING_DELIVERY_PROP_ID
})

export const initialSettlementTradingState = (): SettlementTradingState => ({
  version: SETTLEMENT_TRADING_CONTRACT_VERSION,
  locations: [canonicalLocation()],
  contracts: [canonicalOfferedContract()]
})

const validLocation = (value: unknown): value is SettlementTradingLocation => record(value)
  && keys(value, ['id', 'profileId', 'sourceAreaId', 'serviceId'])
  && value.id === SETTLEMENT_TRADING_LOCATION_ID
  && value.profileId === 'settlement-profile:hearthford-mill-quay'
  && value.sourceAreaId === SETTLEMENT_TRADING_SOURCE_AREA_ID
  && value.serviceId === 'public-tally-table'

const validBurden = (value: unknown): value is SettlementTradeBurden => record(value)
  && keys(value, ['commodityId', 'quantity', 'deliveryPropId'])
  && value.commodityId === SETTLEMENT_TRADING_COMMODITY_ID
  && value.quantity === SETTLEMENT_TRADING_QUANTITY
  && value.deliveryPropId === SETTLEMENT_TRADING_DELIVERY_PROP_ID

const validContractIdentity = (value: Record<string, unknown>): boolean => value.id === SETTLEMENT_TRADING_CONTRACT_ID && value.locationId === SETTLEMENT_TRADING_LOCATION_ID

const validContract = (value: unknown): value is SettlementTradeContract => {
  if (!record(value) || !validContractIdentity(value) || !SETTLEMENT_TRADE_STATUSES.includes(value.status as SettlementTradeStatus)) return false
  if (value.status === 'offered') return keys(value, ['id', 'locationId', 'status'])
  if (value.status === 'accepted') return keys(value, ['id', 'locationId', 'status', 'burden', 'recordedAtWorldTime', 'causalSequence'])
    && validBurden(value.burden) && safeTime(value.recordedAtWorldTime) && sequence(value.causalSequence)
  if (value.status === 'refused') return keys(value, ['id', 'locationId', 'status', 'recordedAtWorldTime', 'causalSequence'])
    && safeTime(value.recordedAtWorldTime) && sequence(value.causalSequence)
  return keys(value, ['id', 'locationId', 'status', 'cargoId', 'recordedAtWorldTime', 'causalSequence'])
    && cargoId(value.cargoId) && safeTime(value.recordedAtWorldTime) && sequence(value.causalSequence)
}

export const settlementTradingContentRecords = (state: Pick<SettlementTradingState, 'locations' | 'contracts'>): readonly ClassifiedMedievalContent[] => [
  ...state.locations.map(location => ({ id: `settlement-trading:location:${location.id}`, domain: 'place' as const, classification: safety() })),
  ...state.contracts.map(contract => ({ id: `settlement-trading:contract:${contract.id}`, domain: 'contract' as const, classification: safety() }))
]

/** Exact closed validation. Cargo cross-reference belongs to world-state validation. */
export const validateSettlementTradingState = (value: unknown, worldTime?: number, causalSequence?: number): readonly SettlementTradingDiagnostic[] => {
  if (!record(value) || !keys(value, ['version', 'locations', 'contracts'])) return ['settlement-trading.malformed-state']
  const diagnostics: SettlementTradingDiagnostic[] = []
  if (value.version !== SETTLEMENT_TRADING_CONTRACT_VERSION) diagnostics.push('settlement-trading.invalid-version')
  if (!Array.isArray(value.locations) || value.locations.length !== 1 || !validLocation(value.locations[0])) diagnostics.push('settlement-trading.invalid-location')
  if (!Array.isArray(value.contracts) || value.contracts.length !== 1 || !validContract(value.contracts[0])) {
    diagnostics.push('settlement-trading.invalid-contract')
  } else {
    const contract = value.contracts[0]
    if (contract.status === 'accepted' && !validBurden(contract.burden)) diagnostics.push('settlement-trading.invalid-burden')
    if (contract.status !== 'offered' && (worldTime === undefined || causalSequence === undefined || contract.recordedAtWorldTime > worldTime || contract.causalSequence > causalSequence)) diagnostics.push('settlement-trading.invalid-evidence')
    if (contract.status === 'delivered' && contract.cargoId !== cargoLotIdFor(contract.causalSequence, SETTLEMENT_TRADING_COMMODITY_ID)) diagnostics.push('settlement-trading.invalid-cargo-reference')
  }
  const content = auditMedievalContentSafety(Array.isArray(value.locations) && Array.isArray(value.contracts)
    ? settlementTradingContentRecords({ locations: value.locations as SettlementTradingLocation[], contracts: value.contracts as SettlementTradeContract[] })
    : [])
  if (content.status === 'rejected') diagnostics.push('settlement-trading.invalid-content-safety')
  return [...new Set(diagnostics)].sort()
}

const checkedState = (state: SettlementTradingState): SettlementTradeContract => {
  const diagnostics = validateSettlementTradingState(state)
  if (diagnostics.length) throw new SettlementTradingContractError(diagnostics)
  const profile = namedSettlementProfile()
  if (validateNamedSettlementProfile(profile).length || profile.id !== 'settlement-profile:hearthford-mill-quay') throw new SettlementTradingContractError(['settlement-trading.invalid-location'])
  return state.contracts[0]
}

const replaceContract = (state: SettlementTradingState, contract: SettlementTradeContract): SettlementTradingState => ({
  version: SETTLEMENT_TRADING_CONTRACT_VERSION,
  locations: [canonicalLocation()],
  contracts: [structuredClone(contract)]
})

export const acceptSettlementTradeContract = (state: SettlementTradingState, recordedAtWorldTime: number, causalSequence: number): SettlementTradingState => {
  const contract = checkedState(state)
  if (contract.status !== 'offered' || !safeTime(recordedAtWorldTime) || !sequence(causalSequence)) throw new SettlementTradingContractError(['settlement-trading.invalid-status'])
  return replaceContract(state, {
    ...canonicalOfferedContract(),
    status: 'accepted',
    burden: canonicalBurden(),
    recordedAtWorldTime,
    causalSequence
  })
}

export const refuseSettlementTradeContract = (state: SettlementTradingState, recordedAtWorldTime: number, causalSequence: number): SettlementTradingState => {
  const contract = checkedState(state)
  if (contract.status !== 'offered' || !safeTime(recordedAtWorldTime) || !sequence(causalSequence)) throw new SettlementTradingContractError(['settlement-trading.invalid-status'])
  return replaceContract(state, { ...canonicalOfferedContract(), status: 'refused', recordedAtWorldTime, causalSequence })
}

export const deliverSettlementTradeContract = (state: SettlementTradingState, recordedAtWorldTime: number, causalSequence: number): SettlementTradingState => {
  const contract = checkedState(state)
  if (contract.status !== 'accepted' || !safeTime(recordedAtWorldTime) || !sequence(causalSequence)) throw new SettlementTradingContractError(['settlement-trading.invalid-status'])
  return replaceContract(state, {
    ...canonicalOfferedContract(),
    status: 'delivered',
    cargoId: cargoLotIdFor(causalSequence, SETTLEMENT_TRADING_COMMODITY_ID),
    recordedAtWorldTime,
    causalSequence
  })
}

export type SettlementTradeFeedback = {
  id: string
  state: 'neutral' | 'ready' | 'warning'
  text: string
  accessibilityText: string
  sourceRecordId: string
  recordedAtWorldTime: number
  causalSequence: number
  contentSafety: MedievalContentSafetyClassification
}

/** Bounded player-facing result text derives only from the canonical state. */
export const settlementTradeFeedback = (state: SettlementTradingState): SettlementTradeFeedback | undefined => {
  const diagnostics = validateSettlementTradingState(state)
  if (diagnostics.length) throw new SettlementTradingContractError(diagnostics)
  const contract = state.contracts[0]
  if (contract.status === 'offered') return undefined
  if (contract.status === 'accepted') return {
    id: 'settlement-trade-feedback:hearthford-mill-ironwork', state: 'ready',
    text: 'Hearthford tally: ironwork case awaits delivery to Jomon’s cargo hold.',
    accessibilityText: 'Hearthford Mill Quay public tally has recorded one ironwork case awaiting delivery to Jomon’s cargo hold.',
    sourceRecordId: `world-state:settlement-trading:${contract.id}`,
    recordedAtWorldTime: contract.recordedAtWorldTime, causalSequence: contract.causalSequence, contentSafety: safety()
  }
  if (contract.status === 'refused') return {
    id: 'settlement-trade-feedback:hearthford-mill-ironwork', state: 'warning',
    text: 'Hearthford tally: the ironwork handoff was refused; the case remains at the quay.',
    accessibilityText: 'Hearthford Mill Quay public tally records that the ironwork handoff was refused. The case remains at the quay.',
    sourceRecordId: `world-state:settlement-trading:${contract.id}`,
    recordedAtWorldTime: contract.recordedAtWorldTime, causalSequence: contract.causalSequence, contentSafety: safety()
  }
  return {
    id: 'settlement-trade-feedback:hearthford-mill-ironwork', state: 'neutral',
    text: 'Hearthford tally: Jomon received the ironwork case; the mill-race work can proceed.',
    accessibilityText: 'Hearthford Mill Quay public tally records that Jomon received the ironwork case. The mill-race work can proceed.',
    sourceRecordId: `world-state:settlement-trading:${contract.id}`,
    recordedAtWorldTime: contract.recordedAtWorldTime, causalSequence: contract.causalSequence, contentSafety: safety()
  }
}

/** The source owner never returns geometry; callers compare its private anchor. */
export const settlementTradingAnchorForPlan = (plan: JomonDeckPlan): { column: number; row: number } => {
  const area = plan.areas.find(candidate => candidate.id === SETTLEMENT_TRADING_SOURCE_AREA_ID)
  if (!area || area.kind !== 'shore-approach') throw new SettlementTradingContractError(['settlement-trading.invalid-location'])
  return structuredClone(area.anchor)
}
