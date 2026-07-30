import { describe, expect, it } from 'vitest'
import { NEW_GAME_PLUS_VALIDATION_VERSION, newGamePlusValidationFailure, validateNewGamePlusCorpus } from './new-game-plus-validation'

const smoke = validateNewGamePlusCorpus('smoke')

describe('New Game+ fixed-seed validation', () => {
  it('validates the declared smoke corpus across Base, NG+, NG++, and the terminal cap', () => {
    const report = smoke
    expect(report).toMatchObject({ version: NEW_GAME_PLUS_VALIDATION_VERSION, mode: 'smoke', seeds: [7], tiers: ['base', 'ngPlus', 'ngPlusPlus'], terminalCap: { blocked: true }, accepted: true, failures: [] })
    expect(report.entries).toHaveLength(3)
    const [base, ngPlus, ngPlusPlus] = report.entries
    expect(base).toMatchObject({ seed: 7, tier: 'base', accepted: true, difficultyPackage: { id: 'base-v1', tier: 'base' }, replay: { difficulty: { campaignTier: 'base' } } })
    expect(ngPlus).toMatchObject({ seed: 7, tier: 'ngPlus', accepted: true, difficultyPackage: { id: 'ng-plus-v1', tier: 'ngPlus' }, replay: { difficulty: { campaignTier: 'ngPlus' } } })
    expect(ngPlus?.replay.companions).toEqual(expect.arrayContaining([expect.objectContaining({ rosterStatus: 'active' })]))
    expect(ngPlusPlus).toMatchObject({ seed: 7, tier: 'ngPlusPlus', accepted: true, difficultyPackage: { id: 'ng-plus-plus-v1', tier: 'ngPlusPlus' }, replay: { difficulty: { campaignTier: 'ngPlusPlus' } } })
  }, 90_000)

  it('emits reproducible failure metadata with route, replay trace, and resolved package', () => {
    const entry = smoke.entries[1]!
    const failure = newGamePlusValidationFailure({ seed: entry.seed, tier: entry.tier, route: { biome: entry.replay.biome, routePosition: 0, areaFloor: entry.replay.areaFloor, floorIndex: entry.replay.floorIndex }, error: 'fixture failure', replay: entry.replay, trace: entry.trace, difficulty: entry.difficulty })
    expect(failure).toMatchObject({ seed: 7, tier: 'ngPlus', route: { biome: entry.replay.biome, areaFloor: entry.replay.areaFloor, floorIndex: entry.replay.floorIndex }, error: 'fixture failure', replay: { difficulty: { campaignTier: 'ngPlus' } }, trace: expect.any(Array), difficultyPackage: { id: 'ng-plus-v1', tier: 'ngPlus' } })
    expect(failure.trace.length).toBeGreaterThan(0)
  }, 90_000)
})
