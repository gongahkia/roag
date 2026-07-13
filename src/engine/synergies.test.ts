import { describe, expect, it } from 'vitest'
import { moveHero } from './combat'
import { castSpell } from './inventory'
import { resolveSynergies } from './synergies'
import { createEnemy, createHero, createRun } from '../test/factories'

describe('cross-system synergies', () => {
  it('resolves tagged geometry and effect modifiers deterministically', () => {
    expect(resolveSynergies({ items: ['whip'], skills: ['str1'] }, { range: 2 })).toMatchObject({ values: { range: 3 }, synergies: ['strength-reach'] })
    expect(resolveSynergies({ scripts: ['ember'], terrain: ['gas'] })).toMatchObject({ values: { damage: 2 }, synergies: ['ember-gas'] })
  })

  it('changes combat geometry and Ember damage while reporting synergies', () => {
    const meleeTarget = createEnemy({ x: 4, y: 1, health: 99, maxHealth: 99, defense: 0, speed: 0 })
    const melee = createRun({ hero: createHero({ equipment: { mainHand: 'whip' }, skills: ['str1'] }) })
    melee.floor.actors = [meleeTarget]
    moveHero(melee, 'e')
    expect(melee.hero.x).toBe(1)
    expect(meleeTarget.health).toBeLessThan(99)
    expect(melee.messages).toContain('Synergy: Iron Grip extends your reach.')

    const emberTarget = createEnemy({ x: 3, y: 1, health: 30, maxHealth: 30, speed: 0 })
    const ember = createRun({ hero: createHero({ inventory: ['ember'] }) })
    ember.floor.tiles[1 * 48 + 2].kind = 'gas'
    ember.floor.actors = [emberTarget]
    castSpell(ember, 'ember', 'e')
    expect(emberTarget.health).toBe(22)
    expect(ember.messages).toContain('Synergy: Ember ignites the gas with extra force.')
  })
})
