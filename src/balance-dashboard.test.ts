import { describe, expect, it } from 'vitest'
import { balanceFloorSamples, summarizeBalanceRuns, type BalanceRun } from './balance-dashboard'
import { newRun } from './engine'
import { BIOME_POOL } from './engine/campaign'
import { createRunTelemetry } from './telemetry'

const runs = (): BalanceRun[] => BIOME_POOL.map((biome, routePosition) => {
  const state = newRun(7, biome, 0, undefined, [], [], BIOME_POOL)
  const metrics = createRunTelemetry(state)
  metrics.terrainInteractions[`${biome}:floor`] = 2
  metrics.bossPhases[`${biome}:pressure`] = 1
  return { seed: 7, biome, outcome: 'complete', complete: true, floor: 1, turns: 20, metrics, floors: balanceFloorSamples(7, biome, routePosition) }
})

describe('balance dashboard', () => {
  it('produces a deterministic per-biome quality dashboard', () => {
    const first = runs()
    const dashboard = summarizeBalanceRuns(first, [7])
    expect(dashboard.acceptance).toEqual({ valid: true, errors: [] })
    expect(dashboard.biomes.mine).toMatchObject({ samples: 4, probeCompletion: { completed: 1, total: 1, rate: 1 }, terrain: { interactions: [{ id: 'floor', count: 2 }] }, bossPhases: { observed: [{ id: 'pressure', count: 1 }] } })
    expect(dashboard.biomes.mine.routeChoiceCost).toEqual([{ id: 'costly', count: 8 }, { id: 'optional', count: 8 }, { id: 'safe', count: 4 }])
    expect(dashboard.biomes.frostReliquary.bossPhases.expected.map(phase => phase.id)).toEqual(['frost:reliquary-warden:cataclysm', 'frost:reliquary-warden:opening', 'frost:reliquary-warden:pressure'])
    expect(JSON.stringify(summarizeBalanceRuns(first, [7]))).toBe(JSON.stringify(summarizeBalanceRuns(first, [7])))
  }, 60_000)

  it('surfaces quality threshold failures', () => {
    const input = runs()
    input[0].floors[0].qualityErrors.push('sparse map')
    expect(summarizeBalanceRuns(input, [7]).biomes.mine.quality).toEqual(expect.objectContaining({ valid: false, errors: expect.arrayContaining(['seed 7 floor 1: sparse map']) }))
  }, 60_000)
})
