import { describe, expect, it } from 'vitest'
import { damageHero, moveHero } from './combat'
import { canBreakRubble, canKnockback, strengthGuard, strengthMeleeBonus } from './strength'
import { createEnemy, createHero, createRun } from '../test/factories'

describe('Strength disciplines', () => {
  it('maps discipline nodes to melee, guard, rubble, and knockback mechanics', () => {
    const hero = createHero({ skills: ['str1', 'str2', 'str3', 'str4', 'str5', 'str6'] })
    expect(strengthMeleeBonus(hero)).toBe(4)
    expect(strengthGuard(hero)).toBe(2)
    expect(canBreakRubble(hero)).toBe(true)
    expect(canKnockback(hero)).toBe(true)
  })

  it('breaks rubble, guards damage, and knocks surviving melee targets back', () => {
    const rubble = createRun({ hero: createHero({ skills: ['str3'] }) })
    rubble.floor.tiles[rubble.hero.y * 48 + rubble.hero.x + 1].kind = 'rubble'
    expect(moveHero(rubble, 'e')).toEqual([{ type: 'boom' }, { type: 'move' }])
    expect(rubble).toMatchObject({ hero: { x: 2, y: 1 }, floor: { tiles: expect.any(Array) } })
    expect(rubble.floor.tiles[rubble.hero.y * 48 + rubble.hero.x].kind).toBe('floor')

    const guarded = createRun({ hero: createHero({ skills: ['str4'] }) })
    damageHero(guarded, 5, 'test')
    expect(guarded.hero.health).toBe(19)

    const melee = (skills: string[]) => {
      const state = createRun({ seed: 701, hero: createHero({ skills }) })
      const target = createEnemy({ id: 'melee', x: 2, y: 1, health: 99, maxHealth: 99, defense: 0, speed: 0 })
      state.floor.actors = [target]
      moveHero(state, 'e')
      return target.health
    }
    expect(melee([]) - melee(['str1', 'str2', 'str6'])).toBe(4)

    const state = createRun({ hero: createHero({ skills: ['str5'] }) })
    const target = createEnemy({ x: 2, y: 1, health: 99, maxHealth: 99, defense: 0 })
    state.floor.actors = [target]
    moveHero(state, 'e')
    expect(target).toMatchObject({ x: 3, y: 1, health: expect.any(Number) })
    expect(target.health).toBeLessThan(99)
  })
})
