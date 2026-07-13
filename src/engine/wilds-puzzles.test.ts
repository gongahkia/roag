import { describe, expect, it } from 'vitest'
import { moveHero } from './combat'
import { createHero, createRun } from '../test/factories'
import { puzzleTemplateById, validatePuzzleTemplates } from '../puzzles'
import { generateAreaFloor, getTile, validateGeneration } from '../world'

describe('Wilds puzzle templates', () => {
  it('provides two tactical routes and validates generated seeds', () => {
    expect(validatePuzzleTemplates()).toEqual([])
    for (let seed = 1; seed <= 24; seed++) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
      const floor = generateAreaFloor(seed, 'wilds', areaFloor)
      const template = puzzleTemplateById(floor.puzzleIds?.[0] ?? '')
      expect(template?.solutions.length).toBeGreaterThanOrEqual(2)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  })

  it('lets a machete clear the bramble route', () => {
    const state = createRun({ hero: createHero({ equipment: { mainHand: 'machete' } }) })
    state.floor.tiles[1 * 48 + 2].kind = 'bramble'
    moveHero(state, 'e')
    expect(state.hero).toMatchObject({ x: 2, y: 1 })
    expect(getTile(state.floor, 2, 1)?.kind).toBe('floor')
  })
})
