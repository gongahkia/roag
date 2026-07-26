import { describe, expect, it } from 'vitest'
import { MONSTERS, SHOP_STOCK, biomeName } from '../content'
import { propDefinitionsFor } from '../props'
import { generateAreaFloor, getTile, validateGeneration } from '../world'
import { BOONS } from './buildcraft'
import { BIOME_POOL, campaignOrderForSeed } from './campaign'
import { RELICS } from './relics'
import { trailcraftChoices } from './trailcraft'
import { newRun } from './run'
import { damageHero, moveHero } from './combat'

const expansionBiomes = ['saltFlats', 'frostReliquary'] as const

describe('Salt Flats and Frost Reliquary content', () => {
  it('registers both biomes in the randomized campaign pool', () => {
    expect(BIOME_POOL).toEqual(expect.arrayContaining(expansionBiomes))
    expect(biomeName.saltFlats).toBe('Mirror Salt Flats')
    expect(biomeName.frostReliquary).toBe('Frost Reliquary')
    const seen = new Set<number>()
    for (let seed = 1; seed <= 160; seed++) for (const biome of campaignOrderForSeed(seed)) if (expansionBiomes.includes(biome as typeof expansionBiomes[number])) seen.add(expansionBiomes.indexOf(biome as typeof expansionBiomes[number]))
    expect(seen.size).toBe(2)
  })

  it('ships the requested local ecosystem and global build rewards', () => {
    for (const biome of expansionBiomes) {
      expect(propDefinitionsFor(biome)).toHaveLength(6)
      expect(MONSTERS.filter(monster => monster.biome === biome && monster.ai !== 'guardian')).toHaveLength(4)
      expect(MONSTERS.some(monster => monster.biome === biome && monster.ai === 'guardian')).toBe(true)
      expect(SHOP_STOCK[biome].length).toBeGreaterThan(5)
      expect(BOONS.filter(boon => boon.biomes?.includes(biome))).toHaveLength(10)
      expect(trailcraftChoices(newRun(91, biome)).some(choice => choice.biomes?.includes(biome))).toBe(true)
    }
    expect(RELICS.map(relic => relic.id)).toEqual(expect.arrayContaining(['prismRelay', 'winterSeal']))
  })

  it('generates valid, traversable floors and exposes every local encounter', () => {
    for (const biome of expansionBiomes) {
      const events = new Set<string>()
      for (let seed = 1; seed <= 24; seed++) {
        const floor = generateAreaFloor(seed, biome, seed % 4, 2)
        expect(validateGeneration(floor).valid).toBe(true)
        events.add(floor.encounters![0].kind)
      }
      expect(events.size).toBe(6)
    }
  }, 20_000)

  it('activates the new tile, Boon, and Relic hooks', () => {
    const salt = newRun(401, 'saltFlats')
    salt.floor.actors = []
    salt.hero.relics = ['prismRelay']
    salt.hero.boons = { sunstep: 1 }
    getTile(salt.floor, salt.hero.x + 1, salt.hero.y)!.kind = 'saltMirror'
    const focus = salt.hero.focus
    moveHero(salt, 'e')
    expect(salt.hero.focus).toBeGreaterThanOrEqual(focus)
    expect(salt.hero.relicCharges?.prismRelay).toBe(1)

    const frost = newRun(402, 'frostReliquary')
    frost.floor.actors = []
    frost.hero.relics = ['winterSeal']
    getTile(frost.floor, frost.hero.x + 1, frost.hero.y)!.kind = 'ice'
    moveHero(frost, 'e')
    const health = frost.hero.health
    damageHero(frost, 6, 'test damage')
    expect(frost.hero.health).toBe(health - 3)
    expect(frost.hero.relicCharges?.winterSeal).toBeUndefined()
  })
})
