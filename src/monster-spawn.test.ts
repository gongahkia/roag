import { describe, expect, it } from 'vitest'
import { spawnMonster } from './world'

describe('monster spawning', () => {
  it('copies authored stats, role, tags, and terrain affinity to runtime actors', () => {
    expect(spawnMonster('mole', { x: 1, y: 1 }, 'mole')).toMatchObject({ maxHealth: 9, health: 9, attack: 4, defense: 10, speed: 90, combatRole: 'pursuer', tags: [], terrainAffinity: [] })
    expect(spawnMonster('fumeeel', { x: 1, y: 1 }, 'eel')).toMatchObject({ maxHealth: 13, attack: 7, defense: 12, speed: 100, combatRole: 'pursuer', tags: expect.arrayContaining(['gas', 'mobility']), terrainAffinity: expect.arrayContaining(['gas', 'smoke']) })
  })

  it('applies route threat after authored baseline stats', () => {
    expect(spawnMonster('sapper', { x: 1, y: 1 }, 'early', { routePosition: 0, threat: 0, healthMultiplier: 1, attackBonus: 0, defenseBonus: 0, eliteChance: 0, guardianPattern: 0 })).toMatchObject({ maxHealth: 8, attack: 5, defense: 9 })
    expect(spawnMonster('sapper', { x: 1, y: 1 }, 'late', { routePosition: 3, threat: 12, healthMultiplier: 1.72, attackBonus: 6, defenseBonus: 2, eliteChance: 28, guardianPattern: 3 })).toMatchObject({ maxHealth: 14, attack: 11, defense: 11 })
  })
})
