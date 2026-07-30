import { assertCampaignCycle } from './engine/campaign'
import type { CampaignCycle, CampaignDifficultyPackageId, CampaignDifficultyPackageMetadata, CampaignTier, DifficultyContext } from './types'

export const CAMPAIGN_DIFFICULTY_PACKAGE_VERSION = 1 as const
export interface CampaignDifficultyModifiers { threat: number; healthMultiplier: number; attackBonus: number; defenseBonus: number; eliteChance: number; guardianPattern: number; hazardMultiplier: number; rewardMultiplier: number }
export interface CampaignDifficultyPackage extends CampaignDifficultyPackageMetadata { version: typeof CAMPAIGN_DIFFICULTY_PACKAGE_VERSION; id: CampaignDifficultyPackageId; tier: CampaignTier; modifiers: CampaignDifficultyModifiers }

export const CAMPAIGN_DIFFICULTY_PACKAGES = [
  { version: 1, id: 'base-v1', tier: 'base', name: 'Base Route', rationale: 'Preserves route-position difficulty without campaign-tier modifiers.', modifiers: { threat: 0, healthMultiplier: 1, attackBonus: 0, defenseBonus: 0, eliteChance: 0, guardianPattern: 0, hazardMultiplier: 1, rewardMultiplier: 1 } },
  { version: 1, id: 'ng-plus-v1', tier: 'ngPlus', name: 'New Game+', rationale: 'Adds fixed pressure after route difficulty while retaining a modest deterministic reward uplift.', modifiers: { threat: 3, healthMultiplier: 1.15, attackBonus: 1, defenseBonus: 1, eliteChance: 8, guardianPattern: 1, hazardMultiplier: 1.15, rewardMultiplier: 1.1 } },
  { version: 1, id: 'ng-plus-plus-v1', tier: 'ngPlusPlus', name: 'New Game++', rationale: 'Raises fixed late-cycle pressure and rewards without any inventory, elapsed-time, or run-count scaling.', modifiers: { threat: 6, healthMultiplier: 1.3, attackBonus: 2, defenseBonus: 2, eliteChance: 16, guardianPattern: 2, hazardMultiplier: 1.3, rewardMultiplier: 1.2 } }
] as const satisfies readonly CampaignDifficultyPackage[]

const threatCap = 15
const eliteCap = 35
const guardianPatternCap = 3
const packageForTier = (tier: CampaignTier): CampaignDifficultyPackage => {
  const value = CAMPAIGN_DIFFICULTY_PACKAGES.find(candidate => candidate.tier === tier)
  if (!value) throw new Error(`missing campaign difficulty package for ${tier}`)
  return value
}

export const campaignDifficultyPackageForCycle = (cycle: CampaignCycle): CampaignDifficultyPackage | undefined => {
  assertCampaignCycle(cycle)
  return cycle.completedCap ? undefined : structuredClone(packageForTier(cycle.currentTier))
}

export const campaignDifficultyPackageMetadata = (cycle: CampaignCycle): CampaignDifficultyPackageMetadata | undefined => {
  const current = campaignDifficultyPackageForCycle(cycle)
  return current ? { version: current.version, id: current.id, tier: current.tier, name: current.name, rationale: current.rationale } : undefined
}

export const resolveCampaignDifficulty = (routeDifficulty: DifficultyContext, cycle: CampaignCycle): DifficultyContext => {
  const current = campaignDifficultyPackageForCycle(cycle)
  if (!current) return { ...routeDifficulty }
  const { modifiers } = current
  return {
    ...routeDifficulty,
    threat: Math.min(threatCap, routeDifficulty.threat + modifiers.threat),
    healthMultiplier: routeDifficulty.healthMultiplier * modifiers.healthMultiplier,
    attackBonus: routeDifficulty.attackBonus + modifiers.attackBonus,
    defenseBonus: routeDifficulty.defenseBonus + modifiers.defenseBonus,
    eliteChance: Math.min(eliteCap, routeDifficulty.eliteChance + modifiers.eliteChance),
    guardianPattern: Math.min(guardianPatternCap, Math.max(routeDifficulty.guardianPattern, modifiers.guardianPattern)),
    hazardMultiplier: modifiers.hazardMultiplier,
    rewardMultiplier: modifiers.rewardMultiplier,
    campaignTier: current.tier,
    difficultyPackage: { version: current.version, id: current.id, tier: current.tier, name: current.name, rationale: current.rationale }
  }
}
