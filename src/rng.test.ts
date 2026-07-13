import { describe, expect, it } from 'vitest'
import { rngFor, streamSeed } from './rng'

describe('named RNG streams', () => {
  it('reproduces each named stream for the same seed and scope', () => {
    const rolls = () => {
      const rng = rngFor(91, 'generation', 2, 'layout')
      return Array.from({ length: 4 }, () => rng.next())
    }
    expect(rolls()).toEqual(rolls())
    expect(streamSeed(91, 'combat', 2, 4)).toBe(streamSeed(91, 'combat', 2, 4))
  })

  it('keeps generation, combat, loot, gates, and legacy rolls isolated', () => {
    const streams = ['generation', 'combat', 'loot', 'gates', 'legacy'] as const
    const first = streams.map(stream => rngFor(91, stream, 2).next())
    for (let roll = 0; roll < 20; roll++) rngFor(91, 'combat', roll).next()
    expect(streams.map(stream => rngFor(91, stream, 2).next())).toEqual(first)
    expect(new Set(first).size).toBe(streams.length)
  })
})
