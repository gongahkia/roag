import { describe, expect, it } from 'vitest'
import type { Biome } from './types'
import { newRun } from './engine'
import { advance } from './engine/combat'
import { DIRECTIONS } from './types'
import { autoplayDecision, autoplayTurnBudget, createAutoplayContext } from './autoplay'
import { runAutoplay } from './autoplay-runner'
import { generateAreaFloor, getTile, isPassable, validateGeneration } from './world'

const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']

describe('biome macro generation', () => {
  it('validates every biome layout across the clearance seeds', () => {
    for (const seed of [7, 42, 999]) for (const biome of biomes) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
      const floor = generateAreaFloor(seed, biome, areaFloor)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
      expect(floor.tiles).toHaveLength(floor.width * floor.height)
    }
  }, 90_000)

  it('uses each macro archetype before the fourth-floor remix', () => {
    for (const biome of biomes) {
      const layouts = Array.from({ length: 4 }, (_, areaFloor) => generateAreaFloor(42, biome, areaFloor).layoutId)
      expect(new Set(layouts.slice(0, 3)).size).toBe(3)
      expect(layouts[3]).toMatch(/-remix$/)
    }
  })

  it('generates directional water networks only in current terrain', () => {
    for (const areaFloor of [0, 1, 2, 3]) {
      const floor = generateAreaFloor(42, 'floodedRuins', areaFloor)
      const flows = floor.tiles.filter(tile => tile.flow)
      expect(flows.length).toBeGreaterThan(10)
      expect(flows.every(tile => tile.kind === 'current')).toBe(true)
    }
  })

  it('resolves a visible current after a turn', () => {
    const state = newRun(42, 'floodedRuins')
    state.floor.actors = []
    const source = state.floor.tiles.findIndex((tile, index) => tile.flow && (() => {
      const point = { x: index % state.floor.width, y: Math.floor(index / state.floor.width) }
      const delta = DIRECTIONS[tile.flow!.direction]
      return isPassable(state.floor, point.x + delta.x, point.y + delta.y)
    })())
    expect(source).toBeGreaterThanOrEqual(0)
    const point = { x: source % state.floor.width, y: Math.floor(source / state.floor.width) }
    const flow = getTile(state.floor, point.x, point.y)!.flow!
    const delta = DIRECTIONS[flow.direction]
    state.hero.x = point.x
    state.hero.y = point.y
    advance(state, [])
    expect(state.hero).toMatchObject({ x: point.x + delta.x, y: point.y + delta.y })
  })

  it('scales autoplay budget to the generated floor footprint', () => {
    expect(autoplayTurnBudget(newRun(42, 'mine'))).toBe(800)
    expect(autoplayTurnBudget(newRun(42, 'burial'))).toBeGreaterThan(2_000)
  })

  it('uses surveyed terrain to recover an unseen exit route on wide maps', () => {
    const state = newRun(42, 'floodedRuins')
    state.floor.actors = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    expect(autoplayDecision(state, 'visible', 'clear', createAutoplayContext())).toMatchObject({ reason: 'survey exit route' })
  })

  it('supports exact one-floor autoplay validation', () => {
    const state = newRun(42, 'mine')
    state.floor.actors = []
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    expect(runAutoplay(state, { mode: 'omniscient', chainAreas: false, chainFloors: false, turnLimit: 1 })).toMatchObject({ outcome: 'complete', floor: 2 })
  })

  it('recovers from visible route invalidation on wide layouts', () => {
    for (const [biome, floor] of [['ruins', 0], ['cliffs', 0], ['cliffs', 2]] as const) expect(runAutoplay(newRun(7, biome, floor), { mode: 'visible', policy: 'clear', turnLimit: 2_400, chainAreas: false, chainFloors: false })).toMatchObject({ outcome: 'complete' })
  }, 60_000)
})
