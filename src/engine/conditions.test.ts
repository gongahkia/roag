import { describe, expect, it } from 'vitest'
import { addCondition, conditionSpeed, hasCondition, modifyIncomingDamage, tickConditions } from './conditions'
import { advance, moveHero } from './combat'
import { createEnemy, createRun } from '../test/factories'

describe('condition framework', () => {
  it('ticks every condition deterministically and expires it', () => {
    const actor = createEnemy()
    for (const kind of ['burning', 'rooted', 'staggered', 'shielded', 'marked', 'slowed'] as const) addCondition(actor, { kind, duration: 1, potency: 2 })
    expect(hasCondition(actor, 'rooted')).toBe(true)
    expect(tickConditions(actor)).toEqual({ burningDamage: 2, expired: ['burning', 'rooted', 'staggered', 'shielded', 'marked', 'slowed'] })
    expect(actor.conditions).toEqual([])
  })

  it('applies slow, shield, mark, and burning during combat turns', () => {
    const state = createRun()
    addCondition(state.hero, { kind: 'burning', duration: 1, potency: 3 })
    addCondition(state.hero, { kind: 'shielded', duration: 2, potency: 2 })
    addCondition(state.hero, { kind: 'marked', duration: 2, potency: 1 })
    expect(conditionSpeed(createEnemy({ conditions: [{ kind: 'slowed', duration: 2, potency: 2 }] }), 100)).toBe(60)
    expect(modifyIncomingDamage(state.hero, 10)).toBe(9)
    const health = state.hero.health
    advance(state, [])
    expect(state.hero.health).toBe(health - 2)
    expect(state.hero.conditions).toContainEqual({ kind: 'shielded', duration: 1, potency: 2 })
  })

  it('prevents rooted movement and staggered enemy actions until their tick expires', () => {
    const rooted = createRun()
    addCondition(rooted.hero, { kind: 'rooted', duration: 1, potency: 1 })
    moveHero(rooted, 'e')
    expect(rooted.hero).toMatchObject({ x: 1, y: 1, conditions: [] })
    const staggeredEnemy = createEnemy({ x: 3, y: 1, energy: 100, conditions: [{ kind: 'staggered', duration: 1, potency: 1 }] })
    const staggered = createRun()
    staggered.floor.actors = [staggeredEnemy]
    advance(staggered, [])
    expect(staggeredEnemy).toMatchObject({ x: 3, y: 1, conditions: [] })
  })
})
