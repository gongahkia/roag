import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { DelegationState } from './delegation'
import type { FidelityIndividualAssignment, FidelityIndividualTier, FidelityPlan } from './fidelity'
import type { PersistentPersonRecord } from './persistent-person'
import { hashSeed } from './rng'
import type { SimulationCatchUpState } from './simulation-catchup'
import type { WorldEraState } from './world-era'

/**
 * Autonomy v1 is deliberately small: it folds adult need pressure and records
 * the current deterministic choice projection. It does not perform work,
 * trade, travel, healing, social-memory, or any other domain effect.
 */
export const AUTONOMY_CONTRACT_VERSION = 1 as const
export const AUTONOMY_STATE_VERSION = 1 as const
export const AUTONOMY_NEED_WINDOW_MINUTES = 30 as const
export const AUTONOMY_URGENT_NEED_THRESHOLD = 4 as const

export const AUTONOMY_LIMITS = {
  processing: 24,
  observations: 24,
  factorsPerObservation: 16,
  identityLength: 160
} as const

/** Need values are integer pressure: 0 is settled and 5 is urgent. */
export const AUTONOMY_NEED_KEYS = ['nourishment', 'rest', 'shelter', 'safety'] as const
export type AutonomyNeedKey = typeof AUTONOMY_NEED_KEYS[number]

export const AUTONOMY_CHOICE_KINDS = [
  'continue-accepted-delegated-work',
  'seek-recovery',
  'address-urgent-personal-needs',
  'maintain-household-readiness',
  'support-household',
  'hold-wait'
] as const
export type AutonomyChoiceKind = typeof AUTONOMY_CHOICE_KINDS[number]

export const AUTONOMY_OPPORTUNITY_KINDS = [
  'accepted-delegated-work',
  'recovery',
  'urgent-personal-needs',
  'household-readiness',
  'household-support',
  'no-active-opportunity'
] as const
export type AutonomyOpportunityKind = typeof AUTONOMY_OPPORTUNITY_KINDS[number]

export type AutonomyObservationDetail = 'detailed' | 'summary'
export type AutonomyObservedTier = Exclude<FidelityIndividualTier, 'deferred' | 'historical-only'>

/** Closed, prose-free explanations used by later UI and social-memory work. */
export const AUTONOMY_FACTOR_CODES = [
  'tier-loaded', 'tier-nearby', 'tier-recurring', 'tier-distant-summary',
  'detail-detailed', 'detail-summary',
  'opportunity-accepted-delegated-work', 'opportunity-recovery', 'opportunity-urgent-personal-needs', 'opportunity-household-readiness', 'opportunity-household-support', 'opportunity-none',
  'choice-continue-accepted-delegated-work', 'choice-seek-recovery', 'choice-address-urgent-personal-needs', 'choice-maintain-household-readiness', 'choice-support-household', 'choice-hold-wait',
  'health-steady', 'health-strained', 'health-injured', 'health-recovering',
  'need-settled', 'need-pressured', 'need-urgent',
  'capacity-ready', 'capacity-unavailable', 'capacity-exhausted',
  'current-work-idle', 'current-work-committed', 'active-delegated-task',
  'household-jomon', 'household-site', 'household-frontier',
  'location-at-home', 'location-away-from-home',
  'relationship-courier-positive', 'relationship-courier-neutral', 'relationship-courier-negative', 'relationship-courier-none',
  'role-jomon-crew', 'role-local-occupation',
  'era-base', 'era-elevated', 'era-plateau'
] as const
export type AutonomyFactorCode = typeof AUTONOMY_FACTOR_CODES[number]

export interface AutonomyPersonProcessingState {
  personId: string
  /** Complete fixed need windows folded into the persistent person record. */
  processedNeedWindows: number
}

/** One canonical latest observation per currently scheduled individual. */
export interface AutonomyObservation {
  id: string
  personId: string
  tier: AutonomyObservedTier
  detail: AutonomyObservationDetail
  opportunity: AutonomyOpportunityKind
  choice: AutonomyChoiceKind
  cursorId: string
  cadenceMinutes: 1 | 5 | 30 | 120
  processedThroughWorldTime: number
  processedIntervals: number
  token: number
  factors: readonly AutonomyFactorCode[]
  contentSafety: MedievalContentSafetyClassification
}

export interface AutonomyState {
  version: typeof AUTONOMY_STATE_VERSION
  processing: readonly AutonomyPersonProcessingState[]
  observations: readonly AutonomyObservation[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

export interface AutonomyContext {
  worldId: string
  creationDigest: string
  worldTime: number
  activeCourierId?: string
  people: readonly PersistentPersonRecord[]
  delegation: DelegationState
  era: WorldEraState
  simulation: SimulationCatchUpState
  /** Supplied only by the owning world reducer/full-world validator. */
  plan?: FidelityPlan
}

export interface AutonomyTransition {
  state: AutonomyState
  people: readonly PersistentPersonRecord[]
}

export type AutonomyDiagnosticCode =
  | 'autonomy.malformed-state'
  | 'autonomy.invalid-version'
  | 'autonomy.invalid-context'
  | 'autonomy.processing-limit'
  | 'autonomy.observation-limit'
  | 'autonomy.duplicate-id'
  | 'autonomy.noncanonical-order'
  | 'autonomy.invalid-processing'
  | 'autonomy.invalid-observation'
  | 'autonomy.invalid-reference'
  | 'autonomy.invalid-token'
  | 'autonomy.invalid-plan-projection'
  | 'autonomy.invalid-content-audit'
  | 'autonomy.invalid-action'
  | MedievalContentSafetyDiagnosticCode

export interface AutonomyDiagnostic {
  recordId: string
  code: AutonomyDiagnosticCode
}

export class AutonomyContractError extends Error {
  constructor(readonly diagnostics: readonly AutonomyDiagnostic[]) {
    super(`autonomy rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'AutonomyContractError'
  }
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const safeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const validId = (value: unknown): value is string => typeof value === 'string' && value.length > 0 && value.length <= AUTONOMY_LIMITS.identityLength && /^[a-z][a-z0-9:._-]*$/i.test(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const canonical = <Value extends string>(values: readonly Value[]): readonly Value[] => [...new Set(values)].sort(compare)
const issue = (recordId: string, code: AutonomyDiagnosticCode): AutonomyDiagnostic => ({ recordId, code })
const canonicalIssues = (issues: readonly AutonomyDiagnostic[]): readonly AutonomyDiagnostic[] => [...new Map(issues.map(value => [`${value.recordId}\u0000${value.code}`, value])).values()].sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const observedTiers: readonly AutonomyObservedTier[] = ['loaded', 'nearby', 'recurring', 'distant-individual-summary']

const observationClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('simulation-summary', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const observationIdFor = (personId: string): string => `autonomy:${personId}`
const cursorIdForPerson = (personId: string): string => `catch-up:person:${personId}`
const detailForTier = (tier: AutonomyObservedTier): AutonomyObservationDetail => tier === 'distant-individual-summary' ? 'summary' : 'detailed'
const safeCadence = (value: unknown): value is 1 | 5 | 30 | 120 => value === 1 || value === 5 || value === 30 || value === 120
const needWindowsAt = (worldTime: number): number => Math.floor(worldTime / AUTONOMY_NEED_WINDOW_MINUTES)
const autonomyTokenFor = (context: Pick<AutonomyContext, 'worldId' | 'creationDigest'>, personId: string, tier: AutonomyObservedTier, through: number): number => hashSeed(`jomon-autonomy-v${AUTONOMY_CONTRACT_VERSION}|${context.creationDigest}|${context.worldId}|${personId}|${tier}|window:${through}`)

export const autonomyContentRecords = (state: Pick<AutonomyState, 'observations'>): readonly ClassifiedMedievalContent[] => state.observations.map(observation => ({
  id: `autonomy:${observation.id}`,
  domain: 'simulation-summary' as const,
  classification: observation.contentSafety
}))

const audit = (state: Pick<AutonomyState, 'observations'>): MedievalContentSafetyAudit => {
  const result = auditMedievalContentSafety(autonomyContentRecords(state))
  if (result.status === 'rejected') throw new AutonomyContractError(result.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  return result
}

const activeTaskFor = (delegation: DelegationState, personId: string): boolean => delegation.tasks.some(task => task.status === 'in-progress' && task.recipientId === personId)
const maximumNeed = (person: PersistentPersonRecord): number => Math.max(...AUTONOMY_NEED_KEYS.map(key => person.needs[key]))
const needFactor = (person: PersistentPersonRecord): AutonomyFactorCode => maximumNeed(person) >= AUTONOMY_URGENT_NEED_THRESHOLD
  ? 'need-urgent'
  : maximumNeed(person) > 0 ? 'need-pressured' : 'need-settled'
const healthFactor = (person: PersistentPersonRecord): AutonomyFactorCode => `health-${person.health.condition}` as AutonomyFactorCode
const tierFactor = (tier: AutonomyObservedTier): AutonomyFactorCode => tier === 'distant-individual-summary' ? 'tier-distant-summary' : `tier-${tier}` as AutonomyFactorCode
const householdFactor = (person: PersistentPersonRecord): AutonomyFactorCode => `household-${person.household.kind.replace('-household', '')}` as AutonomyFactorCode
const roleFactor = (person: PersistentPersonRecord): AutonomyFactorCode => person.work.role.kind === 'jomon-crew-role' ? 'role-jomon-crew' : 'role-local-occupation'
const atHome = (person: PersistentPersonRecord): boolean => person.home.kind === person.location.kind && person.home.id === person.location.id
const relationshipFactor = (person: PersistentPersonRecord, courierId: string | undefined): AutonomyFactorCode => {
  if (courierId === undefined || courierId === person.id) return 'relationship-courier-none'
  const relationship = person.relationships.find(item => item.targetPersonId === courierId)
  if (!relationship) return 'relationship-courier-none'
  return relationship.standing > 0 ? 'relationship-courier-positive' : relationship.standing < 0 ? 'relationship-courier-negative' : 'relationship-courier-neutral'
}
const eraFactor = (era: WorldEraState): AutonomyFactorCode => era.profile.rawEscalation === 'base' ? 'era-base' : era.profile.rawEscalation === 'elevated' ? 'era-elevated' : 'era-plateau'

const householdReadiness = (person: PersistentPersonRecord): boolean => person.household.kind === 'jomon-household'
  && atHome(person)
  && person.materialInterests.some(interest => interest === 'vessel-upkeep' || interest === 'household-upkeep' || interest === 'household-security')
const householdSupport = (person: PersistentPersonRecord, courierId: string | undefined): boolean => person.household.kind === 'jomon-household'
  && atHome(person)
  && relationshipFactor(person, courierId) === 'relationship-courier-positive'

interface ChoiceDecision {
  opportunity: AutonomyOpportunityKind
  choice: AutonomyChoiceKind
  factors: readonly AutonomyFactorCode[]
}

const decisionFor = (context: AutonomyContext, person: PersistentPersonRecord, tier: AutonomyObservedTier): ChoiceDecision => {
  const common: AutonomyFactorCode[] = [
    tierFactor(tier),
    detailForTier(tier) === 'detailed' ? 'detail-detailed' : 'detail-summary',
    healthFactor(person),
    needFactor(person),
    person.work.capacity.current === 0 ? 'capacity-exhausted' : person.work.availability === 'available' ? 'capacity-ready' : 'capacity-unavailable',
    person.work.current.status === 'idle' ? 'current-work-idle' : 'current-work-committed',
    householdFactor(person),
    atHome(person) ? 'location-at-home' : 'location-away-from-home',
    relationshipFactor(person, context.activeCourierId),
    roleFactor(person),
    eraFactor(context.era)
  ]
  if (activeTaskFor(context.delegation, person.id)) return { opportunity: 'accepted-delegated-work', choice: 'continue-accepted-delegated-work', factors: canonical([...common, 'active-delegated-task', 'opportunity-accepted-delegated-work', 'choice-continue-accepted-delegated-work']) }
  if (person.health.condition === 'recovering' || person.health.condition === 'injured') return { opportunity: 'recovery', choice: 'seek-recovery', factors: canonical([...common, 'opportunity-recovery', 'choice-seek-recovery']) }
  if (maximumNeed(person) >= AUTONOMY_URGENT_NEED_THRESHOLD) return { opportunity: 'urgent-personal-needs', choice: 'address-urgent-personal-needs', factors: canonical([...common, 'opportunity-urgent-personal-needs', 'choice-address-urgent-personal-needs']) }
  if (householdReadiness(person)) return { opportunity: 'household-readiness', choice: 'maintain-household-readiness', factors: canonical([...common, 'opportunity-household-readiness', 'choice-maintain-household-readiness']) }
  if (householdSupport(person, context.activeCourierId)) return { opportunity: 'household-support', choice: 'support-household', factors: canonical([...common, 'opportunity-household-support', 'choice-support-household']) }
  return { opportunity: 'no-active-opportunity', choice: 'hold-wait', factors: canonical([...common, 'opportunity-none', 'choice-hold-wait']) }
}

const scheduledAssignment = (assignment: FidelityIndividualAssignment | undefined): assignment is FidelityIndividualAssignment & { tier: AutonomyObservedTier; cadence: { kind: 'time-window'; intervalMinutes: 1 | 5 | 30 | 120; nextEligibleAtWorldTime: number } } => assignment !== undefined
  && oneOf(observedTiers, assignment.tier)
  && assignment.cadence.kind === 'time-window'
  && safeCadence(assignment.cadence.intervalMinutes)

const autonomousPerson = (context: AutonomyContext, person: PersistentPersonRecord): boolean => person.life.status === 'living'
  && (activeTaskFor(context.delegation, person.id) || (person.work.availability === 'available' && person.work.capacity.current > 0))

const observationFor = (context: AutonomyContext, person: PersistentPersonRecord, assignment: FidelityIndividualAssignment): AutonomyObservation | undefined => {
  if (!scheduledAssignment(assignment) || !autonomousPerson(context, person)) return undefined
  const cursor = context.simulation.cursors.find(candidate => candidate.id === cursorIdForPerson(person.id))
  if (!cursor || cursor.tier !== assignment.tier || cursor.cadenceMinutes !== assignment.cadence.intervalMinutes || !safeCadence(cursor.cadenceMinutes)) return undefined
  const decision = decisionFor(context, person, assignment.tier)
  return {
    id: observationIdFor(person.id),
    personId: person.id,
    tier: assignment.tier,
    detail: detailForTier(assignment.tier),
    opportunity: decision.opportunity,
    choice: decision.choice,
    cursorId: cursor.id,
    cadenceMinutes: cursor.cadenceMinutes,
    processedThroughWorldTime: cursor.processedThroughWorldTime,
    processedIntervals: cursor.processedIntervals,
    token: autonomyTokenFor(context, person.id, assignment.tier, cursor.processedThroughWorldTime),
    factors: decision.factors,
    contentSafety: observationClassification()
  }
}

const observationsForPlan = (context: AutonomyContext): readonly AutonomyObservation[] => context.plan === undefined ? [] : context.plan.individuals
  .map(assignment => {
    const person = context.people.find(candidate => candidate.id === assignment.personId)
    return person === undefined ? undefined : observationFor(context, person, assignment)
  })
  .filter((observation): observation is AutonomyObservation => observation !== undefined)
  .sort((left, right) => compare(left.id, right.id))

const validContext = (context: AutonomyContext): boolean => validId(context.worldId)
  && typeof context.creationDigest === 'string'
  && context.creationDigest.length > 0
  && safeInteger(context.worldTime)
  && Array.isArray(context.people)
  && new Set(context.people.map(person => person.id)).size === context.people.length
  && record(context.delegation)
  && record(context.era)
  && record(context.simulation)
  && (context.activeCourierId === undefined || validId(context.activeCourierId))

const validProcessing = (value: unknown, context: AutonomyContext): value is AutonomyPersonProcessingState => record(value)
  && hasOnlyKeys(value, ['personId', 'processedNeedWindows'])
  && validId(value.personId)
  && safeInteger(value.processedNeedWindows)
  && value.processedNeedWindows === needWindowsAt(context.worldTime)
  && context.people.some(person => person.id === value.personId && person.life.status === 'living')

const validFactors = (value: unknown): value is readonly AutonomyFactorCode[] => Array.isArray(value)
  && value.length > 0
  && value.length <= AUTONOMY_LIMITS.factorsPerObservation
  && value.every(factor => oneOf(AUTONOMY_FACTOR_CODES, factor))
  && same(value, canonical(value as AutonomyFactorCode[]))

const validObservation = (value: unknown, context: AutonomyContext): value is AutonomyObservation => record(value)
  && hasOnlyKeys(value, ['id', 'personId', 'tier', 'detail', 'opportunity', 'choice', 'cursorId', 'cadenceMinutes', 'processedThroughWorldTime', 'processedIntervals', 'token', 'factors', 'contentSafety'])
  && validId(value.id)
  && validId(value.personId)
  && value.id === observationIdFor(value.personId)
  && oneOf(observedTiers, value.tier)
  && (value.detail === 'detailed' || value.detail === 'summary')
  && value.detail === detailForTier(value.tier)
  && oneOf(AUTONOMY_OPPORTUNITY_KINDS, value.opportunity)
  && oneOf(AUTONOMY_CHOICE_KINDS, value.choice)
  && value.cursorId === cursorIdForPerson(value.personId)
  && safeCadence(value.cadenceMinutes)
  && safeInteger(value.processedThroughWorldTime)
  && value.processedThroughWorldTime > 0
  && value.processedThroughWorldTime <= context.worldTime
  && safeInteger(value.processedIntervals)
  && value.processedIntervals > 0
  && safeInteger(value.token)
  && value.token === autonomyTokenFor(context, value.personId, value.tier, value.processedThroughWorldTime)
  && validFactors(value.factors)
  && context.people.some(person => person.id === value.personId && person.life.status === 'living')
  && auditMedievalContentSafety([{ id: `autonomy:${value.id}`, domain: 'simulation-summary', classification: value.contentSafety }]).status === 'accepted'

/** Creates processing cursors only; observations start once canonical cadence work is due. */
export const createAutonomyState = (people: readonly PersistentPersonRecord[], worldTime = 0): AutonomyState => {
  if (!safeInteger(worldTime) || people.length > AUTONOMY_LIMITS.processing || new Set(people.map(person => person.id)).size !== people.length) throw new AutonomyContractError([issue('autonomy:create', 'autonomy.invalid-context')])
  const stateWithoutAudit = {
    version: AUTONOMY_STATE_VERSION,
    processing: people.filter(person => person.life.status === 'living').map(person => ({ personId: person.id, processedNeedWindows: needWindowsAt(worldTime) })).sort((left, right) => compare(left.personId, right.personId)),
    observations: [] as const
  } satisfies Omit<AutonomyState, 'contentSafetyAudit'>
  return { ...stateWithoutAudit, contentSafetyAudit: audit(stateWithoutAudit) }
}

/** Structural validation used at the mutable-world/storage boundary. */
export const validateAutonomyState = (context: AutonomyContext, value: unknown): readonly AutonomyDiagnostic[] => {
  if (!validContext(context)) return [issue('autonomy:context', 'autonomy.invalid-context')]
  if (!record(value) || !hasOnlyKeys(value, ['version', 'processing', 'observations', 'contentSafetyAudit'])) return [issue('autonomy', 'autonomy.malformed-state')]
  const issues: AutonomyDiagnostic[] = []
  if (value.version !== AUTONOMY_STATE_VERSION) issues.push(issue('autonomy', 'autonomy.invalid-version'))
  if (!Array.isArray(value.processing) || value.processing.length > AUTONOMY_LIMITS.processing) issues.push(issue('autonomy:processing', Array.isArray(value.processing) && value.processing.length > AUTONOMY_LIMITS.processing ? 'autonomy.processing-limit' : 'autonomy.invalid-processing'))
  const processing = Array.isArray(value.processing) ? value.processing : []
  if (new Set(processing.filter(record).map(item => String(item.personId))).size !== processing.length) issues.push(issue('autonomy:processing', 'autonomy.duplicate-id'))
  if (!processing.every(item => validProcessing(item, context))) issues.push(issue('autonomy:processing', 'autonomy.invalid-processing'))
  if (processing.some((item, index) => index > 0 && record(item) && record(processing[index - 1]) && String(processing[index - 1]!.personId) >= String(item.personId))) issues.push(issue('autonomy:processing', 'autonomy.noncanonical-order'))
  const expectedPeople = context.people.filter(person => person.life.status === 'living').map(person => person.id).sort(compare)
  if (!same(processing.filter(record).map(item => item.personId), expectedPeople)) issues.push(issue('autonomy:processing', 'autonomy.invalid-processing'))

  if (!Array.isArray(value.observations) || value.observations.length > AUTONOMY_LIMITS.observations) issues.push(issue('autonomy:observations', Array.isArray(value.observations) && value.observations.length > AUTONOMY_LIMITS.observations ? 'autonomy.observation-limit' : 'autonomy.invalid-observation'))
  const observations = Array.isArray(value.observations) ? value.observations : []
  if (new Set(observations.filter(record).map(item => String(item.id))).size !== observations.length) issues.push(issue('autonomy:observations', 'autonomy.duplicate-id'))
  if (!observations.every(item => validObservation(item, context))) issues.push(issue('autonomy:observations', 'autonomy.invalid-observation'))
  if (observations.some((item, index) => index > 0 && record(item) && record(observations[index - 1]) && String(observations[index - 1]!.id) >= String(item.id))) issues.push(issue('autonomy:observations', 'autonomy.noncanonical-order'))
  try {
    const contentAudit = auditMedievalContentSafety(autonomyContentRecords({ observations: observations as AutonomyObservation[] }))
    if (contentAudit.status === 'rejected') issues.push(...contentAudit.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
    if (contentAudit.status !== 'accepted' || !same(contentAudit, value.contentSafetyAudit)) issues.push(issue('autonomy:content-safety', 'autonomy.invalid-content-audit'))
  } catch { issues.push(issue('autonomy:content-safety', 'autonomy.invalid-content-audit')) }
  return canonicalIssues(issues)
}

/** Full-world validation additionally proves current observations equal the fidelity/catch-up plan. */
export const validateAutonomyPlanState = (context: AutonomyContext & { plan: FidelityPlan }, value: unknown): readonly AutonomyDiagnostic[] => {
  const issues = [...validateAutonomyState(context, value)]
  if (context.plan.worldId !== context.worldId || context.plan.creationDigest !== context.creationDigest || context.plan.worldTime !== context.worldTime || context.plan.activeCourier.personId !== context.activeCourierId) issues.push(issue('autonomy:plan', 'autonomy.invalid-plan-projection'))
  if (record(value) && Array.isArray(value.observations) && !same(value.observations, observationsForPlan(context))) issues.push(issue('autonomy:observations', 'autonomy.invalid-plan-projection'))
  return canonicalIssues(issues)
}

const countResidueThrough = (window: number, residue: number): number => {
  const first = residue === 0 ? 4 : residue
  return window < first ? 0 : Math.floor((window - first) / AUTONOMY_NEED_KEYS.length) + 1
}
const incrementsFor = (context: Pick<AutonomyContext, 'worldId' | 'creationDigest'>, personId: string, startWindow: number, endWindow: number): Readonly<Record<AutonomyNeedKey, number>> => {
  const offset = hashSeed(`jomon-autonomy-v${AUTONOMY_CONTRACT_VERSION}|${context.creationDigest}|${context.worldId}|${personId}|need-channel`) % AUTONOMY_NEED_KEYS.length
  return Object.fromEntries(AUTONOMY_NEED_KEYS.map((key, index) => {
    const residue = (index - offset + AUTONOMY_NEED_KEYS.length) % AUTONOMY_NEED_KEYS.length
    return [key, countResidueThrough(endWindow, residue) - countResidueThrough(startWindow, residue)]
  })) as Readonly<Record<AutonomyNeedKey, number>>
}

const foldNeeds = (context: Pick<AutonomyContext, 'worldId' | 'creationDigest'>, person: PersistentPersonRecord, startWindow: number, endWindow: number): PersistentPersonRecord => {
  const increments = incrementsFor(context, person.id, startWindow, endWindow)
  return {
    ...structuredClone(person),
    needs: {
      nourishment: Math.min(5, person.needs.nourishment + increments.nourishment),
      rest: Math.min(5, person.needs.rest + increments.rest),
      shelter: Math.min(5, person.needs.shelter + increments.shelter),
      safety: Math.min(5, person.needs.safety + increments.safety)
    }
  }
}

/** Replaces observations with the canonical current-plan projection without advancing time or needs. */
export const reconcileAutonomyState = (state: AutonomyState, context: AutonomyContext & { plan: FidelityPlan }): AutonomyState => {
  const structural = validateAutonomyState(context, state)
  if (structural.length) throw new AutonomyContractError(structural)
  const stateWithoutAudit = {
    version: AUTONOMY_STATE_VERSION,
    processing: structuredClone(state.processing),
    observations: observationsForPlan(context)
  } satisfies Omit<AutonomyState, 'contentSafetyAudit'>
  const next = { ...stateWithoutAudit, contentSafetyAudit: audit(stateWithoutAudit) }
  const diagnostics = validateAutonomyPlanState(context, next)
  if (diagnostics.length) throw new AutonomyContractError(diagnostics)
  return next
}

/**
 * Folds fixed need windows once per living person (and four channels), then
 * derives plan-owned observations. It is O(instantiated people), not elapsed
 * minutes, and produces the same result for any equivalent time partition.
 */
export const advanceAutonomyState = (
  state: AutonomyState,
  before: AutonomyContext & { plan: FidelityPlan },
  after: AutonomyContext & { plan: FidelityPlan },
  action: { startedAtWorldTime: number; atWorldTime: number }
): AutonomyTransition => {
  if (!safeInteger(action.startedAtWorldTime) || !safeInteger(action.atWorldTime) || action.startedAtWorldTime >= action.atWorldTime || before.worldTime !== action.startedAtWorldTime || after.worldTime !== action.atWorldTime) throw new AutonomyContractError([issue('autonomy:action', 'autonomy.invalid-action')])
  const beforeDiagnostics = validateAutonomyPlanState(before, state)
  if (beforeDiagnostics.length) throw new AutonomyContractError(beforeDiagnostics)
  const startWindow = needWindowsAt(action.startedAtWorldTime)
  const endWindow = needWindowsAt(action.atWorldTime)
  const people = after.people.map(person => person.life.status === 'living'
    ? foldNeeds(after, person, startWindow, endWindow)
    : structuredClone(person))
  const updatedContext: AutonomyContext & { plan: FidelityPlan } = { ...after, people }
  const processing = people.filter(person => person.life.status === 'living').map(person => ({ personId: person.id, processedNeedWindows: endWindow })).sort((left, right) => compare(left.personId, right.personId))
  const stateWithoutAudit = { version: AUTONOMY_STATE_VERSION, processing, observations: observationsForPlan(updatedContext) } satisfies Omit<AutonomyState, 'contentSafetyAudit'>
  const next = { ...stateWithoutAudit, contentSafetyAudit: audit(stateWithoutAudit) }
  const diagnostics = validateAutonomyPlanState(updatedContext, next)
  if (diagnostics.length) throw new AutonomyContractError(diagnostics)
  return { state: next, people }
}

/** Canonical projection for replay/partition comparisons; observations are already plan-canonical. */
export const autonomyProjection = (state: AutonomyState): AutonomyState => structuredClone(state)
