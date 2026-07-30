import { describe, expect, it } from 'vitest'
import { runAutoplay } from './autoplay-runner'
import { createAutoplayTraceDocument, createAutoplayTraceRecord, type AutoplayTraceDocument } from './autoplay-trace'
import { replayAutoplayTrace } from './autoplay-trace-replay'
import { advanceCampaignTier, completeCampaignTier, initialCampaignCycle, newHero, newRun } from './engine'
import { companionLeadForRescue } from './engine/companions'

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

  it('replays a trace with deterministic active-party placement', () => {
    const companion = companionLeadForRescue({ id: 'rescue:mine:1:mika', name: 'Mika', biome: 'mine', floor: 1 })
    companion.rosterStatus = 'active'
    const document = runAutoplay(newRun(71, 'mine', 0, undefined, [], [], undefined, undefined, [companion]), { mode: 'visible', policy: 'clear', turnLimit: 1, captureTrace: true }).traceDocument!
    expect(replayAutoplayTrace(document)).toMatchObject({ valid: true })
  })

  it('replays a carried New Game+ hero and route order', () => {
    const hero = newHero({ name: 'Ari', origin: 'tidebound', calling: 'pathmaker', deathMode: 'checkpoint' })
    hero.gold = 173
    hero.inventory.push('focusTonic')
    hero.traversalTools = ['stoneWedge', 'cordAnchor']
    const cycle = advanceCampaignTier(completeCampaignTier(initialCampaignCycle()))
    const areaOrder = ['furnace', 'mine', 'wilds', 'caverns'] as const
    const document = runAutoplay(newRun(71, areaOrder[0], 0, hero, [], [], areaOrder, cycle), { mode: 'visible', policy: 'clear', turnLimit: 1, captureTrace: true }).traceDocument!
    expect(document.records[0]?.replay).toMatchObject({ initialHero: { name: 'Ari', gold: 173, traversalTools: ['stoneWedge', 'cordAnchor'] }, areaOrder, difficulty: { campaignTier: 'ngPlus' } })
    expect(replayAutoplayTrace(document)).toMatchObject({ valid: true })
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
