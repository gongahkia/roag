import { describe, expect, it } from 'vitest'
import { advance } from './engine/combat'
import { advanceGuardianPhase } from './engine/guardians'
import { newRun } from './engine'
import { measureGeneration } from './generation-metrics'
import { fieldReadout } from './engine/readout'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 1, 9]

describe('Kiln Terraces generation contract', () => {
  it('realizes stepped, service, and ash-loop vertical route families', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'furnace', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['stepped-kiln-chain', 'smoke-choked-service-route', 'lift-and-ash-loop']))
    expect(new Set(floors.map(floor => macroRecipeDebug(floor)?.topology))).toEqual(new Set(['vertical', 'directedFlow', 'looped']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const safe = macro.edges.find(edge => edge.modes.includes('safe'))!
      const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'))!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(safe.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'lift')).toBe(true)
      expect(safe.cells.every(point => ['floor', 'lift'].includes(getTile(floor, point.x, point.y)?.kind ?? 'wall'))).toBe(true)
      expect(costly.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'smoke')).toBe(true)
      expect(costly.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'fireVent')).toBe(true)
      expect(floor.climbLinks).toMatchObject([{ anchored: true }])
      expect(getTile(floor, floor.climbLinks![0].lower.x, floor.climbLinks![0].lower.y)?.elevation).toBe(0)
      expect(getTile(floor, floor.climbLinks![0].upper.x, floor.climbLinks![0].upper.y)?.elevation).toBe(1)
      expect(floor.ecology?.[0]).toMatchObject({ kind: 'smoke', route: 'costly', state: 'waiting' })
      expect(placementDebug(floor).find(entry => entry.id === 'ecology:smoke')).toMatchObject({ usedFallback: false })
      costly.cells.forEach(point => { const tile = getTile(floor, point.x, point.y); if (tile?.kind === 'smoke' || tile?.kind === 'fireVent') tile.kind = 'wall' })
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    }
  }, 30_000)

  it('places a cinder creature in an active firing zone on every ordinary Furnace floor', () => {
    for (let areaFloor = 0; areaFloor < 3; areaFloor++) {
      const floor = generateAreaFloor(42, 'furnace', areaFloor, 3)
      const cinderling = floor.actors.find(actor => actor.kind === 'cinderling' && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))
      expect(cinderling).toBeDefined()
      expect(getTile(floor, cinderling!.x, cinderling!.y)?.kind).toBe('fireVent')
      expect(cinderling?.encounter?.answer).toContain('lift lane')
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('announces the firing state and preserves the Kiln Heart escape through its phase changes', () => {
    const state = newRun(42, 'furnace', 3)
    advance(state, [])
    expect(fieldReadout(state).lines.some(line => line.startsWith('ECOLOGY: SMOKE T-') && line.includes('kiln stack opens'))).toBe(true)
    const guardian = state.floor.actors.find(actor => actor.kind === 'kilnheart')!
    guardian.health = Math.floor(guardian.maxHealth * .6)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'pressure', tile: 'smoke' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    guardian.health = Math.floor(guardian.maxHealth * .3)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'cataclysm', tile: 'fireVent' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    expect(validateGeneration(state.floor)).toEqual({ valid: true, errors: [] })
  }, 30_000)
})
