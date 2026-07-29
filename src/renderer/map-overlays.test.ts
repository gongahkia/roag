import { describe, expect, it } from 'vitest'
import { mapCellIndex, mapOverlays } from './map-overlays'

describe('map overlays', () => {
  it('indexes render overlays once while preserving draw precedence', () => {
    const floor = {
      items: [{ id: 'tonic', x: 2, y: 3, count: 1 }, { id: 'rock', x: 2, y: 3, count: 1 }],
      props: [{ id: 'destroyed', kind: 'mine.oreVein' as const, x: 4, y: 3, biome: 'mine' as const, state: 'destroyed' as const, tags: [] }, { id: 'active', kind: 'mine.oreVein' as const, x: 4, y: 3, biome: 'mine' as const, state: 'dormant' as const, tags: [] }],
      actors: [{ id: 'dead', role: 'monster' as const, kind: 'rat', name: 'dead rat', x: 6, y: 3, health: 0, maxHealth: 1, attack: 1, defense: 0, speed: 1, energy: 0, glyph: 'r', color: '#fff', hostile: true }, { id: 'alive', role: 'monster' as const, kind: 'rat', name: 'rat', x: 6, y: 3, health: 1, maxHealth: 1, attack: 1, defense: 0, speed: 1, energy: 0, glyph: 'r', color: '#fff', hostile: true }],
      telegraphs: [{ id: 'first', sourceId: 'rat', actionId: 'spit', cells: [{ x: 8, y: 3 }], danger: 'minor' as const, resolveTurn: 2 }, { id: 'second', sourceId: 'rat', actionId: 'spit', cells: [{ x: 8, y: 3 }], danger: 'major' as const, resolveTurn: 2 }]
    }
    const overlays = mapOverlays(floor, { path: [{ x: 10, y: 3 }], cells: [{ x: 11, y: 3 }] })
    expect(overlays.items[mapCellIndex(2, 3)]?.id).toBe('tonic')
    expect(overlays.props[mapCellIndex(4, 3)]?.id).toBe('active')
    expect(overlays.actors[mapCellIndex(6, 3)]?.id).toBe('alive')
    expect(overlays.telegraphs[mapCellIndex(8, 3)]?.id).toBe('first')
    expect(overlays.previewPath[mapCellIndex(10, 3)]).toBe(1)
    expect(overlays.previewCells[mapCellIndex(11, 3)]).toBe(1)
  })
})
