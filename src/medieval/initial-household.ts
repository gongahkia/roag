import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import { generationConfigurationFingerprint, type WorldGenerationConfig } from './generation-config'
import { SeededRng } from './rng'
import { normalizeCreationSeed } from './settings'
import { FOUNDATION_GENERATOR_VERSION, type CrewRelationship, type CrewRole, type FoundationCrewMember } from './types'

/**
 * The deterministic, immutable zero-time household seam. It owns roster
 * generation and its selection-ready view; mutable courier state remains in
 * `WorldCourierState` and is deliberately not represented here.
 */
export const INITIAL_HOUSEHOLD_CONTRACT_VERSION = 1 as const
export const INITIAL_HOUSEHOLD_ROSTER_SIZE = 6 as const

export interface InitialHouseholdGenerationContext {
  /** Must already be normalized by the creation/settings boundary. */
  seed: string
  configuration: WorldGenerationConfig
}

/** The existing stream's resolved-configuration input, retained for replay validation. */
export interface InitialHouseholdReproducibilityContext {
  seed: string
  configurationFingerprint: string
}

/**
 * Renderer-independent data that a later zero-time selection owner may show.
 * It contains only immutable household identity and selection metadata.
 */
export interface InitialHouseholdActiveCrewMember {
  id: string
  name: string
  role: CrewRole
  conversation: 1 | 2 | 3 | 4 | 5
}

export interface InitialHousehold {
  version: typeof INITIAL_HOUSEHOLD_CONTRACT_VERSION
  roster: readonly FoundationCrewMember[]
  activeCrew: readonly InitialHouseholdActiveCrewMember[]
}

export type InitialHouseholdValidationCode =
  | 'initial-household.malformed-household'
  | 'initial-household.invalid-context'
  | 'initial-household.invalid-roster-size'
  | 'initial-household.invalid-roster-id'
  | 'initial-household.noncanonical-roster-order'
  | 'initial-household.duplicate-name'
  | 'initial-household.invalid-name'
  | 'initial-household.invalid-role'
  | 'initial-household.duplicate-role'
  | 'initial-household.invalid-conversation'
  | 'initial-household.invalid-equipment'
  | 'initial-household.invalid-history'
  | 'initial-household.invalid-eligibility'
  | 'initial-household.invalid-relationship'
  | 'initial-household.invalid-active-crew'
  | 'initial-household.non-reproducible-roster'
  | MedievalContentSafetyDiagnosticCode

export interface InitialHouseholdValidationIssue {
  recordId: string
  code: InitialHouseholdValidationCode
}

export class InitialHouseholdContractError extends Error {
  constructor(readonly diagnostics: readonly InitialHouseholdValidationIssue[]) {
    super(`initial household rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'InitialHouseholdContractError'
  }
}

const roles: readonly CrewRole[] = ['bargemaster', 'pilot', 'factor', 'carpenter', 'guard', 'cook', 'healer', 'scribe', 'carter', 'fisher', 'bard']
const nameStarts = ['Ari', 'Bel', 'Caro', 'Dara', 'Eren', 'Fara', 'Galen', 'Hara', 'Iven', 'Jori', 'Kesa', 'Loran', 'Mira', 'Neris', 'Oren', 'Pava', 'Risa', 'Soren', 'Tavi', 'Vela'] as const
const nameEnds = ['n', 'ra', 'en', 'a', 'is', 'or', 'et', 'i', 'an', 'el'] as const
const familyNames = ['Ash', 'Barrow', 'Cairn', 'Dike', 'Elm', 'Ford', 'Gull', 'Hearth', 'Ivy', 'Keel', 'Lark', 'Moss', 'Nettle', 'Pike', 'Quill', 'Reed', 'Silt', 'Thorn', 'Vale', 'Wren'] as const

/** Role-owned values are deliberately closed and do not add authored content. */
export const INITIAL_HOUSEHOLD_EQUIPMENT_BY_ROLE: Readonly<Record<CrewRole, readonly string[]>> = {
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

export const INITIAL_HOUSEHOLD_HISTORY_BY_ROLE: Readonly<Record<CrewRole, readonly string[]>> = {
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
const canonicalRosterIds = Array.from({ length: INITIAL_HOUSEHOLD_ROSTER_SIZE }, (_, index) => `crew:${index}`)
const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const issue = (recordId: string, code: InitialHouseholdValidationCode): InitialHouseholdValidationIssue => ({ recordId, code })
const canonicalIssues = (issues: readonly InitialHouseholdValidationIssue[]): readonly InitialHouseholdValidationIssue[] => [...new Map(issues.map(value => [`${value.recordId}\u0000${value.code}`, value])).values()]
  .sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))

const rosterIdAt = (index: number): string => canonicalRosterIds[index]!
const nameFor = (rng: SeededRng, used: Set<string>): string => {
  for (let attempts = 0; attempts < 100; attempts++) {
    const name = `${rng.pick(nameStarts)}${rng.pick(nameEnds)} ${rng.pick(familyNames)}`
    if (!used.has(name)) { used.add(name); return name }
  }
  throw new Error('could not generate a unique foundation crew name')
}

const shuffled = <Value>(rng: SeededRng, values: readonly Value[]): Value[] => {
  const next = [...values]
  for (let index = next.length - 1; index > 0; index--) {
    const swap = rng.integer(index + 1)
    const current = next[index]!
    next[index] = next[swap]!
    next[swap] = current
  }
  return next
}

const relationship = (rng: SeededRng, personId: string): CrewRelationship => ({
  personId,
  standing: rng.pick([-2, -1, 0, 1, 2] as const),
  basis: rng.pick(relationshipBases)
})

const householdStream = (context: InitialHouseholdReproducibilityContext): string => `crew:${context.seed}:${FOUNDATION_GENERATOR_VERSION}:${context.configurationFingerprint}`
const isValidContext = (context: InitialHouseholdReproducibilityContext): boolean => typeof context.seed === 'string'
  && context.seed.length > 0
  && context.seed === normalizeCreationSeed(context.seed)
  && typeof context.configurationFingerprint === 'string'
  && context.configurationFingerprint.length > 0

/** The historical relationship record shape has no ID field, so pair identity is derived, not persisted. */
export const initialHouseholdRelationshipId = (memberId: string, targetMemberId: string): string => `${memberId}:relationship:${targetMemberId}`

const generateRoster = (context: InitialHouseholdReproducibilityContext): readonly FoundationCrewMember[] => {
  const rng = new SeededRng(householdStream(context))
  const usedNames = new Set<string>()
  const selectedRoles = shuffled(rng, roles).slice(0, INITIAL_HOUSEHOLD_ROSTER_SIZE)
  const roster = selectedRoles.map((role, index): FoundationCrewMember => ({
    id: rosterIdAt(index),
    name: nameFor(rng, usedNames),
    contentSafety: classifyMedievalContent('person', ['adult-labour', 'travel'], 'adults-only', ['player-facing-text']),
    role,
    conversation: rng.between(1, 5),
    equipment: [...INITIAL_HOUSEHOLD_EQUIPMENT_BY_ROLE[role]],
    history: rng.pick(INITIAL_HOUSEHOLD_HISTORY_BY_ROLE[role]),
    historyContentSafety: classifyMedievalContent('history', ['adult-labour', 'ordinary-hardship', 'travel'], 'adults-only', ['player-facing-text']),
    relationships: [],
    eligible: true
  }))
  return roster.map(member => ({ ...member, relationships: roster.filter(other => other.id !== member.id).map(other => relationship(rng, other.id)) }))
}

/** Every generated player-facing identity and history enters the existing safety audit. */
export const initialHouseholdContentRecords = (roster: readonly FoundationCrewMember[]): readonly ClassifiedMedievalContent[] => roster.flatMap(member => [
  { id: member.id, domain: 'person' as const, classification: member.contentSafety },
  { id: `${member.id}:history`, domain: 'history' as const, classification: member.historyContentSafety }
])

/**
 * Current zero-time eligibility is static creation eligibility. Every roster
 * member is eligible and the canonical selection order is roster order.
 * Life, availability, and the selected courier stay with mutable world state.
 */
const activeCrewForRoster = (roster: readonly FoundationCrewMember[]): readonly InitialHouseholdActiveCrewMember[] => roster
  .filter(member => member.eligible)
  .map(member => ({ id: member.id, name: member.name, role: member.role, conversation: member.conversation as 1 | 2 | 3 | 4 | 5 }))

export const initialHouseholdActiveCrew = (roster: readonly FoundationCrewMember[]): readonly InitialHouseholdActiveCrewMember[] => {
  const validation = validateInitialHouseholdStructure(roster)
  if (validation.length) throw new InitialHouseholdContractError(validation)
  return activeCrewForRoster(roster)
}

/** Validates invariant structure without requiring a seed/configuration context. */
export const validateInitialHouseholdStructure = (value: unknown): readonly InitialHouseholdValidationIssue[] => {
  const issues: InitialHouseholdValidationIssue[] = []
  if (!Array.isArray(value)) return [issue('initial-household', 'initial-household.malformed-household')]
  if (value.length !== INITIAL_HOUSEHOLD_ROSTER_SIZE) issues.push(issue('initial-household', 'initial-household.invalid-roster-size'))
  const roster = value.filter(record) as Record<string, unknown>[]
  if (roster.length !== value.length) issues.push(issue('initial-household', 'initial-household.malformed-household'))
  const knownIds = new Set(canonicalRosterIds)
  const names = new Set<string>()
  const roleSet = new Set<CrewRole>()
  roster.forEach((member, index) => {
    const expectedId = rosterIdAt(index)
    const memberId = typeof member.id === 'string' ? member.id : `initial-household:${index}`
    if (!hasOnlyKeys(member, ['id', 'name', 'contentSafety', 'role', 'conversation', 'equipment', 'history', 'historyContentSafety', 'relationships', 'eligible'])) issues.push(issue(memberId, 'initial-household.malformed-household'))
    if (member.id !== expectedId) issues.push(issue(memberId, knownIds.has(member.id as string) ? 'initial-household.noncanonical-roster-order' : 'initial-household.invalid-roster-id'))
    if (typeof member.name !== 'string' || !member.name || member.name !== member.name.trim() || member.name.length > 96 || !/^[A-Za-z]+ [A-Za-z]+$/u.test(member.name)) issues.push(issue(memberId, 'initial-household.invalid-name'))
    else if (names.has(member.name)) issues.push(issue(memberId, 'initial-household.duplicate-name'))
    else names.add(member.name)
    if (!roles.includes(member.role as CrewRole)) issues.push(issue(memberId, 'initial-household.invalid-role'))
    else if (roleSet.has(member.role as CrewRole)) issues.push(issue(memberId, 'initial-household.duplicate-role'))
    else roleSet.add(member.role as CrewRole)
    if (!Number.isSafeInteger(member.conversation) || (member.conversation as number) < 1 || (member.conversation as number) > 5) issues.push(issue(memberId, 'initial-household.invalid-conversation'))
    const role = member.role as CrewRole
    if (!roles.includes(role) || !same(member.equipment, INITIAL_HOUSEHOLD_EQUIPMENT_BY_ROLE[role])) issues.push(issue(memberId, 'initial-household.invalid-equipment'))
    if (!roles.includes(role) || !INITIAL_HOUSEHOLD_HISTORY_BY_ROLE[role].includes(member.history as string)) issues.push(issue(memberId, 'initial-household.invalid-history'))
    if (member.eligible !== true) issues.push(issue(memberId, 'initial-household.invalid-eligibility'))
  })
  if (names.size !== roster.length) issues.push(issue('initial-household', 'initial-household.duplicate-name'))
  if (roleSet.size !== roster.length) issues.push(issue('initial-household', 'initial-household.duplicate-role'))
  const memberIds = new Set(roster.map(member => member.id).filter((id): id is string => typeof id === 'string'))
  roster.forEach(member => {
    const memberId = typeof member.id === 'string' ? member.id : 'initial-household'
    if (!Array.isArray(member.relationships) || member.relationships.length !== Math.max(0, roster.length - 1)) { issues.push(issue(memberId, 'initial-household.invalid-relationship')); return }
    const targets = new Set<string>()
    const expectedTargets = roster.map(candidate => candidate.id).filter((id): id is string => typeof id === 'string' && id !== member.id)
    member.relationships.forEach((link, relationshipIndex) => {
      if (!record(link) || !hasOnlyKeys(link, ['personId', 'standing', 'basis']) || typeof link.personId !== 'string' || !memberIds.has(link.personId) || link.personId === member.id || targets.has(link.personId)
        || link.personId !== expectedTargets[relationshipIndex]
        || ![-2, -1, 0, 1, 2].includes(link.standing as number)
        || !relationshipBases.includes(link.basis as CrewRelationship['basis'])) issues.push(issue(memberId, 'initial-household.invalid-relationship'))
      if (record(link) && typeof link.personId === 'string') targets.add(link.personId)
    })
    for (const targetId of expectedTargets) {
      const reciprocalMember = roster.find(candidate => candidate.id === targetId)
      const reciprocal = reciprocalMember?.relationships
      if (!Array.isArray(reciprocal) || !reciprocal.some(link => record(link) && link.personId === member.id)) issues.push(issue(initialHouseholdRelationshipId(memberId, targetId), 'initial-household.invalid-relationship'))
    }
  })
  try {
    const audit = auditMedievalContentSafety(initialHouseholdContentRecords(roster as unknown as FoundationCrewMember[]))
    if (audit.status === 'rejected') issues.push(...audit.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  } catch {
    issues.push(issue('initial-household', 'initial-household.malformed-household'))
  }
  return canonicalIssues(issues)
}

/** Validates structure plus exact seed/fingerprint regeneration without mutation. */
export const validateInitialHouseholdRoster = (context: InitialHouseholdReproducibilityContext, roster: unknown): readonly InitialHouseholdValidationIssue[] => {
  const issues = [...validateInitialHouseholdStructure(roster)]
  if (!isValidContext(context)) issues.push(issue('initial-household:context', 'initial-household.invalid-context'))
  if (!issues.length && !same(roster, generateRoster(context))) issues.push(issue('initial-household', 'initial-household.non-reproducible-roster'))
  return canonicalIssues(issues)
}

/** Validates the complete derived result, including its canonical active-crew projection. */
export const validateInitialHousehold = (context: InitialHouseholdReproducibilityContext, value: unknown): readonly InitialHouseholdValidationIssue[] => {
  if (!record(value) || Object.keys(value).sort().join(',') !== 'activeCrew,roster,version') return [issue('initial-household', 'initial-household.malformed-household')]
  const issues = [...validateInitialHouseholdRoster(context, value.roster)]
  if (value.version !== INITIAL_HOUSEHOLD_CONTRACT_VERSION) issues.push(issue('initial-household', 'initial-household.malformed-household'))
  if (!issues.some(candidate => candidate.code === 'initial-household.malformed-household') && Array.isArray(value.roster)) {
    const expectedActiveCrew = activeCrewForRoster(value.roster as FoundationCrewMember[])
    if (!same(value.activeCrew, expectedActiveCrew)) issues.push(issue('initial-household:active-crew', 'initial-household.invalid-active-crew'))
  }
  return canonicalIssues(issues)
}

/** Generates and immediately validates the immutable household from resolved creation inputs. */
export const createInitialHousehold = (context: InitialHouseholdGenerationContext): InitialHousehold => {
  const reproducibility: InitialHouseholdReproducibilityContext = {
    seed: context.seed,
    configurationFingerprint: generationConfigurationFingerprint(context.configuration)
  }
  const roster = generateRoster(reproducibility)
  const household: InitialHousehold = { version: INITIAL_HOUSEHOLD_CONTRACT_VERSION, roster, activeCrew: activeCrewForRoster(roster) }
  const validation = validateInitialHousehold(reproducibility, household)
  if (validation.length) throw new InitialHouseholdContractError(validation)
  return household
}
