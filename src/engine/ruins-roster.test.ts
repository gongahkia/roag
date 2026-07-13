import { describe, expect, it } from 'vitest'
import { MONSTERS } from '../content'
import { advance, moveHero } from './combat'
import { createEnemy, createRun } from '../test/factories'
import { getTile } from '../world'

describe('Ruins enemy roster', () => {
  it('adds ward, dart, lock, and ritual roles', () => {
    expect(MONSTERS.filter(monster => monster.biome === 'ruins').map(monster => monster.id)).toEqual(expect.arrayContaining(['wardacolyte', 'dartadept', 'lockkeeper', 'ritualist']))
    expect(MONSTERS.find(monster => monster.id === 'ritualist')?.tags).toEqual(expect.arrayContaining(['ritual', 'telegraph']))
  })

  it('raises wards, arms darts, seals doors, and marks targets', () => {
    const ward = createRun()
    const acolyte = createEnemy({ id: 'wardacolyte', kind: 'wardacolyte', name: 'Ward Acolyte', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })
    ward.floor.actors = [acolyte]
    advance(ward, [])
    expect(acolyte.conditions).toContainEqual({ kind: 'shielded', duration: 2, potency: 2 })

    const darts = createRun()
    darts.floor.actors = [createEnemy({ id: 'dartadept', kind: 'dartadept', name: 'Dart Adept', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(darts, [])
    moveHero(darts, 'e')
    expect(getTile(darts.floor, 1, 1)?.kind).toBe('dart')

    const lock = createRun()
    lock.floor.tiles[1 * 48 + 3].kind = 'door'
    lock.floor.actors = [createEnemy({ id: 'lockkeeper', kind: 'lockkeeper', name: 'Lock Keeper', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(lock, [])
    expect(getTile(lock.floor, 3, 1)?.kind).toBe('lockedDoor')

    const ritual = createRun()
    ritual.floor.actors = [createEnemy({ id: 'ritualist', kind: 'ritualist', name: 'Ash Ritualist', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(ritual, [])
    advance(ritual, [])
    expect(ritual.hero.conditions).toContainEqual({ kind: 'marked', duration: 1, potency: 1 })
  })
})
