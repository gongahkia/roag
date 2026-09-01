import { MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { generationConfigurationFingerprint, generationRetryPlan, type GenerationAttempt, type SimulationFidelity, type WorldGenerationConfig } from './generation-config'
import { SeededRng, hashSeed } from './rng'

/** A separately versioned, renderer-independent initial-region generator. */
export const INITIAL_WORLD_GENERATOR_VERSION = 'initial-world-1' as const
export const INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION = 1 as const

/**
 * These caps make the foundation region inspectable and keep its saved graph
 * intentionally smaller than the later expanding-frontier and simulation work.
 */
export const INITIAL_WORLD_LIMITS = {
  waterways: 7,
  seasons: 4,
  resources: 7,
  ecologies: 5,
  settlements: 6,
  institutions: 30,
  people: 60,
  routes: 5,
  tradeLinks: 5,
  routeHazards: 5,
  historyEvents: 8
} as const

export interface InitialWatershed {
  id: string
  name: string
  spanLeagues: number
  reliefLevel: number
  outletWaterwayId: string
  contentSafety: MedievalContentSafetyClassification
}

export type InitialWaterwayKind = 'tributary' | 'river' | 'estuary'

export interface InitialWaterway {
  id: string
  name: string
  kind: InitialWaterwayKind
  watershedId: string
  downstreamWaterwayId?: string
  contentSafety: MedievalContentSafetyClassification
}

export interface InitialClimate {
  id: string
  watershedId: string
  profile: WorldGenerationConfig['climate']
  seasonPace: WorldGenerationConfig['eraPace']
  contentSafety: MedievalContentSafetyClassification
}

export type InitialSeasonPhase = 'spring-rise' | 'summer-work' | 'autumn-harvest' | 'winter-draw'

export interface InitialSeason {
  id: string
  name: string
  climateId: string
  phase: InitialSeasonPhase
  navigationPressure: number
  contentSafety: MedievalContentSafetyClassification
}

export interface InitialResource {
  id: string
  name: string
  waterwayId: string
  availabilityLevel: number
  contentSafety: MedievalContentSafetyClassification
}

export interface InitialEcology {
  id: string
  name: string
  climateId: string
  resourceIds: readonly string[]
  contentSafety: MedievalContentSafetyClassification
}

export type InitialSettlementKind = 'ford-market' | 'mill-bank' | 'ferry-quay' | 'estuary-yard'

export interface InitialSettlement {
  id: string
  name: string
  kind: InitialSettlementKind
  waterwayId: string
  resourceIds: readonly string[]
  ecologyId: string
  populationBand: number
  contentSafety: MedievalContentSafetyClassification
}

export type InitialInstitutionKind = 'quay-ledger' | 'weir-works' | 'market-ward' | 'ferry-compact' | 'yard-guild'

export interface InitialInstitution {
  id: string
  name: string
  kind: InitialInstitutionKind
  settlementId: string
  contentSafety: MedievalContentSafetyClassification
}

/**
 * A generation and history participant, not the mutable persistent-person
 * record reserved for Phase 1.3.
 */
export interface InitialPersonSeed {
  id: string
  name: string
  role: 'warden' | 'factor' | 'pilot' | 'keeper' | 'carter'
  settlementId: string
  institutionId: string
  detailLevel: SimulationFidelity
  history: string
  contentSafety: MedievalContentSafetyClassification
  historyContentSafety: MedievalContentSafetyClassification
}

export interface InitialRoute {
  id: string
  name: string
  originSettlementId: string
  destinationSettlementId: string
  waterwayIds: readonly string[]
  contentSafety: MedievalContentSafetyClassification
}

export interface InitialTradeLink {
  id: string
  routeId: string
  originSettlementId: string
  destinationSettlementId: string
  resourceId: string
  contentSafety: MedievalContentSafetyClassification
}

export type InitialRouteHazardKind = 'shoal-watch' | 'weathered-weir' | 'cross-current' | 'fog-bank' | 'narrow-cut'

export interface InitialRouteHazard {
  id: string
  routeId: string
  seasonId: string
  kind: InitialRouteHazardKind
  pressureLevel: number
  contentSafety: MedievalContentSafetyClassification
}

export interface InitialHistoryEvent {
  id: string
  yearsBeforePlay: number
  description: string
  personId: string
  institutionId: string
  settlementId: string
  routeId: string
  contentSafety: MedievalContentSafetyClassification
}

/**
 * Immutable initial-region output. Its references form the stage ordering:
 * water → climate → ecology/resources → settlements → people → routes/history.
 */
export interface InitialWorld {
  version: 1
  id: string
  candidateAttempt: number
  configurationFingerprint: string
  historyHorizonYears: number
  generationDetail: SimulationFidelity
  watershed: InitialWatershed
  waterways: readonly InitialWaterway[]
  climate: InitialClimate
  seasons: readonly InitialSeason[]
  resources: readonly InitialResource[]
  ecologies: readonly InitialEcology[]
  settlements: readonly InitialSettlement[]
  institutions: readonly InitialInstitution[]
  people: readonly InitialPersonSeed[]
  routes: readonly InitialRoute[]
  tradeLinks: readonly InitialTradeLink[]
  routeHazards: readonly InitialRouteHazard[]
  history: readonly InitialHistoryEvent[]
}

export type InitialWorldValidationDiagnosticCode =
  | 'initial-world.invalid-root'
  | 'initial-world.invalid-version'
  | 'initial-world.invalid-candidate-attempt'
  | 'initial-world.configuration-mismatch'
  | 'initial-world.invalid-bounds'
  | 'initial-world.duplicate-id'
  | 'initial-world.invalid-waterway'
  | 'initial-world.invalid-climate'
  | 'initial-world.invalid-season'
  | 'initial-world.invalid-resource'
  | 'initial-world.invalid-ecology'
  | 'initial-world.invalid-settlement'
  | 'initial-world.invalid-institution'
  | 'initial-world.invalid-person-seed'
  | 'initial-world.invalid-route'
  | 'initial-world.invalid-trade-link'
  | 'initial-world.invalid-route-hazard'
  | 'initial-world.invalid-history-event'
  | MedievalContentSafetyDiagnosticCode

export interface InitialWorldValidationIssue {
  code: InitialWorldValidationDiagnosticCode
  recordId: string
}

export interface InitialWorldCandidateDiagnostic {
  attempt: number
  streamSeed: string
  status: 'accepted' | 'rejected'
  issues: readonly InitialWorldValidationIssue[]
}

export interface InitialWorldGenerationDiagnostics {
  version: typeof INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION
  generatorVersion: typeof INITIAL_WORLD_GENERATOR_VERSION
  contentSafetyPolicyVersion: typeof MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
  selectedAttempt: number
  candidates: readonly InitialWorldCandidateDiagnostic[]
}

export interface InitialWorldGeneration {
  world: InitialWorld
  diagnostics: InitialWorldGenerationDiagnostics
}

export interface InitialWorldCandidateSelection {
  status: 'selected'
  world: InitialWorld
  diagnostics: InitialWorldGenerationDiagnostics
}

export interface ExhaustedInitialWorldCandidateSelection {
  status: 'exhausted'
  diagnostics: Omit<InitialWorldGenerationDiagnostics, 'selectedAttempt'>
}

const watershedNames = ['Alder', 'Brine', 'Candle', 'Dun', 'Elder', 'Fallow', 'Gull', 'Hearth'] as const
const waterwayNames = ['Run', 'Cut', 'Reach', 'Mouth', 'Thread', 'Wash', 'Channel'] as const
const resourceNames = ['Reed fibre', 'River clay', 'Oak timber', 'Eel catch', 'Salt grass', 'Mill grain', 'Pitch pine'] as const
const ecologyNames = ['Reed flats', 'Alder banks', 'Salt meadow', 'Mill fields', 'Tidal shallows'] as const
const settlementNames = ['Alderford', 'Brine Quay', 'Candle Mill', 'Dun Ferry', 'Elder Yard', 'Fallow Market'] as const
const institutionKinds: readonly InitialInstitutionKind[] = ['quay-ledger', 'weir-works', 'market-ward', 'ferry-compact', 'yard-guild']
const institutionLabels: Readonly<Record<InitialInstitutionKind, string>> = {
  'quay-ledger': 'Quay Ledger',
  'weir-works': 'Weir Works',
  'market-ward': 'Market Ward',
  'ferry-compact': 'Ferry Compact',
  'yard-guild': 'Yard Guild'
}
const settlementKinds: readonly InitialSettlementKind[] = ['ford-market', 'mill-bank', 'ferry-quay', 'estuary-yard']
const personRoles: readonly InitialPersonSeed['role'][] = ['warden', 'factor', 'pilot', 'keeper', 'carter']
const personNames = ['Arlen', 'Bera', 'Ceren', 'Daro', 'Eris', 'Faran', 'Gala', 'Hale', 'Iria', 'Joren', 'Kest', 'Lora'] as const
const routeHazardKinds: readonly InitialRouteHazardKind[] = ['shoal-watch', 'weathered-weir', 'cross-current', 'fog-bank', 'narrow-cut']
const seasonPhases: readonly InitialSeasonPhase[] = ['spring-rise', 'summer-work', 'autumn-harvest', 'winter-draw']
const seasonNames: Readonly<Record<InitialSeasonPhase, string>> = {
  'spring-rise': 'Spring rise',
  'summer-work': 'Summer work',
  'autumn-harvest': 'Autumn harvest',
  'winter-draw': 'Winter draw'
}

const regionSpanBySize: Readonly<Record<WorldGenerationConfig['regionSize'], number>> = {
  compact: 24,
  standard: 42,
  broad: 68
}

const resourceCountFor = (configuration: WorldGenerationConfig): number => 8 - configuration.resourceScarcity
const settlementCountFor = (configuration: WorldGenerationConfig): number => 1 + configuration.settlementDensity
const historyEventCountFor = (configuration: WorldGenerationConfig): number => 2 + Math.floor(configuration.historyYears / 100)
const stageRng = (attempt: GenerationAttempt, stage: string): SeededRng => new SeededRng(`${attempt.streamSeed}|stage:${stage}`)
const unique = <Value>(values: readonly Value[]): readonly Value[] => [...new Set(values)]

const generateWatershed = (configuration: WorldGenerationConfig, rng: SeededRng): { watershed: InitialWatershed; waterways: readonly InitialWaterway[] } => {
  const watershedId = 'initial:watershed:basin'
  const riverId = 'initial:waterway:river'
  const estuaryId = 'initial:waterway:estuary'
  const basinName = `${rng.pick(watershedNames)} Basin`
  const tributaries = Array.from({ length: configuration.waterwayDensity }, (_, index): InitialWaterway => ({
    id: `initial:waterway:tributary:${index}`,
    name: `${rng.pick(watershedNames)} ${rng.pick(waterwayNames)}`,
    kind: 'tributary',
    watershedId,
    downstreamWaterwayId: riverId,
    contentSafety: classifyMedievalContent('place', ['environment', 'navigation'], 'not-applicable', ['player-facing-text'])
  }))
  const river: InitialWaterway = {
    id: riverId,
    name: `${rng.pick(watershedNames)} River`,
    kind: 'river',
    watershedId,
    downstreamWaterwayId: estuaryId,
    contentSafety: classifyMedievalContent('place', ['environment', 'navigation'], 'not-applicable', ['player-facing-text'])
  }
  const estuary: InitialWaterway = {
    id: estuaryId,
    name: `${rng.pick(watershedNames)} Estuary`,
    kind: 'estuary',
    watershedId,
    contentSafety: classifyMedievalContent('place', ['environment', 'navigation'], 'not-applicable', ['player-facing-text'])
  }
  return {
    watershed: {
      id: watershedId,
      name: basinName,
      spanLeagues: regionSpanBySize[configuration.regionSize],
      reliefLevel: configuration.terrainRuggedness,
      outletWaterwayId: estuaryId,
      contentSafety: classifyMedievalContent('place', ['environment', 'navigation'], 'not-applicable', ['player-facing-text'])
    },
    waterways: [...tributaries, river, estuary]
  }
}

const generateClimate = (configuration: WorldGenerationConfig, watershed: InitialWatershed, rng: SeededRng): { climate: InitialClimate; seasons: readonly InitialSeason[] } => {
  const climateId = 'initial:climate:basin'
  return {
    climate: {
      id: climateId,
      watershedId: watershed.id,
      profile: configuration.climate,
      seasonPace: configuration.eraPace,
      contentSafety: classifyMedievalContent('data', ['environment', 'navigation'], 'not-applicable', ['player-facing-text'])
    },
    seasons: seasonPhases.map((phase, index): InitialSeason => ({
      id: `initial:season:${phase}`,
      name: seasonNames[phase],
      climateId,
      phase,
      navigationPressure: 1 + ((configuration.terrainRuggedness + index + rng.integer(2)) % 5),
      contentSafety: classifyMedievalContent('data', ['environment', 'navigation'], 'not-applicable', ['player-facing-text'])
    }))
  }
}

const generateResourcesAndEcology = (
  configuration: WorldGenerationConfig,
  waterways: readonly InitialWaterway[],
  climate: InitialClimate,
  rng: SeededRng
): { resources: readonly InitialResource[]; ecologies: readonly InitialEcology[] } => {
  const usableWaterways = waterways.filter(waterway => waterway.kind !== 'estuary')
  const resources = Array.from({ length: resourceCountFor(configuration) }, (_, index): InitialResource => ({
    id: `initial:resource:${index}`,
    name: `${resourceNames[index % resourceNames.length]!} ${rng.pick(['store', 'ground', 'bed', 'lot'] as const)}`,
    waterwayId: usableWaterways[index % usableWaterways.length]!.id,
    availabilityLevel: 6 - configuration.resourceScarcity,
    contentSafety: classifyMedievalContent('data', ['commerce', 'environment'], 'not-applicable', ['player-facing-text'])
  }))
  const ecologies = Array.from({ length: configuration.ecologyComplexity }, (_, index): InitialEcology => ({
    id: `initial:ecology:${index}`,
    name: `${ecologyNames[index % ecologyNames.length]!} ${rng.pick(['district', 'margin', 'reach'] as const)}`,
    climateId: climate.id,
    resourceIds: unique([resources[index % resources.length]!.id, resources[(index + 1) % resources.length]!.id]),
    contentSafety: classifyMedievalContent('data', ['environment', 'settlement'], 'not-applicable', ['player-facing-text'])
  }))
  return { resources, ecologies }
}

const generateSettlements = (
  configuration: WorldGenerationConfig,
  waterways: readonly InitialWaterway[],
  resources: readonly InitialResource[],
  ecologies: readonly InitialEcology[],
  rng: SeededRng
): readonly InitialSettlement[] => Array.from({ length: settlementCountFor(configuration) }, (_, index): InitialSettlement => {
  const resource = resources[index % resources.length]!
  const waterway = waterways.find(candidate => candidate.id === resource.waterwayId)!
  const relatedResources = resources.filter(candidate => candidate.waterwayId === waterway.id).map(candidate => candidate.id)
  return {
    id: `initial:settlement:${index}`,
    name: `${settlementNames[index % settlementNames.length]!} ${rng.pick(['Landing', 'Ward', 'Reach'] as const)}`,
    kind: settlementKinds[index % settlementKinds.length]!,
    waterwayId: waterway.id,
    resourceIds: relatedResources,
    ecologyId: ecologies[index % ecologies.length]!.id,
    populationBand: configuration.populationDensity,
    contentSafety: classifyMedievalContent('place', ['commerce', 'settlement'], 'not-applicable', ['player-facing-text'])
  }
})

const generateInstitutionsAndPeople = (
  configuration: WorldGenerationConfig,
  settlements: readonly InitialSettlement[],
  rng: SeededRng
): { institutions: readonly InitialInstitution[]; people: readonly InitialPersonSeed[] } => {
  const institutions: InitialInstitution[] = []
  const people: InitialPersonSeed[] = []
  for (const settlement of settlements) {
    const localInstitutions = Array.from({ length: configuration.politicalFragmentation }, (_, index): InitialInstitution => {
      const kind = institutionKinds[(index + rng.integer(institutionKinds.length)) % institutionKinds.length]!
      return {
        id: `initial:institution:${institutions.length + index}`,
        name: `${settlement.name} ${institutionLabels[kind]}`,
        kind,
        settlementId: settlement.id,
        contentSafety: classifyMedievalContent('data', ['civil-life', 'commerce', 'settlement'], 'adults-only', ['player-facing-text'])
      }
    })
    institutions.push(...localInstitutions)
    const localPeopleCount = localInstitutions.length + configuration.populationDensity
    for (let index = 0; index < localPeopleCount; index++) {
      const institution = localInstitutions[index % localInstitutions.length]!
      const role = personRoles[(index + rng.integer(personRoles.length)) % personRoles.length]!
      const name = `${personNames[(people.length + rng.integer(personNames.length)) % personNames.length]!} ${settlement.name}`
      people.push({
        id: `initial:person:${people.length}`,
        name,
        role,
        settlementId: settlement.id,
        institutionId: institution.id,
        detailLevel: configuration.simulationFidelity,
        history: `${name} keeps adult work records for ${institution.name}.`,
        contentSafety: classifyMedievalContent('person', ['adult-labour', 'civil-life'], 'adults-only', ['player-facing-text']),
        historyContentSafety: classifyMedievalContent('history', ['adult-labour', 'civil-life'], 'adults-only', ['player-facing-text'])
      })
    }
  }
  return { institutions, people }
}

const generateRoutesTradeAndHistory = (
  configuration: WorldGenerationConfig,
  waterways: readonly InitialWaterway[],
  seasons: readonly InitialSeason[],
  resources: readonly InitialResource[],
  settlements: readonly InitialSettlement[],
  institutions: readonly InitialInstitution[],
  people: readonly InitialPersonSeed[],
  rng: SeededRng
): { routes: readonly InitialRoute[]; tradeLinks: readonly InitialTradeLink[]; routeHazards: readonly InitialRouteHazard[]; history: readonly InitialHistoryEvent[] } => {
  const routes = settlements.slice(1).map((destination, index): InitialRoute => {
    const origin = settlements[index]!
    return {
      id: `initial:route:${index}`,
      name: `${origin.name} to ${destination.name}`,
      originSettlementId: origin.id,
      destinationSettlementId: destination.id,
      waterwayIds: unique([origin.waterwayId, destination.waterwayId]),
      contentSafety: classifyMedievalContent('place', ['commerce', 'navigation', 'travel'], 'not-applicable', ['player-facing-text'])
    }
  })
  const tradeLinks = routes.map((route, index): InitialTradeLink => {
    const origin = settlements.find(settlement => settlement.id === route.originSettlementId)!
    return {
      id: `initial:trade:${index}`,
      routeId: route.id,
      originSettlementId: route.originSettlementId,
      destinationSettlementId: route.destinationSettlementId,
      resourceId: origin.resourceIds[index % origin.resourceIds.length]!,
      contentSafety: classifyMedievalContent('data', ['commerce', 'navigation'], 'not-applicable', ['player-facing-text'])
    }
  })
  const routeHazards = Array.from({ length: configuration.dangerPressure }, (_, index): InitialRouteHazard => ({
    id: `initial:route-hazard:${index}`,
    routeId: routes[index % routes.length]!.id,
    seasonId: seasons[index % seasons.length]!.id,
    kind: routeHazardKinds[(index + rng.integer(routeHazardKinds.length)) % routeHazardKinds.length]!,
    pressureLevel: configuration.dangerPressure,
    contentSafety: classifyMedievalContent('hazard', ['environment', 'navigation', 'ordinary-hardship'], 'not-applicable', ['player-facing-text'])
  }))
  const history = Array.from({ length: historyEventCountFor(configuration) }, (_, index): InitialHistoryEvent => {
    const route = routes[index % routes.length]!
    const settlement = settlements.find(candidate => candidate.id === route.originSettlementId)!
    const person = people.find(candidate => candidate.settlementId === settlement.id)!
    const institution = institutions.find(candidate => candidate.id === person.institutionId)!
    return {
      id: `initial:history:${index}`,
      yearsBeforePlay: rng.between(1, configuration.historyYears),
      description: `${person.name} recorded a working agreement between ${settlement.name} and ${route.name} through ${institution.name}.`,
      personId: person.id,
      institutionId: institution.id,
      settlementId: settlement.id,
      routeId: route.id,
      contentSafety: classifyMedievalContent('history', ['adult-labour', 'commerce', 'travel'], 'adults-only', ['player-facing-text'])
    }
  })
  // The explicit parameter makes the generated route graph depend on the
  // hydrology stage even when two neighbouring settlements share a waterway.
  if (!waterways.length || !resources.length) throw new Error('initial-world routes require waterways and resources')
  return { routes, tradeLinks, routeHazards, history }
}

const buildInitialWorldCandidate = (configuration: WorldGenerationConfig, attempt: GenerationAttempt): InitialWorld => {
  const hydrology = generateWatershed(configuration, stageRng(attempt, 'watershed-hydrology'))
  const climate = generateClimate(configuration, hydrology.watershed, stageRng(attempt, 'climate-seasons'))
  const ecology = generateResourcesAndEcology(configuration, hydrology.waterways, climate.climate, stageRng(attempt, 'resources-ecology'))
  const settlements = generateSettlements(configuration, hydrology.waterways, ecology.resources, ecology.ecologies, stageRng(attempt, 'settlement-sites'))
  const society = generateInstitutionsAndPeople(configuration, settlements, stageRng(attempt, 'institutions-people'))
  const routes = generateRoutesTradeAndHistory(configuration, hydrology.waterways, climate.seasons, ecology.resources, settlements, society.institutions, society.people, stageRng(attempt, 'routes-trade-history'))
  return {
    version: 1,
    id: `initial:world:${hashSeed(attempt.streamSeed).toString(36)}`,
    candidateAttempt: attempt.attempt,
    configurationFingerprint: generationConfigurationFingerprint(configuration),
    historyHorizonYears: configuration.historyYears,
    generationDetail: configuration.simulationFidelity,
    watershed: hydrology.watershed,
    waterways: hydrology.waterways,
    climate: climate.climate,
    seasons: climate.seasons,
    resources: ecology.resources,
    ecologies: ecology.ecologies,
    settlements,
    institutions: society.institutions,
    people: society.people,
    routes: routes.routes,
    tradeLinks: routes.tradeLinks,
    routeHazards: routes.routeHazards,
    history: routes.history
  }
}

const issue = (recordId: string, code: InitialWorldValidationDiagnosticCode): InitialWorldValidationIssue => ({ recordId, code })
const has = <Value extends { id: string }>(records: readonly Value[], id: string): boolean => records.some(record => record.id === id)
const byId = <Value extends { id: string }>(records: readonly Value[], id: string): Value | undefined => records.find(record => record.id === id)

const recordIdsAreUnique = (records: readonly { id: string }[], issues: InitialWorldValidationIssue[]): void => {
  const seen = new Set<string>()
  for (const record of records) {
    if (seen.has(record.id)) issues.push(issue(record.id, 'initial-world.duplicate-id'))
    seen.add(record.id)
  }
}

const isWithinBounds = (world: InitialWorld): boolean =>
  world.waterways.length <= INITIAL_WORLD_LIMITS.waterways &&
  world.seasons.length <= INITIAL_WORLD_LIMITS.seasons &&
  world.resources.length <= INITIAL_WORLD_LIMITS.resources &&
  world.ecologies.length <= INITIAL_WORLD_LIMITS.ecologies &&
  world.settlements.length <= INITIAL_WORLD_LIMITS.settlements &&
  world.institutions.length <= INITIAL_WORLD_LIMITS.institutions &&
  world.people.length <= INITIAL_WORLD_LIMITS.people &&
  world.routes.length <= INITIAL_WORLD_LIMITS.routes &&
  world.tradeLinks.length <= INITIAL_WORLD_LIMITS.tradeLinks &&
  world.routeHazards.length <= INITIAL_WORLD_LIMITS.routeHazards &&
  world.history.length <= INITIAL_WORLD_LIMITS.historyEvents

/** Every named/player-facing record is collected before content enters a world. */
export const initialWorldContentRecords = (world: InitialWorld): readonly ClassifiedMedievalContent[] => [
  { id: world.watershed.id, domain: 'place', classification: world.watershed.contentSafety },
  ...world.waterways.map(record => ({ id: record.id, domain: 'place' as const, classification: record.contentSafety })),
  { id: world.climate.id, domain: 'data', classification: world.climate.contentSafety },
  ...world.seasons.map(record => ({ id: record.id, domain: 'data' as const, classification: record.contentSafety })),
  ...world.resources.map(record => ({ id: record.id, domain: 'data' as const, classification: record.contentSafety })),
  ...world.ecologies.map(record => ({ id: record.id, domain: 'data' as const, classification: record.contentSafety })),
  ...world.settlements.map(record => ({ id: record.id, domain: 'place' as const, classification: record.contentSafety })),
  ...world.institutions.map(record => ({ id: record.id, domain: 'data' as const, classification: record.contentSafety })),
  ...world.people.flatMap(record => [
    { id: record.id, domain: 'person' as const, classification: record.contentSafety },
    { id: `${record.id}:history`, domain: 'history' as const, classification: record.historyContentSafety }
  ]),
  ...world.routes.map(record => ({ id: record.id, domain: 'place' as const, classification: record.contentSafety })),
  ...world.tradeLinks.map(record => ({ id: record.id, domain: 'data' as const, classification: record.contentSafety })),
  ...world.routeHazards.map(record => ({ id: record.id, domain: 'hazard' as const, classification: record.contentSafety })),
  ...world.history.map(record => ({ id: record.id, domain: 'history' as const, classification: record.contentSafety }))
]

/**
 * Candidate validation is deterministic and side-effect free. It validates
 * causality first, then delegates every player-visible record to the shared
 * fail-closed content policy.
 */
export const validateInitialWorldCandidate = (world: InitialWorld, configuration: WorldGenerationConfig): readonly InitialWorldValidationIssue[] => {
  const issues: InitialWorldValidationIssue[] = []
  if (world.version !== 1) issues.push(issue('initial:world', 'initial-world.invalid-version'))
  if (!Number.isSafeInteger(world.candidateAttempt) || world.candidateAttempt < 0) issues.push(issue(world.id, 'initial-world.invalid-candidate-attempt'))
  if (world.configurationFingerprint !== generationConfigurationFingerprint(configuration) || world.historyHorizonYears !== configuration.historyYears || world.generationDetail !== configuration.simulationFidelity || world.watershed.spanLeagues !== regionSpanBySize[configuration.regionSize] || world.watershed.reliefLevel !== configuration.terrainRuggedness || world.climate.profile !== configuration.climate || world.climate.seasonPace !== configuration.eraPace || world.waterways.length !== configuration.waterwayDensity + 2 || world.resources.length !== resourceCountFor(configuration) || world.ecologies.length !== configuration.ecologyComplexity || world.settlements.length !== settlementCountFor(configuration) || world.institutions.length !== settlementCountFor(configuration) * configuration.politicalFragmentation || world.people.length !== settlementCountFor(configuration) * (configuration.politicalFragmentation + configuration.populationDensity) || world.routes.length !== settlementCountFor(configuration) - 1 || world.tradeLinks.length !== settlementCountFor(configuration) - 1 || world.routeHazards.length !== configuration.dangerPressure || world.history.length !== historyEventCountFor(configuration)) {
    issues.push(issue(world.id, 'initial-world.configuration-mismatch'))
  }
  if (!isWithinBounds(world)) issues.push(issue(world.id, 'initial-world.invalid-bounds'))

  recordIdsAreUnique([
    world.watershed,
    ...world.waterways,
    world.climate,
    ...world.seasons,
    ...world.resources,
    ...world.ecologies,
    ...world.settlements,
    ...world.institutions,
    ...world.people,
    ...world.routes,
    ...world.tradeLinks,
    ...world.routeHazards,
    ...world.history
  ], issues)

  for (const waterway of world.waterways) {
    const downstreamValid = waterway.kind === 'estuary'
      ? waterway.downstreamWaterwayId === undefined
      : typeof waterway.downstreamWaterwayId === 'string' && waterway.downstreamWaterwayId !== waterway.id && has(world.waterways, waterway.downstreamWaterwayId)
    if (waterway.watershedId !== world.watershed.id || !downstreamValid) issues.push(issue(waterway.id, 'initial-world.invalid-waterway'))
  }
  if (!has(world.waterways, world.watershed.outletWaterwayId) || byId(world.waterways, world.watershed.outletWaterwayId)?.kind !== 'estuary') issues.push(issue(world.watershed.id, 'initial-world.invalid-waterway'))
  if (world.climate.watershedId !== world.watershed.id) issues.push(issue(world.climate.id, 'initial-world.invalid-climate'))
  for (const season of world.seasons) if (season.climateId !== world.climate.id) issues.push(issue(season.id, 'initial-world.invalid-season'))
  for (const resource of world.resources) if (!has(world.waterways, resource.waterwayId) || resource.availabilityLevel !== 6 - configuration.resourceScarcity) issues.push(issue(resource.id, 'initial-world.invalid-resource'))
  for (const ecology of world.ecologies) {
    if (ecology.climateId !== world.climate.id || ecology.resourceIds.length === 0 || ecology.resourceIds.some(id => !has(world.resources, id))) issues.push(issue(ecology.id, 'initial-world.invalid-ecology'))
  }
  for (const settlement of world.settlements) {
    const viableResource = settlement.resourceIds.some(id => byId(world.resources, id)?.waterwayId === settlement.waterwayId)
    if (!has(world.waterways, settlement.waterwayId) || !has(world.ecologies, settlement.ecologyId) || settlement.resourceIds.length === 0 || settlement.resourceIds.some(id => !has(world.resources, id)) || !viableResource || settlement.populationBand !== configuration.populationDensity) issues.push(issue(settlement.id, 'initial-world.invalid-settlement'))
  }
  for (const institution of world.institutions) if (!has(world.settlements, institution.settlementId)) issues.push(issue(institution.id, 'initial-world.invalid-institution'))
  for (const person of world.people) {
    const institution = byId(world.institutions, person.institutionId)
    if (!has(world.settlements, person.settlementId) || !institution || institution.settlementId !== person.settlementId || person.detailLevel !== configuration.simulationFidelity) issues.push(issue(person.id, 'initial-world.invalid-person-seed'))
  }
  for (const route of world.routes) {
    const origin = byId(world.settlements, route.originSettlementId)
    const destination = byId(world.settlements, route.destinationSettlementId)
    if (!origin || !destination || origin.id === destination.id || route.waterwayIds.length === 0 || route.waterwayIds.some(id => !has(world.waterways, id)) || !route.waterwayIds.includes(origin.waterwayId) || !route.waterwayIds.includes(destination.waterwayId)) issues.push(issue(route.id, 'initial-world.invalid-route'))
  }
  for (const link of world.tradeLinks) {
    const route = byId(world.routes, link.routeId)
    const origin = byId(world.settlements, link.originSettlementId)
    const destination = byId(world.settlements, link.destinationSettlementId)
    if (!route || !origin || !destination || route.originSettlementId !== origin.id || route.destinationSettlementId !== destination.id || !origin.resourceIds.includes(link.resourceId) || !has(world.resources, link.resourceId)) issues.push(issue(link.id, 'initial-world.invalid-trade-link'))
  }
  for (const hazard of world.routeHazards) if (!has(world.routes, hazard.routeId) || !has(world.seasons, hazard.seasonId) || hazard.pressureLevel !== configuration.dangerPressure) issues.push(issue(hazard.id, 'initial-world.invalid-route-hazard'))
  for (const event of world.history) {
    const person = byId(world.people, event.personId)
    const institution = byId(world.institutions, event.institutionId)
    const route = byId(world.routes, event.routeId)
    if (!person || !institution || !has(world.settlements, event.settlementId) || !route || event.yearsBeforePlay < 1 || event.yearsBeforePlay > configuration.historyYears || person.institutionId !== institution.id || person.settlementId !== event.settlementId || (route.originSettlementId !== event.settlementId && route.destinationSettlementId !== event.settlementId)) issues.push(issue(event.id, 'initial-world.invalid-history-event'))
  }

  const safety = auditMedievalContentSafety(initialWorldContentRecords(world))
  if (safety.status === 'rejected') issues.push(...safety.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  return issues
}

const isRecord = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const isRecordArray = (value: unknown): value is readonly Record<string, unknown>[] => Array.isArray(value) && value.every(isRecord)

/** Runtime boundary for storage before it treats a record as an initial world. */
export const isInitialWorld = (value: unknown): value is InitialWorld => {
  if (!isRecord(value) || value.version !== 1 || typeof value.id !== 'string' || !value.id || !Number.isSafeInteger(value.candidateAttempt) || typeof value.configurationFingerprint !== 'string' || typeof value.historyHorizonYears !== 'number' || typeof value.generationDetail !== 'string' || !isRecord(value.watershed) || !isRecord(value.climate) || !isRecordArray(value.waterways) || !isRecordArray(value.seasons) || !isRecordArray(value.resources) || !isRecordArray(value.ecologies) || !isRecordArray(value.settlements) || !isRecordArray(value.institutions) || !isRecordArray(value.people) || !isRecordArray(value.routes) || !isRecordArray(value.tradeLinks) || !isRecordArray(value.routeHazards) || !isRecordArray(value.history)) return false
  const allRecords = [value.watershed, value.climate, ...value.waterways, ...value.seasons, ...value.resources, ...value.ecologies, ...value.settlements, ...value.institutions, ...value.people, ...value.routes, ...value.tradeLinks, ...value.routeHazards, ...value.history]
  return allRecords.every(record => typeof record.id === 'string' && record.id) && isWithinBounds(value as unknown as InitialWorld)
}

/**
 * This small pure helper exposes candidate order for focused tests while the
 * production generator always supplies its own four-attempt plan and builder.
 */
export const selectInitialWorldCandidate = (
  plan: readonly GenerationAttempt[],
  buildCandidate: (attempt: GenerationAttempt) => InitialWorld,
  validateCandidate: (world: InitialWorld) => readonly InitialWorldValidationIssue[]
): InitialWorldCandidateSelection | ExhaustedInitialWorldCandidateSelection => {
  const candidates: InitialWorldCandidateDiagnostic[] = []
  for (const attempt of plan) {
    const world = buildCandidate(attempt)
    const issues = [...validateCandidate(world)]
    if (world.candidateAttempt !== attempt.attempt) issues.push(issue(world.id, 'initial-world.invalid-candidate-attempt'))
    const diagnostic: InitialWorldCandidateDiagnostic = { attempt: attempt.attempt, streamSeed: attempt.streamSeed, status: issues.length === 0 ? 'accepted' : 'rejected', issues }
    candidates.push(diagnostic)
    if (diagnostic.status === 'accepted') {
      return {
        status: 'selected',
        world,
        diagnostics: {
          version: INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION,
          generatorVersion: INITIAL_WORLD_GENERATOR_VERSION,
          contentSafetyPolicyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
          selectedAttempt: attempt.attempt,
          candidates
        }
      }
    }
  }
  return {
    status: 'exhausted',
    diagnostics: {
      version: INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION,
      generatorVersion: INITIAL_WORLD_GENERATOR_VERSION,
      contentSafetyPolicyVersion: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION,
      candidates
    }
  }
}

export class InitialWorldGenerationExhaustedError extends Error {
  constructor(readonly diagnostics: ExhaustedInitialWorldCandidateSelection['diagnostics']) {
    super(`initial-world generation exhausted ${diagnostics.candidates.length} deterministic candidates`)
    this.name = 'InitialWorldGenerationExhaustedError'
  }
}

/** Generates the first valid member of the existing bounded retry plan. */
export const generateInitialWorld = (seed: string, configuration: WorldGenerationConfig): InitialWorldGeneration => {
  const plan = generationRetryPlan(seed, configuration)
  const selection = selectInitialWorldCandidate(
    plan,
    attempt => buildInitialWorldCandidate(configuration, attempt),
    world => validateInitialWorldCandidate(world, configuration)
  )
  if (selection.status === 'exhausted') throw new InitialWorldGenerationExhaustedError(selection.diagnostics)
  return { world: selection.world, diagnostics: selection.diagnostics }
}
