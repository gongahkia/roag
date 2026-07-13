import { describe, expect, it } from 'vitest'
import { ITEMS, MONSTERS } from './content'
import { newRun, perform, refreshFov } from './engine'
import { FLOOR_COUNT, MAP_HEIGHT, MAP_WIDTH } from './types'
import { generateFloor, getTile, validateFloor } from './world'

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
      if (!next || seen.has(key) || ['wall', 'lava', 'pit', 'crate', 'chest', 'lockedDoor'].includes(next.kind)) continue
      seen.add(key)
      queue.push({ x, y })
    }
  }
  return false
}

describe('expedition generation', () => {
  it('provides the locked full content roster', () => {
    expect(ITEMS).toHaveLength(30)
    expect(MONSTERS.filter(monster => monster.ai === 'guardian')).toHaveLength(4)
    expect(MONSTERS.filter(monster => monster.ai !== 'guardian')).toHaveLength(20)
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
