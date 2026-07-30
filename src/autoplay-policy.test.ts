import { describe, expect, it } from 'vitest'
import { comparePolicyEvaluations, comparePolicyScores, createPolicyProfile, createPolicyRunMetadata, policyScoreTuple, scorePolicyEpisode } from './autoplay-policy'
import { runAutoplay } from './autoplay-runner'
import { newRun } from './engine'

const score = (campaignClears: number, deaths = 0, stalls = 0, explorationValue = 0, resourceEfficiency = 0) => ({ campaignClears, deaths, stalls, explorationValue, resourceEfficiency })

describe('autoplay policy contract', () => {
  it('orders scores lexicographically by the declared objective', () => {
    expect(comparePolicyScores(score(1, 9, 9, 0, 0), score(0, 0, 0, 999, 999))).toBeLessThan(0)
    expect(comparePolicyScores(score(1, 0, 9, 0, 0), score(1, 1, 0, 999, 999))).toBeLessThan(0)
    expect(comparePolicyScores(score(1, 0, 0, 0, 0), score(1, 0, 1, 999, 999))).toBeLessThan(0)
    expect(comparePolicyScores(score(1, 0, 0, 2, 0), score(1, 0, 0, 1, 999))).toBeLessThan(0)
    expect(comparePolicyScores(score(1, 0, 0, 2, 2), score(1, 0, 0, 2, 1))).toBeLessThan(0)
    expect(policyScoreTuple(score(1, 2, 3, 4, 5))).toEqual([1, 2, 3, 4, 5])
  })

  it('breaks exact objective ties by seed, then profile identity', () => {
    const profile = createPolicyProfile({ policy: 'clear', informationMode: 'visible' })
    expect(comparePolicyEvaluations({ score: score(1), seed: 7, profile }, { score: score(1), seed: 42, profile })).toBeLessThan(0)
    const omniscient = createPolicyProfile({ policy: 'clear', informationMode: 'omniscient' })
    expect(comparePolicyEvaluations({ score: score(1), seed: 7, profile }, { score: score(1), seed: 7, profile: omniscient })).not.toBe(0)
  })

  it('rejects hidden-map access from visible profiles', () => {
    expect(() => createPolicyProfile({ policy: 'clear', informationMode: 'visible', requestedInputs: ['observation', 'hidden-map'] })).toThrow('requests unavailable inputs: hidden-map')
    expect(createPolicyProfile({ policy: 'explore', informationMode: 'visible' }).requestedInputs).toEqual(['history', 'observation'])
  })

  it('covers every current policy and information mode', () => {
    for (const informationMode of ['visible', 'omniscient'] as const) for (const policy of ['survival', 'clear', 'explore', 'legacy'] as const) {
      expect(createPolicyProfile({ policy, informationMode })).toMatchObject({ version: 1, id: `${informationMode}-${policy}`, policy, informationMode, objectiveVersion: 1 })
    }
  })

  it('records deterministic run metadata and score tuple', () => {
    const report = runAutoplay(newRun(7), { mode: 'visible', policy: 'survival', turnLimit: 1 })
    expect(report.policyMetadata).toMatchObject({ seed: 7, turnBudget: 1, profile: { version: 1, id: 'visible-survival', informationMode: 'visible', objectiveVersion: 1 }, scoreTuple: expect.any(Array) })
    expect(report.policyMetadata.scoreTuple).toEqual(policyScoreTuple(report.policyMetadata.score))
  })

  it('scores resource use and incompletion without changing decisions', () => {
    const episode = scorePolicyEpisode({ campaignComplete: false, outcome: 'turn-limit', exploredTiles: 12, metrics: { turns: 0, actions: { moves: 0, attacks: 0, casts: 0, pickups: 0, bombs: 0, ropes: 0, rests: 0 }, kills: 0, damageDealt: 0, damageTaken: 0, goldGained: 0, goldSpent: 0, xpGained: 0, pickups: 2, bombsUsed: 1, ropesUsed: 2, itemsUsed: {}, boonPicks: {}, boonAugments: {}, relicPicks: {}, purchases: {}, enemyKills: {}, eventOutcomes: {}, deathCauses: {}, terrainInteractions: {}, bossPhases: {}, samples: [], floors: [] }, retainedResources: 8 })
    expect(episode).toEqual(score(0, 0, 1, 14, 5))
    const metadata = createPolicyRunMetadata(createPolicyProfile({ policy: 'legacy', informationMode: 'omniscient' }), 7, 3, episode)
    expect(metadata.scoreTuple).toEqual([0, 0, 1, 14, 5])
    expect(JSON.parse(JSON.stringify(metadata))).toEqual(metadata)
    expect(() => createPolicyRunMetadata(metadata.profile, -1, 3, episode)).toThrow('invalid autoplay policy seed')
    expect(() => createPolicyRunMetadata(metadata.profile, 7, 0, episode)).toThrow('invalid autoplay policy turn budget')
  })
})
