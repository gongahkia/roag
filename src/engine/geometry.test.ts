import { describe, expect, it } from 'vitest'
import { actionCells } from './geometry'

describe('action-cell geometry', () => {
  it('returns ordered line and adjacent cells', () => {
    expect(actionCells('adjacent', { x: 1, y: 1 }, 'e', 1)).toEqual([{ x: 2, y: 1 }])
    expect(actionCells('adjacent', { x: 1, y: 1 }, 'e', 2)).toEqual([{ x: 2, y: 1 }, { x: 3, y: 1 }])
    expect(actionCells('line', { x: 1, y: 1 }, 'e', 3)).toEqual([{ x: 2, y: 1 }, { x: 3, y: 1 }, { x: 4, y: 1 }])
  })

  it('creates bounded cone, burst, and cross cells', () => {
    const bounds = { width: 4, height: 4 }
    const shapes = [
      actionCells('cone', { x: 0, y: 0 }, 'se', 3, bounds),
      actionCells('burst', { x: 0, y: 0 }, 'e', 1, bounds),
      actionCells('cross', { x: 2, y: 2 }, 'e', 1, bounds)
    ]
    for (const cells of shapes) {
      expect(cells.length).toBeGreaterThan(0)
      expect(cells.every(cell => cell.x >= 0 && cell.x < bounds.width && cell.y >= 0 && cell.y < bounds.height)).toBe(true)
      expect(new Set(cells.map(cell => `${cell.x},${cell.y}`)).size).toBe(cells.length)
    }
  })
})
