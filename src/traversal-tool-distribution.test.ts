import { describe, expect, it } from 'vitest'
import { acquireOptionalTraversalTool } from './engine/buildcraft'
import { campaignOrderForSeed } from './engine/campaign'
import { chooseEncounter } from './engine/encounters'
import { pickUp } from './engine/inventory'
import { newRun } from './engine/run'
import { migrateRunRecord } from './storage'
import { createHero, createRun } from './test/factories'
import { OPTIONAL_TERRAIN_TOOLS, optionalTerrainToolFor, type OptionalToolSource } from './traversal-tool-distribution'
import type { Biome } from './types'
import { generateAreaFloor, hasPassableTerrainPath } from './world'

const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']
const sources: readonly OptionalToolSource[] = ['waycache', 'loot', 'social', 'encounter']

describe('optional terrain-tool distribution', () => {
  it('is deterministic and reaches every new tool across seeded local sources', () => {
    const picks = (seed: number) => sources.flatMap(source => biomes.flatMap(biome => Array.from({ length: 24 }, (_, floorIndex) => optionalTerrainToolFor({ seed, floorIndex, biome, recipeId: `${biome}-${floorIndex}`, source })).filter((tool): tool is typeof OPTIONAL_TERRAIN_TOOLS[number] => Boolean(tool))))
    expect(picks(73)).toEqual(picks(73))
    expect(new Set(picks(73))).toEqual(new Set(OPTIONAL_TERRAIN_TOOLS))
  })

  it('binds cache tools, suppresses duplicates, and replaces the oldest full-loadout slot', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['stoneWedge', 'reedwing'] }) })
    state.hero.cooldowns = { 'tool:stoneWedge': 3 }
    expect(acquireOptionalTraversalTool(state, 'stoneAdze')).toEqual({ result: 'replaced', replaced: 'stoneWedge' })
    expect(state.hero.traversalTools).toEqual(['reedwing', 'stoneAdze'])
    expect(state.hero.cooldowns?.['tool:stoneWedge']).toBeUndefined()
    expect(acquireOptionalTraversalTool(state, 'stoneAdze')).toEqual({ result: 'duplicate' })
    state.floor.items = [{ id: 'rock', tool: 'resinFireBasket', x: 1, y: 1, count: 1 }]
    pickUp(state)
    expect(state.hero.traversalTools).toEqual(['stoneAdze', 'resinFireBasket'])
    expect(state.messages[0]).toContain('replace Reedwing')
  })

  it('offers encounter tools through the same bounded acquisition path', () => {
    const state = createRun({ hero: createHero({ traversalTools: ['antlerPrybar'] }) })
    state.floor.encounters = [{ id: 'tool-cache', kind: 'shiftingChamber', x: 1, y: 1, state: 'dormant', toolOffer: 'woodenLeverRoller' }]
    chooseEncounter(state, 'tool-cache', '1')
    expect(state.hero.traversalTools).toEqual(['antlerPrybar', 'woodenLeverRoller'])
    expect(state.messages).toContain('You bind the Wooden Lever and Roller.')
  })

  it('persists optional cache and encounter offers', () => {
    const state = newRun(901)
    state.floor.items.push({ id: 'rock', tool: 'stoneAdze', x: state.floor.start.x, y: state.floor.start.y, count: 1 })
    state.floor.encounters![0]!.toolOffer = 'resinFireBasket'
    expect(migrateRunRecord(structuredClone(state))?.floor).toMatchObject({ items: expect.arrayContaining([expect.objectContaining({ tool: 'stoneAdze' })]), encounters: expect.arrayContaining([expect.objectContaining({ toolOffer: 'resinFireBasket' })]) })
  })

  it('keeps generated campaign terrain completable with no traversal tool', () => {
    for (const biome of campaignOrderForSeed(73)) for (const areaFloor of [0, 1, 2, 3]) {
      const floor = generateAreaFloor(73, biome, areaFloor)
      expect(hasPassableTerrainPath(floor, floor.start, floor.exit)).toBe(true)
    }
  }, 30_000)
})
