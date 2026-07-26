import type { Biome, CampaignRouteState, LegacyRecord, LineageEvent } from '../types'
import { rngFor } from '../rng'

export const BIOME_POOL = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial'] as const satisfies readonly Biome[]
export const AREA_ORDER = BIOME_POOL
export const LEGACY_AREA_ORDER = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'] as const satisfies readonly Biome[]
export const DEFAULT_AREA_ORDER = ['mine', 'wilds', 'caverns', 'ruins'] as const satisfies readonly Biome[]
export const isCampaignAreaOrder = (value: readonly Biome[]): boolean => value.length === 4 && value.every(area => BIOME_POOL.includes(area)) && new Set(value).size === 4
export const isLegacyCampaignAreaOrder = (value: readonly Biome[]): boolean => value.length === LEGACY_AREA_ORDER.length && value.every(area => (LEGACY_AREA_ORDER as readonly Biome[]).includes(area)) && new Set(value).size === LEGACY_AREA_ORDER.length
export const campaignOrderForSeed = (seed: number): Biome[] => {
  const rng = rngFor(seed, 'progression', 'campaign-area-order')
  return rng.shuffle([...rng.shuffle([...BIOME_POOL]).slice(0, 4)])
}
export const nextArea = (biome: Biome, areaOrder: readonly Biome[] = AREA_ORDER): Biome | undefined => areaOrder[areaOrder.indexOf(biome) + 1]
export const unlockNextArea = (unlocked: readonly Biome[], completed: Biome, areaOrder: readonly Biome[] = AREA_ORDER): Biome[] => {
  const next = nextArea(completed, areaOrder)
  return next && !unlocked.includes(next) ? [...unlocked, next] : [...unlocked]
}

export const initialCampaignRoute = (seed?: number): CampaignRouteState => {
  const areaOrder = seed === undefined ? [...DEFAULT_AREA_ORDER] : campaignOrderForSeed(seed)
  return { version: 4, areaOrder, completedAreas: [], unlockedAreas: [areaOrder[0]], selectedBiome: areaOrder[0], rescuedNpcs: [], lineageEvents: [], legacyRecords: [] }
}
export const completeCampaignArea = (state: CampaignRouteState, completed: Biome): CampaignRouteState => ({ version: state.version, areaOrder: [...state.areaOrder], completedAreas: state.completedAreas.includes(completed) ? [...state.completedAreas] : [...state.completedAreas, completed], unlockedAreas: [...state.unlockedAreas], selectedBiome: completed, rescuedNpcs: [...state.rescuedNpcs], lineageEvents: [...state.lineageEvents], legacyRecords: [...state.legacyRecords] })
export const unlockCampaignArea = (state: CampaignRouteState, biome: Biome): CampaignRouteState => ({ version: state.version, areaOrder: [...state.areaOrder], completedAreas: [...state.completedAreas], unlockedAreas: state.unlockedAreas.includes(biome) ? [...state.unlockedAreas] : [...state.unlockedAreas, biome], selectedBiome: biome, rescuedNpcs: [...state.rescuedNpcs], lineageEvents: [...state.lineageEvents], legacyRecords: [...state.legacyRecords] })
export const recordCampaignSacrifice = (state: CampaignRouteState, event: LineageEvent): CampaignRouteState => ({ ...state, rescuedNpcs: state.rescuedNpcs.filter(npc => npc.id !== event.npcId), lineageEvents: state.lineageEvents.some(existing => existing.id === event.id) ? [...state.lineageEvents] : [...state.lineageEvents, event].slice(-12) })
export const appendLegacyRecord = (state: CampaignRouteState, record: LegacyRecord): CampaignRouteState => ({ ...state, legacyRecords: [...state.legacyRecords, { ...record }].slice(-12) })
