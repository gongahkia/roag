import { DIRECTIONS, type Direction, type RunState, type TraversalToolId } from '../types'
import { getTile, hasMandatoryCompletionRoute } from '../world'

export const terrainMutationTools = ['antlerPrybar', 'stoneAdze', 'resinFireBasket', 'woodenLeverRoller'] as const satisfies readonly TraversalToolId[]
export type TerrainMutationTool = typeof terrainMutationTools[number]
export type TerrainMutationReason = 'ready' | 'unbound' | 'cooldown' | 'target-unseen' | 'destination-unseen' | 'target-invalid' | 'target-occupied' | 'target-protected' | 'destination-blocked' | 'execution-failed'
export interface TerrainMutationAssessment { tool: TerrainMutationTool; target: { x: number; y: number }; affected: { x: number; y: number }[]; ready: boolean; reason: TerrainMutationReason; cooldown: number; overdrive: boolean; risk?: 'marked' | 'burning'; routeBlocked?: boolean }

export const isTerrainMutationTool = (tool: TraversalToolId): tool is TerrainMutationTool => terrainMutationTools.includes(tool as TerrainMutationTool)
const protectedPoint = (state: RunState, point: { x: number; y: number }): boolean => (point.x === state.floor.start.x && point.y === state.floor.start.y) || (point.x === state.floor.exit.x && point.y === state.floor.exit.y) || state.floor.milestones.some(milestone => !milestone.claimed && milestone.x === point.x && milestone.y === point.y)
const occupiedPoint = (state: RunState, point: { x: number; y: number }): boolean => state.floor.actors.some(actor => actor.health > 0 && actor.x === point.x && actor.y === point.y)

export const terrainMutationAssessment = (state: RunState, tool: TerrainMutationTool, direction: Exclude<Direction, 'wait'>, overdrive = false): TerrainMutationAssessment => {
  const delta = DIRECTIONS[direction]
  const target = { x: state.hero.x + delta.x, y: state.hero.y + delta.y }
  const destination = { x: state.hero.x + delta.x * 2, y: state.hero.y + delta.y * 2 }
  const cooldown = state.hero.cooldowns?.[`tool:${tool}`] ?? 0
  const base = { tool, target, affected: [] as { x: number; y: number }[], cooldown, overdrive, ...(tool === 'antlerPrybar' ? { risk: 'marked' as const } : tool === 'resinFireBasket' ? { risk: 'burning' as const } : {}) }
  if (!(state.hero.traversalTools ?? []).includes(tool)) return { ...base, ready: false, reason: 'unbound' }
  if (cooldown) return { ...base, ready: false, reason: 'cooldown' }
  const targetTile = getTile(state.floor, target.x, target.y)
  if (!targetTile?.visible) return { ...base, ready: false, reason: 'target-unseen' }
  const needsDestination = tool === 'antlerPrybar' || tool === 'woodenLeverRoller'
  const destinationTile = needsDestination ? getTile(state.floor, destination.x, destination.y) : undefined
  if (needsDestination && !destinationTile?.visible) return { ...base, affected: [target], ready: false, reason: 'destination-unseen' }
  const affected = needsDestination ? [target, destination] : [target]
  if (tool === 'antlerPrybar') {
    if (!['boulder', 'breakwall'].includes(targetTile.kind)) return { ...base, affected, ready: false, reason: 'target-invalid' }
    if (destinationTile!.kind !== 'floor' || protectedPoint(state, destination) || occupiedPoint(state, destination) || state.floor.props.some(prop => prop.state !== 'destroyed' && prop.x === destination.x && prop.y === destination.y)) return { ...base, affected, ready: false, reason: 'destination-blocked' }
    const mutated = structuredClone(state.floor)
    getTile(mutated, destination.x, destination.y)!.kind = targetTile.kind
    getTile(mutated, target.x, target.y)!.kind = 'floor'
    if (hasMandatoryCompletionRoute(state.floor) && !hasMandatoryCompletionRoute(mutated)) return { ...base, affected, ready: false, reason: 'destination-blocked', routeBlocked: true }
  }
  if (tool === 'stoneAdze' && !['crate', 'crumble'].includes(targetTile.kind)) return { ...base, affected, ready: false, reason: 'target-invalid' }
  if (tool === 'resinFireBasket') {
    if (!['bramble', 'web'].includes(targetTile.kind)) return { ...base, affected, ready: false, reason: 'target-invalid' }
    if (protectedPoint(state, target)) return { ...base, affected, ready: false, reason: 'target-protected' }
    if (occupiedPoint(state, target)) return { ...base, affected, ready: false, reason: 'target-occupied' }
  }
  if (tool === 'woodenLeverRoller') {
    const prop = state.floor.props.find(candidate => candidate.x === target.x && candidate.y === target.y && candidate.state !== 'destroyed' && ['mine.brokenCart', 'caverns.brokenBoat', 'ruins.collapsedArch'].includes(candidate.kind))
    if (!prop) return { ...base, affected, ready: false, reason: 'target-invalid' }
    if (destinationTile!.kind !== 'floor' || protectedPoint(state, destination) || occupiedPoint(state, destination) || state.floor.props.some(candidate => candidate.state !== 'destroyed' && candidate.x === destination.x && candidate.y === destination.y)) return { ...base, affected, ready: false, reason: 'destination-blocked' }
  }
  return { ...base, affected, ready: true, reason: 'ready' }
}
