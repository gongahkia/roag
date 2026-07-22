import { describe, expect, it } from 'vitest'
import { ITEMS, MONSTERS } from './content'
import { newRun, perform, refreshFov } from './engine'
import { hasLine } from './engine/visibility'
import { FLOOR_COUNT, MAP_HEIGHT, MAP_WIDTH } from './types'
import { generateAreaFloor, generateFloor, getTile, hasPassableTerrainPath, validateFloor, validateGeneration } from './world'

const exitReachable = (floor: ReturnType<typeof generateFloor>): boolean => {
  const seen = new Set<string>([`${floor.start.x},${floor.start.y}`])
  const queue = [{ ...floor.start }]
  while (queue.length) {
    const current = queue.shift()!
    if (current.x === floor.exit.x && current.y === floor.exit.y) return true
    for (const [dx, dy] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) {
      const x = current.x + dx
      const y = current.y + dy
      const next = getTile(floor, x, y)
      const key = `${x},${y}`
      if (!next || seen.has(key) || ['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest', 'lockedDoor'].includes(next.kind)) continue
      seen.add(key)
      queue.push({ x, y })
    }
  }
  return false
}

describe('expedition generation', () => {
  it('provides the locked full content roster', () => {
    expect(ITEMS).toHaveLength(36)
    expect(MONSTERS.filter(monster => monster.ai === 'guardian')).toHaveLength(4)
    expect(MONSTERS.filter(monster => monster.ai !== 'guardian' && monster.spawn !== 'triggered')).toHaveLength(33)
    expect(MONSTERS.find(monster => monster.id === 'startledBirds')?.spawn).toBe('triggered')
  })

  it('builds valid deterministic floors across the complete run', () => {
    for (let floor = 0; floor < FLOOR_COUNT; floor++) {
      const first = generateFloor(12345, floor)
      const second = generateFloor(12345, floor)
      expect(validateFloor(first)).toBe(true)
      expect(first.tiles).toHaveLength(MAP_WIDTH * MAP_HEIGHT)
      expect(first.exit).toEqual(second.exit)
      expect(first.objective).toEqual(second.objective)
      expect(first.actors.map(actor => actor.kind)).toEqual(second.actors.map(actor => actor.kind))
      expect(exitReachable(first)).toBe(true)
    }
  })

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
    run.floor.tiles[1 * 48 + 2].kind = 'bramble'
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
    run.floor.tiles[1 * 48 + 2].kind = 'darkness'
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

  it('rejects unreachable objectives and illegal placements', () => {
    const floor = generateFloor(123, 0)
    const cache = floor.tiles.findIndex(tile => tile.kind === 'crate' || tile.kind === 'chest')
    for (const tile of floor.tiles) if (tile.kind === 'crate' || tile.kind === 'chest') tile.kind = 'floor'
    floor.tiles[cache].kind = 'crate'
    const x = cache % MAP_WIDTH
    const y = Math.floor(cache / MAP_WIDTH)
    for (const [dx, dy] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) floor.tiles[(y + dy) * MAP_WIDTH + x + dx].kind = 'wall'
    floor.actors[0].x = 0
    floor.actors[0].y = 0
    expect(validateGeneration(floor)).toMatchObject({ valid: false, errors: expect.arrayContaining(['objective unreachable: recoverSupplies', 'illegal actor placement']) })
  })

  it('validates diagonal terrain routes the movement system can traverse', () => {
    const floor = generateFloor(123, 0)
    floor.tiles.forEach(tile => { tile.kind = 'wall' })
    floor.tiles[1 * MAP_WIDTH + 1].kind = 'floor'
    floor.tiles[2 * MAP_WIDTH + 2].kind = 'floor'
    expect(hasPassableTerrainPath(floor, { x: 1, y: 1 }, { x: 2, y: 2 })).toBe(true)
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
