export const WORLD_GENERATION_CONFIG_VERSION = 1 as const
export const DEFAULT_WORLD_GENERATION_PRESET = 'watershed' as const
export const MAX_GENERATION_ATTEMPTS = 4 as const

export type WorldGenerationPreset = 'sheltered-reach' | 'watershed' | 'far-coast'
export type RegionSize = 'compact' | 'standard' | 'broad'
export type ClimateProfile = 'cool-wet' | 'temperate' | 'warm-dry'
export type EraPace = 'measured' | 'brisk' | 'pressing'
export type SimulationFidelity = 'focused' | 'balanced' | 'deep'
export type GenerationScale = 1 | 2 | 3 | 4 | 5

/**
 * Every field is resolved before generation starts. The manifest integration
 * follows in Phase 1.2; this contract keeps the configuration independent of
 * the foundation-world schema until then.
 */
export interface WorldGenerationAdvancedSettings {
  regionSize: RegionSize
  historyYears: number
  climate: ClimateProfile
  terrainRuggedness: GenerationScale
  waterwayDensity: GenerationScale
  settlementDensity: GenerationScale
  populationDensity: GenerationScale
  politicalFragmentation: GenerationScale
  resourceScarcity: GenerationScale
  ecologyComplexity: GenerationScale
  dangerPressure: GenerationScale
  eraPace: EraPace
  simulationFidelity: SimulationFidelity
}

export interface WorldGenerationConfig extends WorldGenerationAdvancedSettings {
  version: typeof WORLD_GENERATION_CONFIG_VERSION
  preset: WorldGenerationPreset
}

export interface WorldGenerationConfigRequest {
  preset?: WorldGenerationPreset
  advanced?: Partial<WorldGenerationAdvancedSettings>
}

export interface WorldGenerationPresetDefinition {
  description: string
  defaults: Readonly<WorldGenerationAdvancedSettings>
}

export interface WorldGenerationConfigIssue {
  field: string
  code: 'invalid-value' | 'unknown-setting' | 'incompatible-settings'
  message: string
}

export type WorldGenerationConfigResolution =
  | { status: 'valid'; configuration: WorldGenerationConfig }
  | { status: 'invalid'; issues: readonly WorldGenerationConfigIssue[] }

export interface GenerationAttempt {
  attempt: number
  streamSeed: string
}

export type GenerationAttemptSelection =
  | { status: 'selected'; attempt: GenerationAttempt }
  | { status: 'exhausted'; attempted: number }

const scaleValues = [1, 2, 3, 4, 5] as const
const regionSizes = ['compact', 'standard', 'broad'] as const
const climateProfiles = ['cool-wet', 'temperate', 'warm-dry'] as const
const eraPaces = ['measured', 'brisk', 'pressing'] as const
const simulationFidelities = ['focused', 'balanced', 'deep'] as const
const advancedSettingNames = [
  'regionSize',
  'historyYears',
  'climate',
  'terrainRuggedness',
  'waterwayDensity',
  'settlementDensity',
  'populationDensity',
  'politicalFragmentation',
  'resourceScarcity',
  'ecologyComplexity',
  'dangerPressure',
  'eraPace',
  'simulationFidelity'
] as const satisfies readonly (keyof WorldGenerationAdvancedSettings)[]

const presetDefinitions: Readonly<Record<WorldGenerationPreset, WorldGenerationPresetDefinition>> = {
  'sheltered-reach': {
    description: 'A compact sheltered reach for inspecting the foundation of one river basin.',
    defaults: {
      regionSize: 'compact',
      historyYears: 200,
      climate: 'temperate',
      terrainRuggedness: 2,
      waterwayDensity: 4,
      settlementDensity: 3,
      populationDensity: 2,
      politicalFragmentation: 2,
      resourceScarcity: 2,
      ecologyComplexity: 3,
      dangerPressure: 2,
      eraPace: 'measured',
      simulationFidelity: 'focused'
    }
  },
  watershed: {
    description: 'A balanced river basin with several connected settlements and a credible coastward pull.',
    defaults: {
      regionSize: 'standard',
      historyYears: 300,
      climate: 'temperate',
      terrainRuggedness: 3,
      waterwayDensity: 4,
      settlementDensity: 3,
      populationDensity: 3,
      politicalFragmentation: 3,
      resourceScarcity: 3,
      ecologyComplexity: 3,
      dangerPressure: 3,
      eraPace: 'measured',
      simulationFidelity: 'balanced'
    }
  },
  'far-coast': {
    description: 'A broad, older basin with a more distant coast and more varied pressures.',
    defaults: {
      regionSize: 'broad',
      historyYears: 400,
      climate: 'cool-wet',
      terrainRuggedness: 4,
      waterwayDensity: 3,
      settlementDensity: 3,
      populationDensity: 3,
      politicalFragmentation: 4,
      resourceScarcity: 4,
      ecologyComplexity: 4,
      dangerPressure: 4,
      eraPace: 'brisk',
      simulationFidelity: 'deep'
    }
  }
}

export const GENERATION_CONFIG_PRESETS = presetDefinitions

const isRecord = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const includes = <Value>(values: readonly Value[], value: unknown): value is Value => values.includes(value as Value)
const isScale = (value: unknown): value is GenerationScale => includes(scaleValues, value)
const isHistoryYears = (value: unknown): value is number => typeof value === 'number' && Number.isSafeInteger(value) && value >= 100 && value <= 600 && value % 25 === 0
const validPreset = (value: unknown): value is WorldGenerationPreset => typeof value === 'string' && Object.hasOwn(presetDefinitions, value)

const issue = (field: string, code: WorldGenerationConfigIssue['code'], message: string): WorldGenerationConfigIssue => ({ field, code, message })

const settingValueIsValid = (setting: keyof WorldGenerationAdvancedSettings, value: unknown): boolean => {
  if (setting === 'regionSize') return includes(regionSizes, value)
  if (setting === 'historyYears') return isHistoryYears(value)
  if (setting === 'climate') return includes(climateProfiles, value)
  if (setting === 'terrainRuggedness' || setting === 'waterwayDensity' || setting === 'settlementDensity' || setting === 'populationDensity' || setting === 'politicalFragmentation' || setting === 'resourceScarcity' || setting === 'ecologyComplexity' || setting === 'dangerPressure') return isScale(value)
  if (setting === 'eraPace') return includes(eraPaces, value)
  return includes(simulationFidelities, value)
}

const settingMessage = (setting: keyof WorldGenerationAdvancedSettings): string => {
  if (setting === 'historyYears') return 'historyYears must be an integer multiple of 25 from 100 through 600.'
  if (setting === 'regionSize') return 'regionSize must be compact, standard, or broad.'
  if (setting === 'climate') return 'climate must be cool-wet, temperate, or warm-dry.'
  if (setting === 'eraPace') return 'eraPace must be measured, brisk, or pressing.'
  if (setting === 'simulationFidelity') return 'simulationFidelity must be focused, balanced, or deep.'
  return `${setting} must be an integer level from 1 through 5.`
}

const constraintsFor = (configuration: WorldGenerationConfig): readonly WorldGenerationConfigIssue[] => {
  const issues: WorldGenerationConfigIssue[] = []
  if (configuration.populationDensity > configuration.settlementDensity + 1) {
    issues.push(issue('advanced.populationDensity', 'incompatible-settings', 'populationDensity may be at most one level above settlementDensity.'))
  }
  if (configuration.regionSize === 'broad' && configuration.historyYears < 200) {
    issues.push(issue('advanced.historyYears', 'incompatible-settings', 'broad regionSize requires at least 200 historyYears.'))
  }
  return issues
}

/**
 * Runtime validation makes configuration import and later UI input explicit.
 * A malformed request is rejected rather than repaired, so sharing a manifest
 * never silently changes the intended world.
 */
export const resolveWorldGenerationConfig = (request: WorldGenerationConfigRequest = {}): WorldGenerationConfigResolution => {
  const presetValue = request.preset ?? DEFAULT_WORLD_GENERATION_PRESET
  if (!validPreset(presetValue)) {
    return { status: 'invalid', issues: [issue('preset', 'invalid-value', 'preset must name a known world-generation preset.')] }
  }

  const advancedValue: unknown = request.advanced ?? {}
  if (!isRecord(advancedValue)) {
    return { status: 'invalid', issues: [issue('advanced', 'invalid-value', 'advanced must be an object containing recognized world-generation settings.')] }
  }

  const issues: WorldGenerationConfigIssue[] = []
  const defaults = presetDefinitions[presetValue].defaults
  const resolved: WorldGenerationConfig = { version: WORLD_GENERATION_CONFIG_VERSION, preset: presetValue, ...defaults }
  for (const setting of advancedSettingNames) {
    if (!Object.hasOwn(advancedValue, setting)) continue
    const value = advancedValue[setting]
    if (!settingValueIsValid(setting, value)) {
      issues.push(issue(`advanced.${setting}`, 'invalid-value', settingMessage(setting)))
      continue
    }
    Object.assign(resolved, { [setting]: value })
  }
  for (const setting of Object.keys(advancedValue).filter(name => !advancedSettingNames.includes(name as keyof WorldGenerationAdvancedSettings)).sort()) {
    issues.push(issue(`advanced.${setting}`, 'unknown-setting', `${setting} is not a recognized world-generation setting.`))
  }
  const constraintIssues = constraintsFor(resolved)
  issues.push(...constraintIssues)
  return issues.length ? { status: 'invalid', issues } : { status: 'valid', configuration: resolved }
}

const configSignature = (configuration: WorldGenerationConfig): string => JSON.stringify([
  configuration.version,
  configuration.preset,
  configuration.regionSize,
  configuration.historyYears,
  configuration.climate,
  configuration.terrainRuggedness,
  configuration.waterwayDensity,
  configuration.settlementDensity,
  configuration.populationDensity,
  configuration.politicalFragmentation,
  configuration.resourceScarcity,
  configuration.ecologyComplexity,
  configuration.dangerPressure,
  configuration.eraPace,
  configuration.simulationFidelity
])

/**
 * Future generation validates each candidate in this exact order. A rejected
 * candidate advances only this derived stream; it never mutates configuration
 * or draws entropy from time, the browser, or another world.
 */
export const generationRetryPlan = (seed: string, configuration: WorldGenerationConfig): readonly GenerationAttempt[] => {
  const normalizedSeed = seed.trim()
  if (!normalizedSeed) throw new Error('generation retry requires a non-empty seed')
  const signature = configSignature(configuration)
  return Array.from({ length: MAX_GENERATION_ATTEMPTS }, (_, attempt) => ({
    attempt,
    streamSeed: `jomon-world-generation-v${WORLD_GENERATION_CONFIG_VERSION}|${normalizedSeed}|${signature}|attempt:${attempt}`
  }))
}

export const selectFirstValidGenerationAttempt = (plan: readonly GenerationAttempt[], isValid: (attempt: GenerationAttempt) => boolean): GenerationAttemptSelection => {
  for (const attempt of plan) {
    if (isValid(attempt)) return { status: 'selected', attempt }
  }
  return { status: 'exhausted', attempted: plan.length }
}
