import { describe, expect, it } from 'vitest'
import { initialCampaignRoute, recordCampaignSacrifice, unlockCampaignArea } from './campaign'
import { gateForArea, resolveAreaGate } from './gates'
import { activateSacrificialAnchor, claimRevenantReward, createRevenantEncounter, createSacrificialAnchor, recordDeath, recoverEchoCache, selectLegacyEncounter } from './legacy'
import { newRun } from './run'
import { createLegacy, createRun } from '../test/factories'

const seedFor = (matches: (seed: number) => boolean): number => Array.from({ length: 100 }, (_, seed) => seed).find(matches)!

describe('campaign, gate, and lineage integration', () => {
  it('records a death, selects its cache once, and restores it to a successor', () => {
    const fallen = newRun(811, 'mine', 1)
    fallen.hero.gold = 37
    fallen.hero.inventory = ['tonic', 'ropeBundle']
    let campaign = recordDeath(initialCampaignRoute(), fallen, 'Ari Vale')
    const selection = selectLegacyEncounter(campaign, 'mine', 812)
    expect(selection.record).toMatchObject({ heirName: 'Ari Vale', floor: 1, cache: { gold: 37, items: ['tonic', 'ropeBundle', 'whip'] } })
    campaign = selection.campaign
    const successor = newRun(813, 'mine')
    const recovery = recoverEchoCache(campaign, successor, selection.record!.id)
    expect(recovery.recovered).toBe(true)
    expect(successor.hero).toMatchObject({ gold: 37, inventory: expect.arrayContaining(['tonic', 'ropeBundle', 'whip']) })
    expect(recoverEchoCache(recovery.campaign, successor, selection.record!.id).recovered).toBe(false)
  })

  it('resolves revenant and sacrificial-anchor variants once', () => {
    const revenant = createLegacy({ id: 'revenant', floor: 2, cache: { gold: 90, items: ['whip'] } })
    const revenantSeed = seedFor(seed => Boolean(createRevenantEncounter(revenant, seed, { x: 3, y: 3 })))
    const revenantState = createRun()
    const revenantResult = claimRevenantReward({ ...initialCampaignRoute(), legacyRecords: [revenant] }, revenantState, revenant.id, revenantSeed)
    expect(revenantResult).toMatchObject({ recovered: true, campaign: { legacyRecords: [{ encounter: { kind: 'revenant', resolved: true } }] } })
    expect(revenantState.hero.gold).toBe(40)

    const anchor = createLegacy({ id: 'anchor', cause: 'sacrificed' })
    const anchorSeed = seedFor(seed => Boolean(createSacrificialAnchor(anchor, seed)))
    const anchorState = createRun()
    anchorState.floor.tiles[anchorState.hero.y * 48 + anchorState.hero.x + 1].kind = 'bramble'
    const anchorResult = activateSacrificialAnchor({ ...initialCampaignRoute(), legacyRecords: [anchor] }, anchorState, anchor.id, anchorSeed)
    expect(anchorResult).toMatchObject({ activated: true, campaign: { legacyRecords: [{ encounter: { kind: 'anchor', resolved: true } }] } })
    expect(anchorState.floor.tiles[anchorState.hero.y * 48 + anchorState.hero.x + 1].kind).toBe('floor')
  })

  it('keeps a non-NPC gate route open in every area and records an NPC sacrifice only when chosen', () => {
    const alternatives = [
      ['mine', 1, ['ember'], [], 20, 0], ['mine', 2, [], [], 8, 1], ['wilds', 1, ['lantern', 'ropeBundle'], [], 25, 0], ['wilds', 2, ['blinkRune'], [], 15, 0],
      ['caverns', 1, ['ward'], ['astral1'], 40, 0], ['caverns', 2, ['sunseal'], [], 0, 0], ['ruins', 0, ['ward'], [], 75, 0], ['ruins', 1, ['ember'], [], 75, 0]
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
    const campaign = recordCampaignSacrifice(initialCampaignRoute(), resolution.lineageEvent!)
    expect(resolution).toMatchObject({ resolved: true, sacrificedNpc: { id: 'scout-1' } })
    expect(campaign).toMatchObject({ rescuedNpcs: [], lineageEvents: [{ npcId: 'scout-1', gateId: 'mine-wilds-pass' }] })
  })
})
