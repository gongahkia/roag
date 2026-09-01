import { describe, expect, it } from 'vitest'
import { CREATION_SETTINGS_PROFILE_LIMIT, defaultCreationSettings, emptyCreationSettingsRecord, isCreationSettings, isCreationSettingsRecord, resolveCreationSettings, saveCreationSettingsProfile } from './settings'
import { createFoundationWorld } from './world'

describe('medieval creation settings contract', () => {
  it('round-trips normalized seed, selected settings, and the sole resolved configuration authority', () => {
    const resolution = resolveCreationSettings({
      seed: '  lower   quay  ',
      advancedMode: true,
      configuration: { preset: 'far-coast', advanced: { historyYears: 350, climate: 'temperate' } }
    })

    expect(resolution.status).toBe('valid')
    if (resolution.status !== 'valid') throw new Error('settings should resolve')
    expect(resolution.settings).toEqual({
      version: 1,
      seed: 'lower quay',
      configuration: { preset: 'far-coast', advanced: { historyYears: 350, climate: 'temperate' } },
      advancedMode: true
    })
    expect(isCreationSettings(resolution.settings)).toBe(true)
    expect(resolveCreationSettings(resolution.settings)).toEqual(resolution)
    expect(resolution.resolvedConfiguration).toMatchObject({ preset: 'far-coast', regionSize: 'broad', historyYears: 350, climate: 'temperate' })
  })

  it('rejects invalid input instead of coercing configuration or settings records', () => {
    expect(resolveCreationSettings(null)).toMatchObject({ status: 'invalid', issues: [{ field: 'seed', code: 'invalid-value' }] })
    expect(resolveCreationSettings({ seed: 17 as never })).toMatchObject({ status: 'invalid', issues: [{ field: 'seed', code: 'invalid-value' }] })
    expect(resolveCreationSettings({ configuration: { preset: 'watershed', advanced: { historyYears: 113 } as never } })).toMatchObject({ status: 'invalid', issues: [{ field: 'advanced.historyYears', code: 'invalid-value' }] })
    expect(isCreationSettings({ ...defaultCreationSettings(), seed: '  noncanonical ' })).toBe(false)
    expect(isCreationSettingsRecord({ version: 1, lastUsed: defaultCreationSettings(), profiles: [{ name: 'bad', settings: { ...defaultCreationSettings(), configuration: { preset: 'unknown' } } }] })).toBe(false)
  })

  it('enforces a bounded named-profile collection and replaces a matching name in place', () => {
    const base = defaultCreationSettings()
    let record = emptyCreationSettingsRecord()
    for (let index = 0; index < CREATION_SETTINGS_PROFILE_LIMIT; index++) {
      record = saveCreationSettingsProfile(record, `basin ${index}`, { ...base, seed: `seed ${index}` })
    }

    expect(record.profiles.map(profile => profile.name)).toEqual(['basin 0', 'basin 1', 'basin 2', 'basin 3', 'basin 4', 'basin 5'])
    expect(() => saveCreationSettingsProfile(record, 'seventh basin', base)).toThrow('at most 6')
    const replaced = saveCreationSettingsProfile(record, 'basin 2', { ...base, seed: 'replaced seed' })
    expect(replaced.profiles).toHaveLength(CREATION_SETTINGS_PROFILE_LIMIT)
    expect(replaced.profiles[2]).toMatchObject({ name: 'basin 2', settings: { seed: 'replaced seed' } })
  })

  it('keeps settings changes separate from an already-created immutable manifest', () => {
    const world = createFoundationWorld({ seed: 'immutable-creation', configuration: { preset: 'watershed' } })
    const before = structuredClone(world.manifest)
    const changed = resolveCreationSettings({ seed: 'other configuration', configuration: { preset: 'far-coast', advanced: { dangerPressure: 5 } } })

    expect(changed.status).toBe('valid')
    expect(world.manifest).toEqual(before)
    expect(world.manifest.creation.seed).toBe('immutable-creation')
    expect(world.manifest.creation.resolvedConfiguration.preset).toBe('watershed')
  })
})
