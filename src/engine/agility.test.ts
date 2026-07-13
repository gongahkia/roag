import { describe, expect, it } from 'vitest'
import { advance, moveHero } from './combat'
import { announceTelegraph } from './telegraphs'
import { agilityEvasion, agilityMoveDistance, agilityReachBonus, agilityTelegraphAvoidance } from './agility'
import { createEnemy, createHero, createRun } from '../test/factories'

describe('Agility disciplines', () => {
  it('maps discipline nodes to movement, evasion, reach, and telegraph avoidance', () => {
    const hero = createHero({ skills: ['agi1', 'agi2', 'agi3', 'agi4', 'agi5', 'agi6'] })
    expect(agilityMoveDistance(hero)).toBe(3)
    expect(agilityReachBonus(hero)).toBe(1)
    expect(agilityEvasion(hero)).toBe(3)
    expect(agilityTelegraphAvoidance(hero)).toBe(55)
  })

  it('moves farther, reaches farther, evades attacks, and avoids telegraphs', () => {
    const movement = createRun({ hero: createHero({ skills: ['agi1', 'agi5'] }) })
    expect(moveHero(movement, 'e')).toEqual([{ type: 'move' }])
    expect(movement.hero.x).toBe(4)

    const reach = createRun({ hero: createHero({ skills: ['agi2'] }) })
    const distant = createEnemy({ x: 3, y: 1, defense: 0, speed: 0 })
    reach.floor.actors = [distant]
    moveHero(reach, 'e')
    expect(reach.hero.x).toBe(1)
    expect(distant.health).toBeLessThan(distant.maxHealth)

    const attacked = (seed: number, skills: string[]) => {
      const state = createRun({ seed, hero: createHero({ skills }) })
      state.floor.actors = [createEnemy({ id: 'attacker', x: 2, y: 1, attack: 10 })]
      advance(state, [])
      return state.hero.health
    }
    const evasionSeed = Array.from({ length: 100 }, (_, seed) => seed).find(seed => attacked(seed, []) < 22 && attacked(seed, ['agi4']) === 22)!
    expect(attacked(evasionSeed, ['agi4'])).toBe(22)

    const telegraphed = (seed: number, skills: string[]) => {
      const state = createRun({ seed, hero: createHero({ skills }) })
      state.floor.actors = [createEnemy({ id: 'source', x: 2, y: 1, attack: 20, speed: 0 })]
      announceTelegraph(state, { id: 'shot', sourceId: 'source', actionId: 'enemy-shot', cells: [{ x: 1, y: 1 }], danger: 'major', windup: 1 })
      advance(state, [])
      return state.hero.health
    }
    const avoidanceSeed = Array.from({ length: 100 }, (_, seed) => seed).find(seed => telegraphed(seed, []) < 22 && telegraphed(seed, ['agi3']) === 22)!
    expect(telegraphed(avoidanceSeed, ['agi3'])).toBe(22)
  })
})
