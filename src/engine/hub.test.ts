import { describe, expect, it } from 'vitest'
import { advanceCampaignTier, completeCampaignTier, initialCampaignCycle } from './campaign'
import { companionLeadForRescue, loseCompanionForRescue } from './companions'
import { buyHubItem, createHubState, equipHubItem, hubCampaignStatus, hubCarryoverSummary, hubEquipment, hubView } from './hub'
import { initialRoute, navigate } from './routing'
import { newHero } from './run'

describe('hub state', () => {
  it('provides a courier name, routes, roster, and supplies', () => {
    const state = createHubState(42)
    expect(state).toEqual(createHubState(42))
    expect(state.unlockedAreas).toEqual(['mine'])
    expect(hubView('Mika', state)).toEqual({ courierName: 'Mika', state })
  })

  it('keeps hub destinations physical instead of binding them to menu keys', () => {
    const hub = { ...initialRoute(), screen: 'hub' as const }
    expect(navigate(hub, 'r', false)).toBe(hub)
    expect(navigate(hub, 'Enter', false)).toBe(hub)
    expect(navigate(hub, 'Escape', false).screen).toBe('title')
  })

  it('uses hub cash purchases and equipment swaps without a run state', () => {
    const hero = newHero({ name: 'Mika', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint' })
    hero.gold = 100
    expect(buyHubItem(hero, 'cap')).toMatchObject({ changed: true })
    expect(hero.gold).toBe(45)
    expect(equipHubItem(hero, 'cap')).toMatchObject({ changed: true })
    expect(hero.equipment.head).toBe('cap')
    hero.inventory.push('machete')
    expect(hubEquipment(hero)).toContain('machete')
    expect(equipHubItem(hero, 'machete')).toMatchObject({ changed: true })
    expect(hero.equipment.mainHand).toBe('machete')
    expect(hero.inventory).toContain('whip')
  })

  it('explains declined purchases and equipment changes', () => {
    const hero = newHero({ name: 'Mika', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint' })
    expect(buyHubItem(hero, 'cap')).toEqual({ changed: false, message: 'Need 55 more cash.' })
    expect(equipHubItem(hero, 'cap')).toEqual({ changed: false, message: 'Bark Cap is not in your pack.' })
  })

  it('projects fixed tier history and read-only carryover for the campaign UI', () => {
    const base = initialCampaignCycle()
    const plus = advanceCampaignTier(completeCampaignTier(base))
    const terminal = completeCampaignTier(advanceCampaignTier(completeCampaignTier(plus)))
    const hero = newHero({ name: 'Ari', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint' })
    hero.gold = 88
    hero.inventory = ['tonic', 'tonic', 'cap']
    hero.traversalTools = ['stoneWedge']
    const injured = companionLeadForRescue({ id: 'rescue:mine:1:mika', name: 'Mika', biome: 'mine', floor: 1 })
    injured.injury = 'injured'
    const lost = loseCompanionForRescue(companionLeadForRescue({ id: 'rescue:wilds:1:bo', name: 'Bo', biome: 'wilds', floor: 1 }), 'rescue:wilds:1:bo')
    const carryover = hubCarryoverSummary(hero, [injured, lost])
    const view = hubView('Ari', createHubState(42), { hero, cycle: plus, carryover })
    expect(view.campaign).toMatchObject({ tier: 'ngPlus', tierLabel: 'NG+', completedTiers: ['base'], completedLabel: 'BASE', historyLabel: 'BASE ✓ → NG+ ACTIVE', packageName: 'New Game+', difficultyLines: ['HP ×1.15 · ATK +1 · DEF +1', 'THREAT +3 · ELITE +8 · GUARD 1', 'HAZARD ×1.15 · REWARDS ×1.10'], nextLabel: 'NEXT: finish NG+ to unlock NG++.', continuationPending: false, terminal: false })
    expect(view.carryover).toMatchObject({ currency: 88, items: ['Vital Tonic', 'Vital Tonic', 'Bark Cap'], tools: ['Stone Wedge'], roster: [{ name: 'Mika', status: 'INJURED' }, { name: 'Bo', status: 'LOST' }], injuries: ['Mika (INJURED)'], losses: ['Bo'] })
    expect(hubCampaignStatus(terminal)).toMatchObject({ tier: 'ngPlusPlus', completedTiers: ['base', 'ngPlus', 'ngPlusPlus'], terminal: true, continuationPending: false, nextLabel: 'TERMINAL: NG++ complete — no next tier.' })
  })
})
