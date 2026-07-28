import { describe, expect, it } from 'vitest'
import { measureGeneration } from './generation-metrics'
import { getTile, generateAreaFloor, hasPassablePath, macroRecipeDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 1, 3]

describe('Obsidian Pass generation contract', () => {
  it('realizes all three Mine recipes with landmark, payoff loop, and two route choices', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'mine', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['rail-spine', 'branching-drifts', 'collapse-loop']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(macro.nodes.map(node => node.kind)).toEqual(expect.arrayContaining(['landmark', 'fork', 'optionalReward']))
      expect(floor.props.length).toBeGreaterThan(0)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  })

  it('places a terrain-aware Railguard encounter on each ordinary Mine floor', () => {
    for (let areaFloor = 0; areaFloor < 3; areaFloor++) {
      const floor = generateAreaFloor(42, 'mine', areaFloor, 3)
      const railguard = floor.actors.find(actor => actor.kind === 'railguard' && actor.encounter?.archetype === 'nativeTerrainPack')
      expect(railguard).toBeDefined()
      expect(railguard?.terrainAffinity).toContain(getTile(floor, railguard!.x, railguard!.y)?.kind)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  })

  it('keeps the Warden boss route and objective reachable', () => {
    const floor = generateAreaFloor(42, 'mine', 3, 3)
    expect(routeContractDebug(floor)?.nodes.some(node => node.kind === 'boss')).toBe(true)
    expect(floor.actors.some(actor => actor.kind === 'foreman' && actor.role === 'guardian')).toBe(true)
    expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
  })
})
