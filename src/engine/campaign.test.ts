import { describe, expect, it } from 'vitest'
import { completeCampaignArea, initialCampaignRoute, nextArea, unlockCampaignArea } from './campaign'
import { descend } from './inventory'
import { newRun } from './run'

describe('four-area campaign flow', () => {
  it('advances four local levels before returning to the hub', () => {
    const state = newRun(701, 'mine')
    for (const areaFloor of [0, 1, 2]) {
      state.hero.x = state.floor.exit.x
      state.hero.y = state.floor.exit.y
      state.floor.guardianDefeated = true
      expect(descend(state)).toEqual([{ type: 'floor' }])
      expect(state).toMatchObject({ area: 'mine', areaFloor: areaFloor + 1, floor: { biome: 'mine' } })
    }
    state.hero.x = state.floor.exit.x
    state.hero.y = state.floor.exit.y
    state.floor.guardianDefeated = true
    expect(descend(state)).toEqual([{ type: 'areaComplete' }])
    expect(state.areaFloor).toBe(3)
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
    expect(route).toEqual({ version: 1, completedAreas: ['mine'], unlockedAreas: ['mine'], selectedBiome: 'mine' })
    expect(route).not.toHaveProperty('hero')
  })
})
