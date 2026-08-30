import { MAP_HEIGHT, MAP_WIDTH, type Direction, type Point } from '../types'

export type OutpostTile = 'space' | 'deck' | 'corridor' | 'bulkhead' | 'viewport' | 'airlock' | 'hull' | 'flightConsole'
export type OutpostDestination = 'routes' | 'roster' | 'crew' | 'shop' | 'outfitter'
export interface OutpostDecoration { tile: number; x: number; y: number; scale?: number }
export interface OutpostInteractable { destination: OutpostDestination; name: string; point: Point }
export interface OutpostMap { width: number; height: number; tiles: OutpostTile[]; blocked: Set<number>; decorations: OutpostDecoration[]; interactables: OutpostInteractable[]; spawn: Point }
export interface OutpostMove { position: Point; moved: boolean }

const at = (x: number, y: number): number => y * MAP_WIDTH + x
const inBounds = (x: number, y: number): boolean => x >= 0 && y >= 0 && x < MAP_WIDTH && y < MAP_HEIGHT
const deltas: Record<Direction, Point> = { nw: { x: -1, y: -1 }, n: { x: 0, y: -1 }, ne: { x: 1, y: -1 }, w: { x: -1, y: 0 }, wait: { x: 0, y: 0 }, e: { x: 1, y: 0 }, sw: { x: -1, y: 1 }, s: { x: 0, y: 1 }, se: { x: 1, y: 1 } }

const buildOutpost = (): OutpostMap => {
  const tiles = Array<OutpostTile>(MAP_WIDTH * MAP_HEIGHT).fill('space')
  const blocked = new Set(tiles.map((_, index) => index))
  const decorations: OutpostDecoration[] = []
  const set = (x: number, y: number, tile: OutpostTile, blocks = false): void => { if (inBounds(x, y)) { tiles[at(x, y)] = tile; if (blocks) blocked.add(at(x, y)); else blocked.delete(at(x, y)) } }
  const rect = (x: number, y: number, width: number, height: number, tile: OutpostTile, blocks = false): void => { for (let row = y; row < y + height; row++) for (let column = x; column < x + width; column++) set(column, row, tile, blocks) }
  const profile: ReadonlyArray<readonly [number, number]> = [
    [23, 26], [20, 30], [18, 33], [16, 36], [14, 38], [12, 40], [10, 42], [7, 44], [4, 46], [2, 47],
    [1, 47], [1, 47], [1, 47], [1, 47], [1, 47], [1, 47], [1, 47], [1, 47], [1, 47], [2, 46],
    [3, 45], [5, 43], [8, 40], [12, 37], [16, 35], [19, 33], [21, 31], [23, 29], [25, 27]
  ]
  const ship = new Set<string>()
  profile.forEach(([left, right], index) => {
    const y = index + 2
    for (let x = left; x <= right; x++) ship.add(`${x},${y}`)
  })
  ship.forEach(value => {
    const [x, y] = value.split(',').map(Number)
    const hull = !ship.has(`${x - 1},${y}`) || !ship.has(`${x + 1},${y}`) || !ship.has(`${x},${y - 1}`) || !ship.has(`${x},${y + 1}`)
    set(x, y, hull ? 'hull' : 'deck', hull)
  })

  // the bridge, habitat, cargo elevator, engineering keel, and starboard dock share one carrier spine.
  rect(4, 16, 42, 2, 'corridor')
  rect(25, 7, 3, 19, 'corridor')
  rect(22, 11, 10, 5, 'bulkhead', true)
  rect(25, 11, 3, 5, 'corridor')
  rect(14, 11, 1, 10, 'bulkhead', true)
  rect(14, 16, 1, 2, 'corridor')
  rect(34, 11, 1, 10, 'bulkhead', true)
  rect(34, 16, 1, 2, 'corridor')
  rect(4, 12, 2, 5, 'viewport', true)
  rect(43, 14, 2, 5, 'viewport', true)
  rect(41, 19, 5, 3, 'airlock')
  rect(25, 24, 4, 2, 'airlock')
  set(7, 15, 'flightConsole')
  decorations.push(
    { tile: 8, x: 43, y: 20, scale: 4 },
    { tile: 16, x: 17, y: 19, scale: 6 }, { tile: 17, x: 30, y: 24, scale: 6 }, { tile: 18, x: 38, y: 18, scale: 6 },
    { tile: 9, x: 18, y: 16 }, { tile: 9, x: 32, y: 16 }, { tile: 9, x: 21, y: 23 }, { tile: 9, x: 35, y: 22 },
    { tile: 10, x: 22, y: 25 }, { tile: 12, x: 28, y: 9 }, { tile: 19, x: 10, y: 12 }, { tile: 20, x: 39, y: 14 },
    { tile: 13, x: 19, y: 19 }, { tile: 13, x: 36, y: 20 }, { tile: 21, x: 20, y: 8 }, { tile: 21, x: 31, y: 8 }
  )
  return {
    width: MAP_WIDTH,
    height: MAP_HEIGHT,
    tiles,
    blocked,
    decorations,
    spawn: { x: 44, y: 20 },
    interactables: [
      { destination: 'routes', name: 'bridge flight console', point: { x: 7, y: 15 } },
      { destination: 'shop', name: 'habitat supply locker', point: { x: 17, y: 19 } },
      { destination: 'outfitter', name: 'engineering gear bay', point: { x: 30, y: 24 } },
      { destination: 'roster', name: 'science crew bay', point: { x: 38, y: 18 } },
      { destination: 'crew', name: 'habitat duty terminal', point: { x: 20, y: 8 } }
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
export const outpostInteraction = (position: Point): OutpostInteractable | undefined => outpostMap.interactables.find(interactable => Math.max(Math.abs(position.x - interactable.point.x), Math.abs(position.y - interactable.point.y)) <= 1)
