import type { Point, RunState, Telegraph, TelegraphDanger } from '../types'
import { log } from './shared'
import { recordTelegraph } from './encyclopedia'

export interface TelegraphPlan { id: string; sourceId: string; actionId: string; cells: readonly Point[]; danger: TelegraphDanger; windup: number; collision?: { point: Point; by: string }; cover?: boolean }

export const announceTelegraph = (state: RunState, plan: TelegraphPlan): Telegraph => {
  if (!Number.isInteger(plan.windup) || plan.windup < 1) throw new Error(`invalid telegraph windup: ${plan.windup}`)
  const telegraphs = state.floor.telegraphs ??= []
  if (telegraphs.some(telegraph => telegraph.id === plan.id)) throw new Error(`duplicate telegraph: ${plan.id}`)
  const telegraph: Telegraph = { id: plan.id, sourceId: plan.sourceId, actionId: plan.actionId, cells: plan.cells.map(cell => ({ ...cell })), danger: plan.danger, resolveTurn: state.turn + plan.windup, ...(plan.collision ? { collision: { point: { ...plan.collision.point }, by: plan.collision.by } } : {}), ...(plan.cover === undefined ? {} : { cover: plan.cover }) }
  telegraphs.push(telegraph)
  recordTelegraph(state, plan.actionId)
  log(state, `${plan.sourceId} announces ${plan.actionId}.`)
  return telegraph
}

export const resolveTelegraphs = (state: RunState): Telegraph[] => {
  const telegraphs = state.floor.telegraphs ?? []
  const resolved = telegraphs.filter(telegraph => telegraph.resolveTurn <= state.turn).sort((first, second) => first.resolveTurn - second.resolveTurn || first.id.localeCompare(second.id))
  state.floor.telegraphs = telegraphs.filter(telegraph => telegraph.resolveTurn > state.turn)
  for (const telegraph of resolved) log(state, `${telegraph.actionId} resolves.`)
  return resolved
}
