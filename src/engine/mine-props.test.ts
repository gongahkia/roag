import { describe, expect, it } from 'vitest'
import { castAstral } from './astral'
import { explode } from './combat'
import { castEmber } from './ember'
import { operate, useRope } from './inventory'
import { resolveLineEffect } from './line-effect'
import { applyPropEffects } from './props'
import { refreshFov } from './visibility'
import { generateAreaFloor, hasPassablePath, isPassable, validateGeneration } from '../world'
import { createEnemy, createHero, createRun } from '../test/factories'
import type { Prop, PropId } from '../types'

const mineProp = (kind: PropId, x = 2, y = 1): Prop => {
  const values: Record<Extract<PropId, `mine.${string}`>, Pick<Prop, 'tags' | 'hooks'>> = {
    'mine.oreVein': { tags: ['salvage', 'force'], hooks: ['operate', 'bomb', 'force', 'throw'] },
    'mine.lanternPost': { tags: ['light', 'fire', 'hazard'], hooks: ['operate', 'fire', 'water', 'hazard'] },
    'mine.brokenCart': { tags: ['route', 'force', 'salvage'], hooks: ['operate', 'bomb', 'force', 'throw'] },
    'mine.warningMarker': { tags: ['warning', 'hazard'], hooks: ['operate', 'fire', 'throw', 'hazard'] },
    'mine.skullMarker': { tags: ['warning', 'hazard'], hooks: ['operate', 'bomb', 'throw', 'hazard'] },
    'mine.discardedParcel': { tags: ['cache', 'salvage'], hooks: ['operate', 'bomb', 'fire', 'throw', 'hazard'] }
  }
  return { id: `prop:test:${kind}:${x}:${y}`, kind, x, y, biome: 'mine', state: 'dormant', ...values[kind as Extract<PropId, `mine.${string}`>] }
}

describe('Mine props', () => {
  it('mines ore with a pickaxe, leaves rubble, and lets bombs clear it', () => {
    const mined = createRun({ hero: createHero({ equipment: { mainHand: 'pickaxe' } }) })
    mined.floor.props = [mineProp('mine.oreVein')]
    operate(mined)
    expect(mined.floor.props[0].state).toBe('inspected')
    expect(operate(mined).map(entry => entry.type)).toEqual(expect.arrayContaining(['pickup', 'boom']))
    expect(mined.floor.props[0].state).toBe('destroyed')
    expect(mined.floor.tiles[1 * 48 + 2].kind).toBe('rubble')
    expect(mined.floor.items).toContainEqual(expect.objectContaining({ id: 'rock', count: 2 }))

    const blasted = createRun()
    blasted.floor.props = [mineProp('mine.oreVein')]
    explode(blasted, 2, 1, 4)
    expect(blasted.floor.props[0].state).toBe('destroyed')
    expect(blasted.floor.tiles[1 * 48 + 2].kind).toBe('floor')
    expect(blasted.floor.items).toContainEqual(expect.objectContaining({ id: 'rock', count: 2 }))
  })

  it('relights lanterns and reveals only their local line-of-sight area', () => {
    const state = createRun({ hero: createHero({ x: 1, y: 1 }) })
    state.floor.props = [mineProp('mine.lanternPost', 20, 20)]
    state.floor.tiles.forEach(tile => { tile.explored = false; tile.visible = false })
    castEmber(state, { x: 20, y: 20 })
    refreshFov(state)
    expect(state.floor.props[0].state).toBe('activated')
    expect(state.floor.tiles[20 * 48 + 24].visible).toBe(true)
    expect(state.floor.tiles[20 * 48 + 25].visible).toBe(false)
    applyPropEffects(state, [{ x: 20, y: 20 }], ['water'])
    expect(state.floor.props[0].state).toBe('dormant')
  })

  it('moves carts on rails, uses them as cover, harms collisions, and triggers adjacent traps', () => {
    const state = createRun()
    state.floor.props = [mineProp('mine.brokenCart')]
    for (const x of [2, 3, 4]) state.floor.tiles[1 * 48 + x].kind = 'rail'
    state.floor.tiles[2 * 48 + 3].kind = 'fireVent'
    operate(state)
    const events = operate(state)
    expect(events.map(entry => entry.type)).toEqual(expect.arrayContaining(['move', 'danger']))
    expect(state.floor.props[0]).toMatchObject({ state: 'activated', x: 4, y: 1 })
    expect(state.floor.tiles[2 * 48 + 3].kind).toBe('floor')
    expect(isPassable(state.floor, 4, 1)).toBe(false)
    expect(resolveLineEffect(state.floor, { x: 1, y: 1 }, { x: 6, y: 1 }).blocked?.by).toBe('cart')

    const collision = createRun()
    collision.floor.props = [mineProp('mine.brokenCart')]
    for (const x of [2, 3]) collision.floor.tiles[1 * 48 + x].kind = 'rail'
    collision.floor.actors = [createEnemy({ x: 3, y: 1, health: 5, maxHealth: 5 })]
    operate(collision)
    expect(operate(collision).map(entry => entry.type)).toContain('hit')
    expect(collision.floor.actors).toEqual([])

    const friendly = createRun()
    friendly.floor.props = [mineProp('mine.brokenCart')]
    for (const x of [2, 3]) friendly.floor.tiles[1 * 48 + x].kind = 'rail'
    friendly.floor.actors = [createEnemy({ role: 'ally', hostile: false, name: 'stranded traveler', x: 3, y: 1 })]
    operate(friendly)
    expect(operate(friendly)).toEqual([])
    expect(friendly.floor.props[0]).toMatchObject({ x: 2, y: 1 })
    expect(friendly.floor.actors[0]).toMatchObject({ x: 3, y: 1, hostile: false })
  })

  it('uses a rope to release an inspected cart without changing pit behavior', () => {
    const state = createRun({ hero: createHero({ ropes: 1 }) })
    state.floor.props = [mineProp('mine.brokenCart')]
    for (const x of [2, 3]) state.floor.tiles[1 * 48 + x].kind = 'rail'
    operate(state)
    expect(useRope(state).map(entry => entry.type)).toEqual(expect.arrayContaining(['rope', 'move']))
    expect(state.hero.ropes).toBe(0)
    expect(state.floor.props[0]).toMatchObject({ state: 'activated', x: 3, y: 1 })
  })

  it('uses Gust and Pull to move carts along their rail direction', () => {
    const gust = createRun()
    gust.floor.props = [mineProp('mine.brokenCart')]
    for (const x of [2, 3, 4]) gust.floor.tiles[1 * 48 + x].kind = 'rail'
    castAstral(gust, 'gust', { x: 2, y: 1 })
    expect(gust.floor.props[0].x).toBe(4)

    const pull = createRun({ hero: createHero({ x: 5, y: 1 }) })
    pull.floor.props = [mineProp('mine.brokenCart', 3, 1)]
    for (const x of [2, 3, 4]) pull.floor.tiles[1 * 48 + x].kind = 'rail'
    castAstral(pull, 'pull', { x: 3, y: 1 })
    expect(pull.floor.props[0].x).toBe(4)
  })

  it('reveals limited marker warnings and lets a parcel be recovered or detonated', () => {
    const marker = createRun()
    marker.floor.props = [mineProp('mine.skullMarker')]
    marker.floor.tiles.forEach(tile => { tile.explored = false })
    marker.floor.tiles[1 * 48 + 4].kind = 'dart'
    marker.floor.tiles[1 * 48 + 5].kind = 'fireVent'
    marker.floor.actors = [createEnemy({ x: 3, y: 2 })]
    operate(marker)
    expect(operate(marker).map(entry => entry.type)).toContain('danger')
    expect(marker.floor.tiles[1 * 48 + 4].explored).toBe(true)
    expect(marker.floor.tiles[1 * 48 + 5].explored).toBe(true)
    expect(marker.floor.tiles[2 * 48 + 3].explored).toBe(true)

    const recovered = createRun()
    recovered.floor.props = [mineProp('mine.discardedParcel')]
    operate(recovered)
    expect(operate(recovered).map(entry => entry.type)).toContain('pickup')
    expect(recovered.floor.items).toContainEqual(expect.objectContaining({ id: 'ropeBundle' }))

    const detonated = createRun()
    detonated.floor.props = [mineProp('mine.discardedParcel')]
    explode(detonated, 2, 1, 4)
    expect(detonated.floor.props[0].state).toBe('destroyed')
    expect(detonated.messages).toContain('The abandoned parcel bursts into a noisy blast.')
  })

  it('generates Mine props in rail, light, excavation, and warning contexts', () => {
    const floors = Array.from({ length: 48 }, (_, seed) => generateAreaFloor(seed + 1, 'mine', 0))
    const carts = floors.flatMap(floor => floor.props.filter(prop => prop.kind === 'mine.brokenCart').map(prop => ({ floor, prop })))
    expect(carts.length).toBeGreaterThan(0)
    for (const { floor, prop } of carts) {
      expect(floor.tiles[prop.y * 48 + prop.x].kind).toBe('rail')
      expect([[0, -1], [1, 0], [0, 1], [-1, 0]].some(([x, y]) => floor.tiles[(prop.y + y) * 48 + prop.x + x]?.kind === 'rail')).toBe(true)
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
      expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
    }
    for (const floor of floors) for (const prop of floor.props) {
      const nearby = (radius: number, kinds: string[]) => {
        for (let y = prop.y - radius; y <= prop.y + radius; y++) for (let x = prop.x - radius; x <= prop.x + radius; x++) if (floor.tiles[y * 48 + x]?.kind && kinds.includes(floor.tiles[y * 48 + x].kind)) return true
        return false
      }
      if (prop.kind === 'mine.oreVein') expect(nearby(1, ['support', 'rubble', 'boulder'])).toBe(true)
      if (prop.kind === 'mine.lanternPost' || prop.kind === 'mine.discardedParcel') expect(nearby(1, ['rail', 'support'])).toBe(true)
      if (prop.kind === 'mine.warningMarker') expect(nearby(5, ['spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'gas', 'lava', 'pit'])).toBe(true)
      if (prop.kind === 'mine.skullMarker') expect(nearby(5, ['spikes', 'dart', 'fireVent', 'crumble', 'boulder', 'gas', 'lava', 'pit']) || floor.actors.some(actor => actor.hostile && Math.max(Math.abs(actor.x - prop.x), Math.abs(actor.y - prop.y)) <= 5)).toBe(true)
    }
  })
})
