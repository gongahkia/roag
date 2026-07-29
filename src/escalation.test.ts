import { describe, expect, it } from 'vitest'
import { newRun } from './engine/run'
import { descend } from './engine/inventory'
import { escalationFor } from './escalation'
import type { Biome } from './types'
import { generateAreaFloor, routeContractDebug, tacticalEncounterDebug, validateGeneration } from './world'

const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']

describe('biome expedition escalation', () => {
  it('selects deterministic, materially distinct arcs for every biome', () => {
    for (const biome of biomes) {
      const arcs = new Set(Array.from({ length: 32 }, (_, seed) => escalationFor(seed + 1, biome, 0).arcId))
      expect(arcs.size).toBeGreaterThan(1)
    }
    const first = Array.from({ length: 4 }, (_, areaFloor) => generateAreaFloor(73, 'mine', areaFloor))
    const replay = generateAreaFloor(73, 'mine', 0)
    expect(JSON.stringify(first[0])).toBe(JSON.stringify(replay))
    expect(first.map(floor => floor.escalation?.phase)).toEqual(['survey', 'pressure', 'counterroute', 'climax'])
    expect(first.every(floor => validateGeneration(floor).valid)).toBe(true)
    expect(first[3].objective.label).toContain(first[3].escalation!.payoff)
    expect(first[3].actors.some(actor => actor.role === 'guardian' && actor.status?.includes(`arc:${first[3].escalation!.arcId}:resolved`))).toBe(true)
    expect(first.every(floor => routeContractDebug(floor)?.escalationVariant === `${floor.escalation!.arcId}:${floor.escalation!.phase}`)).toBe(true)
  }, 90_000)

  it('changes encounter direction and ecology with the selected arc', () => {
    const seed = Array.from({ length: 32 }, (_, index) => index + 1).find(candidate => escalationFor(candidate, 'mine', 0).arcId !== escalationFor(candidate + 1, 'mine', 0).arcId)!
    const first = generateAreaFloor(seed, 'mine', 1)
    const second = generateAreaFloor(seed + 1, 'mine', 1)
    expect(first.escalation?.arcId).not.toBe(second.escalation?.arcId)
    expect(first.ecology?.[0].warning).not.toBe(second.ecology?.[0].warning)
    expect(first.tiles.map(tile => tile.kind)).not.toEqual(second.tiles.map(tile => tile.kind))
    expect(tacticalEncounterDebug(first).map(encounter => encounter.archetype)).not.toEqual(tacticalEncounterDebug(second).map(encounter => encounter.archetype))
  })

  it('carries cleared area phases into the next floor', () => {
    const state = newRun(73, 'mine')
    state.floor.objective.status = 'complete'
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    descend(state)
    expect(state.areaArc?.clearedPhases).toEqual(['survey'])
    expect(state.floor.escalation?.carried).toEqual(['survey'])
    expect(state.floor.objective.label).toContain('[carried: survey]')
  })
})
