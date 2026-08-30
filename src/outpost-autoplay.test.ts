import { describe, expect, it } from 'vitest'
import { moveOutpost, outpostInteraction, outpostSpawn } from './engine'
import { outpostAutoplayCommand } from './outpost-autoplay'
import type { Direction } from './types'

const directions: Record<string, Direction> = { i: 'nw', o: 'n', p: 'ne', k: 'w', ';': 'e', ',': 'sw', '.': 's', '/': 'se' }

describe('outpostAutoplayCommand', () => {
  it('finds the bridge flight console from the docking airlock', () => {
    let position = outpostSpawn()
    for (let step = 0; step < 100; step++) {
      const command = outpostAutoplayCommand(position)
      expect(command).toBeDefined()
      if (command === 'c') {
        expect(outpostInteraction(position)?.destination).toBe('routes')
        return
      }
      position = moveOutpost(position, directions[command!]).position
    }
    throw new Error('bridge flight console was not reached')
  })

  it('interacts only when the bridge flight console is nearby', () => {
    expect(outpostAutoplayCommand({ x: 8, y: 15 })).toBe('c')
  })
})
