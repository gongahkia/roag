import { describe, expect, it } from 'vitest'
import { merchantStock } from './rewards'
import { MAX_BOMBS, MAX_GOLD, grantGold } from './economy'
import { inventoryChoice, shopChoice } from './inventory'
import { createHero, createRun } from '../test/factories'
import { gateForArea, resolveAreaGate } from './gates'

describe('economy guardrails', () => {
  it('caps gold and consumable reserves', () => {
    const gold = createRun({ hero: createHero({ gold: MAX_GOLD - 4 }) })
    expect(grantGold(gold, 10)).toBe(4)
    expect(gold.hero.gold).toBe(MAX_GOLD)

    const supplies = createRun({ hero: createHero({ bombs: MAX_BOMBS - 1, inventory: ['bombPack'] }) })
    inventoryChoice(supplies, { kind: 'inventory', mode: 'use' }, '1')
    expect(supplies.hero.bombs).toBe(MAX_BOMBS)
    expect(supplies.hero.inventory).toEqual([])
  })

  it('blocks trivial purchases and prices route alternatives by their resource cost', () => {
    const shop = createRun({ hero: createHero({ gold: 500, bombs: MAX_BOMBS - 2 }) })
    const bombChoice = merchantStock(shop).indexOf('bombPack') + 1
    shopChoice(shop, String(bombChoice))
    expect(shop.hero.inventory).not.toContain('bombPack')
    expect(shop.hero.gold).toBe(500)

    const route = createRun()
    route.hero.inventory = ['ember']
    route.hero.gold = 19
    expect(resolveAreaGate(route, gateForArea('mine'), 1).resolved).toBe(false)
    route.hero.gold = 20
    expect(resolveAreaGate(route, gateForArea('mine'), 1)).toMatchObject({ resolved: true, destination: 'wilds' })
    expect(route.hero.gold).toBe(0)
  })
})
