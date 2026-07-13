import { ITEM } from '../content'
import { rngFor } from '../rng'
import type { Actor, Biome, CampaignRouteState, LegacyRecord, Point, RunState } from '../types'
import { appendLegacyRecord } from './campaign'

export const REVENANT_CHANCE = 20
export interface RevenantEncounter { actor: Actor; reward: { gold: number; item: 'tonic' } }

export const legacyRecordForDeath = (state: RunState, heirName: string, lineage: readonly string[]): LegacyRecord => ({
  id: `legacy:${state.seed}:${state.floor.index}:${state.turn}`,
  heirName,
  cause: 'defeated',
  biome: state.area ?? state.floor.biome,
  floor: state.areaFloor ?? state.floor.index % 4,
  seed: state.seed,
  lineage: [...lineage, heirName].slice(-12),
  location: { x: state.hero.x, y: state.hero.y },
  cache: { gold: state.hero.gold, items: [...new Set([...state.hero.inventory, ...Object.values(state.hero.equipment).filter((item): item is string => Boolean(item))])] },
  encounter: { kind: 'cache', resolved: false }
})

export const recordDeath = (campaign: CampaignRouteState, state: RunState, heirName: string): CampaignRouteState => appendLegacyRecord(campaign, legacyRecordForDeath(state, heirName, campaign.legacyRecords.map(record => record.heirName)))

export interface LegacyEncounterSelection { record?: LegacyRecord; campaign: CampaignRouteState }
export const selectLegacyEncounter = (campaign: CampaignRouteState, biome: Biome, seed: number): LegacyEncounterSelection => {
  if (campaign.legacyEncounterAreas.includes(biome)) return { campaign }
  const eligible = campaign.legacyRecords.filter(record => record.biome === biome && !record.encounter.resolved)
  if (!eligible.length) return { campaign }
  const record = rngFor(seed, 'legacy', `encounter:${biome}`).pick(eligible)
  return { record, campaign: { ...campaign, legacyEncounterAreas: [...campaign.legacyEncounterAreas, biome] } }
}

export const echoCacheEpitaph = (record: LegacyRecord): string => `${record.heirName} fell on ${record.biome} floor ${record.floor + 1}: ${record.cause}.`
export const recoverEchoCache = (campaign: CampaignRouteState, state: RunState, recordId: string): { campaign: CampaignRouteState; recovered: boolean } => {
  const record = campaign.legacyRecords.find(current => current.id === recordId)
  if (!record || record.encounter.kind !== 'cache' || record.encounter.resolved) return { campaign, recovered: false }
  state.hero.gold += record.cache.gold
  for (const item of record.cache.items) if (state.hero.inventory.length < 12) state.hero.inventory.push(item)
  return { campaign: { ...campaign, legacyRecords: campaign.legacyRecords.map(current => current.id === recordId ? { ...current, encounter: { ...current.encounter, resolved: true } } : current) }, recovered: true }
}

export const createRevenantEncounter = (record: LegacyRecord, seed: number, point: Point): RevenantEncounter | undefined => {
  const rng = rngFor(seed, 'legacy', `revenant:${record.id}`)
  if (!rng.chance(REVENANT_CHANCE)) return undefined
  const formerGear = record.cache.items.map(id => ITEM[id]).filter((item): item is typeof ITEM[string] => Boolean(item?.weapon || item?.slot || item?.tags?.length))
  const gear = formerGear.length ? rng.pick(formerGear) : undefined
  const tags = ['revenant', ...(gear ? [`altered:${gear.id}`, ...(gear.weapon?.tags ?? gear.tags ?? [])] : ['altered:memory'])]
  const floor = Math.max(0, Math.min(3, record.floor))
  const weaponDamage = gear?.weapon?.damage ?? 0
  const armor = gear?.defense ?? 0
  return {
    actor: { id: `revenant:${record.id}`, role: 'monster', kind: 'revenant', name: `${record.heirName}'s revenant`, x: point.x, y: point.y, health: 14 + floor * 4, maxHealth: 14 + floor * 4, attack: 4 + floor + Math.floor(weaponDamage / 4), defense: Math.min(16, 10 + floor + armor), speed: 105, energy: 0, glyph: 'R', color: '#d2a4e8', hostile: true, ai: 'chase', status: tags, conditions: [] },
    reward: { gold: 24 + floor * 8, item: 'tonic' }
  }
}

export const claimRevenantReward = (campaign: CampaignRouteState, state: RunState, recordId: string, seed: number): { campaign: CampaignRouteState; recovered: boolean } => {
  const record = campaign.legacyRecords.find(current => current.id === recordId)
  const encounter = record && !record.encounter.resolved ? createRevenantEncounter(record, seed, { x: 0, y: 0 }) : undefined
  if (!record || !encounter) return { campaign, recovered: false }
  state.hero.gold += encounter.reward.gold
  if (state.hero.inventory.length < 12) state.hero.inventory.push(encounter.reward.item)
  return { campaign: { ...campaign, legacyRecords: campaign.legacyRecords.map(current => current.id === recordId ? { ...current, encounter: { kind: 'revenant', resolved: true } } : current) }, recovered: true }
}
