import { describe, expect, it } from 'vitest'
import { objectiveForFloor } from '../objectives'
import { createFloor, createHero, createRun } from '../test/factories'
import { operate } from './inventory'
import { contextualReward, merchantStock, missingCapabilities } from './rewards'
import { indexOf } from '../types'

describe('contextual rewards', () => {
  it('selects rewards from area, objective, build tags, and missing capabilities', () => {
    const wilds = createRun({ area: 'wilds', floor: createFloor({ index: 5, biome: 'wilds', objective: objectiveForFloor(5) }) })
    expect(missingCapabilities(wilds)).toContain('rope')
    expect(contextualReward(wilds, 'container')).toBe('ropeBundle')

    const caverns = createRun({ area: 'caverns', floor: createFloor({ index: 8, biome: 'caverns' }) })
    expect(merchantStock(caverns)[0]).toBe('lantern')
    const guardian = createRun({ area: 'mine', floor: createFloor({ index: 3, biome: 'mine', objective: objectiveForFloor(3) }) })
    expect(merchantStock(guardian)[0]).toBe('bombPack')

    const ruins = createRun({ area: 'ruins', hero: createHero({ inventory: ['ember'] }), floor: createFloor({ index: 14, biome: 'ruins', objective: objectiveForFloor(14) }) })
    expect(contextualReward(ruins, 'altar')).toBe('gate')
  })

  it('uses contextual container and altar rewards in the game flow', () => {
    const container = createRun({ area: 'wilds', floor: createFloor({ index: 5, biome: 'wilds', objective: objectiveForFloor(5) }) })
    container.floor.tiles[indexOf(2, 1)].kind = 'crate'
    operate(container)
    expect(container.hero.inventory).toContain('ropeBundle')

    const altar = createRun({ area: 'ruins', hero: createHero({ gold: 75, inventory: ['ember'] }), floor: createFloor({ index: 14, biome: 'ruins', objective: objectiveForFloor(14) }) })
    altar.floor.tiles[indexOf(1, 1)].kind = 'altar'
    operate(altar)
    expect(altar.hero.inventory).toContain('gate')
    expect(altar.floor.objective.status).toBe('complete')
  })
})
