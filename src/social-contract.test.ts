import { describe, expect, it } from 'vitest'
import { adjustSocialReputation, emptySocialReputation, socialContractFor, socialDispositionFor } from './social-contract'

const input = { seed: 7, floorIndex: 0, biome: 'mine' as const, recipeId: 'rail-spine', arcId: 'braced-shaft' }

describe('social contracts', () => {
  it('is deterministic and keeps no-social recipes valid', () => {
    expect(socialContractFor(input)).toEqual(socialContractFor(input))
    expect(socialContractFor({ ...input, seed: 4 })).toBeUndefined()
  })

  it('tracks explicit allied and hostile reputation states', () => {
    const allied = adjustSocialReputation(emptySocialReputation(), 'trailfolk', 2)
    const hostile = adjustSocialReputation(emptySocialReputation(), 'kami', -2)
    expect(socialDispositionFor(allied, 'trailfolk')).toBe('allied')
    expect(socialDispositionFor(hostile, 'kami')).toBe('hostile')
  })
})
