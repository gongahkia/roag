import type { CompanionActionCategory, Point, RunState, TileKind } from '../types'
import { getTile, hasMandatoryCompletionRoute } from '../world'

export const COMPANION_RESOURCE_OWNERSHIP = {
  cooldown: 'companion',
  courierFiniteResources: 'explicit-command-required',
  courierTraversalTools: 'courier',
  terrainMutations: 'companion'
} as const

const pathmakerTerrain = new Set<TileKind>(['rubble', 'bramble', 'boulder'])
const ritualistHazards = new Set<TileKind>(['spikes', 'dart', 'fireVent', 'gas', 'crumble', 'water', 'deepWater', 'lava', 'pit', 'brine', 'frostRime'])
const pointCompare = (left: Point, right: Point): number => left.y - right.y || left.x - right.x
const distanceFromHero = (state: RunState, point: Point): number => Math.max(Math.abs(state.hero.x - point.x), Math.abs(state.hero.y - point.y))
const protectedPoint = (state: RunState, point: Point): boolean => (point.x === state.floor.start.x && point.y === state.floor.start.y) || (point.x === state.floor.exit.x && point.y === state.floor.exit.y) || state.floor.milestones.some(milestone => !milestone.claimed && milestone.x === point.x && milestone.y === point.y)
const occupiedPoint = (state: RunState, point: Point): boolean => state.floor.actors.some(actor => actor.health > 0 && actor.x === point.x && actor.y === point.y) || state.floor.props.some(prop => prop.state !== 'destroyed' && prop.x === point.x && prop.y === point.y)

export type CompanionTerrainAction = Extract<CompanionActionCategory, 'stabilizeTerrain' | 'stabilizeHazard'>
export type CompanionTerrainMutationReason = 'ready' | 'target-unseen' | 'target-invalid' | 'target-protected' | 'target-occupied' | 'route-blocked'
export interface CompanionTerrainMutationAssessment { action: CompanionTerrainAction; target: Point; ready: boolean; reason: CompanionTerrainMutationReason }

const allowedTerrain = (action: CompanionTerrainAction): ReadonlySet<TileKind> => action === 'stabilizeTerrain' ? pathmakerTerrain : ritualistHazards
export const companionTerrainTargets = (state: RunState, action: CompanionTerrainAction): Point[] => state.floor.tiles.flatMap((tile, index) => tile.visible && tile.explored && allowedTerrain(action).has(tile.kind) ? [{ x: index % state.floor.width, y: Math.floor(index / state.floor.width) }] : []).sort((left, right) => distanceFromHero(state, left) - distanceFromHero(state, right) || pointCompare(left, right)).filter(target => companionTerrainMutationAssessment(state, action, target).ready)

export const companionTerrainMutationAssessment = (state: RunState, action: CompanionTerrainAction, target: Point): CompanionTerrainMutationAssessment => {
  const base = { action, target: { ...target } }
  const tile = getTile(state.floor, target.x, target.y)
  if (!tile?.visible || !tile.explored) return { ...base, ready: false, reason: 'target-unseen' }
  if (!allowedTerrain(action).has(tile.kind)) return { ...base, ready: false, reason: 'target-invalid' }
  if (protectedPoint(state, target)) return { ...base, ready: false, reason: 'target-protected' }
  if (occupiedPoint(state, target)) return { ...base, ready: false, reason: 'target-occupied' }
  const mutated = structuredClone(state.floor)
  const mutatedTile = getTile(mutated, target.x, target.y)!
  mutatedTile.kind = 'floor'
  delete mutatedTile.flow
  if (hasMandatoryCompletionRoute(state.floor) && !hasMandatoryCompletionRoute(mutated)) return { ...base, ready: false, reason: 'route-blocked' }
  return { ...base, ready: true, reason: 'ready' }
}

export const applyCompanionTerrainMutation = (state: RunState, assessment: CompanionTerrainMutationAssessment): boolean => {
  if (!assessment.ready) return false
  const tile = getTile(state.floor, assessment.target.x, assessment.target.y)
  if (!tile) return false
  tile.kind = 'floor'
  delete tile.flow
  return true
}
