import { describe, expect, it } from 'vitest'
import { initialCampaignRoute, unlockCampaignArea } from './campaign'
import { gateForArea, resolveAreaGate } from './gates'
import { createRun } from '../test/factories'

describe('campaign, gate, and lineage integration', () => {
  it('keeps a non-NPC gate route open in every area and keeps NPC sacrifices run-only', () => {
    const alternatives = [
      ['mine', 1, ['ember'], [], 20, 0], ['mine', 2, [], [], 8, 1], ['wilds', 1, ['lantern', 'ropeBundle'], [], 25, 0], ['wilds', 2, ['blinkRune'], [], 15, 0],
      ['caverns', 1, ['ward'], ['int6'], 40, 0], ['caverns', 2, ['sunseal'], [], 0, 0], ['ruins', 1, ['ember'], [], 55, 0], ['ruins', 2, [], [], 20, 1],
      ['furnace', 1, ['liftKey'], [], 65, 0], ['furnace', 2, ['pickaxe'], [], 30, 0], ['floodedRuins', 0, ['anchorSpool'], [], 90, 0], ['floodedRuins', 2, [], [], 90, 1]
    ] as const
    for (const [biome, choice, inventory, skills, gold, bombs] of alternatives) {
      const state = createRun({ area: biome })
      state.hero.inventory = [...inventory]
      state.hero.skills = [...skills]
      state.hero.gold = gold
      state.hero.bombs = bombs
      const resolved = resolveAreaGate(state, gateForArea(biome), choice)
      expect(resolved.resolved).toBe(true)
      expect(resolved.sacrificedNpc).toBeUndefined()
      expect(state.lineageEvents ?? []).toEqual([])
      expect(unlockCampaignArea(initialCampaignRoute(), resolved.destination!).unlockedAreas).toContain(resolved.destination!)
    }
    const state = createRun({ area: 'mine', rescuedNpcs: [{ id: 'scout-1', name: 'Lost Scout', biome: 'mine', floor: 1 }] })
    const resolution = resolveAreaGate(state, gateForArea('mine'), 0)
    expect(resolution).toMatchObject({ resolved: true, sacrificedNpc: { id: 'scout-1' } })
    expect(resolution.lineageEvent).toBeUndefined()
    expect(state.lineageEvents ?? []).toEqual([])
  })
})
