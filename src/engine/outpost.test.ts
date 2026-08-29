import { describe, expect, it } from 'vitest'
import { moveOutpost, outpostInteraction, outpostMap, outpostSpawn, outpostTileAt } from './outpost'

describe('Voyager carrier deck', () => {
  it('spawns a specialist on a walkable airlock corridor', () => {
    expect(outpostTileAt(outpostSpawn())).toBe('corridor')
  })

  it('moves through corridors and blocks viewports and bulkheads', () => {
    expect(moveOutpost({ x: 24, y: 29 }, 'n')).toEqual({ position: { x: 24, y: 28 }, moved: true })
    expect(moveOutpost({ x: 5, y: 9 }, 's').moved).toBe(false)
    expect(moveOutpost({ x: 23, y: 20 }, 's').moved).toBe(true)
  })

  it('exposes every physical destination by adjacency', () => {
    expect(outpostMap.interactables.map(interactable => interactable.destination)).toEqual(['routes', 'shop', 'outfitter', 'roster'])
    expect(outpostTileAt({ x: 23, y: 9 })).toBe('flightConsole')
    expect(outpostInteraction({ x: 23, y: 10 })?.destination).toBe('routes')
    expect(outpostInteraction({ x: 23, y: 11 })).toBeUndefined()
    expect(outpostMap.decorations.some(decoration => decoration.tile === 15)).toBe(false)
    expect(outpostInteraction({ x: 8, y: 17 })?.destination).toBe('shop')
    expect(outpostInteraction({ x: 38, y: 17 })?.destination).toBe('outfitter')
    expect(outpostInteraction({ x: 24, y: 20 })?.destination).toBe('roster')
  })

  it('keeps the airlock-to-console route traversable through the crew-bay entrance', () => {
    let position = outpostSpawn()
    for (let step = 0; step < 19; step++) position = moveOutpost(position, 'n').position
    expect(outpostInteraction(position)?.destination).toBe('routes')
  })
})
