import { describe, expect, it } from 'vitest'
import { autoplayHeuristicProfile, autoplayHeuristicProfiles, parseAutoplayHeuristicProfile } from './autoplay-heuristics'
import { runAutoplay } from './autoplay-runner'
import { newRun } from './engine'

describe('autoplay heuristic profiles', () => {
  it('uses the compatibility profile by default without changing a golden run', () => {
    const implicit = runAutoplay(newRun(7), { mode: 'visible', policy: 'clear', turnLimit: 3, captureTrace: true })
    const explicit = runAutoplay(newRun(7), { mode: 'visible', policy: 'clear', heuristicProfile: autoplayHeuristicProfile('compatibility'), turnLimit: 3, captureTrace: true })
    expect(explicit).toEqual(implicit)
    expect(implicit.heuristicProfile).toEqual({ id: 'compatibility', version: 1 })
    expect(implicit.traceDocument?.episode.heuristicProfile).toEqual(implicit.heuristicProfile)
  })

  it('parses named profiles deterministically and exposes documented term bounds', () => {
    expect(autoplayHeuristicProfile('conservative')).toMatchObject({ id: 'conservative', resourceReserve: { clear: 2, explore: 3 }, failedCommandPenalty: 75 })
    expect(parseAutoplayHeuristicProfile(autoplayHeuristicProfiles.compatibility)).toEqual(autoplayHeuristicProfile('compatibility'))
  })

  it('fails fast for invalid profile data', () => {
    const invalid = autoplayHeuristicProfile('compatibility') as unknown as Record<string, unknown>
    invalid.resourceReserve = { survival: 1, clear: 1, explore: 2, legacy: 99 }
    expect(() => parseAutoplayHeuristicProfile(invalid)).toThrow('invalid autoplay heuristic resourceReserve')
    expect(() => runAutoplay(newRun(7), { heuristicProfile: invalid as unknown as ReturnType<typeof autoplayHeuristicProfile> })).toThrow('invalid autoplay heuristic resourceReserve')
    expect(() => autoplayHeuristicProfile('unknown')).toThrow('unknown autoplay heuristic profile')
  })
})
