import { describe, expect, it } from 'vitest'
import { combatReadabilityScenarios, validateCombatReadability } from './combat-readability'
import { damageHero } from './combat'
import { createRun } from '../test/factories'

describe('combat readability harness', () => {
  it('covers every shipped special action with deterministic terrain and counterplay evidence', () => {
    expect(validateCombatReadability()).toEqual([])
    expect(combatReadabilityScenarios.every(scenario => scenario.response.length > 0 && scenario.escape.length > 0 && scenario.forecast.length > 0)).toBe(true)
  })

  it('records a biome, recipe, and source for lethal combat outcomes', () => {
    const state = createRun()
    state.hero.health = 1
    damageHero(state, 4, 'test hazard', true)
    expect(state.telemetry?.deathCauses).toEqual({ 'mine:fixture:test hazard': 1 })
  })
})
