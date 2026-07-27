import { describe, expect, it } from 'vitest'
import { generateRouteContract, validateRouteContract } from './route-contract'

describe('route contracts', () => {
  it('is deterministic and valid for every biome floor', () => {
    for (const biome of ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'] as const) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
      const first = generateRouteContract(42, areaFloor, biome, areaFloor, 'test-recipe')
      expect(first).toEqual(generateRouteContract(42, areaFloor, biome, areaFloor, 'test-recipe'))
      expect(validateRouteContract(first)).toEqual({ valid: true, errors: [] })
    }
  })

  it('reports a broken route branch by name', () => {
    const contract = generateRouteContract(42, 0, 'mine', 0, 'rail-spine')
    contract.edges = contract.edges.filter(edge => edge.from !== `${contract.biome}:0:rail-spine:fork` || edge.to !== `${contract.biome}:0:rail-spine:riskRoute`)
    expect(validateRouteContract(contract)).toEqual(expect.objectContaining({ valid: false, errors: expect.arrayContaining(['fork lacks safe and risk branches']) }))
  })
})
