import { describe, expect, it } from 'vitest'
import { objectiveForFloor } from '../objectives'
import { createEnemy, createFloor, createHero, createRun } from '../test/factories'
import { moveHero } from './combat'
import { castSpell } from './inventory'

const guardian = (x: number) => createEnemy({ id: 'foreman', role: 'guardian', kind: 'foreman', name: 'The Foreman', ai: 'guardian', x, y: 1, health: 42, maxHealth: 42, attack: 8, defense: 14, speed: 105, energy: 0, guardianPhase: 'opening' })
const guardianRun = (seed: number, hero: ReturnType<typeof createHero>, target: ReturnType<typeof guardian>) => {
  const floor = createFloor({ index: 3, biome: 'mine', guardianDefeated: false, objective: objectiveForFloor(3), actors: [target] })
  return createRun({ seed, hero, floor })
}

const vanguard = (seed: number) => {
  const state = guardianRun(seed, createHero({ health: 40, maxHealth: 40, stats: { strength: 8, agility: 2, vitality: 2, intellect: 2 }, skills: ['str1', 'str2', 'str3', 'str4', 'str5', 'str6'], equipment: { mainHand: 'whip', body: 'mail' } }), guardian(2))
  for (let turn = 0; turn < 3 && !state.floor.guardianDefeated; turn++) moveHero(state, 'e')
  return { cleared: state.floor.guardianDefeated, objective: state.floor.objective.status, turn: state.turn, health: state.hero.health }
}

const controller = (seed: number) => {
  const state = guardianRun(seed, createHero({ stats: { strength: 2, agility: 2, vitality: 2, intellect: 8 }, skills: ['int1', 'int3'], inventory: ['lull', 'ember'] }), guardian(3))
  state.floor.tiles[1 * 48 + 3].kind = 'gas'
  castSpell(state, 'lull', 'e')
  castSpell(state, 'ember', 'e')
  castSpell(state, 'ember', 'e')
  return { cleared: state.floor.guardianDefeated, objective: state.floor.objective.status, turn: state.turn, health: state.hero.health, focus: state.hero.focus }
}

describe('seeded build simulations', () => {
  it('keeps representative guardian clears deterministic and within power bands', () => {
    expect(vanguard(6201)).toEqual(vanguard(6201))
    expect(controller(6202)).toEqual(controller(6202))
    expect(vanguard(6201)).toMatchObject({ cleared: true, objective: 'complete', turn: expect.any(Number), health: expect.any(Number) })
    expect(vanguard(6201).turn).toBeLessThanOrEqual(3)
    expect(vanguard(6201).health).toBeGreaterThanOrEqual(25)
    expect(controller(6202)).toMatchObject({ cleared: true, objective: 'complete', turn: 3, health: 23, focus: 1 })
  })
})
