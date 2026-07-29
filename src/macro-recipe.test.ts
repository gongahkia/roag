import { describe, expect, it } from 'vitest'
import { compileRouteContract, macroRecipeFor } from './macro-recipe'
import { generateRouteContract, type RouteContractInput } from './route-contract'
import { generateAreaFloor, macroRecipeDebug, validateMacroRecipe } from './world'

const input = (overrides: Partial<RouteContractInput> = {}): RouteContractInput => ({ campaignSeed: 101, floorIndex: 0, biome: 'mine', areaFloor: 0, recipeId: 'rail-spine', escalationVariant: 'stage-1', ...overrides })

describe('route macro recipes', () => {
  it('compiles byte-stable role geometry across every topology family', () => {
    const cases = [
      ['mine', 'rail-spine', 48, 35],
      ['wilds', 'river-clearings', 72, 48],
      ['furnace', 'kiln-terraces', 56, 40],
      ['floodedRuins', 'braided-islands', 72, 48],
      ['burial', 'rolling-mounds', 80, 56]
    ] as const
    const topologies = new Set<string>()
    for (const [biome, recipeId, width, height] of cases) {
      const contract = generateRouteContract(input({ biome, recipeId }))
      const first = compileRouteContract(contract, { width, height })
      expect(first).toEqual(compileRouteContract(contract, { width, height }))
      expect(first).toMatchObject({ valid: true, recipeId: macroRecipeFor(contract).id })
      expect(first.nodes).toHaveLength(contract.nodes.length)
      expect(first.edges).toHaveLength(contract.edges.length)
      expect(first.edges.every(edge => edge.cells.length > 1)).toBe(true)
      topologies.add(first.topology)
    }
    expect(topologies).toEqual(new Set(['tight', 'broad', 'vertical', 'directedFlow', 'looped']))
  })

  it('reports deterministic named placement failures after bounded retries', () => {
    const contract = generateRouteContract(input())
    const first = compileRouteContract(contract, { width: 8, height: 8 })
    expect(first).toEqual(compileRouteContract(contract, { width: 8, height: 8 }))
    expect(first).toMatchObject({ valid: false })
    expect(first.diagnostics).toEqual(expect.arrayContaining([expect.stringMatching(/^attempt 0: node .*footprint/), expect.stringMatching(/^attempt 2: node .*footprint/)]))
  })

  it('keeps every Mine floor-four remix footprint disjoint', () => {
    for (const recipeId of ['rail-spine-remix', 'branching-drifts-remix', 'collapse-loop-remix']) {
      const contract = generateRouteContract(input({ areaFloor: 3, floorIndex: 3, recipeId, escalationVariant: 'stage-4' }))
      expect(compileRouteContract(contract, { width: 48, height: 35 })).toMatchObject({ valid: true })
    }
  })

  it('keeps Mine connectors visible in generated-floor metadata and traversable after props', () => {
    for (const recipeFloor of [0, 1, 2, 3]) {
      const floor = generateAreaFloor(101, 'mine', recipeFloor)
      const debug = macroRecipeDebug(floor)
      expect(debug).toMatchObject({ valid: true, recipeId: expect.stringMatching(/^mine:/) })
      expect(debug?.nodes.map(node => node.nodeId)).toHaveLength(6)
      expect(debug?.edges.map(edge => edge.edgeId)).toHaveLength(6)
      expect(validateMacroRecipe(floor)).toEqual([])
    }
  })
})
