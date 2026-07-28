import fc from 'fast-check'
import { describe, expect, it } from 'vitest'
import { newRun } from './engine'
import { BIOME_POOL } from './engine/campaign'
import { validateRouteContract } from './route-contract'
import { migrateRunRecord } from './storage'
import { socialContractFor } from './social-contract'
import type { Biome } from './types'
import { generateAreaFloor, macroRecipeDebug, placementDebug, routeContractDebug, tacticalEncounterDebug, validateGeneration } from './world'

const generatedCase = fc.record({
  seed: fc.integer({ min: 0, max: 0x7fffffff }),
  biome: fc.constantFrom(...BIOME_POOL) as fc.Arbitrary<Biome>,
  areaFloor: fc.integer({ min: 0, max: 3 }),
  routePosition: fc.integer({ min: 0, max: BIOME_POOL.length - 1 })
})

describe('spatial contracts', () => {
  it('preserves generated route, recipe, placement, event, reward, group, boss, and persistence contracts', () => {
    fc.assert(fc.property(generatedCase, ({ seed, biome, areaFloor, routePosition }) => {
      const floor = generateAreaFloor(seed, biome, areaFloor, routePosition)
      const route = routeContractDebug(floor)
      const macro = macroRecipeDebug(floor)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
      expect(route?.id).toContain(`route:${biome}:${floor.index}:${floor.layoutId}:`)
      expect(route ? validateRouteContract(route) : undefined).toEqual({ valid: true, errors: [] })
      expect(macro).toMatchObject({ valid: true, recipeId: `${biome}:${floor.layoutId.replace(/-remix$/, '')}`, diagnostics: [] })
      expect(placementDebug(floor).every(entry => entry.selected || entry.diagnostics.length > 0)).toBe(true)
      expect(floor.ecology).toEqual(expect.arrayContaining([expect.objectContaining({ state: 'waiting', target: expect.any(Object), responses: expect.any(Array) })]))
      expect(floor.ecology?.every(event => event.responses.length > 0 && event.target.x >= 0 && event.target.x < floor.width && event.target.y >= 0 && event.target.y < floor.height)).toBe(true)
      expect(floor.rewardOffers?.every(offer => offer.choices.map(choice => choice.role).join(',') === 'safe,risky,sidegrade')).toBe(true)
      const tactical = tacticalEncounterDebug(floor)
      if (areaFloor === 3) expect(floor.actors.some(actor => actor.role === 'guardian')).toBe(true)
      else {
        expect(floor.actors.some(actor => actor.role === 'guardian')).toBe(false)
        expect(tactical.length).toBeGreaterThan(0)
        expect(tactical.every(group => group.answer.length > 0 && floor.actors.some(actor => actor.encounter?.id === group.id))).toBe(true)
      }
      const social = socialContractFor({ seed: floor.seed, floorIndex: floor.index, biome, recipeId: floor.layoutId, arcId: floor.escalation?.arcId })
      expect(floor.encounters?.[0]?.social).toEqual(social)
      const saved = newRun(seed)
      saved.reputation = { trailfolk: seed % 9 - 4, kami: areaFloor - 2 }
      expect(migrateRunRecord(JSON.parse(JSON.stringify(saved)))).toMatchObject({ seed, reputation: saved.reputation, floor: { seed: saved.floor.seed, biome: 'mine' } })
      const replay = generateAreaFloor(seed, biome, areaFloor, routePosition)
      expect(JSON.stringify(replay)).toBe(JSON.stringify(floor))
    }), { seed: 128, numRuns: 24, verbose: 2 })
  }, 90_000)
})
