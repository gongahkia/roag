import { generateFloor } from '../world'
import type { Hero, RunState } from '../types'
import { refreshFov } from './visibility'

export const newHero = (): Hero => ({
  x: 0, y: 0, health: 22, maxHealth: 22, focus: 8, maxFocus: 8, gold: 0, bombs: 4, ropes: 4, keys: 0, xp: 0, level: 1,
  stats: { strength: 2, agility: 2, vitality: 2, intellect: 2 }, skills: [], inventory: ['tonic', 'rock', 'bombPack', 'ropeBundle', 'ember'], equipment: { mainHand: 'whip' }, conditions: [], cooldowns: {}
})

export function newRun(seed = Math.floor(Math.random() * 0x7fffffff)): RunState {
  const floor = generateFloor(seed, 0)
  const hero = newHero()
  hero.x = floor.start.x
  hero.y = floor.start.y
  const state: RunState = { version: 2, seed, floor, hero, messages: ['You enter the Shale Mine.', 'H opens help.'], status: 'playing', turn: 0 }
  refreshFov(state)
  return state
}
