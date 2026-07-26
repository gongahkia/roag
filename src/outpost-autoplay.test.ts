import { describe, expect, it } from 'vitest'
import { moveOutpost, outpostInteraction, outpostSpawn } from './engine'
import { outpostAutoplayCommand } from './outpost-autoplay'
import type { Direction } from './types'

const directions: Record<string, Direction> = { i: 'nw', o: 'n', p: 'ne', k: 'w', ';': 'e', ',': 'sw', '.': 's', '/': 'se' }

describe('outpostAutoplayCommand', () => {
  it('finds the route board from the village spawn', () => {
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
    throw new Error('route board was not reached')
  })

  it('interacts only when the route board is nearby', () => {
    expect(outpostAutoplayCommand({ x: 23, y: 8 })).toBe('c')
  })
})
