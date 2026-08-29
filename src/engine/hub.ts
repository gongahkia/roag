import { streamSeed } from '../rng'
import { ITEM, shopStock } from '../content'
import { CAMPAIGN_DIFFICULTY_PACKAGES } from '../campaign-difficulty'
import type { Biome, CampaignCycle, CampaignTier, Companion, CompanionControlMode, CompanionDeathMode, Hero, HubState, ItemId, Point } from '../types'
import { toolFor } from './buildcraft'
import { purchaseBlocker } from './economy'

export type HubAction = 'routes' | 'roster' | 'shop' | 'outfitter' | 'continuation'
export interface HubCampaignStatus { tier: CampaignTier; tierLabel: string; completedTiers: CampaignTier[]; completedLabel: string; historyLabel: string; packageName: string; packageRationale: string; difficultyLines: string[]; nextLabel: string; continuationPending: boolean; terminal: boolean; accessibleLabel: string }
export interface HubCarryoverRosterEntry { name: string; status: string; injury: string; permanentlyLost: boolean }
export interface HubCarryoverSummary { currency: number; items: string[]; tools: string[]; roster: HubCarryoverRosterEntry[]; injuries: string[]; losses: string[]; accessibleLabel: string }
export interface HubOptions { hero?: Hero; biome?: Biome; notice?: string; position?: Point; cycle?: CampaignCycle; carryover?: HubCarryoverSummary; companions?: Companion[]; companionControlMode?: CompanionControlMode; companionDeathMode?: CompanionDeathMode }
export interface HubView { courierName: string; state: HubState; hero?: Hero; stock?: ItemId[]; equipment?: ItemId[]; notice?: string; position?: Point; cycle?: CampaignCycle; campaign?: HubCampaignStatus; carryover?: HubCarryoverSummary; companions?: Companion[]; companionControlMode?: CompanionControlMode; companionDeathMode?: CompanionDeathMode }
export interface HubMutation { changed: boolean; message: string }

const packLimit = 12
const tierLabel = (tier: CampaignTier): string => tier === 'base' ? 'BASE' : tier === 'ngPlus' ? 'NG+' : 'NG++'
const numberLabel = (value: number): string => Number.isInteger(value) ? String(value) : value.toFixed(2)
const companionsStatus = (companion: Companion): string => companion.permanentlyLost ? 'LOST' : companion.injury === 'recovering' ? `RECOVERING ${companion.recoveryFloors ?? 0}F` : companion.injury === 'injured' ? 'INJURED' : companion.rosterStatus.toUpperCase()

export const hubCampaignStatus = (cycle: CampaignCycle): HubCampaignStatus => {
  const current = CAMPAIGN_DIFFICULTY_PACKAGES.find(candidate => candidate.tier === cycle.currentTier)
  if (!current) throw new Error(`missing campaign difficulty package for ${cycle.currentTier}`)
  const completedTiers = [...cycle.completedTiers]
  const completedLabel = completedTiers.length ? completedTiers.map(tierLabel).join(', ') : 'NONE'
  const continuationPending = !cycle.completedCap && completedTiers.includes(cycle.currentTier)
  const terminal = cycle.completedCap
  const nextTier: Partial<Record<CampaignTier, CampaignTier>> = { base: 'ngPlus', ngPlus: 'ngPlusPlus' }
  const next = nextTier[cycle.currentTier]
  const historyLabel = [...completedTiers.map(tier => `${tierLabel(tier)} ✓`), ...(completedTiers.includes(cycle.currentTier) ? [] : [`${tierLabel(cycle.currentTier)} ACTIVE`])].join(' → ')
  const difficultyLines = [
    `HP ×${numberLabel(current.modifiers.healthMultiplier)} · ATK +${current.modifiers.attackBonus} · DEF +${current.modifiers.defenseBonus}`,
    `THREAT +${current.modifiers.threat} · ELITE +${current.modifiers.eliteChance} · GUARD ${current.modifiers.guardianPattern}`,
    `HAZARD ×${numberLabel(current.modifiers.hazardMultiplier)} · REWARDS ×${numberLabel(current.modifiers.rewardMultiplier)}`
  ]
  const nextLabel = terminal ? 'TERMINAL: every Voyager route is complete.' : continuationPending ? `NEXT: ${tierLabel(next!)} revised route ready at the flight console.` : next ? `NEXT: finish ${tierLabel(cycle.currentTier)} to unlock ${tierLabel(next)}.` : 'NEXT: finish NG++ to complete the final route.'
  return { tier: cycle.currentTier, tierLabel: tierLabel(cycle.currentTier), completedTiers, completedLabel, historyLabel, packageName: current.name, packageRationale: current.rationale, difficultyLines, nextLabel, continuationPending, terminal, accessibleLabel: `Voyager route tier ${tierLabel(cycle.currentTier)}. Completed tiers: ${completedLabel}. Fixed difficulty package ${current.name}: ${difficultyLines.join('; ')}. ${nextLabel}` }
}

export const hubCarryoverSummary = (hero: Hero, companions: readonly Companion[]): HubCarryoverSummary => {
  const roster = companions.map(companion => ({ name: companion.name, status: companionsStatus(companion), injury: companion.injury, permanentlyLost: companion.permanentlyLost }))
  const items = hero.inventory.map(id => ITEM[id].name)
  const tools = (hero.traversalTools ?? []).map(id => toolFor(id).name)
  const injuries = roster.filter(entry => !entry.permanentlyLost && entry.injury !== 'healthy').map(entry => `${entry.name} (${entry.status})`)
  const losses = roster.filter(entry => entry.permanentlyLost).map(entry => entry.name)
  return { currency: hero.gold, items, tools, roster, injuries, losses, accessibleLabel: `Voyager refits are retained: ${hero.gold} cash; items: ${items.join(', ') || 'none'}; tools: ${tools.join(', ') || 'none'}; roster: ${roster.map(entry => `${entry.name} ${entry.status}`).join(', ') || 'none'}; injuries: ${injuries.join(', ') || 'none'}; losses: ${losses.join(', ') || 'none'}.` }
}

export const createHubState = (seed: number): HubState => ({ season: streamSeed(seed, 'generation', 'hub-season') % 4, supplies: ['tonic', 'ropeBundle', 'rock'], rescued: [], unlockedAreas: ['mine'], completedAreas: [] })
export const hubStock = (biome: Biome): ItemId[] => [...new Set(['tonic', 'focusTonic', 'bombPack', 'ropeBundle', ...shopStock(biome)])]
export const hubEquipment = (hero: Hero): ItemId[] => [...new Set([...Object.values(hero.equipment).filter((id): id is ItemId => Boolean(id)), ...hero.inventory].filter(id => Boolean(ITEM[id]?.slot)))]
export const hubView = (courierName: string, state: HubState, options: HubOptions = {}): HubView => ({
  courierName,
  state,
  ...(options.hero ? { hero: options.hero, stock: hubStock(options.biome ?? 'mine'), equipment: hubEquipment(options.hero) } : {}),
  ...(options.notice ? { notice: options.notice } : {}),
  ...(options.position ? { position: options.position } : {}),
  ...(options.cycle ? { cycle: { ...options.cycle, completedTiers: [...options.cycle.completedTiers], events: options.cycle.events.map(event => ({ ...event })) } } : {}),
  ...(options.cycle ? { campaign: hubCampaignStatus(options.cycle) } : {}),
  ...(options.carryover ? { carryover: { ...options.carryover, items: [...options.carryover.items], tools: [...options.carryover.tools], roster: options.carryover.roster.map(entry => ({ ...entry })), injuries: [...options.carryover.injuries], losses: [...options.carryover.losses] } } : {}),
  ...(options.companions ? { companions: structuredClone(options.companions) } : {}),
  ...(options.companionControlMode ? { companionControlMode: options.companionControlMode } : {}),
  ...(options.companionDeathMode ? { companionDeathMode: options.companionDeathMode } : {})
})

export const buyHubItem = (hero: Hero, id: ItemId): HubMutation => {
  const item = ITEM[id]
  if (!item) return { changed: false, message: 'That stock is unavailable.' }
  if (item.slot && Object.values(hero.equipment).includes(id)) return { changed: false, message: `${item.name} is already equipped.` }
  const blocker = purchaseBlocker(hero, id)
  if (blocker) return { changed: false, message: blocker }
  if (hero.inventory.length >= packLimit) return { changed: false, message: 'Your pack is full.' }
  if (hero.gold < item.value) return { changed: false, message: `Need ${item.value - hero.gold} more cash.` }
  hero.gold -= item.value
  hero.inventory.push(id)
  return { changed: true, message: `Bought ${item.name}.` }
}

export const equipHubItem = (hero: Hero, id: ItemId): HubMutation => {
  const item = ITEM[id]
  if (!item?.slot) return { changed: false, message: 'That cannot be equipped.' }
  if (hero.equipment[item.slot] === id) return { changed: false, message: `${item.name} is already equipped.` }
  const index = hero.inventory.indexOf(id)
  if (index < 0) return { changed: false, message: `${item.name} is not in your pack.` }
  const previous = hero.equipment[item.slot]
  hero.inventory.splice(index, 1)
  if (previous) { hero.inventory.push(previous); hero.lastUnequipped = previous }
  hero.equipment[item.slot] = id
  return { changed: true, message: `Equipped ${item.name}.` }
}
