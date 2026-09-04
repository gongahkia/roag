import { classifyMedievalContent, validateMedievalContentSafety, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { deriveJomonDeckPlan, deriveJomonDeckPlanForVerifiedWorld, type JomonDeckPlan, type JomonDeckPlanCoordinate, type JomonDeckPlanPropBinding } from './jomon-deck-plan'
import type { FoundationWorld, JomonVesselPropKind } from './types'

/**
 * Pure availability assessment for the three existing deck-plan prop bindings.
 * It never owns geometry, input, mutable state, replay, persistence, or time.
 */
export const VESSEL_PROXIMITY_OPERATION_CONTRACT_VERSION = 1 as const

export const VESSEL_PROXIMITY_OPERATION_PROP_IDS = [
  'prop:chart-table',
  'prop:gangplank',
  'prop:task-ledger'
] as const
export type VesselProximityOperationPropId = typeof VESSEL_PROXIMITY_OPERATION_PROP_IDS[number]

export const VESSEL_PROXIMITY_OPERATION_AVAILABILITY = ['implemented', 'reserved', 'unavailable'] as const
export type VesselProximityOperationAvailability = typeof VESSEL_PROXIMITY_OPERATION_AVAILABILITY[number]

export const VESSEL_PROXIMITY_OPERATION_REASONS = [
  'not-at-prop-anchor',
  'route-comparison-not-implemented',
  'quay-travel-not-implemented'
] as const
export type VesselProximityOperationReason = typeof VESSEL_PROXIMITY_OPERATION_REASONS[number]

export const VESSEL_PROXIMITY_STATES = ['at-anchor', 'away-from-anchor'] as const
export type VesselProximityState = typeof VESSEL_PROXIMITY_STATES[number]

export interface VesselProximityOperationSource {
  propBindingId: string
  propId: VesselProximityOperationPropId
  propKind: JomonVesselPropKind
  areaId: string
}

export interface VesselProximityOperation {
  source: VesselProximityOperationSource
  proximity: VesselProximityState
  availability: VesselProximityOperationAvailability
  reason?: VesselProximityOperationReason
  contentSafety: MedievalContentSafetyClassification
}

/** No coordinate is exposed: this only reports whether the active courier is on a prop anchor. */
export type VesselActivePropAnchor =
  | { status: 'no-prop-anchor' }
  | { status: 'at-prop-anchor'; propId: VesselProximityOperationPropId }

export interface VesselProximityOperationAssessment {
  version: typeof VESSEL_PROXIMITY_OPERATION_CONTRACT_VERSION
  activeAnchor: VesselActivePropAnchor
  operations: readonly VesselProximityOperation[]
}

/** Minimal read-only current-position evidence. It deliberately has no world-time or person fields. */
export interface VesselProximityOperationProjection {
  courier: { activeCourierId?: string }
  navigation?: { courierId?: string; coordinate?: JomonDeckPlanCoordinate }
}

export type VesselProximityOperationDiagnosticCode =
  | 'vessel-proximity-operation.invalid-foundation-world'
  | 'vessel-proximity-operation.invalid-deck-plan'
  | 'vessel-proximity-operation.unknown-prop'
  | 'vessel-proximity-operation.invalid-active-courier'
  | 'vessel-proximity-operation.invalid-navigation'
  | 'vessel-proximity-operation.invalid-prop-binding'
  | 'vessel-proximity-operation.malformed-assessment'
  | 'vessel-proximity-operation.invalid-contract-version'
  | 'vessel-proximity-operation.invalid-active-anchor'
  | 'vessel-proximity-operation.invalid-operation'
  | 'vessel-proximity-operation.noncanonical-operation-order'
  | 'vessel-proximity-operation.invalid-content-safety'
  | MedievalContentSafetyDiagnosticCode

export interface VesselProximityOperationDiagnostic {
  recordId: string
  code: VesselProximityOperationDiagnosticCode
}

export class VesselProximityOperationContractError extends Error {
  constructor(readonly diagnostics: readonly VesselProximityOperationDiagnostic[]) {
    super(`vessel proximity operation rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'VesselProximityOperationContractError'
  }
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const actual = Object.keys(value).sort(compare)
  const keys = [...expected].sort(compare)
  return actual.length === keys.length && actual.every((key, index) => key === keys[index])
}
const safeId = (value: unknown): value is string => typeof value === 'string'
  && value.length > 0
  && value.length <= 96
  && /^[a-z][a-z0-9-]*(?::[a-z0-9][a-z0-9-]*)*$/u.test(value)
const safeCoordinate = (value: unknown): value is JomonDeckPlanCoordinate => record(value)
  && hasOnlyKeys(value, ['column', 'row'])
  && typeof value.column === 'number'
  && Number.isSafeInteger(value.column)
  && typeof value.row === 'number'
  && Number.isSafeInteger(value.row)
const sameCoordinate = (left: JomonDeckPlanCoordinate, right: JomonDeckPlanCoordinate): boolean => left.column === right.column && left.row === right.row
const issue = (recordId: string, code: VesselProximityOperationDiagnosticCode): VesselProximityOperationDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly VesselProximityOperationDiagnostic[]): readonly VesselProximityOperationDiagnostic[] => [...new Map(diagnostics.map(item => [`${item.recordId}\u0000${item.code}`, item])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))

interface VesselPropOperationDefinition {
  propId: VesselProximityOperationPropId
  propKind: JomonVesselPropKind
  areaId: string
  atAnchorAvailability: Exclude<VesselProximityOperationAvailability, 'unavailable'>
  reason?: VesselProximityOperationReason
}

const DEFINITIONS: readonly VesselPropOperationDefinition[] = [
  { propId: 'prop:chart-table', propKind: 'table', areaId: 'chart-table', atAnchorAvailability: 'reserved', reason: 'route-comparison-not-implemented' },
  { propId: 'prop:gangplank', propKind: 'gangplank', areaId: 'gangplank', atAnchorAvailability: 'reserved', reason: 'quay-travel-not-implemented' },
  { propId: 'prop:task-ledger', propKind: 'ledger', areaId: 'tavern', atAnchorAvailability: 'implemented' }
]

const operationSafety = (): MedievalContentSafetyClassification => classifyMedievalContent('template', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])
const definitionFor = (propId: unknown): VesselPropOperationDefinition | undefined => DEFINITIONS.find(definition => definition.propId === propId)
const bindingIdFor = (propId: VesselProximityOperationPropId): string => `deck-prop-binding:${propId}`
const sourceForBinding = (binding: JomonDeckPlanPropBinding): VesselProximityOperationSource => ({
  propBindingId: binding.id,
  propId: binding.propId as VesselProximityOperationPropId,
  propKind: binding.propKind,
  areaId: binding.areaId
})

const validExpectedBinding = (binding: JomonDeckPlanPropBinding, definition: VesselPropOperationDefinition): boolean => binding.id === bindingIdFor(definition.propId)
  && binding.propId === definition.propId
  && binding.propKind === definition.propKind
  && binding.areaId === definition.areaId
  && safeCoordinate(binding.anchor)

const planBindings = (plan: JomonDeckPlan): readonly JomonDeckPlanPropBinding[] => {
  if (plan.propBindings.length !== DEFINITIONS.length) throw new VesselProximityOperationContractError([issue('deck-prop-bindings', 'vessel-proximity-operation.invalid-prop-binding')])
  const bindings = DEFINITIONS.map((definition, index) => {
    const binding = plan.propBindings[index]
    if (!binding || !validExpectedBinding(binding, definition)) throw new VesselProximityOperationContractError([issue(binding?.id ?? definition.propId, 'vessel-proximity-operation.invalid-prop-binding')])
    return binding
  })
  if (new Set(bindings.map(binding => binding.id)).size !== bindings.length) throw new VesselProximityOperationContractError([issue('deck-prop-bindings', 'vessel-proximity-operation.invalid-prop-binding')])
  return bindings
}

const areaContains = (plan: JomonDeckPlan, coordinate: JomonDeckPlanCoordinate): boolean => plan.areas.some(area => area.footprint.some(cell => sameCoordinate(cell.coordinate, coordinate)))

const validatedProjection = (world: FoundationWorld, plan: JomonDeckPlan, projection: VesselProximityOperationProjection): { activeCourierId: string; coordinate: JomonDeckPlanCoordinate } => {
  if (!record(projection) || !record(projection.courier) || !safeId(projection.courier.activeCourierId) || !world.crew.some(member => member.id === projection.courier.activeCourierId)) {
    throw new VesselProximityOperationContractError([issue('world-state:courier', 'vessel-proximity-operation.invalid-active-courier')])
  }
  if (!record(projection.navigation) || projection.navigation.courierId !== projection.courier.activeCourierId || !safeCoordinate(projection.navigation.coordinate) || !areaContains(plan, projection.navigation.coordinate)) {
    throw new VesselProximityOperationContractError([issue('world-state:navigation', 'vessel-proximity-operation.invalid-navigation')])
  }
  return { activeCourierId: projection.courier.activeCourierId, coordinate: projection.navigation.coordinate }
}

const operationFor = (binding: JomonDeckPlanPropBinding, coordinate: JomonDeckPlanCoordinate): VesselProximityOperation => {
  const definition = definitionFor(binding.propId)
  if (!definition || !validExpectedBinding(binding, definition)) throw new VesselProximityOperationContractError([issue(binding.id, 'vessel-proximity-operation.invalid-prop-binding')])
  const proximity: VesselProximityState = sameCoordinate(binding.anchor, coordinate) ? 'at-anchor' : 'away-from-anchor'
  const source = sourceForBinding(binding)
  if (proximity === 'away-from-anchor') return {
    source,
    proximity,
    availability: 'unavailable',
    reason: 'not-at-prop-anchor',
    contentSafety: operationSafety()
  }
  return {
    source,
    proximity,
    availability: definition.atAnchorAvailability,
    ...(definition.reason === undefined ? {} : { reason: definition.reason }),
    contentSafety: operationSafety()
  }
}

const assessmentFor = (world: FoundationWorld, plan: JomonDeckPlan, projection: VesselProximityOperationProjection): VesselProximityOperationAssessment => {
  const position = validatedProjection(world, plan, projection)
  const operations = planBindings(plan).map(binding => operationFor(binding, position.coordinate))
  const active = operations.find(operation => operation.proximity === 'at-anchor')
  const assessment: VesselProximityOperationAssessment = {
    version: VESSEL_PROXIMITY_OPERATION_CONTRACT_VERSION,
    activeAnchor: active === undefined ? { status: 'no-prop-anchor' } : { status: 'at-prop-anchor', propId: active.source.propId },
    operations
  }
  const diagnostics = validateVesselProximityOperationAssessment(assessment)
  if (diagnostics.length) throw new VesselProximityOperationContractError(diagnostics)
  return assessment
}

const planForWorld = (world: FoundationWorld, verified: boolean): JomonDeckPlan => {
  try {
    return verified ? deriveJomonDeckPlanForVerifiedWorld(world) : deriveJomonDeckPlan(world)
  } catch {
    throw new VesselProximityOperationContractError([issue('foundation-world', verified ? 'vessel-proximity-operation.invalid-deck-plan' : 'vessel-proximity-operation.invalid-foundation-world')])
  }
}

/** Validates the complete world before deriving its bounded, discardable prop assessment. */
export const assessVesselProximityOperations = (world: FoundationWorld, projection: VesselProximityOperationProjection): VesselProximityOperationAssessment => assessmentFor(world, planForWorld(world, false), projection)

/** Reuses an already validated FoundationWorld boundary without owning that validation. */
export const assessVesselProximityOperationsForVerifiedWorld = (world: FoundationWorld, projection: VesselProximityOperationProjection): VesselProximityOperationAssessment => assessmentFor(world, planForWorld(world, true), projection)

export const assessVesselPropOperationForVerifiedWorld = (
  world: FoundationWorld,
  projection: VesselProximityOperationProjection,
  propId: VesselProximityOperationPropId
): VesselProximityOperation => {
  if (!definitionFor(propId)) throw new VesselProximityOperationContractError([issue(String(propId), 'vessel-proximity-operation.unknown-prop')])
  const operation = assessVesselProximityOperationsForVerifiedWorld(world, projection).operations.find(candidate => candidate.source.propId === propId)
  if (!operation) throw new VesselProximityOperationContractError([issue(propId, 'vessel-proximity-operation.invalid-prop-binding')])
  return structuredClone(operation)
}

/** Validates one canonical plan binding for an existing integration without exposing its anchor. */
export const vesselPropOperationSourceForVerifiedWorld = (world: FoundationWorld, propId: VesselProximityOperationPropId): VesselProximityOperationSource => {
  const definition = definitionFor(propId)
  if (!definition) throw new VesselProximityOperationContractError([issue(String(propId), 'vessel-proximity-operation.unknown-prop')])
  const binding = planBindings(planForWorld(world, true)).find(candidate => candidate.propId === propId)
  if (!binding) throw new VesselProximityOperationContractError([issue(propId, 'vessel-proximity-operation.invalid-prop-binding')])
  return structuredClone(sourceForBinding(binding))
}

const validSource = (value: unknown, definition: VesselPropOperationDefinition): value is VesselProximityOperationSource => record(value)
  && hasOnlyKeys(value, ['propBindingId', 'propId', 'propKind', 'areaId'])
  && value.propBindingId === bindingIdFor(definition.propId)
  && value.propId === definition.propId
  && value.propKind === definition.propKind
  && value.areaId === definition.areaId

const validOperation = (value: unknown, definition: VesselPropOperationDefinition, activePropId: VesselProximityOperationPropId | undefined): boolean => {
  if (!record(value) || !hasOnlyKeys(value, ['source', 'proximity', 'availability', ...(Object.hasOwn(value, 'reason') ? ['reason'] : []), 'contentSafety']) || !validSource(value.source, definition) || (value.proximity !== 'at-anchor' && value.proximity !== 'away-from-anchor') || !same(value.contentSafety, operationSafety())) return false
  if (validateMedievalContentSafety([{ id: bindingIdFor(definition.propId), domain: 'template', classification: value.contentSafety }]).status === 'rejected') return false
  const expectedProximity: VesselProximityState = activePropId === definition.propId ? 'at-anchor' : 'away-from-anchor'
  if (value.proximity !== expectedProximity) return false
  if (expectedProximity === 'away-from-anchor') return value.availability === 'unavailable' && value.reason === 'not-at-prop-anchor'
  return value.availability === definition.atAnchorAvailability && (definition.reason === undefined ? !Object.hasOwn(value, 'reason') : value.reason === definition.reason)
}

/** Strictly validates a public assessment before any renderer or future operation owner consumes it. */
export const validateVesselProximityOperationAssessment = (value: unknown): readonly VesselProximityOperationDiagnostic[] => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'activeAnchor', 'operations'])) return [issue('vessel-proximity-operation:assessment', 'vessel-proximity-operation.malformed-assessment')]
  const diagnostics: VesselProximityOperationDiagnostic[] = []
  if (value.version !== VESSEL_PROXIMITY_OPERATION_CONTRACT_VERSION) diagnostics.push(issue('vessel-proximity-operation:assessment', 'vessel-proximity-operation.invalid-contract-version'))
  let activePropId: VesselProximityOperationPropId | undefined
  if (!record(value.activeAnchor)) {
    diagnostics.push(issue('vessel-proximity-operation:assessment', 'vessel-proximity-operation.invalid-active-anchor'))
  } else if (value.activeAnchor.status === 'no-prop-anchor' && hasOnlyKeys(value.activeAnchor, ['status'])) {
    activePropId = undefined
  } else if (value.activeAnchor.status === 'at-prop-anchor' && hasOnlyKeys(value.activeAnchor, ['status', 'propId']) && definitionFor(value.activeAnchor.propId)) {
    activePropId = value.activeAnchor.propId as VesselProximityOperationPropId
  } else {
    diagnostics.push(issue('vessel-proximity-operation:assessment', 'vessel-proximity-operation.invalid-active-anchor'))
  }
  if (!Array.isArray(value.operations) || value.operations.length !== DEFINITIONS.length) {
    diagnostics.push(issue('vessel-proximity-operation:assessment', 'vessel-proximity-operation.malformed-assessment'))
  } else {
    for (const [index, definition] of DEFINITIONS.entries()) {
      const operation = value.operations[index]
      if (!record(operation) || !record(operation.source) || operation.source.propId !== definition.propId) diagnostics.push(issue(`vessel-proximity-operation:${index}`, 'vessel-proximity-operation.noncanonical-operation-order'))
      if (!validOperation(operation, definition, activePropId)) {
        const code = record(operation) && Object.hasOwn(operation, 'contentSafety') && !same(operation.contentSafety, operationSafety())
          ? 'vessel-proximity-operation.invalid-content-safety'
          : 'vessel-proximity-operation.invalid-operation'
        diagnostics.push(issue(`vessel-proximity-operation:${definition.propId}`, code))
      }
    }
  }
  return canonicalDiagnostics(diagnostics)
}
