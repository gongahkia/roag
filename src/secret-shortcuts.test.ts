import { describe, expect, it } from 'vitest'
import { newRun } from './engine'
import { fieldReadout } from './engine/readout'
import { takeSecretShortcut } from './engine/shortcuts'
import { measureGeneration } from './generation-metrics'
import { getTile, hasPassablePath, validateGeneration, validateSecretRoutes } from './world'

const shortcutFixture = () => {
  const state = newRun(7, 'mine', 1)
  const route = state.floor.secretRoutes?.find(candidate => candidate.kind === 'rare-transition')
  if (!route) throw new Error('missing generated shortcut')
  const room = state.floor.secretRooms?.find(candidate => candidate.id === route.roomId)
  if (!room) throw new Error('missing shortcut room')
  room.discovery = { channel: 'terrain', turn: 0 }
  getTile(state.floor, route.entry.x, route.entry.y)!.kind = 'floor'
  state.hero.x = route.entry.x
  state.hero.y = route.entry.y
  return { state, route }
}

describe('same-biome secret shortcuts', () => {
  it('takes a generated one-way shortcut to a safe same-biome floor start', () => {
    const { state, route } = shortcutFixture()
    expect(route).toMatchObject({ destination: { biome: 'mine', floor: 3 }, direction: 'one-way', arrival: 'floor-start', returnSemantics: 'no-return' })
    expect(takeSecretShortcut(state)?.map(event => event.type)).toContain('floor')
    expect(state.floor).toMatchObject({ biome: 'mine', index: 3 })
    expect(state.hero).toMatchObject(state.floor.start)
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
  })

  it('requires discovery and an opened entry before traversal', () => {
    const { state, route } = shortcutFixture()
    const room = state.floor.secretRooms?.find(candidate => candidate.id === route.roomId)
    if (!room) throw new Error('missing shortcut room')
    room.discovery = undefined
    expect(fieldReadout(state).lines.some(line => line.startsWith('SHORTCUT:'))).toBe(false)
    expect(takeSecretShortcut(state)).toBeUndefined()
    room.discovery = { channel: 'terrain', turn: 0 }
    getTile(state.floor, route.entry.x, route.entry.y)!.kind = 'breakwall'
    expect(takeSecretShortcut(state)).toBeUndefined()
  })

  it('honors a two-way fixture with a safe return link', () => {
    const { state, route } = shortcutFixture()
    route.direction = 'two-way'
    route.returnSemantics = 'return-link'
    takeSecretShortcut(state)
    expect(state.shortcutReturn?.routeId).toBe(route.id)
    state.hero.x = state.floor.start.x
    state.hero.y = state.floor.start.y
    takeSecretShortcut(state)
    expect(state.shortcutReturn).toBeUndefined()
    expect(state.floor.index).toBe(1)
    expect(state.hero).toMatchObject(route.entry)
  })

  it('rejects cross-biome and invalid shortcut landings', () => {
    const { state, route } = shortcutFixture()
    route.destination = { biome: 'wilds', floor: 3 }
    expect(takeSecretShortcut(state)).toEqual([])
    expect(state.floor.biome).toBe('mine')
    expect(state.messages[0]).toContain('cannot leave its biome')
    expect(validateSecretRoutes(state.floor)).toEqual(expect.arrayContaining([`invalid secret transition: ${route.id}`]))
  })

  it('keeps the base route clear when the shortcut is ignored', () => {
    const { state } = shortcutFixture()
    expect(hasPassablePath(state.floor, state.floor.start, state.floor.exit)).toBe(true)
  })

  it('reports seeded shortcut direction, landing, and entry requirement', () => {
    const { state } = shortcutFixture()
    const metrics = measureGeneration({ floor: state.floor, validation: validateGeneration(state.floor) })

    expect(metrics.sideSpaces.shortcuts).toEqual([expect.objectContaining({
      report: 'one-way mine shortcut to floor 4, arrival at floor start; no return',
      entryCondition: 'sealed-breakwall'
    })])
  })
})
