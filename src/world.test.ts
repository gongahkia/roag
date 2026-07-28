import { describe, expect, it } from 'vitest'
import { ITEMS, MONSTERS } from './content'
import { newRun, perform, refreshFov } from './engine'
import { hasLine } from './engine/visibility'
import { createFloor } from './test/factories'
import { FLOOR_COUNT } from './types'
import { generateAreaFloor, generateFloor, getTile, hasPassablePath, hasPassableTerrainPath, placementDebug, tacticalEncounterDebug, traverseFloor, validateFloor, validateGeneration } from './world'

const exitReachable = (floor: ReturnType<typeof generateFloor>): boolean => hasPassablePath(floor, floor.start, floor.exit)

describe('expedition generation', () => {
  it('provides the locked full content roster', () => {
    expect(ITEMS).toHaveLength(79)
    expect(MONSTERS.filter(monster => monster.ai === 'guardian')).toHaveLength(10)
    expect(MONSTERS.filter(monster => monster.ai !== 'guardian' && monster.spawn !== 'triggered')).toHaveLength(69)
    expect(MONSTERS.find(monster => monster.id === 'startledBirds')?.spawn).toBe('triggered')
  })

  it('builds valid deterministic floors across the complete run', () => {
    for (let floor = 0; floor < FLOOR_COUNT; floor++) {
      const first = generateFloor(12345, floor)
      const second = generateFloor(12345, floor)
      expect(validateFloor(first)).toBe(true)
      expect(first.tiles).toHaveLength(first.width * first.height)
      expect(first.exit).toEqual(second.exit)
      expect(first.objective).toEqual(second.objective)
      expect(first.actors.map(actor => actor.kind)).toEqual(second.actors.map(actor => actor.kind))
      expect(first).not.toHaveProperty('routeContract')
      expect(exitReachable(first)).toBe(true)
    }
  }, 30_000)

  it('assigns deterministic objectives for all local floor roles', () => {
    expect(Array.from({ length: 4 }, (_, index) => generateFloor(99, index).objective.kind)).toEqual(['recoverSupplies', 'rescueScout', 'invokeAltar', 'defeatGuardian'])
  })

  it('uses supports, collapses, rails, and rubble on solvable Mine routes', () => {
    for (const seed of [7, 41, 999]) {
      const floor = generateAreaFloor(seed, 'mine', 0)
      const kinds = floor.tiles.map(tile => tile.kind)
      expect(kinds).toContain('support')
      expect(kinds).toContain('crumble')
      expect(kinds).toContain('rail')
      expect(kinds).toContain('rubble')
      expect(exitReachable(floor)).toBe(true)
    }
  })

  it('records contextual dead-end payoff, guarded shrine, and explicit placement failures', () => {
    const floor = generateAreaFloor(42, 'mine', 0)
    const debug = placementDebug(floor)
    expect(debug.find(entry => entry.id === 'milestone:boon-payoff')).toMatchObject({ selected: expect.any(Object), usedFallback: false, requirements: { nodeKinds: ['optionalReward'] } })
    expect(debug.find(entry => entry.id === 'encounter:guarded-shrine')).toMatchObject({ selected: expect.any(Object) })
    expect(debug.every(entry => entry.selected || entry.diagnostics.length)).toBe(true)
  })

  it('keeps a Wilds native-terrain pack on affinity terrain with a readable answer', () => {
    const floor = generateAreaFloor(42, 'wilds', 0, 3)
    const native = floor.actors.filter(actor => actor.encounter?.archetype === 'nativeTerrainPack')
    expect(native).not.toHaveLength(0)
    expect(native.some(actor => actor.encounter?.leader && actor.terrainAffinity?.includes(getTile(floor, actor.x, actor.y)?.kind ?? 'wall'))).toBe(true)
    expect(tacticalEncounterDebug(floor).find(encounter => encounter.archetype === 'nativeTerrainPack')?.answer).toContain('native terrain')
  }, 30_000)

  it('restores Mine contract connectors before floor-four event placement', () => {
    for (let seed = 0; seed < 24; seed++) expect(validateGeneration(generateAreaFloor(seed, 'mine', 3))).toEqual({ valid: true, errors: [] })
  }, 30_000)

  it('uses water, brambles, and webs to vary solvable Wilds sightlines', () => {
    for (const seed of [8, 42, 1000]) {
      const floor = generateAreaFloor(seed, 'wilds', 0)
      const kinds = floor.tiles.map(tile => tile.kind)
      expect(kinds).toContain('water')
      expect(kinds).toContain('bramble')
      expect(kinds).toContain('web')
      expect(exitReachable(floor)).toBe(true)
    }
    const run = newRun(12)
    run.floor.tiles[1 * run.floor.width + 2].kind = 'bramble'
    run.hero.x = 1
    run.hero.y = 1
    expect(hasLine(run, run.hero, { x: 3, y: 1 })).toBe(false)
  })

  it('uses lava, gas, vents, and lantern-sensitive Cavern routes', () => {
    for (const seed of [9, 43, 1001]) {
      const floor = generateAreaFloor(seed, 'caverns', 0)
      const kinds = floor.tiles.map(tile => tile.kind)
      expect(kinds).toContain('lava')
      expect(kinds).toContain('gas')
      expect(kinds).toContain('fireVent')
      expect(kinds).toContain('darkness')
      expect(exitReachable(floor)).toBe(true)
    }
    const run = newRun(13, 'caverns')
    run.floor.tiles[1 * run.floor.width + 2].kind = 'darkness'
    run.hero.x = 1
    run.hero.y = 1
    expect(hasLine(run, run.hero, { x: 3, y: 1 })).toBe(false)
    run.hero.equipment.offHand = 'lantern'
    expect(hasLine(run, run.hero, { x: 3, y: 1 })).toBe(true)
  })

  it('uses locks, darts, brittle floors, and ritual spaces on solvable Ruins routes', () => {
    for (const seed of [10, 44, 1002]) {
      const floor = generateAreaFloor(seed, 'ruins', 0)
      const kinds = floor.tiles.map(tile => tile.kind)
      expect(kinds).toContain('lockedDoor')
      expect(kinds).toContain('dart')
      expect(kinds).toContain('crumble')
      expect(kinds).toContain('altar')
      expect(exitReachable(floor)).toBe(true)
    }
  })

  it('generates Furnace and Flooded Ruins traversal terrain with baseline routes', () => {
    for (const seed of [14, 55, 1004]) {
      const furnace = generateAreaFloor(seed, 'furnace', 0)
      const furnaceKinds = furnace.tiles.map(tile => tile.kind)
      expect(furnaceKinds).toEqual(expect.arrayContaining(['smoke', 'lift', 'breakwall']))
      expect(exitReachable(furnace)).toBe(true)
      expect(furnace.milestones.filter(milestone => milestone.kind === 'augment')).toHaveLength(1)

      const flooded = generateAreaFloor(seed, 'floodedRuins', 0)
      const floodedKinds = flooded.tiles.map(tile => tile.kind)
      expect(floodedKinds).toEqual(expect.arrayContaining(['current', 'deepWater', 'anchor']))
      expect(exitReachable(flooded)).toBe(true)
      expect(flooded.milestones.filter(milestone => milestone.kind === 'augment')).toHaveLength(1)
    }
  })

  it('rejects unreachable objectives and illegal placements', () => {
    const floor = generateFloor(123, 0)
    const cache = floor.tiles.findIndex(tile => tile.kind === 'crate' || tile.kind === 'chest')
    for (const tile of floor.tiles) if (tile.kind === 'crate' || tile.kind === 'chest') tile.kind = 'floor'
    floor.tiles[cache].kind = 'crate'
    const x = cache % floor.width
    const y = Math.floor(cache / floor.width)
    for (const [dx, dy] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) floor.tiles[(y + dy) * floor.width + x + dx].kind = 'wall'
    floor.actors[0].x = 0
    floor.actors[0].y = 0
    expect(validateGeneration(floor)).toMatchObject({ valid: false, errors: expect.arrayContaining(['objective unreachable: recoverSupplies', 'illegal actor placement']) })
  })

  it('validates diagonal terrain routes the movement system can traverse', () => {
    const floor = generateFloor(123, 0)
    floor.tiles.forEach(tile => { tile.kind = 'wall' })
    floor.tiles[1 * floor.width + 1].kind = 'floor'
    floor.tiles[2 * floor.width + 2].kind = 'floor'
    expect(hasPassableTerrainPath(floor, { x: 1, y: 1 }, { x: 2, y: 2 })).toBe(true)
  })

  it('uses the same prop-aware traversal trace for reachability and diagnostics', () => {
    const floor = createFloor({ biome: 'mine', start: { x: 1, y: 1 }, exit: { x: 3, y: 1 }, props: [{ id: 'blocking-cart', kind: 'mine.brokenCart', biome: 'mine', x: 2, y: 1, state: 'dormant', tags: ['route', 'force', 'salvage'], hooks: ['operate', 'bomb', 'force', 'throw'] }] })
    floor.tiles.forEach(tile => { tile.kind = 'wall' })
    floor.tiles[1 * floor.width + 1].kind = 'floor'
    floor.tiles[1 * floor.width + 2].kind = 'rail'
    floor.tiles[1 * floor.width + 3].kind = 'exit'
    const trace = traverseFloor(floor, floor.start, { target: floor.exit })
    expect(trace.path).toBeUndefined()
    expect(trace.blockers).toContain('2,1:prop:blocking-cart')
    expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(false)
    expect(hasPassableTerrainPath(floor, floor.start, floor.exit)).toBe(true)
  })

  it('starts an explorer on a visible, passable map cell', () => {
    const run = newRun(42)
    expect(getTile(run.floor, run.hero.x, run.hero.y)?.visible).toBe(true)
    expect(getTile(run.floor, run.hero.x, run.hero.y)?.kind).not.toBe('wall')
  })

  it('uses a player action to advance the turn and refresh visibility', () => {
    const run = newRun(77)
    const turn = run.turn
    perform(run, 'l')
    refreshFov(run)
    expect(run.turn).toBe(turn + 1)
    expect(run.floor.tiles.some(tile => tile.explored)).toBe(true)
  })
})
