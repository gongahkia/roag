import { describe, expect, it } from 'vitest'
import { advance } from './engine/combat'
import { newRun } from './engine'
import { measureGeneration } from './generation-metrics'
import { fieldReadout } from './engine/readout'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, reachableFloorIndexes, routeContractDebug, validateGeneration } from './world'

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

  it('makes irregular caverns, water-to-wall clues, and hidden chambers escalate by floor', () => {
    const floors = Array.from({ length: 4 }, (_, areaFloor) => generateAreaFloor(91, 'caverns', areaFloor, 3))
    expect(floors[3]!.tiles.filter(tile => tile.kind === 'current').length).toBeGreaterThan(floors[0]!.tiles.filter(tile => tile.kind === 'current').length)
    for (const [areaFloor, floor] of floors.entries()) {
      const chambers = floor.sideSpaces ?? []
      expect(floor.tiles.filter(tile => tile.kind === 'wall').length).toBeGreaterThan(floor.tiles.length * .45)
      expect(chambers).toHaveLength(1 + Math.floor(areaFloor / 2))
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
      const reachable = reachableFloorIndexes(floor)
      for (const chamber of chambers) {
        expect(chamber.kind).toBe('cavern-hidden-chamber')
        if (chamber.kind !== 'cavern-hidden-chamber') throw new Error('expected cavern hidden chamber')
        expect(getTile(floor, chamber.entry.x, chamber.entry.y)?.kind).toBe('breakwall')
        expect(getTile(floor, chamber.waterHint.x, chamber.waterHint.y)).toMatchObject({ kind: 'current', flow: { direction: getTile(floor, chamber.waterHint.x, chamber.waterHint.y)?.flow?.direction } })
        expect(reachable.has(chamber.reward.y * floor.width + chamber.reward.x)).toBe(false)
        getTile(floor, chamber.entry.x, chamber.entry.y)!.kind = 'floor'
        expect(reachableFloorIndexes(floor).has(chamber.reward.y * floor.width + chamber.reward.x)).toBe(true)
        getTile(floor, chamber.entry.x, chamber.entry.y)!.kind = 'breakwall'
      }
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  })
})
