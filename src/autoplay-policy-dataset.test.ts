import { describe, expect, it } from 'vitest'
import { newRun } from './engine'
import { exportAutoplayPolicyDatasetJsonl, autoplayPolicyDatasetRecords, assertAutoplayPolicyDatasetRecord } from './autoplay-policy-dataset'
import { runAutoplay } from './autoplay-runner'

const trace = (mode: 'visible' | 'omniscient' = 'visible') => runAutoplay(newRun(7), { mode, policy: 'clear', turnLimit: 2, captureTrace: true }).traceDocument!

describe('autoplay policy dataset', () => {
  it('exports checked development records with AP-03 field provenance', () => {
    const records = autoplayPolicyDatasetRecords([trace()], 'development')
    expect(records).toHaveLength(2)
    expect(records[0]).toMatchObject({ version: 1, metadata: { partition: 'development', sequence: 0, informationMode: 'visible', fieldProvenance: { observation: 'AP-03 decision-time observation' } }, chosenAction: { command: expect.any(String) }, reward: { events: expect.any(Array) }, terminal: { outcome: 'turn-limit' } })
    records.forEach(assertAutoplayPolicyDatasetRecord)
  })

  it('is byte-stable across document input order and retains partition identity', () => {
    const visible = trace('visible')
    const omniscient = trace('omniscient')
    expect(exportAutoplayPolicyDatasetJsonl([visible, omniscient], 'held-out')).toBe(exportAutoplayPolicyDatasetJsonl([omniscient, visible], 'held-out'))
    expect(exportAutoplayPolicyDatasetJsonl([visible], 'held-out')).toContain('"partition":"held-out"')
  })

  it('proves visible records omit replay and hidden-map fields', () => {
    const record = autoplayPolicyDatasetRecords([trace('visible')], 'development')[0]!
    const json = JSON.stringify(record)
    expect(json).not.toContain('replay')
    expect(json).not.toContain('layoutId')
    expect(json).not.toContain('hiddenMap')
    expect(() => assertAutoplayPolicyDatasetRecord({ ...record, replay: {} } as typeof record)).toThrow('forbidden field: replay')
  })
})
