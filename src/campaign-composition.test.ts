import { describe, expect, it } from 'vitest'
import { campaignAreaOrders, sweepCampaignComposition } from './campaign-composition'
import { completeCampaignArea, initialCampaignRoute, recordCampaignSacrifice, unlockCampaignArea, BIOME_POOL } from './engine/campaign'
import { newHero, newRun } from './engine/run'

describe('shuffled campaign composition', () => {
  it('enumerates every supported four-biome order exactly once', () => {
    const orders = campaignAreaOrders()
    expect(orders).toHaveLength(5_040)
    expect(new Set(orders.map(order => order.join('>'))).size).toBe(orders.length)
    expect(orders.every(order => order.length === 4 && new Set(order).size === 4 && order.every(biome => BIOME_POOL.includes(biome)))).toBe(true)
  })

  it('keeps every ordering self-contained and contrastful', () => {
    const report = sweepCampaignComposition(77123)
    expect(report.orders).toBe(5_040)
    expect(report.errors).toEqual([])
    expect(report.repeatedRecipeEscalations).toEqual([])
    expect(report.weakDiversity).toEqual([])
    expect(Object.keys(report.transitions)).toHaveLength(BIOME_POOL.length * (BIOME_POOL.length - 1))
    expect(Object.values(report.transitions)).toEqual(Array(BIOME_POOL.length * (BIOME_POOL.length - 1)).fill(168))
  }, 30_000)

  it('preserves lineage state and inherited boons through shuffled successors', () => {
    for (const order of campaignAreaOrders()) {
      const npc = { id: 'scout', name: 'Scout', biome: order[0], floor: 1 }
      const lineage = { id: `sacrifice:${order[0]}`, kind: 'npcSacrifice' as const, npcId: npc.id, npcName: npc.name, biome: order[0], floor: 1, gateId: `gate:${order[0]}`, seed: 77123 }
      let route = { ...initialCampaignRoute(), areaOrder: [...order], rescuedNpcs: [npc], legacyRecords: [{ id: `legacy:${order[0]}`, heirName: 'Ari', biome: order[0], floor: 1, seed: 77123 }] }
      route = recordCampaignSacrifice(route, lineage)
      for (const [index, biome] of order.entries()) {
        route = completeCampaignArea(route, biome)
        if (order[index + 1]) route = unlockCampaignArea(route, order[index + 1])
      }
      expect(route.completedAreas).toEqual(order)
      expect(route.lineageEvents).toEqual([lineage])
      expect(route.legacyRecords).toHaveLength(1)
    }
    const hero = newHero()
    hero.boons = { sunstep: 2 }
    hero.traversalTools = ['reedwing']
    const successor = newRun(77123, 'frostReliquary', 0, hero, [], [], ['mine', 'frostReliquary', 'burial', 'cliffs'])
    expect(successor.hero).toMatchObject({ boons: { sunstep: 2 }, traversalTools: ['reedwing'] })
    expect(successor.floor.escalation?.phase).toBe('survey')
  }, 30_000)
})
