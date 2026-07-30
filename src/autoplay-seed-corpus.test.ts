import { describe, expect, it } from 'vitest'
import { autoplaySeedCorpusEntry, autoplaySeedCorpusManifest, autoplaySeedCorpusPartition, parseAutoplaySeedCorpus } from './autoplay-seed-corpus'
import { CAMPAIGN_AUTOPLAY_PROFILES, campaignAutoplayRunMatchesCorpus, campaignAutoplaySuite, compactCampaignAutoplayRun, type CampaignAutoplayRun } from './autoplay-campaign'
import { runAutoplay } from './autoplay-runner'
import { newSeededCampaignRun } from './engine'
import { campaignOrderForSeed } from './engine/campaign'

type RawManifest = { partitions: Record<string, Array<[number, string[]]>> }

describe('autoplay seed corpus', () => {
  it('contains fixed, disjoint 48-seed partitions with complete metadata', () => {
    const development = autoplaySeedCorpusPartition('development')
    const heldOut = autoplaySeedCorpusPartition('held-out')
    expect(development).toHaveLength(48)
    expect(heldOut).toHaveLength(48)
    expect(new Set([...development, ...heldOut].map(entry => entry.seed)).size).toBe(96)
    for (const entry of [...development, ...heldOut]) {
      expect(entry).toMatchObject({ gameVersion: '0.1.0', routeConfiguration: { id: 'four-selected-biomes-v1' }, policyModes: ['visible', 'omniscient'], turnBudget: 38_400, expectedValidation: 'non-error' })
      expect(entry.routeConfiguration.areaOrder).toEqual(campaignOrderForSeed(entry.seed))
    }
  })

  it('parses deterministically and rejects duplicate or unknown partitions', () => {
    const first = parseAutoplaySeedCorpus(autoplaySeedCorpusManifest)
    const second = parseAutoplaySeedCorpus(autoplaySeedCorpusManifest)
    expect(second).toEqual(first)
    const duplicate = structuredClone(autoplaySeedCorpusManifest) as RawManifest
    duplicate.partitions['held-out']![0]![0] = duplicate.partitions.development![0]![0]
    expect(() => parseAutoplaySeedCorpus(duplicate)).toThrow('duplicate autoplay seed corpus seed')
    const unknown = structuredClone(autoplaySeedCorpusManifest) as RawManifest
    unknown.partitions.experimental = []
    expect(() => parseAutoplaySeedCorpus(unknown)).toThrow('unknown autoplay seed corpus partition: experimental')
    expect(() => autoplaySeedCorpusPartition('experimental')).toThrow('unknown autoplay seed corpus partition: experimental')
    expect(() => autoplaySeedCorpusEntry('development', 999_999)).toThrow('unknown autoplay seed corpus seed: 999999')
  })

  it('smokes one deterministic campaign from each partition', () => {
    for (const partition of ['development', 'held-out'] as const) {
      const entry = autoplaySeedCorpusPartition(partition)[0]!
      const report = runAutoplay(newSeededCampaignRun(entry.seed), { mode: 'omniscient', policy: 'clear', turnLimit: 1, captureTrace: false })
      const compact = compactCampaignAutoplayRun(entry.seed, { id: 'omniscient-clear', mode: 'omniscient', policy: 'clear' }, report)
      expect(report.outcome).not.toBe('error')
      expect(campaignAutoplayRunMatchesCorpus(entry, compact)).toBe(true)
    }
  })

  it('selects each partition deterministically for the campaign runner', () => {
    for (const partition of ['development', 'held-out'] as const) {
      const entries = autoplaySeedCorpusPartition(partition)
      const runs: CampaignAutoplayRun[] = entries.flatMap(entry => CAMPAIGN_AUTOPLAY_PROFILES.map(profile => ({ seed: entry.seed, profile: profile.id, mode: profile.mode, policy: profile.policy, areaOrder: [...entry.routeConfiguration.areaOrder], campaignComplete: false, outcome: 'turn-limit', turns: 0, finalBiome: entry.routeConfiguration.areaOrder[0]!, floor: 1, completedAreas: [] })))
      expect(campaignAutoplaySuite(runs, partition)).toMatchObject({ version: 4, corpusVersion: 1, partition, seeds: entries.map(entry => entry.seed) })
    }
  })
})
