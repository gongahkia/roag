import { describe, expect, it } from 'vitest'
import { advance } from './combat'
import { castSpell } from './inventory'
import { gateForArea, resolveAreaGate } from './gates'
import { hasAstralGateAccess, intellectFocusDiscount, intellectFocusRecovery, intellectScriptRange, intellectWardBonus } from './intellect'
import { createEnemy, createHero, createRun } from '../test/factories'

describe('Intellect disciplines', () => {
  it('maps nodes to focus, script geometry, wards, and gate access', () => {
    const hero = createHero({ skills: ['int1', 'int2', 'int3', 'int4', 'int5', 'int6'] })
    expect(intellectFocusDiscount(hero)).toBe(1)
    expect(intellectFocusRecovery(hero)).toBe(2)
    expect(intellectScriptRange(hero)).toBe(3)
    expect(intellectWardBonus(hero)).toBe(2)
    expect(hasAstralGateAccess(hero)).toBe(true)
  })

  it('changes script cost and range, focus recovery, wards, and astral gates in the game flow', () => {
    const script = createRun({ hero: createHero({ skills: ['int1', 'int3'], inventory: ['ember'] }) })
    const target = createEnemy({ x: 3, y: 1, health: 30, maxHealth: 30 })
    script.floor.actors = [target]
    castSpell(script, 'ember', 'e')
    expect(script.hero.focus).toBe(6)
    expect(target.health).toBeLessThan(30)

    const insight = createRun({ turn: 7, hero: createHero({ focus: 0, skills: ['int2', 'int6'] }) })
    advance(insight, [])
    expect(insight.hero.focus).toBe(3)

    const ward = createRun({ hero: createHero({ skills: ['int4'] }) })
    castSpell(ward, 'wardScript', 'e')
    expect(ward.hero.maxHealth).toBe(26)
    expect(ward.hero.conditions).toContainEqual({ kind: 'shielded', duration: 2, potency: 3 })

    const gate = createRun({ area: 'caverns', hero: createHero({ gold: 40, skills: ['int6'], inventory: ['ward'] }) })
    expect(resolveAreaGate(gate, gateForArea('caverns'), 1)).toMatchObject({ resolved: true, destination: 'ruins' })
  })
})
