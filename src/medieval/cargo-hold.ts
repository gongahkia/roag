import { commodityCatalogue, type CommodityDefinition, type JomonCommodityId } from './commodity-catalogue'

/**
 * Bounded physical cargo held by Jomon. Commodity definitions remain static
 * content; this owner contains only current lot identity, quantity, condition,
 * and whether a lot remains in the hold or is recoverable after a loss.
 */
export const VESSEL_CARGO_STATE_VERSION = 1 as const

export const VESSEL_CARGO_LIMITS = {
  lots: 24,
  quantityPerLot: 12
} as const

export const VESSEL_CARGO_CONDITIONS = ['sound', 'spoiled', 'damaged'] as const
export type VesselCargoCondition = typeof VESSEL_CARGO_CONDITIONS[number]

export const VESSEL_CARGO_STATUSES = ['in-hold', 'lost'] as const
export type VesselCargoStatus = typeof VESSEL_CARGO_STATUSES[number]

export const VESSEL_CARGO_FAILURE_OUTCOMES = ['spoilage', 'damage', 'loss'] as const
export type VesselCargoFailureOutcome = typeof VESSEL_CARGO_FAILURE_OUTCOMES[number]

export interface VesselCargoLot {
  id: string
  commodityId: JomonCommodityId
  quantity: number
  condition: VesselCargoCondition
  status: VesselCargoStatus
}

export interface VesselCargoState {
  version: typeof VESSEL_CARGO_STATE_VERSION
  lots: readonly VesselCargoLot[]
}

export type VesselCargoDiagnostic =
  | 'vessel-cargo.malformed-state'
  | 'vessel-cargo.invalid-version'
  | 'vessel-cargo.lot-limit'
  | 'vessel-cargo.invalid-lot'
  | 'vessel-cargo.duplicate-lot-id'
  | 'vessel-cargo.noncanonical-lot-order'
  | 'vessel-cargo.unknown-commodity'
  | 'vessel-cargo.invalid-quantity'
  | 'vessel-cargo.invalid-condition'
  | 'vessel-cargo.invalid-status'
  | 'vessel-cargo.capacity-exceeded'

export class VesselCargoContractError extends Error {
  constructor(readonly diagnostics: readonly VesselCargoDiagnostic[]) {
    super(`vessel cargo rejected: ${diagnostics.join(', ')}`)
    this.name = 'VesselCargoContractError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const keys = [...expected].sort(compare)
  return actual.length === keys.length && actual.every((key, index) => key === keys[index])
}
const positiveInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 1 && value <= VESSEL_CARGO_LIMITS.quantityPerLot
const validId = (value: unknown): value is string => typeof value === 'string' && /^cargo:[1-9][0-9]*:commodity:[a-z][a-z0-9-]*$/u.test(value)
const definitions = (): readonly CommodityDefinition[] => commodityCatalogue().commodities
const definitionFor = (commodityId: unknown): CommodityDefinition | undefined => definitions().find(definition => definition.id === commodityId)

/** One unit is derived only from the existing closed weight/bulk vocabulary. */
export const cargoUnitsPerCommodityUnit = (commodityId: JomonCommodityId): number => {
  const definition = definitionFor(commodityId)
  if (!definition) throw new VesselCargoContractError(['vessel-cargo.unknown-commodity'])
  const weight = definition.weight === 'light' ? 1 : definition.weight === 'medium' ? 2 : 3
  const bulk = definition.bulk === 'compact' ? 0 : 1
  return weight + bulk
}

export const cargoUnitsForLot = (lot: Pick<VesselCargoLot, 'commodityId' | 'quantity' | 'status'>): number => lot.status === 'in-hold'
  ? cargoUnitsPerCommodityUnit(lot.commodityId) * lot.quantity
  : 0

export const vesselCargoUsedUnits = (state: Pick<VesselCargoState, 'lots'>): number => state.lots.reduce((total, lot) => total + cargoUnitsForLot(lot), 0)

export const initialVesselCargoState = (): VesselCargoState => ({ version: VESSEL_CARGO_STATE_VERSION, lots: [] })

const validLot = (value: unknown): value is VesselCargoLot => record(value)
  && hasOnlyKeys(value, ['id', 'commodityId', 'quantity', 'condition', 'status'])
  && validId(value.id)
  && definitionFor(value.commodityId) !== undefined
  && positiveInteger(value.quantity)
  && VESSEL_CARGO_CONDITIONS.includes(value.condition as VesselCargoCondition)
  && VESSEL_CARGO_STATUSES.includes(value.status as VesselCargoStatus)

/** Structural validation; causal replay proves every accepted transition. */
export const validateVesselCargoState = (value: unknown, capacityUnits?: number): readonly VesselCargoDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'lots'])) return ['vessel-cargo.malformed-state']
  const diagnostics: VesselCargoDiagnostic[] = []
  if (value.version !== VESSEL_CARGO_STATE_VERSION) diagnostics.push('vessel-cargo.invalid-version')
  if (!Array.isArray(value.lots)) return [...diagnostics, 'vessel-cargo.malformed-state']
  if (value.lots.length > VESSEL_CARGO_LIMITS.lots) diagnostics.push('vessel-cargo.lot-limit')
  const ids = new Set<string>()
  for (const candidate of value.lots) {
    if (!validLot(candidate)) {
      if (record(candidate) && candidate.commodityId !== undefined && !definitionFor(candidate.commodityId)) diagnostics.push('vessel-cargo.unknown-commodity')
      else if (record(candidate) && candidate.quantity !== undefined && !positiveInteger(candidate.quantity)) diagnostics.push('vessel-cargo.invalid-quantity')
      else if (record(candidate) && candidate.condition !== undefined && !VESSEL_CARGO_CONDITIONS.includes(candidate.condition as VesselCargoCondition)) diagnostics.push('vessel-cargo.invalid-condition')
      else if (record(candidate) && candidate.status !== undefined && !VESSEL_CARGO_STATUSES.includes(candidate.status as VesselCargoStatus)) diagnostics.push('vessel-cargo.invalid-status')
      else diagnostics.push('vessel-cargo.invalid-lot')
      continue
    }
    if (ids.has(candidate.id)) diagnostics.push('vessel-cargo.duplicate-lot-id')
    ids.add(candidate.id)
  }
  const lots = value.lots.filter(validLot)
  if (lots.some((lot, index) => index > 0 && compare(lots[index - 1]!.id, lot.id) >= 0)) diagnostics.push('vessel-cargo.noncanonical-lot-order')
  if (capacityUnits !== undefined && (!Number.isSafeInteger(capacityUnits) || capacityUnits < 0 || vesselCargoUsedUnits({ lots }) > capacityUnits)) diagnostics.push('vessel-cargo.capacity-exceeded')
  return [...new Set(diagnostics)].sort(compare)
}

export const cargoLotIdFor = (causalSequence: number, commodityId: JomonCommodityId): string => {
  if (!Number.isSafeInteger(causalSequence) || causalSequence < 1 || !definitionFor(commodityId)) throw new VesselCargoContractError(['vessel-cargo.invalid-lot'])
  return `cargo:${causalSequence}:${commodityId}`
}

const validatedState = (state: VesselCargoState, capacityUnits: number): void => {
  const diagnostics = validateVesselCargoState(state, capacityUnits)
  if (diagnostics.length) throw new VesselCargoContractError(diagnostics)
}

/** Loads one known commodity lot after the world reducer proves hold proximity. */
export const loadVesselCargo = (
  state: VesselCargoState,
  capacityUnits: number,
  causalSequence: number,
  commodityId: JomonCommodityId,
  quantity: number
): VesselCargoState => {
  validatedState(state, capacityUnits)
  if (!definitionFor(commodityId)) throw new VesselCargoContractError(['vessel-cargo.unknown-commodity'])
  if (!positiveInteger(quantity)) throw new VesselCargoContractError(['vessel-cargo.invalid-quantity'])
  const lot: VesselCargoLot = { id: cargoLotIdFor(causalSequence, commodityId), commodityId, quantity, condition: 'sound', status: 'in-hold' }
  if (state.lots.some(candidate => candidate.id === lot.id) || state.lots.length >= VESSEL_CARGO_LIMITS.lots) throw new VesselCargoContractError([state.lots.length >= VESSEL_CARGO_LIMITS.lots ? 'vessel-cargo.lot-limit' : 'vessel-cargo.duplicate-lot-id'])
  const next = { version: VESSEL_CARGO_STATE_VERSION, lots: [...state.lots, lot].sort((left, right) => compare(left.id, right.id)) }
  validatedState(next, capacityUnits)
  return next
}

/** Unloading removes the lot and returns its capacity; causal history remains the event evidence. */
export const unloadVesselCargo = (state: VesselCargoState, capacityUnits: number, cargoId: string): VesselCargoState => {
  validatedState(state, capacityUnits)
  const lot = state.lots.find(candidate => candidate.id === cargoId)
  if (!lot || lot.status !== 'in-hold') throw new VesselCargoContractError(['vessel-cargo.invalid-lot'])
  return { version: VESSEL_CARGO_STATE_VERSION, lots: state.lots.filter(candidate => candidate.id !== cargoId).map(candidate => structuredClone(candidate)) }
}

/** A catalogue-bound spoilage, damage, or loss outcome never creates cargo. */
export const resolveVesselCargoFailure = (state: VesselCargoState, capacityUnits: number, cargoId: string, outcome: VesselCargoFailureOutcome): VesselCargoState => {
  validatedState(state, capacityUnits)
  if (!VESSEL_CARGO_FAILURE_OUTCOMES.includes(outcome)) throw new VesselCargoContractError(['vessel-cargo.invalid-condition'])
  const lot = state.lots.find(candidate => candidate.id === cargoId)
  if (!lot || lot.status !== 'in-hold') throw new VesselCargoContractError(['vessel-cargo.invalid-lot'])
  const definition = definitionFor(lot.commodityId)!
  if (outcome === 'spoilage' && definition.failureMode !== 'damp-spoilage') throw new VesselCargoContractError(['vessel-cargo.invalid-condition'])
  const nextLot: VesselCargoLot = outcome === 'loss'
    ? { ...lot, status: 'lost' }
    : { ...lot, condition: outcome === 'spoilage' ? 'spoiled' : 'damaged' }
  const next = { version: VESSEL_CARGO_STATE_VERSION, lots: state.lots.map(candidate => candidate.id === cargoId ? nextLot : structuredClone(candidate)) }
  validatedState(next, capacityUnits)
  return next
}

/** Recovery can return only an explicitly retained lost lot to the same hold capacity. */
export const recoverVesselCargo = (state: VesselCargoState, capacityUnits: number, cargoId: string): VesselCargoState => {
  validatedState(state, capacityUnits)
  const lot = state.lots.find(candidate => candidate.id === cargoId)
  if (!lot || lot.status !== 'lost') throw new VesselCargoContractError(['vessel-cargo.invalid-lot'])
  const next = { version: VESSEL_CARGO_STATE_VERSION, lots: state.lots.map(candidate => candidate.id === cargoId ? { ...candidate, status: 'in-hold' as const, condition: candidate.condition === 'sound' ? 'damaged' as const : candidate.condition } : structuredClone(candidate)) }
  validatedState(next, capacityUnits)
  return next
}
