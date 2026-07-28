import { describe, expect, it } from 'vitest'
import { boonChoices, toolChoices } from './engine/buildcraft'
import { generateRouteContract, validateRouteContract } from './route-contract'
import { createFloor, createRun } from './test/factories'
import type { BoonRewardOffer, ToolRewardOffer } from './types'
import { generateAreaFloor, primaryToolUseAvailable, validateGeneration } from './world'

const input = { campaignSeed: 73, floorIndex: 0, biome: 'mine' as const, areaFloor: 0, recipeId: 'rail-spine', escalationVariant: 'braced-shaft:survey' }

describe('route reward contracts', () => {
  it('pre-rolls deterministic annotated safe, risky, and sidegrade offers', () => {
    const first = generateRouteContract(input)
    expect(first).toEqual(generateRouteContract(input))
    expect(validateRouteContract(first)).toEqual({ valid: true, errors: [] })
    expect(first.rewardOffers).toHaveLength(4)
    for (const offer of first.rewardOffers) {
      expect(offer.choices.map(choice => choice.role)).toEqual(['safe', 'risky', 'sidegrade'])
      expect(offer.choices[0]).toMatchObject({ biomeFit: 'local', route: expect.any(String), terrain: expect.any(String), problem: expect.any(String), payoff: expect.any(String) })
      expect(offer.choices[2]).toMatchObject({ biomeFit: 'global' })
    }
  })

  it('uses the generated drafts and falls back when a draft is invalid', () => {
    const contract = generateRouteContract(input)
    const boonSite = { id: 'milestone:0:boon-teach:1:1', kind: 'boon' as const, rewardKey: 'boon-teach' as const, x: 1, y: 1, discovered: true, claimed: false }
    const waycache = { id: 'milestone:0:waycache:2:1', kind: 'waycache' as const, rewardKey: 'waycache' as const, x: 2, y: 1, discovered: true, claimed: false }
    const state = createRun({ floor: createFloor({ rewardOffers: structuredClone(contract.rewardOffers), milestones: [boonSite, waycache] }) })
    const boonOffer = contract.rewardOffers.find((offer): offer is BoonRewardOffer => offer.milestoneId === 'boon-teach')!
    const toolOffer = contract.rewardOffers.find((offer): offer is ToolRewardOffer => offer.milestoneId === 'waycache')!
    expect(boonChoices(state, boonSite).map(choice => choice.id)).toEqual(boonOffer.choices.map(choice => choice.id))
    expect(toolChoices(state, waycache).map(choice => choice.id)).toEqual(toolOffer.choices.map(choice => choice.id))
    state.floor.rewardOffers![1] = { ...boonOffer, choices: [{ ...boonOffer.choices[0], id: 'invalidBoon' }, ...boonOffer.choices.slice(1)] }
    expect(boonChoices(state, boonSite).map(choice => choice.id)).not.toContain('invalidBoon')
  })

  it('places the local Waycache before the costly test and keeps its answer usable', () => {
    const floor = generateAreaFloor(73, 'mine', 0)
    expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    const waycache = floor.milestones.find(milestone => milestone.rewardKey === 'waycache')!
    const test = floor.milestones.find(milestone => milestone.rewardKey === 'boon-test')!
    const distance = (point: { x: number; y: number }) => Math.abs(point.x - floor.start.x) + Math.abs(point.y - floor.start.y)
    expect(distance(waycache)).toBeLessThan(distance(test))
    expect(floor.rewardOffers?.find(offer => offer.milestoneId === 'waycache')?.choices[0]).toMatchObject({ role: 'safe', biomeFit: 'local', route: 'safe' })
    expect(primaryToolUseAvailable(floor)).toBe(true)
  })
})
