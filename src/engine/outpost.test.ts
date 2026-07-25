import { describe, expect, it } from 'vitest'
import { moveOutpost, outpostInteraction, outpostMap, outpostSpawn, outpostTileAt } from './outpost'

describe('village outpost', () => {
  it('spawns a courier on a walkable gate path', () => {
    expect(outpostTileAt(outpostSpawn())).toBe('path')
  })

  it('moves through paths and blocks water and building foundations', () => {
    expect(moveOutpost({ x: 24, y: 29 }, 'n')).toEqual({ position: { x: 24, y: 28 }, moved: true })
    expect(moveOutpost({ x: 5, y: 9 }, 's').moved).toBe(false)
    expect(moveOutpost({ x: 23, y: 20 }, 's').moved).toBe(true)
  })

  it('exposes every physical destination by adjacency', () => {
    expect(outpostMap.interactables.map(interactable => interactable.destination)).toEqual(['routes', 'shop', 'outfitter', 'roster'])
    expect(outpostInteraction({ x: 23, y: 10 })?.destination).toBe('routes')
    expect(outpostInteraction({ x: 22, y: 6 })?.destination).toBe('routes')
    expect(outpostInteraction({ x: 8, y: 17 })?.destination).toBe('shop')
    expect(outpostInteraction({ x: 38, y: 17 })?.destination).toBe('outfitter')
    expect(outpostInteraction({ x: 24, y: 20 })?.destination).toBe('roster')
  })

  it('keeps the gate-to-board route traversable through the lodge entrance', () => {
    let position = outpostSpawn()
    for (let step = 0; step < 19; step++) position = moveOutpost(position, 'n').position
    expect(outpostInteraction(position)?.destination).toBe('routes')
  })
})
