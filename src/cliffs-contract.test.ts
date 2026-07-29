import { describe, expect, it } from 'vitest'
import { advance } from './engine/combat'
import { advanceGuardianPhase } from './engine/guardians'
import { useRope } from './engine/inventory'
import { newRun } from './engine'
import { fieldReadout } from './engine/readout'
import { measureGeneration } from './generation-metrics'
import { generateAreaFloor, getTile, hasPassablePath, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration } from './world'

const shapeSeeds = [0, 2, 9]

describe('Windcut Escarpment generation contract', () => {
  it('realizes switchback, ravine-loop, and anchor-chain height graphs', () => {
    const floors = shapeSeeds.map(seed => generateAreaFloor(seed, 'cliffs', 0, 3))
    expect(new Set(floors.map(floor => floor.layoutId))).toEqual(new Set(['switchback-face', 'ravine-bridge-loop', 'anchor-chain']))
    expect(new Set(floors.map(floor => macroRecipeDebug(floor)?.topology))).toEqual(new Set(['vertical', 'looped', 'directedFlow']))
    for (const floor of floors) {
      const macro = macroRecipeDebug(floor)!
      const safe = macro.edges.find(edge => edge.modes.includes('safe'))!
      const costly = macro.edges.find(edge => edge.modes.includes('costly') && edge.modes.includes('optional'))!
      const report = measureGeneration({ floor, route: routeContractDebug(floor), macro, validation: validateGeneration(floor) })
      expect(report.acceptance).toEqual({ valid: true, errors: [] })
      expect(floor.tiles.filter(tile => tile.kind === 'cliffWall')).not.toHaveLength(0)
      expect(safe.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'rope')).toBe(true)
      expect(safe.cells.every(point => ['floor', 'rope', 'ledge'].includes(getTile(floor, point.x, point.y)?.kind ?? 'wall'))).toBe(true)
      expect(costly.cells.some(point => getTile(floor, point.x, point.y)?.kind === 'ledge')).toBe(true)
      expect(floor.climbLinks).toHaveLength(2)
      for (const link of floor.climbLinks!) {
        expect(link.anchored).toBe(false)
        expect(getTile(floor, link.lower.x, link.lower.y)?.elevation).toBeLessThan(getTile(floor, link.upper.x, link.upper.y)?.elevation ?? 0)
      }
      expect(floor.ecology?.[0]).toMatchObject({ kind: 'wind', route: 'costly', effectFlow: { hazard: 'squall' }, state: 'waiting' })
      expect(placementDebug(floor).find(entry => entry.id === 'ecology:wind')).toMatchObject({ usedFallback: false })
      costly.cells.forEach(point => { const tile = getTile(floor, point.x, point.y); if (tile?.kind === 'ledge') tile.kind = 'wall' })
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    }
  }, 30_000)

  it('escalates from one rope decision to a multi-altitude wind network with cliff alcoves', () => {
    const floors = Array.from({ length: 4 }, (_, areaFloor) => generateAreaFloor(91, 'cliffs', areaFloor, 3))
    expect(floors.map(floor => floor.climbLinks?.length)).toEqual([2, 3, 4, 5])
    expect(floors.map(floor => floor.sideSpaces?.length)).toEqual([1, 2, 3, 4])
    expect(floors.map(floor => floor.cliffLayout?.windCorridors.length)).toEqual([0, 1, 2, 3])
    for (const floor of floors) {
      expect(new Set(floor.tiles.map(tile => tile.elevation).filter((elevation): elevation is 0 | 1 | 2 => elevation !== undefined))).toEqual(new Set([0, 1, 2]))
      expect(floor.cliffLayout).toMatchObject({ lowRoute: expect.any(Array), midLedges: expect.any(Array), highRidge: expect.any(Array), shelteredPockets: expect.any(Array) })
      expect(floor.cliffLayout!.lowRoute.length).toBeGreaterThan(3)
      expect(floor.cliffLayout!.midLedges.length).toBeGreaterThan(3)
      expect(floor.cliffLayout!.highRidge.length).toBeGreaterThan(3)
      for (const corridor of floor.cliffLayout!.windCorridors) {
        expect(corridor).toHaveLength(3)
        expect(corridor.slice(0, -1).every(point => getTile(floor, point.x, point.y)?.flow?.hazard === 'squall')).toBe(true)
        expect(getTile(floor, corridor.at(-1)!.x, corridor.at(-1)!.y)?.kind).toBe('rope')
      }
      for (const alcove of floor.sideSpaces ?? []) {
        expect(alcove.kind).toBe('cliff-alcove')
        expect(getTile(floor, alcove.entry.x, alcove.entry.y)?.kind).toBe('rope')
        expect(alcove.chamber.every(point => getTile(floor, point.x, point.y)?.kind === 'ledge')).toBe(true)
      }
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
  }, 30_000)

  it('places an exposed high-perch flyer in the foothill encounter group', () => {
    const floor = generateAreaFloor(42, 'cliffs', 0, 3)
    const stormCrow = floor.actors.find(actor => actor.kind === 'stormCrow' && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))
    expect(stormCrow).toBeDefined()
    expect(getTile(floor, stormCrow!.x, stormCrow!.y)).toMatchObject({ kind: 'ledge', elevation: 2 })
    expect(stormCrow?.encounter?.answer).toContain('anchored high route')
    expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
  }, 30_000)

  it('telegraphs squalls, permits rope-braced counterplay, and keeps climbs reversible', () => {
    const pushed = newRun(9, 'cliffs')
    const ecology = pushed.floor.ecology![0]
    pushed.floor.actors = []
    pushed.hero.x = ecology.target.x
    pushed.hero.y = ecology.target.y
    pushed.turn = ecology.startsAt - 1
    advance(pushed, [])
    expect(pushed.hero).not.toMatchObject(ecology.target)
    expect(pushed.messages[0]).toContain('squall drives')

    const braced = newRun(9, 'cliffs')
    const bracedEcology = braced.floor.ecology![0]
    braced.floor.actors = []
    braced.hero.x = bracedEcology.target.x
    braced.hero.y = bracedEcology.target.y
    braced.hero.traversalTools = ['cordAnchor']
    braced.turn = bracedEcology.startsAt - 1
    advance(braced, [])
    expect(braced.hero).toMatchObject(bracedEcology.target)
    expect(braced.messages[0]).toContain('rope anchor')

    const link = braced.floor.climbLinks![0]
    braced.hero.ropes = 1
    braced.hero.x = link.lower.x
    braced.hero.y = link.lower.y
    useRope(braced)
    expect(link.anchored).toBe(true)
    useRope(braced)
    expect(braced.hero).toMatchObject(link.upper)
  })

  it('announces wind vectors and preserves the Sky Warden escape through phase changes', () => {
    const state = newRun(9, 'cliffs', 3)
    advance(state, [])
    expect(fieldReadout(state).lines.some(line => line.startsWith('ECOLOGY: WIND T-') && line.includes('Wind vectors mark'))).toBe(true)
    const guardian = state.floor.actors.find(actor => actor.kind === 'skyWarden')!
    guardian.health = Math.floor(guardian.maxHealth * .6)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'pressure', tile: 'ledge' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    guardian.health = Math.floor(guardian.maxHealth * .3)
    expect(advanceGuardianPhase(state, guardian)).toMatchObject({ to: 'cataclysm', tile: 'smoke' })
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
    expect(validateGeneration(state.floor)).toEqual({ valid: true, errors: [] })
  }, 30_000)
})
