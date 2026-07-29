import type { Floor, Point, TileKind } from '../types'
import { actorAt, getTile } from '../world'
import { linePropBlocker, propAt } from '../props'

export type EffectBlocker = 'wall' | 'door' | 'lockedDoor' | 'boulder' | 'rubble' | 'bramble' | 'crate' | 'chest' | 'cart' | 'crystal' | 'cover' | 'actor' | 'bounds'
type TileEffectBlocker = Exclude<EffectBlocker, 'actor' | 'bounds' | 'cart' | 'crystal' | 'cover'>
export type TerrainModifier = 'dampened' | 'obscured' | 'amplified'
export interface EffectModifier { point: Point; modifier: TerrainModifier }
export interface LineEffect { cells: Point[]; modifiers: EffectModifier[]; blocked?: { point: Point; by: EffectBlocker } }

const isEffectBlocker = (kind: TileKind): kind is TileEffectBlocker => ['wall', 'door', 'lockedDoor', 'boulder', 'rubble', 'bramble', 'crate', 'chest'].some(blocker => blocker === kind)
const terrainModifier = (kind: TileKind): TerrainModifier | undefined => kind === 'water' ? 'dampened' : kind === 'gas' ? 'obscured' : kind === 'lava' || kind === 'fireVent' ? 'amplified' : undefined
const samePoint = (first: Point, second: Point): boolean => first.x === second.x && first.y === second.y

export const lineCells = (from: Point, to: Point): Point[] => {
  const cells: Point[] = []
  let x = from.x
  let y = from.y
  const dx = Math.abs(to.x - from.x)
  const dy = -Math.abs(to.y - from.y)
  const sx = from.x < to.x ? 1 : -1
  const sy = from.y < to.y ? 1 : -1
  let error = dx + dy
  while (!(x === to.x && y === to.y)) {
    const twice = error * 2
    if (twice >= dy) { error += dy; x += sx }
    if (twice <= dx) { error += dx; y += sy }
    cells.push({ x, y })
  }
  return cells
}

export const resolveLineEffect = (floor: Floor, from: Point, to: Point): LineEffect => {
  const effect: LineEffect = { cells: [], modifiers: [] }
  for (const point of lineCells(from, to)) {
    const tile = getTile(floor, point.x, point.y)
    if (!tile) return { ...effect, blocked: { point, by: 'bounds' } }
    if (isEffectBlocker(tile.kind)) return { ...effect, blocked: { point, by: tile.kind } }
    const prop = propAt(floor.props, point.x, point.y)
    const propBlocker = linePropBlocker(prop)
    if (propBlocker) return { ...effect, blocked: { point, by: propBlocker } }
    effect.cells.push(point)
    const modifier = terrainModifier(tile.kind)
    if (modifier) effect.modifiers.push({ point, modifier })
    if (actorAt(floor, point.x, point.y)) return { ...effect, blocked: { point, by: 'actor' } }
  }
  return effect
}

export const canAffect = (floor: Floor, from: Point, to: Point): boolean => resolveLineEffect(floor, from, to).cells.some(point => samePoint(point, to))
