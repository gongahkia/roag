import { describe, expect, it } from 'vitest'
import { CONTENT, validateContent } from './content'
import { descend } from './engine/inventory'
import { newRun } from './engine/run'
import { migrateRunRecord } from './storage'
import type { Biome, Hero } from './types'
import { generateFloor, validateGeneration } from './world'

const biomes: readonly Biome[] = ['mine', 'wilds', 'caverns', 'ruins']
const floorFingerprint = (seed: number, index: number) => {
  const floor = generateFloor(seed, index)
  return { tiles: floor.tiles.map(tile => tile.kind), actors: floor.actors.map(actor => `${actor.id}:${actor.kind}:${actor.x},${actor.y}`), props: floor.props, puzzleIds: floor.puzzleIds }
}

describe('release validation suite', () => {
  it('replays a complete four-area campaign smoke path', () => {
    let hero: Hero | undefined
    for (const biome of biomes) for (let areaFloor = 0; areaFloor < 4; areaFloor++) {
      const state = newRun(77123, biome, areaFloor, hero)
      state.floor.objective.status = 'complete'
      state.floor.guardianDefeated = true
      state.hero.x = state.floor.exit.x
      state.hero.y = state.floor.exit.y
      const events = descend(state)
      expect(events.some(event => event.type === (areaFloor === 3 ? 'areaComplete' : 'floor'))).toBe(true)
      hero = structuredClone(state.hero)
    }
  })

  it('keeps seeds deterministic, generated floors valid, and content valid within the smoke budget', () => {
    const started = performance.now()
    expect(() => validateContent(CONTENT)).not.toThrow()
    for (const seed of [7, 42, 999]) for (let floor = 0; floor < 16; floor++) {
      expect(floorFingerprint(seed, floor)).toEqual(floorFingerprint(seed, floor))
      expect(validateGeneration(generateFloor(seed, floor))).toEqual({ valid: true, errors: [] })
    }
    expect(performance.now() - started).toBeLessThan(10_000)
  }, 15_000)

  it('migrates pre-prop saves before replay consumers inspect them', () => {
    const legacy = structuredClone(newRun(77124)) as unknown as { version: number; floor: Record<string, unknown> }
    legacy.version = 2
    delete legacy.floor.props
    expect(migrateRunRecord(legacy)).toMatchObject({ version: 3, floor: { props: [] } })
  })
})
