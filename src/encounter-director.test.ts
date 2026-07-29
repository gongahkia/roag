import { describe, expect, it } from 'vitest'
import { definitionForEncounter, encounterPlansFor, membersForEncounter } from './encounter-director'
import type { MonsterDefinition } from './content'
import { TACTICAL_ENCOUNTERS } from './types'

const monster = (id: string, role: NonNullable<MonsterDefinition['role']>, terrainAffinity: MonsterDefinition['terrainAffinity'] = []): MonsterDefinition => ({ id, name: id, glyph: 'm', color: '#fff', health: 1, attack: 1, defense: 1, speed: 100, ai: 'chase', xp: 1, biome: 'mine', role, terrainAffinity })

describe('encounter director', () => {
  it('schedules distinct directed groups with explicit counterplay', () => {
    const archetypes = new Set(Array.from({ length: TACTICAL_ENCOUNTERS.length }, (_, routePosition) => encounterPlansFor({ biome: 'mine', areaFloor: 2, routePosition, pilot: true }, ['rail'])[0].archetype))
    expect(archetypes).toEqual(new Set(TACTICAL_ENCOUNTERS))
    const plans = encounterPlansFor({ biome: 'mine', areaFloor: 2, routePosition: 0, pilot: true }, ['rail'])
    expect(plans).toHaveLength(2)
    expect(plans.every(plan => plan.answer.length > 0 && plan.roles.length > 0)).toBe(true)
    expect(membersForEncounter(0, 0)).toBe(2)
    expect(membersForEncounter(2, 0)).toBe(3)
  })

  it('prefers authored native roles on the matching terrain', () => {
    const nativePlan = encounterPlansFor({ biome: 'mine', areaFloor: 2, routePosition: 2, pilot: true }, ['rail'])[0]
    expect(nativePlan.archetype).toBe('nativeTerrainPack')
    const definition = definitionForEncounter([monster('rail-pursuer', 'pursuer', ['rail']), monster('plain-pursuer', 'pursuer')], nativePlan, 0, ['rail'])
    expect(definition?.id).toBe('rail-pursuer')
  })
})
