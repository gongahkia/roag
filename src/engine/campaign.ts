import type { Biome, CampaignRouteState, LegacyRecord, LineageEvent } from '../types'

export const AREA_ORDER: Biome[] = ['mine', 'wilds', 'caverns', 'ruins']
export const nextArea = (biome: Biome): Biome | undefined => AREA_ORDER[AREA_ORDER.indexOf(biome) + 1]
export const unlockNextArea = (unlocked: readonly Biome[], completed: Biome): Biome[] => {
  const next = nextArea(completed)
  return next && !unlocked.includes(next) ? [...unlocked, next] : [...unlocked]
}

export const initialCampaignRoute = (): CampaignRouteState => ({ version: 1, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [], lineageEvents: [], legacyRecords: [] })
export const completeCampaignArea = (state: CampaignRouteState, completed: Biome): CampaignRouteState => ({ version: 1, completedAreas: state.completedAreas.includes(completed) ? [...state.completedAreas] : [...state.completedAreas, completed], unlockedAreas: [...state.unlockedAreas], selectedBiome: completed, rescuedNpcs: [...state.rescuedNpcs], lineageEvents: [...state.lineageEvents], legacyRecords: [...state.legacyRecords] })
export const unlockCampaignArea = (state: CampaignRouteState, biome: Biome): CampaignRouteState => ({ version: 1, completedAreas: [...state.completedAreas], unlockedAreas: state.unlockedAreas.includes(biome) ? [...state.unlockedAreas] : [...state.unlockedAreas, biome], selectedBiome: biome, rescuedNpcs: [...state.rescuedNpcs], lineageEvents: [...state.lineageEvents], legacyRecords: [...state.legacyRecords] })
export const recordCampaignSacrifice = (state: CampaignRouteState, event: LineageEvent): CampaignRouteState => ({ ...state, rescuedNpcs: state.rescuedNpcs.filter(npc => npc.id !== event.npcId), lineageEvents: state.lineageEvents.some(existing => existing.id === event.id) ? [...state.lineageEvents] : [...state.lineageEvents, event].slice(-12) })
export const appendLegacyRecord = (state: CampaignRouteState, record: LegacyRecord): CampaignRouteState => ({ ...state, legacyRecords: [...state.legacyRecords, { ...record, lineage: [...record.lineage], location: { ...record.location }, cache: { gold: record.cache.gold, items: [...record.cache.items] }, encounter: { ...record.encounter } }].slice(-12) })
