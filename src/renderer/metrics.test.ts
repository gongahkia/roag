import { describe, expect, it } from 'vitest'
import { CELL_HEIGHT, CELL_WIDTH, cellRect } from './metrics'

describe('map cell metrics', () => {
  it('uses one coordinate system for terminal cells and sprites', () => {
    expect(cellRect(3, 4)).toEqual({ x: 3 * CELL_WIDTH, y: 4 * CELL_HEIGHT, width: CELL_WIDTH, height: CELL_HEIGHT })
  })
})
