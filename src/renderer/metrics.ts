import { MAP_HEIGHT, MAP_WIDTH } from '../types'

export const CELL_WIDTH = 10
export const CELL_HEIGHT = 12
export { MAP_HEIGHT, MAP_WIDTH }

export interface CellRect { x: number; y: number; width: number; height: number }

export const cellRect = (x: number, y: number): CellRect => ({ x: x * CELL_WIDTH, y: y * CELL_HEIGHT, width: CELL_WIDTH, height: CELL_HEIGHT })
