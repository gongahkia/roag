import { describe, expect, it } from 'vitest'
import { migrateCampaignRoute } from '../storage'
import type { CampaignRouteState, Records } from '../types'
import { completeCampaignTier, continueCampaignRoute, initialCampaignRoute } from './campaign'
import { companionLeadForRescue, loseCompanionForRescue } from './companions'
import { snapshotCampaignCarryover, transferCampaignCarryover } from './carryover'
import { newHero } from './run'

const records: Records = { bestDepth: 17, wins: 3, deaths: 2, runs: [{ seed: 7, floor: 4, score: 221, won: true, date: '2026-07-30T00:00:00.000Z' }], analyses: [] }

describe('New Game+ carryover', () => {
  it('preserves full inventory, companion injury/loss, records, and irreversible state exactly once', () => {
    const hero = newHero({ name: 'Ari', origin: 'tidebound', calling: 'pathmaker', deathMode: 'ironTrail' })
    hero.inventory = ['tonic', 'tonic', 'focusTonic', 'rock', 'bombPack', 'ropeBundle', 'ember', 'sight', 'tideSpear', 'cap', 'boots', 'sunseal']
    hero.gold = 487
    hero.bombs = 1
    hero.ropes = 0
    hero.keys = 3
    hero.level = 8
    hero.xp = 333
    hero.skills = ['agi1', 'vit1']
    hero.cooldowns = { dash: 3, ward: 1 }
    hero.traversalTools = ['stoneWedge']
    hero.relics = ['ashCircuit']
    hero.relicCharges = { ashCircuit: 0 }
    hero.boons = { windfall: 2 }
    hero.boonEvolutions = { windfall: 1 }
    const activeRescue = { id: 'rescue:mine:2:mika', name: 'Mika', biome: 'mine' as const, floor: 2 }
    const lostRescue = { id: 'rescue:wilds:1:bo', name: 'Bo', biome: 'wilds' as const, floor: 1 }
    const active = companionLeadForRescue(activeRescue)
    active.rosterStatus = 'active'
    active.controlMode = 'direct'
    active.injury = 'injured'
    active.abilityState = { cooldowns: { intercept: 2 }, retired: ['old-call'] }
    active.toolState = { cooldown: 4, retired: true }
    const lost = loseCompanionForRescue(companionLeadForRescue(lostRescue, 'direct'), lostRescue.id)
    const campaign: CampaignRouteState = { ...initialCampaignRoute(), completedAreas: ['mine', 'wilds', 'caverns', 'ruins'], unlockedAreas: ['mine', 'wilds', 'caverns', 'ruins'], selectedBiome: 'ruins', rescuedNpcs: [activeRescue], companions: [active, lost], companionControlMode: 'direct', companionControlHistory: [{ sequence: 0, mode: 'autonomous', source: 'creation' }, { sequence: 1, mode: 'direct', source: 'lodge' }], lineageEvents: [{ id: 'sacrifice:bo', kind: 'npcSacrifice', npcId: lostRescue.id, npcName: 'Bo', biome: 'wilds', floor: 1, gateId: 'wilds-caverns-pass', seed: 7 }], legacyRecords: [{ id: 'legacy:ari', heirName: 'Ari', biome: 'mine', floor: 3, seed: 7 }], alignment: { kami: 2, villagePact: 4 }, reputation: { trailfolk: 3, kami: -2 }, cycle: completeCampaignTier(initialCampaignRoute().cycle) }
    const snapshot = snapshotCampaignCarryover(hero, campaign, records)
    const transfer = transferCampaignCarryover(continueCampaignRoute(campaign), snapshot)
    expect(transfer).toMatchObject({ hero, records, campaign: { completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [activeRescue], companions: [active, lost], lineageEvents: campaign.lineageEvents, legacyRecords: campaign.legacyRecords, alignment: campaign.alignment, reputation: campaign.reputation, cycle: { currentTier: 'ngPlus' } }, diagnostic: { fromTier: 'base', toTier: 'ngPlus', inventory: { before: hero.inventory, after: hero.inventory, added: [], removed: [] }, roster: { added: [], removed: [], changed: [] } } })
    expect(transfer.campaign.carryoverDiagnostics).toHaveLength(1)
    expect(migrateCampaignRoute(JSON.parse(JSON.stringify(transfer.campaign)))).toEqual(transfer.campaign)
    expect(transferCampaignCarryover(transfer.campaign, snapshot)).toMatchObject({ hero, records, campaign: { companions: [active, lost], carryoverDiagnostics: [transfer.diagnostic] } })
    transfer.hero.inventory.pop()
    transfer.campaign.companions[0]!.abilityState.cooldowns.intercept = 0
    expect(hero.inventory).toHaveLength(12)
    expect(active.abilityState.cooldowns.intercept).toBe(2)
  })
})
