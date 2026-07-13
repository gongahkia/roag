import { ITEM } from '../content'
import { rngFor, type RngScope, type RngStream } from '../rng'
import type { Hero, RunState } from '../types'

export type GameEventType = 'move' | 'hit' | 'hurt' | 'pickup' | 'spell' | 'boom' | 'danger' | 'menu' | 'death' | 'win' | 'floor' | 'areaComplete' | 'gateResolved' | 'rescue'
export interface GameEvent { type: GameEventType }
export type ActionResult = GameEvent[]
export const event = (type: GameEventType): GameEvent => ({ type })
export const hasEvent = (events: readonly GameEvent[], type: GameEventType): boolean => events.some(event => event.type === type)

export const equipmentDefense = (hero: Hero) => Object.values(hero.equipment).reduce((total, id) => total + (id ? ITEM[id].defense ?? 0 : 0), 0)
export const consume = (state: RunState, index: number) => state.hero.inventory.splice(index, 1)
export const distance = (a: { x: number; y: number }, b: { x: number; y: number }) => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))
export const log = (state: RunState, message: string) => { state.messages.unshift(message); state.messages = state.messages.slice(0, 9) }
export const turnRng = (state: RunState, stream: RngStream, scope: RngScope) => rngFor(state.seed, stream, state.floor.index, state.turn, scope)
