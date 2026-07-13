import { describe, expect, it } from 'vitest'
import { MONSTERS } from '../content'
import { advance, moveHero } from './combat'
import { hasCondition } from './conditions'
import { createEnemy, createRun } from '../test/factories'
import { getTile } from '../world'

describe('Wilds enemy roster', () => {
  it('adds root, water-mobility, and web roles', () => {
    expect(MONSTERS.filter(monster => monster.biome === 'wilds').map(monster => monster.id)).toEqual(expect.arrayContaining(['vinebinder', 'marshskater', 'webweaver']))
    expect(MONSTERS.find(monster => monster.id === 'webweaver')?.tags).toEqual(expect.arrayContaining(['web', 'snare', 'telegraph']))
  })

  it('telegraphs roots and webs, while water accelerates Marsh Skaters', () => {
    const rooted = createRun()
    rooted.floor.actors = [createEnemy({ id: 'vinebinder', kind: 'vinebinder', name: 'Vine Binder', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(rooted, [])
    advance(rooted, [])
    expect(rooted.hero.conditions).toContainEqual({ kind: 'rooted', duration: 1, potency: 1 })

    const root = createRun()
    root.floor.actors = [createEnemy({ id: 'vinebinder', kind: 'vinebinder', name: 'Vine Binder', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(root, [])
    expect(root.floor.telegraphs).toMatchObject([{ actionId: 'enemy-root' }])
    moveHero(root, 'e')
    expect(hasCondition(root.hero, 'rooted')).toBe(false)

    const web = createRun()
    web.floor.actors = [createEnemy({ id: 'webweaver', kind: 'webweaver', name: 'Web Weaver', ai: 'ranged', x: 4, y: 1, speed: 100, energy: 0 })]
    advance(web, [])
    advance(web, [])
    expect(web.hero.conditions).toContainEqual({ kind: 'slowed', duration: 1, potency: 1 })
    expect(getTile(web.floor, 1, 1)?.kind).toBe('web')

    const water = createRun()
    const skater = createEnemy({ id: 'marshskater', kind: 'marshskater', name: 'Marsh Skater', x: 4, y: 1, speed: 100, energy: 0 })
    water.floor.tiles[1 * 48 + 4].kind = 'water'
    water.floor.actors = [skater]
    advance(water, [])
    expect(skater).toMatchObject({ x: 3, energy: 50 })
  })
})
