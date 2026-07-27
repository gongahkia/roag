import { describe, expect, it } from 'vitest'
import { findStructurallyPlayableCampaignSeed, validateCampaignTopology } from './campaign-validation'

describe('campaign seed validation', () => {
  it('keeps every campaign-pool biome structurally playable for a deterministic seed', () => {
    expect(validateCampaignTopology(7)).toEqual([])
  }, 90_000)

  it('uses the requested seed when it is structurally valid', () => {
    expect(findStructurallyPlayableCampaignSeed(7)).toMatchObject({ requestedSeed: 7, seed: 7, accepted: true, kind: 'clear' })
  }, 90_000)

  for (const seed of [0, 41, 99]) it(`keeps campaign seed ${seed} structurally playable across every pooled biome and route position`, () => {
    expect(validateCampaignTopology(seed)).toEqual([])
  }, 90_000)
})
