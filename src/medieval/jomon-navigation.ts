import { deriveJomonDeckPlan, type JomonDeckPlan, type JomonDeckPlanAreaId } from './jomon-deck-plan'
import type { FoundationWorld } from './types'

/** Pure movement geometry derived from the deck-plan authority; it owns no world state. */
export const JOMON_DECK_NAVIGATION_CONTRACT_VERSION = 1 as const

export const JOMON_DECK_MOVEMENT_DIRECTIONS = [
  'north-west', 'north', 'north-east', 'west', 'east', 'south-west', 'south', 'south-east'
] as const
export type JomonDeckMovementDirection = typeof JOMON_DECK_MOVEMENT_DIRECTIONS[number]

export interface JomonDeckCoordinate {
  column: number
  row: number
}

export type JomonDeckCollision = 'out-of-bounds' | 'hull-boundary' | 'non-walkable' | 'access-boundary' | 'diagonal-corner'

export type JomonDeckStepAssessment =
  | { status: 'moved'; from: JomonDeckCoordinate; to: JomonDeckCoordinate; direction: JomonDeckMovementDirection }
  | { status: 'blocked'; from: JomonDeckCoordinate; direction: JomonDeckMovementDirection; collision: JomonDeckCollision }

const compare = (left: string, right: string): number => left === right ? 0 : left < right ? -1 : 1
const coordinateKey = (coordinate: JomonDeckCoordinate): string => `${coordinate.column}:${coordinate.row}`
const sameCoordinate = (left: JomonDeckCoordinate, right: JomonDeckCoordinate): boolean => left.column === right.column && left.row === right.row
const offsetFor = (direction: JomonDeckMovementDirection): JomonDeckCoordinate => ({
  'north-west': { column: -1, row: -1 }, north: { column: 0, row: -1 }, 'north-east': { column: 1, row: -1 }, west: { column: -1, row: 0 }, east: { column: 1, row: 0 }, 'south-west': { column: -1, row: 1 }, south: { column: 0, row: 1 }, 'south-east': { column: 1, row: 1 }
}[direction])

const areaAt = (plan: JomonDeckPlan, coordinate: JomonDeckCoordinate) => plan.areas.find(area => area.footprint.some(cell => sameCoordinate(cell.coordinate, coordinate)))
const hullAt = (plan: JomonDeckPlan, coordinate: JomonDeckCoordinate): boolean => plan.structuralCells.some(cell => sameCoordinate(cell.coordinate, coordinate))
const inBounds = (plan: JomonDeckPlan, coordinate: JomonDeckCoordinate): boolean => coordinate.column >= 0 && coordinate.row >= 0 && coordinate.column < plan.bounds.width && coordinate.row < plan.bounds.height
const areasConnected = (plan: JomonDeckPlan, from: JomonDeckPlanAreaId, to: JomonDeckPlanAreaId): boolean => from === to || plan.accessEdges.some(edge => (edge.fromAreaId === from && edge.toAreaId === to) || (edge.fromAreaId === to && edge.toAreaId === from))

const cardinalStep = (plan: JomonDeckPlan, from: JomonDeckCoordinate, to: JomonDeckCoordinate): JomonDeckCollision | undefined => {
  if (!inBounds(plan, to)) return 'out-of-bounds'
  if (hullAt(plan, to)) return 'hull-boundary'
  const fromArea = areaAt(plan, from)
  const toArea = areaAt(plan, to)
  if (!fromArea || !toArea) return 'non-walkable'
  return areasConnected(plan, fromArea.id, toArea.id) ? undefined : 'access-boundary'
}

/** The tavern anchor is the sole canonical zero-time initial courier spawn. */
export const canonicalJomonDeckSpawn = (world: FoundationWorld): JomonDeckCoordinate => {
  const plan = deriveJomonDeckPlan(world)
  const tavern = plan.areas.find(area => area.id === 'tavern')
  if (!tavern) throw new Error('validated Jomon deck plan has no tavern spawn')
  return structuredClone(tavern.anchor)
}

/** Resolves one orthogonal/diagonal local step without mutating a world or advancing time. */
export const assessJomonDeckStep = (world: FoundationWorld, from: JomonDeckCoordinate, direction: JomonDeckMovementDirection): JomonDeckStepAssessment => {
  const plan = deriveJomonDeckPlan(world)
  if (!areaAt(plan, from)) throw new Error('courier deck coordinate is not a walkable plan cell')
  const offset = offsetFor(direction)
  const to = { column: from.column + offset.column, row: from.row + offset.row }
  if (offset.column === 0 || offset.row === 0) {
    const collision = cardinalStep(plan, from, to)
    return collision === undefined ? { status: 'moved', from: structuredClone(from), to, direction } : { status: 'blocked', from: structuredClone(from), direction, collision }
  }
  const horizontal = { column: from.column + offset.column, row: from.row }
  const vertical = { column: from.column, row: from.row + offset.row }
  const horizontalCollision = cardinalStep(plan, from, horizontal) ?? cardinalStep(plan, horizontal, to)
  const verticalCollision = cardinalStep(plan, from, vertical) ?? cardinalStep(plan, vertical, to)
  if (horizontalCollision !== undefined || verticalCollision !== undefined) return { status: 'blocked', from: structuredClone(from), direction, collision: 'diagonal-corner' }
  return { status: 'moved', from: structuredClone(from), to, direction }
}

/** Canonical stable description for diagnostics/tests, never a world fact. */
export const jomonDeckCoordinateId = (coordinate: JomonDeckCoordinate): string => `deck-c${coordinate.column}-r${coordinate.row}`

export const isJomonDeckMovementDirection = (value: unknown): value is JomonDeckMovementDirection => typeof value === 'string' && JOMON_DECK_MOVEMENT_DIRECTIONS.includes(value as JomonDeckMovementDirection)

/** Ordinal-only deterministic order for any future local-navigation reports. */
export const canonicalJomonDeckCoordinateIds = (coordinates: readonly JomonDeckCoordinate[]): readonly string[] => coordinates.map(jomonDeckCoordinateId).sort(compare)
