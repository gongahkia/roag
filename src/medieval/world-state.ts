import { auditMedievalContentSafety, contentSafetyAuditMatches, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { createInitialFrontierState, frontierContentRecords, validateFrontierState, type FrontierState, type FrontierValidationDiagnosticCode } from './frontier'
import { INITIAL_WORLD_LIMITS, type InitialWorld } from './initial-world'
import { instantiateFoundationCrewPeople, persistentPersonContentRecords, PERSISTENT_PERSON_LIMITS, validatePersistentPeople, type PersistentPersonRecord, type PersistentPersonValidationDiagnosticCode } from './persistent-person'
import { isMedievalTemporalState, temporalContentRecords, type MedievalTemporalState } from './temporal'
import { generationConfigurationFingerprint, type WorldGenerationConfig } from './generation-config'
import { createSimulationCatchUpState, simulationCatchUpContentRecords, validateSimulationCatchUpState, SIMULATION_CATCH_UP_LIMITS, type SimulationCatchUpState, type SimulationCatchUpDiagnosticCode } from './simulation-catchup'
import { createWorldEraState, validateWorldEraState, WORLD_ERA_CONTRACT_VERSION, WORLD_ERA_LIMITS, type WorldEraDiagnosticCode, type WorldEraState } from './world-era'
import { causalHistoryContentRecords, causalReplayProjection, createCausalHistoryState, validateCausalHistoryState, validateLegacyCausalHistoryStateV4, type CausalHistoryDiagnosticCode, type CausalHistoryState, type CausalReplayProjection } from './causal-history'
import { createDelegationState, delegationContentRecords, validateDelegationSchedulerLinks, validateDelegationState, type DelegationDiagnosticCode, type DelegationState } from './delegation'
import { autonomyContentRecords, createAutonomyState, validateAutonomyState, type AutonomyDiagnosticCode, type AutonomyState } from './autonomy'
import { createSocialMemoryState, socialMemoryContentRecords, validateSocialMemoryState, type SocialMemoryDiagnosticCode, type SocialMemoryState } from './social-memory'
import { validateInitialHouseholdStructure, type InitialHouseholdValidationCode } from './initial-household'
import { initialVesselPropActionState, validateVesselPropActionState, vesselPropActionFeedbacks, type VesselPropActionState } from './vessel-prop-action'
import type { FoundationCrewMember, FoundationJomon } from './types'

/**
 * This is the durable mutable half of a medieval world. It deliberately has
 * no renderer, storage, browser, or prototype dependency.
 */
/** v15 adds one bounded latest-action record for each immutable vessel prop. */
export const MEDIEVAL_WORLD_STATE_VERSION = 15 as const
/** v14 has current courier continuity but no durable vessel-prop action state. */
export const LEGACY_VESSEL_PROP_ACTION_WORLD_STATE_VERSION = 14 as const
/** v13 separates immutable creation selection from the mutable active courier. */
export const LEGACY_ACTIVE_COURIER_WORLD_STATE_VERSION = 13 as const
/** v12 added local navigation but still overloaded the selected courier as active. */
export const LEGACY_MEDIEVAL_WORLD_STATE_VERSION = 12 as const
export const EARLIER_MEDIEVAL_WORLD_STATE_VERSION = 11 as const
export const WORLD_GEOGRAPHY_STATE_VERSION = 1 as const
export const WORLD_SITES_STATE_VERSION = 1 as const
export const WORLD_ROUTES_STATE_VERSION = 1 as const
export const WORLD_MARKETS_STATE_VERSION = 1 as const
export const WORLD_PEOPLE_STATE_VERSION = 5 as const
export const WORLD_INSTITUTIONS_STATE_VERSION = 1 as const
export const WORLD_DELEGATION_STATE_VERSION = 2 as const
export const WORLD_AUTONOMY_STATE_VERSION = 1 as const
export const WORLD_SOCIAL_MEMORY_STATE_VERSION = 1 as const
export const WORLD_CAUSAL_HISTORY_STATE_VERSION = 5 as const
export const WORLD_JOMON_STATE_VERSION = 2 as const
export const LEGACY_WORLD_JOMON_STATE_VERSION = 1 as const
export const WORLD_COURIER_STATE_VERSION = 3 as const
export const WORLD_DECK_NAVIGATION_STATE_VERSION = 1 as const
export const WORLD_ERA_STATE_VERSION = WORLD_ERA_CONTRACT_VERSION

/** Bounded containers keep the first persistent-state schema inspectable. */
export const MEDIEVAL_WORLD_STATE_LIMITS = {
  sites: INITIAL_WORLD_LIMITS.settlements + 12,
  quays: 8,
  routeConditions: INITIAL_WORLD_LIMITS.routes,
  markets: INITIAL_WORLD_LIMITS.settlements + 12,
  people: PERSISTENT_PERSON_LIMITS.records,
  institutions: INITIAL_WORLD_LIMITS.institutions,
  simulationCursors: SIMULATION_CATCH_UP_LIMITS.cursors,
  simulationRecords: SIMULATION_CATCH_UP_LIMITS.records,
  eraGrowthEvidence: WORLD_ERA_LIMITS.growthEvidence,
  eraTransitions: WORLD_ERA_LIMITS.transitions,
  delegationTasks: 24,
  socialMemoryRecords: 48,
  autonomyProcessing: 24,
  autonomyObservations: 24,
  causalHistoryTail: 8,
  capacityMaximum: 100
} as const

export interface WorldGeographyState {
  version: typeof WORLD_GEOGRAPHY_STATE_VERSION
  initialWorldId: string
  frontier: FrontierState
}

export type WorldSiteKind = 'initial-settlement' | 'frontier-materialization'

/** Site labels remain in their immutable initial/frontier source record. */
export interface WorldSiteState {
  id: string
  kind: WorldSiteKind
  regionId: string
  sourceId: string
  quayIds: readonly string[]
}

export interface WorldQuayState {
  id: string
  vesselId: string
  sourceQuayId: string
}

export interface WorldSitesState {
  version: typeof WORLD_SITES_STATE_VERSION
  sites: readonly WorldSiteState[]
  quays: readonly WorldQuayState[]
}

export type WorldRouteConditionKind = 'unassessed'

export interface WorldRouteConditionState {
  id: string
  routeId: string
  originSiteId: string
  destinationSiteId: string
  waterwayIds: readonly string[]
  condition: WorldRouteConditionKind
  assessedAtWorldTime: number
}

export interface WorldRoutesState {
  version: typeof WORLD_ROUTES_STATE_VERSION
  conditions: readonly WorldRouteConditionState[]
}

/** Commodity values are deliberately empty until the later market system. */
export interface WorldMarketState {
  id: string
  siteId: string
  commodityStates: readonly []
}

export interface WorldMarketsState {
  version: typeof WORLD_MARKETS_STATE_VERSION
  markets: readonly WorldMarketState[]
}

export interface WorldPeopleState {
  version: typeof WORLD_PEOPLE_STATE_VERSION
  /**
   * Delegation is the first public person mutation: its closed causal commands
   * replay work reservations, commitments, and source-linked social recall exactly.
   */
  records: readonly PersistentPersonRecord[]
}

export interface WorldDelegationState extends DelegationState {
  version: typeof WORLD_DELEGATION_STATE_VERSION
}

export interface WorldAutonomyState extends AutonomyState {
  version: typeof WORLD_AUTONOMY_STATE_VERSION
}

/** Source-linked delegation recall; it never replaces immutable foundation history. */
export interface WorldSocialMemoryState extends SocialMemoryState {
  version: typeof WORLD_SOCIAL_MEMORY_STATE_VERSION
}

export interface WorldInstitutionState {
  id: string
  sourceInstitutionId: string
  siteId: string
}

export interface WorldInstitutionsState {
  version: typeof WORLD_INSTITUTIONS_STATE_VERSION
  registry: readonly WorldInstitutionState[]
}

export interface WorldJomonLocation {
  kind: 'site' | 'quay'
  id: string
}

export interface WorldJomonState {
  version: typeof WORLD_JOMON_STATE_VERSION
  vesselId: 'vessel:jomon'
  operationalStatus: 'moored'
  location: WorldJomonLocation
  integrity: { current: number; maximum: number }
  capacity: { cargoUnits: number; berthSlots: number; workSlots: number }
  /** Canonical latest outcome only; static prop data remains immutable Jomon provenance. */
  propActions: VesselPropActionState
}

/** Read-only v14 state accepted only by the explicit full-envelope upgrader. */
export interface LegacyWorldJomonStateV1 {
  version: typeof LEGACY_WORLD_JOMON_STATE_VERSION
  vesselId: 'vessel:jomon'
  operationalStatus: 'moored'
  location: WorldJomonLocation
  integrity: { current: number; maximum: number }
  capacity: { cargoUnits: number; berthSlots: number; workSlots: number }
}

export interface WorldCourierState {
  version: typeof WORLD_COURIER_STATE_VERSION
  initialCourierId?: string
  /** Current perspective only; it can change at a later source-backed transition. */
  activeCourierId?: string
  /** Permanent Jomon departures retain person records but cannot become couriers again. */
  departedCourierIds?: readonly string[]
}

/** Read-only v12 input accepted solely by the explicit FoundationWorld upgrader. */
export interface LegacyWorldCourierStateV1 {
  version: 1
  initialCourierId?: string
}

/** Read-only v13 input accepted solely by the explicit FoundationWorld upgrader. */
export interface LegacyWorldCourierStateV2 {
  version: 2
  initialCourierId?: string
  activeCourierId?: string
}

/** The sole durable local-position owner; deck geometry remains in jomon-deck-plan. */
export interface WorldDeckNavigationState {
  version: typeof WORLD_DECK_NAVIGATION_STATE_VERSION
  courierId?: string
  coordinate?: { column: number; row: number }
}

export interface MedievalWorldState {
  version: typeof MEDIEVAL_WORLD_STATE_VERSION
  geography: WorldGeographyState
  sites: WorldSitesState
  routes: WorldRoutesState
  markets: WorldMarketsState
  people: WorldPeopleState
  institutions: WorldInstitutionsState
  delegation: WorldDelegationState
  autonomy: WorldAutonomyState
  socialMemory: WorldSocialMemoryState
  /** The sole authoritative mutable command journal; local evidence stays local. */
  causalHistory: CausalHistoryState
  jomon: WorldJomonState
  courier: WorldCourierState
  navigation: WorldDeckNavigationState
  temporal: MedievalTemporalState
  simulation: SimulationCatchUpState
  era: WorldEraState
  contentSafetyAudit: MedievalContentSafetyAudit
}

/** Read-only v12 input accepted solely by the explicit FoundationWorld upgrader. */
export interface LegacyMedievalWorldStateV12 extends Omit<MedievalWorldState, 'version' | 'courier'> {
  version: 12
  courier: LegacyWorldCourierStateV1
}

/** Read-only v13 input accepted solely by the explicit courier-continuity upgrader. */
export interface LegacyMedievalWorldStateV13 extends Omit<MedievalWorldState, 'version' | 'courier'> {
  version: 13
  courier: LegacyWorldCourierStateV2
}

/** Read-only v15/state-v14 envelope accepted only by the prop-action upgrader. */
export interface LegacyMedievalWorldStateV14 extends Omit<MedievalWorldState, 'version' | 'jomon'> {
  version: typeof LEGACY_VESSEL_PROP_ACTION_WORLD_STATE_VERSION
  jomon: LegacyWorldJomonStateV1
}

/** Read-only v11 input accepted by the existing navigation conversion. */
export interface LegacyMedievalWorldStateV11 extends Omit<LegacyMedievalWorldStateV12, 'version' | 'navigation'> {
  version: 11
}

export type WorldStateValidationDiagnosticCode =
  | 'world-state.malformed-state'
  | 'world-state.invalid-version'
  | 'world-state.invalid-geography'
  | 'world-state.invalid-frontier-root'
  | 'world-state.invalid-sites'
  | 'world-state.invalid-quays'
  | 'world-state.invalid-routes'
  | 'world-state.invalid-markets'
  | 'world-state.invalid-people'
  | 'world-state.invalid-institutions'
  | 'world-state.invalid-delegation'
  | 'world-state.invalid-autonomy'
  | 'world-state.invalid-social-memory'
  | 'world-state.invalid-causal-history'
  | 'world-state.invalid-jomon'
  | 'world-state.invalid-vessel-prop-actions'
  | 'world-state.invalid-courier'
  | 'world-state.invalid-temporal'
  | 'world-state.invalid-simulation'
  | 'world-state.invalid-era'
  | 'world-state.duplicate-id'
  | 'world-state.noncanonical-order'
  | 'world-state.invalid-reference'
  | 'world-state.budget-exceeded'
  | 'world-state.invalid-content-audit'
  | FrontierValidationDiagnosticCode
  | PersistentPersonValidationDiagnosticCode
  | SimulationCatchUpDiagnosticCode
  | WorldEraDiagnosticCode
  | DelegationDiagnosticCode
  | AutonomyDiagnosticCode
  | SocialMemoryDiagnosticCode
  | CausalHistoryDiagnosticCode
  | InitialHouseholdValidationCode
  | MedievalContentSafetyDiagnosticCode

export interface WorldStateValidationIssue {
  code: WorldStateValidationDiagnosticCode
  recordId: string
}

export interface WorldStateValidationContext {
  seed: string
  configuration: WorldGenerationConfig
  initialWorld: InitialWorld
  jomon: FoundationJomon
  crew: readonly FoundationCrewMember[]
}

export interface WorldStateConstructionContext extends WorldStateValidationContext {
  frontier: FrontierState
  temporal: MedievalTemporalState
  initialCourierId?: string
  activeCourierId?: string
  /** Only the courier-continuity reducer may construct selected/no-active terminal state. */
  terminalCrewExtinct?: true
  departedCourierIds?: readonly string[]
  /** Replayed local navigation, never browser-only focus state. */
  navigationState?: WorldDeckNavigationState
  /** Lets later reducers preserve Jomon values while retaining this v1 shape. */
  jomonState?: WorldJomonState
  /** Later person reducers must supply validated records; creation instantiates only crew. */
  peopleState?: WorldPeopleState
  /** The catch-up kernel owns its own bounded cursor and outcome state. */
  simulationState?: SimulationCatchUpState
  /** The era reducer owns its derived totals and transition history. */
  eraState?: WorldEraState
  /** The task/delegation reducer owns task lifecycle state and person links. */
  delegationState?: WorldDelegationState
  /** The autonomy reducer owns deterministic need pressure and observations. */
  autonomyState?: WorldAutonomyState
  /** Delegation/time reducers preserve source-linked social recall here. */
  socialMemoryState?: WorldSocialMemoryState
  /** Public command reducers supply their append/compaction result. */
  causalHistoryState?: CausalHistoryState
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const sortedById = <Value extends { id: string }>(values: readonly Value[]): readonly Value[] => [...values].sort((left, right) => compare(left.id, right.id))
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const isSortedById = <Value extends { id: string }>(values: readonly Value[]): boolean => values.every((value, index) => index === 0 || compare(values[index - 1]!.id, value.id) < 0)
const issue = (recordId: string, code: WorldStateValidationDiagnosticCode): WorldStateValidationIssue => ({ recordId, code })
const canonicalIssues = (issues: readonly WorldStateValidationIssue[]): readonly WorldStateValidationIssue[] => [...new Map(issues.map(value => [`${value.recordId}\u0000${value.code}`, value])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const idsAreUnique = (values: readonly { id: string }[]): boolean => new Set(values.map(value => value.id)).size === values.length
const stateLike = (value: unknown): value is MedievalWorldState | LegacyMedievalWorldStateV14 | LegacyMedievalWorldStateV13 | LegacyMedievalWorldStateV12 | LegacyMedievalWorldStateV11 => record(value)
  && ((value.version === EARLIER_MEDIEVAL_WORLD_STATE_VERSION && hasOnlyKeys(value, ['version', 'geography', 'sites', 'routes', 'markets', 'people', 'institutions', 'delegation', 'autonomy', 'socialMemory', 'causalHistory', 'jomon', 'courier', 'temporal', 'simulation', 'era', 'contentSafetyAudit']))
    || ((value.version === LEGACY_MEDIEVAL_WORLD_STATE_VERSION || value.version === LEGACY_ACTIVE_COURIER_WORLD_STATE_VERSION || value.version === LEGACY_VESSEL_PROP_ACTION_WORLD_STATE_VERSION || value.version === MEDIEVAL_WORLD_STATE_VERSION) && hasOnlyKeys(value, ['version', 'geography', 'sites', 'routes', 'markets', 'people', 'institutions', 'delegation', 'autonomy', 'socialMemory', 'causalHistory', 'jomon', 'courier', 'navigation', 'temporal', 'simulation', 'era', 'contentSafetyAudit'])))
const validateIdArrayOrder = (value: unknown, recordId: string, issues: WorldStateValidationIssue[]): void => {
  if (!Array.isArray(value) || !value.every(candidate => record(candidate) && validWorldId(candidate.id))) return
  const records = value as { id: string }[]
  if (!idsAreUnique(records)) issues.push(issue(recordId, 'world-state.duplicate-id'))
  if (!isSortedById(records)) issues.push(issue(recordId, 'world-state.noncanonical-order'))
}

const initialSiteRecords = (initialWorld: InitialWorld): readonly WorldSiteState[] => initialWorld.settlements.map(settlement => ({
  id: settlement.id,
  kind: 'initial-settlement' as const,
  regionId: initialWorld.id,
  sourceId: settlement.id,
  quayIds: []
}))

const frontierSiteRecords = (frontier: FrontierState): readonly WorldSiteState[] => frontier.regions
  .filter(region => region.status === 'materialized')
  .map(region => ({
    id: region.materialization.site.id,
    kind: 'frontier-materialization' as const,
    regionId: region.commitment.id,
    sourceId: region.materialization.site.id,
    quayIds: []
  }))

const siteRecordsFor = (initialWorld: InitialWorld, frontier: FrontierState): readonly WorldSiteState[] => sortedById([...initialSiteRecords(initialWorld), ...frontierSiteRecords(frontier)])
const quayRecordsFor = (jomon: FoundationJomon): readonly WorldQuayState[] => sortedById(jomon.quays.map(quay => ({ id: quay.id, vesselId: jomon.id, sourceQuayId: quay.id })))
const routeRecordsFor = (initialWorld: InitialWorld): readonly WorldRouteConditionState[] => sortedById(initialWorld.routes.map(route => ({
  id: `route-condition:${route.id}`,
  routeId: route.id,
  originSiteId: route.originSettlementId,
  destinationSiteId: route.destinationSettlementId,
  waterwayIds: [...route.waterwayIds],
  condition: 'unassessed' as const,
  assessedAtWorldTime: 0
})))
const marketRecordsFor = (sites: readonly WorldSiteState[]): readonly WorldMarketState[] => sortedById(sites.map(site => ({ id: `market:${site.id}`, siteId: site.id, commodityStates: [] as const })))
const institutionRecordsFor = (initialWorld: InitialWorld): readonly WorldInstitutionState[] => sortedById(initialWorld.institutions.map(institution => ({ id: institution.id, sourceInstitutionId: institution.id, siteId: institution.settlementId })))

/**
 * Player-visible mutable texts are limited to safety-audited frontier,
 * journal, and scheduler records. The era subdomain carries only closed semantic tags and
 * no displayable text; its exact-shape validator rejects a text-bearing bypass.
 */
type WorldStateContentSource = Pick<MedievalWorldState, 'geography' | 'people' | 'delegation' | 'autonomy' | 'socialMemory' | 'causalHistory' | 'temporal' | 'simulation'> & { jomon?: unknown }
export const medievalWorldStateContentRecords = (state: WorldStateContentSource): readonly ClassifiedMedievalContent[] => {
  const propActions = record(state.jomon) ? state.jomon.propActions : undefined
  return [
    ...frontierContentRecords(state.geography.frontier),
    ...persistentPersonContentRecords(state.people.records),
    ...delegationContentRecords(state.delegation),
    ...autonomyContentRecords(state.autonomy),
    ...socialMemoryContentRecords(state.socialMemory),
    ...causalHistoryContentRecords(state.causalHistory),
    ...(propActions === undefined ? [] : vesselPropActionFeedbacks(propActions as VesselPropActionState).map(item => ({ id: `vessel-prop-action:${item.propId}`, domain: 'event' as const, classification: item.contentSafety }))),
    ...temporalContentRecords(state.temporal),
    ...simulationCatchUpContentRecords(state.simulation)
  ]
}

const auditStateContent = (state: WorldStateContentSource): MedievalContentSafetyAudit => {
  const audit = auditMedievalContentSafety(medievalWorldStateContentRecords(state))
  if (audit.status === 'rejected') throw new Error(`world state content rejected: ${audit.diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
  return audit
}

const initialJomonState = (jomon: FoundationJomon, sites: readonly WorldSiteState[], crew: readonly FoundationCrewMember[]): WorldJomonState => ({
  version: WORLD_JOMON_STATE_VERSION,
  vesselId: jomon.id,
  operationalStatus: 'moored',
  location: { kind: 'site', id: sites[0]!.id },
  integrity: { current: 100, maximum: 100 },
  capacity: { cargoUnits: 12, berthSlots: crew.length, workSlots: 2 },
  propActions: initialVesselPropActionState(jomon)
})

/** Constructs only derived bounded containers; it cannot mutate creation evidence. */
export const createMedievalWorldState = (context: WorldStateConstructionContext): MedievalWorldState => {
  const sites = siteRecordsFor(context.initialWorld, context.frontier)
  const eraContext = {
    worldId: context.temporal.provenance.worldId,
    creationDigest: context.temporal.provenance.creationDigest,
    eraPace: context.configuration.eraPace,
    worldTime: context.temporal.worldTime,
    jomonVesselId: context.jomon.id,
    jomonPropIds: context.jomon.props.map(prop => prop.id)
  } as const
  const people: WorldPeopleState = {
    version: WORLD_PEOPLE_STATE_VERSION,
    records: structuredClone(context.peopleState?.records ?? instantiateFoundationCrewPeople({
      seed: context.seed,
      configurationFingerprint: generationConfigurationFingerprint(context.configuration),
      jomon: context.jomon,
      crew: context.crew
    }))
  }
  const simulation = structuredClone(context.simulationState ?? createSimulationCatchUpState())
  const era = structuredClone(context.eraState ?? createWorldEraState(eraContext))
  const delegation: WorldDelegationState = structuredClone(context.delegationState ?? createDelegationState())
  const autonomy: WorldAutonomyState = structuredClone(context.autonomyState ?? createAutonomyState(people.records, context.temporal.worldTime))
  const socialMemory: WorldSocialMemoryState = structuredClone(context.socialMemoryState ?? createSocialMemoryState())
  const courier: WorldCourierState = context.initialCourierId === undefined
    ? { version: WORLD_COURIER_STATE_VERSION }
    : {
        version: WORLD_COURIER_STATE_VERSION,
        initialCourierId: context.initialCourierId,
        ...(context.terminalCrewExtinct ? {} : { activeCourierId: context.activeCourierId ?? context.initialCourierId }),
        departedCourierIds: structuredClone(context.departedCourierIds ?? [])
      }
  const navigation: WorldDeckNavigationState = structuredClone(context.navigationState ?? { version: WORLD_DECK_NAVIGATION_STATE_VERSION })
  const stateWithoutAudit: Omit<MedievalWorldState, 'contentSafetyAudit'> = {
    version: MEDIEVAL_WORLD_STATE_VERSION,
    geography: { version: WORLD_GEOGRAPHY_STATE_VERSION, initialWorldId: context.initialWorld.id, frontier: structuredClone(context.frontier) },
    sites: { version: WORLD_SITES_STATE_VERSION, sites, quays: quayRecordsFor(context.jomon) },
    routes: { version: WORLD_ROUTES_STATE_VERSION, conditions: routeRecordsFor(context.initialWorld) },
    markets: { version: WORLD_MARKETS_STATE_VERSION, markets: marketRecordsFor(sites) },
    people,
    institutions: { version: WORLD_INSTITUTIONS_STATE_VERSION, registry: institutionRecordsFor(context.initialWorld) },
    delegation,
    autonomy,
    socialMemory,
    jomon: structuredClone(context.jomonState ?? initialJomonState(context.jomon, sites, context.crew)),
    courier,
    navigation,
    temporal: structuredClone(context.temporal),
    simulation,
    era,
    causalHistory: structuredClone(context.causalHistoryState ?? createCausalHistoryState({ worldId: context.temporal.provenance.worldId, creationDigest: context.temporal.provenance.creationDigest }, causalReplayProjection({
      courier,
      navigation,
      people,
      temporal: context.temporal,
      simulation,
      era,
      delegation,
      autonomy,
      socialMemory,
      jomon: structuredClone(context.jomonState ?? initialJomonState(context.jomon, sites, context.crew))
    })))
  }
  const state: MedievalWorldState = { ...stateWithoutAudit, contentSafetyAudit: auditStateContent(stateWithoutAudit) }
  const validation = validateMedievalWorldState(context, state)
  if (validation.length) throw new WorldStateContractError(validation)
  return state
}

const validSubdomain = (value: unknown, version: number, keys: readonly string[]): value is Record<string, unknown> => record(value) && value.version === version && hasOnlyKeys(value, keys)
const validWorldId = (value: unknown): value is string => typeof value === 'string' && value.length > 0
const validLocation = (value: unknown): value is WorldJomonLocation => record(value) && hasOnlyKeys(value, ['kind', 'id']) && (value.kind === 'site' || value.kind === 'quay') && validWorldId(value.id)
const rootCommitmentsMatch = (context: WorldStateValidationContext, frontier: FrontierState): boolean => {
  const roots = createInitialFrontierState({ seed: context.seed, configuration: context.configuration, initialWorld: context.initialWorld }).regions
  return roots.every(root => {
    const actual = frontier.regions.find(region => region.commitment.id === root.commitment.id)
    return actual !== undefined && same(actual.commitment, root.commitment)
  })
}

/** Pure, canonical validation for storage and reconstruction. */
export const validateMedievalWorldState = (context: WorldStateValidationContext, value: unknown): readonly WorldStateValidationIssue[] => {
  const issues: WorldStateValidationIssue[] = []
  if (!stateLike(value)) {
    const currentShape = record(value) && hasOnlyKeys(value, ['version', 'geography', 'sites', 'routes', 'markets', 'people', 'institutions', 'delegation', 'autonomy', 'socialMemory', 'causalHistory', 'jomon', 'courier', 'navigation', 'temporal', 'simulation', 'era', 'contentSafetyAudit'])
    const legacyShape = record(value) && hasOnlyKeys(value, ['version', 'geography', 'sites', 'routes', 'markets', 'people', 'institutions', 'delegation', 'autonomy', 'socialMemory', 'causalHistory', 'jomon', 'courier', 'temporal', 'simulation', 'era', 'contentSafetyAudit'])
    return [issue('world-state', currentShape || legacyShape ? 'world-state.invalid-version' : 'world-state.malformed-state')]
  }
  if (value.version !== MEDIEVAL_WORLD_STATE_VERSION && value.version !== LEGACY_VESSEL_PROP_ACTION_WORLD_STATE_VERSION && value.version !== LEGACY_ACTIVE_COURIER_WORLD_STATE_VERSION && value.version !== LEGACY_MEDIEVAL_WORLD_STATE_VERSION && value.version !== EARLIER_MEDIEVAL_WORLD_STATE_VERSION) issues.push(issue('world-state', 'world-state.invalid-version'))
  issues.push(...validateInitialHouseholdStructure(context.crew)
    .map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))

  let frontier: FrontierState | undefined
  if (!validSubdomain(value.geography, WORLD_GEOGRAPHY_STATE_VERSION, ['version', 'initialWorldId', 'frontier']) || value.geography.initialWorldId !== context.initialWorld.id || !record(value.geography.frontier)) {
    issues.push(issue('world-state:geography', 'world-state.invalid-geography'))
  } else {
    try {
      const candidate = value.geography.frontier as FrontierState
      const frontierIssues = validateFrontierState({ seed: context.seed, configuration: context.configuration, initialWorld: context.initialWorld }, candidate)
      if (frontierIssues.length) issues.push(...frontierIssues.map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))
      if (!rootCommitmentsMatch(context, candidate)) issues.push(issue('world-state:frontier', 'world-state.invalid-frontier-root'))
      if (!frontierIssues.length && rootCommitmentsMatch(context, candidate)) frontier = candidate
    } catch {
      issues.push(issue('world-state:geography', 'world-state.invalid-geography'))
    }
  }

  const expectedSites = frontier === undefined ? [] : siteRecordsFor(context.initialWorld, frontier)
  validateIdArrayOrder(value.sites?.sites, 'world-state:sites', issues)
  validateIdArrayOrder(value.sites?.quays, 'world-state:quays', issues)
  validateIdArrayOrder(value.routes?.conditions, 'world-state:routes', issues)
  validateIdArrayOrder(value.markets?.markets, 'world-state:markets', issues)
  validateIdArrayOrder(value.people?.records, 'world-state:people', issues)
  validateIdArrayOrder(value.institutions?.registry, 'world-state:institutions', issues)
  if (!validSubdomain(value.sites, WORLD_SITES_STATE_VERSION, ['version', 'sites', 'quays']) || !Array.isArray(value.sites.sites) || !Array.isArray(value.sites.quays) || value.sites.sites.length > MEDIEVAL_WORLD_STATE_LIMITS.sites || !same(value.sites.sites, expectedSites)) issues.push(issue('world-state:sites', value.sites && Array.isArray(value.sites.sites) && value.sites.sites.length > MEDIEVAL_WORLD_STATE_LIMITS.sites ? 'world-state.budget-exceeded' : 'world-state.invalid-sites'))
  const expectedQuays = quayRecordsFor(context.jomon)
  if (!validSubdomain(value.sites, WORLD_SITES_STATE_VERSION, ['version', 'sites', 'quays']) || !Array.isArray(value.sites.quays) || value.sites.quays.length > MEDIEVAL_WORLD_STATE_LIMITS.quays || !same(value.sites.quays, expectedQuays)) issues.push(issue('world-state:quays', value.sites && Array.isArray(value.sites.quays) && value.sites.quays.length > MEDIEVAL_WORLD_STATE_LIMITS.quays ? 'world-state.budget-exceeded' : 'world-state.invalid-quays'))

  const expectedRoutes = routeRecordsFor(context.initialWorld)
  if (!validSubdomain(value.routes, WORLD_ROUTES_STATE_VERSION, ['version', 'conditions']) || !Array.isArray(value.routes.conditions) || value.routes.conditions.length > MEDIEVAL_WORLD_STATE_LIMITS.routeConditions || !same(value.routes.conditions, expectedRoutes)) issues.push(issue('world-state:routes', value.routes && Array.isArray(value.routes.conditions) && value.routes.conditions.length > MEDIEVAL_WORLD_STATE_LIMITS.routeConditions ? 'world-state.budget-exceeded' : 'world-state.invalid-routes'))

  const expectedMarkets = marketRecordsFor(expectedSites)
  if (!validSubdomain(value.markets, WORLD_MARKETS_STATE_VERSION, ['version', 'markets']) || !Array.isArray(value.markets.markets) || value.markets.markets.length > MEDIEVAL_WORLD_STATE_LIMITS.markets || !same(value.markets.markets, expectedMarkets)) issues.push(issue('world-state:markets', value.markets && Array.isArray(value.markets.markets) && value.markets.markets.length > MEDIEVAL_WORLD_STATE_LIMITS.markets ? 'world-state.budget-exceeded' : 'world-state.invalid-markets'))

  const temporal = isMedievalTemporalState(value.temporal) ? value.temporal : undefined
  if (!temporal) issues.push(issue('world-state:temporal', 'world-state.invalid-temporal'))

  const simulationContext = temporal === undefined ? undefined : {
    worldId: temporal.provenance.worldId,
    creationDigest: temporal.provenance.creationDigest,
    worldTime: temporal.worldTime,
    personIds: Array.isArray(value.people?.records) ? value.people.records.filter(record).map(item => String(item.id)) : [],
    marketIds: Array.isArray(value.markets?.markets) ? value.markets.markets.filter(record).map(item => String(item.id)) : [],
    institutionIds: Array.isArray(value.institutions?.registry) ? value.institutions.registry.filter(record).map(item => String(item.id)) : []
  }
  if (simulationContext === undefined) {
    issues.push(issue('world-state:simulation', 'world-state.invalid-simulation'))
  } else {
    const simulationIssues = validateSimulationCatchUpState(simulationContext, value.simulation)
    if (simulationIssues.length) issues.push(...simulationIssues.map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))
  }

  if (temporal === undefined) {
    issues.push(issue('world-state:era', 'world-state.invalid-era'))
  } else {
    const eraIssues = validateWorldEraState({
      worldId: temporal.provenance.worldId,
      creationDigest: temporal.provenance.creationDigest,
      eraPace: context.configuration.eraPace,
      worldTime: temporal.worldTime,
      jomonVesselId: context.jomon.id,
      jomonPropIds: context.jomon.props.map(prop => prop.id)
    }, value.era)
    if (eraIssues.length) issues.push(...eraIssues.map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))
  }

  if (!validSubdomain(value.people, WORLD_PEOPLE_STATE_VERSION, ['version', 'records']) || !Array.isArray(value.people.records) || value.people.records.length > MEDIEVAL_WORLD_STATE_LIMITS.people) {
    issues.push(issue('world-state:people', value.people && Array.isArray(value.people.records) && value.people.records.length > MEDIEVAL_WORLD_STATE_LIMITS.people ? 'world-state.budget-exceeded' : 'world-state.invalid-people'))
  } else if (frontier === undefined || temporal === undefined) {
    issues.push(issue('world-state:people', 'world-state.invalid-people'))
  } else {
    const peopleIssues = validatePersistentPeople({
      seed: context.seed,
      configurationFingerprint: generationConfigurationFingerprint(context.configuration),
      initialWorld: context.initialWorld,
      frontier,
      jomon: context.jomon,
      crew: context.crew,
      siteIds: expectedSites.map(site => site.id),
      worldTime: temporal.worldTime
    }, value.people.records)
    if (peopleIssues.length) issues.push(...peopleIssues.map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))
  }

  if (!validSubdomain(value.delegation, WORLD_DELEGATION_STATE_VERSION, ['version', 'tasks', 'contentSafetyAudit']) || temporal === undefined || !Array.isArray(value.people?.records)) {
    issues.push(issue('world-state:delegation', 'world-state.invalid-delegation'))
  } else {
    const delegationIssues = validateDelegationState({
      worldId: temporal.provenance.worldId,
      creationDigest: temporal.provenance.creationDigest,
      worldTime: temporal.worldTime,
      people: value.people.records as PersistentPersonRecord[]
    }, value.delegation)
    if (delegationIssues.length) issues.push(...delegationIssues.map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))
    if (record(value.simulation) && Array.isArray(value.simulation.delegatedWork)) {
      const linkIssues = validateDelegationSchedulerLinks(value.delegation as WorldDelegationState, value.simulation.delegatedWork)
      if (linkIssues.length) issues.push(...linkIssues.map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))
    }
  }

  if (!validSubdomain(value.socialMemory, WORLD_SOCIAL_MEMORY_STATE_VERSION, ['version', 'records', 'contentSafetyAudit']) || temporal === undefined || !Array.isArray(value.people?.records) || !record(value.delegation) || !Array.isArray(value.delegation.tasks)) {
    issues.push(issue('world-state:social-memory', 'world-state.invalid-social-memory'))
  } else {
    const socialIssues = validateSocialMemoryState({
      worldId: temporal.provenance.worldId,
      creationDigest: temporal.provenance.creationDigest,
      worldTime: temporal.worldTime,
      people: value.people.records as PersistentPersonRecord[],
      tasks: value.delegation.tasks as DelegationState['tasks']
    }, value.socialMemory)
    if (socialIssues.length) issues.push(...socialIssues.map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))
  }

  if (!validSubdomain(value.autonomy, WORLD_AUTONOMY_STATE_VERSION, ['version', 'processing', 'observations', 'contentSafetyAudit']) || temporal === undefined || !Array.isArray(value.people?.records) || !record(value.delegation) || !record(value.era) || !record(value.simulation)) {
    issues.push(issue('world-state:autonomy', 'world-state.invalid-autonomy'))
  } else {
    const autonomyIssues = validateAutonomyState({
      worldId: temporal.provenance.worldId,
      creationDigest: temporal.provenance.creationDigest,
      worldTime: temporal.worldTime,
      activeCourierId: (value.courier?.version === WORLD_COURIER_STATE_VERSION || value.courier?.version === 2) && typeof value.courier.activeCourierId === 'string'
        ? value.courier.activeCourierId
        : typeof value.courier?.initialCourierId === 'string' ? value.courier.initialCourierId : undefined,
      people: value.people.records as PersistentPersonRecord[],
      delegation: value.delegation as WorldDelegationState,
      era: value.era as WorldEraState,
      simulation: value.simulation as SimulationCatchUpState
    }, value.autonomy)
    if (autonomyIssues.length) issues.push(...autonomyIssues.map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))
  }

  const expectedInstitutions = institutionRecordsFor(context.initialWorld)
  if (!validSubdomain(value.institutions, WORLD_INSTITUTIONS_STATE_VERSION, ['version', 'registry']) || !Array.isArray(value.institutions.registry) || value.institutions.registry.length > MEDIEVAL_WORLD_STATE_LIMITS.institutions || !same(value.institutions.registry, expectedInstitutions)) issues.push(issue('world-state:institutions', value.institutions && Array.isArray(value.institutions.registry) && value.institutions.registry.length > MEDIEVAL_WORLD_STATE_LIMITS.institutions ? 'world-state.budget-exceeded' : 'world-state.invalid-institutions'))

  const initialCourierId = value.courier?.initialCourierId
  const currentCourierContract = (value.version === MEDIEVAL_WORLD_STATE_VERSION || value.version === LEGACY_VESSEL_PROP_ACTION_WORLD_STATE_VERSION) && value.courier.version === WORLD_COURIER_STATE_VERSION
  const activeCourierId = currentCourierContract
    ? value.courier?.activeCourierId
    : initialCourierId
  const departedCourierIds = currentCourierContract && Array.isArray(value.courier?.departedCourierIds)
    ? value.courier.departedCourierIds
    : []
  const activeCourierPerson = Array.isArray(value.people?.records) ? value.people.records.find(candidate => record(candidate) && candidate.id === activeCourierId) : undefined
  const currentCourierKeys = initialCourierId === undefined
    ? ['version']
    : activeCourierId === undefined
      ? ['version', 'initialCourierId', 'departedCourierIds']
      : ['version', 'initialCourierId', 'activeCourierId', 'departedCourierIds']
  const legacyCourierKeys = initialCourierId === undefined ? ['version'] : ['version', 'initialCourierId']
  const activeCourierLegacyKeys = initialCourierId === undefined ? ['version'] : ['version', 'initialCourierId', 'activeCourierId']
  const courierShapeValid = value.version === MEDIEVAL_WORLD_STATE_VERSION || value.version === LEGACY_VESSEL_PROP_ACTION_WORLD_STATE_VERSION
    ? validSubdomain(value.courier, WORLD_COURIER_STATE_VERSION, currentCourierKeys)
    : value.version === LEGACY_ACTIVE_COURIER_WORLD_STATE_VERSION
      ? validSubdomain(value.courier, 2, activeCourierLegacyKeys)
      : validSubdomain(value.courier, 1, legacyCourierKeys)
  const initialCourierValid = initialCourierId === undefined
    ? activeCourierId === undefined
    : context.crew.some(member => member.id === initialCourierId && member.eligible)
  const canonicalDepartures = context.crew.filter(member => departedCourierIds.includes(member.id)).map(member => member.id)
  const departuresValid = currentCourierContract
    ? initialCourierId === undefined
      ? value.courier?.departedCourierIds === undefined
      : Array.isArray(value.courier?.departedCourierIds)
      && departedCourierIds.length === canonicalDepartures.length
      && departedCourierIds.every((id, index) => id === canonicalDepartures[index])
      && departedCourierIds.every(id => context.crew.some(member => member.id === id && member.eligible))
      && Array.isArray(value.people?.records)
      && departedCourierIds.every(id => {
        const person = value.people.records.find(candidate => record(candidate) && candidate.id === id)
        return record(person) && record(person.life) && person.life.status === 'living'
      })
    : departedCourierIds.length === 0
  const eligibleLivingNonDeparted = Array.isArray(value.people?.records)
    ? context.crew.some(member => member.eligible && !departedCourierIds.includes(member.id)
      && value.people.records.some(candidate => record(candidate) && candidate.id === member.id && record(candidate.life) && candidate.life.status === 'living'))
    : false
  const activeCourierValid = activeCourierId === undefined
    ? initialCourierId === undefined || !eligibleLivingNonDeparted
    : context.crew.some(member => member.id === activeCourierId && member.eligible)
      && !departedCourierIds.includes(activeCourierId)
      && record(activeCourierPerson) && record(activeCourierPerson.life) && activeCourierPerson.life.status === 'living'
  if (!courierShapeValid || !initialCourierValid || !departuresValid || !activeCourierValid) issues.push(issue('world-state:courier', 'world-state.invalid-courier'))

  const navigation = 'navigation' in value ? value.navigation : undefined
  const hasNavigation = value.version === MEDIEVAL_WORLD_STATE_VERSION || value.version === LEGACY_VESSEL_PROP_ACTION_WORLD_STATE_VERSION || value.version === LEGACY_ACTIVE_COURIER_WORLD_STATE_VERSION || value.version === LEGACY_MEDIEVAL_WORLD_STATE_VERSION
  const validCoordinate = (coordinate: unknown): coordinate is { column: number; row: number } => record(coordinate) && hasOnlyKeys(coordinate, ['column', 'row']) && safeInteger(coordinate.column) && safeInteger(coordinate.row)
  if (hasNavigation && (!validSubdomain(navigation, WORLD_DECK_NAVIGATION_STATE_VERSION, activeCourierId === undefined ? ['version'] : ['version', 'courierId', 'coordinate']) || (activeCourierId === undefined ? navigation.courierId !== undefined || navigation.coordinate !== undefined : navigation.courierId !== activeCourierId || !validCoordinate(navigation.coordinate)))) issues.push(issue('world-state:navigation', 'world-state.invalid-reference'))

  if (temporal === undefined) {
    issues.push(issue('world-state:causal-history', 'world-state.invalid-causal-history'))
  } else {
    const causalContext = { worldId: temporal.provenance.worldId, creationDigest: temporal.provenance.creationDigest }
    const causalIssues = value.version === MEDIEVAL_WORLD_STATE_VERSION
      ? validateCausalHistoryState(causalContext, value.causalHistory)
      : validateLegacyCausalHistoryStateV4(causalContext, value.causalHistory)
    if (causalIssues.length) issues.push(...causalIssues.map(diagnostic => issue(diagnostic.recordId, diagnostic.code)))
  }

  const sites = Array.isArray(value.sites?.sites) ? value.sites.sites as WorldSiteState[] : []
  const quays = Array.isArray(value.sites?.quays) ? value.sites.quays as WorldQuayState[] : []
  const jomon = value.jomon
  const jomonKeys = value.version === MEDIEVAL_WORLD_STATE_VERSION
    ? ['version', 'vesselId', 'operationalStatus', 'location', 'integrity', 'capacity', 'propActions']
    : ['version', 'vesselId', 'operationalStatus', 'location', 'integrity', 'capacity']
  const expectedJomonVersion = value.version === MEDIEVAL_WORLD_STATE_VERSION ? WORLD_JOMON_STATE_VERSION : LEGACY_WORLD_JOMON_STATE_VERSION
  if (!validSubdomain(jomon, expectedJomonVersion, jomonKeys) || jomon.vesselId !== context.jomon.id || jomon.operationalStatus !== 'moored' || !validLocation(jomon.location) || (jomon.location.kind === 'site' ? !sites.some(site => site.id === jomon.location.id) : !quays.some(quay => quay.id === jomon.location.id)) || !record(jomon.integrity) || !hasOnlyKeys(jomon.integrity, ['current', 'maximum']) || !safeInteger(jomon.integrity.current) || !safeInteger(jomon.integrity.maximum) || jomon.integrity.maximum < 1 || jomon.integrity.maximum > MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum || jomon.integrity.current > jomon.integrity.maximum || !record(jomon.capacity) || !hasOnlyKeys(jomon.capacity, ['cargoUnits', 'berthSlots', 'workSlots']) || !safeInteger(jomon.capacity.cargoUnits) || !safeInteger(jomon.capacity.berthSlots) || !safeInteger(jomon.capacity.workSlots) || jomon.capacity.cargoUnits > MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum || jomon.capacity.berthSlots > MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum || jomon.capacity.workSlots > MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum) {
    issues.push(issue('world-state:jomon', 'world-state.invalid-jomon'))
  } else if (value.version === MEDIEVAL_WORLD_STATE_VERSION) {
    const sequence = record(value.causalHistory) && record(value.causalHistory.checkpoint) && safeInteger(value.causalHistory.checkpoint.sequence) && Array.isArray(value.causalHistory.tail)
      ? value.causalHistory.checkpoint.sequence + value.causalHistory.tail.length
      : undefined
    const actionIssues = validateVesselPropActionState(context.jomon, jomon.propActions, temporal?.worldTime, sequence)
    if (actionIssues.length) issues.push(issue('world-state:jomon:prop-actions', 'world-state.invalid-vessel-prop-actions'))
  }

  try {
    if (value.contentSafetyAudit === undefined || !contentSafetyAuditMatches(medievalWorldStateContentRecords(value), value.contentSafetyAudit)) issues.push(issue('world-state:content-safety', 'world-state.invalid-content-audit'))
  } catch {
    issues.push(issue('world-state:content-safety', 'world-state.invalid-content-audit'))
  }
  return canonicalIssues(issues)
}

export const isMedievalWorldState = (context: WorldStateValidationContext, value: unknown): value is MedievalWorldState => validateMedievalWorldState(context, value).length === 0

/** The exact command-owned mutable projection used by the global journal. */
export const causalReplayProjectionForWorldState = (state: Pick<MedievalWorldState, 'courier' | 'people' | 'temporal' | 'simulation' | 'era' | 'delegation' | 'autonomy' | 'socialMemory' | 'jomon'> & Partial<Pick<MedievalWorldState, 'navigation'>>): CausalReplayProjection => causalReplayProjection(state)

export class WorldStateContractError extends Error {
  constructor(readonly diagnostics: readonly WorldStateValidationIssue[]) {
    super(`world state rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'WorldStateContractError'
  }
}
