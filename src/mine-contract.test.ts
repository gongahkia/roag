import { describe, expect, it } from 'vitest'
import { measureGeneration } from './generation-metrics'
import { getTile, generateAreaFloor, hasPassablePath, macroRecipeDebug, reachableFloorIndexes, routeContractDebug, validateGeneration } from './world'

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

  it('adds deterministic, breakwall-gated breach rooms with escalating discovery space', () => {
    const layouts = Array.from({ length: 4 }, (_, areaFloor) => generateAreaFloor(91, 'mine', areaFloor, 3))
    for (const [areaFloor, floor] of layouts.entries()) {
      const rooms = floor.sideSpaces ?? []
      expect(rooms).toHaveLength(areaFloor + 1)
      const reachable = reachableFloorIndexes(floor)
      for (const room of rooms) {
        expect(getTile(floor, room.entry.x, room.entry.y)?.kind).toBe('breakwall')
        expect(reachable.has(room.chamber[0]!.y * floor.width + room.chamber[0]!.x)).toBe(false)
        expect(floor.items).toContainEqual(room.reward)
        getTile(floor, room.entry.x, room.entry.y)!.kind = 'floor'
        const opened = reachableFloorIndexes(floor)
        expect(room.chamber.every(point => opened.has(point.y * floor.width + point.x))).toBe(true)
        getTile(floor, room.entry.x, room.entry.y)!.kind = 'breakwall'
      }
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
    const repeated = generateAreaFloor(91, 'mine', 3, 3)
    expect(repeated.sideSpaces).toEqual(layouts[3]!.sideSpaces)
  })

  it('records deterministic rare breach transitions in generation audits', () => {
    const transitions = (generateAreaFloor(7, 'mine', 1, 3).sideSpaces ?? []).flatMap(room => room.kind === 'mine-breach-room' && room.rareTransition ? [room.rareTransition] : [])
    expect(transitions).toEqual([{ kind: 'floorSkip', targetBiome: 'mine', targetFloor: 3 }])
  })
})
