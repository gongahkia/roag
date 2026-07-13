import { describe, expect, it } from 'vitest'
import { resolveTerrainReactions, terrainTags } from './terrain'
import { createFloor } from '../test/factories'

describe('terrain reactions', () => {
  it('exposes fire, gas, water, rubble, pit, and volatile tags', () => {
    expect(terrainTags('fireVent')).toContain('fire')
    expect(terrainTags('gas')).toContain('gas')
    expect(terrainTags('water')).toContain('water')
    expect(terrainTags('boulder')).toContain('rubble')
    expect(terrainTags('pit')).toContain('pit')
    expect(terrainTags('crate')).toContain('volatile')
  })

  it('resolves fire, bomb, water, rubble, pit, gas, and volatile interactions', () => {
    const floor = createFloor()
    const set = (x: number, kind: typeof floor.tiles[number]['kind']) => { floor.tiles[1 * 48 + x].kind = kind }
    set(2, 'water')
    set(3, 'gas')
    set(4, 'boulder')
    set(5, 'crate')
    set(6, 'pit')
    expect(resolveTerrainReactions(floor, [{ x: 2, y: 1 }], ['fire']).map(effect => effect.reaction)).toEqual(['steam'])
    expect(resolveTerrainReactions(floor, [{ x: 3, y: 1 }, { x: 4, y: 1 }, { x: 5, y: 1 }], ['bomb']).map(effect => effect.reaction)).toEqual(['ignited-gas', 'cleared-rubble', 'detonated-volatile'])
    expect(resolveTerrainReactions(floor, [{ x: 6, y: 1 }], ['water']).map(effect => effect.reaction)).toEqual(['flooded-pit'])
    expect(floor.tiles.slice(1 * 48 + 2, 1 * 48 + 7).map(tile => tile.kind)).toEqual(['floor', 'floor', 'floor', 'floor', 'water'])
  })
})
