import { DIRECTIONS, MAP_HEIGHT, MAP_WIDTH, type Direction, type Point } from '../types'
import type { ActionShape } from './actions'

export interface GridBounds { width: number; height: number }

const defaultBounds: GridBounds = { width: MAP_WIDTH, height: MAP_HEIGHT }
const key = (point: Point): string => `${point.x},${point.y}`
const inBounds = (point: Point, bounds: GridBounds): boolean => point.x >= 0 && point.x < bounds.width && point.y >= 0 && point.y < bounds.height

export const actionCells = (shape: ActionShape, origin: Point, direction: Exclude<Direction, 'wait'>, range: number, bounds: GridBounds = defaultBounds): Point[] => {
  const vector = DIRECTIONS[direction]
  const cells: Point[] = []
  const seen = new Set<string>()
  const add = (point: Point): void => {
    if (!inBounds(point, bounds) || seen.has(key(point))) return
    seen.add(key(point))
    cells.push(point)
  }
  const atDistance = (distance: number): Point => ({ x: origin.x + vector.x * distance, y: origin.y + vector.y * distance })
  if (shape === 'adjacent') add(atDistance(1))
  if (shape === 'line') for (let distance = 1; distance <= range; distance++) add(atDistance(distance))
  if (shape === 'cone') {
    const perpendicular = { x: -vector.y, y: vector.x }
    for (let distance = 1; distance <= range; distance++) for (let offset = -(distance - 1); offset <= distance - 1; offset++) add({ x: origin.x + vector.x * distance + perpendicular.x * offset, y: origin.y + vector.y * distance + perpendicular.y * offset })
  }
  if (shape === 'burst') {
    const center = atDistance(range)
    for (let y = -1; y <= 1; y++) for (let x = -1; x <= 1; x++) add({ x: center.x + x, y: center.y + y })
  }
  if (shape === 'cross') {
    const center = atDistance(range)
    add(center)
    for (const delta of [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }]) add({ x: center.x + delta.x, y: center.y + delta.y })
  }
  return cells
}
