import { describe, expect, it } from 'vitest'
import { moveOutpost, outpostInteraction, outpostMap, outpostSpawn, outpostTileAt } from './outpost'

describe('Voyager carrier deck', () => {
  it('spawns a specialist in the starboard docking airlock', () => {
    expect(outpostTileAt(outpostSpawn())).toBe('airlock')
  })

  it('keeps space, hull, and cargo bulkheads out of bounds while leaving the carrier spine walkable', () => {
    expect(moveOutpost({ x: 2, y: 16 }, 'w').moved).toBe(false)
    expect(moveOutpost({ x: 22, y: 10 }, 's').moved).toBe(false)
    expect(moveOutpost({ x: 26, y: 10 }, 's')).toEqual({ position: { x: 26, y: 11 }, moved: true })
  })

  it('exposes every physical destination by adjacency', () => {
    expect(outpostMap.interactables.map(interactable => interactable.destination)).toEqual(['routes', 'shop', 'outfitter', 'roster'])
    expect(outpostTileAt({ x: 7, y: 15 })).toBe('flightConsole')
    expect(outpostInteraction({ x: 7, y: 16 })?.destination).toBe('routes')
    expect(outpostInteraction({ x: 17, y: 18 })?.destination).toBe('shop')
    expect(outpostInteraction({ x: 30, y: 23 })?.destination).toBe('outfitter')
    expect(outpostInteraction({ x: 38, y: 17 })?.destination).toBe('roster')
  })

  it('keeps a walkable path from the docking airlock to the bridge console', () => {
    let position = outpostSpawn()
    const traverse = (direction: 'nw' | 'w', steps: number): void => {
      for (let step = 0; step < steps; step++) {
        const moved = moveOutpost(position, direction)
        expect(moved.moved).toBe(true)
        position = moved.position
      }
    }
    traverse('nw', 2)
    traverse('w', 7)
    traverse('nw', 1)
    traverse('w', 26)
    traverse('nw', 1)
    expect(outpostInteraction(position)?.destination).toBe('routes')
  })
})
