import type { Biome } from '../types'

export const AREA_ORDER: Biome[] = ['mine', 'wilds', 'caverns', 'ruins']
export const nextArea = (biome: Biome): Biome | undefined => AREA_ORDER[AREA_ORDER.indexOf(biome) + 1]
export const unlockNextArea = (unlocked: readonly Biome[], completed: Biome): Biome[] => {
  const next = nextArea(completed)
  return next && !unlocked.includes(next) ? [...unlocked, next] : [...unlocked]
}
