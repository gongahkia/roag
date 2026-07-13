import type { Biome, CampaignRouteState } from '../types'

export const AREA_ORDER: Biome[] = ['mine', 'wilds', 'caverns', 'ruins']
export const nextArea = (biome: Biome): Biome | undefined => AREA_ORDER[AREA_ORDER.indexOf(biome) + 1]
export const unlockNextArea = (unlocked: readonly Biome[], completed: Biome): Biome[] => {
  const next = nextArea(completed)
  return next && !unlocked.includes(next) ? [...unlocked, next] : [...unlocked]
}

export const initialCampaignRoute = (): CampaignRouteState => ({ version: 1, completedAreas: [], unlockedAreas: ['mine'], selectedBiome: 'mine' })
export const completeCampaignArea = (state: CampaignRouteState, completed: Biome): CampaignRouteState => ({ version: 1, completedAreas: state.completedAreas.includes(completed) ? [...state.completedAreas] : [...state.completedAreas, completed], unlockedAreas: unlockNextArea(state.unlockedAreas, completed), selectedBiome: nextArea(completed) ?? completed })
