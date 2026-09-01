import { SeededRng, hashSeed } from './rng'
import { auditMedievalContentSafety, classifyMedievalContent, contentSafetyAuditMatches, isMedievalContentSafetyAudit, type ClassifiedMedievalContent, type MedievalContentSafetyAudit, type MedievalContentSafetyDiagnostic } from './content-safety'
import { generationConfigurationFingerprint, generationRetryPlan, isReproducibleGenerationDiagnostics, resolveWorldGenerationConfig, type WorldGenerationConfig, type WorldGenerationConfigIssue, type WorldGenerationConfigRequest } from './generation-config'
import { generateInitialWorld, initialWorldContentRecords, type InitialWorld, type InitialWorldGenerationDiagnostics } from './initial-world'
import { FOUNDATION_GENERATOR_VERSION, FOUNDATION_MANIFEST_VERSION, type CausalRecord, type ChronicleReason, type CrewRelationship, type CrewRole, type FoundationCrewMember, type FoundationJomon, type FoundationWorld, type WorldChronicle, type WorldManifest } from './types'

const DEFAULT_SEED = 'jomon-foundation'

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

export const normalizeSeed = (seed: string | undefined): string => {
  const normalized = seed?.trim().replace(/\s+/g, ' ') ?? ''
  return normalized || DEFAULT_SEED
}

const idForManifest = (seed: string, configuration: WorldGenerationConfig): string => {
  const identity = `${FOUNDATION_GENERATOR_VERSION}|${generationConfigurationFingerprint(configuration)}|${seed}`
  const forward = hashSeed(identity).toString(36)
  const reverse = hashSeed([...identity].reverse().join(''), 0x9e3779b9).toString(36)
  return `world:${forward}-${reverse}`
}

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
  labelContentSafety: WorldManifest['labelContentSafety'],
  jomon: FoundationJomon,
  crew: readonly FoundationCrewMember[],
  causalHistory: readonly CausalRecord[],
  initialWorld: InitialWorld
): readonly ClassifiedMedievalContent[] => [
  { id: 'world:label', domain: 'place', classification: labelContentSafety },
  { id: jomon.id, domain: 'place', classification: jomon.contentSafety },
  ...crew.flatMap(member => [
    { id: member.id, domain: 'person' as const, classification: member.contentSafety },
    { id: `${member.id}:history`, domain: 'history' as const, classification: member.historyContentSafety }
  ]),
  ...causalHistory.map(record => ({ id: `causal:${record.sequence}:${record.kind}`, domain: 'event' as const, classification: record.contentSafety })),
  ...initialWorldContentRecords(initialWorld)
]

const auditFoundationContent = (
  labelContentSafety: WorldManifest['labelContentSafety'],
  jomon: FoundationJomon,
  crew: readonly FoundationCrewMember[],
  causalHistory: readonly CausalRecord[],
  initialWorld: InitialWorld
): MedievalContentSafetyAudit => {
  const audit = auditMedievalContentSafety(foundationContentRecords(labelContentSafety, jomon, crew, causalHistory, initialWorld))
  if (audit.status === 'rejected') throw new MedievalContentSafetyPolicyError(audit.diagnostics)
  return audit
}

interface ExpectedFoundationContentProvenance {
  label: string
  labelContentSafety: WorldManifest['labelContentSafety']
  initialWorld: InitialWorld
  initialWorldGeneration: InitialWorldGenerationDiagnostics
  contentSafetyAudit: MedievalContentSafetyAudit
}

const expectedFoundationContentProvenance = (
  seed: string,
  configuration: WorldGenerationConfig,
  initialCourierId: string | undefined
): ExpectedFoundationContentProvenance | undefined => {
  const label = labelForSeed(seed, configuration)
  const labelContentSafety = classifyMedievalContent('place', ['environment', 'settlement'], 'not-applicable', ['player-facing-text'])
  const jomon = foundationJomon()
  const crew = generateCrew(seed, configuration)
  const initialWorldGeneration = generateInitialWorld(seed, configuration)
  const causalHistory = [worldCreatedRecord(label, seed)]
  if (initialCourierId !== undefined) {
    const courier = crew.find(member => member.id === initialCourierId)
    if (!courier?.eligible) return undefined
    causalHistory.push(initialCourierSelectedRecord(causalHistory.length, courier.name))
  }
  return {
    label,
    labelContentSafety,
    initialWorld: initialWorldGeneration.world,
    initialWorldGeneration: initialWorldGeneration.diagnostics,
    contentSafetyAudit: auditFoundationContent(labelContentSafety, jomon, crew, causalHistory, initialWorldGeneration.world)
  }
}

const cloneWorld = (world: FoundationWorld): FoundationWorld => structuredClone(world)

export const foundationWorldIdForManifest = (manifest: WorldManifest): string => idForManifest(manifest.seed, manifest.resolvedConfiguration)

export const isReproducibleWorldManifest = (value: unknown): value is WorldManifest => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return false
  const manifest = value as Record<string, unknown>
  if (manifest.version !== FOUNDATION_MANIFEST_VERSION || typeof manifest.seed !== 'string' || !manifest.seed || normalizeSeed(manifest.seed) !== manifest.seed || manifest.generatorVersion !== FOUNDATION_GENERATOR_VERSION || typeof manifest.label !== 'string' || !manifest.label || !isMedievalContentSafetyAudit(manifest.contentSafetyAudit)) return false
  if (manifest.initialCourierId !== undefined && (typeof manifest.initialCourierId !== 'string' || !manifest.initialCourierId)) return false
  if (!isReproducibleGenerationDiagnostics(manifest.seed, manifest.selectedConfiguration, manifest.resolvedConfiguration, manifest.generationDiagnostics)) return false
  const expected = expectedFoundationContentProvenance(manifest.seed, manifest.resolvedConfiguration as WorldGenerationConfig, manifest.initialCourierId as string | undefined)
  return expected !== undefined && manifest.label === expected.label && JSON.stringify(manifest.labelContentSafety) === JSON.stringify(expected.labelContentSafety) && JSON.stringify(manifest.initialWorldGeneration) === JSON.stringify(expected.initialWorldGeneration) && JSON.stringify(manifest.contentSafetyAudit) === JSON.stringify(expected.contentSafetyAudit)
}

export const createFoundationWorld = (input: FoundationWorldInput = {}): FoundationWorld => {
  const seed = normalizeSeed(input.seed)
  const configurationResolution = resolveWorldGenerationConfig(input.configuration)
  if (configurationResolution.status !== 'valid') throw new InvalidWorldGenerationConfigurationError(configurationResolution.issues)
  const label = labelForSeed(seed, configurationResolution.configuration)
  const labelContentSafety = classifyMedievalContent('place', ['environment', 'settlement'], 'not-applicable', ['player-facing-text'])
  const jomon = foundationJomon()
  const crew = generateCrew(seed, configurationResolution.configuration)
  const initialWorldGeneration = generateInitialWorld(seed, configurationResolution.configuration)
  const causalHistory = [worldCreatedRecord(label, seed)]
  const contentSafetyAudit = auditFoundationContent(labelContentSafety, jomon, crew, causalHistory, initialWorldGeneration.world)
  const manifest: WorldManifest = {
    version: FOUNDATION_MANIFEST_VERSION,
    seed,
    selectedConfiguration: configurationResolution.selectedConfiguration,
    resolvedConfiguration: configurationResolution.configuration,
    generationDiagnostics: { validation: { status: 'accepted', issues: [] }, retryPlan: generationRetryPlan(seed, configurationResolution.configuration) },
    initialWorldGeneration: initialWorldGeneration.diagnostics,
    generatorVersion: FOUNDATION_GENERATOR_VERSION,
    label,
    labelContentSafety,
    contentSafetyAudit
  }
  const id = idForManifest(seed, configurationResolution.configuration)
  return {
    version: 1,
    id,
    status: 'active',
    manifest,
    jomon,
    crew,
    initialWorld: initialWorldGeneration.world,
    worldTime: 0,
    causalHistory
  }
}

export const recreateFoundationWorld = (manifest: WorldManifest): FoundationWorld => {
  if (!isReproducibleWorldManifest(manifest)) throw new Error('world manifest does not reproduce the current medieval generation contract')
  const recreated = createFoundationWorld({ seed: manifest.seed, configuration: manifest.selectedConfiguration })
  return manifest.initialCourierId === undefined ? recreated : chooseInitialCourier(recreated, manifest.initialCourierId)
}

export const chooseInitialCourier = (world: FoundationWorld, courierId: string): FoundationWorld => {
  if (world.status !== 'active') throw new Error('only an active world can select an initial courier')
  if (world.manifest.initialCourierId !== undefined) throw new Error('initial courier has already been selected')
  const candidate = world.crew.find(member => member.id === courierId)
  if (!candidate?.eligible) throw new Error('selected courier must be an eligible crew member')
  const next = cloneWorld(world)
  next.manifest.initialCourierId = courierId
  next.causalHistory = [...next.causalHistory, initialCourierSelectedRecord(next.causalHistory.length, candidate.name)]
  next.manifest.contentSafetyAudit = auditFoundationContent(next.manifest.labelContentSafety, next.jomon, next.crew, next.causalHistory, next.initialWorld)
  return next
}

/** Storage uses this after structural validation so saved worlds cannot bypass the policy. */
export const foundationWorldContentSatisfiesSafetyPolicy = (world: FoundationWorld): boolean => contentSafetyAuditMatches(
  foundationContentRecords(world.manifest.labelContentSafety, world.jomon, world.crew, world.causalHistory, world.initialWorld),
  world.manifest.contentSafetyAudit
)

/** Storage uses this to reject a modified region even when its tags still look safe. */
export const foundationWorldInitialWorldMatchesManifest = (world: FoundationWorld): boolean => {
  const expected = generateInitialWorld(world.manifest.seed, world.manifest.resolvedConfiguration)
  return JSON.stringify(world.initialWorld) === JSON.stringify(expected.world) && JSON.stringify(world.manifest.initialWorldGeneration) === JSON.stringify(expected.diagnostics)
}

export const finalizeWorldAsChronicle = (world: FoundationWorld, reason: ChronicleReason): WorldChronicle => ({
  version: 1,
  id: `chronicle:${world.id}`,
  status: 'finalized',
  reason,
  world: cloneWorld(world)
})

export const chronicleExport = (chronicle: WorldChronicle): string => JSON.stringify(chronicle, null, 2)
