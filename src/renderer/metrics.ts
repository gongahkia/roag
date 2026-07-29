import { MAP_HEIGHT, MAP_WIDTH, type Point } from '../types'

export const CELL_WIDTH = 10
export const CELL_HEIGHT = 12
export { MAP_HEIGHT, MAP_WIDTH }

export interface CellRect { x: number; y: number; width: number; height: number }
export interface CameraFrame { focus: Point; left: number; top: number; right: number; bottom: number }

export const cellRect = (x: number, y: number): CellRect => ({ x: x * CELL_WIDTH, y: y * CELL_HEIGHT, width: CELL_WIDTH, height: CELL_HEIGHT })
export const cameraFrame = (floor: { width: number; height: number }, camera: Point, zoom = 1): CameraFrame => {
  if (!Number.isFinite(zoom) || zoom <= 0) throw new Error(`invalid board zoom: ${zoom}`)
  const visibleWidth = MAP_WIDTH / zoom
  const visibleHeight = MAP_HEIGHT / zoom
  const focus = (value: number, size: number, visible: number): number => size <= visible ? (size - 1) / 2 : Math.max((visible - 1) / 2, Math.min(size - 1 - (visible - 1) / 2, value))
  const x = focus(camera.x, floor.width, visibleWidth)
  const y = focus(camera.y, floor.height, visibleHeight)
  return { focus: { x, y }, left: x + .5 - visibleWidth / 2, top: y + .5 - visibleHeight / 2, right: x + .5 + visibleWidth / 2, bottom: y + .5 + visibleHeight / 2 }
}
