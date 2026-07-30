import { describe, expect, it } from 'vitest'
import { runAutoplay } from './autoplay-runner'
import { createAutoplayTraceDocument, createAutoplayTraceRecord, type AutoplayTraceDocument } from './autoplay-trace'
import { replayAutoplayTrace } from './autoplay-trace-replay'
import { newRun } from './engine'

const trace = (): AutoplayTraceDocument => runAutoplay(newRun(7), { mode: 'visible', policy: 'clear', turnLimit: 2, captureTrace: true }).traceDocument!
const rehashed = (document: AutoplayTraceDocument, patch: (record: AutoplayTraceDocument['records'][number]) => AutoplayTraceDocument['records'][number], episode = document.episode): AutoplayTraceDocument => {
  let previousHash: string | null = null
  const records = document.records.map(record => {
    const { hash: _hash, version: _version, ...value } = patch(record)
    const next = createAutoplayTraceRecord({ ...value, previousHash })
    previousHash = next.hash
    return next
  })
  return createAutoplayTraceDocument(episode, records, document.terminal)
}

describe('autoplay trace replay', () => {
  it('replays a valid trace to its terminal state', () => {
    expect(replayAutoplayTrace(trace())).toMatchObject({ valid: true, terminal: { outcome: 'turn-limit', turns: 2 } })
  })

  it('reports tampered action, seed, mode, and resource delta precisely', () => {
    const document = trace()
    const action = rehashed(document, record => ({ ...record, chosen: { ...record.chosen, command: 'l' } }))
    expect(replayAutoplayTrace(action).divergence?.field).toBe('chosen-command')
    const episode = { ...document.episode, seed: 8 }
    const seed = rehashed(document, record => ({ ...record, episode }), episode)
    expect(replayAutoplayTrace(seed).divergence?.field).toBe('seed')
    const modeEpisode = { ...document.episode, informationMode: 'omniscient' as const }
    const mode = rehashed(document, record => ({ ...record, episode: modeEpisode }), modeEpisode)
    expect(replayAutoplayTrace(mode).divergence?.field).toBe('information-mode')
    const delta = rehashed(document, record => ({ ...record, resourceDelta: { ...record.resourceDelta, gold: 1 } }))
    expect(replayAutoplayTrace(delta).divergence?.field).toBe('resource-delta')
  })
})
