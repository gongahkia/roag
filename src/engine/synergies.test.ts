import { describe, expect, it } from 'vitest'
import { moveHero } from './combat'
import { castSpell } from './inventory'
import { resolveSynergies } from './synergies'
import { createEnemy, createHero, createRun } from '../test/factories'

describe('cross-system synergies', () => {
  it('resolves tagged geometry and effect modifiers deterministically', () => {
    expect(resolveSynergies({ items: ['whip'], skills: ['str1'] }, { range: 2 })).toMatchObject({ values: { range: 3 }, synergies: ['strength-reach'] })
    expect(resolveSynergies({ scripts: ['ember'], terrain: ['gas'] })).toMatchObject({ values: { damage: 2 }, synergies: ['ember-gas'] })
    expect(resolveSynergies({ tags: ['flintTemper', 'windKnot'] }, { range: 2 })).toMatchObject({ values: { range: 3 }, synergies: ['tempered-gale'] })
    expect(resolveSynergies({ tags: ['barkBinding', 'spiritThread'] })).toMatchObject({ values: { focus: 1 }, synergies: ['hearth-thread'] })
    expect(resolveSynergies({ items: ['cordmarkTalisman', 'reedstepBoots'] }, { range: 1 })).toMatchObject({ values: { range: 2 }, synergies: ['cordmark-reedstep'] })
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

    const wayfinderTarget = createEnemy({ x: 3, y: 1, health: 99, maxHealth: 99, defense: 0, speed: 0 })
    const wayfinder = createRun({ hero: createHero({ equipment: { charm: 'cordmarkTalisman', boots: 'reedstepBoots' } }) })
    wayfinder.floor.actors = [wayfinderTarget]
    moveHero(wayfinder, 'e')
    expect(wayfinderTarget.health).toBeLessThan(99)
    expect(wayfinder.messages).toContain('Synergy: Cordmark Talisman and Reedstep Boots lengthen your strike.')
  })

  it('applies trailcraft pairs to strikes and casts', () => {
    const target = createEnemy({ x: 4, y: 1, health: 99, maxHealth: 99, defense: 0, speed: 0 })
    const melee = createRun({ hero: createHero({ equipment: { mainHand: 'whip' }, trailcrafts: { flintTemper: 1, windKnot: 1 } }) })
    melee.floor.actors = [target]
    moveHero(melee, 'e')
    expect(target.health).toBeLessThan(99)
    expect(melee.messages).toContain('Synergy: Flint Temper and Wind Knot extend your strike.')

    const ember = createRun({ hero: createHero({ focus: 4, inventory: ['ember'], trailcrafts: { barkBinding: 1, spiritThread: 1 } }) })
    castSpell(ember, 'ember', 'e')
    expect(ember.hero.focus).toBe(2)
    expect(ember.messages).toContain('Synergy: Bark Binding and Spirit Thread restore focus.')
  })
})
