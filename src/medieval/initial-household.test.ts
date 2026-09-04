import { describe, expect, it } from 'vitest'
import { classifyMedievalContent } from './content-safety'
import { generationConfigurationFingerprint, resolveWorldGenerationConfig } from './generation-config'
import { INITIAL_HOUSEHOLD_EQUIPMENT_BY_ROLE, INITIAL_HOUSEHOLD_HISTORY_BY_ROLE, INITIAL_HOUSEHOLD_ROSTER_SIZE, createInitialHousehold, initialHouseholdActiveCrew, initialHouseholdRelationshipId, validateInitialHousehold, validateInitialHouseholdRoster, validateInitialHouseholdStructure } from './initial-household'
import { createFoundationWorld } from './world'

const resolved = (preset: 'sheltered-reach' | 'watershed' | 'far-coast' = 'watershed') => {
  const resolution = resolveWorldGenerationConfig({ preset })
  if (resolution.status !== 'valid') throw new Error('test configuration must resolve')
  return resolution.configuration
}

const contextFor = (seed = 'household-contract', preset: 'sheltered-reach' | 'watershed' | 'far-coast' = 'watershed') => {
  const configuration = resolved(preset)
  return { seed, configuration, configurationFingerprint: generationConfigurationFingerprint(configuration) }
}

describe('initial household contract', () => {
  it('recreates the same immutable roster and selection-ready active crew from a normalized seed and resolved configuration', () => {
    const context = contextFor('household reproducibility')
    const first = createInitialHousehold(context)
    const second = createInitialHousehold(context)

    expect(second).toEqual(first)
    expect(validateInitialHousehold({ seed: context.seed, configurationFingerprint: context.configurationFingerprint }, first)).toEqual([])
    expect(first.activeCrew).toEqual(initialHouseholdActiveCrew(first.roster))
    expect(first.activeCrew.map(member => member.id)).toEqual(first.roster.map(member => member.id))
  })

  it('uses the existing seed/configuration stream without an individual reroll seam', () => {
    const baseline = createInitialHousehold(contextFor('household stream', 'watershed'))
    const changedSeed = createInitialHousehold(contextFor('household stream changed', 'watershed'))
    const changedConfiguration = createInitialHousehold(contextFor('household stream', 'far-coast'))

    expect(changedSeed.roster).not.toEqual(baseline.roster)
    expect(changedConfiguration.roster).not.toEqual(baseline.roster)
  })

  it('enforces canonical IDs/order, roles, role-owned values, directional reciprocal links, and static eligibility', () => {
    const household = createInitialHousehold(contextFor('household invariants'))

    expect(household.roster).toHaveLength(INITIAL_HOUSEHOLD_ROSTER_SIZE)
    expect(household.roster.map(member => member.id)).toEqual(['crew:0', 'crew:1', 'crew:2', 'crew:3', 'crew:4', 'crew:5'])
    expect(new Set(household.roster.map(member => member.name)).size).toBe(INITIAL_HOUSEHOLD_ROSTER_SIZE)
    expect(new Set(household.roster.map(member => member.role)).size).toBe(INITIAL_HOUSEHOLD_ROSTER_SIZE)
    expect(household.roster.every(member => member.eligible && Number.isInteger(member.conversation) && member.conversation >= 1 && member.conversation <= 5)).toBe(true)
    for (const member of household.roster) {
      expect(member.equipment).toEqual(INITIAL_HOUSEHOLD_EQUIPMENT_BY_ROLE[member.role])
      expect(INITIAL_HOUSEHOLD_HISTORY_BY_ROLE[member.role]).toContain(member.history)
      expect(member.relationships.map(link => link.personId)).toEqual(household.roster.filter(other => other.id !== member.id).map(other => other.id))
      for (const relationship of member.relationships) {
        expect(initialHouseholdRelationshipId(member.id, relationship.personId)).toBe(`${member.id}:relationship:${relationship.personId}`)
        expect(household.roster.find(other => other.id === relationship.personId)?.relationships.some(link => link.personId === member.id)).toBe(true)
      }
    }

    // Links are directional. Reciprocal presence is required, but standing/basis
    // are not silently normalized or forced to match in both directions.
    const directional = structuredClone(household.roster)
    const firstLink = directional[0]!.relationships[0]!
    const reciprocal = directional.find(member => member.id === firstLink.personId)!.relationships.find(link => link.personId === directional[0]!.id)!
    firstLink.basis = reciprocal.basis === 'debt' ? 'work' : 'debt'
    expect(firstLink.basis).not.toBe(reciprocal.basis)
    expect(validateInitialHouseholdStructure(directional)).toEqual([])
    expect(validateInitialHouseholdRoster({ seed: 'household invariants', configurationFingerprint: contextFor('household invariants').configurationFingerprint }, directional).map(issue => issue.code)).toEqual(['initial-household.non-reproducible-roster'])
  })

  it('fails closed with stable diagnostics for structural, context, reproducibility, and player-facing safety tampering', () => {
    const context = contextFor('household rejection')
    const household = createInitialHousehold(context)
    const malformed = [...structuredClone(household.roster)]
    malformed.pop()
    const changedName = structuredClone(household.roster)
    changedName[0]!.name = 'Other Name'
    const unsafe = structuredClone(household.roster)
    ;(unsafe[0]!.historyContentSafety.exclusions as unknown as Record<string, string>).torture = 'present'
    const unclassified = structuredClone(household.roster)
    unclassified[0]!.contentSafety = undefined as never
    const unexpectedMemberField = structuredClone(household.roster)
    ;(unexpectedMemberField[0] as unknown as Record<string, unknown>).unexpected = true
    const missingMemberField = structuredClone(household.roster)
    delete (missingMemberField[0] as unknown as Record<string, unknown>).history
    const unexpectedRelationshipField = structuredClone(household.roster)
    ;(unexpectedRelationshipField[0]!.relationships[0] as unknown as Record<string, unknown>).unexpected = true

    expect(validateInitialHouseholdStructure(malformed).map(issue => issue.code)).toContain('initial-household.invalid-roster-size')
    expect(validateInitialHouseholdRoster({ seed: 'other household rejection', configurationFingerprint: context.configurationFingerprint }, household.roster).map(issue => issue.code)).toEqual(['initial-household.non-reproducible-roster'])
    expect(validateInitialHouseholdRoster({ seed: context.seed, configurationFingerprint: contextFor(context.seed, 'far-coast').configurationFingerprint }, household.roster).map(issue => issue.code)).toEqual(['initial-household.non-reproducible-roster'])
    expect(validateInitialHouseholdRoster({ seed: ' not normalized ', configurationFingerprint: context.configurationFingerprint }, household.roster).map(issue => issue.code)).toEqual(['initial-household.invalid-context'])
    expect(validateInitialHouseholdRoster({ seed: context.seed, configurationFingerprint: context.configurationFingerprint }, changedName).map(issue => issue.code)).toEqual(['initial-household.non-reproducible-roster'])
    expect(validateInitialHouseholdStructure(unsafe).map(issue => issue.code)).toContain('content-safety.prohibited.torture')
    expect(validateInitialHouseholdStructure(unclassified).map(issue => issue.code)).toContain('content-safety.missing-classification')
    expect(validateInitialHouseholdStructure(unexpectedMemberField).map(issue => issue.code)).toContain('initial-household.malformed-household')
    expect(validateInitialHouseholdStructure(missingMemberField).map(issue => issue.code)).toContain('initial-household.malformed-household')
    expect(validateInitialHouseholdStructure(unexpectedRelationshipField).map(issue => issue.code)).toContain('initial-household.invalid-relationship')
  })

  it('is a zero-time, no-hidden-data projection and does not select or mutate a courier', () => {
    const world = createFoundationWorld({ seed: 'household zero time' })
    const before = structuredClone(world)
    const activeCrew = initialHouseholdActiveCrew(world.crew)

    expect(world).toEqual(before)
    expect(world.state.temporal).toMatchObject({ worldTime: 0, actionSequence: 0, pendingEvents: [], causalRecords: [] })
    expect(world.state.courier.initialCourierId).toBeUndefined()
    expect(world.state.navigation).toEqual({ version: 1 })
    expect(activeCrew).toEqual(world.crew.map(({ id, name, role, conversation }) => ({ id, name, role, conversation })))
    expect(activeCrew.every(member => Object.keys(member).sort().join(',') === 'conversation,id,name,role')).toBe(true)
    expect(JSON.stringify(activeCrew)).not.toContain('initial:')
    expect(JSON.stringify(activeCrew)).not.toContain('frontier:')
    expect(JSON.stringify(activeCrew)).not.toContain('browser')
  })

  it('keeps the existing selection content scope affirmative and auditable', () => {
    const household = createInitialHousehold(contextFor('household audit'))
    expect(household.roster.every(member => member.contentSafety.participantScope === 'adults-only' && member.historyContentSafety.participantScope === 'adults-only')).toBe(true)
    expect(classifyMedievalContent('person', ['adult-labour'], 'adults-only', ['player-facing-text']).exclusions).toEqual(household.roster[0]!.contentSafety.exclusions)
  })
})
