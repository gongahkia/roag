import { auditMedievalContentSafety, contentSafetyAuditMatches, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { createInitialFrontierState, frontierContentRecords, validateFrontierState, type FrontierState, type FrontierValidationDiagnosticCode } from './frontier'
import { INITIAL_WORLD_LIMITS, type InitialWorld } from './initial-world'
import { isMedievalTemporalState, TEMPORAL_LIMITS, temporalContentRecords, type MedievalTemporalState } from './temporal'
import type { WorldGenerationConfig } from './generation-config'
import type { CausalRecord, FoundationCrewMember, FoundationJomon } from './types'

/**
 * This is the durable mutable half of a medieval world. It deliberately has
 * no renderer, storage, browser, or prototype dependency.
 */
export const MEDIEVAL_WORLD_STATE_VERSION = 1 as const
export const WORLD_GEOGRAPHY_STATE_VERSION = 1 as const
export const WORLD_SITES_STATE_VERSION = 1 as const
export const WORLD_ROUTES_STATE_VERSION = 1 as const
export const WORLD_MARKETS_STATE_VERSION = 1 as const
export const WORLD_PEOPLE_STATE_VERSION = 1 as const
export const WORLD_INSTITUTIONS_STATE_VERSION = 1 as const
export const WORLD_HISTORY_STATE_VERSION = 1 as const
export const WORLD_JOMON_STATE_VERSION = 1 as const
export const WORLD_COURIER_STATE_VERSION = 1 as const

/** Bounded containers keep the first persistent-state schema inspectable. */
export const MEDIEVAL_WORLD_STATE_LIMITS = {
  sites: INITIAL_WORLD_LIMITS.settlements + 12,
  quays: 8,
  routeConditions: INITIAL_WORLD_LIMITS.routes,
  markets: INITIAL_WORLD_LIMITS.settlements + 12,
  people: INITIAL_WORLD_LIMITS.people + 6,
  institutions: INITIAL_WORLD_LIMITS.institutions,
  historyRecords: TEMPORAL_LIMITS.causalRecords + 2,
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

export type WorldPersonRegistryOrigin = 'foundation-crew' | 'initial-person-seed'

export interface WorldPersonResidence {
  kind: 'vessel' | 'site'
  id: string
}

/** Registry entries identify existing generated people without inventing their later simulation fields. */
export interface WorldPersonRegistryEntry {
  id: string
  origin: WorldPersonRegistryOrigin
  sourceId: string
  residence: WorldPersonResidence
}

export interface WorldPeopleState {
  version: typeof WORLD_PEOPLE_STATE_VERSION
  registry: readonly WorldPersonRegistryEntry[]
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

export interface WorldHistoryState {
  version: typeof WORLD_HISTORY_STATE_VERSION
  records: readonly CausalRecord[]
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
}

export interface WorldCourierState {
  version: typeof WORLD_COURIER_STATE_VERSION
  initialCourierId?: string
}

export interface MedievalWorldState {
  version: typeof MEDIEVAL_WORLD_STATE_VERSION
  geography: WorldGeographyState
  sites: WorldSitesState
  routes: WorldRoutesState
  markets: WorldMarketsState
  people: WorldPeopleState
  institutions: WorldInstitutionsState
  history: WorldHistoryState
  jomon: WorldJomonState
  courier: WorldCourierState
  temporal: MedievalTemporalState
  contentSafetyAudit: MedievalContentSafetyAudit
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
  | 'world-state.invalid-history'
  | 'world-state.invalid-jomon'
  | 'world-state.invalid-courier'
  | 'world-state.invalid-temporal'
  | 'world-state.duplicate-id'
  | 'world-state.noncanonical-order'
  | 'world-state.invalid-reference'
  | 'world-state.budget-exceeded'
  | 'world-state.invalid-content-audit'
  | FrontierValidationDiagnosticCode
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
  /** The deterministic creation records for the current courier choice. */
  foundationHistory: readonly CausalRecord[]
}

export interface WorldStateConstructionContext extends WorldStateValidationContext {
  frontier: FrontierState
  temporal: MedievalTemporalState
  initialCourierId?: string
  /** Lets later reducers preserve Jomon values while retaining this v1 shape. */
  jomonState?: WorldJomonState
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
const stateLike = (value: unknown): value is MedievalWorldState => record(value) && hasOnlyKeys(value, ['version', 'geography', 'sites', 'routes', 'markets', 'people', 'institutions', 'history', 'jomon', 'courier', 'temporal', 'contentSafetyAudit'])
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
const peopleRecordsFor = (initialWorld: InitialWorld, jomon: FoundationJomon, crew: readonly FoundationCrewMember[]): readonly WorldPersonRegistryEntry[] => sortedById([
  ...crew.map(member => ({ id: member.id, origin: 'foundation-crew' as const, sourceId: member.id, residence: { kind: 'vessel' as const, id: jomon.id } })),
  ...initialWorld.people.map(person => ({ id: person.id, origin: 'initial-person-seed' as const, sourceId: person.id, residence: { kind: 'site' as const, id: person.settlementId } }))
])
const institutionRecordsFor = (initialWorld: InitialWorld): readonly WorldInstitutionState[] => sortedById(initialWorld.institutions.map(institution => ({ id: institution.id, sourceInstitutionId: institution.id, siteId: institution.settlementId })))

export const causalHistoryWithTemporal = (foundationRecords: readonly CausalRecord[], temporal: MedievalTemporalState): readonly CausalRecord[] => [
  ...foundationRecords,
  ...temporal.causalRecords.map(record => ({
    sequence: foundationRecords.length + record.sequence,
    atWorldTime: record.atWorldTime,
    kind: record.kind === 'action-completed' ? 'temporal-action' as const : 'scheduled-event-resolved' as const,
    detail: record.detail,
    contentSafety: record.contentSafety
  }))
]

/** Player-visible mutable texts are limited to safety-audited frontier/history/scheduler records. */
export const medievalWorldStateContentRecords = (state: Pick<MedievalWorldState, 'geography' | 'history' | 'temporal'>): readonly ClassifiedMedievalContent[] => [
  ...frontierContentRecords(state.geography.frontier),
  ...state.history.records.map(record => ({ id: `world-state:causal:${record.sequence}:${record.kind}`, domain: 'event' as const, classification: record.contentSafety })),
  ...temporalContentRecords(state.temporal)
]

const auditStateContent = (state: Pick<MedievalWorldState, 'geography' | 'history' | 'temporal'>): MedievalContentSafetyAudit => {
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
  capacity: { cargoUnits: 12, berthSlots: crew.length, workSlots: 2 }
})

/** Constructs only derived bounded containers; it cannot mutate creation evidence. */
export const createMedievalWorldState = (context: WorldStateConstructionContext): MedievalWorldState => {
  const sites = siteRecordsFor(context.initialWorld, context.frontier)
  const stateWithoutAudit: Omit<MedievalWorldState, 'contentSafetyAudit'> = {
    version: MEDIEVAL_WORLD_STATE_VERSION,
    geography: { version: WORLD_GEOGRAPHY_STATE_VERSION, initialWorldId: context.initialWorld.id, frontier: structuredClone(context.frontier) },
    sites: { version: WORLD_SITES_STATE_VERSION, sites, quays: quayRecordsFor(context.jomon) },
    routes: { version: WORLD_ROUTES_STATE_VERSION, conditions: routeRecordsFor(context.initialWorld) },
    markets: { version: WORLD_MARKETS_STATE_VERSION, markets: marketRecordsFor(sites) },
    people: { version: WORLD_PEOPLE_STATE_VERSION, registry: peopleRecordsFor(context.initialWorld, context.jomon, context.crew) },
    institutions: { version: WORLD_INSTITUTIONS_STATE_VERSION, registry: institutionRecordsFor(context.initialWorld) },
    history: { version: WORLD_HISTORY_STATE_VERSION, records: causalHistoryWithTemporal(context.foundationHistory, context.temporal) },
    jomon: structuredClone(context.jomonState ?? initialJomonState(context.jomon, sites, context.crew)),
    courier: { version: WORLD_COURIER_STATE_VERSION, ...(context.initialCourierId === undefined ? {} : { initialCourierId: context.initialCourierId }) },
    temporal: structuredClone(context.temporal)
  }
  const state: MedievalWorldState = { ...stateWithoutAudit, contentSafetyAudit: auditStateContent(stateWithoutAudit) }
  const validation = validateMedievalWorldState(context, state)
  if (validation.length) throw new WorldStateContractError(validation)
  return state
}

const validSubdomain = (value: unknown, version: number, keys: readonly string[]): value is Record<string, unknown> => record(value) && value.version === version && hasOnlyKeys(value, keys)
const validWorldId = (value: unknown): value is string => typeof value === 'string' && value.length > 0
const validLocation = (value: unknown): value is WorldJomonLocation => record(value) && hasOnlyKeys(value, ['kind', 'id']) && (value.kind === 'site' || value.kind === 'quay') && validWorldId(value.id)
const validCausalRecord = (value: unknown): value is CausalRecord => record(value) && hasOnlyKeys(value, ['sequence', 'atWorldTime', 'kind', 'detail', 'contentSafety']) && safeInteger(value.sequence) && safeInteger(value.atWorldTime) && ['world-created', 'initial-courier-selected', 'temporal-action', 'scheduled-event-resolved'].includes(String(value.kind)) && typeof value.detail === 'string'

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
  if (!stateLike(value)) return [issue('world-state', 'world-state.malformed-state')]
  if (value.version !== MEDIEVAL_WORLD_STATE_VERSION) issues.push(issue('world-state', 'world-state.invalid-version'))

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
  validateIdArrayOrder(value.people?.registry, 'world-state:people', issues)
  validateIdArrayOrder(value.institutions?.registry, 'world-state:institutions', issues)
  if (!validSubdomain(value.sites, WORLD_SITES_STATE_VERSION, ['version', 'sites', 'quays']) || !Array.isArray(value.sites.sites) || !Array.isArray(value.sites.quays) || value.sites.sites.length > MEDIEVAL_WORLD_STATE_LIMITS.sites || !same(value.sites.sites, expectedSites)) issues.push(issue('world-state:sites', value.sites && Array.isArray(value.sites.sites) && value.sites.sites.length > MEDIEVAL_WORLD_STATE_LIMITS.sites ? 'world-state.budget-exceeded' : 'world-state.invalid-sites'))
  const expectedQuays = quayRecordsFor(context.jomon)
  if (!validSubdomain(value.sites, WORLD_SITES_STATE_VERSION, ['version', 'sites', 'quays']) || !Array.isArray(value.sites.quays) || value.sites.quays.length > MEDIEVAL_WORLD_STATE_LIMITS.quays || !same(value.sites.quays, expectedQuays)) issues.push(issue('world-state:quays', value.sites && Array.isArray(value.sites.quays) && value.sites.quays.length > MEDIEVAL_WORLD_STATE_LIMITS.quays ? 'world-state.budget-exceeded' : 'world-state.invalid-quays'))

  const expectedRoutes = routeRecordsFor(context.initialWorld)
  if (!validSubdomain(value.routes, WORLD_ROUTES_STATE_VERSION, ['version', 'conditions']) || !Array.isArray(value.routes.conditions) || value.routes.conditions.length > MEDIEVAL_WORLD_STATE_LIMITS.routeConditions || !same(value.routes.conditions, expectedRoutes)) issues.push(issue('world-state:routes', value.routes && Array.isArray(value.routes.conditions) && value.routes.conditions.length > MEDIEVAL_WORLD_STATE_LIMITS.routeConditions ? 'world-state.budget-exceeded' : 'world-state.invalid-routes'))

  const expectedMarkets = marketRecordsFor(expectedSites)
  if (!validSubdomain(value.markets, WORLD_MARKETS_STATE_VERSION, ['version', 'markets']) || !Array.isArray(value.markets.markets) || value.markets.markets.length > MEDIEVAL_WORLD_STATE_LIMITS.markets || !same(value.markets.markets, expectedMarkets)) issues.push(issue('world-state:markets', value.markets && Array.isArray(value.markets.markets) && value.markets.markets.length > MEDIEVAL_WORLD_STATE_LIMITS.markets ? 'world-state.budget-exceeded' : 'world-state.invalid-markets'))

  const expectedPeople = peopleRecordsFor(context.initialWorld, context.jomon, context.crew)
  if (!validSubdomain(value.people, WORLD_PEOPLE_STATE_VERSION, ['version', 'registry']) || !Array.isArray(value.people.registry) || value.people.registry.length > MEDIEVAL_WORLD_STATE_LIMITS.people || !same(value.people.registry, expectedPeople)) issues.push(issue('world-state:people', value.people && Array.isArray(value.people.registry) && value.people.registry.length > MEDIEVAL_WORLD_STATE_LIMITS.people ? 'world-state.budget-exceeded' : 'world-state.invalid-people'))

  const expectedInstitutions = institutionRecordsFor(context.initialWorld)
  if (!validSubdomain(value.institutions, WORLD_INSTITUTIONS_STATE_VERSION, ['version', 'registry']) || !Array.isArray(value.institutions.registry) || value.institutions.registry.length > MEDIEVAL_WORLD_STATE_LIMITS.institutions || !same(value.institutions.registry, expectedInstitutions)) issues.push(issue('world-state:institutions', value.institutions && Array.isArray(value.institutions.registry) && value.institutions.registry.length > MEDIEVAL_WORLD_STATE_LIMITS.institutions ? 'world-state.budget-exceeded' : 'world-state.invalid-institutions'))

  const temporal = isMedievalTemporalState(value.temporal) ? value.temporal : undefined
  if (!temporal) issues.push(issue('world-state:temporal', 'world-state.invalid-temporal'))
  if (!validSubdomain(value.courier, WORLD_COURIER_STATE_VERSION, value.courier && record(value.courier) && value.courier.initialCourierId === undefined ? ['version'] : ['version', 'initialCourierId']) || (value.courier.initialCourierId !== undefined && !context.crew.some(member => member.id === value.courier.initialCourierId && member.eligible))) issues.push(issue('world-state:courier', 'world-state.invalid-courier'))

  const expectedHistory = temporal === undefined ? [] : causalHistoryWithTemporal(context.foundationHistory, temporal)
  if (!validSubdomain(value.history, WORLD_HISTORY_STATE_VERSION, ['version', 'records']) || !Array.isArray(value.history.records) || value.history.records.length > MEDIEVAL_WORLD_STATE_LIMITS.historyRecords || !value.history.records.every(validCausalRecord) || !same(value.history.records, expectedHistory)) issues.push(issue('world-state:history', value.history && Array.isArray(value.history.records) && value.history.records.length > MEDIEVAL_WORLD_STATE_LIMITS.historyRecords ? 'world-state.budget-exceeded' : 'world-state.invalid-history'))
  else if ((value.history.records as CausalRecord[]).some((entry, index) => entry.sequence !== index)) issues.push(issue('world-state:history', 'world-state.noncanonical-order'))

  const sites = Array.isArray(value.sites?.sites) ? value.sites.sites as WorldSiteState[] : []
  const quays = Array.isArray(value.sites?.quays) ? value.sites.quays as WorldQuayState[] : []
  const jomon = value.jomon
  if (!validSubdomain(jomon, WORLD_JOMON_STATE_VERSION, ['version', 'vesselId', 'operationalStatus', 'location', 'integrity', 'capacity']) || jomon.vesselId !== context.jomon.id || jomon.operationalStatus !== 'moored' || !validLocation(jomon.location) || (jomon.location.kind === 'site' ? !sites.some(site => site.id === jomon.location.id) : !quays.some(quay => quay.id === jomon.location.id)) || !record(jomon.integrity) || !hasOnlyKeys(jomon.integrity, ['current', 'maximum']) || !safeInteger(jomon.integrity.current) || !safeInteger(jomon.integrity.maximum) || jomon.integrity.maximum < 1 || jomon.integrity.maximum > MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum || jomon.integrity.current > jomon.integrity.maximum || !record(jomon.capacity) || !hasOnlyKeys(jomon.capacity, ['cargoUnits', 'berthSlots', 'workSlots']) || !safeInteger(jomon.capacity.cargoUnits) || !safeInteger(jomon.capacity.berthSlots) || !safeInteger(jomon.capacity.workSlots) || jomon.capacity.cargoUnits > MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum || jomon.capacity.berthSlots > MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum || jomon.capacity.workSlots > MEDIEVAL_WORLD_STATE_LIMITS.capacityMaximum) issues.push(issue('world-state:jomon', 'world-state.invalid-jomon'))

  try {
    if (value.contentSafetyAudit === undefined || !contentSafetyAuditMatches(medievalWorldStateContentRecords(value), value.contentSafetyAudit)) issues.push(issue('world-state:content-safety', 'world-state.invalid-content-audit'))
  } catch {
    issues.push(issue('world-state:content-safety', 'world-state.invalid-content-audit'))
  }
  return canonicalIssues(issues)
}

export const isMedievalWorldState = (context: WorldStateValidationContext, value: unknown): value is MedievalWorldState => validateMedievalWorldState(context, value).length === 0

export class WorldStateContractError extends Error {
  constructor(readonly diagnostics: readonly WorldStateValidationIssue[]) {
    super(`world state rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'WorldStateContractError'
  }
}
