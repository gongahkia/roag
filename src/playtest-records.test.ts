import { describe, expect, it } from 'vitest'
import { summarizePlaytestRecords, type PlaytestFailure, type PlaytestRecords } from './playtest-records'
import { BIOME_POOL } from './engine/campaign'

const records = (): PlaytestRecords => ({
  version: 1,
  records: BIOME_POOL.map(biome => ({ biome, targetThesis: 'readable space', sourceGrounded: { note: 'grounded note', url: 'https://example.test/source' }, fantasyInvention: 'invented mechanic', sessions: [] }))
})

const session = (participantId: string, failureModes: PlaytestFailure[] = []) => ({
  id: `session-${participantId}`,
  date: '2026-07-28',
  participantId,
  sessionKind: 'human' as const,
  seed: 7,
  routeChosen: 'safe rail',
  landmarkRecall: 'lit cart',
  threatComprehension: 'marked collapse',
  terrainUse: 'pushed cart for cover',
  boonRelevance: 'rope opened the optional route',
  encounterRead: 'guard protected the lane',
  confusion: 'none',
  memorableMoments: 'crossing the collapse',
  funRating: 4,
  funReason: 'a tactical route choice',
  thesisWithoutName: 'a dangerous quarry with readable exits',
  thesisAssessment: 'clear' as const,
  failureModes
})

describe('playtest records', () => {
  it('keeps pending templates structurally valid but incomplete', () => {
    const summary = summarizePlaytestRecords(records())
    expect(summary).toMatchObject({ valid: true, complete: false, biomes: { mine: { sessions: 0, complete: false } } })
    expect(summarizePlaytestRecords(records(), true).errors).toContain('mine: no human session')
  })

  it('requires a linked issue for a recurrent opacity or unfairness finding', () => {
    const input = records()
    input.records[0].sessions = [session('p1', [{ kind: 'opacity', code: 'collapse-marker', detail: 'collapse marker is unclear' }]), session('p2', [{ kind: 'opacity', code: 'collapse-marker', detail: 'collapse marker is unclear' }])]
    expect(summarizePlaytestRecords(input).errors).toContain('mine: recurrent opacity:collapse-marker needs a linked GitHub issue')
    input.records[0].sessions[1].failureModes[0].issue = 42
    expect(summarizePlaytestRecords(input).errors).not.toContain('mine: recurrent opacity:collapse-marker needs a linked GitHub issue')
  })
})
