import { describe, expect, it } from 'vitest'
import { appendPolicyFeatureHistory, encodePolicyFeatures, visiblePolicyFeatures } from './autoplay-features'
import { runAutoplay } from './autoplay-runner'
import { createEnemy, createRun } from './test/factories'
import { indexOf } from './types'

describe('autoplay policy features', () => {
  it('excludes hidden rooms, enemies, and items from visible features', () => {
    const state = createRun()
    state.floor.tiles[indexOf(10, 10)] = { kind: 'floor', explored: false, visible: false }
    state.floor.actors = [createEnemy({ id: 'hidden', x: 10, y: 10 })]
    state.floor.items = [{ id: 'hidden-item', x: 10, y: 10, count: 1 }]
    const visible = encodePolicyFeatures(state, 'visible')
    const omniscient = encodePolicyFeatures(state, 'omniscient')
    expect(visible.knownTerrain.some(tile => tile.x === 10 && tile.y === 10)).toBe(false)
    expect(visible.visibleEntities).toEqual([])
    expect(visible.visibleItems).toEqual([])
    expect(omniscient.knownTerrain).toContainEqual({ x: 10, y: 10, kind: 'floor' })
    expect(omniscient.visibleEntities).toContainEqual(expect.objectContaining({ id: 'hidden' }))
    expect(() => visiblePolicyFeatures(omniscient)).toThrow('visible policy cannot consume omniscient features')
  })

  it('keeps exactly the newest eight serialized history entries', () => {
    const history = Array.from({ length: 9 }, (_, turn) => ({ turn, command: 'l', reason: `wait:${turn}`, events: ['wait'], resourceDelta: { health: 0, focus: 0, gold: 0, bombs: 0, ropes: 0, keys: 0 } })).reduce(appendPolicyFeatureHistory, [])
    const features = encodePolicyFeatures(createRun(), 'visible', history)
    expect(features.history).toHaveLength(8)
    expect(features.history.map(entry => entry.turn)).toEqual([1, 2, 3, 4, 5, 6, 7, 8])
    expect(JSON.parse(JSON.stringify(features))).toEqual(features)
  })

  it('stores the bounded feature vector in decision traces deterministically', () => {
    const first = runAutoplay(createRun({ seed: 7 }), { mode: 'visible', policy: 'clear', turnLimit: 2, captureTrace: true }).traceDocument!
    const second = runAutoplay(createRun({ seed: 7 }), { mode: 'visible', policy: 'clear', turnLimit: 2, captureTrace: true }).traceDocument!
    expect(first.records[0]!.features).toMatchObject({ version: 1, informationMode: 'visible', history: [] })
    expect(first.records[1]!.features.history).toHaveLength(1)
    expect(JSON.stringify(first.records.map(record => record.features))).toBe(JSON.stringify(second.records.map(record => record.features)))
  })
})
