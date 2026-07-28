import { describe, expect, it } from 'vitest'
import { advance } from './engine/combat'
import { newRun } from './engine'
import { measureGeneration } from './generation-metrics'
import { fieldReadout } from './engine/readout'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 4, 7]

describe('River Wilds generation contract', () => {
  it('realizes broad river, grove, and wetland routes with announced costly pressure', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'wilds', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['river-clearings', 'root-maze', 'wetland-causeways']))
    expect(new Set(floors.map(floor => macroRecipeDebug(floor)?.topology))).toEqual(new Set(['broad', 'looped', 'directedFlow']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const safe = macro.edges.find(edge => edge.modes.includes('safe'))!
      const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'))!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(floor.tiles.filter(tile => tile.kind !== 'wall').length).toBeGreaterThan(floor.tiles.length * 0.7)
      expect(floor.tiles.map(tile => tile.kind)).toEqual(expect.arrayContaining(['water', 'bramble', 'web']))
      expect(safe.cells.every(point => getTile(floor, point.x, point.y)?.kind === 'floor')).toBe(true)
      expect(costly.cells.some(point => ['water', 'web'].includes(getTile(floor, point.x, point.y)?.kind ?? 'wall'))).toBe(true)
      expect(floor.ecology?.[0]).toMatchObject({ kind: 'nesting', route: 'costly', state: 'waiting' })
      expect(placementDebug(floor).find(entry => entry.id === 'ecology:nesting')).toMatchObject({ usedFallback: false })
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('places a water or web native encounter on every ordinary Wilds floor', () => {
    for (let areaFloor = 0; areaFloor < 3; areaFloor++) {
      const floor = generateAreaFloor(42, 'wilds', areaFloor, 3)
      const native = floor.actors.find(actor => actor.encounter?.archetype === 'nativeTerrainPack' && actor.encounter.leader)
      expect(native).toBeDefined()
      expect(native?.terrainAffinity).toContain(getTile(floor, native!.x, native!.y)?.kind)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('announces nesting before escalation and keeps the Heartwood route traversable', () => {
    const state = newRun(42, 'wilds')
    advance(state, [])
    expect(fieldReadout(state).lines.some(line => line.startsWith('ECOLOGY: NESTING T-3:'))).toBe(true)
    const floor = generateAreaFloor(42, 'wilds', 3, 3)
    expect(routeContractDebug(floor)?.nodes.some(node => node.kind === 'boss')).toBe(true)
    expect(floor.actors.some(actor => actor.kind === 'heartwood' && actor.role === 'guardian')).toBe(true)
    expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
  }, 30_000)
})
