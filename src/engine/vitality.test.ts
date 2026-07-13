import { describe, expect, it } from 'vitest'
import { damageHero, moveHero } from './combat'
import { inventoryChoice, operate } from './inventory'
import { vitalityHazardReduction, vitalityRecovery, vitalityRescueRecovery, vitalityShield } from './vitality'
import { createEnemy, createFloor, createHero, createRun } from '../test/factories'
import { indexOf } from '../types'

describe('Vitality disciplines', () => {
  it('maps nodes to recovery, shielding, hazards, and rescue survivability', () => {
    const hero = createHero({ skills: ['vit2', 'vit3', 'vit4', 'vit5', 'vit6'] })
    expect(vitalityRecovery(hero)).toBe(4)
    expect(vitalityShield(hero)).toBe(1)
    expect(vitalityHazardReduction(hero)).toBe(2)
    expect(vitalityRescueRecovery(hero)).toBe(6)
  })

  it('applies recovery, shield, hazard, and rescue effects in the game flow', () => {
    const recovery = createRun({ hero: createHero({ health: 5, skills: ['vit2', 'vit4'], inventory: ['tonic'] }) })
    inventoryChoice(recovery, { kind: 'inventory', mode: 'use' }, '1')
    expect(recovery.hero.health).toBe(19)

    const shielded = createRun({ hero: createHero({ skills: ['vit3'] }) })
    damageHero(shielded, 5, 'test')
    expect(shielded.hero.health).toBe(18)

    const hazard = createRun({ hero: createHero({ skills: ['vit5'] }) })
    hazard.floor.tiles[indexOf(2, 1)].kind = 'boulder'
    moveHero(hazard, 'e')
    expect(hazard.hero.health).toBe(18)

    const scout = createEnemy({ id: 'scout', role: 'ally', kind: 'ally', name: 'lost scout', hostile: false, x: 1, y: 1 })
    const rescue = createRun({ hero: createHero({ health: 5, skills: ['vit6'] }), floor: createFloor({ actors: [scout] }) })
    rescue.floor.tiles[indexOf(1, 1)].kind = 'rescue'
    operate(rescue)
    expect(rescue.hero.health).toBe(19)
  })
})
