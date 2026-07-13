import type { Floor, Point } from '../types'
import { resolveLineEffect, type EffectBlocker } from './line-effect'

export interface ProjectileCollision { point: Point; by: EffectBlocker | 'target' }
export interface ProjectilePath { cells: Point[]; collision?: ProjectileCollision; cover: boolean }

const samePoint = (first: Point, second: Point): boolean => first.x === second.x && first.y === second.y

export const projectBolt = (floor: Floor, from: Point, target: Point): ProjectilePath => {
  const effect = resolveLineEffect(floor, from, target)
  const reachesTarget = effect.cells.some(point => samePoint(point, target))
  if (reachesTarget) return { cells: effect.cells, collision: { point: { ...target }, by: 'target' }, cover: false }
  return { cells: effect.cells, ...(effect.blocked ? { collision: { point: effect.blocked.point, by: effect.blocked.by } } : {}), cover: Boolean(effect.blocked) }
}
