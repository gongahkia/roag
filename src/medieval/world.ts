import { SeededRng, hashSeed } from './rng'
import { MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION, auditMedievalContentSafety, classifyMedievalContent, contentSafetyAuditMatches, isMedievalContentSafetyAudit, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyDiagnostic } from './content-safety'
import { FRONTIER_CONTRACT_VERSION, createInitialFrontierState, frontierContentRecords, type FrontierState } from './frontier'
import { generationConfigurationFingerprint, generationRetryPlan, isReproducibleGenerationDiagnostics, resolveWorldGenerationConfig, type WorldGenerationConfig, type WorldGenerationConfigIssue, type WorldGenerationConfigRequest } from './generation-config'
import { INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION, INITIAL_WORLD_GENERATOR_VERSION, generateInitialWorld, initialWorldContentRecords, type InitialWorld, type InitialWorldGenerationDiagnostics, type InitialWorldGenerationProgressObserver } from './initial-world'
import { normalizeCreationSeed } from './settings'
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

const initialCourierSelectedRecord = (sequence: number, name: string): CausalRecord => ({
  sequence,
  atWorldTime: 0,
  kind: 'initial-courier-selected',
  detail: `${name} chosen as the initial courier.`,
  contentSafety: classifyMedievalContent('event', ['civil-life', 'travel'], 'adults-only', ['player-facing-text'])
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
  initialCourierId: string | undefined,
  onGenerationProgress?: InitialWorldGenerationProgressObserver
): ExpectedFoundationState | undefined => {
  const label = labelForSeed(seed, configuration)
  const labelContentSafety = classifyMedievalContent('place', ['environment', 'settlement'], 'not-applicable', ['player-facing-text'])
  const jomon = foundationJomon()
  const crew = generateCrew(seed, configuration)
  const initialWorldGeneration = generateInitialWorld(seed, configuration, onGenerationProgress)
  const frontier = createInitialFrontierState({ seed, configuration, initialWorld: initialWorldGeneration.world })
  const causalHistory = [worldCreatedRecord(label, seed)]
  if (initialCourierId !== undefined) {
    const courier = crew.find(member => member.id === initialCourierId)
    if (!courier?.eligible) return undefined
    causalHistory.push(initialCourierSelectedRecord(causalHistory.length, courier.name))
  }
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

const expectedCreationProvenance = (seed: string, selectedConfiguration: WorldCreationProvenance['selectedConfiguration'], resolvedConfiguration: WorldGenerationConfig): WorldCreationProvenance | undefined => {
  const state = expectedFoundationState(seed, resolvedConfiguration, undefined)
  return state === undefined ? undefined : creationProvenanceFor(seed, selectedConfiguration, resolvedConfiguration, state)
}

const cloneWorld = (world: FoundationWorld): FoundationWorld => structuredClone(world)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)

export const foundationWorldIdForManifest = (manifest: WorldManifest): string => idForCreationProvenance(manifest.creation)

/** Stable JSON for sharing/export; it refuses a manifest outside the current contract. */
export const serializeWorldManifest = (manifest: WorldManifest): string => {
  if (!isReproducibleWorldManifest(manifest)) throw new Error('world manifest does not reproduce the current medieval generation contract')
  return canonicalJson(manifest)
}

export const isReproducibleWorldManifest = (value: unknown): value is WorldManifest => {
  if (!record(value) || value.version !== FOUNDATION_MANIFEST_VERSION || !hasOnlyKeys(value, value.initialCourierId === undefined ? ['version', 'creation', 'currentContentSafetyAudit'] : ['version', 'creation', 'initialCourierId', 'currentContentSafetyAudit']) || !record(value.creation) || !isMedievalContentSafetyAudit(value.currentContentSafetyAudit)) return false
  if (value.initialCourierId !== undefined && (typeof value.initialCourierId !== 'string' || !value.initialCourierId)) return false
  const creation = value.creation
  if (creation.version !== WORLD_CREATION_PROVENANCE_VERSION || typeof creation.seed !== 'string' || !creation.seed || normalizeSeed(creation.seed) !== creation.seed || typeof creation.configurationFingerprint !== 'string' || !record(creation.validationHistory) || creation.validationHistory.version !== WORLD_MANIFEST_VALIDATION_HISTORY_VERSION || !isReproducibleGenerationDiagnostics(creation.seed, creation.selectedConfiguration, creation.resolvedConfiguration, creation.validationHistory.generation)) return false
  const resolvedConfiguration = creation.resolvedConfiguration as WorldGenerationConfig
  if (creation.configurationFingerprint !== generationConfigurationFingerprint(resolvedConfiguration)) return false
  const contractVersions = creation.contractVersions
  if (!record(contractVersions) || contractVersions.foundationGenerator !== FOUNDATION_GENERATOR_VERSION || contractVersions.initialWorldGenerator !== INITIAL_WORLD_GENERATOR_VERSION || contractVersions.initialWorldDiagnostics !== INITIAL_WORLD_GENERATION_DIAGNOSTICS_VERSION || contractVersions.frontierContract !== FRONTIER_CONTRACT_VERSION || contractVersions.contentSafetyPolicy !== MEDIEVAL_CONTENT_SAFETY_POLICY_VERSION || !isMedievalContentSafetyAudit(creation.contentSafetyAudit)) return false
  const expectedCreation = expectedCreationProvenance(creation.seed, creation.selectedConfiguration as WorldCreationProvenance['selectedConfiguration'], resolvedConfiguration)
  const expectedState = expectedFoundationState(creation.seed, resolvedConfiguration, value.initialCourierId as string | undefined)
  return expectedCreation !== undefined && expectedState !== undefined && equivalent(creation, expectedCreation) && equivalent(value.currentContentSafetyAudit, expectedState.contentSafetyAudit)
}

const worldFromCreationProvenance = (
  creation: WorldCreationProvenance,
  initialCourierId: string | undefined,
  precomputedState?: ExpectedFoundationState
): FoundationWorld => {
  const state = precomputedState ?? expectedFoundationState(creation.seed, creation.resolvedConfiguration, initialCourierId)
  if (!state) throw new Error('creation provenance does not identify an eligible initial courier')
  return {
    version: 1,
    id: idForCreationProvenance(creation),
    status: 'active',
    manifest: {
      version: FOUNDATION_MANIFEST_VERSION,
      creation,
      ...(initialCourierId === undefined ? {} : { initialCourierId }),
      currentContentSafetyAudit: state.contentSafetyAudit
    },
    jomon: state.jomon,
    crew: state.crew,
    initialWorld: state.initialWorld,
    worldTime: 0,
    causalHistory: state.causalHistory
  }
}

export const createFoundationWorld = (input: FoundationWorldInput = {}): FoundationWorld => {
  const seed = normalizeSeed(input.seed)
  const configurationResolution = resolveWorldGenerationConfig(input.configuration)
  if (configurationResolution.status !== 'valid') throw new InvalidWorldGenerationConfigurationError(configurationResolution.issues)
  const state = expectedFoundationState(seed, configurationResolution.configuration, undefined, input.onGenerationProgress)
  if (!state) throw new Error('foundation world creation requires an unselected courier state')
  const creation = creationProvenanceFor(seed, configurationResolution.selectedConfiguration, configurationResolution.configuration, state)
  return worldFromCreationProvenance(creation, undefined, state)
}

export const recreateFoundationWorld = (manifest: WorldManifest): FoundationWorld => {
  if (!isReproducibleWorldManifest(manifest)) throw new Error('world manifest does not reproduce the current medieval generation contract')
  return worldFromCreationProvenance(manifest.creation, manifest.initialCourierId)
}

export const chooseInitialCourier = (world: FoundationWorld, courierId: string): FoundationWorld => {
  if (world.status !== 'active') throw new Error('only an active world can select an initial courier')
  if (world.manifest.initialCourierId !== undefined) throw new Error('initial courier has already been selected')
  const candidate = world.crew.find(member => member.id === courierId)
  if (!candidate?.eligible) throw new Error('selected courier must be an eligible crew member')
  const next = cloneWorld(world)
  next.manifest.initialCourierId = courierId
  next.causalHistory = [...next.causalHistory, initialCourierSelectedRecord(next.causalHistory.length, candidate.name)]
  const frontier = createInitialFrontierState({ seed: next.manifest.creation.seed, configuration: next.manifest.creation.resolvedConfiguration, initialWorld: next.initialWorld })
  next.manifest.currentContentSafetyAudit = auditFoundationContent(next.manifest.creation.labelContentSafety, next.jomon, next.crew, next.causalHistory, next.initialWorld, frontier)
  return next
}

/** Storage uses this after structural validation so saved worlds cannot bypass the policy. */
export const foundationWorldContentSatisfiesSafetyPolicy = (world: FoundationWorld): boolean => {
  try {
    const frontier = createInitialFrontierState({ seed: world.manifest.creation.seed, configuration: world.manifest.creation.resolvedConfiguration, initialWorld: world.initialWorld })
    return contentSafetyAuditMatches(
      foundationContentRecords(world.manifest.creation.labelContentSafety, world.jomon, world.crew, world.causalHistory, world.initialWorld, frontier),
      world.manifest.currentContentSafetyAudit
    )
  } catch { return false }
}

/** Storage uses this to reject a modified region even when its tags still look safe. */
export const foundationWorldInitialWorldMatchesManifest = (world: FoundationWorld): boolean => {
  try {
    const expected = generateInitialWorld(world.manifest.creation.seed, world.manifest.creation.resolvedConfiguration)
    return equivalent(world.initialWorld, expected.world) && equivalent(world.manifest.creation.initialWorld, initialWorldIdentityFor(expected.world)) && equivalent(world.manifest.creation.validationHistory.initialWorld, expected.diagnostics)
  } catch { return false }
}

export const finalizeWorldAsChronicle = (world: FoundationWorld, reason: ChronicleReason): WorldChronicle => ({
  version: 1,
  id: `chronicle:${world.id}`,
  status: 'finalized',
  reason,
  world: cloneWorld(world)
})

export const chronicleExport = (chronicle: WorldChronicle): string => JSON.stringify(chronicle, null, 2)
