import { rngFor } from './rng'
import type { Biome, TraversalToolId } from './types'

export const OPTIONAL_TERRAIN_TOOLS = ['antlerPrybar', 'stoneAdze', 'resinFireBasket', 'woodenLeverRoller'] as const satisfies readonly TraversalToolId[]
export type OptionalToolSource = 'waycache' | 'loot' | 'social' | 'encounter'
export interface OptionalToolDistributionInput { seed: number; floorIndex: number; biome: Biome; recipeId: string; source: OptionalToolSource }

const localTools: Record<Biome, readonly typeof OPTIONAL_TERRAIN_TOOLS[number][]> = {
  mine: ['antlerPrybar', 'stoneAdze', 'woodenLeverRoller'],
  wilds: ['resinFireBasket'],
  caverns: ['woodenLeverRoller'],
  ruins: ['antlerPrybar', 'stoneAdze', 'woodenLeverRoller'],
  furnace: ['stoneAdze', 'resinFireBasket'],
  floodedRuins: ['woodenLeverRoller'],
  cliffs: ['antlerPrybar'],
  burial: ['stoneAdze'],
  saltFlats: ['resinFireBasket'],
  frostReliquary: ['antlerPrybar']
}

const chance: Record<OptionalToolSource, number> = { waycache: 50, loot: 28, social: 55, encounter: 34 }

export const optionalTerrainToolFor = (input: OptionalToolDistributionInput): typeof OPTIONAL_TERRAIN_TOOLS[number] | undefined => {
  const choices = localTools[input.biome]
  const rng = rngFor(input.seed, 'progression', 'optional-terrain-tool', input.source, input.floorIndex, input.biome, input.recipeId)
  return rng.chance(chance[input.source]) ? rng.pick(choices) : undefined
}
