import { SeededRng, hashSeed } from './rng'
import { FOUNDATION_GENERATOR_VERSION, type ChronicleReason, type CrewRelationship, type CrewRole, type FoundationCrewMember, type FoundationJomon, type FoundationWorld, type FoundationWorldConfiguration, type WorldChronicle, type WorldManifest } from './types'

const DEFAULT_SEED = 'jomon-foundation'
const foundationConfiguration = (): FoundationWorldConfiguration => ({ version: 1, profile: 'foundation' })

const foundationJomon = (): FoundationJomon => ({
  id: 'vessel:jomon',
  name: 'Jomon',
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
  configuration?: FoundationWorldConfiguration
}

export const normalizeSeed = (seed: string | undefined): string => {
  const normalized = seed?.trim().replace(/\s+/g, ' ') ?? ''
  return normalized || DEFAULT_SEED
}

const configurationFor = (configuration: FoundationWorldConfiguration | undefined): FoundationWorldConfiguration => {
  if (!configuration || configuration.version !== 1 || configuration.profile !== 'foundation') return foundationConfiguration()
  return { version: configuration.version, profile: configuration.profile }
}

const idForManifest = (seed: string, configuration: FoundationWorldConfiguration): string => {
  const identity = `${FOUNDATION_GENERATOR_VERSION}|${configuration.version}|${configuration.profile}|${seed}`
  const forward = hashSeed(identity).toString(36)
  const reverse = hashSeed([...identity].reverse().join(''), 0x9e3779b9).toString(36)
  return `world:${forward}-${reverse}`
}

const labelForSeed = (seed: string): string => {
  const rng = new SeededRng(`label:${seed}`)
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

const generateCrew = (manifest: WorldManifest): readonly FoundationCrewMember[] => {
  const rng = new SeededRng(`crew:${manifest.seed}:${manifest.generatorVersion}`)
  const usedNames = new Set<string>()
  const selectedRoles = shuffled(rng, roles).slice(0, 6)
  const crew = selectedRoles.map((role, index): FoundationCrewMember => ({
    id: `crew:${index}`,
    name: nameFor(rng, usedNames),
    role,
    conversation: rng.between(1, 5),
    equipment: [...equipmentByRole[role]],
    history: rng.pick(historyByRole[role]),
    relationships: [],
    eligible: true
  }))
  return crew.map(member => ({ ...member, relationships: crew.filter(other => other.id !== member.id).map(other => relationship(rng, other.id)) }))
}

const cloneWorld = (world: FoundationWorld): FoundationWorld => structuredClone(world)

export const createFoundationWorld = (input: FoundationWorldInput = {}): FoundationWorld => {
  const seed = normalizeSeed(input.seed)
  const configuration = configurationFor(input.configuration)
  const manifest: WorldManifest = { version: 1, seed, configuration, generatorVersion: FOUNDATION_GENERATOR_VERSION, label: labelForSeed(seed) }
  const id = idForManifest(seed, configuration)
  return {
    version: 1,
    id,
    status: 'active',
    manifest,
    jomon: foundationJomon(),
    crew: generateCrew(manifest),
    worldTime: 0,
    causalHistory: [{ sequence: 0, atWorldTime: 0, kind: 'world-created', detail: `Foundation world ${manifest.label} created from seed ${seed}.` }]
  }
}

export const chooseInitialCourier = (world: FoundationWorld, courierId: string): FoundationWorld => {
  if (world.status !== 'active') throw new Error('only an active world can select an initial courier')
  if (world.manifest.initialCourierId !== undefined) throw new Error('initial courier has already been selected')
  const candidate = world.crew.find(member => member.id === courierId)
  if (!candidate?.eligible) throw new Error('selected courier must be an eligible crew member')
  const next = cloneWorld(world)
  next.manifest.initialCourierId = courierId
  next.causalHistory = [...next.causalHistory, { sequence: next.causalHistory.length, atWorldTime: 0, kind: 'initial-courier-selected', detail: `${candidate.name} chosen as the initial courier.` }]
  return next
}

export const finalizeWorldAsChronicle = (world: FoundationWorld, reason: ChronicleReason): WorldChronicle => ({
  version: 1,
  id: `chronicle:${world.id}`,
  status: 'finalized',
  reason,
  world: cloneWorld(world)
})

export const chronicleExport = (chronicle: WorldChronicle): string => JSON.stringify(chronicle, null, 2)
