import { describe, expect, it } from 'vitest'
import { advance } from './engine/combat'
import { fieldReadout } from './engine/readout'
import { createEnemy, createRun } from './test/factories'
import { ECOLOGY_EVENT_KINDS, type EcologyEvent, type Biome } from './types'
import { generateAreaFloor, getTile, validateGeneration } from './world'
import { ecologyEventFor, ecologyProfileFor } from './ecology'
import { createFloor } from './test/factories'

const collapse = (): EcologyEvent => ({ id: 'ecology:test:collapse', kind: 'collapse', source: 'brace', target: { x: 3, y: 1 }, warning: 'The support beams groan; leave the marked shelf.', startsAt: 2, duration: 2, responses: ['step off unstable ground'], cleanup: 'The dust settles and the shelf holds.', state: 'waiting', original: 'floor', effect: 'crumble' })

describe('deterministic ecology', () => {
  it('maps active ecology to biome recipes and escalates duration by area floor', () => {
    const biomes: Biome[] = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary']
    const activeKinds = new Set(biomes.map(biome => ecologyProfileFor(biome).kind))
    for (const kind of ECOLOGY_EVENT_KINDS.filter(kind => kind !== 'fire')) expect(activeKinds.has(kind)).toBe(true)
    expect(ecologyProfileFor('caverns').kind).toBe('tide')
    const early = ecologyEventFor(createFloor({ index: 0, seed: 7 }), { x: 3, y: 1 }, 'source')
    const late = ecologyEventFor(createFloor({ index: 2, seed: 7 }), { x: 3, y: 1 }, 'source')
    expect(late).toMatchObject({ startsAt: early.startsAt + 2, duration: early.duration + 2 })
  })

  it('warns, changes terrain, coordinates a pursuer, and cleans up on scheduled turns', () => {
    const state = createRun()
    const source = createEnemy({ id: 'brace', name: 'Brace Guard', x: 4, y: 1, speed: 0, combatRole: 'pursuer' })
    state.floor.actors = [source]
    state.floor.ecology = [collapse()]
    advance(state, [])
    expect(fieldReadout(state).lines).toContain('ECOLOGY: COLLAPSE T-1: The support beams groan; leave the marked shelf.')
    advance(state, [])
    expect(getTile(state.floor, 3, 1)?.kind).toBe('crumble')
    expect(state.floor.ecology?.[0]).toMatchObject({ state: 'active', warned: true })
    expect(source.conditions).toContainEqual({ kind: 'shielded', duration: 1, potency: 1 })
    advance(state, [])
    advance(state, [])
    expect(getTile(state.floor, 3, 1)?.kind).toBe('floor')
    expect(state.floor.ecology?.[0].state).toBe('resolved')
  })

  it('generates reproducible, solvable Mine collapse contracts', () => {
    const first = generateAreaFloor(73, 'mine', 0, 1)
    const second = generateAreaFloor(73, 'mine', 0, 1)
    expect(first.ecology).toEqual(second.ecology)
    expect(first.ecology?.[0]).toMatchObject({ kind: 'collapse', state: 'waiting', responses: expect.any(Array) })
    expect(validateGeneration(first)).toEqual({ valid: true, errors: [] })
  }, 30_000)
})
