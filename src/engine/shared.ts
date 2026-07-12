import { ITEM } from '../content'
import { mixSeed, Rng } from '../rng'
import type { Hero, RunState } from '../types'

export type GameEvent = 'move' | 'hit' | 'hurt' | 'pickup' | 'spell' | 'boom' | 'danger' | 'menu' | 'death' | 'win' | 'floor'

export const equipmentDefense = (hero: Hero) => Object.values(hero.equipment).reduce((total, id) => total + (id ? ITEM[id].defense ?? 0 : 0), 0)
export const consume = (state: RunState, index: number) => state.hero.inventory.splice(index, 1)
export const distance = (a: { x: number; y: number }, b: { x: number; y: number }) => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))
export const log = (state: RunState, message: string) => { state.messages.unshift(message); state.messages = state.messages.slice(0, 9) }
export const turnRng = (state: RunState) => new Rng(mixSeed(state.seed, state.turn * 97 + state.floor.index * 13))
