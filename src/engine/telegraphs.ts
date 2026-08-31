import type { Point, RunState, TacticalIntentCategory, TacticalIntentResponse, TacticalIntentSourceKind, Telegraph, TelegraphDanger } from '../types'
import { log } from './shared'
import { recordTelegraph } from './encyclopedia'

export interface TelegraphPlan {
  id: string
  sourceId: string
  actionId: string
  cells: readonly Point[]
  danger: TelegraphDanger
  windup: number
  sourceKind?: TacticalIntentSourceKind
  category?: TacticalIntentCategory
  responses?: TacticalIntentResponse[]
  collision?: { point: Point; by: string }
  cover?: boolean
}

const recordIntentState = (state: RunState, id: string, intentState: 'resolved' | 'cancelled'): void => {
  const history = state.floor.tacticalIntentHistory ??= []
  if (history.some(entry => entry.id === id)) return
  history.push({ id, state: intentState, turn: state.turn })
  if (history.length > 16) history.splice(0, history.length - 16)
}

export const announceTelegraph = (state: RunState, plan: TelegraphPlan): Telegraph => {
  if (!Number.isInteger(plan.windup) || plan.windup < 1) throw new Error(`invalid telegraph windup: ${plan.windup}`)
  const telegraphs = state.floor.telegraphs ??= []
  if (telegraphs.some(telegraph => telegraph.id === plan.id)) throw new Error(`duplicate telegraph: ${plan.id}`)
  const telegraph: Telegraph = { id: plan.id, sourceId: plan.sourceId, actionId: plan.actionId, cells: plan.cells.map(cell => ({ ...cell })), danger: plan.danger, declaredTurn: state.turn, resolveTurn: state.turn + plan.windup, sourceKind: plan.sourceKind ?? 'actor', category: plan.category ?? 'enemyAttack', responses: [...(plan.responses ?? ['move'])], state: 'pending', ...(plan.collision ? { collision: { point: { ...plan.collision.point }, by: plan.collision.by } } : {}), ...(plan.cover === undefined ? {} : { cover: plan.cover }) }
  telegraphs.push(telegraph)
  recordTelegraph(state, plan.actionId)
  log(state, `${plan.sourceId} announces ${plan.actionId}.`)
  return telegraph
}

export const resolveTelegraphs = (state: RunState): Telegraph[] => {
  const telegraphs = state.floor.telegraphs ?? []
  const resolved = telegraphs.filter(telegraph => (telegraph.state ?? 'pending') === 'pending' && telegraph.resolveTurn <= state.turn).sort((first, second) => first.resolveTurn - second.resolveTurn || first.id.localeCompare(second.id))
  state.floor.telegraphs = telegraphs.filter(telegraph => !resolved.some(current => current.id === telegraph.id))
  for (const telegraph of resolved) {
    telegraph.state = 'resolved'
    recordIntentState(state, telegraph.id, 'resolved')
    log(state, `${telegraph.actionId} resolves.`)
  }
  return resolved
}

export const cancelTelegraphs = (state: RunState, matches: (telegraph: Telegraph) => boolean, reason = 'source unavailable'): Telegraph[] => {
  const cancelled = (state.floor.telegraphs ?? []).filter(telegraph => (telegraph.state ?? 'pending') === 'pending' && matches(telegraph)).sort((first, second) => first.id.localeCompare(second.id))
  if (!cancelled.length) return []
  state.floor.telegraphs = (state.floor.telegraphs ?? []).filter(telegraph => !cancelled.some(current => current.id === telegraph.id))
  for (const telegraph of cancelled) {
    telegraph.state = 'cancelled'
    recordIntentState(state, telegraph.id, 'cancelled')
    log(state, `${telegraph.actionId} cancelled: ${reason}.`)
  }
  return cancelled
}
