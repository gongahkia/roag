import { getTile, hasPassableTerrainPath } from './world'
import type { Floor, Point, PropId, TileKind } from './types'

export type TerrainInteractionTool = 'stoneAdze' | 'antlerPrybar' | 'resinFireBasket' | 'woodenLever'
export type TerrainInteractionTarget = TileKind | 'movable-prop'
export interface TerrainInteractionContract { tool: TerrainInteractionTool; target: TerrainInteractionTarget; requiredPosition: 'adjacent'; cooldown: number; result?: TileKind; propKinds?: PropId[]; hazard: 'dust' | 'none' | 'embers'; event: string }
export interface TerrainInteractionResult { applied: boolean; reason: string; floor: Floor; event?: string }

export const TERRAIN_INTERACTION_CONTRACTS: readonly TerrainInteractionContract[] = [
  { tool: 'stoneAdze', target: 'boulder', requiredPosition: 'adjacent', cooldown: 3, result: 'rubble', hazard: 'dust', event: 'terrain-chip' },
  { tool: 'stoneAdze', target: 'breakwall', requiredPosition: 'adjacent', cooldown: 3, result: 'floor', hazard: 'dust', event: 'terrain-breach' },
  { tool: 'stoneAdze', target: 'rubble', requiredPosition: 'adjacent', cooldown: 2, result: 'floor', hazard: 'dust', event: 'terrain-clear' },
  { tool: 'resinFireBasket', target: 'bramble', requiredPosition: 'adjacent', cooldown: 3, result: 'floor', hazard: 'embers', event: 'terrain-burn' },
  { tool: 'resinFireBasket', target: 'web', requiredPosition: 'adjacent', cooldown: 2, result: 'floor', hazard: 'embers', event: 'terrain-burn' },
  { tool: 'antlerPrybar', target: 'movable-prop', requiredPosition: 'adjacent', cooldown: 2, propKinds: ['mine.brokenCart', 'caverns.brokenBoat', 'ruins.collapsedArch'], hazard: 'none', event: 'prop-shift' },
  { tool: 'woodenLever', target: 'movable-prop', requiredPosition: 'adjacent', cooldown: 2, propKinds: ['mine.brokenCart', 'caverns.brokenBoat', 'ruins.collapsedArch'], hazard: 'none', event: 'prop-shift' }
]

const adjacent = (left: Point, right: Point): boolean => Math.abs(left.x - right.x) + Math.abs(left.y - right.y) === 1
const protectedTile = (floor: Floor, point: Point): boolean => (point.x === floor.start.x && point.y === floor.start.y) || (point.x === floor.exit.x && point.y === floor.exit.y) || floor.actors.some(actor => actor.health > 0 && actor.x === point.x && actor.y === point.y) || floor.milestones.some(milestone => !milestone.claimed && milestone.x === point.x && milestone.y === point.y)
const failure = (floor: Floor, reason: string): TerrainInteractionResult => ({ applied: false, reason, floor })

export const applyTerrainInteraction = (floor: Floor, position: Point, target: Point, tool: TerrainInteractionTool): TerrainInteractionResult => {
  const current = structuredClone(floor)
  if (!adjacent(position, target)) return failure(current, 'target is not adjacent')
  if (protectedTile(current, target)) return failure(current, 'target is protected')
  const tile = getTile(current, target.x, target.y)
  if (!tile) return failure(current, 'target is out of bounds')
  const prop = current.props.find(candidate => candidate.x === target.x && candidate.y === target.y && candidate.state !== 'destroyed')
  const contract = TERRAIN_INTERACTION_CONTRACTS.find(candidate => candidate.tool === tool && (candidate.target === tile.kind || candidate.target === 'movable-prop' && Boolean(prop && candidate.propKinds?.includes(prop.kind))))
  if (!contract) return failure(current, 'unsupported terrain interaction')
  if (contract.target === 'movable-prop') prop!.state = 'activated'
  else tile.kind = contract.result!
  if (!hasPassableTerrainPath(current, current.start, current.exit)) return failure(floor, 'mutation would remove campaign route')
  return { applied: true, reason: 'applied', floor: current, event: contract.event }
}
