import { isResolvedWorldGenerationConfig, type SimulationFidelity, type WorldGenerationConfig } from './generation-config'
import type { PersistentPersonRecord } from './persistent-person'
import type { FoundationWorld } from './types'
import { foundationWorldContentSatisfiesSafetyPolicy, foundationWorldInitialWorldMatchesManifest, foundationWorldTemporalStateMatches } from './world'

/**
 * A fidelity plan is an ephemeral, deterministic view of durable world data.
 * It deliberately has no reducer and no persisted cursor: the following
 * scheduler slice owns when its cadence is actually processed.
 */
export const FIDELITY_PLANNING_CONTRACT_VERSION = 1 as const

export const FIDELITY_BUDGETS = {
  focused: {
    loadedPlaces: 2,
    loadedPeople: 2,
    nearbyPeople: 1,
    recurringPeople: 1,
    distantIndividualSummaries: 1,
    loadedInstitutions: 2,
    distantSettlementSummaries: 2,
    distantInstitutionSummaries: 2
  },
  balanced: {
    loadedPlaces: 4,
    loadedPeople: 3,
    nearbyPeople: 2,
    recurringPeople: 2,
    distantIndividualSummaries: 4,
    loadedInstitutions: 4,
    distantSettlementSummaries: 4,
    distantInstitutionSummaries: 4
  },
  deep: {
    loadedPlaces: 8,
    loadedPeople: 4,
    nearbyPeople: 3,
    recurringPeople: 3,
    distantIndividualSummaries: 8,
    loadedInstitutions: 8,
    distantSettlementSummaries: 8,
    distantInstitutionSummaries: 8
  }
} as const

export type FidelityBudget = typeof FIDELITY_BUDGETS[SimulationFidelity]
export type FidelityLoadedLocationKind = 'site' | 'frontier-region'
export type FidelityIndividualTier = 'loaded' | 'nearby' | 'recurring' | 'distant-individual-summary' | 'deferred' | 'historical-only'
export type FidelityPlaceTier = 'loaded-place' | 'distant-settlement-summary' | 'deferred'
export type FidelityInstitutionTier = 'loaded-institution' | 'distant-institution-summary' | 'deferred'
export type FidelityRecurrenceReason = 'current-work' | 'relationship' | 'family' | 'memory' | 'commitment'

export interface FidelityLoadedLocation {
  kind: FidelityLoadedLocationKind
  id: string
}

export interface FidelityPlanningRequest {
  /** Must match the saved active courier; it is not UI focus or selection state. */
  activeCourierId: string
  /** Additional loaded simulation places. Jomon and its moored site are always included. */
  loadedLocations: readonly FidelityLoadedLocation[]
  world: FoundationWorld
}

export type FidelityCadence =
  | { kind: 'every-time-bearing-action' }
  | { kind: 'elapsed-summary'; intervalMinutes: 5 | 30 | 120 | 240; nextEligibleAtWorldTime: number }
  | { kind: 'not-scheduled' }

export interface FidelityJomonPlan {
  vesselId: 'vessel:jomon'
  location: { kind: 'site' | 'quay'; id: string }
  tier: 'loaded-place'
  cadence: { kind: 'every-time-bearing-action' }
}

export interface FidelityActiveCourierPlan {
  personId: string
  tier: 'loaded'
}

export interface FidelityIndividualAssignment {
  personId: string
  tier: FidelityIndividualTier
  recurrenceReasons: readonly FidelityRecurrenceReason[]
  cadence: FidelityCadence
}

export interface FidelityPlaceAssignment {
  siteId: string
  tier: FidelityPlaceTier
  cadence: FidelityCadence
}

export interface FidelityInstitutionAssignment {
  institutionId: string
  siteId: string
  tier: FidelityInstitutionTier
  cadence: FidelityCadence
}

export interface FidelityPlan {
  version: typeof FIDELITY_PLANNING_CONTRACT_VERSION
  worldId: string
  creationDigest: string
  worldTime: number
  simulationFidelity: SimulationFidelity
  budget: FidelityBudget
  jomon: FidelityJomonPlan
  activeCourier: FidelityActiveCourierPlan
  /** Canonical set: Jomon, its current site when applicable, then context locations. */
  loadedLocations: readonly ({ kind: 'jomon'; id: 'vessel:jomon' } | FidelityLoadedLocation)[]
  individuals: readonly FidelityIndividualAssignment[]
  places: readonly FidelityPlaceAssignment[]
  institutions: readonly FidelityInstitutionAssignment[]
}

export type FidelityPlanningDiagnosticCode =
  | 'fidelity.malformed-request'
  | 'fidelity.invalid-world'
  | 'fidelity.invalid-configuration'
  | 'fidelity.invalid-active-courier'
  | 'fidelity.invalid-courier-location'
  | 'fidelity.malformed-loaded-location'
  | 'fidelity.duplicate-loaded-location'
  | 'fidelity.invalid-loaded-location'
  | 'fidelity.loaded-location-budget-exceeded'
  | 'fidelity.time-overflow'

export interface FidelityPlanningDiagnostic {
  code: FidelityPlanningDiagnosticCode
  recordId: string
}

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const validId = (value: unknown): value is string => typeof value === 'string' && value.length > 0
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const issue = (recordId: string, code: FidelityPlanningDiagnosticCode): FidelityPlanningDiagnostic => ({ recordId, code })
const canonicalDiagnostics = (diagnostics: readonly FidelityPlanningDiagnostic[]): readonly FidelityPlanningDiagnostic[] => [...new Map(diagnostics.map(diagnostic => [`${diagnostic.recordId}\u0000${diagnostic.code}`, diagnostic])).values()].sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const locationKey = (location: FidelityLoadedLocation): string => `${location.kind}:${location.id}`
const budgetFor = (fidelity: SimulationFidelity): FidelityBudget => ({ ...FIDELITY_BUDGETS[fidelity] })

const cadence = (tier: FidelityIndividualTier | FidelityPlaceTier | FidelityInstitutionTier, worldTime: number): FidelityCadence => {
  if (tier === 'loaded' || tier === 'loaded-place' || tier === 'loaded-institution') return { kind: 'every-time-bearing-action' }
  if (tier === 'nearby') return elapsedCadence(5, worldTime)
  if (tier === 'recurring') return elapsedCadence(30, worldTime)
  if (tier === 'distant-individual-summary') return elapsedCadence(120, worldTime)
  if (tier === 'distant-settlement-summary' || tier === 'distant-institution-summary') return elapsedCadence(240, worldTime)
  return { kind: 'not-scheduled' }
}

const elapsedCadence = (intervalMinutes: 5 | 30 | 120 | 240, worldTime: number): FidelityCadence => ({
  kind: 'elapsed-summary',
  intervalMinutes,
  nextEligibleAtWorldTime: (Math.floor(worldTime / intervalMinutes) + 1) * intervalMinutes
})

const validWorld = (value: unknown): value is FoundationWorld => {
  if (!record(value) || value.version !== 4 || value.status !== 'active' || !record(value.manifest) || !record(value.manifest.creation) || !record(value.state)) return false
  try {
    return foundationWorldInitialWorldMatchesManifest(value as unknown as FoundationWorld)
      && foundationWorldContentSatisfiesSafetyPolicy(value as unknown as FoundationWorld)
      && foundationWorldTemporalStateMatches(value as unknown as FoundationWorld)
  } catch { return false }
}

const validLoadedLocationShape = (value: unknown): value is FidelityLoadedLocation => record(value) && hasOnlyKeys(value, ['kind', 'id']) && (value.kind === 'site' || value.kind === 'frontier-region') && validId(value.id)

const currentJomonSite = (world: FoundationWorld): FidelityLoadedLocation | undefined => world.state.jomon.location.kind === 'site'
  ? { kind: 'site', id: world.state.jomon.location.id }
  : undefined

const isMaterializedRegion = (world: FoundationWorld, regionId: string): boolean => world.state.geography.frontier.regions.some(region => region.commitment.id === regionId && region.status === 'materialized')
const loadedLocationIsValid = (world: FoundationWorld, location: FidelityLoadedLocation): boolean => location.kind === 'site'
  ? world.state.sites.sites.some(site => site.id === location.id)
  : isMaterializedRegion(world, location.id)

const personAtLocation = (person: PersistentPersonRecord, loaded: ReadonlySet<string>): boolean => person.location.kind === 'jomon'
  ? person.location.id === 'vessel:jomon'
  : loaded.has(`${person.location.kind}:${person.location.id}`)

const isNearJomonOrCourier = (person: PersistentPersonRecord, courier: PersistentPersonRecord, jomonSite: FidelityLoadedLocation | undefined): boolean => {
  if (person.id === courier.id || (person.location.kind === courier.location.kind && person.location.id === courier.location.id)) return true
  if (person.location.kind === 'jomon' && courier.location.kind === 'site' && jomonSite !== undefined && courier.location.id === jomonSite.id) return true
  if (courier.location.kind === 'jomon' && person.location.kind === 'site' && jomonSite !== undefined && person.location.id === jomonSite.id) return true
  return false
}

const recurrenceReasonsFor = (person: PersistentPersonRecord, courier: PersistentPersonRecord, worldTime: number): readonly FidelityRecurrenceReason[] => {
  const sharedAssignment = person.work.assignment.status === 'assigned'
    && courier.work.assignment.status === 'assigned'
    && person.work.assignment.assignmentId === courier.work.assignment.assignmentId
  const relationship = person.relationships.some(item => item.targetPersonId === courier.id)
  const family = person.family.some(link => link.relative.kind === 'instantiated-person' && link.relative.personId === courier.id)
  const memory = person.memories.some(item => item.atWorldTime <= worldTime)
  const commitment = person.commitments.some(item => item.status === 'active')
  return [
    ...(sharedAssignment ? ['current-work' as const] : []),
    ...(relationship ? ['relationship' as const] : []),
    ...(family ? ['family' as const] : []),
    ...(memory ? ['memory' as const] : []),
    ...(commitment ? ['commitment' as const] : [])
  ]
}

interface IndividualCandidate {
  person: PersistentPersonRecord
  recurrenceReasons: readonly FidelityRecurrenceReason[]
  latestMemoryAtWorldTime: number
}

/** Stable pressure order: active courier, current work, recurrence evidence, capacity, then ID. */
const compareCandidates = (activeCourierId: string) => (left: IndividualCandidate, right: IndividualCandidate): number => {
  const active = Number(right.person.id === activeCourierId) - Number(left.person.id === activeCourierId)
  if (active) return active
  const assigned = Number(right.person.work.assignment.status === 'assigned') - Number(left.person.work.assignment.status === 'assigned')
  if (assigned) return assigned
  const recurrence = right.recurrenceReasons.length - left.recurrenceReasons.length
  if (recurrence) return recurrence
  const memory = right.latestMemoryAtWorldTime - left.latestMemoryAtWorldTime
  if (memory) return memory
  const capacity = right.person.work.capacity.current - left.person.work.capacity.current
  if (capacity) return capacity
  return compare(left.person.id, right.person.id)
}

const candidateFor = (person: PersistentPersonRecord, courier: PersistentPersonRecord, worldTime: number): IndividualCandidate => ({
  person,
  recurrenceReasons: recurrenceReasonsFor(person, courier, worldTime),
  latestMemoryAtWorldTime: Math.max(-1, ...person.memories.map(memory => memory.atWorldTime))
})

const assignPeople = (world: FoundationWorld, courier: PersistentPersonRecord, loaded: ReadonlySet<string>, budget: FidelityBudget): readonly FidelityIndividualAssignment[] => {
  const living = world.state.people.records.filter(person => person.life.status === 'living').map(person => candidateFor(person, courier, world.state.temporal.worldTime))
  const assigned = new Map<string, FidelityIndividualTier>()
  const select = (tier: FidelityIndividualTier, candidates: readonly IndividualCandidate[], limit: number): void => {
    for (const candidate of [...candidates].filter(candidate => !assigned.has(candidate.person.id)).sort(compareCandidates(courier.id)).slice(0, limit)) assigned.set(candidate.person.id, tier)
  }
  select('loaded', living.filter(candidate => personAtLocation(candidate.person, loaded)), budget.loadedPeople)
  select('nearby', living.filter(candidate => isNearJomonOrCourier(candidate.person, courier, currentJomonSite(world))), budget.nearbyPeople)
  select('recurring', living.filter(candidate => candidate.recurrenceReasons.length > 0), budget.recurringPeople)
  select('distant-individual-summary', living, budget.distantIndividualSummaries)

  return world.state.people.records.map(person => {
    const candidate = candidateFor(person, courier, world.state.temporal.worldTime)
    const tier: FidelityIndividualTier = person.life.status === 'dead' ? 'historical-only' : assigned.get(person.id) ?? 'deferred'
    return { personId: person.id, tier, recurrenceReasons: candidate.recurrenceReasons, cadence: cadence(tier, world.state.temporal.worldTime) }
  }).sort((left, right) => compare(left.personId, right.personId))
}

const siteIsLoaded = (world: FoundationWorld, siteId: string, locations: ReadonlySet<string>): boolean => {
  if (locations.has(`site:${siteId}`)) return true
  return world.state.geography.frontier.regions.some(region => region.status === 'materialized' && region.materialization.site.id === siteId && locations.has(`frontier-region:${region.commitment.id}`))
}

const assignPlaces = (world: FoundationWorld, locations: ReadonlySet<string>, budget: FidelityBudget): readonly FidelityPlaceAssignment[] => {
  const assigned = new Map<string, FidelityPlaceTier>()
  const sites = [...world.state.sites.sites].sort((left, right) => compare(left.id, right.id))
  for (const site of sites.filter(site => siteIsLoaded(world, site.id, locations)).slice(0, budget.loadedPlaces)) assigned.set(site.id, 'loaded-place')
  for (const site of sites.filter(site => !assigned.has(site.id)).slice(0, budget.distantSettlementSummaries)) assigned.set(site.id, 'distant-settlement-summary')
  return sites.map(site => {
    const tier = assigned.get(site.id) ?? 'deferred'
    return { siteId: site.id, tier, cadence: cadence(tier, world.state.temporal.worldTime) }
  })
}

const assignInstitutions = (world: FoundationWorld, places: readonly FidelityPlaceAssignment[], budget: FidelityBudget): readonly FidelityInstitutionAssignment[] => {
  const placeTiers = new Map(places.map(place => [place.siteId, place.tier]))
  const institutions = [...world.state.institutions.registry].sort((left, right) => compare(left.id, right.id))
  const assigned = new Map<string, FidelityInstitutionTier>()
  for (const institution of institutions.filter(institution => placeTiers.get(institution.siteId) === 'loaded-place').slice(0, budget.loadedInstitutions)) assigned.set(institution.id, 'loaded-institution')
  for (const institution of institutions.filter(institution => !assigned.has(institution.id)).slice(0, budget.distantInstitutionSummaries)) assigned.set(institution.id, 'distant-institution-summary')
  return institutions.map(institution => {
    const tier = assigned.get(institution.id) ?? 'deferred'
    return { institutionId: institution.id, siteId: institution.siteId, tier, cadence: cadence(tier, world.state.temporal.worldTime) }
  })
}

/** Pure validation: failure never writes state, schedules an event, or advances time. */
export const validateFidelityPlanningRequest = (value: unknown): readonly FidelityPlanningDiagnostic[] => {
  const diagnostics: FidelityPlanningDiagnostic[] = []
  if (!record(value) || !hasOnlyKeys(value, ['activeCourierId', 'loadedLocations', 'world']) || !validId(value.activeCourierId) || !Array.isArray(value.loadedLocations)) return [issue('fidelity:request', 'fidelity.malformed-request')]
  if (!validWorld(value.world)) return [issue('fidelity:world', 'fidelity.invalid-world')]
  const world = value.world
  const configuration: WorldGenerationConfig = world.manifest.creation.resolvedConfiguration
  if (!isResolvedWorldGenerationConfig(configuration) || configuration.simulationFidelity !== world.manifest.creation.resolvedConfiguration.simulationFidelity) diagnostics.push(issue('fidelity:configuration', 'fidelity.invalid-configuration'))
  if (world.state.temporal.worldTime > Number.MAX_SAFE_INTEGER - 240) diagnostics.push(issue('fidelity:clock', 'fidelity.time-overflow'))
  if (world.state.courier.initialCourierId !== value.activeCourierId) diagnostics.push(issue('fidelity:courier', 'fidelity.invalid-active-courier'))
  const courier = world.state.people.records.find(person => person.id === value.activeCourierId)
  if (!courier || courier.life.status !== 'living' || courier.work.availability !== 'available') diagnostics.push(issue('fidelity:courier', 'fidelity.invalid-active-courier'))

  const seen = new Set<string>()
  const implicitJomonSite = currentJomonSite(world)
  const implicitJomonSiteKey = implicitJomonSite === undefined ? undefined : locationKey(implicitJomonSite)
  for (const [index, location] of value.loadedLocations.entries()) {
    const id = `fidelity:loaded-location:${index}`
    if (!validLoadedLocationShape(location)) { diagnostics.push(issue(id, 'fidelity.malformed-loaded-location')); continue }
    const key = locationKey(location)
    if (seen.has(key) || key === implicitJomonSiteKey) diagnostics.push(issue(key, 'fidelity.duplicate-loaded-location'))
    seen.add(key)
    if (!loadedLocationIsValid(world, location)) diagnostics.push(issue(key, 'fidelity.invalid-loaded-location'))
  }
  const budget = budgetFor(configuration.simulationFidelity)
  const totalLoadedPlaces = new Set([...(implicitJomonSite === undefined ? [] : [locationKey(implicitJomonSite)]), ...seen]).size
  if (totalLoadedPlaces > budget.loadedPlaces) diagnostics.push(issue('fidelity:loaded-locations', 'fidelity.loaded-location-budget-exceeded'))
  const allLoaded = new Set<string>(['jomon:vessel:jomon', ...(implicitJomonSite === undefined ? [] : [locationKey(implicitJomonSite)]), ...seen])
  if (courier && !personAtLocation(courier, allLoaded)) diagnostics.push(issue(`fidelity:courier:${courier.id}`, 'fidelity.invalid-courier-location'))
  return canonicalDiagnostics(diagnostics)
}

/**
 * Produces an inspectable plan only. It does not add temporal events or alter
 * world records, so browser idle/inspection cannot gain simulation authority.
 */
export const createFidelityPlan = (request: FidelityPlanningRequest | unknown): FidelityPlan => {
  const diagnostics = validateFidelityPlanningRequest(request)
  if (diagnostics.length) throw new FidelityPlanningContractError(diagnostics)
  const { world, activeCourierId } = request as FidelityPlanningRequest
  const budget = budgetFor(world.manifest.creation.resolvedConfiguration.simulationFidelity)
  const implicitJomonSite = currentJomonSite(world)
  const additional = (request as FidelityPlanningRequest).loadedLocations.map(location => ({ kind: location.kind, id: location.id })).sort((left, right) => compare(locationKey(left), locationKey(right)))
  const loadedLocations = [
    { kind: 'jomon' as const, id: world.jomon.id },
    ...(implicitJomonSite === undefined ? [] : [implicitJomonSite]),
    ...additional.filter(location => implicitJomonSite === undefined || locationKey(location) !== locationKey(implicitJomonSite))
  ]
  const loadedLocationKeys = new Set(loadedLocations.map(location => `${location.kind}:${location.id}`))
  const courier = world.state.people.records.find(person => person.id === activeCourierId)!
  const individuals = assignPeople(world, courier, loadedLocationKeys, budget)
  const places = assignPlaces(world, loadedLocationKeys, budget)
  const institutions = assignInstitutions(world, places, budget)
  return {
    version: FIDELITY_PLANNING_CONTRACT_VERSION,
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: world.state.temporal.worldTime,
    simulationFidelity: world.manifest.creation.resolvedConfiguration.simulationFidelity,
    budget,
    jomon: { vesselId: world.jomon.id, location: structuredClone(world.state.jomon.location), tier: 'loaded-place', cadence: { kind: 'every-time-bearing-action' } },
    activeCourier: { personId: activeCourierId, tier: 'loaded' },
    loadedLocations,
    individuals,
    places,
    institutions
  }
}

export class FidelityPlanningContractError extends Error {
  constructor(readonly diagnostics: readonly FidelityPlanningDiagnostic[]) {
    super(`fidelity plan rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'FidelityPlanningContractError'
  }
}
