import { describe, expect, it } from 'vitest'
import { appendLegacyRecord, completeCampaignArea, initialCampaignRoute, nextArea, recordCampaignSacrifice, unlockCampaignArea } from './campaign'
import { descend } from './inventory'
import { newRun } from './run'

describe('four-area campaign flow', () => {
  it('advances four local levels before returning to the hub', () => {
    const state = newRun(701, 'mine')
    for (const areaFloor of [0, 1, 2]) {
      state.hero.x = state.floor.exit.x
      state.hero.y = state.floor.exit.y
      state.floor.guardianDefeated = true
      state.floor.objective.status = 'complete'
      expect(descend(state)).toEqual([{ type: 'floor' }])
      expect(state).toMatchObject({ area: 'mine', areaFloor: areaFloor + 1, floor: { biome: 'mine' } })
    }
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    state.floor.guardianDefeated = true
    state.floor.objective.status = 'complete'
    expect(descend(state)).toEqual([{ type: 'areaComplete' }])
    expect(state.areaFloor).toBe(3)
  })

  it('rejects the exit until the current objective is complete', () => {
    const state = newRun(703, 'mine')
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    expect(descend(state)).toEqual([])
    expect(state.messages[0]).toBe('Objective incomplete: Secure a trail cache.')
    state.floor.objective.status = 'complete'
    expect(descend(state)).toEqual([{ type: 'floor' }])
  })

  it('unlocks and starts the next area while carrying the heir', () => {
    const mine = newRun(702, 'mine')
    mine.hero.gold = 55
    const wilds = newRun(702, nextArea('mine')!, 0, mine.hero)
    expect(unlockCampaignArea(initialCampaignRoute(), 'wilds').unlockedAreas).toEqual(['mine', 'wilds'])
    expect(wilds).toMatchObject({ area: 'wilds', areaFloor: 0, floor: { biome: 'wilds' }, hero: { gold: 55 } })
  })

  it('records routes without embedding hero power', () => {
    const route = completeCampaignArea(initialCampaignRoute(), 'mine')
    expect(route).toEqual({ version: 2, completedAreas: ['mine'], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], lineageEvents: [], legacyRecords: [] })
    expect(route).not.toHaveProperty('hero')
  })

  it('removes a sacrificed NPC and retains its lineage event once', () => {
    const route = { ...initialCampaignRoute(), rescuedNpcs: [{ id: 'scout-1', name: 'Lost Scout', biome: 'mine' as const, floor: 1 }] }
    const event = { id: 'sacrifice:mine-wilds-pass:scout-1', kind: 'npcSacrifice' as const, npcId: 'scout-1', npcName: 'Lost Scout', biome: 'mine' as const, floor: 1, gateId: 'mine-wilds-pass', seed: 702 }
    expect(recordCampaignSacrifice(recordCampaignSacrifice(route, event), event)).toMatchObject({ rescuedNpcs: [], lineageEvents: [event] })
  })

  it('keeps the latest twelve death records', () => {
    let route = initialCampaignRoute()
    for (let i = 0; i < 13; i++) route = appendLegacyRecord(route, { id: `legacy-${i}`, heirName: 'Ari', biome: 'mine', floor: i % 4, seed: i })
    expect(route.legacyRecords.map(record => record.id)).toEqual(Array.from({ length: 12 }, (_, i) => `legacy-${i + 1}`))
  })
})
