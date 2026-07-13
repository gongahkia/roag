import { describe, expect, it } from 'vitest'
import { MONSTERS } from '../content'
import { advance } from './combat'
import { createEnemy, createRun } from '../test/factories'

describe('Mine enemy roster', () => {
  it('adds rail and telegraph roles with explicit counterplay tags', () => {
    expect(MONSTERS.filter(monster => monster.biome === 'mine').map(monster => monster.id)).toEqual(expect.arrayContaining(['railguard', 'fusewarden']))
    expect(MONSTERS.find(monster => monster.id === 'fusewarden')?.tags).toEqual(expect.arrayContaining(['telegraph', 'cover', 'explosive']))
  })

  it('accelerates Rail Guards on rails and telegraphs Fuse Warden shots that cover can block', () => {
    const rail = createRun()
    const railguard = createEnemy({ id: 'railguard', kind: 'railguard', name: 'Rail Guard', x: 4, y: 1, speed: 100, energy: 0 })
    rail.floor.tiles[1 * 48 + 4].kind = 'rail'
    rail.floor.actors = [railguard]
    advance(rail, [])
    expect(railguard).toMatchObject({ x: 3, energy: 50 })

    const open = createRun()
    open.floor.actors = [createEnemy({ id: 'fusewarden', kind: 'fusewarden', name: 'Fuse Warden', ai: 'ranged', x: 5, y: 1, energy: 0 })]
    advance(open, [])
    expect(open.floor.telegraphs).toMatchObject([{ actionId: 'enemy-shot' }])
    expect(open.messages).toContain('The Fuse Warden primes a blast line; find cover.')

    const covered = createRun()
    covered.floor.tiles[1 * 48 + 3].kind = 'wall'
    covered.floor.actors = [createEnemy({ id: 'fusewarden', kind: 'fusewarden', name: 'Fuse Warden', ai: 'ranged', x: 5, y: 1, energy: 0 })]
    advance(covered, [])
    expect(covered.floor.telegraphs).toEqual([])
    expect(covered.messages).toContain("Fuse Warden's shot is blocked by cover.")
  })
})
