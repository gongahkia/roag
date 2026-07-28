import { describe, expect, it } from 'vitest'
import { advance } from './engine/combat'
import { advanceGuardianPhase } from './engine/guardians'
import { newRun } from './engine'
import { fieldReadout } from './engine/readout'
import { DIRECTIONS } from './types'
import { measureGeneration } from './generation-metrics'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 2, 42]

describe('Tidal Floodlands generation contract', () => {
  it('realizes braided, floodgate, and island-hop flow networks with dry refuge routes', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'floodedRuins', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['braided-current-delta', 'anchor-gated-ruin', 'island-hop-network']))
    expect(new Set(floors.map(floor => macroRecipeDebug(floor)?.topology))).toEqual(new Set(['directedFlow', 'looped', 'broad']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const safe = macro.edges.find(edge => edge.modes.includes('safe'))!
      const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'))!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(safe.cells.every(point => ['floor', 'anchor'].includes(getTile(floor, point.x, point.y)?.kind ?? 'wall'))).toBe(true)
      expect(safe.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'anchor')).toBe(true)
      const flowCells = costly.cells.filter(point => getTile(floor, point.x, point.y)?.kind === 'current')
      expect(flowCells.length).toBeGreaterThan(2)
      for (const point of flowCells) {
        const flow = getTile(floor, point.x, point.y)?.flow
        expect(flow).toBeDefined()
        const delta = DIRECTIONS[flow!.direction]
        expect(getTile(floor, point.x + delta.x, point.y + delta.y)).toBeDefined()
      }
      expect(floor.ecology?.[0]).toMatchObject({ kind: 'tide', route: 'costly', state: 'waiting' })
      expect(placementDebug(floor).find(entry => entry.id === 'ecology:tide')).toMatchObject({ usedFallback: false })
      costly.cells.forEach(point => { const tile = getTile(floor, point.x, point.y); if (tile?.kind === 'current') { tile.kind = 'deepWater'; delete tile.flow } })
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    }
  }, 30_000)

  it('places a tidewraith on a native current route on every ordinary Flooded floor', () => {
    for (let areaFloor = 0; areaFloor < 3; areaFloor++) {
      const floor = generateAreaFloor(42, 'floodedRuins', areaFloor, 3)
      const tidewraith = floor.actors.find(actor => actor.kind === 'tidewraith' && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))
      expect(tidewraith).toBeDefined()
      expect(getTile(floor, tidewraith!.x, tidewraith!.y)?.kind).toBe('current')
      expect(tidewraith?.encounter).toBeDefined()
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('announces floodgate flow and preserves the Tidal Guardian route through flood states', () => {
    const state = newRun(42, 'floodedRuins', 3)
    advance(state, [])
    expect(fieldReadout(state).lines.some(line => line.startsWith('ECOLOGY: TIDE T-') && line.includes('Current arrows turn'))).toBe(true)
    const guardian = state.floor.actors.find(actor => actor.kind === 'drownedRegent')!
    guardian.health = Math.floor(guardian.maxHealth * .6)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'pressure', tile: 'current' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    guardian.health = Math.floor(guardian.maxHealth * .3)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'cataclysm', tile: 'deepWater' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    expect(validateGeneration(state.floor)).toEqual({ valid: true, errors: [] })
  }, 30_000)
})
