import { SeededRng, hashSeed } from './rng'
import { MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, auditMedievalContentSafety, classifyMedievalContent, contentSafetyAuditMatches, isMedievalContentSafetyAudit, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyDiagnostic } from './content-safety'
import { FRONTIER_CONTRACT_VERSION, createInitialFrontierState, frontierContentRecords, type FrontierState } from './frontier'
import { generationConfigurationFingerprint, generationRetryPlan, isReproducibleGenerationDiagnostics, resolveWorldGenerationConfig, type WorldGenerationConfig, type WorldGenerationConfigIssue, type WorldGenerationConfigRequest } from './generation-config'
import { INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION, INITIAL_WORLD_GENERATOR_VERSION, generateInitialWorld, initialWorldContentRecords, isInitialWorld, type InitialWorld, type InitialWorldGenerationDiagnostics, type InitialWorldGenerationProgressObserver } from './initial-world'
import { normalizeCreationSeed } from './settings'
import { advanceMedievalTemporalState, createMedievalTemporalState, isMedievalTemporalState, type TemporalCommand, type TemporalProvenance, type TimeBearingTemporalAction } from './temporal'
import { causalReplayProjectionForWorldState, createMedievalWorldState, isMedievalWorldState, type MedievalWorldState, type WorldAutonomyState, type WorldDelegationState, type WorldPeopleState, type WorldSocialMemoryState } from './world-state'
import { createFidelityPlanForVerifiedWorld } from './fidelity'
import { advanceSimulationCatchUpState, reconcileSimulationCatchUpPlanState, resolveDelegatedWorkPlaceholder, validateSimulationCatchUpPlanState, withDelegatedWorkPlaceholder } from './simulation-catchup'
import { advanceWorldEraForTemporalAction, recordDurableJomonGrowthEvidence, type DurableJomonGrowthEvidence, type WorldEraContext } from './world-era'
import { appendCausalCommand, causalReplayProjection, createCausalCommand, replayCausalHistory, validateCausalHistoryReplay, type CausalCommandEvent, type CausalHistoryContext, type CausalReplayProjection } from './causal-history'
import { CONVERSATION_CONTRACT_VERSION, assessCourierConversation, assessCourierConversationForValidatedReplay, type ConversationAssessment } from './conversation'
import { advanceDelegatedTasks, delegatedWorkPlaceholderForTask, delegationInterruptionTemporalAction, delegationOfferTemporalAction, interruptDelegatedTask, isDelegationInterruptionInput, isDelegationOfferInput, offerDelegatedTask as offerDelegationTransition, type DelegationInterruptionInput, type DelegationOfferInput } from './delegation'
import { advanceAutonomyState, createAutonomyState, reconcileAutonomyState, validateAutonomyPlanState } from './autonomy'
import { assessJomonDeckStep, canonicalJomonDeckSpawn, isWalkableJomonDeckCoordinate, jomonDeckCoordinateId, type JomonDeckCollision, type JomonDeckCoordinate, type JomonDeckMovementDirection } from './jomon-navigation'
import { FOUNDATION_GENERATOR_VERSION, FOUNDATION_MANIFEST_VERSION, WORLD_CREATION_PROVENANCE_VERSION, WORLD_MANIFEST_FRONTIER_PROVENANCE_VERSION, WORLD_MANIFEST_VALIDATION_HISTORY_VERSION, type CausalRecord, type ChronicleReason, type CrewRelationship, type CrewRole, type FoundationCrewMember, type FoundationJomon, type FoundationWorld, type FrontierManifestProvenance, type FrontierRootManifestIdentity, type InitialWorldManifestIdentity, type WorldChronicle, type WorldCreationProvenance, type WorldManifest } from './types'

const foundationJomon = (): FoundationJomon => ({
  id: 'vessel:jomon',
  name: 'Jomon',
  contentSafety: classifyMedievalContent('place', ['navigation', 'settlement'], 'not-applicable', ['player-facing-text']),
  deckPartitions: ['tavern', 'chart-table', 'cargo-hold', 'repair-space', 'stores', 'berths', 'galley', 'gangplank'],
  quays: [],
  props: [
    { id: 'prop:chart-table', kind: 'table', partition: 'chart-table' },
    { id: 'prop:task-ledger', kind: 'ledger', partition: 'tavern' },
    { id: 'prop:gangplank', kind: 'gangplank', partition: 'gangplank' }
  ]
})

const roles: readonly CrewRole[] = ['bargemaster', 'pilot', 'factor', 'carpenter', 'guard', 'cook', 'healer', 'scribe', 'carter', 'fisher', 'bard']
const nameStarts = ['Ari', 'Bel', 'Caro', 'Dara', 'Eren', 'Fara', 'Galen', 'Hara', 'Iven', 'Jori', 'Kesa', 'Loran', 'Mira', 'Neris', 'Oren', 'Pava', 'Risa', 'Soren', 'Tavi', 'Vela'] as const
const nameEnds = ['n', 'ra', 'en', 'a', 'is', 'or', 'et', 'i', 'an', 'el'] as const
const familyNames = ['Ash', 'Barrow', 'Cairn', 'Dike', 'Elm', 'Ford', 'Gull', 'Hearth', 'Ivy', 'Keel', 'Lark', 'Moss', 'Nettle', 'Pike', 'Quill', 'Reed', 'Silt', 'Thorn', 'Vale', 'Wren'] as const
const equipmentByRole: Readonly<Record<CrewRole, readonly string[]>> = {
  bargemaster: ['river pole', 'waxed chart'],
  pilot: ['lead line', 'signal whistle'],
  factor: ['ledger', 'seal case'],
  carpenter: ['adze', 'oakum roll'],
  guard: ['buckler', 'hooked staff'],
  cook: ['iron pot', 'spice pouch'],
  healer: ['bandage roll', 'herb case'],
  scribe: ['paper folio', 'ink horn'],
  carter: ['harness knife', 'load straps'],
  fisher: ['net needle', 'line spool'],
  bard: ['small lute', 'songbook']
}
const historyByRole: Readonly<Record<CrewRole, readonly string[]>> = {
  bargemaster: ['kept a flood-season barge from striking the lock gates', 'learned the river by carrying mill flour through fog'],
  pilot: ['mapped shoals for a ferry owner who never paid in full', 'served as a canal guide during a winter thaw'],
  factor: ['balanced a market house ledger after a failed harvest', 'carried sealed prices between rival quays'],
  carpenter: ['rebuilt a fishing skiff from storm-broken planks', 'worked a yard where every nail was counted'],
  guard: ['escorted grain carts through a disputed ford', 'kept watch on a night crossing during a toll dispute'],
  cook: ['fed a repair crew through a week of rain', 'learned preservation from a coastal smokehouse'],
  healer: ['treated rope burns and winter coughs at a ferry inn', 'kept a travelling medicine chest for river workers'],
  scribe: ['copied contracts for a waterside court', 'kept weather records for a merchant household'],
  carter: ['moved timber between wet roads and narrow quays', 'worked pack animals along an estuary causeway'],
  fisher: ['worked eel traps in a reed marsh', 'sailed a small net boat beyond the river mouth'],
  bard: ['collected work songs from lock crews', 'earned passage by keeping a crowded quay awake']
}
const relationshipBases: readonly CrewRelationship['basis'][] = ['kinship', 'work', 'debt', 'friendship', 'rivalry']

export interface FoundationWorldInput {
  seed?: string
  configuration?: WorldGenerationConfigRequest
  /** Actual initial-world generator progress for the local creation surface. */
  onGenerationProgress?: InitialWorldGenerationProgressObserver
}

export class InvalidWorldGenerationConfigurationError extends Error {
  constructor(readonly issues: readonly WorldGenerationConfigIssue[]) {
    super(`world generation configuration is invalid: ${issues.map(item => item.field).join(', ')}`)
    this.name = 'InvalidWorldGenerationConfigurationError'
  }
}

export class MedievalContentSafetyPolicyError extends Error {
  constructor(readonly diagnostics: readonly MedievalContentSafetyDiagnostic[]) {
    super(`generated medieval content violates policy: ${diagnostics.map(item => item.code).join(', ')}`)
    this.name = 'MedievalContentSafetyPolicyError'
  }
}

export const normalizeSeed = normalizeCreationSeed

const labelForSeed = (seed: string, configuration: WorldGenerationConfig): string => {
  const rng = new SeededRng(`label:${seed}:${generationConfigurationFingerprint(configuration)}`)
  return `${rng.pick(['Ash', 'Brackish', 'Candle', 'Drowned', 'Eel', 'Far', 'Grey', 'Hollow', 'Ivy', 'Low'])} ${rng.pick(['Basin', 'Current', 'Estuary', 'Ford', 'Mooring', 'Reach', 'Sound', 'Weir', 'Wick', 'Wold'])}`
}

const nameFor = (rng: SeededRng, used: Set<string>): string => {
  for (let attempts = 0; attempts < 100; attempts++) {
    const name = `${rng.pick(nameStarts)}${rng.pick(nameEnds)} ${rng.pick(familyNames)}`
    if (!used.has(name)) { used.add(name); return name }
  }
  throw new Error('could not generate a unique foundation crew name')
}

const relationship = (rng: SeededRng, personId: string): CrewRelationship => ({
  personId,
  standing: rng.pick([-2, -1, 0, 1, 2] as const),
  basis: rng.pick(relationshipBases)
})

const shuffled = <T>(rng: SeededRng, values: readonly T[]): T[] => {
  const next = [...values]
  for (let index = next.length - 1; index > 0; index--) {
    const swap = rng.integer(index + 1)
    const current = next[index]!
    next[index] = next[swap]!
    next[swap] = current
  }
  return next
}

const generateCrew = (seed: string, configuration: WorldGenerationConfig): readonly FoundationCrewMember[] => {
  const rng = new SeededRng(`crew:${seed}:${FOUNDATION_GENERATOR_VERSION}:${generationConfigurationFingerprint(configuration)}`)
  const usedNames = new Set<string>()
  const selectedRoles = shuffled(rng, roles).slice(0, 6)
  const crew = selectedRoles.map((role, index): FoundationCrewMember => ({
    id: `crew:${index}`,
    name: nameFor(rng, usedNames),
    contentSafety: classifyMedievalContent('person', ['adult-labour', 'travel'], 'adults-only', ['player-facing-text']),
    role,
    conversation: rng.between(1, 5),
    equipment: [...equipmentByRole[role]],
    history: rng.pick(historyByRole[role]),
    historyContentSafety: classifyMedievalContent('history', ['adult-labour', 'ordinary-hardship', 'travel'], 'adults-only', ['player-facing-text']),
    relationships: [],
    eligible: true
  }))
  return crew.map(member => ({ ...member, relationships: crew.filter(other => other.id !== member.id).map(other => relationship(rng, other.id)) }))
}

const worldCreatedRecord = (label: string, seed: string): CausalRecord => ({
  sequence: 0,
  atWorldTime: 0,
  kind: 'world-created',
  detail: `Foundation world ${label} created from seed ${seed}.`,
  contentSafety: classifyMedievalContent('event', ['civil-life', 'navigation'], 'not-applicable', ['player-facing-text'])
})

const foundationContentRecords = (
  labelContentSafety: WorldCreationProvenance['labelContentSafety'],
  jomon: FoundationJomon,
  crew: readonly FoundationCrewMember[],
  causalHistory: readonly CausalRecord[],
  initialWorld: InitialWorld,
  frontier: FrontierState
): readonly ClassifiedMedievalContent[] => [
  { id: 'world:label', domain: 'place', classification: labelContentSafety },
  { id: jomon.id, domain: 'place', classification: jomon.contentSafety },
  ...crew.flatMap(member => [
    { id: member.id, domain: 'person' as const, classification: member.contentSafety },
    { id: `${member.id}:history`, domain: 'history' as const, classification: member.historyContentSafety }
  ]),
  ...causalHistory.map(record => ({ id: `causal:${record.sequence}:${record.kind}`, domain: 'event' as const, classification: record.contentSafety })),
  ...initialWorldContentRecords(initialWorld),
  ...frontierContentRecords(frontier)
]

const auditFoundationContent = (
  labelContentSafety: WorldCreationProvenance['labelContentSafety'],
  jomon: FoundationJomon,
  crew: readonly FoundationCrewMember[],
  causalHistory: readonly CausalRecord[],
  initialWorld: InitialWorld,
  frontier: FrontierState
): MedievalContentSafetyAudit => {
  const audit = auditMedievalContentSafety(foundationContentRecords(labelContentSafety, jomon, crew, causalHistory, initialWorld, frontier))
  if (audit.status === 'rejected') throw new MedievalContentSafetyPolicyError(audit.diagnostics)
  return audit
}

interface ExpectedFoundationState {
  label: string
  labelContentSafety: WorldCreationProvenance['labelContentSafety']
  jomon: FoundationJomon
  crew: readonly FoundationCrewMember[]
  initialWorld: InitialWorld
  initialWorldGeneration: InitialWorldGenerationDiagnostics
  frontier: FrontierState
  causalHistory: readonly CausalRecord[]
  contentSafetyAudit: MedievalContentSafetyAudit
}

const expectedFoundationState = (
  seed: string,
  configuration: WorldGenerationConfig,
  onGenerationProgress?: InitialWorldGenerationProgressObserver
): ExpectedFoundationState | undefined => {
  const label = labelForSeed(seed, configuration)
  const labelContentSafety = classifyMedievalContent('place', ['environment', 'settlement'], 'not-applicable', ['player-facing-text'])
  const jomon = foundationJomon()
  const crew = generateCrew(seed, configuration)
  const initialWorldGeneration = generateInitialWorld(seed, configuration, onGenerationProgress)
  const frontier = createInitialFrontierState({ seed, configuration, initialWorld: initialWorldGeneration.world })
  const causalHistory = [worldCreatedRecord(label, seed)]
  return {
    label,
    labelContentSafety,
    jomon,
    crew,
    initialWorld: initialWorldGeneration.world,
    initialWorldGeneration: initialWorldGeneration.diagnostics,
    frontier,
    causalHistory,
    contentSafetyAudit: auditFoundationContent(labelContentSafety, jomon, crew, causalHistory, initialWorldGeneration.world, frontier)
  }
}

const canonicalJson = (value: unknown): string => {
  if (value === null) return 'null'
  if (typeof value === 'string' || typeof value === 'boolean') return JSON.stringify(value)
  if (typeof value === 'number') {
    if (!Number.isFinite(value)) throw new Error('canonical manifest values must be finite')
    return JSON.stringify(Object.is(value, -0) ? 0 : value)
  }
  if (Array.isArray(value)) return `[${value.map(canonicalJson).join(',')}]`
  if (!value || typeof value !== 'object' || (Object.getPrototypeOf(value) !== Object.prototype && Object.getPrototypeOf(value) !== null)) throw new Error('canonical manifest values must be serializable records')
  const record = value as Record<string, unknown>
  return `{${Object.keys(record).sort().map(key => `${JSON.stringify(key)}:${canonicalJson(record[key])}`).join(',')}}`
}

const equivalent = (left: unknown, right: unknown): boolean => {
  try { return canonicalJson(left) === canonicalJson(right) } catch { return false }
}

const digestFor = (scope: string, value: unknown): string => {
  const source = `${scope}|${canonicalJson(value)}`
  const forward = hashSeed(source).toString(36)
  const reverse = hashSeed([...source].reverse().join(''), 0x9e3779b9).toString(36)
  return `${forward}-${reverse}`
}

const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const sortedExpected = [...expected].sort()
  return keys.length === sortedExpected.length && keys.every((key, index) => key === sortedExpected[index])
}

const initialWorldIdentityFor = (world: InitialWorld): InitialWorldManifestIdentity => ({
  id: world.id,
  candidateAttempt: world.candidateAttempt,
  configurationFingerprint: world.configurationFingerprint,
  digest: digestFor('initial-world', world),
  watershedId: world.watershed.id,
  waterwayIds: world.waterways.map(record => record.id),
  climateId: world.climate.id,
  seasonIds: world.seasons.map(record => record.id),
  resourceIds: world.resources.map(record => record.id),
  ecologyIds: world.ecologies.map(record => record.id),
  settlementIds: world.settlements.map(record => record.id),
  institutionIds: world.institutions.map(record => record.id),
  personIds: world.people.map(record => record.id),
  routeIds: world.routes.map(record => record.id),
  tradeLinkIds: world.tradeLinks.map(record => record.id),
  routeHazardIds: world.routeHazards.map(record => record.id),
  historyEventIds: world.history.map(record => record.id)
})

const frontierRootIdentityFor = (frontier: FrontierState): readonly FrontierRootManifestIdentity[] => frontier.regions
  .map(region => region.commitment)
  .sort((left, right) => left.generationOrder - right.generationOrder || left.id.localeCompare(right.id))
  .map(commitment => ({
    id: commitment.id,
    coordinate: { ...commitment.coordinate },
    kind: commitment.kind,
    generationOrder: commitment.generationOrder,
    generatorStream: commitment.generatorStream,
    anchor: { ...commitment.anchor },
    connection: { ...commitment.connection }
  }))

const frontierProvenanceFor = (frontier: FrontierState): FrontierManifestProvenance => {
  const roots = frontierRootIdentityFor(frontier)
  const draft = {
    version: WORLD_MANIFEST_FRONTIER_PROVENANCE_VERSION,
    contractVersion: FRONTIER_CONTRACT_VERSION,
    initialWorldId: frontier.provenance.initialWorldId,
    roots
  }
  return { ...draft, digest: digestFor('frontier-roots', draft) }
}

const creationProvenanceFor = (
  seed: string,
  selectedConfiguration: WorldCreationProvenance['selectedConfiguration'],
  resolvedConfiguration: WorldGenerationConfig,
  state: ExpectedFoundationState
): WorldCreationProvenance => {
  const draft: Omit<WorldCreationProvenance, 'digest'> = {
    version: WORLD_CREATION_PROVENANCE_VERSION,
    seed,
    selectedConfiguration,
    resolvedConfiguration,
    configurationFingerprint: generationConfigurationFingerprint(resolvedConfiguration),
    contractVersions: {
      foundationGenerator: FOUNDATION_GENERATOR_VERSION,
      initialWorldGenerator: INITIAL_WORLD_GENERATOR_VERSION,
      initialWorldDiagnostics: INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION,
      frontierContract: FRONTIER_CONTRACT_VERSION,
      contentSafetyPolicy: MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION
    },
    validationHistory: {
      version: WORLD_MANIFEST_VALIDATION_HISTORY_VERSION,
      generation: { validation: { status: 'accepted', issues: [] }, retryPlan: generationRetryPlan(seed, resolvedConfiguration) },
      initialWorld: state.initialWorldGeneration,
      frontier: { status: 'accepted', issues: [] }
    },
    initialWorld: initialWorldIdentityFor(state.initialWorld),
    frontier: frontierProvenanceFor(state.frontier),
    label: state.label,
    labelContentSafety: state.labelContentSafety,
    contentSafetyAudit: state.contentSafetyAudit
  }
  return { ...draft, digest: digestFor('world-creation', draft) }
}

const idForCreationProvenance = (creation: WorldCreationProvenance): string => `world:${creation.digest}`

const temporalProvenanceForCreation = (creation: WorldCreationProvenance): TemporalProvenance => ({
  version: 1,
  worldId: idForCreationProvenance(creation),
  creationDigest: creation.digest,
  seed: creation.seed
})

const expectedCreationProvenance = (seed: string, selectedConfiguration: WorldCreationProvenance['selectedConfiguration'], resolvedConfiguration: WorldGenerationConfig): WorldCreationProvenance | undefined => {
  const state = expectedFoundationState(seed, resolvedConfiguration)
  return state === undefined ? undefined : creationProvenanceFor(seed, selectedConfiguration, resolvedConfiguration, state)
}

const cloneWorld = (world: FoundationWorld): FoundationWorld => structuredClone(world)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)

export type FoundationWorldValidationCode =
  | 'foundation-world.malformed-record'
  | 'foundation-world.invalid-version'
  | 'foundation-world.invalid-id'
  | 'foundation-world.invalid-status'
  | 'foundation-world.invalid-manifest'
  | 'foundation-world.invalid-initial-world'
  | 'foundation-world.invalid-immutable-content'
  | 'foundation-world.invalid-mutable-state'
  | 'foundation-world.invalid-catch-up'
  | 'foundation-world.invalid-autonomy'
  | 'foundation-world.invalid-navigation'
  | 'foundation-world.invalid-causal-history'

export interface FoundationWorldValidationIssue {
  code: FoundationWorldValidationCode
  recordId: string
}

const foundationWorldIssue = (recordId: string, code: FoundationWorldValidationCode): FoundationWorldValidationIssue => ({ recordId, code })

/**
 * One fail-closed boundary for repository reads and public mutable-world
 * transitions. Keeping this evidence together prevents action-time checks
 * from accepting a world that local persistence would reject.
 */
export const validateFoundationWorld = (value: unknown): readonly FoundationWorldValidationIssue[] => {
  if (!record(value)) return [foundationWorldIssue('foundation-world', 'foundation-world.malformed-record')]
  if (!hasOnlyKeys(value, ['version', 'id', 'status', 'manifest', 'jomon', 'crew', 'initialWorld', 'state'])) return [foundationWorldIssue('foundation-world', 'foundation-world.malformed-record')]
  const issues: FoundationWorldValidationIssue[] = []
  if (value.version !== 14) issues.push(foundationWorldIssue('foundation-world', 'foundation-world.invalid-version'))
  if (typeof value.id !== 'string' || !value.id) issues.push(foundationWorldIssue('foundation-world', 'foundation-world.invalid-id'))
  if (value.status !== 'active') issues.push(foundationWorldIssue('foundation-world', 'foundation-world.invalid-status'))
  if (!isReproducibleWorldManifest(value.manifest)) {
    issues.push(foundationWorldIssue('foundation-world:manifest', 'foundation-world.invalid-manifest'))
    return issues
  }
  const world = value as unknown as FoundationWorld
  if (world.id !== foundationWorldIdForManifest(world.manifest)) issues.push(foundationWorldIssue('foundation-world:id', 'foundation-world.invalid-id'))
  if (!isInitialWorld(world.initialWorld)) issues.push(foundationWorldIssue('foundation-world:initial-world', 'foundation-world.invalid-initial-world'))
  try {
    if (!foundationWorldInitialWorldMatchesManifest(world)) issues.push(foundationWorldIssue('foundation-world:initial-world', 'foundation-world.invalid-initial-world'))
    if (!foundationWorldContentSatisfiesSafetyPolicy(world)) issues.push(foundationWorldIssue('foundation-world:immutable-content', 'foundation-world.invalid-immutable-content'))
    if (!foundationWorldTemporalStateMatches(world)) issues.push(foundationWorldIssue('foundation-world:mutable-state', 'foundation-world.invalid-mutable-state'))
    if (!foundationWorldNavigationStateMatches(world)) issues.push(foundationWorldIssue('foundation-world:navigation', 'foundation-world.invalid-navigation'))
    if (!foundationWorldCatchUpStateMatches(world)) issues.push(foundationWorldIssue('foundation-world:catch-up', 'foundation-world.invalid-catch-up'))
    if (!foundationWorldAutonomyStateMatches(world)) issues.push(foundationWorldIssue('foundation-world:autonomy', 'foundation-world.invalid-autonomy'))
    if (!foundationWorldCausalHistoryMatches(world)) issues.push(foundationWorldIssue('foundation-world:causal-history', 'foundation-world.invalid-causal-history'))
  } catch {
    issues.push(foundationWorldIssue('foundation-world', 'foundation-world.invalid-mutable-state'))
  }
  return issues
}

export const isValidFoundationWorld = (value: unknown): value is FoundationWorld => validateFoundationWorld(value).length === 0

export const foundationWorldIdForManifest = (manifest: WorldManifest): string => idForCreationProvenance(manifest.creation)

/** Stable JSON for sharing/export; it refuses a manifest outside the current contract. */
export const serializeWorldManifest = (manifest: WorldManifest): string => {
  if (!isReproducibleWorldManifest(manifest)) throw new Error('world manifest does not reproduce the current medieval generation contract')
  return canonicalJson(manifest)
}

export const isReproducibleWorldManifest = (value: unknown): value is WorldManifest => {
  if (!record(value) || value.version !== FOUNDATION_MANIFEST_VERSION || !hasOnlyKeys(value, ['version', 'creation']) || !record(value.creation)) return false
  const creation = value.creation
  if (creation.version !== WORLD_CREATION_PROVENANCE_VERSION || typeof creation.seed !== 'string' || !creation.seed || normalizeSeed(creation.seed) !== creation.seed || typeof creation.configurationFingerprint !== 'string' || !record(creation.validationHistory) || creation.validationHistory.version !== WORLD_MANIFEST_VALIDATION_HISTORY_VERSION || !isReproducibleGenerationDiagnostics(creation.seed, creation.selectedConfiguration, creation.resolvedConfiguration, creation.validationHistory.generation)) return false
  const resolvedConfiguration = creation.resolvedConfiguration as WorldGenerationConfig
  if (creation.configurationFingerprint !== generationConfigurationFingerprint(resolvedConfiguration)) return false
  const contractVersions = creation.contractVersions
  if (!record(contractVersions) || contractVersions.foundationGenerator !== FOUNDATION_GENERATOR_VERSION || contractVersions.initialWorldGenerator !== INITIAL_WORLD_GENERATOR_VERSION || contractVersions.initialWorldDiagnostics !== INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION || contractVersions.frontierContract !== FRONTIER_CONTRACT_VERSION || contractVersions.contentSafetyPolicy !== MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION || !isMedievalContentSafetyAudit(creation.contentSafetyAudit)) return false
  const expectedCreation = expectedCreationProvenance(creation.seed, creation.selectedConfiguration as WorldCreationProvenance['selectedConfiguration'], resolvedConfiguration)
  return expectedCreation !== undefined && equivalent(creation, expectedCreation)
}

const worldFromCreationProvenance = (
  creation: WorldCreationProvenance,
  precomputedState?: ExpectedFoundationState
): FoundationWorld => {
  const state = precomputedState ?? expectedFoundationState(creation.seed, creation.resolvedConfiguration)
  if (!state) throw new Error('creation provenance does not identify a valid foundation world')
  const temporal = createMedievalTemporalState(temporalProvenanceForCreation(creation))
  return {
    version: 14,
    id: idForCreationProvenance(creation),
    status: 'active',
    manifest: {
      version: FOUNDATION_MANIFEST_VERSION,
      creation
    },
    jomon: state.jomon,
    crew: state.crew,
    initialWorld: state.initialWorld,
    state: createMedievalWorldState({
      seed: creation.seed,
      configuration: creation.resolvedConfiguration,
      initialWorld: state.initialWorld,
      jomon: state.jomon,
      crew: state.crew,
      frontier: state.frontier,
      temporal
    })
  }
}

export const createFoundationWorld = (input: FoundationWorldInput = {}): FoundationWorld => {
  const seed = normalizeSeed(input.seed)
  const configurationResolution = resolveWorldGenerationConfig(input.configuration)
  if (configurationResolution.status !== 'valid') throw new InvalidWorldGenerationConfigurationError(configurationResolution.issues)
  const state = expectedFoundationState(seed, configurationResolution.configuration, input.onGenerationProgress)
  if (!state) throw new Error('foundation world creation requires an unselected courier state')
  const creation = creationProvenanceFor(seed, configurationResolution.selectedConfiguration, configurationResolution.configuration, state)
  return worldFromCreationProvenance(creation, state)
}

export const recreateFoundationWorld = (manifest: WorldManifest): FoundationWorld => {
  if (!isReproducibleWorldManifest(manifest)) throw new Error('world manifest does not reproduce the current medieval generation contract')
  return worldFromCreationProvenance(manifest.creation)
}

/** Storage uses this after structural validation so saved worlds cannot bypass the policy. */
export const foundationWorldContentSatisfiesSafetyPolicy = (world: FoundationWorld): boolean => {
  try {
    const staticState = expectedFoundationState(world.manifest.creation.seed, world.manifest.creation.resolvedConfiguration)
    if (!staticState) return false
    return equivalent(world.jomon, staticState.jomon)
      && equivalent(world.crew, staticState.crew)
      && contentSafetyAuditMatches(foundationContentRecords(world.manifest.creation.labelContentSafety, world.jomon, world.crew, staticState.causalHistory, world.initialWorld, staticState.frontier), world.manifest.creation.contentSafetyAudit)
  } catch { return false }
}

/** Storage uses this to reject a modified region even when its tags still look safe. */
export const foundationWorldInitialWorldMatchesManifest = (world: FoundationWorld): boolean => {
  try {
    const expected = generateInitialWorld(world.manifest.creation.seed, world.manifest.creation.resolvedConfiguration)
    return equivalent(world.initialWorld, expected.world) && equivalent(world.manifest.creation.initialWorld, initialWorldIdentityFor(expected.world)) && equivalent(world.manifest.creation.validationHistory.initialWorld, expected.diagnostics)
  } catch { return false }
}

/** The mutable scheduler is bound to immutable creation evidence but never modifies it. */
export const foundationWorldTemporalStateMatches = (world: FoundationWorld): boolean => {
  try {
    const provenance = temporalProvenanceForCreation(world.manifest.creation)
    if (!isMedievalTemporalState(world.state.temporal, provenance)) return false
    return isMedievalWorldState({
      seed: world.manifest.creation.seed,
      configuration: world.manifest.creation.resolvedConfiguration,
      initialWorld: world.initialWorld,
      jomon: world.jomon,
      crew: world.crew
    }, world.state)
  } catch { return false }
}

/** The mutable coordinate belongs to state, while valid cells remain plan-derived. */
export const foundationWorldNavigationStateMatches = (world: FoundationWorld): boolean => {
  const courierId = world.state.courier.initialCourierId
  const navigation = world.state.navigation
  if (courierId === undefined) return navigation.courierId === undefined && navigation.coordinate === undefined
  return navigation.courierId === courierId
    && navigation.coordinate !== undefined
    && isWalkableJomonDeckCoordinate(world, navigation.coordinate)
}

/**
 * Catch-up is a separate proof from temporal shape: it must account for each
 * canonical cadence window implied by the current deterministic fidelity plan.
 */
export const foundationWorldCatchUpStateMatches = (world: FoundationWorld): boolean => {
  try {
    const courierId = world.state.courier.initialCourierId
    if (courierId === undefined) {
      return world.state.simulation.cursors.length === 0
        && world.state.simulation.delegatedWork.length === 0
        && world.state.simulation.records.length === 0
    }
    const plan = createFidelityPlanForVerifiedWorld(world, courierId)
    return validateSimulationCatchUpPlanState(world.state.simulation, plan).length === 0
  } catch { return false }
}

/** Autonomy observations are a canonical view of current fidelity/catch-up state. */
export const foundationWorldAutonomyStateMatches = (world: FoundationWorld): boolean => {
  try {
    const courierId = world.state.courier.initialCourierId
    if (courierId === undefined) return world.state.autonomy.observations.length === 0
    const plan = createFidelityPlanForVerifiedWorld(world, courierId)
    return validateAutonomyPlanState({
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: world.state.temporal.worldTime,
      activeCourierId: courierId,
      people: world.state.people.records,
      delegation: world.state.delegation,
      era: world.state.era,
      simulation: world.state.simulation,
      plan
    }, world.state.autonomy).length === 0
  } catch { return false }
}

const worldEraContextFor = (world: FoundationWorld, worldTime = world.state.temporal.worldTime): WorldEraContext => ({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  eraPace: world.manifest.creation.resolvedConfiguration.eraPace,
  worldTime,
  jomonVesselId: world.jomon.id,
  jomonPropIds: world.jomon.props.map(prop => prop.id)
})

const causalHistoryContextFor = (world: FoundationWorld): CausalHistoryContext => ({ worldId: world.id, creationDigest: world.manifest.creation.digest })

const stateFromProjection = (world: FoundationWorld, projection: CausalReplayProjection, causalHistoryState = world.state.causalHistory): MedievalWorldState => createMedievalWorldState({
  seed: world.manifest.creation.seed,
  configuration: world.manifest.creation.resolvedConfiguration,
  initialWorld: world.initialWorld,
  jomon: world.jomon,
  crew: world.crew,
  frontier: world.state.geography.frontier,
  temporal: projection.temporal,
  ...(projection.courier.initialCourierId === undefined ? {} : { initialCourierId: projection.courier.initialCourierId }),
  ...(projection.navigation === undefined ? {} : { navigationState: projection.navigation }),
  jomonState: world.state.jomon,
  peopleState: projection.people as WorldPeopleState,
  simulationState: projection.simulation,
  eraState: projection.era,
  delegationState: projection.delegation as WorldDelegationState,
  autonomyState: projection.autonomy as WorldAutonomyState,
  socialMemoryState: projection.socialMemory as WorldSocialMemoryState,
  causalHistoryState
})

const worldFromProjection = (world: FoundationWorld, projection: CausalReplayProjection, causalHistoryState = world.state.causalHistory): FoundationWorld => ({
  ...cloneWorld(world),
  state: stateFromProjection(world, projection, causalHistoryState)
})

/** The shared pure reducer for an initial-courier journal command. */
const selectCourierProjection = (world: FoundationWorld, projection: CausalReplayProjection, courierId: string): CausalReplayProjection => {
  if (projection.courier.initialCourierId !== undefined) throw new Error('initial courier has already been selected')
  if (projection.temporal.worldTime !== 0 || projection.temporal.actionSequence !== 0 || projection.temporal.pendingEvents.length !== 0) throw new Error('initial courier must be selected before time-bearing actions')
  const candidate = world.crew.find(member => member.id === courierId)
  const person = world.state.people.records.find(candidatePerson => candidatePerson.id === courierId)
  if (!candidate?.eligible || person?.life.status !== 'living' || person.work.availability !== 'available') throw new Error('selected courier must be an eligible living available crew member')
  return causalReplayProjection({ ...projection, courier: { version: 1, initialCourierId: courierId }, navigation: { version: 1, courierId, coordinate: canonicalJomonDeckSpawn(world) } })
}

/** The shared pure reducer for an accepted time-bearing journal command. */
const advanceTimeProjection = (world: FoundationWorld, projection: CausalReplayProjection, command: TemporalCommand | unknown): CausalReplayProjection => {
  const transition = advanceMedievalTemporalState(projection.temporal, command)
  const eraPlan = advanceWorldEraForTemporalAction(projection.era, worldEraContextFor(world, transition.state.worldTime), transition.action)
  // Fidelity is selected from the valid state at the action boundary. The
  // reducer then folds its windows through the action; task completion and
  // any resulting tier change are reconciled at the action's end below.
  const beforeAction = worldFromProjection(world, projection)
  const courierId = beforeAction.state.courier.initialCourierId
  if (courierId === undefined) throw new Error('time-bearing simulation requires an active courier')
  const beforePlan = createFidelityPlanForVerifiedWorld(beforeAction, courierId)
  const catchUp = advanceSimulationCatchUpState(projection.simulation, beforePlan, transition.action)
  const delegation = advanceDelegatedTasks({
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: transition.state.worldTime,
    people: projection.people.records
  }, projection.delegation, projection.people.records, projection.socialMemory, transition.action)
  const resolvedSimulation = delegation.resolvedTaskIds.reduce((state, taskId) => {
    const task = delegation.state.tasks.find(candidate => candidate.id === taskId)!
    return resolveDelegatedWorkPlaceholder(state, {
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: transition.state.worldTime,
      personIds: delegation.people.map(person => person.id),
      marketIds: world.state.markets.markets.map(market => market.id),
      institutionIds: world.state.institutions.registry.map(institution => institution.id)
    }, taskId, task.outcome!.atWorldTime)
  }, catchUp.state)
  const finalProjection = causalReplayProjection({
    courier: projection.courier,
    ...(projection.navigation === undefined ? {} : { navigation: projection.navigation }),
    people: { version: 5, records: delegation.people },
    temporal: transition.state,
    simulation: resolvedSimulation,
    era: eraPlan,
    delegation: delegation.state,
    // This transient planning view must carry end-time processing cursors;
    // the real reducer below folds the prior autonomy state before return.
    autonomy: createAutonomyState(delegation.people, transition.state.worldTime),
    socialMemory: delegation.socialMemory
  })
  const finalWorld = worldFromProjection(world, finalProjection)
  const simulation = reconcileSimulationCatchUpPlanState(
    resolvedSimulation,
    createFidelityPlanForVerifiedWorld(finalWorld, courierId)
  )
  const autonomyWorld = worldFromProjection(world, causalReplayProjection({ ...finalProjection, simulation }))
  const finalPlan = createFidelityPlanForVerifiedWorld(autonomyWorld, courierId)
  const autonomy = advanceAutonomyState(
    projection.autonomy,
    {
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: transition.action.startedAtWorldTime,
      activeCourierId: courierId,
      people: projection.people.records,
      delegation: projection.delegation,
      era: projection.era,
      simulation: projection.simulation,
      plan: beforePlan
    },
    {
      worldId: world.id,
      creationDigest: world.manifest.creation.digest,
      worldTime: transition.action.atWorldTime,
      activeCourierId: courierId,
      people: delegation.people,
      delegation: delegation.state,
      era: eraPlan,
      simulation,
      plan: finalPlan
    },
    transition.action
  )
  return causalReplayProjection({
    courier: projection.courier,
    ...(projection.navigation === undefined ? {} : { navigation: projection.navigation }),
    people: { version: 5, records: autonomy.people },
    temporal: transition.state,
    simulation,
    era: eraPlan,
    delegation: delegation.state,
    autonomy: autonomy.state,
    socialMemory: delegation.socialMemory
  })
}

/** The shared pure reducer for a completed physical Jomon-growth command. */
const recordGrowthProjection = (world: FoundationWorld, projection: CausalReplayProjection, evidence: DurableJomonGrowthEvidence): CausalReplayProjection => {
  const era = recordDurableJomonGrowthEvidence(projection.era, worldEraContextFor(world, projection.temporal.worldTime), evidence)
  const advanced = causalReplayProjection({ ...projection, era })
  const courierId = advanced.courier.initialCourierId
  if (courierId === undefined) return advanced
  // An era profile is an input to choice explanation, so zero-time durable
  // growth reprojects observations without advancing needs or scheduling work.
  const advancedWorld = worldFromProjection(world, advanced)
  const plan = createFidelityPlanForVerifiedWorld(advancedWorld, courierId)
  const autonomy = reconcileAutonomyState(advanced.autonomy, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: advanced.temporal.worldTime,
    activeCourierId: courierId,
    people: advanced.people.records,
    delegation: advanced.delegation,
    era,
    simulation: advanced.simulation,
    plan
  })
  return causalReplayProjection({ ...advanced, autonomy })
}

/** Shared pure reducer for a successful one-minute local deck step. */
const moveDeckProjection = (world: FoundationWorld, projection: CausalReplayProjection, payload: { actionId: string; courierId: string; direction: JomonDeckMovementDirection; from: JomonDeckCoordinate; to: JomonDeckCoordinate }): CausalReplayProjection => {
  const navigation = projection.navigation
  if (!navigation?.coordinate || navigation.courierId !== payload.courierId || projection.courier.initialCourierId !== payload.courierId) throw new Error('deck movement requires the active courier position')
  if (navigation.coordinate.column !== payload.from.column || navigation.coordinate.row !== payload.from.row) throw new Error('deck movement source position does not match replay state')
  const assessment = assessJomonDeckStep(world, navigation.coordinate, payload.direction)
  if (assessment.status !== 'moved' || assessment.to.column !== payload.to.column || assessment.to.row !== payload.to.row) throw new Error('deck movement is not a valid plan step')
  const advanced = advanceTimeProjection(world, projection, {
    id: payload.actionId,
    kind: 'movement',
    durationMinutes: 1,
    contentSafety: classifyMedievalContent('event', ['adult-labour', 'navigation'], 'adults-only', ['data'])
  })
  return causalReplayProjection({ ...advanced, navigation: { version: 1, courierId: payload.courierId, coordinate: structuredClone(payload.to) } })
}

const delegationContextFor = (world: FoundationWorld, projection: CausalReplayProjection): {
  worldId: string
  creationDigest: string
  worldTime: number
  people: WorldPeopleState['records']
} => ({
  worldId: world.id,
  creationDigest: world.manifest.creation.digest,
  worldTime: projection.temporal.worldTime,
  people: projection.people.records
})

/** Shared pure offer reducer: conversation gates the one-minute commitment action. */
const offerDelegationProjection = (world: FoundationWorld, projection: CausalReplayProjection, offer: DelegationOfferInput | unknown, replay = false): CausalReplayProjection => {
  if (!isDelegationOfferInput(offer)) throw new Error('delegation offer is invalid')
  const beforeOffer = worldFromProjection(world, projection)
  const assessmentRequest = {
    version: CONVERSATION_CONTRACT_VERSION,
    courierId: offer.courierId,
    recipientId: offer.recipientId,
    proposal: offer.proposal
  } as const
  const assessment: ConversationAssessment = replay
    ? assessCourierConversationForValidatedReplay(beforeOffer, assessmentRequest)
    : assessCourierConversation(beforeOffer, assessmentRequest)
  const afterAction = advanceTimeProjection(world, projection, delegationOfferTemporalAction(offer))
  const transition = offerDelegationTransition(delegationContextFor(world, afterAction), afterAction.delegation, afterAction.people.records, afterAction.socialMemory, offer, assessment)
  const simulation = transition.task.status === 'in-progress'
    ? withDelegatedWorkPlaceholder(afterAction.simulation, {
        worldId: world.id,
        creationDigest: world.manifest.creation.digest,
        worldTime: afterAction.temporal.worldTime,
        personIds: transition.people.map(person => person.id),
        marketIds: world.state.markets.markets.map(market => market.id),
        institutionIds: world.state.institutions.registry.map(institution => institution.id)
      }, delegatedWorkPlaceholderForTask(transition.task))
    : afterAction.simulation
  const offeredProjection = causalReplayProjection({
    ...afterAction,
    people: { version: 5, records: transition.people },
    simulation,
    delegation: transition.state,
    socialMemory: transition.socialMemory
  })
  const offeredWorld = worldFromProjection(world, offeredProjection)
  const reconciledSimulation = reconcileSimulationCatchUpPlanState(simulation, createFidelityPlanForVerifiedWorld(offeredWorld, offer.courierId))
  const autonomyWorld = worldFromProjection(world, causalReplayProjection({ ...offeredProjection, simulation: reconciledSimulation }))
  const plan = createFidelityPlanForVerifiedWorld(autonomyWorld, offer.courierId)
  const autonomy = reconcileAutonomyState(offeredProjection.autonomy, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: offeredProjection.temporal.worldTime,
    activeCourierId: offer.courierId,
    people: transition.people,
    delegation: transition.state,
    era: offeredProjection.era,
    simulation: reconciledSimulation,
    plan
  })
  return causalReplayProjection({ ...offeredProjection, simulation: reconciledSimulation, autonomy })
}

/** Shared pure interruption reducer. Completion wins if its due minute is crossed. */
const interruptDelegationProjection = (world: FoundationWorld, projection: CausalReplayProjection, interruption: DelegationInterruptionInput | unknown): CausalReplayProjection => {
  if (!isDelegationInterruptionInput(interruption)) throw new Error('delegation interruption is invalid')
  if (projection.courier.initialCourierId !== interruption.courierId) throw new Error('only the active courier can interrupt delegated work')
  const afterAction = advanceTimeProjection(world, projection, delegationInterruptionTemporalAction(interruption))
  const transition = interruptDelegatedTask(delegationContextFor(world, afterAction), afterAction.delegation, afterAction.people.records, afterAction.socialMemory, interruption)
  const task = transition.state.tasks.find(candidate => candidate.id === interruption.taskId)!
  const simulation = resolveDelegatedWorkPlaceholder(afterAction.simulation, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: afterAction.temporal.worldTime,
    personIds: transition.people.map(person => person.id),
    marketIds: world.state.markets.markets.map(market => market.id),
    institutionIds: world.state.institutions.registry.map(institution => institution.id)
  }, task.id, task.outcome!.atWorldTime)
  const interruptedProjection = causalReplayProjection({
    ...afterAction,
    people: { version: 5, records: transition.people },
    simulation,
    delegation: transition.state,
    socialMemory: transition.socialMemory
  })
  const interruptedWorld = worldFromProjection(world, interruptedProjection)
  const reconciledSimulation = reconcileSimulationCatchUpPlanState(simulation, createFidelityPlanForVerifiedWorld(interruptedWorld, interruption.courierId))
  const autonomyWorld = worldFromProjection(world, causalReplayProjection({ ...interruptedProjection, simulation: reconciledSimulation }))
  const plan = createFidelityPlanForVerifiedWorld(autonomyWorld, interruption.courierId)
  const autonomy = reconcileAutonomyState(interruptedProjection.autonomy, {
    worldId: world.id,
    creationDigest: world.manifest.creation.digest,
    worldTime: interruptedProjection.temporal.worldTime,
    activeCourierId: interruption.courierId,
    people: transition.people,
    delegation: transition.state,
    era: interruptedProjection.era,
    simulation: reconciledSimulation,
    plan
  })
  return causalReplayProjection({ ...interruptedProjection, simulation: reconciledSimulation, autonomy })
}

const replayCommandProjection = (world: FoundationWorld, projection: CausalReplayProjection, command: CausalCommandEvent): CausalReplayProjection => {
  if (command.kind === 'initial-courier-selected') return selectCourierProjection(world, projection, command.payload.courierId)
  if (command.kind === 'time-bearing-action') return advanceTimeProjection(world, projection, command.payload.action)
  if (command.kind === 'deck-moved') return moveDeckProjection(world, projection, command.payload)
  if (command.kind === 'durable-jomon-growth') return recordGrowthProjection(world, projection, command.payload.evidence)
  if (command.kind === 'delegation-offered') return offerDelegationProjection(world, projection, command.payload.offer, true)
  return interruptDelegationProjection(world, projection, command.payload.interruption)
}

/** Replays the authoritative journal through internal pure reducers for inspection/tests. */
export const replayFoundationWorldCausalHistory = (world: FoundationWorld): CausalReplayProjection => replayCausalHistory(
  causalHistoryContextFor(world),
  world.state.causalHistory,
  (projection, command) => replayCommandProjection(world, projection, command)
)

/**
 * Proves that the global journal reproduces the command-owned present state.
 * A sequence-zero checkpoint is additionally bound to immutable world genesis.
 */
export const foundationWorldCausalHistoryMatches = (world: FoundationWorld): boolean => {
  try {
    const context = causalHistoryContextFor(world)
    if (world.state.causalHistory.checkpoint.sequence === 0) {
      const genesisTemporal = createMedievalTemporalState(temporalProvenanceForCreation(world.manifest.creation))
      const genesis = createMedievalWorldState({
        seed: world.manifest.creation.seed,
        configuration: world.manifest.creation.resolvedConfiguration,
        initialWorld: world.initialWorld,
        jomon: world.jomon,
        crew: world.crew,
        frontier: world.state.geography.frontier,
        temporal: genesisTemporal
      })
      if (!equivalent(world.state.causalHistory.checkpoint.projection, causalReplayProjectionForWorldState(genesis))) return false
    }
    return validateCausalHistoryReplay(context, world.state.causalHistory, causalReplayProjectionForWorldState(world.state), (projection, command) => replayCommandProjection(world, projection, command)).length === 0
  } catch { return false }
}

export const chooseInitialCourier = (world: FoundationWorld, courierId: string): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can select an initial courier')
  const projection = selectCourierProjection(world, causalReplayProjectionForWorldState(world.state), courierId)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'initial-courier-selected', { courierId })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return worldFromProjection(world, projection, history)
}

export type FoundationDeckMovementResult =
  | { status: 'moved'; world: FoundationWorld; direction: JomonDeckMovementDirection; from: JomonDeckCoordinate; to: JomonDeckCoordinate }
  | { status: 'blocked'; direction: JomonDeckMovementDirection; from: JomonDeckCoordinate; collision: JomonDeckCollision }

/** Attempts one canonical local deck move. Blocked results intentionally have no world write. */
export const moveFoundationWorldCourier = (world: FoundationWorld, direction: JomonDeckMovementDirection): FoundationDeckMovementResult => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can move its courier')
  const courierId = world.state.courier.initialCourierId
  const navigation = world.state.navigation
  if (courierId === undefined || navigation.courierId !== courierId || navigation.coordinate === undefined) throw new Error('an active courier with a deck coordinate is required')
  const assessment = assessJomonDeckStep(world, navigation.coordinate, direction)
  if (assessment.status === 'blocked') return assessment
  const nextSequence = world.state.causalHistory.checkpoint.sequence + world.state.causalHistory.tail.length + 1
  const payload = {
    actionId: `deck-move:${nextSequence}:${courierId}:${jomonDeckCoordinateId(assessment.from)}:${jomonDeckCoordinateId(assessment.to)}`,
    courierId,
    direction,
    from: assessment.from,
    to: assessment.to
  }
  const projection = moveDeckProjection(world, causalReplayProjectionForWorldState(world.state), payload)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'deck-moved', payload)
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return { status: 'moved', world: worldFromProjection(world, projection, history), direction, from: assessment.from, to: assessment.to }
}

/** Applies the temporal contract and its canonical catch-up/era reducers, then journals one command. */
export const advanceFoundationWorldTime = (world: FoundationWorld, command: TemporalCommand | unknown): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can advance time')
  const projection = advanceTimeProjection(world, causalReplayProjectionForWorldState(world.state), command)
  const journalCommand = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'time-bearing-action', { action: command as TimeBearingTemporalAction })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, journalCommand, projection)
  return worldFromProjection(world, projection, history)
}

/** Records a completed durable Jomon change without advancing time, then journals it. */
export const recordDurableJomonGrowth = (world: FoundationWorld, evidence: DurableJomonGrowthEvidence): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can record durable Jomon growth')
  const projection = recordGrowthProjection(world, causalReplayProjectionForWorldState(world.state), evidence)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'durable-jomon-growth', { evidence })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return worldFromProjection(world, projection, history)
}

/** Offers a constrained task after a required pure conversation assessment. */
export const offerFoundationWorldDelegatedTask = (world: FoundationWorld, offer: DelegationOfferInput): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can offer delegated work')
  const projection = offerDelegationProjection(world, causalReplayProjectionForWorldState(world.state), offer)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'delegation-offered', { offer })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return worldFromProjection(world, projection, history)
}

/** Resolves the only v1 interruption causes after their explicit one-minute action. */
export const interruptFoundationWorldDelegatedTask = (world: FoundationWorld, interruption: DelegationInterruptionInput): FoundationWorld => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  if (world.status !== 'active') throw new Error('only an active world can interrupt delegated work')
  const projection = interruptDelegationProjection(world, causalReplayProjectionForWorldState(world.state), interruption)
  const command = createCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, 'delegation-interrupted', { interruption })
  const history = appendCausalCommand(causalHistoryContextFor(world), world.state.causalHistory, command, projection)
  return worldFromProjection(world, projection, history)
}

export const finalizeWorldAsChronicle = (world: FoundationWorld, reason: ChronicleReason): WorldChronicle => {
  if (!isValidFoundationWorld(world)) throw new Error('world does not satisfy the complete medieval foundation contract')
  return {
    version: 12,
    id: `chronicle:${world.id}`,
    status: 'finalized',
    reason,
    world: cloneWorld(world)
  }
}

export const chronicleExport = (chronicle: WorldChronicle): string => JSON.stringify(chronicle, null, 2)
