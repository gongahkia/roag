import { describe, expect, it } from 'vitest'
import { compactCampaignAutoplayRun } from './autoplay-campaign'
import { runAutoplay } from './autoplay-runner'
import { assertAutoplayTraceDocument } from './autoplay-trace'
import { newRun } from './engine'
import { MAP_WIDTH } from './types'

const traced = (mode: 'visible' | 'omniscient' = 'visible') => runAutoplay(newRun(7), { mode, policy: 'clear', turnLimit: 1, captureTrace: true })

describe('autoplay decision trace', () => {
  it('emits the versioned golden record with a stable hash', () => {
    const document = traced().traceDocument
    expect(document).toMatchObject({ version: 1, episode: { seed: 7, policy: 'clear', informationMode: 'visible', policyVersion: 1, objectiveVersion: 1, turnBudget: 1 }, terminal: { outcome: 'turn-limit', reason: 'turn-limit', turns: 1 }, hash: 'fnv1a32:fa46eb2b' })
    expect(document?.records).toMatchObject([{ version: 1, sequence: 0, chosen: { command: 'p', reason: 'survey objective route:recoverSupplies' }, legalCandidates: [{ command: 'p', reason: 'survey objective route:recoverSupplies', score: 126 }, { command: 'p', reason: 'reach frontier', score: 24 }], resourceDelta: { health: 0, focus: 0, gold: 0, bombs: 0, ropes: 0, keys: 0 }, previousHash: null, hash: 'fnv1a32:35767fd3' }])
    assertAutoplayTraceDocument(document!)
  })

  it('rejects tampered schema records and preserves the input simulation', () => {
    const state = newRun(7)
    const before = structuredClone(state)
    const document = runAutoplay(state, { mode: 'visible', policy: 'clear', turnLimit: 1, captureTrace: true }).traceDocument!
    expect(state).toEqual(before)
    document.records[0]!.chosen.command = 'x'
    expect(() => assertAutoplayTraceDocument(document)).toThrow('autoplay trace record hash mismatch at 0')
  })

  it('is byte-equivalent for the same seed and profile', () => {
    expect(JSON.stringify(traced().traceDocument)).toBe(JSON.stringify(traced().traceDocument))
  })

  it('excludes hidden tiles, actors, and items from visible observations', () => {
    const state = newRun(7)
    const document = runAutoplay(state, { mode: 'visible', policy: 'clear', turnLimit: 1, captureTrace: true }).traceDocument!
    const observation = document.records[0]!.observation
    expect(observation.tiles.every(tile => state.floor.tiles[tile.y * MAP_WIDTH + tile.x]?.visible)).toBe(true)
    expect(observation.actors.every(actor => state.floor.tiles[actor.y * MAP_WIDTH + actor.x]?.visible)).toBe(true)
    expect(observation.items.every(item => state.floor.tiles[item.y * MAP_WIDTH + item.x]?.visible)).toBe(true)
  })

  it('preserves full trace documents in campaign failures', () => {
    const report = traced('omniscient')
    const compact = compactCampaignAutoplayRun(7, { id: 'omniscient-clear', mode: 'omniscient', policy: 'clear' }, report)
    expect(compact.failure?.traceDocument?.hash).toBe(report.traceDocument?.hash)
  })
})
