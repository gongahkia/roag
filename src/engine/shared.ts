import { ITEM } from '../content'
import { rngFor, type RngScope, type RngStream } from '../rng'
import type { Hero, RunState } from '../types'
import { evaluateEquipmentEffects } from './equipment'
import { recordTelemetryCount } from '../telemetry'

export type GameEventType = 'move' | 'traverse' | 'encounter' | 'hit' | 'hurt' | 'pickup' | 'spell' | 'boom' | 'danger' | 'menu' | 'level' | 'rope' | 'suspend' | 'death' | 'win' | 'floor' | 'areaComplete' | 'gateResolved' | 'rescue' | 'terrain'
export interface GameEvent { type: GameEventType; id?: string; reason?: string }
export type ActionResult = GameEvent[]
export const event = (type: GameEventType, id?: string, reason?: string): GameEvent => ({ type, ...(id ? { id } : {}), ...(reason ? { reason } : {}) })
export const eventLabel = (value: GameEvent): string => value.type === 'terrain' ? `terrain:${value.id ?? 'unknown'}:${value.reason ?? 'unknown'}` : value.type
export const hasEvent = (events: readonly GameEvent[], type: GameEventType): boolean => events.some(event => event.type === type)

export const equipmentDefense = (hero: Hero) => evaluateEquipmentEffects(hero, 'passive', {}, { defense: Object.values(hero.equipment).reduce((total, id) => total + (id ? ITEM[id].defense ?? 0 : 0), 0) }).values.defense ?? 0
export const consume = (state: RunState, index: number) => {
  const consumed = state.hero.inventory.splice(index, 1)
  if (consumed[0]) recordTelemetryCount(state, 'itemsUsed', consumed[0])
  return consumed
}
export const distance = (a: { x: number; y: number }, b: { x: number; y: number }) => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))
export const log = (state: RunState, message: string) => { state.messages.unshift(message); state.messages = state.messages.slice(0, 9) }
export const turnRng = (state: RunState, stream: RngStream, scope: RngScope) => rngFor(state.seed, stream, state.floor.index, state.turn, scope)
