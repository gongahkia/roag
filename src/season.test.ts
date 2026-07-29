import { describe, expect, it } from 'vitest'
import { mineSeason } from './season'

describe('Village trail seasons', () => {
  it('selects a deterministic visual-only season for each heir seed', () => {
    expect(mineSeason(77)).toEqual(mineSeason(77))
    expect(['frost', 'rain', 'bloom', 'emberfall']).toContain(mineSeason(77).season)
  })

  it('keeps the trail copy and palette in the visual layer', () => {
    const season = mineSeason(901)
    expect(season.scene).toContain('village trail')
    expect(season.color).toMatch(/^#/)
  })
})
