import type { Point, TileKind } from './types'
import type { RouteEdgeMode, RouteNodeKind } from './route-contract'

export interface PlacementRequirements {
  nodeKinds?: RouteNodeKind[]
  edgeModes?: RouteEdgeMode[]
  terrain?: TileKind[]
  minDistance?: number
  maxDistance?: number
  visibility?: 'visible' | 'hidden'
  cover?: boolean
  chokepoint?: boolean
  adjacentTerrain?: TileKind[]
  optional?: boolean
  routeCost?: 'safe' | 'costly'
  near?: Point
  nearDistance?: number
}

export interface PlacementContract {
  id: string
  requirements: PlacementRequirements
  fallback?: PlacementRequirements
}

export interface PlacementContext {
  points: readonly Point[]
  terrainAt: (point: Point) => TileKind | undefined
  passableAt: (point: Point) => boolean
  blockedAt: (point: Point) => boolean
  distanceFromStart: (point: Point) => number
  visibleAt: (point: Point) => boolean
  coveredAt: (point: Point) => boolean
  chokepointAt: (point: Point) => boolean
  adjacentTerrainAt: (point: Point) => readonly TileKind[]
  nodeKindsAt: (point: Point) => readonly RouteNodeKind[]
  edgeModesAt: (point: Point) => readonly RouteEdgeMode[]
}

export interface PlacementCandidate { point: Point; score: number }
export interface PlacementDebug { id: string; selected?: Point; ranked: number; usedFallback: boolean; diagnostics: string[]; requirements: PlacementRequirements }
export interface PlacementSelection { point?: Point; debug: PlacementDebug }

const distance = (left: Point, right: Point): number => Math.abs(left.x - right.x) + Math.abs(left.y - right.y)
const includes = <T>(values: readonly T[] | undefined, value: T): boolean => !values?.length || values.includes(value)
const intersects = <T>(required: readonly T[] | undefined, present: readonly T[]): boolean => !required?.length || required.some(value => present.includes(value))

const reasonsFor = (point: Point, requirements: PlacementRequirements, context: PlacementContext): string[] => {
  const reasons: string[] = []
  const terrain = context.terrainAt(point)
  const distanceFromStart = context.distanceFromStart(point)
  const modes = context.edgeModesAt(point)
  if (!context.passableAt(point)) reasons.push('impassable')
  if (context.blockedAt(point)) reasons.push('blocked')
  if (!terrain || !includes(requirements.terrain, terrain)) reasons.push('terrain')
  if (!intersects(requirements.nodeKinds, context.nodeKindsAt(point))) reasons.push('node role')
  if (!intersects(requirements.edgeModes, modes)) reasons.push('edge role')
  if (requirements.minDistance !== undefined && distanceFromStart < requirements.minDistance) reasons.push('minimum distance')
  if (requirements.maxDistance !== undefined && distanceFromStart > requirements.maxDistance) reasons.push('maximum distance')
  if (requirements.visibility !== undefined && context.visibleAt(point) !== (requirements.visibility === 'visible')) reasons.push('visibility')
  if (requirements.cover !== undefined && context.coveredAt(point) !== requirements.cover) reasons.push('cover')
  if (requirements.chokepoint !== undefined && context.chokepointAt(point) !== requirements.chokepoint) reasons.push('chokepoint')
  if (!intersects(requirements.adjacentTerrain, context.adjacentTerrainAt(point))) reasons.push('adjacency')
  if (requirements.optional !== undefined && modes.includes('optional') !== requirements.optional) reasons.push('optional route')
  if (requirements.routeCost !== undefined && !modes.includes(requirements.routeCost)) reasons.push('route cost')
  if (requirements.near && distance(point, requirements.near) > (requirements.nearDistance ?? 1)) reasons.push('setpiece distance')
  return reasons
}

const scoreFor = (point: Point, requirements: PlacementRequirements, context: PlacementContext): number => {
  const modes = context.edgeModesAt(point)
  return context.distanceFromStart(point) + (context.coveredAt(point) ? 20 : 0) + (context.chokepointAt(point) ? 15 : 0) + (modes.includes('optional') ? 30 : 0) + (modes.includes('costly') ? 20 : 0) + (requirements.near ? Math.max(0, (requirements.nearDistance ?? 1) - distance(point, requirements.near)) : 0)
}

export const rankPlacementCandidates = (requirements: PlacementRequirements, context: PlacementContext): PlacementCandidate[] => context.points
  .filter(point => !reasonsFor(point, requirements, context).length)
  .map(point => ({ point, score: scoreFor(point, requirements, context) }))
  .sort((left, right) => right.score - left.score || left.point.y - right.point.y || left.point.x - right.point.x)

const requirementSummary = (requirements: PlacementRequirements): string => Object.entries(requirements).map(([key, value]) => `${key}=${Array.isArray(value) ? value.join('|') : typeof value === 'object' ? `${value.x},${value.y}` : value}`).join(',')

export const selectPlacement = (contract: PlacementContract, context: PlacementContext): PlacementSelection => {
  const primary = rankPlacementCandidates(contract.requirements, context)
  if (primary.length) return { point: primary[0].point, debug: { id: contract.id, selected: primary[0].point, ranked: primary.length, usedFallback: false, diagnostics: [], requirements: contract.requirements } }
  const diagnostics = [`placement ${contract.id}: no candidate satisfies ${requirementSummary(contract.requirements)}`]
  if (!contract.fallback) return { debug: { id: contract.id, ranked: 0, usedFallback: false, diagnostics, requirements: contract.requirements } }
  const fallback = rankPlacementCandidates(contract.fallback, context)
  if (fallback.length) return { point: fallback[0].point, debug: { id: contract.id, selected: fallback[0].point, ranked: fallback.length, usedFallback: true, diagnostics, requirements: contract.fallback } }
  diagnostics.push(`placement ${contract.id}: declared fallback has no candidate satisfying ${requirementSummary(contract.fallback)}`)
  return { debug: { id: contract.id, ranked: 0, usedFallback: true, diagnostics, requirements: contract.fallback } }
}
