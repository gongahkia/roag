import { describe, expect, it } from 'vitest'
import { assessCourierConversation } from './conversation'
import { classifyMedievalContent } from './content-safety'
import { DELEGATION_CONTRACT_VERSION, DELEGATION_TASK_DEFINITIONS, type DelegationOfferInput } from './delegation'
import { MANAGEMENT_SIDEBAR_LIMITS, MANAGEMENT_SIDEBAR_SECTION_IDS, createManagementSidebarModel, managementSidebarAccessibleSummary, managementSidebarFreshnessLabel, validateManagementSidebarModel } from './management-sidebar'
import { advanceFoundationWorldTime, chooseInitialCourier, createFoundationWorld, moveFoundationWorldCourier, offerFoundationWorldDelegatedTask } from './world'

const selectedWorld = (seed: string, preset: 'sheltered-reach' | 'watershed' | 'far-coast' = 'sheltered-reach') => chooseInitialCourier(createFoundationWorld({ seed, configuration: { preset } }), 'crew:0')
const safety = () => classifyMedievalContent('event', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
const wait = (id: string, durationMinutes: number) => ({ id, kind: 'wait' as const, durationMinutes, contentSafety: safety() })

const taskOfferFor = (world: ReturnType<typeof selectedWorld>): DelegationOfferInput => {
  const courierId = world.state.courier.initialCourierId!
  for (const recipient of world.state.people.records.filter(person => person.id !== courierId)) {
    for (const definition of DELEGATION_TASK_DEFINITIONS) {
      const materialInterest = definition.relevantMaterialInterests.find(interest => recipient.materialInterests.includes(interest))
      if (!materialInterest || !definition.relevantSkills.some(skill => recipient.work.skills.some(candidate => candidate.kind === skill))) continue
      const proposal = {
        version: DELEGATION_CONTRACT_VERSION,
        kind: 'request' as const,
        urgency: 'routine' as const,
        complexity: 'routine' as const,
        materialInterest,
        contentSafety: classifyMedievalContent('contract', ['adult-labour', 'civil-life'], 'adults-only', ['data'])
      }
      const assessment = assessCourierConversation(world, { version: DELEGATION_CONTRACT_VERSION, courierId, recipientId: recipient.id, proposal })
      if (assessment.eligibility === 'eligible' && assessment.unlockedApproaches.includes('direct-request')) {
        return { version: DELEGATION_CONTRACT_VERSION, id: `sidebar:offer:${recipient.id}:${definition.family}`, courierId, recipientId: recipient.id, family: definition.family, approach: 'direct-request', proposal }
      }
    }
  }
  throw new Error('sidebar fixture did not find a valid task offer')
}

const allFacts = (model: ReturnType<typeof createManagementSidebarModel>) => model.sections.flatMap(section => section.facts)

describe('management sidebar projection', () => {
  it('is deterministic, canonical, bounded, and carries source/discovery/freshness metadata on every fact', () => {
    const first = createManagementSidebarModel(selectedWorld('management-canonical'))
    const second = createManagementSidebarModel(selectedWorld('management-canonical'))

    expect(second).toEqual(first)
    expect(first.version).toBe(1)
    expect(first.sections.map(section => section.id)).toEqual(MANAGEMENT_SIDEBAR_SECTION_IDS)
    expect(first.sections.every(section => section.facts.length <= MANAGEMENT_SIDEBAR_LIMITS.factsPerSection)).toBe(true)
    expect(first.summary.sectionItemCounts).toEqual(first.sections.map(section => ({ sectionId: section.id, count: section.facts.length })))
    expect(first.contentSafetyAudit.status).toBe('accepted')
    for (const item of allFacts(first)) {
      expect(item.id).toMatch(/^management:/)
      expect(item.category).toBeTruthy()
      expect(item.source.type).toBeTruthy()
      expect(item.source.label).toBeTruthy()
      expect(item.source.recordId).toBeTruthy()
      expect(item.recordedAtWorldTime).toBeGreaterThanOrEqual(0)
      expect(item.discoveredAtWorldTime).toBeGreaterThanOrEqual(0)
      expect(managementSidebarFreshnessLabel(item.freshness)).toMatch(/CURRENT|TIMELESS|FRESH \d+M/)
      expect(item.contentSafety.policyVersion).toBe(1)
    }
    expect(managementSidebarAccessibleSummary(first, 'overview', true)).toMatch(/expanded.*OVERVIEW.*household-known facts/u)
    expect(managementSidebarAccessibleSummary(first, 'overview', false)).toMatch(/collapsed/u)
  })

  it('covers current household people/work, created tasks, risks, and bounded causal history without changing the world', () => {
    const initial = selectedWorld('management-current-state')
    const advanced = advanceFoundationWorldTime(initial, wait('wait:sidebar-observations', 30))
    const world = offerFoundationWorldDelegatedTask(advanced, taskOfferFor(advanced))
    const before = structuredClone(world)
    const model = createManagementSidebarModel(world)

    expect(model.sections.find(section => section.id === 'people-work')?.facts).toHaveLength(world.state.people.records.length)
    expect(model.sections.find(section => section.id === 'tasks')?.facts.some(item => item.value.kind === 'delegated-task')).toBe(true)
    expect(model.sections.find(section => section.id === 'risks')?.facts.length).toBeGreaterThan(0)
    expect(model.sections.find(section => section.id === 'history')?.facts.some(item => item.value.kind === 'causal-command')).toBe(true)
    expect(model.sections.find(section => section.id === 'history')?.facts.some(item => item.value.kind === 'social-memory' && item.source.label === 'social-memory' && item.freshness.kind === 'timeless')).toBe(true)
    expect(model.sections.find(section => section.id === 'people-work')?.facts.some(item => item.value.kind === 'household-person-work' && item.value.autonomyChoice !== undefined)).toBe(true)
    expect(world).toEqual(before)
  })

  it('accepts current facts recorded after their initial discovery', () => {
    const advanced = advanceFoundationWorldTime(selectedWorld('management-current-freshness'), wait('wait:sidebar-current-freshness', 1))
    const result = moveFoundationWorldCourier(advanced, 'north')
    if (result.status !== 'moved') throw new Error('expected a canonical movement step')
    const world = result.world
    const model = createManagementSidebarModel(world)

    expect(model.sections.flatMap(section => section.facts).some(fact => fact.freshness.kind === 'current' && fact.recordedAtWorldTime > fact.discoveredAtWorldTime)).toBe(true)
    expect(validateManagementSidebarModel(world, model)).toEqual([])
  })

  it('shows existing revealed frontier knowledge only and omits generated but undiscovered places, people, institutions, routes, counts, and commitments', () => {
    const world = selectedWorld('management-known-only', 'far-coast')
    const model = createManagementSidebarModel(world)
    const facts = model.sections.find(section => section.id === 'known-sites-routes')!.facts
    const revealed = world.state.geography.frontier.regions.filter(region => region.status !== 'ungenerated').flatMap(region => region.revealedFacts)
    const encoded = JSON.stringify(model)
    const ungeneratedCommitments = world.state.geography.frontier.regions.filter(region => region.status === 'ungenerated').map(region => region.commitment.id)

    expect(facts.filter(item => item.value.kind === 'frontier-revealed-fact').map(item => item.id)).toEqual(revealed.map(item => `management:known:${item.id}`).sort())
    expect(facts.every(item => item.source.type === 'frontier-knowledge')).toBe(true)
    for (const id of ungeneratedCommitments) expect(encoded).not.toContain(id)
    for (const id of [
      ...world.initialWorld.settlements.filter(site => site.id !== world.state.jomon.location.id).map(site => site.id),
      world.initialWorld.people[0]!.id,
      world.initialWorld.institutions[0]!.id,
      world.initialWorld.routes[0]!.id,
      world.state.markets.markets[0]!.id
    ]) expect(encoded).not.toContain(id)
    expect(encoded).not.toContain('adultHouseholdBand')
    expect(encoded).not.toContain('anonymousRoles')
  })

  it('rejects malformed or unsafe full-world input and forged sidebar models fail closed', () => {
    const world = selectedWorld('management-validation')
    const unsafe = structuredClone(world)
    ;(unsafe.state.people.records[0]!.identity.contentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const model = createManagementSidebarModel(world)
    const forged = structuredClone(model)
    forged.sections[0]!.facts = [...forged.sections[0]!.facts].reverse()

    expect(() => createManagementSidebarModel(unsafe)).toThrow('management sidebar rejected')
    expect(validateManagementSidebarModel(world, forged).map(diagnostic => diagnostic.code)).toContain('management-sidebar.invalid-model')
    expect(validateManagementSidebarModel(unsafe, model).map(diagnostic => diagnostic.code)).toContain('management-sidebar.invalid-world')
  })
})
