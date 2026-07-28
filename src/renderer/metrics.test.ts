import { describe, expect, it } from 'vitest'
import { CELL_HEIGHT, CELL_WIDTH, cameraFrame, cellRect } from './metrics'

describe('map cell metrics', () => {
  it('uses one coordinate system for terminal cells and sprites', () => {
    expect(cellRect(3, 4)).toEqual({ x: 3 * CELL_WIDTH, y: 4 * CELL_HEIGHT, width: CELL_WIDTH, height: CELL_HEIGHT })
  })

  it('keeps large maps framed at each edge without losing spatial direction', () => {
    expect(cameraFrame({ width: 72, height: 48 }, { x: 0, y: 0 })).toEqual({ focus: { x: 23.5, y: 17 }, left: 0, top: 0, right: 48, bottom: 35 })
    expect(cameraFrame({ width: 72, height: 48 }, { x: 71, y: 47 })).toEqual({ focus: { x: 47.5, y: 30 }, left: 24, top: 13, right: 72, bottom: 48 })
    for (const floor of [{ width: 48, height: 35 }, { width: 56, height: 44 }, { width: 64, height: 48 }, { width: 72, height: 48 }, { width: 80, height: 56 }]) {
      const frame = cameraFrame(floor, { x: floor.width - 1, y: floor.height - 1 })
      expect(frame.left).toBeGreaterThanOrEqual(0)
      expect(frame.top).toBeGreaterThanOrEqual(0)
      expect(frame.right).toBeLessThanOrEqual(floor.width)
      expect(frame.bottom).toBeLessThanOrEqual(floor.height)
    }
  })

  it('centers a fully visible map at reduced zoom', () => {
    expect(cameraFrame({ width: 72, height: 48 }, { x: 0, y: 0 }, .5)).toMatchObject({ focus: { x: 35.5, y: 23.5 }, left: -12, top: -11, right: 84, bottom: 59 })
  })
})
