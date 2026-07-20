import { describe, expect, it } from 'vitest'
import { propDefinition } from '../props'
import type { Prop, PropId } from '../types'
import { generateFloor, getTile, isPassable, validateGeneration } from '../world'
import { advance } from './combat'
import { operate, useRope } from './inventory'
import { resolveLineEffect } from './line-effect'
import { applyPropEffects } from './props'
import { refreshFov } from './visibility'
import { createRun } from '../test/factories'

const cavernProp = (kind: Extract<PropId, `caverns.${string}`>, overrides: Partial<Prop> = {}): Prop => {
  const definition = propDefinition(kind)
  return { id: `prop:test:${kind}`, kind, x: 2, y: 1, biome: 'caverns', state: 'dormant', tags: [...definition.tags], hooks: [...definition.hooks], ...overrides }
}

const cavernRun = () => {
  const state = createRun()
  state.floor.biome = 'caverns'
  return state
}

const hasNeighbor = (floor: ReturnType<typeof generateFloor>, prop: Prop, kind: string): boolean => [-1, 0, 1].some(y => [-1, 0, 1].some(x => (x || y) && getTile(floor, prop.x + x, prop.y + y)?.kind === kind))

describe('Caverns props', () => {
  it('generates deterministic props only in their water or darkness contexts', () => {
    for (const seed of [71, 187, 409, 733]) {
      const first = generateFloor(seed, 8)
      expect(first).toEqual(generateFloor(seed, 8))
      expect(validateGeneration(first)).toEqual({ valid: true, errors: [] })
      for (const prop of first.props) {
        if (prop.kind === 'caverns.crystalCluster' || prop.kind === 'caverns.glowingFungus') expect(hasNeighbor(first, prop, 'darkness')).toBe(true)
        if (prop.kind === 'caverns.barnacledShrine' || prop.kind === 'caverns.brokenBoat' || prop.kind === 'caverns.eelTunnel') expect(hasNeighbor(first, prop, 'water')).toBe(true)
      }
    }
  })

  it('blocks a crystal line until force refracts it, then mines safe rubble and shards', () => {
    const refracted = cavernRun()
    refracted.floor.props = [cavernProp('caverns.crystalCluster')]
    expect(resolveLineEffect(refracted.floor, { x: 1, y: 1 }, { x: 5, y: 1 }).blocked).toMatchObject({ by: 'crystal' })
    applyPropEffects(refracted, [{ x: 2, y: 1 }], ['force'])
    expect(resolveLineEffect(refracted.floor, { x: 1, y: 1 }, { x: 5, y: 1 }).cells).toContainEqual({ x: 5, y: 1 })

    const mined = cavernRun()
    mined.hero.equipment.mainHand = 'pickaxe'
    mined.floor.props = [cavernProp('caverns.crystalCluster')]
    operate(mined)
    operate(mined)
    expect(mined.floor.props[0].state).toBe('destroyed')
    expect(mined.floor.tiles[1 * 48 + 2].kind).toBe('rubble')
    expect(mined.floor.items).toContainEqual(expect.objectContaining({ id: 'rock', count: 2 }))
  })

  it('changes fungal light and brine-shrine water state with bounded expiry', () => {
    const fungus = cavernRun()
    fungus.floor.props = [cavernProp('caverns.glowingFungus')]
    applyPropEffects(fungus, [{ x: 2, y: 1 }], ['water'])
    expect(fungus.floor.props[0].state).toBe('activated')
    applyPropEffects(fungus, [{ x: 2, y: 1 }], ['fire'])
    expect(fungus.floor.props[0].state).toBe('destroyed')
    expect(fungus.floor.tiles[1 * 48 + 2].kind).toBe('fireVent')

    const shrine = cavernRun()
    shrine.hero.inventory = ['tonic']
    shrine.floor.props = [cavernProp('caverns.barnacledShrine')]
    operate(shrine)
    operate(shrine)
    const channels = shrine.floor.props[0].effectCells ?? []
    expect(channels).not.toHaveLength(0)
    expect(channels.every(point => shrine.floor.tiles[point.y * 48 + point.x].kind === 'water')).toBe(true)
    while (shrine.turn < 4) advance(shrine, [])
    expect(channels.every(point => shrine.floor.tiles[point.y * 48 + point.x].kind === 'floor')).toBe(true)
  })

  it('renders fungal state as a local light source beyond the unlit Caverns radius', () => {
    const state = cavernRun()
    state.floor.props = [cavernProp('caverns.glowingFungus', { x: 10, y: 1 })]
    refreshFov(state)
    expect(state.floor.tiles[1 * 48 + 12].visible).toBe(true)
    state.floor.props[0].state = 'destroyed'
    refreshFov(state)
    expect(state.floor.tiles[1 * 48 + 12].visible).toBe(false)
  })

  it('anchors a boat crossing, toggles the eel shortcut, and spends a cache key', () => {
    const boat = cavernRun()
    boat.hero.ropes = 1
    boat.floor.props = [cavernProp('caverns.brokenBoat')]
    expect(isPassable(boat.floor, 2, 1)).toBe(false)
    operate(boat)
    useRope(boat)
    expect(boat.floor.props[0].state).toBe('activated')
    expect(boat.hero.ropes).toBe(0)
    expect(isPassable(boat.floor, 2, 1)).toBe(true)

    const tunnel = cavernRun()
    tunnel.hero.bombs = 1
    tunnel.floor.props = [cavernProp('caverns.eelTunnel')]
    operate(tunnel)
    operate(tunnel)
    expect(tunnel.floor.props[0].state).toBe('activated')
    expect(isPassable(tunnel.floor, 2, 1)).toBe(false)
    applyPropEffects(tunnel, [{ x: 2, y: 1 }], ['force'])
    expect(tunnel.floor.props[0].state).toBe('dormant')
    expect(isPassable(tunnel.floor, 2, 1)).toBe(true)
    applyPropEffects(tunnel, [{ x: 2, y: 1 }], ['fire'])
    expect(tunnel.floor.actors).toContainEqual(expect.objectContaining({ kind: 'fumeEel', hostile: true }))

    const parcel = cavernRun()
    parcel.hero.keys = 1
    parcel.floor.props = [cavernProp('caverns.sealedParcel')]
    operate(parcel)
    operate(parcel)
    expect(parcel.hero.keys).toBe(0)
    expect(parcel.floor.props[0].state).toBe('activated')
    expect(parcel.floor.items).toContainEqual(expect.objectContaining({ id: 'focusTonic' }))
  })
})
