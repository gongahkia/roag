import { DIRECTIONS, type Modal, type RunState } from '../types'
import { actionCells } from './geometry'
import { resolveLineEffect } from './line-effect'
import { isTerrainMutationTool, terrainMutationAssessment, type TerrainMutationAssessment } from './terrain-mutations'

export interface TargetPreview { path: { x: number; y: number }[]; cells: { x: number; y: number }[]; mutation?: TerrainMutationAssessment }

export const targetPreview = (state: RunState, modal: Extract<Modal, { kind: 'target' }>): TargetPreview => {
  if (!modal.direction) return { path: [], cells: [] }
  const delta = DIRECTIONS[modal.direction]
  const origin = state.hero
  const bounds = { width: state.floor.width, height: state.floor.height }
  if (modal.tool && isTerrainMutationTool(modal.tool)) {
    const mutation = terrainMutationAssessment(state, modal.tool, modal.direction, modal.overdrive)
    return { path: mutation.affected, cells: mutation.affected, mutation }
  }
  if (modal.action === 'drill' || modal.action === 'bridge' || modal.action === 'winch' || modal.action === 'stoneWedge' || modal.action === 'antlerPrybar' || modal.action === 'stoneAdze' || modal.action === 'resinFireBasket' || modal.action === 'woodenLeverRoller') {
    const point = { x: origin.x + delta.x, y: origin.y + delta.y }
    return { path: [point], cells: [point] }
  }
  if (modal.action === 'glide' || modal.action === 'grapple' || modal.action === 'dash' || modal.action === 'reedwing' || modal.action === 'cordAnchor' || modal.action === 'ashwayRites') {
    const length = modal.action === 'grapple' ? 3 : modal.action === 'cordAnchor' ? modal.overdrive ? 4 : 2 : modal.action === 'reedwing' ? modal.overdrive ? 3 : 2 : modal.action === 'ashwayRites' ? modal.overdrive ? 3 : 2 : 2
    const middle = { x: origin.x + delta.x, y: origin.y + delta.y }
    const landing = { x: origin.x + delta.x * length, y: origin.y + delta.y * length }
    const path = Array.from({ length }, (_, index) => ({ x: origin.x + delta.x * (index + 1), y: origin.y + delta.y * (index + 1) }))
    return { path: modal.action === 'ashwayRites' ? path : path.length ? path : [middle], cells: modal.action === 'ashwayRites' ? path : [landing] }
  }
  if (modal.action === 'throw') {
    const path = resolveLineEffect(state.floor, origin, { x: origin.x + delta.x * 5, y: origin.y + delta.y * 5 }).cells
    return { path, cells: path.length ? [path[path.length - 1]] : [] }
  }
  if (modal.action === 'bomb') return { path: actionCells('line', origin, modal.direction, 2, bounds), cells: actionCells('burst', origin, modal.direction, 2, bounds) }
  return { path: actionCells('line', origin, modal.direction, 1, bounds), cells: actionCells('adjacent', origin, modal.direction, 1, bounds) }
}
