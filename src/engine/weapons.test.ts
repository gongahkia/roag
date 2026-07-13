import { describe, expect, it } from 'vitest'
import { ITEMS } from '../content'
import { moveHero } from './combat'
import { createEnemy, createHero, createRun } from '../test/factories'

describe('weapon tactical profiles', () => {
  it('replaces weapon damage-only data with reach, shape, cooldown, and tags', () => {
    const weapons = ITEMS.filter(item => item.weapon)
    expect(weapons).toHaveLength(5)
    for (const weapon of weapons) {
      expect(weapon).not.toHaveProperty('damage')
      expect(weapon.weapon).toMatchObject({ reach: expect.any(Number), shape: expect.any(String), cooldown: expect.any(Number), tags: expect.any(Array) })
      expect(weapon.weapon!.tags.length).toBeGreaterThan(0)
    }
  })

  it('uses profile reach and enforces cooldowns', () => {
    const state = createRun({ hero: createHero({ equipment: { mainHand: 'whip' } }) })
    const distant = createEnemy({ x: 3, y: 1, defense: 0 })
    state.floor.actors = [distant]
    moveHero(state, 'e')
    expect(distant.health).toBeLessThan(distant.maxHealth)
    state.hero.equipment.mainHand = 'machete'
    const adjacent = createEnemy({ id: 'adjacent', x: 2, y: 1, defense: 0 })
    state.floor.actors = [adjacent]
    moveHero(state, 'e')
    expect(state.hero.cooldowns?.machete).toBe(1)
  })
})
