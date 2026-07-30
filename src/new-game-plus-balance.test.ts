import { describe, expect, it } from 'vitest'
import { newGamePlusBalanceReport, NEW_GAME_PLUS_BALANCE_VERSION } from './new-game-plus-balance'

describe('New Game+ balance report', () => {
  it('compares deterministic tier samples without a tuning gate', () => {
    const report = newGamePlusBalanceReport('smoke', 1)
    expect(report).toMatchObject({ version: NEW_GAME_PLUS_BALANCE_VERSION, mode: 'smoke', seeds: [7], turnLimit: 1, policy: 'omniscient-clear', reviewOnly: true, samples: [
      { seed: 7, tier: 'base', outcome: 'turn-limit', stall: true },
      { seed: 7, tier: 'ngPlus', outcome: 'turn-limit', stall: true },
      { seed: 7, tier: 'ngPlusPlus', outcome: 'turn-limit', stall: true }
    ] })
    expect(report.byTier).toMatchObject({ base: { samples: 1 }, ngPlus: { samples: 1 }, ngPlusPlus: { samples: 1 } })
  })
})
