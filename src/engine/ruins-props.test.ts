import { describe, expect, it } from 'vitest'
import { propDefinition } from '../props'
import type { Prop, PropId } from '../types'
import { generateFloor, isPassable, validateGeneration } from '../world'
import { advance } from './combat'
import { castAstral } from './astral'
import { operate, useRope } from './inventory'
import { resolveLineEffect } from './line-effect'
import { applyPropEffects } from './props'
import { createRun } from '../test/factories'

const ruinsProp = (kind: Extract<PropId, `ruins.${string}`>, overrides: Partial<Prop> = {}): Prop => {
  const definition = propDefinition(kind)
  return { id: `prop:test:${kind}`, kind, x: 2, y: 1, biome: 'ruins', state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks], ...overrides }
}

const ruinsRun = () => {
  const state = createRun()
  state.floor.biome = 'ruins'
  return state
}

describe('Ruins props', () => {
  it('generates deterministic valid props without blocking mandatory routes', () => {
    for (const seed of [43, 177, 509, 829]) {
      const first = generateFloor(seed, 12)
      expect(first).toEqual(generateFloor(seed, 12))
      expect(validateGeneration(first)).toEqual({ valid: true, errors: [] })
    }
  })

  it('turns statues into cover or rubble and reads concrete tablet timing', () => {
    const statue = ruinsRun()
    statue.floor.props = [ruinsProp('ruins.brokenStatue')]
    applyPropEffects(statue, [{ x: 2, y: 1 }], ['force'])
    expect(resolveLineEffect(statue.floor, { x: 1, y: 1 }, { x: 5, y: 1 }).blocked).toMatchObject({ by: 'cover' })
    applyPropEffects(statue, [{ x: 2, y: 1 }], ['bomb'])
    expect(statue.floor.props[0].state).toBe('destroyed')
    expect(statue.floor.tiles[1 * 48 + 2].kind).toBe('rubble')

    const tablet = ruinsRun()
    tablet.floor.props = [ruinsProp('ruins.glyphTablet')]
    tablet.floor.telegraphs = [{ id: 'dart', sourceId: 'adept', actionId: 'enemy-dart', cells: [{ x: 3, y: 1 }], danger: 'major', resolveTurn: 3 }]
    operate(tablet)
    operate(tablet)
    expect(tablet.messages[0]).toContain('enemy-dart in 3')
  })

  it('fuels or quenches braziers, then opens arches with a rope or an axe', () => {
    const brazier = ruinsRun()
    brazier.hero.inventory = ['ember']
    brazier.floor.props = [ruinsProp('ruins.ritualBrazier')]
    operate(brazier)
    operate(brazier)
    expect(brazier.floor.props[0].state).toBe('activated')
    expect(brazier.floor.tiles[1 * 48 + 2].kind).toBe('fireVent')
    applyPropEffects(brazier, [{ x: 2, y: 1 }], ['water'])
    expect(brazier.floor.props[0].state).toBe('dormant')
    expect(brazier.floor.tiles[1 * 48 + 2].kind).toBe('floor')

    const warded = ruinsRun()
    warded.floor.props = [ruinsProp('ruins.ritualBrazier')]
    castAstral(warded, 'ward', { x: 2, y: 1 })
    expect(warded.floor.props[0].state).toBe('activated')
    expect(warded.hero.conditions).toEqual(expect.arrayContaining([expect.objectContaining({ kind: 'shielded' }), expect.objectContaining({ kind: 'marked' })]))

    const rope = ruinsRun()
    rope.hero.ropes = 1
    rope.floor.props = [ruinsProp('ruins.collapsedArch')]
    expect(isPassable(rope.floor, 2, 1)).toBe(false)
    operate(rope)
    useRope(rope)
    expect(rope.floor.props[0].state).toBe('activated')
    expect(isPassable(rope.floor, 2, 1)).toBe(true)

    const axe = ruinsRun()
    axe.hero.equipment.mainHand = 'pickaxe'
    axe.floor.props = [ruinsProp('ruins.collapsedArch')]
    operate(axe)
    operate(axe)
    expect(axe.floor.props[0].state).toBe('activated')
  })

  it('spends a real cache key and uses a monolith to absorb one telegraphed action', () => {
    const cache = ruinsRun()
    cache.hero.keys = 1
    cache.floor.props = [ruinsProp('ruins.sealedCache')]
    operate(cache)
    operate(cache)
    expect(cache.hero.keys).toBe(0)
    expect(cache.floor.props[0].state).toBe('activated')
    expect(cache.floor.items).toContainEqual(expect.objectContaining({ id: 'sunseal' }))

    const monolith = ruinsRun()
    monolith.hero.focus = 4
    monolith.floor.props = [ruinsProp('ruins.monolith')]
    monolith.floor.telegraphs = [{ id: 'ritual', sourceId: 'oracle', actionId: 'enemy-ritual', cells: [{ x: 3, y: 1 }], danger: 'major', resolveTurn: 2 }]
    operate(monolith)
    operate(monolith)
    expect(monolith.floor.props[0].state).toBe('activated')
    advance(monolith, [])
    expect(monolith.floor.props[0].state).toBe('destroyed')
    expect(monolith.floor.telegraphs).toHaveLength(0)
    expect(monolith.messages[0]).toContain('absorbs 1 nearby telegraph')
  })
})
