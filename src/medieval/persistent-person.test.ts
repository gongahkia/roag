import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { revealNamedFrontierPerson } from './frontier'
import { PERSISTENT_PERSON_CONTRACT_VERSION, PERSISTENT_PERSON_LIMITS, instantiateFoundationCrewPeople, persistentPersonContentRecords, validatePersistentPeople, type PersistentPersonValidationContext } from './persistent-person'
import { chooseInitialCourier, createFoundationWorld, recreateFoundationWorld } from './world'

const contextFor = (world: ReturnType<typeof createFoundationWorld>): PersistentPersonValidationContext => ({
  seed: world.manifest.creation.seed,
  configurationFingerprint: world.manifest.creation.configurationFingerprint,
  initialWorld: world.initialWorld,
  frontier: world.state.geography.frontier,
  jomon: world.jomon,
  crew: world.crew,
  siteIds: world.state.sites.sites.map(site => site.id),
  worldTime: world.state.temporal.worldTime
})

const codes = (context: PersistentPersonValidationContext, people: unknown): readonly string[] => validatePersistentPeople(context, people).map(issue => issue.code)

describe('persistent medieval people', () => {
  it('instantiates exactly the deterministic foundation crew and reconstructs it from immutable provenance', () => {
    const first = createFoundationWorld({ seed: 'persistent crew' })
    const second = createFoundationWorld({ seed: 'persistent crew' })
    const restored = recreateFoundationWorld(first.manifest)

    expect(first.state.people.version).toBe(2)
    expect(first.state.people.records).toEqual(second.state.people.records)
    expect(restored.state.people.records).toEqual(first.state.people.records)
    expect(instantiateFoundationCrewPeople(contextFor(first))).toEqual(first.state.people.records)
    expect(first.state.people.records.map(person => person.id)).toEqual(first.crew.map(member => member.id).sort())
    expect(first.state.people.records).toHaveLength(first.crew.length)
    expect(first.state.people.records).toHaveLength(6)
    expect(first.state.people.records.every(person => person.origin === 'foundation-crew' && person.location.id === 'vessel:jomon')).toBe(true)
  })

  it('preserves each crew member’s name, role, conversation, history, relationships, and equipment in complete records', () => {
    const world = createFoundationWorld({ seed: 'persistent fields' })

    for (const crew of world.crew) {
      const person = world.state.people.records.find(candidate => candidate.id === crew.id)
      expect(person).toBeDefined()
      expect(person).toMatchObject({
        version: PERSISTENT_PERSON_CONTRACT_VERSION,
        sourceCrewId: crew.id,
        identity: { name: crew.name, adult: true, conversation: crew.conversation },
        work: { role: crew.role, assignment: { status: 'unassigned' }, availability: 'available' },
        health: { condition: 'steady', injuries: [], recovery: { status: 'none' } },
        family: [],
        commitments: []
      })
      expect(person?.possessions.map(possession => possession.name)).toEqual(crew.equipment)
      expect(person?.memories).toContainEqual(expect.objectContaining({ kind: 'foundation-history', detail: crew.history }))
      expect(person?.relationships.map(relationship => ({ personId: relationship.targetPersonId, standing: relationship.standing, basis: relationship.basis }))).toEqual(crew.relationships)
      expect(person?.life.birth.yearsBeforeWorldCreation).toBeGreaterThanOrEqual(PERSISTENT_PERSON_LIMITS.minimumAdultYears)
      expect(person?.life.birth.yearsBeforeWorldCreation).toBeLessThanOrEqual(PERSISTENT_PERSON_LIMITS.maximumAdultYears)
    }
    expect(validatePersistentPeople(contextFor(world), world.state.people.records)).toEqual([])
  })

  it('keeps uninstantiated initial seeds and frontier commitments outside the mutable registry while allowing a typed known-family reference', () => {
    const world = createFoundationWorld({ seed: 'known relatives' })
    const people = structuredClone(world.state.people.records)
    const initialSeed = world.initialWorld.people[0]!
    people[0]!.family = [{
      id: `${people[0]!.id}:family:seed`,
      relation: 'kin',
      relative: {
        kind: 'known-uninstantiated-person',
        source: 'initial-person-seed',
        sourceId: initialSeed.id,
        personId: initialSeed.id,
        name: initialSeed.name,
        contentSafety: initialSeed.contentSafety
      }
    }]

    expect(world.state.people.records.some(person => world.initialWorld.people.some(seed => seed.id === person.id))).toBe(false)
    expect(world.state.people.records.some(person => world.state.geography.frontier.regions.some(region => region.status !== 'ungenerated' && region.namedPeople.some(commitment => commitment.futurePersonId === person.id)))).toBe(false)
    expect(validatePersistentPeople(contextFor(world), people)).toEqual([])
  })

  it('keeps a revealed frontier identity non-mutable until its later materialization trigger', () => {
    const world = createFoundationWorld({ seed: 'frontier relative' })
    const regionalContext = { seed: world.manifest.creation.seed, configuration: world.manifest.creation.resolvedConfiguration, initialWorld: world.initialWorld }
    const root = world.state.geography.frontier.regions.find(region => region.status === 'known-but-unvisited')!
    const frontier = revealNamedFrontierPerson(regionalContext, world.state.geography.frontier, root.commitment.id, 'letter:known-relative', {
      kind: 'letter',
      sourceRecordId: root.commitment.anchor.historyEventId,
      reportedAtWorldTime: 0,
      freshnessAtWorldTime: 0
    })
    const named = frontier.regions.find(region => region.commitment.id === root.commitment.id)
    if (!named || named.status === 'ungenerated') throw new Error('frontier name should become known')
    const commitment = named.namedPeople[0]!
    const people = structuredClone(world.state.people.records)
    people[0]!.family = [{
      id: `${people[0]!.id}:family:frontier`,
      relation: 'kin',
      relative: {
        kind: 'known-uninstantiated-person',
        source: 'frontier-named-person',
        sourceId: commitment.id,
        personId: commitment.futurePersonId,
        name: commitment.name,
        contentSafety: commitment.contentSafety
      }
    }]
    const context = { ...contextFor(world), frontier }

    expect(people.some(person => person.id === commitment.futurePersonId)).toBe(false)
    expect(validatePersistentPeople(context, people)).toEqual([])
  })

  it('rejects incomplete records, bad locations, possessions, references, and noncanonical registry order', () => {
    const world = createFoundationWorld({ seed: 'person rejection' })
    const context = contextFor(world)
    const missingNeeds = structuredClone(world.state.people.records) as unknown as { needs?: unknown }[]
    delete missingNeeds[0]!.needs
    const badLocation = structuredClone(world.state.people.records)
    badLocation[0]!.location = { kind: 'site', id: 'site:not-real' }
    const badPossession = structuredClone(world.state.people.records)
    badPossession[0]!.possessions[0]!.ownerPersonId = 'crew:not-owner'
    const badFamily = structuredClone(world.state.people.records)
    badFamily[0]!.family = [{ id: 'family:bad', relation: 'kin', relative: { kind: 'instantiated-person', personId: 'person:not-real' } }]
    const reversed = [...structuredClone(world.state.people.records)].reverse()

    expect(codes(context, missingNeeds)).toContain('persistent-person.malformed-record')
    expect(codes(context, badLocation)).toContain('persistent-person.invalid-location')
    expect(codes(context, badPossession)).toContain('persistent-person.invalid-possession')
    expect(codes(context, badFamily)).toContain('persistent-person.invalid-family')
    expect(codes(context, reversed)).toContain('persistent-person.noncanonical-order')
  })

  it('preserves dead people as historical records but rejects active courier/work/commitment state', () => {
    const world = createFoundationWorld({ seed: 'dead record' })
    const context = contextFor(world)
    const dead = structuredClone(world.state.people.records)
    dead[0]!.life = { status: 'dead', birth: dead[0]!.life.birth, death: { atWorldTime: 0 } }
    dead[0]!.work.availability = 'unavailable'

    expect(validatePersistentPeople(context, dead)).toEqual([])
    dead[0]!.work.availability = 'available'
    expect(codes(context, dead)).toContain('persistent-person.dead-restriction')
    dead[0]!.work.availability = 'unavailable'
    dead[0]!.commitments = [{
      id: 'commitment:dead',
      kind: 'vessel-duty',
      status: 'active',
      createdAtWorldTime: 0,
      detail: 'An active duty.',
      contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['person'])
    }]
    expect(codes(context, dead)).toContain('persistent-person.dead-restriction')

    const noCourier = createFoundationWorld({ seed: 'dead courier' })
    const unavailable = structuredClone(noCourier)
    unavailable.state.people.records[0]!.life = { status: 'dead', birth: unavailable.state.people.records[0]!.life.birth, death: { atWorldTime: 0 } }
    unavailable.state.people.records[0]!.work.availability = 'unavailable'
    expect(() => chooseInitialCourier(unavailable, unavailable.state.people.records[0]!.id)).toThrow('eligible living available')
  })

  it('fails closed when persistent text is unsafe or unclassified, and includes every classified value in the safety audit', () => {
    const world = createFoundationWorld({ seed: 'person safety' })
    const unsafe = structuredClone(world.state.people.records)
    ;(unsafe[0]!.memories[0]!.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const unclassified = structuredClone(world.state.people.records)
    unclassified[0]!.possessions[0]!.contentSafety = undefined as never

    expect(codes(contextFor(world), unsafe)).toContain('content-safety.prohibited.torture')
    expect(codes(contextFor(world), unclassified)).toContain('content-safety.missing-classification')
    expect(persistentPersonContentRecords(world.state.people.records).length).toBeGreaterThan(world.state.people.records.length)
  })
})
