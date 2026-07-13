import { describe, expect, it } from 'vitest'
import { moveHero } from './combat'
import { createHero, createRun } from '../test/factories'
import { puzzleTemplateById, validatePuzzleTemplates } from '../puzzles'
import { generateAreaFloor, getTile, validateGeneration } from '../world'

describe('Ruins puzzle templates', () => {
  it('provides two tactical routes and validates generated seeds', () => {
    expect(validatePuzzleTemplates()).toEqual([])
    for (const seed of [1, 7, 19, 41, 999]) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
      const floor = generateAreaFloor(seed, 'ruins', areaFloor)
      const template = puzzleTemplateById(floor.puzzleIds?.[0] ?? '')
      expect(template?.solutions.length).toBeGreaterThanOrEqual(2)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  })

  it('supports a key route or a damaging dart route', () => {
    const key = createRun({ hero: createHero({ keys: 1 }) })
    key.floor.tiles[1 * 48 + 2].kind = 'lockedDoor'
    moveHero(key, 'e')
    expect(key.hero.keys).toBe(0)
    expect(getTile(key.floor, 2, 1)?.kind).toBe('floor')

    const darts = createRun()
    darts.floor.tiles[1 * 48 + 2].kind = 'dart'
    moveHero(darts, 'e')
    expect(darts.hero).toMatchObject({ x: 2, y: 1, health: 18 })
  })
})
