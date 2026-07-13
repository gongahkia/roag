import { describe, expect, it } from 'vitest'
import { castSpell } from './inventory'
import { createEnemy, createHero, createRun } from '../test/factories'
import { getTile } from '../world'

describe('Verdant', () => {
  it('mends and roots enemies in place', () => {
    const mend = createRun({ hero: createHero({ health: 10, inventory: ['mend'] }) })
    castSpell(mend, 'mend', 'e')
    expect(mend.hero.health).toBe(19)

    const target = createEnemy({ x: 3, y: 1, health: 30, maxHealth: 30, energy: 0 })
    const root = createRun({ hero: createHero({ inventory: ['root'] }) })
    root.floor.actors = [target]
    castSpell(root, 'root', 'e')
    expect(target).toMatchObject({ x: 3, y: 1, conditions: [{ kind: 'rooted', duration: 2, potency: 1 }] })
  })

  it('quenches fire and uses lull to stop a creature turn', () => {
    const water = createRun({ hero: createHero({ inventory: ['waterScript'] }) })
    water.floor.tiles[1 * 48 + 3].kind = 'fireVent'
    castSpell(water, 'waterScript', 'e')
    expect(getTile(water.floor, 3, 1)?.kind).toBe('floor')

    const target = createEnemy({ x: 3, y: 1, energy: 0 })
    const lull = createRun({ hero: createHero({ inventory: ['lull'] }) })
    lull.floor.actors = [target]
    castSpell(lull, 'lull', 'e')
    expect(target).toMatchObject({ x: 3, y: 1, conditions: [{ kind: 'staggered', duration: 2, potency: 1 }] })
  })
})
