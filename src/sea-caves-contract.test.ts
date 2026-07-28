import { describe, expect, it } from 'vitest'
import { advance } from './engine/combat'
import { newRun } from './engine'
import { measureGeneration } from './generation-metrics'
import { fieldReadout } from './engine/readout'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 1, 3]

describe('Sea Caves generation contract', () => {
  it('realizes three tide topologies with an announced wet option and dry safe route', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'caverns', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['tide-chambers', 'sinkhole-galleries', 'fault-tunnels']))
    expect(new Set(floors.map(floor => macroRecipeDebug(floor)?.topology))).toEqual(new Set(['directedFlow', 'vertical', 'looped']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const safe = macro.edges.find(edge => edge.modes.includes('safe'))!
      const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'))!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(safe.cells.every(point => getTile(floor, point.x, point.y)?.kind === 'floor')).toBe(true)
      expect(costly.cells.some(point => ['water', 'current'].includes(getTile(floor, point.x, point.y)?.kind ?? 'wall'))).toBe(true)
      expect(floor.ecology?.[0]).toMatchObject({ kind: 'tide', route: 'costly', state: 'waiting' })
      expect(placementDebug(floor).find(entry => entry.id === 'ecology:tide')).toMatchObject({ usedFallback: false })
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  })

  it('places tide-native creatures on every ordinary Sea Cave floor', () => {
    for (let areaFloor = 0; areaFloor < 3; areaFloor++) {
      const floor = generateAreaFloor(42, 'caverns', areaFloor, 3)
      const tideEel = floor.actors.find(actor => actor.kind === 'fumeeel' && actor.encounter?.archetype === 'nativeTerrainPack')
      expect(tideEel).toBeDefined()
      expect(tideEel?.terrainAffinity).toContain(getTile(floor, tideEel!.x, tideEel!.y)?.kind)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  })

  it('announces the tide before its transition and keeps the Tidemaw route traversable', () => {
    const state = newRun(42, 'caverns')
    advance(state, [])
    expect(fieldReadout(state).lines.some(line => line.startsWith('ECOLOGY: TIDE T-1:'))).toBe(true)
    const floor = generateAreaFloor(42, 'caverns', 3, 3)
    expect(routeContractDebug(floor)?.nodes.some(node => node.kind === 'boss')).toBe(true)
    expect(floor.actors.some(actor => actor.kind === 'geode' && actor.role === 'guardian')).toBe(true)
    expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
  })
})
