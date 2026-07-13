import { describe, expect, it } from 'vitest'
import { castSpell } from './inventory'
import { createHero, createRun } from '../test/factories'
import { puzzleTemplateById, validatePuzzleTemplates } from '../puzzles'
import { generateAreaFloor, getTile, validateGeneration } from '../world'

describe('Caverns puzzle templates', () => {
  it('provides two tactical routes and validates generated seeds', () => {
    expect(validatePuzzleTemplates()).toEqual([])
    for (let seed = 1; seed <= 24; seed++) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
      const floor = generateAreaFloor(seed, 'caverns', areaFloor)
      const template = puzzleTemplateById(floor.puzzleIds?.[0] ?? '')
      expect(template?.solutions.length).toBeGreaterThanOrEqual(2)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  })

  it('lets Tide quench vents and Ember ignite gas routes', () => {
    const tide = createRun({ hero: createHero({ inventory: ['waterScript'] }) })
    tide.floor.tiles[1 * 48 + 3].kind = 'fireVent'
    castSpell(tide, 'waterScript', 'e')
    expect(getTile(tide.floor, 3, 1)?.kind).toBe('floor')

    const ember = createRun({ hero: createHero({ inventory: ['ember'] }) })
    ember.floor.tiles[1 * 48 + 2].kind = 'gas'
    castSpell(ember, 'ember', 'e')
    expect(getTile(ember.floor, 2, 1)?.kind).toBe('floor')
  })
})
