import { MAP_HEIGHT, MAP_WIDTH, type Direction, type Point } from '../types'

export type OutpostTile = 'grass' | 'path' | 'cobble' | 'water' | 'bridge' | 'fence'
export type OutpostDestination = 'routes' | 'roster' | 'shop' | 'outfitter'
export interface OutpostDecoration { tile: number; x: number; y: number; scale?: number }
export interface OutpostInteractable { destination: OutpostDestination; name: string; point: Point; radius?: number; label?: string; labelPoint?: Point }
export interface OutpostMap { width: number; height: number; tiles: OutpostTile[]; blocked: Set<number>; decorations: OutpostDecoration[]; interactables: OutpostInteractable[]; spawn: Point }
export interface OutpostMove { position: Point; moved: boolean }

const at = (x: number, y: number): number => y * MAP_WIDTH + x
const inBounds = (x: number, y: number): boolean => x >= 0 && y >= 0 && x < MAP_WIDTH && y < MAP_HEIGHT
const deltas: Record<Direction, Point> = { nw: { x: -1, y: -1 }, n: { x: 0, y: -1 }, ne: { x: 1, y: -1 }, w: { x: -1, y: 0 }, wait: { x: 0, y: 0 }, e: { x: 1, y: 0 }, sw: { x: -1, y: 1 }, s: { x: 0, y: 1 }, se: { x: 1, y: 1 } }

const buildOutpost = (): OutpostMap => {
  const tiles = Array<OutpostTile>(MAP_WIDTH * MAP_HEIGHT).fill('grass')
  const blocked = new Set<number>()
  const decorations: OutpostDecoration[] = []
  const set = (x: number, y: number, tile: OutpostTile, blocks = false): void => { if (inBounds(x, y)) { tiles[at(x, y)] = tile; if (blocks) blocked.add(at(x, y)); else blocked.delete(at(x, y)) } }
  const rect = (x: number, y: number, width: number, height: number, tile: OutpostTile, blocks = false): void => { for (let row = y; row < y + height; row++) for (let column = x; column < x + width; column++) set(column, row, tile, blocks) }
  rect(0, 0, MAP_WIDTH, 1, 'fence', true)
  rect(0, MAP_HEIGHT - 1, MAP_WIDTH, 1, 'fence', true)
  rect(0, 0, 1, MAP_HEIGHT, 'fence', true)
  rect(MAP_WIDTH - 1, 0, 1, MAP_HEIGHT, 'fence', true)
  rect(1, 3, 4, 23, 'water', true)
  rect(43, 4, 4, 20, 'water', true)
  rect(20, 1, 8, 4, 'water', true)
  rect(22, 3, 4, 2, 'bridge')
  rect(23, 4, 3, 29, 'path')
  rect(4, 16, 40, 3, 'path')
  rect(21, 21, 7, 9, 'cobble')
  rect(5, 10, 9, 6, 'cobble', true)
  rect(34, 10, 9, 6, 'cobble', true)
  rect(20, 22, 9, 6, 'cobble', true)
  rect(7, 15, 3, 2, 'path')
  rect(37, 15, 3, 2, 'path')
  rect(23, 20, 3, 10, 'path')
  rect(21, 30, 7, 4, 'path')
  rect(23, 29, 3, 5, 'path')
  for (let x = 3; x < MAP_WIDTH - 3; x += 6) decorations.push({ tile: 11, x, y: 1, scale: 3 }, { tile: 11, x, y: 26, scale: 3 })
  decorations.push(
    { tile: 8, x: 21, y: 30, scale: 4 }, { tile: 15, x: 22, y: 6, scale: 3 },
    { tile: 16, x: 5, y: 10, scale: 6 }, { tile: 17, x: 34, y: 10, scale: 6 }, { tile: 18, x: 20, y: 22, scale: 6 },
    { tile: 9, x: 18, y: 14 }, { tile: 9, x: 29, y: 14 }, { tile: 9, x: 18, y: 25 }, { tile: 9, x: 29, y: 25 },
    { tile: 10, x: 14, y: 21 }, { tile: 12, x: 31, y: 21 }, { tile: 19, x: 15, y: 8 }, { tile: 20, x: 31, y: 8 },
    { tile: 13, x: 10, y: 23 }, { tile: 13, x: 36, y: 24 }, { tile: 21, x: 11, y: 29 }, { tile: 21, x: 35, y: 29 }
  )
  return {
    width: MAP_WIDTH,
    height: MAP_HEIGHT,
    tiles,
    blocked,
    decorations,
    spawn: { x: 24, y: 29 },
    interactables: [
      { destination: 'routes', name: 'route board', point: { x: 23, y: 9 }, radius: 3, label: 'ROUTE BOARD', labelPoint: { x: 18, y: 10 } },
      { destination: 'shop', name: 'supply stall', point: { x: 8, y: 16 } },
      { destination: 'outfitter', name: 'outfitter', point: { x: 38, y: 16 } },
      { destination: 'roster', name: 'companion lodge', point: { x: 24, y: 21 } }
    ]
  }
}

export const outpostMap = buildOutpost()
export const outpostSpawn = (): Point => ({ ...outpostMap.spawn })
export const outpostTileAt = (point: Point): OutpostTile | undefined => inBounds(point.x, point.y) ? outpostMap.tiles[at(point.x, point.y)] : undefined
export const moveOutpost = (position: Point, direction: Direction): OutpostMove => {
  if (direction === 'wait') return { position: { ...position }, moved: false }
  const delta = deltas[direction]
  const target = { x: position.x + delta.x, y: position.y + delta.y }
  if (!inBounds(target.x, target.y) || outpostMap.blocked.has(at(target.x, target.y))) return { position: { ...position }, moved: false }
  return { position: target, moved: true }
}
export const outpostInteraction = (position: Point): OutpostInteractable | undefined => outpostMap.interactables.find(interactable => Math.max(Math.abs(position.x - interactable.point.x), Math.abs(position.y - interactable.point.y)) <= (interactable.radius ?? 1))
