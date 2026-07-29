import { describe, expect, it } from 'vitest'
import { createLevelPreview, levelPreviewConfig, levelPreviewQuery } from './level-preview'

describe('level preview', () => {
  it('normalizes URL state and emits a stable shareable query', () => {
    const config = levelPreviewConfig(new URLSearchParams('seed=999&biome=frostReliquary&floor=3&position=7&macro=0'))
    expect(config).toEqual({ seed: 999, biome: 'frostReliquary', areaFloor: 3, routePosition: 7, macro: false, placements: true, entities: true })
    expect(levelPreviewQuery(config)).toBe('?debug=levels&seed=999&biome=frostReliquary&floor=3&position=7&macro=0')
  })

  it('falls back for invalid URL state', () => {
    expect(levelPreviewConfig(new URLSearchParams('seed=-1&biome=nope&floor=9&position=10'))).toMatchObject({ seed: 42, biome: 'mine', areaFloor: 0, routePosition: 0 })
  })

  it('exposes deterministic generator audit data', () => {
    const preview = createLevelPreview({ seed: 42, biome: 'mine', areaFloor: 0, routePosition: 0 })
    expect(preview.validation).toEqual({ valid: true, errors: [] })
    expect(preview.metrics.acceptance.valid).toBe(true)
    expect(preview.metrics.trace).toMatchObject({ seed: 42, biome: 'mine', floor: 0, recipe: preview.floor.layoutId })
    expect(preview.macro?.nodes.length).toBeGreaterThan(0)
    expect(preview.placements.length).toBeGreaterThan(0)
  })
})
