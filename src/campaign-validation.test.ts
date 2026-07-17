import { describe, expect, it } from 'vitest'
import { findStructurallyPlayableCampaignSeed, validateCampaignTopology } from './campaign-validation'

describe('campaign seed validation', () => {
  it('keeps all sixteen floors structurally playable for a deterministic seed', () => {
    expect(validateCampaignTopology(7)).toEqual([])
  })

  it('uses the requested seed when it is structurally valid', () => {
    expect(findStructurallyPlayableCampaignSeed(7)).toMatchObject({ requestedSeed: 7, seed: 7, accepted: true, kind: 'clear' })
  })

  it('keeps the first hundred campaign seeds structurally playable', () => {
    for (let seed = 0; seed < 100; seed++) expect(validateCampaignTopology(seed)).toEqual([])
  }, 30_000)
})
