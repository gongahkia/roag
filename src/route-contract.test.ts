import { describe, expect, it } from 'vitest'
import { generateRouteContract, validateRouteContract, type RouteContract, type RouteContractInput } from './route-contract'

const input = (overrides: Partial<RouteContractInput> = {}): RouteContractInput => ({ campaignSeed: 42, floorIndex: 0, biome: 'mine', areaFloor: 0, recipeId: 'rail-spine', escalationVariant: 'survey', ...overrides })
const clone = (contract: RouteContract): RouteContract => structuredClone(contract)

describe('route contracts', () => {
  it('is byte-stable for fixed campaign, biome, recipe, and escalation inputs', () => {
    for (const biome of ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'] as const) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
      const routeInput = input({ biome, floorIndex: areaFloor, areaFloor, escalationVariant: `stage-${areaFloor}` })
      const first = generateRouteContract(routeInput)
      expect(JSON.stringify(first)).toBe(JSON.stringify(generateRouteContract(routeInput)))
      expect(validateRouteContract(first)).toEqual({ valid: true, errors: [] })
      expect(first.nodes.every(node => Object.keys(node.tags).sort().join(',') === 'encounter,escalation,gate,reward,terrain,visual')).toBe(true)
      expect(first.edges.every(edge => Object.keys(edge.tags).sort().join(',') === 'encounter,escalation,gate,reward,terrain,visual')).toBe(true)
    }
  })

  it('models a lock, loop, and optional reward without breaking the main route', () => {
    const contract = clone(generateRouteContract(input()))
    const safe = contract.edges.find(edge => edge.modes.includes('safe'))!
    safe.tags.gate = ['locked:key']
    const reward = contract.nodes.find(node => node.kind === 'optionalReward')!
    const fork = contract.nodes.find(node => node.kind === 'fork')!
    contract.edges.push({ id: 'optional-reward-loop', from: reward.id, to: fork.id, modes: ['optional'], tags: structuredClone(reward.tags) })
    expect(validateRouteContract(contract)).toEqual({ valid: true, errors: [] })
  })

  it('names the broken node or edge in diagnostics', () => {
    const contract = clone(generateRouteContract(input()))
    const fork = contract.nodes.find(node => node.kind === 'fork')!
    const costly = contract.edges.find(edge => edge.from === fork.id && edge.modes.includes('costly'))!
    contract.edges = contract.edges.filter(edge => edge.id !== costly.id)
    expect(validateRouteContract(contract)).toEqual(expect.objectContaining({ valid: false, errors: expect.arrayContaining([`node ${fork.id}: missing costly route choice`, `node ${contract.nodes.find(node => node.kind === 'optionalReward')!.id}: optional payoff is unreachable from ${contract.nodes.find(node => node.kind === 'start')!.id}`]) }))
  })

  it('rejects malformed tags and multiple main routes with named diagnostics', () => {
    const contract = clone(generateRouteContract(input()))
    const start = contract.nodes.find(node => node.kind === 'start')!
    const end = contract.nodes.find(node => node.kind === 'exit')!
    contract.edges.push({ id: 'duplicate-main', from: start.id, to: end.id, modes: ['main'], tags: { ...structuredClone(start.tags), visual: [] } })
    expect(validateRouteContract(contract)).toEqual(expect.objectContaining({ valid: false, errors: expect.arrayContaining(['edge duplicate-main: missing visual tags', 'node ' + start.id + ': expected exactly one main route to ' + end.id + ', found 2']) }))
  })
})
