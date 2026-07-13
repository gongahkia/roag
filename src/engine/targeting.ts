import { DIRECTIONS, MAP_HEIGHT, MAP_WIDTH, type Modal, type RunState } from '../types'
import { actionCells } from './geometry'
import { resolveLineEffect } from './line-effect'

export interface TargetPreview { path: { x: number; y: number }[]; cells: { x: number; y: number }[] }

export const targetPreview = (state: RunState, modal: Extract<Modal, { kind: 'target' }>): TargetPreview => {
  if (!modal.direction) return { path: [], cells: [] }
  const delta = DIRECTIONS[modal.direction]
  const origin = state.hero
  const bounds = { width: MAP_WIDTH, height: MAP_HEIGHT }
  if (modal.action === 'throw') {
    const path = resolveLineEffect(state.floor, origin, { x: origin.x + delta.x * 5, y: origin.y + delta.y * 5 }).cells
    return { path, cells: path.length ? [path[path.length - 1]] : [] }
  }
  if (modal.action === 'bomb') return { path: actionCells('line', origin, modal.direction, 1, bounds), cells: actionCells('burst', origin, modal.direction, 1, bounds) }
  return { path: actionCells('line', origin, modal.direction, 1, bounds), cells: actionCells('adjacent', origin, modal.direction, 1, bounds) }
}
