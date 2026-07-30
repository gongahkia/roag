import { describe, expect, it } from 'vitest'
import { runAutoplay } from './autoplay-runner'
import { resolveCampaignDifficulty } from './campaign-difficulty'
import { perform } from './engine/input'
import { advanceCampaignTier, completeCampaignTier, initialCampaignCycle } from './engine/campaign'
import { newRun } from './engine/run'
import { damageHero } from './engine/combat'
import { grantGold } from './engine/economy'
import { generateAreaFloor } from './world'

const base = initialCampaignCycle()
const plus = advanceCampaignTier(completeCampaignTier(base))
const plusPlus = advanceCampaignTier(completeCampaignTier(plus))

describe('campaign difficulty application', () => {
  it('routes each generated campaign floor through exactly one tier context', () => {
    const baseFloor = generateAreaFloor(808, 'mine', 0, 0, base)
    const plusFloor = generateAreaFloor(808, 'mine', 0, 0, plus)
    const plusPlusFloor = generateAreaFloor(808, 'mine', 0, 0, plusPlus)
    expect(baseFloor.difficulty).toMatchObject({ campaignTier: 'base', difficultyPackage: { id: 'base-v1' }, threat: 0, healthMultiplier: 1, attackBonus: 0, defenseBonus: 0, eliteChance: 4, guardianPattern: 0, hazardMultiplier: 1, rewardMultiplier: 1 })
    expect(plusFloor.difficulty).toMatchObject({ campaignTier: 'ngPlus', difficultyPackage: { id: 'ng-plus-v1' }, threat: 3, healthMultiplier: 1.15, attackBonus: 1, defenseBonus: 1, eliteChance: 12, guardianPattern: 1, hazardMultiplier: 1.15, rewardMultiplier: 1.1 })
    expect(plusPlusFloor.difficulty).toMatchObject({ campaignTier: 'ngPlusPlus', difficultyPackage: { id: 'ng-plus-plus-v1' }, threat: 6, healthMultiplier: 1.3, attackBonus: 2, defenseBonus: 2, eliteChance: 20, guardianPattern: 2, hazardMultiplier: 1.3, rewardMultiplier: 1.2 })
    expect(plusFloor.items.length).toBeGreaterThan(baseFloor.items.length)
    expect(plusPlusFloor.items.length).toBeGreaterThan(plusFloor.items.length)
    const baseActor = baseFloor.actors.find(actor => actor.hostile)!
    const plusActor = plusFloor.actors.find(actor => actor.id === baseActor.id)!
    expect(plusActor).toMatchObject({ health: expect.any(Number), attack: baseActor.attack + 1, defense: baseActor.defense + 1 })
    expect(plusActor.maxHealth).toBeGreaterThanOrEqual(baseActor.maxHealth)
    expect(() => resolveCampaignDifficulty(plusFloor.difficulty!, plus)).toThrow('campaign difficulty package already applied')
  })

  it('applies tiered guardian, hazard, reward, floor-transition, and replay context deterministically', () => {
    const guardian = generateAreaFloor(809, 'mine', 3, 0, plusPlus)
    expect(guardian.actors.find(actor => actor.role === 'guardian')).toMatchObject({ attack: expect.any(Number), defense: expect.any(Number), status: expect.arrayContaining(['pattern:2']) })
    const baseState = newRun(810, 'mine', 0, undefined, [], [], undefined, base)
    const plusState = newRun(810, 'mine', 0, undefined, [], [], undefined, plus)
    damageHero(baseState, 4, 'test hazard', true)
    damageHero(plusState, 4, 'test hazard', true)
    expect(plusState.hero.health).toBeLessThan(baseState.hero.health)
    expect(grantGold(baseState, 10)).toBe(10)
    expect(grantGold(plusState, 10)).toBe(11)
    plusState.floor.objective.status = 'complete'
    plusState.floor.guardianDefeated = true
    plusState.hero.x = plusState.floor.exit.x
    plusState.hero.y = plusState.floor.exit.y
    perform(plusState, 'q')
    expect(plusState.floor.difficulty).toMatchObject({ campaignTier: 'ngPlus', difficultyPackage: { id: 'ng-plus-v1' } })
    const report = runAutoplay(newRun(811, 'mine', 0, undefined, [], [], undefined, plusPlus), { mode: 'visible', policy: 'clear', turnLimit: 1, captureTrace: true })
    expect(report.replay.difficulty).toMatchObject({ campaignTier: 'ngPlusPlus', difficultyPackage: { id: 'ng-plus-plus-v1' } })
    expect(generateAreaFloor(812, 'wilds', 2, 1, plusPlus)).toEqual(generateAreaFloor(812, 'wilds', 2, 1, plusPlus))
  })
})
