import { describe, expect, it } from 'vitest'
import { advance } from './engine/combat'
import { newRun } from './engine'
import { measureGeneration } from './generation-metrics'
import { fieldReadout } from './engine/readout'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 1, 4]

describe('Ceremonial Ruins generation contract', () => {
  it('realizes open precinct, processional, and courtyard route families', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'ruins', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['circular-precinct', 'broken-processional-loop', 'courtyard-lattice']))
    expect(new Set(floors.map(floor => macroRecipeDebug(floor)?.topology))).toEqual(new Set(['broad', 'looped', 'directedFlow']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const safe = macro.edges.find(edge => edge.modes.includes('safe'))!
      const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'))!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(floor.tiles.filter(tile => tile.kind !== 'wall').length).toBeGreaterThan(floor.tiles.length * 0.7)
      expect(safe.cells.every(point => getTile(floor, point.x, point.y)?.kind === 'floor')).toBe(true)
      expect(costly.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'dart')).toBe(true)
      expect(floor.ecology?.[0]).toMatchObject({ kind: 'visibility', route: 'costly', state: 'waiting' })
      expect(placementDebug(floor).find(entry => entry.id === 'ecology:visibility')).toMatchObject({ usedFallback: false })
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('separates the central ritual altar from the altar event and guards it with a ward post', () => {
    const floor = generateAreaFloor(42, 'ruins', 2, 3)
    const objective = macroRecipeDebug(floor)!.nodes.find(node => node.kind === 'objective')!
    const ritual = { x: objective.footprint.x + Math.floor(objective.footprint.width / 2), y: objective.footprint.y + Math.floor(objective.footprint.height / 2) }
    const event = placementDebug(floor).find(entry => entry.id === `event:${floor.index}`)?.selected
    const ward = floor.actors.find(actor => actor.kind === 'wardacolyte')
    expect(getTile(floor, ritual.x, ritual.y)?.kind).toBe('altar')
    expect(event).toBeDefined()
    expect(event).not.toEqual(ritual)
    expect(getTile(floor, event!.x, event!.y)?.kind).toBe('altar')
    expect(ward?.terrainAffinity).toContain(getTile(floor, ward!.x, ward!.y)?.kind)
    expect(ward?.encounter).toBeDefined()
    expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
  }, 30_000)

  it('announces ward pressure, preserves lock counter-routes, and keeps the Regent route traversable', () => {
    const state = newRun(42, 'ruins')
    advance(state, [])
    expect(fieldReadout(state).lines.some(line => line.startsWith('ECOLOGY: VISIBILITY T-') && line.includes('Ward lamps dim'))).toBe(true)
    const floor = generateAreaFloor(42, 'ruins', 0, 3)
    const locks = floor.tiles.filter(tile => tile.kind === 'lockedDoor')
    expect(locks.length).toBeGreaterThan(0)
    locks.forEach(tile => { tile.kind = 'wall' })
    expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    const boss = generateAreaFloor(42, 'ruins', 3, 3)
    expect(routeContractDebug(boss)?.nodes.some(node => node.kind === 'boss')).toBe(true)
    expect(boss.actors.some(actor => actor.kind === 'regent' && actor.role === 'guardian')).toBe(true)
    expect(hasPassablePath(boss, boss.start, boss.exit)).toBe(true)
    expect(validateGeneration(boss)).toEqual({ valid: true, errors: [] })
  }, 30_000)
})
