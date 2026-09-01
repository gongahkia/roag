import { resolveWorldGenerationConfig, type WorldGenerationConfig, type WorldGenerationConfigIssue, type WorldGenerationConfigRequest, type WorldGenerationConfigSelection } from './generation-config'

export const CREATION_SETTINGS_VERSION = 1 as const
export const CREATION_SETTINGS_RECORD_VERSION = 1 as const
export const CREATION_SETTINGS_PROFILE_LIMIT = 6 as const
export const CREATION_SETTINGS_PROFILE_NAME_LIMIT = 32 as const
export const DEFAULT_CREATION_SEED = 'jomon-foundation' as const

export interface CreationSettings {
  version: typeof CREATION_SETTINGS_VERSION
  seed: string
  configuration: WorldGenerationConfigSelection
  advancedMode: boolean
}

export interface CreationSettingsInput {
  seed?: string
  configuration?: WorldGenerationConfigRequest
  advancedMode?: boolean
}

export interface CreationSettingsIssue {
  field: 'seed' | 'advancedMode'
  code: 'invalid-value'
  message: string
}

export type CreationSettingsResolution =
  | { status: 'valid'; settings: CreationSettings; resolvedConfiguration: WorldGenerationConfig }
  | { status: 'invalid'; issues: readonly (CreationSettingsIssue | WorldGenerationConfigIssue)[] }

export interface CreationSettingsProfile {
  name: string
  settings: CreationSettings
}

/** The only settings record retained in the isolated medieval IndexedDB namespace. */
export interface CreationSettingsRecord {
  version: typeof CREATION_SETTINGS_RECORD_VERSION
  lastUsed: CreationSettings
  profiles: readonly CreationSettingsProfile[]
}

const record = (value: unknown): value is Record<string, unknown> => Boolean(value) && typeof value === 'object' && !Array.isArray(value)
const hasOnlyKeys = (value: Record<string, unknown>, keys: readonly string[]): boolean => Object.keys(value).every(key => keys.includes(key)) && keys.every(key => Object.hasOwn(value, key))
const equal = (left: unknown, right: unknown): boolean => JSON.stringify(left) === JSON.stringify(right)

/** This is the one normalizer used by settings persistence and world creation. */
export const normalizeCreationSeed = (seed: string | undefined): string => {
  const normalized = seed?.trim().replace(/\s+/gu, ' ') ?? ''
  return normalized || DEFAULT_CREATION_SEED
}

export const resolveCreationSettings = (input: CreationSettingsInput = {}): CreationSettingsResolution => {
  const issues: (CreationSettingsIssue | WorldGenerationConfigIssue)[] = []
  if (input.seed !== undefined && typeof input.seed !== 'string') issues.push({ field: 'seed', code: 'invalid-value', message: 'seed must be text.' })
  if (input.advancedMode !== undefined && typeof input.advancedMode !== 'boolean') issues.push({ field: 'advancedMode', code: 'invalid-value', message: 'advancedMode must be true or false.' })
  const configuration = resolveWorldGenerationConfig(input.configuration)
  if (configuration.status === 'invalid') issues.push(...configuration.issues)
  if (issues.length || configuration.status === 'invalid') return { status: 'invalid', issues }
  return {
    status: 'valid',
    settings: {
      version: CREATION_SETTINGS_VERSION,
      seed: normalizeCreationSeed(input.seed),
      configuration: configuration.selectedConfiguration,
      advancedMode: input.advancedMode ?? false
    },
    resolvedConfiguration: configuration.configuration
  }
}

export const defaultCreationSettings = (): CreationSettings => {
  const resolution = resolveCreationSettings()
  if (resolution.status !== 'valid') throw new Error('default creation settings must be valid')
  return resolution.settings
}

export const isCreationSettings = (value: unknown): value is CreationSettings => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'seed', 'configuration', 'advancedMode']) || value.version !== CREATION_SETTINGS_VERSION || typeof value.seed !== 'string' || value.seed !== normalizeCreationSeed(value.seed) || typeof value.advancedMode !== 'boolean' || !record(value.configuration)) return false
  const resolution = resolveCreationSettings({ seed: value.seed, configuration: value.configuration as WorldGenerationConfigRequest, advancedMode: value.advancedMode })
  return resolution.status === 'valid' && equal(value, resolution.settings)
}

export const normalizeCreationSettingsProfileName = (name: string): string => name.trim().replace(/\s+/gu, ' ')

export const isCreationSettingsProfileName = (name: unknown): name is string => typeof name === 'string' && name === normalizeCreationSettingsProfileName(name) && name.length > 0 && name.length <= CREATION_SETTINGS_PROFILE_NAME_LIMIT

export const emptyCreationSettingsRecord = (): CreationSettingsRecord => ({
  version: CREATION_SETTINGS_RECORD_VERSION,
  lastUsed: defaultCreationSettings(),
  profiles: []
})

export const isCreationSettingsRecord = (value: unknown): value is CreationSettingsRecord => {
  if (!record(value) || !hasOnlyKeys(value, ['version', 'lastUsed', 'profiles']) || value.version !== CREATION_SETTINGS_RECORD_VERSION || !isCreationSettings(value.lastUsed) || !Array.isArray(value.profiles) || value.profiles.length > CREATION_SETTINGS_PROFILE_LIMIT) return false
  const names = new Set<string>()
  return value.profiles.every(profile => {
    if (!record(profile) || !hasOnlyKeys(profile, ['name', 'settings']) || !isCreationSettingsProfileName(profile.name) || !isCreationSettings(profile.settings) || names.has(profile.name)) return false
    names.add(profile.name)
    return true
  })
}

export const withLastUsedCreationSettings = (recordValue: CreationSettingsRecord, settings: CreationSettings): CreationSettingsRecord => {
  if (!isCreationSettingsRecord(recordValue) || !isCreationSettings(settings)) throw new Error('creation settings must satisfy the medieval settings contract')
  return { version: CREATION_SETTINGS_RECORD_VERSION, lastUsed: structuredClone(settings), profiles: structuredClone(recordValue.profiles) }
}

/** Replaces the matching name in place; a seventh distinct profile is rejected. */
export const saveCreationSettingsProfile = (recordValue: CreationSettingsRecord, name: string, settings: CreationSettings): CreationSettingsRecord => {
  if (!isCreationSettingsRecord(recordValue) || !isCreationSettings(settings)) throw new Error('creation settings must satisfy the medieval settings contract')
  if (!isCreationSettingsProfileName(name)) throw new Error(`profile names must be 1-${CREATION_SETTINGS_PROFILE_NAME_LIMIT} normalized characters`)
  const existingIndex = recordValue.profiles.findIndex(profile => profile.name === name)
  if (existingIndex < 0 && recordValue.profiles.length >= CREATION_SETTINGS_PROFILE_LIMIT) throw new Error(`at most ${CREATION_SETTINGS_PROFILE_LIMIT} named creation-setting profiles may be stored`)
  const profile: CreationSettingsProfile = { name, settings: structuredClone(settings) }
  const profiles = existingIndex < 0
    ? [...recordValue.profiles, profile]
    : recordValue.profiles.map((existing, index) => index === existingIndex ? profile : existing)
  return { version: CREATION_SETTINGS_RECORD_VERSION, lastUsed: structuredClone(settings), profiles }
}
