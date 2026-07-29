import { describe, expect, it } from 'vitest'
import { castSpell } from './inventory'
import { evaluateEquipmentEffects } from './equipment'
import { equipmentDefense } from './shared'
import { createHero, createRun } from '../test/factories'

describe('contextual equipment effects', () => {
  it('evaluates passive, action, and triggered effects from equipped items', () => {
    const hero = createHero({ equipment: { mainHand: 'whip', offHand: 'buckler', charm: 'ward' } })
    expect(equipmentDefense(hero)).toBe(5)
    expect(evaluateEquipmentEffects(hero, 'action', { actionId: 'player-strike' }, { damage: 4, range: 2, cooldown: 0 })).toMatchObject({ values: { damage: 5, range: 2, cooldown: 0 }, effects: ['whip:surveying-strike'] })
    expect(evaluateEquipmentEffects(hero, 'triggered', { trigger: 'spell', scripts: ['ember'] })).toMatchObject({ values: { focus: 1 }, effects: ['ward:arcane-return'] })
  })

  it('applies spell-triggered focus recovery in the game flow', () => {
    const state = createRun({ hero: createHero({ equipment: { charm: 'ward' }, inventory: ['ember'] }) })
    expect(castSpell(state, 'ember', 'e')).toEqual([{ type: 'spell' }])
    expect(state.hero.focus).toBe(6)
  })
})
