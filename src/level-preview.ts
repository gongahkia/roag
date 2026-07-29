import { measureGeneration, type GenerationMetrics } from './generation-metrics'
import type { MacroRecipeDebug } from './macro-recipe'
import type { PlacementDebug } from './placement-contract'
import type { RouteContract } from './route-contract'
import type { Biome, Floor } from './types'
import { generateAreaFloor, macroRecipeDebug, placementDebug, routeContractDebug, validateGeneration, type GenerationValidation } from './world'

export const LEVEL_PREVIEW_BIOMES = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'] as const satisfies readonly Biome[]
export interface LevelPreviewConfig { seed: number; biome: Biome; areaFloor: number; routePosition: number; macro: boolean; placements: boolean; entities: boolean }
export interface LevelPreview {
  floor: Floor
  route?: RouteContract
  macro?: MacroRecipeDebug
  placements: readonly PlacementDebug[]
  validation: GenerationValidation
  metrics: GenerationMetrics
}

export const DEFAULT_LEVEL_PREVIEW: LevelPreviewConfig = { seed: 42, biome: 'mine', areaFloor: 0, routePosition: 0, macro: true, placements: true, entities: true }
const boundedInteger = (value: string | null, min: number, max: number, fallback: number): number => {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback
}
const enabled = (value: string | null, fallback: boolean): boolean => value === null ? fallback : value !== '0'

export const levelPreviewConfig = (query: URLSearchParams): LevelPreviewConfig => {
  const biome = query.get('biome')
  return {
    seed: boundedInteger(query.get('seed'), 0, 0xffffffff, DEFAULT_LEVEL_PREVIEW.seed),
    biome: LEVEL_PREVIEW_BIOMES.includes(biome as Biome) ? biome as Biome : DEFAULT_LEVEL_PREVIEW.biome,
    areaFloor: boundedInteger(query.get('floor'), 0, 3, DEFAULT_LEVEL_PREVIEW.areaFloor),
    routePosition: boundedInteger(query.get('position'), 0, 9, DEFAULT_LEVEL_PREVIEW.routePosition),
    macro: enabled(query.get('macro'), DEFAULT_LEVEL_PREVIEW.macro),
    placements: enabled(query.get('placements'), DEFAULT_LEVEL_PREVIEW.placements),
    entities: enabled(query.get('entities'), DEFAULT_LEVEL_PREVIEW.entities)
  }
}

export const levelPreviewQuery = (config: LevelPreviewConfig): string => {
  const query = new URLSearchParams({ debug: 'levels', seed: String(config.seed), biome: config.biome, floor: String(config.areaFloor), position: String(config.routePosition) })
  if (!config.macro) query.set('macro', '0')
  if (!config.placements) query.set('placements', '0')
  if (!config.entities) query.set('entities', '0')
  return '?' + query.toString()
}

export const createLevelPreview = (config: Pick<LevelPreviewConfig, 'seed' | 'biome' | 'areaFloor' | 'routePosition'>): LevelPreview => {
  const floor = generateAreaFloor(config.seed, config.biome, config.areaFloor, config.routePosition)
  const route = routeContractDebug(floor)
  const macro = macroRecipeDebug(floor)
  const placements = placementDebug(floor)
  const validation = validateGeneration(floor)
  return { floor, route, macro, placements, validation, metrics: measureGeneration({ floor, route, macro, placements, validation }) }
}
