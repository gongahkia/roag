import { describe, expect, it } from 'vitest'
import { BlockType, WORLD_HEIGHT, WORLD_SIZE } from './types'
import { generateWorld } from './world'

describe('BlockWorld', () => {
  it('generates the same terrain for the same seed', () => {
    const first = generateWorld(42)
    const second = generateWorld(42)
    for (let z = 0; z < WORLD_SIZE; z++) for (let x = 0; x < WORLD_SIZE; x++) {
      expect(first.highestSolid(x, z)).toBe(second.highestSolid(x, z))
    }
  })

  it('keeps valid edits in saves and restores them', () => {
    const world = generateWorld(12)
    expect(world.set(3, WORLD_HEIGHT - 1, 4, BlockType.Brick)).toBe(true)
    const restored = generateWorld(12)
    expect(restored.applySave(world.toSave())).toBe(true)
    expect(restored.get(3, WORLD_HEIGHT - 1, 4)).toBe(BlockType.Brick)
  })

  it('rejects out-of-bounds edits', () => {
    const world = generateWorld(1)
    expect(world.set(-1, 0, 0, BlockType.Brick)).toBe(false)
    expect(world.set(0, WORLD_HEIGHT, 0, BlockType.Brick)).toBe(false)
  })
})
