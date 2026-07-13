import { rngFor } from '../rng'
import type { Biome, CampaignRouteState, LegacyRecord, RunState } from '../types'
import { appendLegacyRecord } from './campaign'

export const legacyRecordForDeath = (state: RunState, heirName: string, lineage: readonly string[]): LegacyRecord => ({
  id: `legacy:${state.seed}:${state.floor.index}:${state.turn}`,
  heirName,
  cause: 'defeated',
  biome: state.area ?? state.floor.biome,
  floor: state.areaFloor ?? state.floor.index % 4,
  seed: state.seed,
  lineage: [...lineage, heirName].slice(-12),
  location: { x: state.hero.x, y: state.hero.y },
  cache: { gold: state.hero.gold, items: [...state.hero.inventory] },
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
