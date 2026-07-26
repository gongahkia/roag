import type { Biome, CampaignRouteState, LegacyRecord, LineageEvent } from '../types'
import { rngFor } from '../rng'

export const AREA_ORDER = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'] as const satisfies readonly Biome[]
export const isCampaignAreaOrder = (value: readonly Biome[]): boolean => value.length === AREA_ORDER.length && value.every(area => AREA_ORDER.includes(area)) && new Set(value).size === AREA_ORDER.length
export const campaignOrderForSeed = (seed: number): Biome[] => rngFor(seed, 'progression', 'campaign-area-order').shuffle([...AREA_ORDER])
export const nextArea = (biome: Biome, areaOrder: readonly Biome[] = AREA_ORDER): Biome | undefined => areaOrder[areaOrder.indexOf(biome) + 1]
export const unlockNextArea = (unlocked: readonly Biome[], completed: Biome, areaOrder: readonly Biome[] = AREA_ORDER): Biome[] => {
  const next = nextArea(completed, areaOrder)
  return next && !unlocked.includes(next) ? [...unlocked, next] : [...unlocked]
}

export const initialCampaignRoute = (seed?: number): CampaignRouteState => {
  const areaOrder = seed === undefined ? [...AREA_ORDER] : campaignOrderForSeed(seed)
  return { version: 3, areaOrder, completedAreas: [], unlockedAreas: [areaOrder[0]], selectedBiome: areaOrder[0], rescuedNpcs: [], lineageEvents: [], legacyRecords: [] }
}
export const completeCampaignArea = (state: CampaignRouteState, completed: Biome): CampaignRouteState => ({ version: 3, areaOrder: [...state.areaOrder], completedAreas: state.completedAreas.includes(completed) ? [...state.completedAreas] : [...state.completedAreas, completed], unlockedAreas: [...state.unlockedAreas], selectedBiome: completed, rescuedNpcs: [...state.rescuedNpcs], lineageEvents: [...state.lineageEvents], legacyRecords: [...state.legacyRecords] })
export const unlockCampaignArea = (state: CampaignRouteState, biome: Biome): CampaignRouteState => ({ version: 3, areaOrder: [...state.areaOrder], completedAreas: [...state.completedAreas], unlockedAreas: state.unlockedAreas.includes(biome) ? [...state.unlockedAreas] : [...state.unlockedAreas, biome], selectedBiome: biome, rescuedNpcs: [...state.rescuedNpcs], lineageEvents: [...state.lineageEvents], legacyRecords: [...state.legacyRecords] })
export const recordCampaignSacrifice = (state: CampaignRouteState, event: LineageEvent): CampaignRouteState => ({ ...state, rescuedNpcs: state.rescuedNpcs.filter(npc => npc.id !== event.npcId), lineageEvents: state.lineageEvents.some(existing => existing.id === event.id) ? [...state.lineageEvents] : [...state.lineageEvents, event].slice(-12) })
export const appendLegacyRecord = (state: CampaignRouteState, record: LegacyRecord): CampaignRouteState => ({ ...state, legacyRecords: [...state.legacyRecords, { ...record }].slice(-12) })
