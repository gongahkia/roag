import { describe, expect, it } from 'vitest'
import { propDefinition } from '../props'
import type { Prop, PropId } from '../types'
import { generateFloor, isPassable, validateGeneration } from '../world'
import { advance } from './combat'
import { operate } from './inventory'
import { applyPropEffects } from './props'
import { createRun } from '../test/factories'

const wildsProp = (kind: Extract<PropId, `wilds.${string}`>, overrides: Partial<Prop> = {}): Prop => {
  const definition = propDefinition(kind)
  return { id: `prop:test:${kind}`, kind, x: 2, y: 1, biome: 'wilds', state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks], ...overrides }
}

const wildsRun = () => {
  const state = createRun()
  state.floor.biome = 'wilds'
  return state
}

describe('Wilds props', () => {
  it('generates deterministic, valid Wilds overlays without blocking mandatory routes', () => {
    for (const seed of [91, 217, 503, 801]) {
      const first = generateFloor(seed, 4)
      const second = generateFloor(seed, 4)
      expect(first).toEqual(second)
      expect(first.props.every(prop => prop.biome === 'wilds')).toBe(true)
      expect(validateGeneration(first)).toEqual({ valid: true, errors: [] })
    }
  })

  it('offers harvest, cut, loot, shrine, and recovery choices with distinct outcomes', () => {
    const mushrooms = wildsRun()
    mushrooms.floor.props = [wildsProp('wilds.mushrooms')]
    operate(mushrooms)
    expect(mushrooms.messages[0]).toContain('Harvest')
    operate(mushrooms)
    expect(mushrooms.floor.props[0].state).toBe('activated')
    expect(mushrooms.floor.items).toContainEqual(expect.objectContaining({ id: 'tonic' }))

    const charm = wildsRun()
    charm.hero.equipment.mainHand = 'machete'
    charm.floor.tiles[1 * 48 + 3].kind = 'bramble'
    charm.floor.props = [wildsProp('wilds.danglingCharm')]
    operate(charm)
    operate(charm)
    expect(charm.floor.props[0].state).toBe('destroyed')
    expect(charm.floor.tiles[1 * 48 + 3].kind).toBe('floor')

    const parcel = wildsRun()
    parcel.floor.tiles[1 * 48 + 3].kind = 'bramble'
    parcel.floor.props = [wildsProp('wilds.lostParcel')]
    operate(parcel)
    operate(parcel)
    expect(parcel.floor.items).toContainEqual(expect.objectContaining({ id: 'ropeBundle' }))
    expect(parcel.floor.tiles[1 * 48 + 3].kind).toBe('floor')
  })

  it('creates reversible thorn screens and controlled nest threats', () => {
    const shrine = wildsRun()
    shrine.hero.inventory = ['root']
    shrine.floor.props = [wildsProp('wilds.rootShrine')]
    operate(shrine)
    operate(shrine)
    const screen = shrine.floor.props[0].effectCells ?? []
    expect(screen).not.toHaveLength(0)
    expect(screen.every(point => shrine.floor.tiles[point.y * 48 + point.x].kind === 'bramble')).toBe(true)
    while (shrine.turn < 4) advance(shrine, [])
    expect(screen.every(point => shrine.floor.tiles[point.y * 48 + point.x].kind === 'floor')).toBe(true)

    const nest = wildsRun()
    nest.floor.props = [wildsProp('wilds.birdNest')]
    applyPropEffects(nest, [{ x: 2, y: 1 }], ['throw'])
    expect(nest.floor.actors).toContainEqual(expect.objectContaining({ kind: 'startledBirds', hostile: true, ai: 'chase' }))
    while (nest.turn < 4) advance(nest, [])
    expect(nest.floor.actors.find(actor => actor.kind === 'startledBirds')).toBeUndefined()
  })

  it('keeps the root arch closed until a visible alternative opens its route', () => {
    const state = wildsRun()
    state.floor.props = [wildsProp('wilds.rootArch')]
    expect(isPassable(state.floor, 2, 1)).toBe(false)
    operate(state)
    expect(state.messages[0]).toContain('Brush Blade')
    applyPropEffects(state, [{ x: 2, y: 1 }], ['root'])
    expect(state.floor.props[0].state).toBe('activated')
    expect(isPassable(state.floor, 2, 1)).toBe(true)
  })
})
