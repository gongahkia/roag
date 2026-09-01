import { auditMedievalContentSafety, classifyMedievalContent, type ClassifiedMedievalContent, type MedievalContentSafetyClassification, type MedievalContentSafetyDiagnosticCode } from './content-safety'
import type { FrontierNamedPersonCommitment, FrontierState } from './frontier'
import type { InitialWorld } from './initial-world'
import { SeededRng } from './rng'
import type { CrewRole, FoundationCrewMember, FoundationJomon } from './types'

/**
 * Persistent people are deliberately separate from generation seeds and frontier
 * commitments. A record exists only once somebody has actually been instantiated.
 */
export const PERSISTENT_PERSON_CONTRACT_VERSION = 1 as const

export const PERSISTENT_PERSON_LIMITS = {
  records: 24,
  skillsPerPerson: 3,
  possessionsPerPerson: 12,
  familyLinksPerPerson: 8,
  relationshipsPerPerson: 12,
  memoriesPerPerson: 12,
  commitmentsPerPerson: 8,
  injuriesPerPerson: 6,
  needMaximum: 5,
  capacityMaximum: 5,
  minimumAdultYears: 18,
  maximumAdultYears: 90
} as const

export type PersistentPersonOrigin = 'foundation-crew'
export type PersistentPersonLifeStatus = 'living' | 'dead'
export type PersistentPersonLocationKind = 'jomon' | 'site' | 'frontier-region'
export type PersistentPersonAvailability = 'available' | 'committed' | 'unavailable'
export type PersistentPersonAssignmentStatus = 'unassigned' | 'assigned'
export type PersistentPersonHealthCondition = 'steady' | 'strained' | 'injured' | 'recovering'
export type PersistentPersonRecoveryStatus = 'none' | 'recovering'
export type PersistentPersonPossessionCondition = 'sound' | 'worn' | 'broken'
export type PersistentPersonFamilyRelation = 'parent' | 'child' | 'sibling' | 'partner' | 'kin'
export type PersistentPersonRelationshipBasis = 'kinship' | 'work' | 'debt' | 'friendship' | 'rivalry'
export type PersistentPersonMemoryKind = 'foundation-history' | 'observed-event' | 'reported-fact'
export type PersistentPersonCommitmentKind = 'vessel-duty' | 'personal-agreement'
export type PersistentPersonCommitmentStatus = 'active' | 'resolved' | 'cancelled'
export type PersistentPersonSkillKind = 'navigation' | 'commerce' | 'craft' | 'care' | 'record-keeping' | 'guarding' | 'provisioning' | 'hauling' | 'fishing' | 'performance' | 'command'

export interface PersistentPersonIdentity {
  name: string
  adult: true
  conversation: number
  contentSafety: MedievalContentSafetyClassification
}

export interface PersistentPersonBirth {
  /** Age at zero-time creation, which avoids an invented civil calendar. */
  yearsBeforeWorldCreation: number
}

export interface PersistentPersonDeath {
  atWorldTime: number
}

export interface PersistentPersonLife {
  status: PersistentPersonLifeStatus
  birth: PersistentPersonBirth
  death: PersistentPersonDeath | null
}

export interface PersistentPersonLocation {
  kind: PersistentPersonLocationKind
  id: string
}

export interface PersistentPersonSkill {
  id: string
  kind: PersistentPersonSkillKind
  level: 1 | 2 | 3
}

export interface PersistentPersonCapacity {
  current: number
  maximum: number
}

export interface PersistentPersonAssignment {
  status: PersistentPersonAssignmentStatus
  assignmentId?: string
  assignedAtWorldTime?: number
}

export interface PersistentPersonWork {
  role: CrewRole
  skills: readonly PersistentPersonSkill[]
  capacity: PersistentPersonCapacity
  assignment: PersistentPersonAssignment
  availability: PersistentPersonAvailability
}

export interface PersistentPersonNeeds {
  nourishment: number
  rest: number
  shelter: number
  safety: number
}

export interface PersistentPersonInjury {
  id: string
  kind: 'strain' | 'minor-wound'
  receivedAtWorldTime: number
  recovery: PersistentPersonRecoveryStatus
  contentSafety: MedievalContentSafetyClassification
}

export interface PersistentPersonHealth {
  condition: PersistentPersonHealthCondition
  injuries: readonly PersistentPersonInjury[]
  recovery: { status: 'none' } | { status: 'recovering'; injuryId: string; completeAtWorldTime: number }
}

export interface PersistentPersonPossession {
  id: string
  ownerPersonId: string
  name: string
  condition: PersistentPersonPossessionCondition
  contentSafety: MedievalContentSafetyClassification
}

export interface InstantiatedPersonReference {
  kind: 'instantiated-person'
  personId: string
}

export interface KnownUninstantiatedPersonReference {
  kind: 'known-uninstantiated-person'
  source: 'initial-person-seed' | 'frontier-named-person'
  sourceId: string
  personId: string
  name: string
  contentSafety: MedievalContentSafetyClassification
}

export type PersistentPersonFamilyReference = InstantiatedPersonReference | KnownUninstantiatedPersonReference

export interface PersistentPersonFamilyLink {
  id: string
  relation: PersistentPersonFamilyRelation
  relative: PersistentPersonFamilyReference
}

export interface PersistentPersonRelationship {
  id: string
  targetPersonId: string
  reciprocalId: string
  standing: -2 | -1 | 0 | 1 | 2
  basis: PersistentPersonRelationshipBasis
  contentSafety: MedievalContentSafetyClassification
}

export interface PersistentPersonMemory {
  id: string
  kind: PersistentPersonMemoryKind
  atWorldTime: number
  detail: string
  contentSafety: MedievalContentSafetyClassification
}

export interface PersistentPersonCommitment {
  id: string
  kind: PersistentPersonCommitmentKind
  status: PersistentPersonCommitmentStatus
  createdAtWorldTime: number
  resolvedAtWorldTime?: number
  detail: string
  contentSafety: MedievalContentSafetyClassification
}

export interface PersistentPersonRecord {
  version: typeof PERSISTENT_PERSON_CONTRACT_VERSION
  id: string
  origin: PersistentPersonOrigin
  sourceCrewId: string
  identity: PersistentPersonIdentity
  life: PersistentPersonLife
  location: PersistentPersonLocation
  work: PersistentPersonWork
  needs: PersistentPersonNeeds
  health: PersistentPersonHealth
  possessions: readonly PersistentPersonPossession[]
  family: readonly PersistentPersonFamilyLink[]
  relationships: readonly PersistentPersonRelationship[]
  memories: readonly PersistentPersonMemory[]
  commitments: readonly PersistentPersonCommitment[]
}

export interface PersistentPersonValidationContext {
  seed: string
  configurationFingerprint: string
  initialWorld: InitialWorld
  frontier: FrontierState
  jomon: FoundationJomon
  crew: readonly FoundationCrewMember[]
  siteIds: readonly string[]
  worldTime: number
}

export type PersistentPersonValidationDiagnosticCode =
  | 'persistent-person.malformed-record'
  | 'persistent-person.invalid-version'
  | 'persistent-person.invalid-origin'
  | 'persistent-person.duplicate-id'
  | 'persistent-person.noncanonical-order'
  | 'persistent-person.budget-exceeded'
  | 'persistent-person.invalid-source'
  | 'persistent-person.missing-crew-person'
  | 'persistent-person.invalid-identity'
  | 'persistent-person.invalid-life'
  | 'persistent-person.invalid-location'
  | 'persistent-person.invalid-work'
  | 'persistent-person.invalid-needs'
  | 'persistent-person.invalid-health'
  | 'persistent-person.invalid-possession'
  | 'persistent-person.invalid-family'
  | 'persistent-person.invalid-relationship'
  | 'persistent-person.invalid-memory'
  | 'persistent-person.invalid-commitment'
  | 'persistent-person.dead-restriction'
  | 'persistent-person.invalid-content'
  | MedievalContentSafetyDiagnosticCode

export interface PersistentPersonValidationIssue {
  recordId: string
  code: PersistentPersonValidationDiagnosticCode
}

const crewSkills: Readonly<Record<CrewRole, readonly PersistentPersonSkillKind[]>> = {
  bargemaster: ['command', 'navigation'],
  pilot: ['navigation', 'fishing'],
  factor: ['commerce', 'record-keeping'],
  carpenter: ['craft', 'hauling'],
  guard: ['guarding', 'navigation'],
  cook: ['provisioning', 'care'],
  healer: ['care', 'record-keeping'],
  scribe: ['record-keeping', 'commerce'],
  carter: ['hauling', 'craft'],
  fisher: ['fishing', 'navigation'],
  bard: ['performance', 'record-keeping']
}
const crewRoles: readonly CrewRole[] = ['bargemaster', 'pilot', 'factor', 'carpenter', 'guard', 'cook', 'healer', 'scribe', 'carter', 'fisher', 'bard']
const skillKinds: readonly PersistentPersonSkillKind[] = ['navigation', 'commerce', 'craft', 'care', 'record-keeping', 'guarding', 'provisioning', 'hauling', 'fishing', 'performance', 'command']
const relationshipBases: readonly PersistentPersonRelationshipBasis[] = ['kinship', 'work', 'debt', 'friendship', 'rivalry']
const familyRelations: readonly PersistentPersonFamilyRelation[] = ['parent', 'child', 'sibling', 'partner', 'kin']
const memoryKinds: readonly PersistentPersonMemoryKind[] = ['foundation-history', 'observed-event', 'reported-fact']
const commitmentKinds: readonly PersistentPersonCommitmentKind[] = ['vessel-duty', 'personal-agreement']
const commitmentStatuses: readonly PersistentPersonCommitmentStatus[] = ['active', 'resolved', 'cancelled']
const injuryKinds: readonly PersistentPersonInjury['kind'][] = ['strain', 'minor-wound']
const possessionConditions: readonly PersistentPersonPossessionCondition[] = ['sound', 'worn', 'broken']

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const same = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)
const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const validId = (value: unknown): value is string => typeof value === 'string' && value.length > 0
const nonNegativeInteger = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0
const boundedInteger = (value: unknown, maximum: number): value is number => nonNegativeInteger(value) && value <= maximum
const oneOf = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const hasOnlyKeys = (value: Record<string, unknown>, expected: readonly string[]): boolean => {
  const keys = Object.keys(value).sort()
  const expectedKeys = [...expected].sort()
  return keys.length === expectedKeys.length && keys.every((key, index) => key === expectedKeys[index])
}
const isSortedById = <Value extends { id: string }>(values: readonly Value[]): boolean => values.every((value, index) => index === 0 || compare(values[index - 1]!.id, value.id) < 0)
const sortedById = <Value extends { id: string }>(values: readonly Value[]): readonly Value[] => [...values].sort((left, right) => compare(left.id, right.id))
const issue = (recordId: string, code: PersistentPersonValidationDiagnosticCode): PersistentPersonValidationIssue => ({ recordId, code })
const canonicalIssues = (issues: readonly PersistentPersonValidationIssue[]): readonly PersistentPersonValidationIssue[] => [...new Map(issues.map(value => [`${value.recordId}\u0000${value.code}`, value])).values()].sort((left, right) => compare(left.recordId, right.recordId) || compare(left.code, right.code))
const content = (id: string, domain: ClassifiedMedievalContent['domain'], classification: MedievalContentSafetyClassification): ClassifiedMedievalContent => ({ id, domain, classification })
const personStream = (context: Pick<PersistentPersonValidationContext, 'seed' | 'configurationFingerprint'>, personId: string): string => `jomon-persistent-person-v${PERSISTENT_PERSON_CONTRACT_VERSION}|${context.seed}|${context.configurationFingerprint}|${personId}`

const relationshipClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('person', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const possessionClassification = (): MedievalContentSafetyClassification => classifyMedievalContent('person', ['adult-labour', 'craft'], 'adults-only', ['player-facing-text'])

/**
 * Deterministically expands the immutable six-person crew into the initial
 * mutable records. Initial-world person seeds and frontier commitments are not
 * consulted here, so they cannot turn into latent mutable people.
 */
export const instantiateFoundationCrewPeople = (context: Pick<PersistentPersonValidationContext, 'seed' | 'configurationFingerprint' | 'jomon' | 'crew'>): readonly PersistentPersonRecord[] => sortedById(context.crew.map(member => {
  const rng = new SeededRng(personStream(context, member.id))
  const relationshipRecords = member.relationships.map(source => ({
    id: `${member.id}:relationship:${source.personId}`,
    targetPersonId: source.personId,
    reciprocalId: `${source.personId}:relationship:${member.id}`,
    standing: source.standing,
    basis: source.basis,
    contentSafety: relationshipClassification()
  }))
  const skills = crewSkills[member.role].map((kind, index) => ({
    id: `${member.id}:skill:${index}`,
    kind,
    level: (1 + rng.integer(3)) as 1 | 2 | 3
  }))
  return {
    version: PERSISTENT_PERSON_CONTRACT_VERSION,
    id: member.id,
    origin: 'foundation-crew' as const,
    sourceCrewId: member.id,
    identity: { name: member.name, adult: true as const, conversation: member.conversation, contentSafety: member.contentSafety },
    life: { status: 'living' as const, birth: { yearsBeforeWorldCreation: PERSISTENT_PERSON_LIMITS.minimumAdultYears + rng.integer(48) }, death: null },
    location: { kind: 'jomon' as const, id: context.jomon.id },
    work: {
      role: member.role,
      skills: sortedById(skills),
      capacity: { current: 2 + rng.integer(3), maximum: PERSISTENT_PERSON_LIMITS.capacityMaximum },
      assignment: { status: 'unassigned' as const },
      availability: 'available' as const
    },
    needs: { nourishment: rng.integer(3), rest: rng.integer(3), shelter: rng.integer(2), safety: rng.integer(2) },
    health: { condition: 'steady' as const, injuries: [], recovery: { status: 'none' as const } },
    possessions: sortedById(member.equipment.map((name, index) => ({ id: `${member.id}:possession:${index}`, ownerPersonId: member.id, name, condition: 'sound' as const, contentSafety: possessionClassification() }))),
    family: [],
    relationships: sortedById(relationshipRecords),
    memories: [{ id: `${member.id}:memory:foundation-history`, kind: 'foundation-history' as const, atWorldTime: 0, detail: member.history, contentSafety: member.historyContentSafety }],
    commitments: []
  }
}))

/** Every free-text/value-bearing persistent-person record joins the state audit. */
export const persistentPersonContentRecords = (people: readonly PersistentPersonRecord[]): readonly ClassifiedMedievalContent[] => people.flatMap(person => [
  content(`persistent-person:${person.id}`, 'person', person.identity.contentSafety),
  ...person.possessions.map(possession => content(`persistent-person:${person.id}:possession:${possession.id}`, 'person', possession.contentSafety)),
  ...person.family.flatMap(link => link.relative.kind === 'known-uninstantiated-person' ? [content(`persistent-person:${person.id}:family:${link.id}`, 'person', link.relative.contentSafety)] : []),
  ...person.relationships.map(relationship => content(`persistent-person:${person.id}:relationship:${relationship.id}`, 'person', relationship.contentSafety)),
  ...person.memories.map(memory => content(`persistent-person:${person.id}:memory:${memory.id}`, 'history', memory.contentSafety)),
  ...person.commitments.map(commitment => content(`persistent-person:${person.id}:commitment:${commitment.id}`, 'contract', commitment.contentSafety)),
  ...person.health.injuries.map(injury => content(`persistent-person:${person.id}:injury:${injury.id}`, 'hazard', injury.contentSafety))
])

const knownFrontierPeople = (frontier: FrontierState): readonly FrontierNamedPersonCommitment[] => frontier.regions.flatMap(region => region.status === 'ungenerated' ? [] : region.namedPeople)

const validClassificationRecord = (id: string, domain: ClassifiedMedievalContent['domain'], classification: unknown): boolean => auditMedievalContentSafety([{ id, domain, classification }]).status === 'accepted'

const validLocation = (value: unknown, context: PersistentPersonValidationContext): value is PersistentPersonLocation => record(value) && hasOnlyKeys(value, ['kind', 'id']) && validId(value.id) && (
  value.kind === 'jomon' ? value.id === context.jomon.id
    : value.kind === 'site' ? context.siteIds.includes(value.id)
      : value.kind === 'frontier-region' ? context.frontier.regions.some(region => region.commitment.id === value.id)
        : false
)

const validKnownReference = (value: unknown, context: PersistentPersonValidationContext): value is KnownUninstantiatedPersonReference => {
  if (!record(value) || !hasOnlyKeys(value, ['kind', 'source', 'sourceId', 'personId', 'name', 'contentSafety']) || value.kind !== 'known-uninstantiated-person' || !validId(value.sourceId) || !validId(value.personId) || typeof value.name !== 'string' || !validClassificationRecord(`known-reference:${value.sourceId}`, 'person', value.contentSafety)) return false
  if (value.source === 'initial-person-seed') {
    const seed = context.initialWorld.people.find(person => person.id === value.sourceId)
    return seed !== undefined && value.personId === seed.id && value.name === seed.name && same(value.contentSafety, seed.contentSafety)
  }
  if (value.source === 'frontier-named-person') {
    const commitment = knownFrontierPeople(context.frontier).find(person => person.id === value.sourceId)
    return commitment !== undefined && value.personId === commitment.futurePersonId && value.name === commitment.name && same(value.contentSafety, commitment.contentSafety)
  }
  return false
}

const validFamily = (value: unknown, personIds: ReadonlySet<string>, context: PersistentPersonValidationContext): value is PersistentPersonFamilyLink => {
  if (!record(value) || !hasOnlyKeys(value, ['id', 'relation', 'relative']) || !validId(value.id) || !oneOf(familyRelations, value.relation) || !record(value.relative)) return false
  if (value.relative.kind === 'instantiated-person') return hasOnlyKeys(value.relative, ['kind', 'personId']) && validId(value.relative.personId) && value.relative.personId !== undefined && personIds.has(value.relative.personId)
  return validKnownReference(value.relative, context)
}

const validRelationship = (value: unknown, personId: string, personIds: ReadonlySet<string>): value is PersistentPersonRelationship => record(value) && hasOnlyKeys(value, ['id', 'targetPersonId', 'reciprocalId', 'standing', 'basis', 'contentSafety']) && validId(value.id) && validId(value.targetPersonId) && value.targetPersonId !== personId && personIds.has(value.targetPersonId) && validId(value.reciprocalId) && [-2, -1, 0, 1, 2].includes(value.standing as number) && oneOf(relationshipBases, value.basis) && validClassificationRecord(`relationship:${value.id}`, 'person', value.contentSafety)

const validRecordShape = (value: unknown): value is PersistentPersonRecord => record(value) && hasOnlyKeys(value, ['version', 'id', 'origin', 'sourceCrewId', 'identity', 'life', 'location', 'work', 'needs', 'health', 'possessions', 'family', 'relationships', 'memories', 'commitments']) && validId(value.id)

const validCommitment = (value: unknown, worldTime: number): value is PersistentPersonCommitment => {
  if (!record(value) || !validId(value.id) || !oneOf(commitmentKinds, value.kind) || !oneOf(commitmentStatuses, value.status) || !nonNegativeInteger(value.createdAtWorldTime) || value.createdAtWorldTime > worldTime || typeof value.detail !== 'string' || !validClassificationRecord(`commitment:${value.id}`, 'contract', value.contentSafety)) return false
  if (value.status === 'active') return hasOnlyKeys(value, ['id', 'kind', 'status', 'createdAtWorldTime', 'detail', 'contentSafety'])
  return hasOnlyKeys(value, ['id', 'kind', 'status', 'createdAtWorldTime', 'resolvedAtWorldTime', 'detail', 'contentSafety']) && nonNegativeInteger(value.resolvedAtWorldTime) && value.resolvedAtWorldTime >= value.createdAtWorldTime
}

const validatePerson = (candidate: PersistentPersonRecord, context: PersistentPersonValidationContext, personIds: ReadonlySet<string>, allPeople: readonly PersistentPersonRecord[], issues: PersistentPersonValidationIssue[]): void => {
  const id = candidate.id
  const source = context.crew.find(member => member.id === candidate.sourceCrewId)
  if (candidate.version !== PERSISTENT_PERSON_CONTRACT_VERSION) issues.push(issue(id, 'persistent-person.invalid-version'))
  if (candidate.origin !== 'foundation-crew') issues.push(issue(id, 'persistent-person.invalid-origin'))
  if (!source || candidate.sourceCrewId !== candidate.id) issues.push(issue(id, 'persistent-person.invalid-source'))
  if (!record(candidate.identity) || !hasOnlyKeys(candidate.identity, ['name', 'adult', 'conversation', 'contentSafety']) || candidate.identity.adult !== true || typeof candidate.identity.name !== 'string' || !boundedInteger(candidate.identity.conversation, 5) || candidate.identity.conversation < 1 || !validClassificationRecord(`person:${id}`, 'person', candidate.identity.contentSafety) || (source !== undefined && (candidate.identity.name !== source.name || candidate.identity.conversation !== source.conversation || !same(candidate.identity.contentSafety, source.contentSafety)))) issues.push(issue(id, 'persistent-person.invalid-identity'))

  const life = candidate.life
  if (!record(life) || !hasOnlyKeys(life, ['status', 'birth', 'death']) || (life.status !== 'living' && life.status !== 'dead') || !record(life.birth) || !hasOnlyKeys(life.birth, ['yearsBeforeWorldCreation']) || !nonNegativeInteger(life.birth.yearsBeforeWorldCreation) || life.birth.yearsBeforeWorldCreation < PERSISTENT_PERSON_LIMITS.minimumAdultYears || life.birth.yearsBeforeWorldCreation > PERSISTENT_PERSON_LIMITS.maximumAdultYears || (life.status === 'living' ? life.death !== null : !record(life.death) || !hasOnlyKeys(life.death, ['atWorldTime']) || !nonNegativeInteger(life.death.atWorldTime) || life.death.atWorldTime > context.worldTime)) issues.push(issue(id, 'persistent-person.invalid-life'))
  if (!validLocation(candidate.location, context)) issues.push(issue(id, 'persistent-person.invalid-location'))

  const work = candidate.work
  const assignmentValid = record(work) && record(work.assignment) && (
    (hasOnlyKeys(work.assignment, ['status']) && work.assignment.status === 'unassigned') ||
    (hasOnlyKeys(work.assignment, ['status', 'assignmentId', 'assignedAtWorldTime']) && work.assignment.status === 'assigned' && validId(work.assignment.assignmentId) && nonNegativeInteger(work.assignment.assignedAtWorldTime) && work.assignment.assignedAtWorldTime <= context.worldTime)
  )
  const skillsValid = record(work) && Array.isArray(work.skills) && work.skills.length > 0 && work.skills.length <= PERSISTENT_PERSON_LIMITS.skillsPerPerson && isSortedById(work.skills as PersistentPersonSkill[]) && new Set((work.skills as PersistentPersonSkill[]).map(skill => skill.id)).size === work.skills.length && work.skills.every(skill => record(skill) && hasOnlyKeys(skill, ['id', 'kind', 'level']) && validId(skill.id) && oneOf(skillKinds, skill.kind) && [1, 2, 3].includes(skill.level as number))
  const capacityValid = record(work) && record(work.capacity) && hasOnlyKeys(work.capacity, ['current', 'maximum']) && boundedInteger(work.capacity.current, PERSISTENT_PERSON_LIMITS.capacityMaximum) && boundedInteger(work.capacity.maximum, PERSISTENT_PERSON_LIMITS.capacityMaximum) && work.capacity.maximum > 0 && work.capacity.current <= work.capacity.maximum
  if (!record(work) || !hasOnlyKeys(work, ['role', 'skills', 'capacity', 'assignment', 'availability']) || !oneOf(crewRoles, work.role) || (source !== undefined && work.role !== source.role) || !skillsValid || !capacityValid || !assignmentValid || !oneOf(['available', 'committed', 'unavailable'] as const, work.availability) || (work.assignment.status === 'assigned' && work.availability !== 'committed') || (work.assignment.status === 'unassigned' && work.availability === 'committed')) issues.push(issue(id, 'persistent-person.invalid-work'))

  const needs = candidate.needs
  if (!record(needs) || !hasOnlyKeys(needs, ['nourishment', 'rest', 'shelter', 'safety']) || !boundedInteger(needs.nourishment, PERSISTENT_PERSON_LIMITS.needMaximum) || !boundedInteger(needs.rest, PERSISTENT_PERSON_LIMITS.needMaximum) || !boundedInteger(needs.shelter, PERSISTENT_PERSON_LIMITS.needMaximum) || !boundedInteger(needs.safety, PERSISTENT_PERSON_LIMITS.needMaximum)) issues.push(issue(id, 'persistent-person.invalid-needs'))

  const health = candidate.health
  const injuriesValid = record(health) && Array.isArray(health.injuries) && health.injuries.length <= PERSISTENT_PERSON_LIMITS.injuriesPerPerson && isSortedById(health.injuries as PersistentPersonInjury[]) && new Set((health.injuries as PersistentPersonInjury[]).map(injury => injury.id)).size === health.injuries.length && health.injuries.every(injury => record(injury) && hasOnlyKeys(injury, ['id', 'kind', 'receivedAtWorldTime', 'recovery', 'contentSafety']) && validId(injury.id) && oneOf(injuryKinds, injury.kind) && nonNegativeInteger(injury.receivedAtWorldTime) && injury.receivedAtWorldTime <= context.worldTime && oneOf(['none', 'recovering'] as const, injury.recovery) && validClassificationRecord(`injury:${injury.id}`, 'hazard', injury.contentSafety))
  const recovery = record(health) && record(health.recovery) ? health.recovery : undefined
  const recoveryValid = recovery !== undefined && (
    (hasOnlyKeys(recovery, ['status']) && recovery.status === 'none') ||
    (hasOnlyKeys(recovery, ['status', 'injuryId', 'completeAtWorldTime']) && recovery.status === 'recovering' && validId(recovery.injuryId) && nonNegativeInteger(recovery.completeAtWorldTime) && recovery.completeAtWorldTime >= 0 && Array.isArray(health.injuries) && health.injuries.some(injury => record(injury) && injury.id === recovery.injuryId && injury.recovery === 'recovering'))
  )
  if (!record(health) || !hasOnlyKeys(health, ['condition', 'injuries', 'recovery']) || !oneOf(['steady', 'strained', 'injured', 'recovering'] as const, health.condition) || !injuriesValid || !recoveryValid || (health.condition === 'steady' && health.injuries.length !== 0) || (health.condition === 'recovering' && health.recovery.status !== 'recovering') || (health.condition === 'injured' && health.injuries.length === 0)) issues.push(issue(id, 'persistent-person.invalid-health'))

  const possessions = candidate.possessions
  if (!Array.isArray(possessions) || possessions.length > PERSISTENT_PERSON_LIMITS.possessionsPerPerson || !isSortedById(possessions) || new Set(possessions.map(possession => possession.id)).size !== possessions.length || !possessions.every(possession => record(possession) && hasOnlyKeys(possession, ['id', 'ownerPersonId', 'name', 'condition', 'contentSafety']) && validId(possession.id) && possession.ownerPersonId === id && typeof possession.name === 'string' && possession.name.length > 0 && oneOf(possessionConditions, possession.condition) && validClassificationRecord(`possession:${possession.id}`, 'person', possession.contentSafety)) || (source !== undefined && !source.equipment.every((name, index) => possessions.some(possession => possession.id === `${id}:possession:${index}` && possession.name === name)))) issues.push(issue(id, 'persistent-person.invalid-possession'))

  const family = candidate.family
  if (!Array.isArray(family) || family.length > PERSISTENT_PERSON_LIMITS.familyLinksPerPerson || !isSortedById(family) || new Set(family.map(link => link.id)).size !== family.length || !family.every(link => validFamily(link, personIds, context))) issues.push(issue(id, 'persistent-person.invalid-family'))

  const relationships = candidate.relationships
  const sourceRelationships = source?.relationships ?? []
  const reciprocalValid = Array.isArray(relationships) && relationships.every(relationship => {
    const target = allPeople.find(person => person.id === relationship.targetPersonId)
    return target?.relationships.some(candidateRelationship => candidateRelationship.id === relationship.reciprocalId && candidateRelationship.targetPersonId === id && candidateRelationship.reciprocalId === relationship.id) === true
  })
  if (!Array.isArray(relationships) || relationships.length > PERSISTENT_PERSON_LIMITS.relationshipsPerPerson || !isSortedById(relationships) || new Set(relationships.map(relationship => relationship.id)).size !== relationships.length || !relationships.every(relationship => validRelationship(relationship, id, personIds)) || !reciprocalValid || (source !== undefined && (!same(relationships.map(relationship => ({ targetPersonId: relationship.targetPersonId, standing: relationship.standing, basis: relationship.basis })), sourceRelationships.map(relationship => ({ targetPersonId: relationship.personId, standing: relationship.standing, basis: relationship.basis }))))) ) issues.push(issue(id, 'persistent-person.invalid-relationship'))

  const memories = candidate.memories
  if (!Array.isArray(memories) || memories.length > PERSISTENT_PERSON_LIMITS.memoriesPerPerson || !isSortedById(memories) || new Set(memories.map(memory => memory.id)).size !== memories.length || !memories.every(memory => record(memory) && hasOnlyKeys(memory, ['id', 'kind', 'atWorldTime', 'detail', 'contentSafety']) && validId(memory.id) && oneOf(memoryKinds, memory.kind) && nonNegativeInteger(memory.atWorldTime) && memory.atWorldTime <= context.worldTime && typeof memory.detail === 'string' && validClassificationRecord(`memory:${memory.id}`, 'history', memory.contentSafety)) || (source !== undefined && !memories.some(memory => memory.id === `${id}:memory:foundation-history` && memory.kind === 'foundation-history' && memory.atWorldTime === 0 && memory.detail === source.history && same(memory.contentSafety, source.historyContentSafety)))) issues.push(issue(id, 'persistent-person.invalid-memory'))

  const commitments = candidate.commitments
  if (!Array.isArray(commitments) || commitments.length > PERSISTENT_PERSON_LIMITS.commitmentsPerPerson || !isSortedById(commitments) || new Set(commitments.map(commitment => commitment.id)).size !== commitments.length || !commitments.every(commitment => validCommitment(commitment, context.worldTime))) issues.push(issue(id, 'persistent-person.invalid-commitment'))

  if (life?.status === 'dead' && (work?.availability !== 'unavailable' || work?.assignment?.status !== 'unassigned' || commitments?.some(commitment => commitment.status === 'active'))) issues.push(issue(id, 'persistent-person.dead-restriction'))
}

/** Pure, fail-closed validation for the bounded mutable person registry. */
export const validatePersistentPeople = (context: PersistentPersonValidationContext, value: unknown): readonly PersistentPersonValidationIssue[] => {
  const issues: PersistentPersonValidationIssue[] = []
  if (!Array.isArray(value)) return [issue('persistent-people', 'persistent-person.malformed-record')]
  if (value.length > PERSISTENT_PERSON_LIMITS.records) issues.push(issue('persistent-people', 'persistent-person.budget-exceeded'))
  const people = value.filter(validRecordShape) as PersistentPersonRecord[]
  value.forEach((candidate, index) => { if (!validRecordShape(candidate)) issues.push(issue(`persistent-people:${index}`, 'persistent-person.malformed-record')) })
  if (new Set(people.map(person => person.id)).size !== people.length) issues.push(issue('persistent-people', 'persistent-person.duplicate-id'))
  if (!isSortedById(people)) issues.push(issue('persistent-people', 'persistent-person.noncanonical-order'))
  const personIds = new Set(people.map(person => person.id))
  for (const crewMember of context.crew) if (!personIds.has(crewMember.id)) issues.push(issue(crewMember.id, 'persistent-person.missing-crew-person'))
  for (const person of people) validatePerson(person, context, personIds, people, issues)
  try {
    const safety = auditMedievalContentSafety(persistentPersonContentRecords(people))
    if (safety.status === 'rejected') issues.push(...safety.diagnostics.map(diagnostic => issue(diagnostic.contentId, diagnostic.code)))
  } catch {
    issues.push(issue('persistent-people', 'persistent-person.invalid-content'))
  }
  return canonicalIssues(issues)
}

export class PersistentPersonContractError extends Error {
  constructor(readonly diagnostics: readonly PersistentPersonValidationIssue[]) {
    super(`persistent people rejected: ${diagnostics.map(diagnostic => diagnostic.code).join(', ')}`)
    this.name = 'PersistentPersonContractError'
  }
}
