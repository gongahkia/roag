import { describe, expect, it } from 'vitest'
import { advance, explode } from './engine/combat'
import { castAstral } from './engine/astral'
import { castEmber } from './engine/ember'
import { operate, throwItem } from './engine/inventory'
import { castVerdant } from './engine/verdant'
import { PROP_DEFINITIONS, PROP_IDS, propDefinition, validatePropDefinitions } from './props'
import { spriteSheetSpecs } from './sprites'
import { createRun } from './test/factories'
import type { Prop } from './types'
import { generateFloor, hasPassableTerrainPath, validateGeneration } from './world'

const prop = (overrides: Partial<Prop> = {}): Prop => ({ id: 'prop:test:mine.oreVein:2:1', kind: 'mine.oreVein', x: 2, y: 1, biome: 'mine', state: 'dormant', tags: ['salvage', 'force'], hooks: ['operate', 'bomb', 'force', 'throw'], ...overrides })

describe('world props', () => {
  it('maps every manifest prop cell to one complete definition', () => {
    const manifestIds = spriteSheetSpecs.flatMap(sheet => sheet.labels.filter(label => label.startsWith('prop.')).map(label => label.slice('prop.'.length))).sort()
    expect(PROP_DEFINITIONS).toHaveLength(24)
    expect(manifestIds).toEqual([...PROP_IDS].sort())
    expect(validatePropDefinitions()).toEqual([])
    for (const id of PROP_IDS) expect(propDefinition(id).hooks).toEqual(expect.arrayContaining(['operate']))
  })

  it('generates deterministic, reachable overlay props without changing route validation', () => {
    for (const floorIndex of Array.from({ length: 16 }, (_, index) => index)) {
      const first = generateFloor(12345, floorIndex)
      const second = generateFloor(12345, floorIndex)
      expect(first.props).toEqual(second.props)
      expect(first.props).not.toHaveLength(0)
      expect(validateGeneration(first)).toEqual({ valid: true, errors: [] })
      for (const current of first.props) expect(hasPassableTerrainPath(first, first.start, current)).toBe(true)
    }
  })

  it('rejects unreachable or overlapping props', () => {
    const floor = generateFloor(5, 0)
    floor.props[0].x = 0
    floor.props[0].y = 0
    floor.props.push({ ...floor.props[0], id: 'prop:duplicate' })
    expect(validateGeneration(floor)).toMatchObject({ valid: false, errors: expect.arrayContaining([`illegal prop placement: ${floor.props[0].id}`, 'overlapping prop placement: 0,0']) })
  })

  it('keeps existing C actions ahead of props, then transitions props deterministically', () => {
    const state = createRun()
    state.floor.props = [prop()]
    state.floor.tiles[1 * 48 + 2].kind = 'crate'
    expect(operate(state).map(event => event.type)).toEqual(['pickup'])
    expect(state.floor.props[0].state).toBe('dormant')
    state.floor.tiles[1 * 48 + 2].kind = 'floor'
    expect(operate(state).map(event => event.type)).toEqual(['pickup'])
    expect(state.floor.props[0].state).toBe('activated')
    expect(state.floor.items).toContainEqual(expect.objectContaining({ id: 'rock', x: 2, y: 1 }))
  })

  it('routes explosions through reusable prop effect hooks', () => {
    const state = createRun()
    state.floor.props = [prop()]
    explode(state, 2, 1, 4)
    expect(state.floor.props[0].state).toBe('destroyed')
    expect(state.floor.items).toContainEqual(expect.objectContaining({ id: 'rock', x: 2, y: 1 }))
  })

  it('routes spell, throw, and telegraph effects through prop hooks', () => {
    const root = createRun()
    root.floor.props = [prop({ kind: 'wilds.rootShrine', biome: 'wilds', hooks: ['operate', 'root'], tags: ['ritual', 'root', 'growth'] })]
    castVerdant(root, 'root', { x: 2, y: 1 })
    expect(root.floor.props[0].state).toBe('destroyed')

    const force = createRun()
    force.floor.props = [prop({ kind: 'caverns.crystalCluster', biome: 'caverns', hooks: ['operate', 'force'], tags: ['salvage', 'force', 'light'] })]
    castAstral(force, 'gust', { x: 2, y: 1 })
    expect(force.floor.props[0].state).toBe('destroyed')

    const fire = createRun()
    fire.floor.props = [prop({ kind: 'mine.lanternPost', hooks: ['operate', 'fire'], tags: ['light', 'fire', 'hazard'] })]
    castEmber(fire, { x: 2, y: 1 })
    expect(fire.floor.props[0].state).toBe('destroyed')

    const thrown = createRun()
    thrown.hero.inventory = ['rock']
    thrown.floor.props = [prop({ x: 6, y: 1 })]
    throwItem(thrown, 'rock', 'e')
    expect(thrown.floor.props[0].state).toBe('destroyed')

    const hazard = createRun()
    hazard.floor.props = [prop({ kind: 'mine.warningMarker', hooks: ['operate', 'hazard'], tags: ['warning', 'hazard'] })]
    hazard.floor.telegraphs = [{ id: 'hazard', sourceId: 'missing', actionId: 'enemy-shot', cells: [{ x: 2, y: 1 }], danger: 'minor', resolveTurn: 1 }]
    advance(hazard, [])
    expect(hazard.floor.props[0].state).toBe('destroyed')
  })
})
