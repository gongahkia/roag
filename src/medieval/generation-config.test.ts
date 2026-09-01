import { describe, expect, it } from 'vitest'
import { GENERATION_CONFIG_PRESETS, generationRetryPlan, resolveWorldGenerationConfig, selectFirstValidGenerationAttempt } from './generation-config'

describe('world generation configuration', () => {
  it('resolves the named watershed preset into a complete versioned configuration', () => {
    const resolution = resolveWorldGenerationConfig({ preset: 'watershed' })

    expect(resolution).toEqual({
      status: 'valid',
      configuration: {
        version: 1,
        preset: 'watershed',
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
    })
    expect(GENERATION_CONFIG_PRESETS.watershed.description).toMatch(/river basin/i)
  })

  it('applies valid advanced overrides without mutating the preset or input', () => {
    const advanced = { climate: 'cool-wet', terrainRuggedness: 4, historyYears: 350 } as const
    const resolution = resolveWorldGenerationConfig({ preset: 'far-coast', advanced })

    expect(resolution).toEqual({
      status: 'valid',
      configuration: expect.objectContaining({
        preset: 'far-coast',
        regionSize: 'broad',
        climate: 'cool-wet',
        terrainRuggedness: 4,
        historyYears: 350
      })
    })
    expect(advanced).toEqual({ climate: 'cool-wet', terrainRuggedness: 4, historyYears: 350 })
    expect(GENERATION_CONFIG_PRESETS['far-coast'].defaults.terrainRuggedness).toBe(4)
  })

  it('rejects unknown, malformed, and incompatible settings with field diagnostics', () => {
    const resolution = resolveWorldGenerationConfig({
      preset: 'watershed',
      advanced: {
        historyYears: 310,
        populationDensity: 5,
        settlementDensity: 1,
        unrecordedPressure: 2
      } as never
    })

    expect(resolution).toEqual({
      status: 'invalid',
      issues: [
        { field: 'advanced.historyYears', code: 'invalid-value', message: 'historyYears must be an integer multiple of 25 from 100 through 600.' },
        { field: 'advanced.unrecordedPressure', code: 'unknown-setting', message: 'unrecordedPressure is not a recognized world-generation setting.' },
        { field: 'advanced.populationDensity', code: 'incompatible-settings', message: 'populationDensity may be at most one level above settlementDensity.' }
      ]
    })
  })

  it('derives a bounded, repeatable retry sequence from the seed and resolved configuration', () => {
    const resolved = resolveWorldGenerationConfig({ preset: 'watershed' })
    if (resolved.status !== 'valid') throw new Error('fixture configuration must resolve')

    const first = generationRetryPlan('reed-bank-44', resolved.configuration)
    const second = generationRetryPlan('reed-bank-44', resolved.configuration)
    const changed = generationRetryPlan('reed-bank-45', resolved.configuration)

    expect(first).toEqual(second)
    expect(first).toHaveLength(4)
    expect(first.map(attempt => attempt.attempt)).toEqual([0, 1, 2, 3])
    expect(first.map(attempt => attempt.streamSeed)).not.toEqual(changed.map(attempt => attempt.streamSeed))
  })

  it('selects the first valid deterministic attempt and reports bounded exhaustion', () => {
    const resolved = resolveWorldGenerationConfig({ preset: 'sheltered-reach' })
    if (resolved.status !== 'valid') throw new Error('fixture configuration must resolve')
    const plan = generationRetryPlan('lock-gate', resolved.configuration)

    expect(selectFirstValidGenerationAttempt(plan, attempt => attempt.attempt === 2)).toEqual({ status: 'selected', attempt: plan[2] })
    expect(selectFirstValidGenerationAttempt(plan, () => false)).toEqual({ status: 'exhausted', attempted: 4 })
  })
})
