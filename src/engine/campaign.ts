import type { Biome, CampaignRouteState } from '../types'

export const AREA_ORDER: Biome[] = ['mine', 'wilds', 'caverns', 'ruins']
export const nextArea = (biome: Biome): Biome | undefined => AREA_ORDER[AREA_ORDER.indexOf(biome) + 1]
export const unlockNextArea = (unlocked: readonly Biome[], completed: Biome): Biome[] => {
  const next = nextArea(completed)
  return next && !unlocked.includes(next) ? [...unlocked, next] : [...unlocked]
}

export const initialCampaignRoute = (): CampaignRouteState => ({ version: 1, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine', rescuedNpcs: [] })
export const completeCampaignArea = (state: CampaignRouteState, completed: Biome): CampaignRouteState => ({ version: 1, completedAreas: state.completedAreas.includes(completed) ? [...state.completedAreas] : [...state.completedAreas, completed], unlockedAreas: [...state.unlockedAreas], selectedBiome: completed, rescuedNpcs: [...state.rescuedNpcs] })
export const unlockCampaignArea = (state: CampaignRouteState, biome: Biome): CampaignRouteState => ({ version: 1, completedAreas: [...state.completedAreas], unlockedAreas: state.unlockedAreas.includes(biome) ? [...state.unlockedAreas] : [...state.unlockedAreas, biome], selectedBiome: biome, rescuedNpcs: [...state.rescuedNpcs] })
