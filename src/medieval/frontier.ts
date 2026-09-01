import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { generationConfigurationFingerprint, isResolvedWorldGenerationConfig, type WorldGenerationConfig } from './generation-config'
import { type InitialWorld, validateInitialWorldCandidate } from './initial-world'
import { SeededRng, hashSeed } from './rng'

/** A contract-only version; durable manifest storage follows in the next slice. */
export const FRONTIER_CONTRACT_VERSION = 1 as const

/** Fixed caps prevent an unrevealed frontier from becoming latent world state. */
export const FRONTIER_LIMITS = {
  regions: 12,
  anonymousRolesPerRegion: 3,
  anonymousInstitutionsPerRegion: 3,
  factsPerRegion: 6,
  namedPeoplePerRegion: 3,
  materializedSitesPerRegion: 1
} as const

export interface FrontierCoordinate {
  x: number
  y: number
}

export type FrontierRegionKind = 'upper-reach' | 'coastal-reach' | 'branch-marsh' | 'side-channel'
export type FrontierConnectionKind = 'initial-world-link' | 'parent-region-link'
export type FrontierKnowledgeSourceKind = 'rumour' | 'chart' | 'trader' | 'letter' | 'traveller' | 'cargo-mark' | 'institution-ledger'
export type FrontierFactSubjectKind = 'region' | 'site' | 'route' | 'person' | 'institution'
export type FrontierFactKind = 'region-name' | 'site-name' | 'route-link' | 'person-name' | 'person-role' | 'person-relationship' | 'institution-role' | 'history-link'
export type FrontierAnonymousRole = 'boat-hand' | 'weir-tender' | 'market-clerk' | 'net-maker' | 'yard-worker'
export type FrontierAnonymousInstitutionKind = 'quay-ward' | 'ferry-office' | 'market-stall' | 'weir-works' | 'yard-lease'
export type FrontierDirection = 'upstream' | 'coastward' | 'branch-north' | 'branch-south'

export interface FrontierCausalAnchor {
  initialWorldId: string
  watershedId: string
  waterwayId: string
  settlementId: string
  routeId: string
  tradeLinkId: string
  climateId: string
  ecologyId: string
  historyEventId: string
}

export interface FrontierConnection {
  id: string
  kind: FrontierConnectionKind
  parentRegionId?: string
  initialRouteId: string
  initialWaterwayId: string
  contentSafety: MedievalContentSafetyClassification
}

/** Anonymous commitments deliberately never allocate a mutable person record. */
export interface FrontierAnonymousRoleCommitment {
  id: string
  role: FrontierAnonymousRole
  contentSafety: MedievalContentSafetyClassification
}

export interface FrontierAnonymousInstitutionCommitment {
  id: string
  kind: FrontierAnonymousInstitutionKind
  contentSafety: MedievalContentSafetyClassification
}

export interface FrontierPopulationCommitment {
  adultHouseholdBand: number
  contentSafety: MedievalContentSafetyClassification
}

export interface FrontierRegionCommitment {
  id: string
  coordinate: FrontierCoordinate
  kind: FrontierRegionKind
  generationOrder: number
  generatorStream: string
  anchor: FrontierCausalAnchor
  connection: FrontierConnection
  population: FrontierPopulationCommitment
  anonymousRoles: readonly FrontierAnonymousRoleCommitment[]
  anonymousInstitutions: readonly FrontierAnonymousInstitutionCommitment[]
  contentSafety: MedievalContentSafetyClassification
}

export interface FrontierKnowledgeSource {
  kind: FrontierKnowledgeSourceKind
  label: string
  sourceRecordId: string
  reportedAtWorldTime: number
  freshnessAtWorldTime: number
  contentSafety: MedievalContentSafetyClassification
}

/**
 * Facts are immutable claims. Their subject/fact key prevents later changes
 * of fact; a relationship value is another stable named-person commitment ID.
 */
export interface FrontierRevealedFact {
  id: string
  regionId: string
  subjectKind: FrontierFactSubjectKind
  subjectId: string
  kind: FrontierFactKind
  value: string
  source: FrontierKnowledgeSource
  knownAtWorldTime: number
  contentSafety: MedievalContentSafetyClassification
}

export interface FrontierNamedPersonCommitment {
  id: string
  futurePersonId: string
  regionId: string
  revelationKey: string
  name: string
  role: FrontierAnonymousRole
  institutionCommitmentId: string
  sourceFactId: string
  instantiationTrigger: 'region-materialized'
  contentSafety: MedievalContentSafetyClassification
}

/** This is only a stable future record shape; Phase 1.3 owns mutable people. */
export interface FrontierPersonInstantiationPlan {
  id: string
  futurePersonId: string
  regionId: string
  commitmentId: string
  name: string
  role: FrontierAnonymousRole
  institutionCommitmentId: string
  trigger: 'region-materialized'
  contentSafety: MedievalContentSafetyClassification
}

export interface FrontierMaterializedSite {
  id: string
  regionId: string
  name: string
  kind: FrontierRegionKind
  initialWaterwayId: string
  initialEcologyId: string
  materialPurpose: 'ferry-landing' | 'net-yard' | 'tide-store' | 'weir-bank'
  contentSafety: MedievalContentSafetyClassification
}

/** Static regional data now; later simulation owns any mutable region state. */
export interface FrontierMaterializationPlan {
  version: 1
  generatorStream: string
  materializedAtWorldTime: number
  site: FrontierMaterializedSite
  namedPersonPlans: readonly FrontierPersonInstantiationPlan[]
  contentSafety: MedievalContentSafetyClassification
}

export interface UngeneratedFrontierRegion {
  status: 'ungenerated'
  commitment: FrontierRegionCommitment
}

export interface KnownFrontierRegion {
  status: 'known-but-unvisited'
  commitment: FrontierRegionCommitment
  revealedFacts: readonly FrontierRevealedFact[]
  namedPeople: readonly FrontierNamedPersonCommitment[]
}

export interface MaterializedFrontierRegion {
  status: 'materialized'
  commitment: FrontierRegionCommitment
  revealedFacts: readonly FrontierRevealedFact[]
  namedPeople: readonly FrontierNamedPersonCommitment[]
  materialization: FrontierMaterializationPlan
}

export type FrontierRegion = UngeneratedFrontierRegion | KnownFrontierRegion | MaterializedFrontierRegion

export interface FrontierProvenance {
  seed: string
  configurationFingerprint: string
  initialWorldId: string
}

/**
 * The state is a pure value. It is intentionally not added to the save
 * envelope in this task; the following manifest task owns that boundary.
 */
export interface FrontierState {
  version: typeof FRONTIER_CONTRACT_VERSION
  provenance: FrontierProvenance
  regions: readonly FrontierRegion[]
}

export interface FrontierGenerationContext {
  seed: string
  configuration: WorldGenerationConfig
  initialWorld: InitialWorld
}

export interface FrontierKnowledgeSourceInput {
  kind: FrontierKnowledgeSourceKind
  sourceRecordId: string
  reportedAtWorldTime: number
  freshnessAtWorldTime: number
}

export type FrontierValidationDiagnosticCode =
  | 'frontier.invalid-context'
  | 'frontier.invalid-root'
  | 'frontier.duplicate-id'
  | 'frontier.duplicate-coordinate'
  | 'frontier.invalid-generation-order'
  | 'frontier.invalid-anchor'
  | 'frontier.invalid-connection'
  | 'frontier.invalid-state'
  | 'frontier.invalid-fact'
  | 'frontier.invalid-fact-timing'
  | 'frontier.contradictory-fact'
  | 'frontier.invalid-named-person'
  | 'frontier.invalid-materialization'
  | 'frontier.budget-exceeded'
  | MedievalContentSafetyDiagnosticCode

export interface FrontierValidationIssue {
  code: FrontierValidationDiagnosticCode
  recordId: string
}

const coordinateDirections: Readonly<Record<FrontierDirection, FrontierCoordinate>> = {
  upstream: { x: -1, y: 0 },
  coastward: { x: 1, y: 0 },
  'branch-north': { x: 0, y: 1 },
  'branch-south': { x: 0, y: -1 }
}
const sourceLabels: Readonly<Record<FrontierKnowledgeSourceKind, string>> = {
  rumour: 'quay rumour',
  chart: 'marked chart',
  trader: 'trader account',
  letter: 'sealed letter',
  traveller: 'traveller account',
  'cargo-mark': 'cargo mark',
  'institution-ledger': 'institution ledger'
}
const regionalNameStarts = ['Alder', 'Brine', 'Candle', 'Dun', 'Eel', 'Fallow', 'Gull', 'Hearth'] as const
const regionalNameEnds = ['Reach', 'Inlet', 'Marsh', 'Bank', 'Sound', 'Cut'] as const
const roles: readonly FrontierAnonymousRole[] = ['boat-hand', 'weir-tender', 'market-clerk', 'net-maker', 'yard-worker']
const institutions: readonly FrontierAnonymousInstitutionKind[] = ['quay-ward', 'ferry-office', 'market-stall', 'weir-works', 'yard-lease']
const materialPurposes: readonly FrontierMaterializedSite['materialPurpose'][] = ['ferry-landing', 'net-yard', 'tide-store', 'weir-bank']

const normalizeSeed = (seed: string): string => seed.trim().replace(/\s+/g, ' ')
const coordinateKey = (coordinate: FrontierCoordinate): string => `${coordinate.x},${coordinate.y}`
const comparison = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const nonNegativeInteger = (value: number): boolean => Number.isSafeInteger(value) && value >= 0
const issue = (recordId: string, code: FrontierValidationDiagnosticCode): FrontierValidationIssue => ({ recordId, code })

export const frontierRegionIdFor = (initialWorld: InitialWorld, coordinate: FrontierCoordinate): string => `frontier:region:${initialWorld.id}:${coordinate.x}:${coordinate.y}`

/** Knowledge-source labels are generated from this closed vocabulary, never free text. */
export const createFrontierKnowledgeSource = (input: FrontierKnowledgeSourceInput): FrontierKnowledgeSource => ({
  ...input,
  label: sourceLabels[input.kind],
  contentSafety: classifyMedievalContent('player-facing-text', ['civil-life', 'navigation', 'travel'], 'not-applicable', ['rumour'])
})

const streamSeedFor = (context: FrontierGenerationContext, coordinate: FrontierCoordinate, operation: string): string => {
  const seed = normalizeSeed(context.seed)
  return `jomon-frontier-v${FRONTIER_CONTRACT_VERSION}|${seed}|${generationConfigurationFingerprint(context.configuration)}|${context.initialWorld.id}|${coordinateKey(coordinate)}|${operation}`
}

const kindForCoordinate = (coordinate: FrontierCoordinate): FrontierRegionKind => {
  if (coordinate.x < 0) return 'upper-reach'
  if (coordinate.x > 0 && coordinate.y === 0) return 'coastal-reach'
  if (coordinate.y !== 0) return 'branch-marsh'
  return 'side-channel'
}

const regionNameFor = (context: FrontierGenerationContext, coordinate: FrontierCoordinate, operation: string): string => {
  const rng = new SeededRng(streamSeedFor(context, coordinate, operation))
  return `${rng.pick(regionalNameStarts)} ${rng.pick(regionalNameEnds)}`
}

const rootAnchorFor = (initialWorld: InitialWorld, index: number): FrontierCausalAnchor => {
  const route = initialWorld.routes[index % initialWorld.routes.length]!
  const trade = initialWorld.tradeLinks.find(link => link.routeId === route.id)!
  const settlement = initialWorld.settlements.find(candidate => candidate.id === route.originSettlementId)!
  const history = initialWorld.history.find(event => event.routeId === route.id)!
  return {
    initialWorldId: initialWorld.id,
    watershedId: initialWorld.watershed.id,
    waterwayId: route.waterwayIds[0]!,
    settlementId: settlement.id,
    routeId: route.id,
    tradeLinkId: trade.id,
    climateId: initialWorld.climate.id,
    ecologyId: settlement.ecologyId,
    historyEventId: history.id
  }
}

const commitmentFor = (
  context: FrontierGenerationContext,
  coordinate: FrontierCoordinate,
  generationOrder: number,
  anchor: FrontierCausalAnchor,
  connection: FrontierConnection
): FrontierRegionCommitment => {
  const stream = streamSeedFor(context, coordinate, `commitment:${generationOrder}`)
  const rng = new SeededRng(stream)
  const regionId = frontierRegionIdFor(context.initialWorld, coordinate)
  const roleCount = 1 + (context.configuration.populationDensity % FRONTIER_LIMITS.anonymousRolesPerRegion)
  const institutionCount = 1 + (context.configuration.politicalFragmentation % FRONTIER_LIMITS.anonymousInstitutionsPerRegion)
  return {
    id: regionId,
    coordinate: { ...coordinate },
    kind: kindForCoordinate(coordinate),
    generationOrder,
    generatorStream: stream,
    anchor,
    connection,
    population: {
      adultHouseholdBand: context.configuration.populationDensity,
      contentSafety: classifyMedievalContent('template', ['adult-labour', 'settlement'], 'adults-only', ['data'])
    },
    anonymousRoles: Array.from({ length: roleCount }, (_, index) => ({
      id: `${regionId}:role:${index}`,
      role: roles[(index + rng.integer(roles.length)) % roles.length]!,
      contentSafety: classifyMedievalContent('template', ['adult-labour', 'settlement'], 'adults-only', ['data'])
    })),
    anonymousInstitutions: Array.from({ length: institutionCount }, (_, index) => ({
      id: `${regionId}:institution:${index}`,
      kind: institutions[(index + rng.integer(institutions.length)) % institutions.length]!,
      contentSafety: classifyMedievalContent('template', ['civil-life', 'commerce', 'settlement'], 'adults-only', ['data'])
    })),
    contentSafety: classifyMedievalContent('data', ['environment', 'navigation', 'settlement'], 'not-applicable', ['player-facing-text'])
  }
}

const initialConnection = (regionId: string, anchor: FrontierCausalAnchor): FrontierConnection => ({
  id: `${regionId}:connection`,
  kind: 'initial-world-link',
  initialRouteId: anchor.routeId,
  initialWaterwayId: anchor.waterwayId,
  contentSafety: classifyMedievalContent('data', ['navigation', 'travel'], 'not-applicable', ['player-facing-text'])
})

const parentConnection = (regionId: string, parentRegionId: string, anchor: FrontierCausalAnchor): FrontierConnection => ({
  id: `${regionId}:connection`,
  kind: 'parent-region-link',
  parentRegionId,
  initialRouteId: anchor.routeId,
  initialWaterwayId: anchor.waterwayId,
  contentSafety: classifyMedievalContent('data', ['navigation', 'travel'], 'not-applicable', ['player-facing-text'])
})

const regionNameFact = (context: FrontierGenerationContext, commitment: FrontierRegionCommitment): FrontierRevealedFact => ({
  id: `${commitment.id}:fact:region-name`,
  regionId: commitment.id,
  subjectKind: 'region',
  subjectId: commitment.id,
  kind: 'region-name',
  value: regionNameFor(context, commitment.coordinate, 'initial-chart-name'),
  source: createFrontierKnowledgeSource({
    kind: 'chart',
    sourceRecordId: commitment.anchor.historyEventId,
    reportedAtWorldTime: 0,
    freshnessAtWorldTime: 0
  }),
  knownAtWorldTime: 0,
  contentSafety: classifyMedievalContent('rumour', ['navigation', 'travel'], 'not-applicable', ['player-facing-text'])
})

const validContext = (context: FrontierGenerationContext): boolean => {
  const seed = normalizeSeed(context.seed)
  return Boolean(seed) && seed === context.seed && isResolvedWorldGenerationConfig(context.configuration) && validateInitialWorldCandidate(context.initialWorld, context.configuration).length === 0
}

const idsInInitialWorld = (world: InitialWorld): ReadonlySet<string> => new Set([
  world.watershed.id,
  world.climate.id,
  ...world.waterways.map(record => record.id),
  ...world.settlements.map(record => record.id),
  ...world.routes.map(record => record.id),
  ...world.tradeLinks.map(record => record.id),
  ...world.ecologies.map(record => record.id),
  ...world.history.map(record => record.id)
])

const regionFacts = (region: FrontierRegion): readonly FrontierRevealedFact[] => region.status === 'ungenerated' ? [] : region.revealedFacts
const regionPeople = (region: FrontierRegion): readonly FrontierNamedPersonCommitment[] => region.status === 'ungenerated' ? [] : region.namedPeople

/** Every player-visible and future-template frontier record is policy-audited. */
export const frontierContentRecords = (state: FrontierState): readonly ClassifiedMedievalContent[] => state.regions.flatMap(region => {
  const commitment = region.commitment
  const materialized = region.status === 'materialized' ? region.materialization : undefined
  return [
    { id: commitment.id, domain: 'data' as const, classification: commitment.contentSafety },
    { id: commitment.connection.id, domain: 'data' as const, classification: commitment.connection.contentSafety },
    { id: `${commitment.id}:population`, domain: 'template' as const, classification: commitment.population.contentSafety },
    ...commitment.anonymousRoles.map(record => ({ id: record.id, domain: 'template' as const, classification: record.contentSafety })),
    ...commitment.anonymousInstitutions.map(record => ({ id: record.id, domain: 'template' as const, classification: record.contentSafety })),
    ...regionFacts(region).flatMap(record => [
      { id: record.id, domain: record.kind === 'history-link' ? 'history' as const : 'rumour' as const, classification: record.contentSafety },
      { id: `${record.id}:source`, domain: 'player-facing-text' as const, classification: record.source.contentSafety }
    ]),
    ...regionPeople(region).map(record => ({ id: record.id, domain: 'person' as const, classification: record.contentSafety })),
    ...(materialized === undefined ? [] : [
      { id: `${commitment.id}:materialization`, domain: 'data' as const, classification: materialized.contentSafety },
      { id: materialized.site.id, domain: 'place' as const, classification: materialized.site.contentSafety },
      ...materialized.namedPersonPlans.map(record => ({ id: record.id, domain: 'person' as const, classification: record.contentSafety }))
    ])
  ]
})

const has = <Value extends { id: string }>(records: readonly Value[], id: string): boolean => records.some(record => record.id === id)
const byId = <Value extends { id: string }>(records: readonly Value[], id: string): Value | undefined => records.find(record => record.id === id)

const allRecordIds = (state: FrontierState): readonly string[] => state.regions.flatMap(region => {
  const materialized = region.status === 'materialized' ? region.materialization : undefined
  return [
    region.commitment.id,
    region.commitment.connection.id,
    `${region.commitment.id}:population`,
    ...region.commitment.anonymousRoles.map(record => record.id),
    ...region.commitment.anonymousInstitutions.map(record => record.id),
    ...regionFacts(region).flatMap(record => [record.id, `${record.id}:source`]),
    ...regionPeople(region).map(record => record.id),
    ...(materialized === undefined ? [] : [
      `${region.commitment.id}:materialization`,
      materialized.site.id,
      ...materialized.namedPersonPlans.map(record => record.id)
    ])
  ]
})

const hasValidAnchor = (anchor: FrontierCausalAnchor, initialWorld: InitialWorld): boolean => {
  const ids = idsInInitialWorld(initialWorld)
  return anchor.initialWorldId === initialWorld.id && ids.has(anchor.watershedId) && ids.has(anchor.waterwayId) && ids.has(anchor.settlementId) && ids.has(anchor.routeId) && ids.has(anchor.tradeLinkId) && ids.has(anchor.climateId) && ids.has(anchor.ecologyId) && ids.has(anchor.historyEventId)
}

const factKey = (fact: FrontierRevealedFact): string => `${fact.regionId}|${fact.subjectKind}|${fact.subjectId}|${fact.kind}`
const siteIdFor = (regionId: string): string => `${regionId}:site:0`
const namedPersonIdFor = (state: FrontierState, regionId: string, revelationKey: string): string => `frontier:named-person:${hashSeed(`${state.provenance.seed}|${regionId}|${revelationKey}`).toString(36)}`
const futurePersonIdFor = (state: FrontierState, regionId: string, revelationKey: string): string => `person:frontier:${hashSeed(`${state.provenance.seed}|${regionId}|${revelationKey}|persistent`).toString(36)}`
const factSignature = (fact: FrontierRevealedFact): string => JSON.stringify([
  fact.id,
  fact.regionId,
  fact.subjectKind,
  fact.subjectId,
  fact.kind,
  fact.value,
  fact.source.kind,
  fact.source.label,
  fact.source.sourceRecordId,
  fact.source.reportedAtWorldTime,
  fact.source.freshnessAtWorldTime,
  fact.source.contentSafety,
  fact.knownAtWorldTime,
  fact.contentSafety
])

/** Canonical, side-effect-free validation for facts, connections, and future plans. */
export const validateFrontierState = (context: FrontierGenerationContext, state: FrontierState): readonly FrontierValidationIssue[] => {
  const issues: FrontierValidationIssue[] = []
  if (!validContext(context)) return [issue('frontier:context', 'frontier.invalid-context')]
  if (state.version !== FRONTIER_CONTRACT_VERSION || state.provenance.seed !== context.seed || state.provenance.configurationFingerprint !== generationConfigurationFingerprint(context.configuration) || state.provenance.initialWorldId !== context.initialWorld.id || !Array.isArray(state.regions)) return [issue('frontier:state', 'frontier.invalid-root')]
  if (state.regions.length > FRONTIER_LIMITS.regions) issues.push(issue('frontier:state', 'frontier.budget-exceeded'))

  const seenIds = new Set<string>()
  const seenCoordinates = new Set<string>()
  const seenGenerationOrders = new Set<number>()
  const factsByKey = new Map<string, string>()
  const initialIds = idsInInitialWorld(context.initialWorld)
  const allKnownFactIds = new Set(state.regions.flatMap(region => regionFacts(region).map(fact => fact.id)))
  for (const region of [...state.regions].sort((left, right) => left.commitment.generationOrder - right.commitment.generationOrder || comparison(left.commitment.id, right.commitment.id))) {
    const commitment = region.commitment
    const coordinate = commitment.coordinate
    if (!Number.isSafeInteger(coordinate.x) || !Number.isSafeInteger(coordinate.y)) issues.push(issue(commitment.id, 'frontier.invalid-root'))
    const coordinateId = coordinateKey(coordinate)
    if (seenCoordinates.has(coordinateId)) issues.push(issue(commitment.id, 'frontier.duplicate-coordinate'))
    seenCoordinates.add(coordinateId)
    if (commitment.id !== frontierRegionIdFor(context.initialWorld, coordinate) || commitment.kind !== kindForCoordinate(coordinate) || commitment.generationOrder < 0 || !Number.isSafeInteger(commitment.generationOrder) || seenGenerationOrders.has(commitment.generationOrder)) issues.push(issue(commitment.id, 'frontier.invalid-generation-order'))
    seenGenerationOrders.add(commitment.generationOrder)
    if (!hasValidAnchor(commitment.anchor, context.initialWorld)) issues.push(issue(commitment.id, 'frontier.invalid-anchor'))
    const expectedStream = streamSeedFor(context, coordinate, `commitment:${commitment.generationOrder}`)
    if (commitment.generatorStream !== expectedStream) issues.push(issue(commitment.id, 'frontier.invalid-generation-order'))
    if (commitment.anonymousRoles.length > FRONTIER_LIMITS.anonymousRolesPerRegion || commitment.anonymousInstitutions.length > FRONTIER_LIMITS.anonymousInstitutionsPerRegion || regionFacts(region).length > FRONTIER_LIMITS.factsPerRegion || regionPeople(region).length > FRONTIER_LIMITS.namedPeoplePerRegion) issues.push(issue(commitment.id, 'frontier.budget-exceeded'))
    if (commitment.population.adultHouseholdBand !== context.configuration.populationDensity || commitment.anonymousRoles.length !== 1 + (context.configuration.populationDensity % FRONTIER_LIMITS.anonymousRolesPerRegion) || commitment.anonymousInstitutions.length !== 1 + (context.configuration.politicalFragmentation % FRONTIER_LIMITS.anonymousInstitutionsPerRegion)) issues.push(issue(commitment.id, 'frontier.invalid-state'))

    const connection = commitment.connection
    const parent = connection.parentRegionId === undefined ? undefined : byId(state.regions.map(candidate => ({ id: candidate.commitment.id, candidate })), connection.parentRegionId)?.candidate
    if (connection.id !== `${commitment.id}:connection` || connection.initialRouteId !== commitment.anchor.routeId || connection.initialWaterwayId !== commitment.anchor.waterwayId || (connection.kind === 'initial-world-link' && connection.parentRegionId !== undefined) || (connection.kind === 'parent-region-link' && (!parent || parent.commitment.generationOrder >= commitment.generationOrder))) issues.push(issue(connection.id, 'frontier.invalid-connection'))
    if ((region.status === 'ungenerated' && (regionFacts(region).length !== 0 || regionPeople(region).length !== 0)) || (region.status === 'known-but-unvisited' && regionFacts(region).length === 0)) issues.push(issue(commitment.id, 'frontier.invalid-state'))

    for (const fact of [...regionFacts(region)].sort((left, right) => comparison(left.id, right.id))) {
      const sourceRecordIsKnown = initialIds.has(fact.source.sourceRecordId) || allKnownFactIds.has(fact.source.sourceRecordId)
      const expectedSubject = fact.kind === 'region-name' ? commitment.id
        : fact.kind === 'site-name' ? siteIdFor(commitment.id)
          : fact.kind === 'route-link' || fact.kind === 'history-link' ? commitment.anchor.routeId
            : fact.subjectId
      const person = byId(regionPeople(region), fact.subjectId)
      const institution = byId(commitment.anonymousInstitutions, fact.subjectId)
      const subjectValid = (fact.kind === 'region-name' && fact.subjectKind === 'region') || (fact.kind === 'site-name' && fact.subjectKind === 'site') || ((fact.kind === 'route-link' || fact.kind === 'history-link') && fact.subjectKind === 'route') || ((fact.kind === 'person-name' || fact.kind === 'person-role' || fact.kind === 'person-relationship') && fact.subjectKind === 'person' && person !== undefined) || (fact.kind === 'institution-role' && fact.subjectKind === 'institution' && institution !== undefined)
      const relationshipValid = fact.kind !== 'person-relationship' || (person !== undefined && fact.value !== person.id && has(regionPeople(region), fact.value))
      if (fact.regionId !== commitment.id || !fact.id || !fact.value || fact.subjectId !== expectedSubject || !subjectValid || !relationshipValid) issues.push(issue(fact.id || commitment.id, 'frontier.invalid-fact'))
      if (!nonNegativeInteger(fact.knownAtWorldTime) || !nonNegativeInteger(fact.source.reportedAtWorldTime) || !nonNegativeInteger(fact.source.freshnessAtWorldTime) || fact.source.reportedAtWorldTime > fact.source.freshnessAtWorldTime || fact.source.freshnessAtWorldTime > fact.knownAtWorldTime || fact.source.label !== sourceLabels[fact.source.kind] || !sourceRecordIsKnown) issues.push(issue(fact.id || commitment.id, 'frontier.invalid-fact-timing'))
      const previousValue = factsByKey.get(factKey(fact))
      if (previousValue !== undefined && previousValue !== fact.value) issues.push(issue(fact.id, 'frontier.contradictory-fact'))
      factsByKey.set(factKey(fact), fact.value)
    }

    for (const person of [...regionPeople(region)].sort((left, right) => comparison(left.id, right.id))) {
      const sourceFact = byId(regionFacts(region), person.sourceFactId)
      if (person.regionId !== commitment.id || person.id !== namedPersonIdFor(state, commitment.id, person.revelationKey) || person.futurePersonId !== futurePersonIdFor(state, commitment.id, person.revelationKey) || !has(commitment.anonymousInstitutions, person.institutionCommitmentId) || sourceFact?.kind !== 'person-name' || sourceFact.subjectId !== person.id || sourceFact.value !== person.name || person.instantiationTrigger !== 'region-materialized') issues.push(issue(person.id, 'frontier.invalid-named-person'))
    }

    if (region.status === 'materialized') {
      const plan = region.materialization
      const regionName = regionFacts(region).find(fact => fact.kind === 'region-name')?.value
      const siteName = regionFacts(region).find(fact => fact.kind === 'site-name')?.value ?? `${regionName ?? regionNameFor(context, coordinate, 'materialized-name')} Landing`
      const expectedMaterializationStream = streamSeedFor(context, coordinate, 'materialization')
      const planMatchesPeople = plan.namedPersonPlans.length === region.namedPeople.length && plan.namedPersonPlans.every((candidate: FrontierPersonInstantiationPlan) => region.namedPeople.some((person: FrontierNamedPersonCommitment) => candidate.id === `${person.id}:instantiation` && person.id === candidate.commitmentId && person.futurePersonId === candidate.futurePersonId && person.name === candidate.name && person.role === candidate.role && person.institutionCommitmentId === candidate.institutionCommitmentId && candidate.trigger === 'region-materialized'))
      if (plan.version !== 1 || !nonNegativeInteger(plan.materializedAtWorldTime) || plan.materializedAtWorldTime < Math.max(0, ...region.revealedFacts.map((fact: FrontierRevealedFact) => fact.knownAtWorldTime)) || plan.generatorStream !== expectedMaterializationStream || plan.site.id !== siteIdFor(commitment.id) || plan.site.regionId !== commitment.id || plan.site.name !== siteName || plan.site.kind !== commitment.kind || plan.site.initialWaterwayId !== commitment.anchor.waterwayId || plan.site.initialEcologyId !== commitment.anchor.ecologyId || !materialPurposes.includes(plan.site.materialPurpose) || plan.namedPersonPlans.length > FRONTIER_LIMITS.namedPeoplePerRegion || !planMatchesPeople) issues.push(issue(commitment.id, 'frontier.invalid-materialization'))
    }
  }

  for (const id of [...allRecordIds(state)].sort(comparison)) {
    if (seenIds.has(id)) issues.push(issue(id, 'frontier.duplicate-id'))
    seenIds.add(id)
  }
  const generationOrders = [...seenGenerationOrders].sort((left, right) => left - right)
  if (generationOrders.some((order, index) => order !== index)) issues.push(issue('frontier:state', 'frontier.invalid-generation-order'))
  const safety = auditMedievalContentSafety(frontierContentRecords(state))
  if (safety.status === 'rejected') issues.push(...safety.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  return issues
}

export class FrontierContractError extends Error {
  constructor(readonly diagnostics: readonly FrontierValidationIssue[]) {
    super(`frontier contract rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'FrontierContractError'
  }
}

const requireValid = (context: FrontierGenerationContext, state: FrontierState): void => {
  const diagnostics = validateFrontierState(context, state)
  if (diagnostics.length) throw new FrontierContractError(diagnostics)
}

/** Creates rooted commitments plus one chart-known coastward region; no people materialize. */
export const createInitialFrontierState = (context: FrontierGenerationContext): FrontierState => {
  if (!validContext(context)) throw new FrontierContractError([issue('frontier:context', 'frontier.invalid-context')])
  const upstreamCoordinate = { x: -1, y: 0 }
  const coastCoordinate = { x: 1, y: 0 }
  const outerCoordinate = { x: 2, y: 0 }
  const upstreamId = frontierRegionIdFor(context.initialWorld, upstreamCoordinate)
  const coastId = frontierRegionIdFor(context.initialWorld, coastCoordinate)
  const upstreamAnchor = rootAnchorFor(context.initialWorld, 0)
  const coastAnchor = rootAnchorFor(context.initialWorld, 1)
  const upstreamCommitment = commitmentFor(context, upstreamCoordinate, 0, upstreamAnchor, initialConnection(upstreamId, upstreamAnchor))
  const coastCommitment = commitmentFor(context, coastCoordinate, 1, coastAnchor, initialConnection(coastId, coastAnchor))
  const outerCommitment = commitmentFor(context, outerCoordinate, 2, coastAnchor, parentConnection(frontierRegionIdFor(context.initialWorld, outerCoordinate), coastId, coastAnchor))
  const state: FrontierState = {
    version: FRONTIER_CONTRACT_VERSION,
    provenance: {
      seed: context.seed,
      configurationFingerprint: generationConfigurationFingerprint(context.configuration),
      initialWorldId: context.initialWorld.id
    },
    regions: [
      { status: 'ungenerated', commitment: upstreamCommitment },
      { status: 'known-but-unvisited', commitment: coastCommitment, revealedFacts: [regionNameFact(context, coastCommitment)], namedPeople: [] },
      { status: 'ungenerated', commitment: outerCommitment }
    ]
  }
  requireValid(context, state)
  return state
}

const replaceRegion = (state: FrontierState, replacement: FrontierRegion): FrontierState => ({
  ...state,
  regions: state.regions.map(region => region.commitment.id === replacement.commitment.id ? replacement : region).sort((left, right) => left.commitment.generationOrder - right.commitment.generationOrder)
})

/**
 * Records externally surfaced information without assigning time or changing
 * simulation. An identical fact ID is idempotent; different content is rejected.
 */
export const revealFrontierFacts = (context: FrontierGenerationContext, state: FrontierState, facts: readonly FrontierRevealedFact[]): FrontierState => {
  requireValid(context, state)
  let next = state
  for (const fact of facts) {
    const region = next.regions.find(candidate => candidate.commitment.id === fact.regionId)
    if (!region) throw new FrontierContractError([issue(fact.id, 'frontier.invalid-fact')])
    if (region.status === 'materialized') throw new FrontierContractError([issue(fact.id, 'frontier.invalid-state')])
    const existing = regionFacts(region).find(candidate => candidate.id === fact.id)
    if (existing !== undefined) {
      if (factSignature(existing) !== factSignature(fact)) throw new FrontierContractError([issue(fact.id, 'frontier.contradictory-fact')])
      continue
    }
    const known: KnownFrontierRegion | MaterializedFrontierRegion = region.status === 'ungenerated'
      ? { status: 'known-but-unvisited', commitment: region.commitment, revealedFacts: [fact], namedPeople: [] }
      : { ...region, revealedFacts: [...region.revealedFacts, fact].sort((left, right) => comparison(left.id, right.id)) }
    next = replaceRegion(next, known)
    requireValid(context, next)
  }
  return next
}

/** A named fact commits an identity but never creates a mutable person. */
export const revealNamedFrontierPerson = (
  context: FrontierGenerationContext,
  state: FrontierState,
  regionId: string,
  revelationKey: string,
  source: FrontierKnowledgeSourceInput
): FrontierState => {
  requireValid(context, state)
  const region = state.regions.find(candidate => candidate.commitment.id === regionId)
  if (!region || region.status === 'materialized' || !revelationKey) throw new FrontierContractError([issue(regionId, 'frontier.invalid-named-person')])
  const personId = namedPersonIdFor(state, regionId, revelationKey)
  const existing = regionPeople(region).find(person => person.id === personId)
  if (existing) return state
  if (regionPeople(region).length >= FRONTIER_LIMITS.namedPeoplePerRegion) throw new FrontierContractError([issue(regionId, 'frontier.budget-exceeded')])
  const rng = new SeededRng(streamSeedFor(context, region.commitment.coordinate, `named-person:${revelationKey}`))
  const role = rng.pick(roles)
  const institution = rng.pick(region.commitment.anonymousInstitutions)
  const name = `${rng.pick(regionalNameStarts)} ${rng.pick(['Vale', 'Reed', 'Keel', 'Moss', 'Wren'] as const)}`
  const sourceFact: FrontierRevealedFact = {
    id: `${personId}:fact:name`,
    regionId,
    subjectKind: 'person',
    subjectId: personId,
    kind: 'person-name',
    value: name,
    source: createFrontierKnowledgeSource(source),
    knownAtWorldTime: source.freshnessAtWorldTime,
    contentSafety: classifyMedievalContent('rumour', ['adult-labour', 'travel'], 'adults-only', ['player-facing-text'])
  }
  const person: FrontierNamedPersonCommitment = {
    id: personId,
    futurePersonId: futurePersonIdFor(state, regionId, revelationKey),
    regionId,
    revelationKey,
    name,
    role,
    institutionCommitmentId: institution.id,
    sourceFactId: sourceFact.id,
    instantiationTrigger: 'region-materialized',
    contentSafety: classifyMedievalContent('person', ['adult-labour', 'civil-life'], 'adults-only', ['player-facing-text'])
  }
  const known: KnownFrontierRegion | MaterializedFrontierRegion = region.status === 'ungenerated'
    ? { status: 'known-but-unvisited', commitment: region.commitment, revealedFacts: [sourceFact], namedPeople: [person] }
    : { ...region, revealedFacts: [...region.revealedFacts, sourceFact].sort((left, right) => comparison(left.id, right.id)), namedPeople: [...region.namedPeople, person].sort((left, right) => comparison(left.id, right.id)) }
  const next = replaceRegion(state, known)
  requireValid(context, next)
  return next
}

/** Adds one bounded anonymous child commitment from a named coordinate direction. */
export const commitAdjacentFrontierRegion = (context: FrontierGenerationContext, state: FrontierState, parentRegionId: string, direction: FrontierDirection): FrontierState => {
  requireValid(context, state)
  if (state.regions.length >= FRONTIER_LIMITS.regions) throw new FrontierContractError([issue(parentRegionId, 'frontier.budget-exceeded')])
  const parent = state.regions.find(region => region.commitment.id === parentRegionId)
  if (!parent) throw new FrontierContractError([issue(parentRegionId, 'frontier.invalid-connection')])
  const offset = coordinateDirections[direction]
  const coordinate = { x: parent.commitment.coordinate.x + offset.x, y: parent.commitment.coordinate.y + offset.y }
  if (state.regions.some(region => coordinateKey(region.commitment.coordinate) === coordinateKey(coordinate))) throw new FrontierContractError([issue(frontierRegionIdFor(context.initialWorld, coordinate), 'frontier.duplicate-coordinate')])
  const generationOrder = Math.max(...state.regions.map(region => region.commitment.generationOrder)) + 1
  const id = frontierRegionIdFor(context.initialWorld, coordinate)
  const commitment = commitmentFor(context, coordinate, generationOrder, parent.commitment.anchor, parentConnection(id, parentRegionId, parent.commitment.anchor))
  const next: FrontierState = { ...state, regions: [...state.regions, { status: 'ungenerated', commitment }] }
  requireValid(context, next)
  return next
}

/** Produces static region data and future-person construction plans without running simulation. */
export const materializeFrontierRegion = (context: FrontierGenerationContext, state: FrontierState, regionId: string, materializedAtWorldTime: number): FrontierState => {
  requireValid(context, state)
  const region = state.regions.find(candidate => candidate.commitment.id === regionId)
  if (!region || region.status === 'ungenerated' || !nonNegativeInteger(materializedAtWorldTime)) throw new FrontierContractError([issue(regionId, 'frontier.invalid-materialization')])
  if (region.status === 'materialized') return state
  const parentId = region.commitment.connection.parentRegionId
  if (parentId !== undefined && state.regions.find(candidate => candidate.commitment.id === parentId)?.status !== 'materialized') throw new FrontierContractError([issue(regionId, 'frontier.invalid-connection')])
  const rng = new SeededRng(streamSeedFor(context, region.commitment.coordinate, 'materialization'))
  const regionName = region.revealedFacts.find(fact => fact.kind === 'region-name')?.value ?? regionNameFor(context, region.commitment.coordinate, 'materialized-name')
  const siteName = region.revealedFacts.find(fact => fact.kind === 'site-name')?.value ?? `${regionName} Landing`
  const materialization: FrontierMaterializationPlan = {
    version: 1,
    generatorStream: streamSeedFor(context, region.commitment.coordinate, 'materialization'),
    materializedAtWorldTime,
    site: {
      id: siteIdFor(regionId),
      regionId,
      name: siteName,
      kind: region.commitment.kind,
      initialWaterwayId: region.commitment.anchor.waterwayId,
      initialEcologyId: region.commitment.anchor.ecologyId,
      materialPurpose: rng.pick(materialPurposes),
      contentSafety: classifyMedievalContent('place', ['commerce', 'navigation', 'settlement'], 'not-applicable', ['player-facing-text'])
    },
    namedPersonPlans: region.namedPeople.map(person => ({
      id: `${person.id}:instantiation`,
      futurePersonId: person.futurePersonId,
      regionId,
      commitmentId: person.id,
      name: person.name,
      role: person.role,
      institutionCommitmentId: person.institutionCommitmentId,
      trigger: 'region-materialized',
      contentSafety: classifyMedievalContent('person', ['adult-labour', 'civil-life'], 'adults-only', ['player-facing-text'])
    })),
    contentSafety: classifyMedievalContent('data', ['environment', 'navigation', 'settlement'], 'not-applicable', ['player-facing-text'])
  }
  const next = replaceRegion(state, { status: 'materialized', commitment: region.commitment, revealedFacts: region.revealedFacts, namedPeople: region.namedPeople, materialization })
  requireValid(context, next)
  return next
}
