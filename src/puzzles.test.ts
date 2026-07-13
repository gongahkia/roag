import { describe, expect, it } from 'vitest'
import { puzzleTemplateById, validatePuzzleTemplates } from './puzzles'
import { generateAreaFloor, validateGeneration } from './world'

describe('Mine puzzle templates', () => {
  it('provides two tactical solutions and validates generated seeds', () => {
    expect(validatePuzzleTemplates()).toEqual([])
    for (let seed = 1; seed <= 24; seed++) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
      const floor = generateAreaFloor(seed, 'mine', areaFloor)
      const template = puzzleTemplateById(floor.puzzleIds?.[0] ?? '')
      expect(template?.solutions.length).toBeGreaterThanOrEqual(2)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  })
})
