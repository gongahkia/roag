import { describe, expect, it } from 'vitest'
import { BIOME_POOL, campaignOrderForSeed } from './campaign'
import { newRun } from './run'
import { useRope } from './inventory'
import { generateAreaFloor, spawnMonster } from '../world'
import { chooseEncounter } from './encounters'
import { damageHero } from './combat'

describe('scalable biome content', () => {
  it('draws four unique biomes in deterministic random order and exposes every biome in every route slot', () => {
    const seen = Array.from({ length: 4 }, () => new Set<string>())
    for (let seed = 1; seed <= 512; seed++) {
      const first = campaignOrderForSeed(seed)
      expect(campaignOrderForSeed(seed)).toEqual(first)
      expect(first).toHaveLength(4)
      expect(new Set(first).size).toBe(4)
      first.forEach((biome, slot) => seen[slot].add(biome))
    }
    for (const slot of seen) expect(slot.size).toBe(BIOME_POOL.length)
  })

  it('scales the same biome by route threat instead of assigning biome difficulty', () => {
    const early = generateAreaFloor(77, 'cliffs', 0, 0)
    const late = generateAreaFloor(77, 'cliffs', 0, 3)
    expect(early.difficulty?.threat).toBe(0)
    expect(late.difficulty?.threat).toBe(12)
    expect(late.actors.find(actor => actor.hostile)?.maxHealth).toBeGreaterThan(early.actors.find(actor => actor.hostile)?.maxHealth ?? 0)
    expect(late.actors.find(actor => actor.hostile)?.attack).toBeGreaterThan(early.actors.find(actor => actor.hostile)?.attack ?? 0)
  })

  it('preserves authored enemy baseline stats before threat scaling', () => {
    const mine = spawnMonster('mole', { x: 1, y: 1 }, 'mine', undefined)
    const furnace = spawnMonster('liftwarden', { x: 1, y: 1 }, 'furnace', undefined)
    expect(mine).toMatchObject({ maxHealth: 9, attack: 4, defense: 10, speed: 90, combatRole: 'pursuer' })
    expect(furnace).toMatchObject({ maxHealth: 24, attack: 12, defense: 19, speed: 85, combatRole: 'pursuer' })
  })

  it('anchors and climbs a reusable vertical cliff rope', () => {
    const state = newRun(88, 'cliffs')
    const link = state.floor.climbLinks?.[0]
    expect(link).toBeDefined()
    state.hero.ropes = 1
    state.hero.x = link!.lower.x
    state.hero.y = link!.lower.y
    useRope(state)
    expect(link!.anchored).toBe(true)
    expect(state.hero.ropes).toBe(0)
    useRope(state)
    expect(state.hero).toMatchObject(link!.upper)
  })

  it('distinguishes explicitly lethal and severe cursed-object failures', () => {
    const lethal = newRun(89, 'mine')
    lethal.floor.encounters = [{ id: 'curse', kind: 'cursedObject', x: lethal.hero.x, y: lethal.hero.y, state: 'dormant' }]
    chooseEncounter(lethal, 'curse', '1')
    damageHero(lethal, 1, 'test')
    expect(lethal.status).toBe('dead')

    const severe = newRun(90, 'mine')
    severe.floor.encounters = [{ id: 'curse', kind: 'cursedObject', x: severe.hero.x, y: severe.hero.y, state: 'dormant' }]
    const initialMaxHealth = severe.hero.maxHealth
    chooseEncounter(severe, 'curse', '2')
    damageHero(severe, 1, 'test')
    expect(severe.status).toBe('playing')
    expect(severe.hero.maxHealth).toBe(initialMaxHealth - 3)
    expect(severe.hero.curse).toBeUndefined()
  })
})
